using System;
using System.Collections.Generic;
using System.Linq;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Kpis;
using VinamilkHRM.Domain.Entities.Salaries;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Services.TotalRewards;

/// <summary>
/// Động cơ tính toán Total Rewards thuần Domain (0 phụ thuộc DB).
/// Thực thi:
/// 1. Tính KPI năm từ 12 kỳ đã khóa.
/// 2. Gợi ý % tăng lương theo ma trận KPI x Compa-Ratio.
/// 3. Co giãn ngân sách đơn vị (Proration Factor).
/// 4. Kiểm soát ngân sách và biên độ điều chỉnh +/- 3%.
/// 5. Đánh giá điều kiện đặc cách & xác định bước phê duyệt Hội đồng quản trị.
/// 6. Tự động sinh khoản truy lĩnh PayrollAdjustment cho các kỳ lương đã khóa.
/// </summary>
public static class TotalRewardsEngine
{
    /// <summary>
    /// Tính điểm và xếp loại KPI năm = trung bình cộng điểm 12 kỳ KPI đã khóa (bỏ qua kỳ không xếp loại).
    /// </summary>
    public static (decimal AnnualScore, KpiGrade Grade) CalculateAnnualKpi(IReadOnlyList<KpiResult> monthlyResults)
    {
        if (monthlyResults == null || monthlyResults.Count == 0)
            return (0m, KpiGrade.E);

        // Bỏ kỳ không xếp loại (IsUnrated)
        var validResults = monthlyResults.Where(r => !r.IsUnrated).ToList();
        if (validResults.Count == 0)
            return (0m, KpiGrade.E);

        decimal avgScore = Math.Round(validResults.Average(r => r.TotalScore), 2, MidpointRounding.AwayFromZero);

        KpiGrade grade = avgScore switch
        {
            >= 110m => KpiGrade.A,
            >= 95m => KpiGrade.B,
            >= 80m => KpiGrade.C,
            >= 60m => KpiGrade.D,
            _ => KpiGrade.E
        };

        return (avgScore, grade);
    }

    /// <summary>
    /// Tra ma trận Merit Matrix: % gợi ý theo xếp loại KPI năm x Compa-Ratio.
    /// Nhóm Compa-ratio:
    /// - CR < 0.80: A 10%, B 8%, C 5%, D 0%, E 0%
    /// - 0.80 <= CR < 1.00: A 8%, B 6%, C 4%, D 0%, E 0% (Tại CR 0.96, B -> 6%)
    /// - 1.00 <= CR <= 1.15: A 6%, B 4%, C 2%, D 0%, E 0%
    /// - CR > 1.15: A 4%, B 2%, C 0%, D 0%, E 0%
    /// </summary>
    public static decimal GetMeritIncreasePercentage(KpiGrade grade, decimal compaRatio)
    {
        if (compaRatio < 0.80m)
        {
            return grade switch
            {
                KpiGrade.A => 0.10m,
                KpiGrade.B => 0.08m,
                KpiGrade.C => 0.05m,
                _ => 0m
            };
        }
        else if (compaRatio < 1.00m)
        {
            return grade switch
            {
                KpiGrade.A => 0.08m,
                KpiGrade.B => 0.06m, // 6% tại P1 12M, Mid 12.5M (CR = 0.96)
                KpiGrade.C => 0.04m,
                _ => 0m
            };
        }
        else if (compaRatio <= 1.15m)
        {
            return grade switch
            {
                KpiGrade.A => 0.06m,
                KpiGrade.B => 0.04m,
                KpiGrade.C => 0.02m,
                _ => 0m
            };
        }
        else // CR > 1.15
        {
            return grade switch
            {
                KpiGrade.A => 0.04m,
                KpiGrade.B => 0.02m,
                _ => 0m
            };
        }
    }

    /// <summary>
    /// Tính toán hệ số co giãn ngân sách (Proration Factor).
    /// Nếu tổng gợi ý > ngân sách: Factor = Ngân sách / Tổng gợi ý.
    /// </summary>
    public static decimal CalculateProrationFactor(decimal unitBudget, decimal totalSuggestedIncrease)
    {
        if (unitBudget < 0)
            throw new DomainException("Ngân sách đơn vị không thể âm.", "ERR_SAL_BUDGET_NEGATIVE");

        if (totalSuggestedIncrease <= 0)
            return 1.0m;

        if (totalSuggestedIncrease <= unitBudget)
            return 1.0m;

        return Math.Round(unitBudget / totalSuggestedIncrease, 4, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Kiểm tra nộp xét tăng lương của Trưởng đơn vị:
    /// Không cho phép nộp nếu tổng mức đề xuất vượt quá hạn mức ngân sách được giao.
    /// </summary>
    public static void ValidateUnitSubmission(decimal unitBudget, decimal totalProposedIncrease)
    {
        if (totalProposedIncrease > unitBudget)
        {
            throw new DomainException(
                $"Tổng ngân sách đề xuất tăng lương ({totalProposedIncrease:N0} đ) vượt quá hạn mức ngân sách được giao ({unitBudget:N0} đ).",
                "ERR_SAL_BUDGET_EXCEEDED");
        }
    }

    /// <summary>
    /// Đánh giá đề xuất tăng lương Đặc cách:
    /// Trần đặc cách: <= 30% mức lương hiện tại và <= Max của khung lương.
    /// Nếu vượt trần (> 30% hoặc > Max của khung) -> bắt buộc thêm bước phê duyệt Hội đồng quản trị.
    /// </summary>
    public static (bool ExceedsThreshold, decimal Percentage, string Reason) EvaluateSpecialAdjustment(
        decimal currentP1,
        decimal proposedP1,
        SalaryBand? band = null)
    {
        if (currentP1 <= 0)
            throw new DomainException("Mức lương hiện tại phải lớn hơn 0.", "ERR_SAL_CURRENT_INVALID");

        if (proposedP1 <= currentP1)
            throw new DomainException("Mức lương đặc cách đề xuất phải cao hơn mức lương hiện tại.", "ERR_SAL_PROPOSED_NOT_GREATER");

        decimal pct = Math.Round((proposedP1 - currentP1) / currentP1, 4, MidpointRounding.AwayFromZero);
        bool exceedsPercentage = pct > 0.30m; // Vượt trần 30%
        bool exceedsBandMax = band != null && proposedP1 > band.Max; // Vượt Max của khung

        bool requiresBoard = exceedsPercentage || exceedsBandMax;
        string reason = string.Empty;

        if (exceedsPercentage && exceedsBandMax)
            reason = $"Mức tăng (+{pct * 100:0.##}%) vượt trần 30% và mức lương mới ({proposedP1:N0} đ) vượt mức tối đa của khung lương ({band!.Max:N0} đ).";
        else if (exceedsPercentage)
            reason = $"Mức tăng (+{pct * 100:0.##}%) vượt trần đặc cách 30%.";
        else if (exceedsBandMax)
            reason = $"Mức lương mới ({proposedP1:N0} đ) vượt mức tối đa của khung lương ({band!.Max:N0} đ).";

        return (requiresBoard, pct, reason);
    }

    /// <summary>
    /// Tự động sinh danh sách PayrollAdjustment truy lĩnh (ARREARS) cho các kỳ lương đã khóa
    /// nằm trong khoảng từ EffectiveDate đến ApprovalDate.
    /// </summary>
    public static List<PayrollAdjustment> GenerateRetroactiveAdjustments(
        int employeeId,
        decimal currentP1,
        decimal proposedP1,
        DateOnly effectiveDate,
        DateOnly approvalDate,
        IReadOnlyList<PayrollRun> lockedRuns,
        int approverId)
    {
        var adjustments = new List<PayrollAdjustment>();
        decimal diff = proposedP1 - currentP1;

        if (diff <= 0 || approvalDate <= effectiveDate || lockedRuns == null || lockedRuns.Count == 0)
            return adjustments;

        // Kỳ mục tiêu (kỳ lương hiện tại áp dụng truy lĩnh)
        string targetPeriod = $"{approvalDate.Year:D4}-{approvalDate.Month:D2}";

        // Quét các kỳ lương đã khóa nằm giữa EffectiveDate và ApprovalDate
        foreach (var run in lockedRuns)
        {
            // run.Period định dạng YYYY-MM
            if (string.IsNullOrWhiteSpace(run.Period) || run.Period.Length < 7)
                continue;

            int runYear = int.Parse(run.Period.Substring(0, 4));
            int runMonth = int.Parse(run.Period.Substring(5, 2));
            var runDate = new DateOnly(runYear, runMonth, 1);

            // Kiểm tra kỳ nằm từ effectiveDate đến trước approvalDate
            var effectiveMonthDate = new DateOnly(effectiveDate.Year, effectiveDate.Month, 1);
            var approvalMonthDate = new DateOnly(approvalDate.Year, approvalDate.Month, 1);

            if (runDate >= effectiveMonthDate && runDate < approvalMonthDate)
            {
                string reason = $"Truy lĩnh tăng lương kỳ {run.Period} theo Quyết định duyệt ngày {approvalDate:dd/MM/yyyy} (hiệu lực từ {effectiveDate:dd/MM/yyyy})";
                var adj = new PayrollAdjustment(
                    employeeId: employeeId,
                    sourcePeriod: run.Period,
                    targetPeriod: targetPeriod,
                    adjustmentType: "ARREARS",
                    amount: diff,
                    reason: reason,
                    approvedBy: approverId);

                adjustments.Add(adj);
            }
        }

        return adjustments;
    }
}