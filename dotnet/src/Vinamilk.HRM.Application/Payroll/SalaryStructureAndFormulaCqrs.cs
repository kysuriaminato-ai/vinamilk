using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NCalc;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Domain.Entities.Core;
using Vinamilk.HRM.Domain.Entities.Payroll;
using Vinamilk.HRM.Domain.Entities.SystemEntities;
using Vinamilk.HRM.Domain.Entities.Time;

namespace Vinamilk.HRM.Application.Payroll;

// ============================================================================
// 1. NCALC FORMULA ENGINE + CYCLE DETECTION + MID-MONTH SALARY SPLITTER
// ============================================================================

public sealed record FormulaComponentSpec(
    int SalaryComponentId,
    string ComponentCode,
    string ComponentName,
    string ComponentType, // Earning | Allowance | Bonus | Deduction | Info
    string CalculationType, // Fixed | Formula | System
    decimal DefaultAmount,
    string? FormulaExpression,
    int SortOrder,
    bool IsTaxable,
    decimal TaxExemptLimit,
    string? TaxExemptLimitParameterCode,
    bool IsSocialInsuranceBase,
    bool ShowOnPayslip);

public sealed record FormulaValidationResultDto(
    bool IsValid,
    bool HasSyntaxError,
    bool HasCircularDependency,
    IReadOnlyList<string> CyclePath,
    IReadOnlyList<string> ReferencedIdentifiers,
    IReadOnlyList<string> DependencyOrder,
    decimal? EvaluatedAmount,
    decimal? TaxableAmount,
    string? ErrorMessage);

public sealed record EvaluatedSalaryLineDto(
    int SalaryComponentId,
    string ComponentCode,
    string ComponentName,
    string ComponentType,
    decimal RawValue,
    decimal FinalAmount,
    decimal TaxableAmount,
    bool IsTaxable,
    bool IsSocialInsuranceBase,
    bool ShowOnPayslip,
    string? Note);

public sealed record FormulaEvaluationBatchResult(
    IReadOnlyList<EvaluatedSalaryLineDto> Lines,
    IReadOnlyDictionary<string, decimal> ResolvedValues,
    decimal TotalEarningsAndAllowances,
    decimal TotalCustomDeductions,
    decimal TotalTaxExemptAmount,
    decimal? CustomInsuranceSalaryBase);

public static partial class NCalcSalaryFormulaEngine
{
    private static readonly HashSet<string> BuiltInIdentifiers = new(StringComparer.OrdinalIgnoreCase)
    {
        "if", "iif", "min", "max", "round", "abs", "floor", "ceiling", "pow", "sqrt", "clamp",
        "true", "false", "and", "or", "not", "in", "pi", "e"
    };

    [GeneratedRegex(@"\[([A-Za-z_][A-Za-z0-9_]*)\]|'[^']*'|\b([A-Za-z_][A-Za-z0-9_]*)\b(\s*\()?")]
    private static partial Regex IdentifierTokenRegex();

    public static IReadOnlyList<string> ExtractReferencedIdentifiers(string? formula)
    {
        if (string.IsNullOrWhiteSpace(formula))
            return Array.Empty<string>();

        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in IdentifierTokenRegex().Matches(formula))
        {
            // Bracketed [IDENTIFIER]
            if (match.Groups[1].Success)
            {
                var bracketCode = match.Groups[1].Value;
                if (!BuiltInIdentifiers.Contains(bracketCode) && seen.Add(bracketCode))
                    result.Add(bracketCode);
                continue;
            }

            // Bare identifier NOT followed by '(' (not a function call)
            if (match.Groups[2].Success && !match.Groups[3].Success)
            {
                var ident = match.Groups[2].Value;
                if (!BuiltInIdentifiers.Contains(ident) && seen.Add(ident))
                    result.Add(ident);
            }
        }

        return result;
    }

    public static (bool HasCycle, IReadOnlyList<string> CyclePath, IReadOnlyList<FormulaComponentSpec> TopologicalOrder)
        DetectCycleAndSort(IReadOnlyList<FormulaComponentSpec> nodes)
    {
        var byCode = nodes
            .GroupBy(n => n.ComponentCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var deps = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in nodes)
        {
            var refs = string.Equals(node.CalculationType, "Fixed", StringComparison.OrdinalIgnoreCase)
                ? new List<string>()
                : ExtractReferencedIdentifiers(node.FormulaExpression)
                    .Where(id => byCode.ContainsKey(id))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            deps[node.ComponentCode] = refs;
        }

        var state = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); // 0=unvisited,1=visiting,2=visited
        var stack = new List<string>();
        var topo = new List<FormulaComponentSpec>();

        IReadOnlyList<string>? Dfs(string code)
        {
            state[code] = 1;
            stack.Add(code);

            if (deps.TryGetValue(code, out var nextList))
            {
                foreach (var depCode in nextList)
                {
                    if (!state.TryGetValue(depCode, out var st) || st == 0)
                    {
                        var cycle = Dfs(depCode);
                        if (cycle != null)
                            return cycle;
                    }
                    else if (st == 1)
                    {
                        int idx = stack.FindIndex(x => string.Equals(x, depCode, StringComparison.OrdinalIgnoreCase));
                        var path = stack.Skip(idx).ToList();
                        path.Add(depCode);
                        return path;
                    }
                }
            }

            stack.RemoveAt(stack.Count - 1);
            state[code] = 2;
            if (byCode.TryGetValue(code, out var spec))
                topo.Add(spec);
            return null;
        }

        foreach (var node in nodes.OrderBy(n => n.SortOrder).ThenBy(n => n.ComponentCode))
        {
            if (!state.TryGetValue(node.ComponentCode, out var st) || st == 0)
            {
                var cycle = Dfs(node.ComponentCode);
                if (cycle != null)
                    return (true, cycle, Array.Empty<FormulaComponentSpec>());
            }
        }

        return (false, Array.Empty<string>(), topo);
    }

    public static FormulaValidationResultDto ValidateAndEvaluateSingle(
        string? formulaExpression,
        string? targetComponentCode,
        IReadOnlyList<FormulaComponentSpec> existingComponents,
        IReadOnlyDictionary<string, decimal>? sampleVariables = null,
        bool isTaxable = true,
        decimal taxExemptLimit = 0m)
    {
        var refs = ExtractReferencedIdentifiers(formulaExpression);
        if (string.IsNullOrWhiteSpace(formulaExpression))
        {
            return new FormulaValidationResultDto(
                IsValid: false,
                HasSyntaxError: true,
                HasCircularDependency: false,
                CyclePath: Array.Empty<string>(),
                ReferencedIdentifiers: refs,
                DependencyOrder: Array.Empty<string>(),
                EvaluatedAmount: null,
                TaxableAmount: null,
                ErrorMessage: "Công thức không được để trống khi ở chế độ Formula.");
        }

        // 1. Validate NCalc syntax first
        if (!TryCompileNCalc(formulaExpression, out var syntaxError))
        {
            return new FormulaValidationResultDto(
                IsValid: false,
                HasSyntaxError: true,
                HasCircularDependency: false,
                CyclePath: Array.Empty<string>(),
                ReferencedIdentifiers: refs,
                DependencyOrder: Array.Empty<string>(),
                EvaluatedAmount: null,
                TaxableAmount: null,
                ErrorMessage: $"Lỗi cú pháp công thức NCalc: {syntaxError}");
        }

        // 2. Check circular dependencies by substituting or adding targetComponentCode in graph
        var code = string.IsNullOrWhiteSpace(targetComponentCode) ? "__TEST_TARGET__" : targetComponentCode.Trim().ToUpperInvariant();
        var merged = existingComponents
            .Where(c => !string.Equals(c.ComponentCode, code, StringComparison.OrdinalIgnoreCase))
            .ToList();

        merged.Add(new FormulaComponentSpec(
            0,
            code,
            code,
            "Earning",
            "Formula",
            0m,
            formulaExpression,
            existingComponents.Count + 1,
            isTaxable,
            taxExemptLimit,
            null,
            false,
            true));

        var (hasCycle, cyclePath, topoOrder) = DetectCycleAndSort(merged);
        if (hasCycle)
        {
            return new FormulaValidationResultDto(
                IsValid: false,
                HasSyntaxError: false,
                HasCircularDependency: true,
                CyclePath: cyclePath,
                ReferencedIdentifiers: refs,
                DependencyOrder: Array.Empty<string>(),
                EvaluatedAmount: null,
                TaxableAmount: null,
                ErrorMessage: $"Phát hiện vòng lặp tham chiếu công thức: {string.Join(" -> ", cyclePath)}");
        }

        // 3. Evaluate sample using topological order so multi-level references work in preview!
        try
        {
            var vars = BuildDefaultSampleVariables();
            if (sampleVariables != null)
            {
                foreach (var kv in sampleVariables)
                    vars[kv.Key] = kv.Value;
            }

            var batch = EvaluateOrderedComponents(topoOrder, vars, "RoundToDong", null);
            batch.ResolvedValues.TryGetValue(code, out var evalVal);
            var targetLine = batch.Lines.FirstOrDefault(l => string.Equals(l.ComponentCode, code, StringComparison.OrdinalIgnoreCase));

            return new FormulaValidationResultDto(
                IsValid: true,
                HasSyntaxError: false,
                HasCircularDependency: false,
                CyclePath: Array.Empty<string>(),
                ReferencedIdentifiers: refs,
                DependencyOrder: topoOrder.Select(t => t.ComponentCode).ToList(),
                EvaluatedAmount: evalVal,
                TaxableAmount: targetLine?.TaxableAmount ?? evalVal,
                ErrorMessage: null);
        }
        catch (Exception ex)
        {
            return new FormulaValidationResultDto(
                IsValid: false,
                HasSyntaxError: true,
                HasCircularDependency: false,
                CyclePath: Array.Empty<string>(),
                ReferencedIdentifiers: refs,
                DependencyOrder: topoOrder.Select(t => t.ComponentCode).ToList(),
                EvaluatedAmount: null,
                TaxableAmount: null,
                ErrorMessage: $"Lỗi đánh giá công thức: {ex.Message}");
        }
    }

    public static FormulaEvaluationBatchResult EvaluateComponents(
        IReadOnlyList<FormulaComponentSpec> components,
        IDictionary<string, decimal> initialVariables,
        string roundingRule = "RoundToDong",
        IReadOnlyDictionary<string, decimal>? taxExemptParamValues = null)
    {
        var (hasCycle, cyclePath, topoOrder) = DetectCycleAndSort(components);
        if (hasCycle)
            throw new InvalidOperationException($"Không thể tính công thức do phát hiện vòng lặp tham chiếu: {string.Join(" -> ", cyclePath)}");

        return EvaluateOrderedComponents(topoOrder, initialVariables, roundingRule, taxExemptParamValues);
    }

    private static FormulaEvaluationBatchResult EvaluateOrderedComponents(
        IReadOnlyList<FormulaComponentSpec> topoOrder,
        IDictionary<string, decimal> variables,
        string roundingRule,
        IReadOnlyDictionary<string, decimal>? taxExemptParamValues)
    {
        var workingVars = new Dictionary<string, decimal>(variables, StringComparer.OrdinalIgnoreCase);
        var lines = new List<EvaluatedSalaryLineDto>(topoOrder.Count);

        decimal totalEarnings = 0m;
        decimal totalDeductions = 0m;
        decimal totalExempt = 0m;
        decimal? customInsBase = null;
        bool hasInsBaseSpec = false;
        decimal insBaseAccum = 0m;

        foreach (var comp in topoOrder)
        {
            decimal rawVal;
            string? note;

            if (string.Equals(comp.CalculationType, "Fixed", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(comp.FormulaExpression))
            {
                rawVal = workingVars.TryGetValue(comp.ComponentCode, out var preSet)
                    ? preSet
                    : comp.DefaultAmount;
                note = "Giá trị cố định / gán từ hồ sơ";
            }
            else
            {
                rawVal = EvaluateNCalcDecimal(comp.FormulaExpression, workingVars);
                note = $"Công thức: {comp.FormulaExpression}";
            }

            rawVal = Math.Round(rawVal, 4, MidpointRounding.AwayFromZero);

            bool isInfo = string.Equals(comp.ComponentType, "Info", StringComparison.OrdinalIgnoreCase);
            decimal finalAmount = isInfo
                ? rawVal
                : ApplyRounding(rawVal, roundingRule);

            // Store intermediate precision (decimal 18,4) for Info components or final value so multi-level formulas stay exact!
            workingVars[comp.ComponentCode] = isInfo ? rawVal : finalAmount;

            decimal exemptLimit = comp.TaxExemptLimit;
            if (!string.IsNullOrWhiteSpace(comp.TaxExemptLimitParameterCode) &&
                taxExemptParamValues != null &&
                taxExemptParamValues.TryGetValue(comp.TaxExemptLimitParameterCode, out var paramVal))
            {
                exemptLimit = paramVal;
            }

            decimal taxableAmount = 0m;
            decimal effectiveExempt = 0m;
            if (!isInfo && !string.Equals(comp.ComponentType, "Deduction", StringComparison.OrdinalIgnoreCase))
            {
                if (!comp.IsTaxable)
                {
                    effectiveExempt = Math.Max(0m, finalAmount);
                    taxableAmount = 0m;
                }
                else if (exemptLimit > 0m)
                {
                    effectiveExempt = Math.Min(Math.Max(0m, finalAmount), exemptLimit);
                    taxableAmount = Math.Max(0m, finalAmount - effectiveExempt);
                }
                else
                {
                    taxableAmount = finalAmount;
                }
            }

            if (comp.ComponentType is "Earning" or "Allowance" or "Bonus")
            {
                totalEarnings += finalAmount;
                totalExempt += effectiveExempt;
                if (comp.IsSocialInsuranceBase)
                {
                    hasInsBaseSpec = true;
                    insBaseAccum += finalAmount;
                }
            }
            else if (string.Equals(comp.ComponentType, "Deduction", StringComparison.OrdinalIgnoreCase))
            {
                totalDeductions += finalAmount;
            }

            lines.Add(new EvaluatedSalaryLineDto(
                comp.SalaryComponentId,
                comp.ComponentCode,
                comp.ComponentName,
                comp.ComponentType,
                rawVal,
                finalAmount,
                taxableAmount,
                comp.IsTaxable,
                comp.IsSocialInsuranceBase,
                comp.ShowOnPayslip,
                note));
        }

        if (hasInsBaseSpec)
            customInsBase = insBaseAccum;

        return new FormulaEvaluationBatchResult(
            lines,
            workingVars,
            totalEarnings,
            totalDeductions,
            totalExempt,
            customInsBase);
    }

    public static decimal ApplyRounding(decimal value, string roundingRule) => roundingRule switch
    {
        "RoundToThousand" => Math.Round(value / 1000m, 0, MidpointRounding.AwayFromZero) * 1000m,
        "RoundHalfToEven" => Math.Round(value, 0, MidpointRounding.ToEven),
        _ => Math.Round(value, 0, MidpointRounding.AwayFromZero)
    };

    private static bool TryCompileNCalc(string expressionText, out string? errorMessage)
    {
        try
        {
            var expr = new Expression(expressionText, ExpressionOptions.IgnoreCaseAtBuiltInFunctions);
            if (expr.HasErrors())
            {
                errorMessage = expr.Error?.ToString() ?? "Lỗi cú pháp NCalc.";
                return false;
            }
            errorMessage = null;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    public static decimal EvaluateNCalcDecimal(string expressionText, IReadOnlyDictionary<string, decimal> variables)
    {
        var expr = new Expression(expressionText, ExpressionOptions.IgnoreCaseAtBuiltInFunctions);
        if (expr.HasErrors())
            throw new InvalidOperationException($"Lỗi cú pháp công thức '{expressionText}': {expr.Error}");

        expr.EvaluateParameter += (name, args) =>
        {
            if (variables.TryGetValue(name, out var decVal))
            {
                args.Result = Convert.ToDouble(decVal, CultureInfo.InvariantCulture);
            }
            else
            {
                args.Result = 0d;
            }
        };

        expr.EvaluateFunction += (name, args) =>
        {
            var fn = name.ToLowerInvariant();
            if (fn == "min" && args.Parameters.Count >= 2)
            {
                decimal best = Convert.ToDecimal(args.Parameters.Evaluate(0), CultureInfo.InvariantCulture);
                for (int i = 1; i < args.Parameters.Count; i++)
                {
                    var candidate = Convert.ToDecimal(args.Parameters.Evaluate(i), CultureInfo.InvariantCulture);
                    if (candidate < best) best = candidate;
                }
                args.Result = Convert.ToDouble(best, CultureInfo.InvariantCulture);
            }
            else if (fn == "max" && args.Parameters.Count >= 2)
            {
                decimal best = Convert.ToDecimal(args.Parameters.Evaluate(0), CultureInfo.InvariantCulture);
                for (int i = 1; i < args.Parameters.Count; i++)
                {
                    var candidate = Convert.ToDecimal(args.Parameters.Evaluate(i), CultureInfo.InvariantCulture);
                    if (candidate > best) best = candidate;
                }
                args.Result = Convert.ToDouble(best, CultureInfo.InvariantCulture);
            }
            else if (fn == "clamp" && args.Parameters.Count == 3)
            {
                var x = Convert.ToDecimal(args.Parameters.Evaluate(0), CultureInfo.InvariantCulture);
                var lo = Convert.ToDecimal(args.Parameters.Evaluate(1), CultureInfo.InvariantCulture);
                var hi = Convert.ToDecimal(args.Parameters.Evaluate(2), CultureInfo.InvariantCulture);
                args.Result = Convert.ToDouble(Math.Clamp(x, lo, hi), CultureInfo.InvariantCulture);
            }
            else if (fn == "round" && args.Parameters.Count is 1 or 2)
            {
                var x = Convert.ToDecimal(args.Parameters.Evaluate(0), CultureInfo.InvariantCulture);
                int digits = args.Parameters.Count == 2 ? Convert.ToInt32(args.Parameters.Evaluate(1), CultureInfo.InvariantCulture) : 0;
                args.Result = Convert.ToDouble(Math.Round(x, Math.Clamp(digits, 0, 4), MidpointRounding.AwayFromZero), CultureInfo.InvariantCulture);
            }
            else if (fn == "iif" && args.Parameters.Count == 3)
            {
                var cond = Convert.ToBoolean(args.Parameters.Evaluate(0), CultureInfo.InvariantCulture);
                args.Result = cond ? args.Parameters.Evaluate(1) : args.Parameters.Evaluate(2);
            }
        };

        var evaluated = expr.Evaluate();
        if (evaluated == null)
            return 0m;

        return Convert.ToDecimal(evaluated, CultureInfo.InvariantCulture);
    }

    public static Dictionary<string, decimal> BuildDefaultSampleVariables() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["LUONG_CO_BAN"] = 20_000_000m,
        ["LUONG_CO_BAN_HIEN_TAI"] = 20_000_000m,
        ["LUONG_DONG_BH"] = 20_000_000m,
        ["PHU_CAP_CHUC_VU"] = 2_000_000m,
        ["PHU_CAP_KHAC"] = 1_000_000m,
        ["CONG_CHUAN"] = 26m,
        ["CONG_THUC_TE"] = 26m,
        ["TY_LE_THU_VIEC"] = 1.0m,
        ["OT150"] = 4m,
        ["OT200"] = 0m,
        ["OT300"] = 0m,
        ["OT_TONG"] = 4m,
        ["GIO_DEM"] = 16m,
        ["GIO_LAM_VIEC_NGAY"] = 8m,
        ["DON_GIA_NGAY"] = 0m,
        ["DON_GIA_GIO"] = 0m,
        ["DON_GIA_SP"] = 0m,
        ["SO_SAN_PHAM"] = 0m,
        ["SO_GIO_LAM_VIEC"] = 208m,
        ["DIEM_KPI"] = 1.0m,
        ["HOA_HONG_DOANH_SO"] = 0m,
        ["PC_DOC_HAI"] = 500_000m,
        ["PC_AN_CA"] = 730_000m,
        ["DOAN_VIEN_CD"] = 1m
    };
}

public sealed record PayrollPeriodProfileSliceInput(
    DateOnly SegmentStart,
    DateOnly SegmentEnd,
    decimal SegmentPayableWorkdays,
    decimal SegmentOt150,
    decimal SegmentOt200,
    decimal SegmentOt300,
    decimal SegmentNightHours,
    EmployeeSalaryProfile Profile,
    IReadOnlyList<FormulaComponentSpec>? TemplateComponents);

public sealed record MidMonthPayrollSliceBreakdown(
    DateOnly SegmentStart,
    DateOnly SegmentEnd,
    decimal SegmentWorkdays,
    string PayMode,
    decimal BaseSalary,
    decimal ActualBaseSalary,
    decimal PositionAllowance,
    decimal OtherAllowance,
    decimal OvertimePay,
    decimal ExtraTemplateEarnings,
    decimal CustomDeductions,
    decimal TaxExemptAmount,
    decimal InsuranceSalaryBaseUsed,
    IReadOnlyList<EvaluatedSalaryLineDto> Lines);

public static class MidMonthSalarySplitter
{
    public static int CountWorkingDaysInRange(DateOnly start, DateOnly end)
    {
        if (end < start)
            return 0;
        int count = 0;
        for (var d = start; d <= end; d = d.AddDays(1))
        {
            if (d.DayOfWeek != DayOfWeek.Sunday)
                count++;
        }
        return count;
    }

    public static IReadOnlyList<PayrollPeriodProfileSliceInput> BuildPeriodSlices(
        DateOnly periodStart,
        DateOnly periodEnd,
        decimal totalPayableWorkdays,
        decimal totalOt150,
        decimal totalOt200,
        decimal totalOt300,
        decimal totalNightHours,
        IReadOnlyList<EmployeeSalaryProfile> orderedProfiles,
        IReadOnlyDictionary<int, IReadOnlyList<FormulaComponentSpec>> templateComponentMap,
        IReadOnlyList<DailyAttendance>? dailyAttendances = null)
    {
        var activeInPeriod = orderedProfiles
            .Where(p => p.IsActive && p.EffectiveFrom <= periodEnd && (!p.EffectiveTo.HasValue || p.EffectiveTo.Value >= periodStart))
            .OrderBy(p => p.EffectiveFrom)
            .ToList();

        if (activeInPeriod.Count == 0)
            return Array.Empty<PayrollPeriodProfileSliceInput>();

        var rawIntervals = new List<(DateOnly Start, DateOnly End, EmployeeSalaryProfile Profile)>();
        for (int i = 0; i < activeInPeriod.Count; i++)
        {
            var prof = activeInPeriod[i];
            var segStart = prof.EffectiveFrom > periodStart ? prof.EffectiveFrom : periodStart;
            var nextStartMinus1 = i + 1 < activeInPeriod.Count
                ? activeInPeriod[i + 1].EffectiveFrom.AddDays(-1)
                : periodEnd;
            var profEnd = prof.EffectiveTo.HasValue && prof.EffectiveTo.Value < periodEnd
                ? prof.EffectiveTo.Value
                : periodEnd;
            var segEnd = nextStartMinus1 < profEnd ? nextStartMinus1 : profEnd;

            // If employee started the period before the first profile's EffectiveFrom, attach gap to earliest active profile
            if (i == 0 && segStart > periodStart)
                segStart = periodStart;

            if (segEnd >= segStart)
                rawIntervals.Add((segStart, segEnd, prof));
        }

        if (rawIntervals.Count == 0)
            return Array.Empty<PayrollPeriodProfileSliceInput>();

        if (rawIntervals.Count == 1)
        {
            var only = rawIntervals[0];
            templateComponentMap.TryGetValue(only.Profile.SalaryTemplateId ?? 0, out var specs);
            return new[]
            {
                new PayrollPeriodProfileSliceInput(
                    only.Start, only.End, totalPayableWorkdays, totalOt150, totalOt200, totalOt300, totalNightHours, only.Profile, specs)
            };
        }

        // Split workdays & OT proportionally using DailyAttendance logs per date range when present, otherwise working-day weights!
        var dayWeights = new decimal[rawIntervals.Count];
        var ot150Seg = new decimal[rawIntervals.Count];
        var ot200Seg = new decimal[rawIntervals.Count];
        var ot300Seg = new decimal[rawIntervals.Count];
        var nightSeg = new decimal[rawIntervals.Count];

        bool hasDailyLogs = dailyAttendances != null && dailyAttendances.Count > 0;
        if (hasDailyLogs)
        {
            for (int i = 0; i < rawIntervals.Count; i++)
            {
                var (s, e, _) = rawIntervals[i];
                var segRows = dailyAttendances!.Where(d => d.AttendanceDate >= s && d.AttendanceDate <= e).ToList();
                dayWeights[i] = segRows.Sum(d => d.WorkdayCount);
                ot150Seg[i] = segRows.Sum(d => d.OvertimeHours150);
                ot200Seg[i] = segRows.Sum(d => d.OvertimeHours200);
                ot300Seg[i] = segRows.Sum(d => d.OvertimeHours300);
                nightSeg[i] = segRows.Sum(d => d.NightShiftHours);
            }
        }

        if (!hasDailyLogs || dayWeights.Sum() <= 0m)
        {
            for (int i = 0; i < rawIntervals.Count; i++)
                dayWeights[i] = Math.Max(0, CountWorkingDaysInRange(rawIntervals[i].Start, rawIntervals[i].End));
        }

        var workdaysSplit = DistributeTotal(totalPayableWorkdays, dayWeights);
        var ot150Split = DistributeByReferenceOrWeights(totalOt150, ot150Seg, dayWeights);
        var ot200Split = DistributeByReferenceOrWeights(totalOt200, ot200Seg, dayWeights);
        var ot300Split = DistributeByReferenceOrWeights(totalOt300, ot300Seg, dayWeights);
        var nightSplit = DistributeByReferenceOrWeights(totalNightHours, nightSeg, dayWeights);

        var result = new List<PayrollPeriodProfileSliceInput>(rawIntervals.Count);
        for (int i = 0; i < rawIntervals.Count; i++)
        {
            var (s, e, prof) = rawIntervals[i];
            templateComponentMap.TryGetValue(prof.SalaryTemplateId ?? 0, out var specs);
            result.Add(new PayrollPeriodProfileSliceInput(
                s, e, workdaysSplit[i], ot150Split[i], ot200Split[i], ot300Split[i], nightSplit[i], prof, specs));
        }

        return result;
    }

    private static decimal[] DistributeByReferenceOrWeights(decimal total, decimal[] observed, decimal[] fallbackWeights)
    {
        if (observed.Sum() > 0m)
            return DistributeTotal(total, observed);
        return DistributeTotal(total, fallbackWeights);
    }

    private static decimal[] DistributeTotal(decimal total, decimal[] weights)
    {
        var shares = new decimal[weights.Length];
        if (weights.Length == 0 || total == 0m)
            return shares;

        decimal weightSum = weights.Sum();
        if (weightSum <= 0m)
        {
            shares[^1] = total;
            return shares;
        }

        decimal assigned = 0m;
        for (int i = 0; i < weights.Length - 1; i++)
        {
            var part = Math.Round(total * (weights[i] / weightSum), 4, MidpointRounding.AwayFromZero);
            shares[i] = part;
            assigned += part;
        }
        shares[^1] = Math.Max(0m, total - assigned);
        return shares;
    }

    public static MidMonthPayrollSliceBreakdown EvaluateSlice(
        PayrollPeriodProfileSliceInput slice,
        decimal standardWorkdays,
        string roundingRule,
        IReadOnlyDictionary<string, decimal>? taxExemptParamValues = null,
        decimal extraCommissionOrKpi = 0m)
    {
        var p = slice.Profile;
        decimal probationFactor = p.IsProbation ? Math.Clamp(p.ProbationRatePercent, 0m, 200m) / 100m : 1.0m;
        decimal effectiveBase = Math.Round(p.BaseSalary * probationFactor, 0, MidpointRounding.AwayFromZero);
        decimal workRatio = standardWorkdays > 0m ? slice.SegmentPayableWorkdays / standardWorkdays : 0m;

        // Calculate actual base pay according to PayMode (Monthly | Daily | Hourly | PieceRate)
        decimal actualBase = p.PayMode switch
        {
            "Daily" => NCalcSalaryFormulaEngine.ApplyRounding(
                (p.UnitRate > 0m ? p.UnitRate : (standardWorkdays > 0m ? effectiveBase / standardWorkdays : 0m)) * slice.SegmentPayableWorkdays,
                roundingRule),
            "Hourly" => NCalcSalaryFormulaEngine.ApplyRounding(
                (p.UnitRate > 0m ? p.UnitRate : (standardWorkdays > 0m ? effectiveBase / (standardWorkdays * 8m) : 0m)) * (slice.SegmentPayableWorkdays * 8m),
                roundingRule),
            "PieceRate" => NCalcSalaryFormulaEngine.ApplyRounding(
                (p.UnitRate > 0m ? p.UnitRate * slice.SegmentPayableWorkdays : effectiveBase * workRatio),
                roundingRule),
            _ => NCalcSalaryFormulaEngine.ApplyRounding(effectiveBase * workRatio, roundingRule)
        };

        var hourlyRate = standardWorkdays > 0m ? effectiveBase / (standardWorkdays * 8m) : 0m;
        var defaultOtPay = NCalcSalaryFormulaEngine.ApplyRounding(
            hourlyRate * (slice.SegmentOt150 * 1.5m + slice.SegmentOt200 * 2.0m + slice.SegmentOt300 * 3.0m),
            roundingRule);

        var posAllow = NCalcSalaryFormulaEngine.ApplyRounding(p.PositionAllowance * workRatio, roundingRule);
        var otherAllow = NCalcSalaryFormulaEngine.ApplyRounding(p.OtherAllowance * workRatio, roundingRule);

        var evaluatedLines = new List<EvaluatedSalaryLineDto>();
        decimal extraTemplateEarnings = 0m;
        decimal customDeductions = 0m;
        decimal taxExemptAmount = 0m;

        if (slice.TemplateComponents != null && slice.TemplateComponents.Count > 0)
        {
            var vars = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["LUONG_CO_BAN"] = p.BaseSalary,
                ["LUONG_CO_BAN_HIEN_TAI"] = effectiveBase,
                ["LUONG_DONG_BH"] = p.InsuranceSalary,
                ["PHU_CAP_CHUC_VU"] = posAllow,
                ["PHU_CAP_KHAC"] = otherAllow,
                ["BASIC_PAY"] = actualBase,
                ["POS_ALLOWANCE"] = posAllow,
                ["OTHER_ALLOWANCE"] = otherAllow,
                ["OT_PAY"] = defaultOtPay,
                ["CONG_CHUAN"] = standardWorkdays,
                ["CONG_THUC_TE"] = slice.SegmentPayableWorkdays,
                ["TY_LE_THU_VIEC"] = probationFactor,
                ["OT150"] = slice.SegmentOt150,
                ["OT200"] = slice.SegmentOt200,
                ["OT300"] = slice.SegmentOt300,
                ["OT_TONG"] = slice.SegmentOt150 + slice.SegmentOt200 + slice.SegmentOt300,
                ["GIO_DEM"] = slice.SegmentNightHours,
                ["GIO_LAM_VIEC_NGAY"] = 8m,
                ["SO_GIO_LAM_VIEC"] = slice.SegmentPayableWorkdays * 8m,
                ["DON_GIA_NGAY"] = p.PayMode == "Daily" ? p.UnitRate : (standardWorkdays > 0m ? effectiveBase / standardWorkdays : 0m),
                ["DON_GIA_GIO"] = p.PayMode == "Hourly" ? p.UnitRate : hourlyRate,
                ["DON_GIA_SP"] = p.PayMode == "PieceRate" ? p.UnitRate : 0m,
                ["HOA_HONG_DOANH_SO"] = extraCommissionOrKpi,
                ["DOAN_VIEN_CD"] = p.IsUnionMember ? 1m : 0m
            };

            foreach (var fa in p.FixedAllowances)
            {
                vars[fa.ComponentCode] = NCalcSalaryFormulaEngine.ApplyRounding(fa.Amount * workRatio, roundingRule);
            }

            // For Daily/Hourly/PieceRate profiles, keep PayMode-computed BASIC_PAY unless template formula explicitly references PayMode unit-rate variables.
            var effectiveTemplateComponents = !string.Equals(p.PayMode, "Monthly", StringComparison.OrdinalIgnoreCase)
                ? slice.TemplateComponents.Select(spec =>
                    string.Equals(spec.ComponentCode, "BASIC_PAY", StringComparison.OrdinalIgnoreCase)
                    && (string.IsNullOrWhiteSpace(spec.FormulaExpression)
                        || (!spec.FormulaExpression.Contains("DON_GIA_NGAY", StringComparison.OrdinalIgnoreCase)
                            && !spec.FormulaExpression.Contains("DON_GIA_GIO", StringComparison.OrdinalIgnoreCase)
                            && !spec.FormulaExpression.Contains("DON_GIA_SP", StringComparison.OrdinalIgnoreCase)))
                        ? spec with { CalculationType = "System", FormulaExpression = null }
                        : spec).ToList()
                : slice.TemplateComponents;

            // Include POS_ALLOWANCE / OTHER_ALLOWANCE inside vars only as defaults for System placeholders;
            // Fixed allowances from EmployeeSalaryProfile can also provide component values directly by ComponentCode.
            var formulaBatch = NCalcSalaryFormulaEngine.EvaluateComponents(effectiveTemplateComponents, vars, roundingRule, taxExemptParamValues);
            evaluatedLines.AddRange(formulaBatch.Lines);

            foreach (var line in formulaBatch.Lines)
            {
                if (string.Equals(line.ComponentType, "Info", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (string.Equals(line.ComponentCode, "BASIC_PAY", StringComparison.OrdinalIgnoreCase))
                    actualBase = line.FinalAmount;
                else if (string.Equals(line.ComponentCode, "POS_ALLOWANCE", StringComparison.OrdinalIgnoreCase))
                    posAllow = line.FinalAmount;
                else if (string.Equals(line.ComponentCode, "OTHER_ALLOWANCE", StringComparison.OrdinalIgnoreCase))
                    otherAllow = line.FinalAmount;
                else if (string.Equals(line.ComponentCode, "OT_PAY", StringComparison.OrdinalIgnoreCase))
                    defaultOtPay = line.FinalAmount;
                else if (line.ComponentType is "Earning" or "Allowance" or "Bonus")
                    extraTemplateEarnings += line.FinalAmount;
                else if (string.Equals(line.ComponentType, "Deduction", StringComparison.OrdinalIgnoreCase) &&
                         !string.Equals(line.ComponentCode, "SI_DEDUCTION", StringComparison.OrdinalIgnoreCase) &&
                         !string.Equals(line.ComponentCode, "PIT_TAX", StringComparison.OrdinalIgnoreCase))
                    customDeductions += line.FinalAmount;

                taxExemptAmount += Math.Max(0m, line.FinalAmount - line.TaxableAmount);
            }
        }

        return new MidMonthPayrollSliceBreakdown(
            slice.SegmentStart,
            slice.SegmentEnd,
            slice.SegmentPayableWorkdays,
            p.PayMode,
            effectiveBase,
            actualBase,
            posAllow,
            otherAllow,
            defaultOtPay,
            extraTemplateEarnings,
            customDeductions,
            taxExemptAmount,
            p.InsuranceSalary,
            evaluatedLines);
    }
}

// ============================================================================
// 2. DTOs, QUERIES & COMMANDS CHO QUẢN TRỊ CẤU TRÚC LƯƠNG CL5
// ============================================================================

public sealed record SalaryComponentDto(
    int Id,
    string ComponentCode,
    string ComponentName,
    string ComponentType,
    string CalculationType,
    decimal DefaultAmount,
    bool IsTaxable,
    decimal TaxExemptLimit,
    string? TaxExemptLimitParameterCode,
    bool IsSocialInsuranceBase,
    bool ShowOnPayslip,
    string? FormulaExpression,
    int SortOrder,
    bool IsActive,
    string? LegalReference,
    bool NeedsHrVerification,
    string? Description);

public sealed record SalaryTemplateComponentDto(
    int Id,
    int SalaryComponentId,
    string ComponentCode,
    string ComponentName,
    string ComponentType,
    int SortOrder,
    string? OverrideFormulaExpression,
    decimal? DefaultFixedAmount,
    bool IsRequired);

public sealed record SalaryTemplateDto(
    int Id,
    string TemplateCode,
    string TemplateName,
    string StaffGroup,
    string DefaultPayFrequency,
    decimal DefaultProbationSalaryRatePercent,
    string RoundingRule,
    bool IsDefaultForCompany,
    bool IsActive,
    string? LegalReference,
    bool NeedsHrVerification,
    string? Description,
    IReadOnlyList<SalaryTemplateComponentDto> Components);

public sealed record EmployeeSalaryAllowanceDto(
    int SalaryComponentId,
    string ComponentCode,
    string ComponentName,
    decimal Amount,
    string? Notes);

public sealed record EmployeeSalaryProfileDto(
    int Id,
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    decimal BaseSalary,
    decimal InsuranceSalary,
    int? SalaryScaleId,
    int? SalaryStepId,
    string? ScaleAndStepLabel,
    int? SalaryTemplateId,
    string? TemplateName,
    string PayMode,
    decimal UnitRate,
    bool IsProbation,
    decimal ProbationRatePercent,
    decimal PositionAllowance,
    decimal OtherAllowance,
    bool IsUnionMember,
    string? DecisionNumber,
    string? Reason,
    IReadOnlyList<EmployeeSalaryAllowanceDto> FixedAllowances);

public sealed record SalaryStructuresOverviewDto(
    IReadOnlyList<SalaryComponentDto> Components,
    IReadOnlyList<SalaryTemplateDto> Templates,
    IReadOnlyList<SimpleEmployeeLookupDto> Employees,
    IReadOnlyList<SimpleStepLookupDto> SalarySteps,
    IReadOnlyList<SimpleParamLookupDto> TaxExemptParameters,
    int? SelectedEmployeeId,
    IReadOnlyList<EmployeeSalaryProfileDto> EmployeeProfiles,
    decimal MinProbationSalaryRatePercent);

public sealed record SimpleEmployeeLookupDto(int Id, string EmployeeCode, string FullName, string UnitName, decimal BaseSalary, bool IsUnionMember);
public sealed record SimpleStepLookupDto(int Id, int SalaryScaleId, string Label, decimal StandardSalary);
public sealed record SimpleParamLookupDto(string ParamCode, string ParamName, decimal ParamValue);

public sealed record GetSalaryStructuresOverviewQuery(int? EmployeeId = null) : IRequest<SalaryStructuresOverviewDto>;

public sealed class GetSalaryStructuresOverviewQueryHandler : IRequestHandler<GetSalaryStructuresOverviewQuery, SalaryStructuresOverviewDto>
{
    private readonly IApplicationDbContext _context;

    public GetSalaryStructuresOverviewQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<SalaryStructuresOverviewDto> Handle(GetSalaryStructuresOverviewQuery request, CancellationToken cancellationToken)
    {
        var components = await _context.SalaryComponents
            .AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.ComponentCode)
            .Select(c => new SalaryComponentDto(
                c.Id,
                c.ComponentCode,
                c.ComponentName,
                c.ComponentType,
                c.CalculationType,
                c.DefaultAmount,
                c.IsTaxable,
                c.TaxExemptLimit,
                c.TaxExemptLimitParameterCode,
                c.IsSocialInsuranceBase,
                c.ShowOnPayslip,
                c.FormulaExpression,
                c.SortOrder,
                c.IsActive,
                c.LegalReference,
                c.NeedsHrVerification,
                c.Description))
            .ToListAsync(cancellationToken);

        var templatesRaw = await _context.SalaryTemplates
            .AsNoTracking()
            .Include(t => t.Components).ThenInclude(tc => tc.SalaryComponent)
            .OrderBy(t => t.TemplateCode)
            .ToListAsync(cancellationToken);

        var templates = templatesRaw.Select(t => new SalaryTemplateDto(
            t.Id,
            t.TemplateCode,
            t.TemplateName,
            t.StaffGroup,
            t.DefaultPayFrequency,
            t.DefaultProbationSalaryRatePercent,
            t.RoundingRule,
            t.IsDefaultForCompany,
            t.IsActive,
            t.LegalReference,
            t.NeedsHrVerification,
            t.Description,
            t.Components.OrderBy(c => c.SortOrder).Select(c => new SalaryTemplateComponentDto(
                c.Id,
                c.SalaryComponentId,
                c.SalaryComponent.ComponentCode,
                c.SalaryComponent.ComponentName,
                c.SalaryComponent.ComponentType,
                c.SortOrder,
                c.OverrideFormulaExpression,
                c.DefaultFixedAmount,
                c.IsRequired)).ToList())).ToList();

        var employees = await _context.Employees
            .AsNoTracking()
            .Include(e => e.OrganizationUnit)
            .Where(e => e.WorkStatus != "Terminated" && e.WorkStatus != "Resigned")
            .OrderBy(e => e.EmployeeCode)
            .Select(e => new SimpleEmployeeLookupDto(e.Id, e.EmployeeCode, e.FullName, e.OrganizationUnit.UnitName, e.BaseSalary, e.IsUnionMember))
            .ToListAsync(cancellationToken);

        var selectedEmpId = request.EmployeeId ?? employees.FirstOrDefault()?.Id;

        var profileQuery = _context.EmployeeSalaryProfiles
            .AsNoTracking()
            .Include(p => p.Employee)
            .Include(p => p.SalaryStep).ThenInclude(s => s!.SalaryScale)
            .Include(p => p.SalaryTemplate)
            .Include(p => p.FixedAllowances).ThenInclude(a => a.SalaryComponent)
            .AsQueryable();

        if (selectedEmpId.HasValue)
            profileQuery = profileQuery.Where(p => p.EmployeeId == selectedEmpId.Value);

        var profilesRaw = await profileQuery
            .OrderByDescending(p => p.EffectiveFrom)
            .ThenByDescending(p => p.Id)
            .ToListAsync(cancellationToken);

        var profiles = profilesRaw.Select(p => new EmployeeSalaryProfileDto(
            p.Id,
            p.EmployeeId,
            p.Employee.EmployeeCode,
            p.Employee.FullName,
            p.EffectiveFrom,
            p.EffectiveTo,
            p.BaseSalary,
            p.InsuranceSalary,
            p.SalaryScaleId,
            p.SalaryStepId,
            p.SalaryStep != null ? $"{p.SalaryStep.SalaryScale.ScaleCode} - Bậc {p.SalaryStep.StepOrder} ({p.SalaryStep.StepName})" : null,
            p.SalaryTemplateId,
            p.SalaryTemplate?.TemplateName,
            p.PayMode,
            p.UnitRate,
            p.IsProbation,
            p.ProbationRatePercent,
            p.PositionAllowance,
            p.OtherAllowance,
            p.IsUnionMember,
            p.DecisionNumber,
            p.Reason,
            p.FixedAllowances.Select(a => new EmployeeSalaryAllowanceDto(
                a.SalaryComponentId,
                a.ComponentCode,
                a.SalaryComponent.ComponentName,
                a.Amount,
                a.Notes)).ToList())).ToList();

        var steps = await _context.SalarySteps
            .AsNoTracking()
            .Include(s => s.SalaryScale)
            .OrderBy(s => s.SalaryScale.ScaleCode)
            .ThenBy(s => s.StepOrder)
            .Select(s => new SimpleStepLookupDto(s.Id, s.SalaryScaleId, $"{s.SalaryScale.ScaleCode} - Bậc {s.StepOrder} ({s.StandardSalary:N0} đ)", s.StandardSalary))
            .ToListAsync(cancellationToken);

        var exemptParams = await _context.PayrollParameters
            .AsNoTracking()
            .OrderBy(p => p.ParamCode)
            .Select(p => new SimpleParamLookupDto(p.ParamCode, p.ParamName, p.ParamValue))
            .ToListAsync(cancellationToken);

        var latestStat = await _context.PayrollStatutoryParameters
            .AsNoTracking()
            .OrderByDescending(s => s.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

        return new SalaryStructuresOverviewDto(
            components,
            templates,
            employees,
            steps,
            exemptParams,
            selectedEmpId,
            profiles,
            latestStat?.MinProbationSalaryRatePercent ?? 85m);
    }
}

public sealed record ValidateAndTestSalaryFormulaQuery(
    string FormulaExpression,
    string? TargetComponentCode = null,
    IReadOnlyDictionary<string, decimal>? SampleVariables = null,
    bool IsTaxable = true,
    decimal TaxExemptLimit = 0m,
    string? TaxExemptLimitParameterCode = null) : IRequest<FormulaValidationResultDto>;

public sealed class ValidateAndTestSalaryFormulaQueryHandler
    : IRequestHandler<ValidateAndTestSalaryFormulaQuery, FormulaValidationResultDto>
{
    private readonly IApplicationDbContext _context;

    public ValidateAndTestSalaryFormulaQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<FormulaValidationResultDto> Handle(ValidateAndTestSalaryFormulaQuery request, CancellationToken cancellationToken)
    {
        var existing = await _context.SalaryComponents
            .AsNoTracking()
            .Select(c => new FormulaComponentSpec(
                c.Id,
                c.ComponentCode,
                c.ComponentName,
                c.ComponentType,
                c.CalculationType,
                c.DefaultAmount,
                c.FormulaExpression,
                c.SortOrder,
                c.IsTaxable,
                c.TaxExemptLimit,
                c.TaxExemptLimitParameterCode,
                c.IsSocialInsuranceBase,
                c.ShowOnPayslip))
            .ToListAsync(cancellationToken);

        decimal resolvedExempt = request.TaxExemptLimit;
        if (!string.IsNullOrWhiteSpace(request.TaxExemptLimitParameterCode))
        {
            var param = await _context.PayrollParameters
                .AsNoTracking()
                .Where(p => p.ParamCode == request.TaxExemptLimitParameterCode)
                .OrderByDescending(p => p.EffectiveFrom)
                .FirstOrDefaultAsync(cancellationToken);
            if (param != null)
                resolvedExempt = param.ParamValue;
        }

        return NCalcSalaryFormulaEngine.ValidateAndEvaluateSingle(
            request.FormulaExpression,
            request.TargetComponentCode,
            existing,
            request.SampleVariables,
            request.IsTaxable,
            resolvedExempt);
    }
}

public sealed record SaveSalaryComponentCommand(
    int? Id,
    string ComponentCode,
    string ComponentName,
    string ComponentType, // Earning | Allowance | Bonus | Deduction | Info
    string CalculationType, // Fixed | Formula | System
    decimal DefaultAmount,
    bool IsTaxable,
    decimal TaxExemptLimit,
    string? TaxExemptLimitParameterCode,
    bool IsSocialInsuranceBase,
    bool ShowOnPayslip,
    string? FormulaExpression,
    int SortOrder,
    bool IsActive,
    string? LegalReference = null,
    bool NeedsHrVerification = false,
    string? Description = null) : IRequest<int>;

public sealed class SaveSalaryComponentCommandHandler : IRequestHandler<SaveSalaryComponentCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public SaveSalaryComponentCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(SaveSalaryComponentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();
        var username = _currentUser.RequireUsername();

        var normalizedCode = request.ComponentCode.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalizedCode))
            throw new InvalidOperationException("Mã thành phần lương không được để trống.");
        if (string.IsNullOrWhiteSpace(request.ComponentName))
            throw new InvalidOperationException("Tên thành phần lương không được để trống.");

        if (string.Equals(request.CalculationType, "Formula", StringComparison.OrdinalIgnoreCase))
        {
            var existingSpecs = await _context.SalaryComponents
                .AsNoTracking()
                .Where(c => !request.Id.HasValue || c.Id != request.Id.Value)
                .Select(c => new FormulaComponentSpec(
                    c.Id, c.ComponentCode, c.ComponentName, c.ComponentType, c.CalculationType,
                    c.DefaultAmount, c.FormulaExpression, c.SortOrder, c.IsTaxable, c.TaxExemptLimit,
                    c.TaxExemptLimitParameterCode, c.IsSocialInsuranceBase, c.ShowOnPayslip))
                .ToListAsync(cancellationToken);

            var check = NCalcSalaryFormulaEngine.ValidateAndEvaluateSingle(
                request.FormulaExpression,
                normalizedCode,
                existingSpecs,
                null,
                request.IsTaxable,
                request.TaxExemptLimit);

            if (!check.IsValid)
                throw new InvalidOperationException(check.ErrorMessage ?? "Công thức không hợp lệ.");
        }

        SalaryComponent entity;
        if (request.Id.HasValue && request.Id.Value > 0)
        {
            entity = await _context.SalaryComponents.FirstOrDefaultAsync(c => c.Id == request.Id.Value, cancellationToken)
                ?? throw new KeyNotFoundException($"Không tìm thấy thành phần lương #{request.Id.Value}.");
        }
        else
        {
            var dup = await _context.SalaryComponents.AnyAsync(c => c.ComponentCode == normalizedCode, cancellationToken);
            if (dup)
                throw new InvalidOperationException($"Mã thành phần lương '{normalizedCode}' đã tồn tại.");
            entity = new SalaryComponent();
            await _context.SalaryComponents.AddAsync(entity, cancellationToken);
        }

        entity.ComponentCode = normalizedCode;
        entity.ComponentName = request.ComponentName.Trim();
        entity.ComponentType = request.ComponentType;
        entity.CalculationType = request.CalculationType;
        entity.DefaultAmount = Math.Round(request.DefaultAmount, 0, MidpointRounding.AwayFromZero);
        entity.IsTaxable = request.IsTaxable;
        entity.TaxExemptLimit = Math.Round(request.TaxExemptLimit, 0, MidpointRounding.AwayFromZero);
        entity.TaxExemptLimitParameterCode = string.IsNullOrWhiteSpace(request.TaxExemptLimitParameterCode) ? null : request.TaxExemptLimitParameterCode.Trim().ToUpperInvariant();
        entity.IsSocialInsuranceBase = request.IsSocialInsuranceBase;
        entity.ShowOnPayslip = request.ShowOnPayslip;
        entity.FormulaExpression = string.IsNullOrWhiteSpace(request.FormulaExpression) ? null : request.FormulaExpression.Trim();
        entity.SortOrder = request.SortOrder;
        entity.IsActive = request.IsActive;
        entity.LegalReference = request.LegalReference?.Trim();
        entity.NeedsHrVerification = request.NeedsHrVerification;
        entity.Description = request.Description?.Trim();

        await _context.AuditLogs.AddAsync(new AuditLog
        {
            UserId = userId,
            Username = username,
            ModuleName = "Payroll",
            ActionType = request.Id.HasValue ? "SalaryComponent_Update" : "SalaryComponent_Create",
            EntityName = nameof(SalaryComponent),
            EntityId = entity.ComponentCode,
            NewValuesJson = JsonSerializer.Serialize(new { entity.ComponentCode, entity.ComponentName, entity.ComponentType, entity.CalculationType, entity.FormulaExpression, entity.IsTaxable, entity.TaxExemptLimit, entity.IsSocialInsuranceBase }),
            IpAddress = _currentUser.IpAddress,
            UserAgent = _currentUser.UserAgent,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = username
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

public sealed record SaveSalaryTemplateItemInput(
    int SalaryComponentId,
    int SortOrder,
    string? OverrideFormulaExpression = null,
    decimal? DefaultFixedAmount = null,
    bool IsRequired = true);

public sealed record SaveSalaryTemplateCommand(
    int? Id,
    string TemplateCode,
    string TemplateName,
    string StaffGroup, // Office | Factory | Sales | Management | General
    string DefaultPayFrequency, // Monthly | Daily | Hourly | PieceRate
    decimal DefaultProbationSalaryRatePercent,
    string RoundingRule,
    bool IsDefaultForCompany,
    bool IsActive,
    string? LegalReference,
    bool NeedsHrVerification,
    string? Description,
    IReadOnlyList<SaveSalaryTemplateItemInput>? Components = null) : IRequest<int>;

public sealed class SaveSalaryTemplateCommandHandler : IRequestHandler<SaveSalaryTemplateCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public SaveSalaryTemplateCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(SaveSalaryTemplateCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();
        var username = _currentUser.RequireUsername();

        var latestStat = await _context.PayrollStatutoryParameters
            .AsNoTracking()
            .OrderByDescending(s => s.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

        decimal minProbRate = latestStat?.MinProbationSalaryRatePercent ?? 85m;
        if (request.DefaultProbationSalaryRatePercent < minProbRate)
        {
            throw new InvalidOperationException(
                $"Tỷ lệ lương thử việc mặc định ({request.DefaultProbationSalaryRatePercent:0.#}%) không được thấp hơn mức tối thiểu {minProbRate:0.#}% theo {latestStat?.LegalReference ?? "quy định"}.");
        }

        var normalizedCode = request.TemplateCode.Trim().ToUpperInvariant();
        SalaryTemplate template;
        if (request.Id.HasValue && request.Id.Value > 0)
        {
            template = await _context.SalaryTemplates
                .Include(t => t.Components)
                .FirstOrDefaultAsync(t => t.Id == request.Id.Value, cancellationToken)
                ?? throw new KeyNotFoundException($"Không tìm thấy mẫu lương #{request.Id.Value}.");
        }
        else
        {
            if (await _context.SalaryTemplates.AnyAsync(t => t.TemplateCode == normalizedCode, cancellationToken))
                throw new InvalidOperationException($"Mã mẫu lương '{normalizedCode}' đã tồn tại.");
            template = new SalaryTemplate();
            await _context.SalaryTemplates.AddAsync(template, cancellationToken);
        }

        template.TemplateCode = normalizedCode;
        template.TemplateName = request.TemplateName.Trim();
        template.StaffGroup = request.StaffGroup;
        template.DefaultPayFrequency = request.DefaultPayFrequency;
        template.DefaultProbationSalaryRatePercent = request.DefaultProbationSalaryRatePercent;
        template.RoundingRule = string.IsNullOrWhiteSpace(request.RoundingRule) ? "RoundToDong" : request.RoundingRule;
        template.IsDefaultForCompany = request.IsDefaultForCompany;
        template.IsActive = request.IsActive;
        template.LegalReference = request.LegalReference?.Trim();
        template.NeedsHrVerification = request.NeedsHrVerification;
        template.Description = request.Description?.Trim();

        if (request.Components != null)
        {
            if (template.Components.Count > 0)
                _context.SalaryTemplateComponents.RemoveRange(template.Components);

            foreach (var item in request.Components)
            {
                template.Components.Add(new SalaryTemplateComponent
                {
                    SalaryComponentId = item.SalaryComponentId,
                    SortOrder = item.SortOrder,
                    OverrideFormulaExpression = string.IsNullOrWhiteSpace(item.OverrideFormulaExpression) ? null : item.OverrideFormulaExpression.Trim(),
                    DefaultFixedAmount = item.DefaultFixedAmount.HasValue ? Math.Round(item.DefaultFixedAmount.Value, 0, MidpointRounding.AwayFromZero) : null,
                    IsRequired = item.IsRequired
                });
            }
        }

        await _context.AuditLogs.AddAsync(new AuditLog
        {
            UserId = userId,
            Username = username,
            ModuleName = "Payroll",
            ActionType = request.Id.HasValue ? "SalaryTemplate_Update" : "SalaryTemplate_Create",
            EntityName = nameof(SalaryTemplate),
            EntityId = template.TemplateCode,
            NewValuesJson = JsonSerializer.Serialize(new { template.TemplateCode, template.TemplateName, template.StaffGroup, template.DefaultPayFrequency, template.DefaultProbationSalaryRatePercent }),
            IpAddress = _currentUser.IpAddress,
            UserAgent = _currentUser.UserAgent,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = username
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return template.Id;
    }
}

public sealed record EmployeeSalaryAllowanceInput(int SalaryComponentId, decimal Amount, string? Notes = null);

public sealed record SaveEmployeeSalaryProfileCommand(
    int EmployeeId,
    DateOnly EffectiveFrom,
    decimal BaseSalary,
    decimal InsuranceSalary,
    int? SalaryStepId,
    int? SalaryTemplateId,
    string PayMode, // Monthly | Daily | Hourly | PieceRate
    decimal UnitRate,
    bool IsProbation,
    decimal ProbationRatePercent,
    decimal PositionAllowance,
    decimal OtherAllowance,
    bool IsUnionMember,
    string? DecisionNumber = null,
    string? Reason = null,
    IReadOnlyList<EmployeeSalaryAllowanceInput>? FixedAllowances = null) : IRequest<int>;

public sealed class SaveEmployeeSalaryProfileCommandHandler : IRequestHandler<SaveEmployeeSalaryProfileCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public SaveEmployeeSalaryProfileCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(SaveEmployeeSalaryProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();
        var username = _currentUser.RequireUsername();

        var emp = await _context.Employees.FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy nhân viên #{request.EmployeeId}.");

        // Block creating a backdated profile whose EffectiveFrom falls inside a locked payroll period
        var lockedOverlap = await _context.PayrollPeriods
            .AsNoTracking()
            .AnyAsync(p => (p.IsLocked || p.Status == PayrollPeriodStatuses.Locked || p.Status == PayrollPeriodStatuses.Paid)
                && p.EndDate >= request.EffectiveFrom
                && p.StartDate <= request.EffectiveFrom, cancellationToken);
        if (lockedOverlap)
            throw new InvalidOperationException($"Ngày hiệu lực {request.EffectiveFrom:dd/MM/yyyy} thuộc kỳ lương đã khóa/chi trả, không thể chèn lịch sử lương vào kỳ đã khóa.");

        if (request.BaseSalary < 0m || request.InsuranceSalary < 0m)
            throw new InvalidOperationException("Lương cơ bản và lương đóng BHXH không được âm.");

        // Read probation floor from statutory config (not hardcoded!)
        var statutory = await _context.PayrollStatutoryParameters
            .AsNoTracking()
            .Where(p => p.EffectiveFrom <= request.EffectiveFrom && (p.EffectiveTo == null || p.EffectiveTo >= request.EffectiveFrom))
            .OrderByDescending(p => p.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken)
            ?? await _context.PayrollStatutoryParameters.AsNoTracking().OrderByDescending(p => p.EffectiveFrom).FirstOrDefaultAsync(cancellationToken);

        decimal minProbation = statutory?.MinProbationSalaryRatePercent ?? 85m;
        if (request.IsProbation && request.ProbationRatePercent < minProbation)
        {
            throw new InvalidOperationException(
                $"Tỷ lệ lương thử việc ({request.ProbationRatePercent:0.#}%) không được thấp hơn mức tối thiểu {minProbation:0.#}% theo {statutory?.LegalReference ?? "Điều 26 BLLĐ 2019"}.");
        }

        int? salaryScaleId = null;
        if (request.SalaryStepId.HasValue)
        {
            var step = await _context.SalarySteps.AsNoTracking().FirstOrDefaultAsync(s => s.Id == request.SalaryStepId.Value, cancellationToken)
                ?? throw new KeyNotFoundException($"Không tìm thấy bậc lương #{request.SalaryStepId.Value}.");
            salaryScaleId = step.SalaryScaleId;
        }

        // Maintain non-overlapping timeline history by closing any prior profile whose EffectiveFrom < new EffectiveFrom
        var existingProfiles = await _context.EmployeeSalaryProfiles
            .Where(p => p.EmployeeId == emp.Id && p.IsActive)
            .OrderBy(p => p.EffectiveFrom)
            .ToListAsync(cancellationToken);

        foreach (var prev in existingProfiles.Where(p => p.EffectiveFrom < request.EffectiveFrom))
        {
            var closeDate = request.EffectiveFrom.AddDays(-1);
            if (!prev.EffectiveTo.HasValue || prev.EffectiveTo.Value >= request.EffectiveFrom)
                prev.EffectiveTo = closeDate;
        }

        var nextAfter = existingProfiles
            .Where(p => p.EffectiveFrom > request.EffectiveFrom)
            .OrderBy(p => p.EffectiveFrom)
            .FirstOrDefault();

        var sameDayExisting = existingProfiles.FirstOrDefault(p => p.EffectiveFrom == request.EffectiveFrom);
        if (sameDayExisting != null)
        {
            sameDayExisting.IsActive = false;
            sameDayExisting.EffectiveTo = request.EffectiveFrom;
        }

        var profile = new EmployeeSalaryProfile
        {
            EmployeeId = emp.Id,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = nextAfter != null ? nextAfter.EffectiveFrom.AddDays(-1) : null,
            BaseSalary = Math.Round(request.BaseSalary, 0, MidpointRounding.AwayFromZero),
            InsuranceSalary = Math.Round(request.InsuranceSalary, 0, MidpointRounding.AwayFromZero),
            SalaryScaleId = salaryScaleId,
            SalaryStepId = request.SalaryStepId,
            SalaryTemplateId = request.SalaryTemplateId,
            PayMode = string.IsNullOrWhiteSpace(request.PayMode) ? "Monthly" : request.PayMode,
            UnitRate = Math.Round(request.UnitRate, 4, MidpointRounding.AwayFromZero),
            IsProbation = request.IsProbation,
            ProbationRatePercent = request.IsProbation ? request.ProbationRatePercent : 100.00m,
            PositionAllowance = Math.Round(request.PositionAllowance, 0, MidpointRounding.AwayFromZero),
            OtherAllowance = Math.Round(request.OtherAllowance, 0, MidpointRounding.AwayFromZero),
            IsUnionMember = request.IsUnionMember,
            DecisionNumber = request.DecisionNumber?.Trim(),
            Reason = request.Reason?.Trim(),
            IsActive = true
        };

        if (request.FixedAllowances != null && request.FixedAllowances.Count > 0)
        {
            var compIds = request.FixedAllowances.Select(a => a.SalaryComponentId).Distinct().ToList();
            var compMap = await _context.SalaryComponents
                .AsNoTracking()
                .Where(c => compIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.ComponentCode, cancellationToken);

            foreach (var fa in request.FixedAllowances)
            {
                if (compMap.TryGetValue(fa.SalaryComponentId, out var code))
                {
                    profile.FixedAllowances.Add(new EmployeeSalaryProfileAllowance
                    {
                        SalaryComponentId = fa.SalaryComponentId,
                        ComponentCode = code,
                        Amount = Math.Round(fa.Amount, 0, MidpointRounding.AwayFromZero),
                        Notes = fa.Notes?.Trim()
                    });
                }
            }
        }

        await _context.EmployeeSalaryProfiles.AddAsync(profile, cancellationToken);

        // Sync snapshot onto Employee when EffectiveFrom <= today and no later profile overrides it
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        if (request.EffectiveFrom <= today && nextAfter == null)
        {
            emp.BaseSalary = profile.BaseSalary;
            emp.PositionAllowance = profile.PositionAllowance;
            emp.OtherAllowance = profile.OtherAllowance;
            emp.SalaryStepId = profile.SalaryStepId;
            emp.IsUnionMember = profile.IsUnionMember;
        }

        await _context.AuditLogs.AddAsync(new AuditLog
        {
            UserId = userId,
            Username = username,
            ModuleName = "Payroll",
            ActionType = "EmployeeSalaryProfile_Create",
            EntityName = nameof(EmployeeSalaryProfile),
            EntityId = $"{emp.EmployeeCode}_{request.EffectiveFrom:yyyyMMdd}",
            NewValuesJson = JsonSerializer.Serialize(new
            {
                emp.EmployeeCode,
                profile.EffectiveFrom,
                profile.BaseSalary,
                profile.InsuranceSalary,
                profile.PayMode,
                profile.IsProbation,
                profile.ProbationRatePercent,
                profile.IsUnionMember
            }),
            IpAddress = _currentUser.IpAddress,
            UserAgent = _currentUser.UserAgent,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = username
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return profile.Id;
    }
}
