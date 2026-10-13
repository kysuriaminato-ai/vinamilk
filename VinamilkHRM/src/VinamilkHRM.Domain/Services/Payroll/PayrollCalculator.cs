using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Statutory;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Services.Payroll;

/// <summary>
/// Động cơ tính lương thuần (Pure Domain Payroll Engine) độc lập với DB và EF Core.
/// Triển khai đầy đủ mô hình lương 3P (Position, Person, Performance), bảo hiểm xã hội,
/// thuế thu nhập cá nhân (5 bậc mới 2026 / 7 bậc cũ 2025), phụ cấp, làm thêm giờ và các chế độ pháp lý.
/// </summary>
public static class PayrollCalculator
{
    private static readonly CultureInfo ViCulture = CultureInfo.GetCultureInfo("vi-VN");

    public static PayrollResult Calculate(PayrollInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        // 1. Kiểm tra bảng công đã khóa
        if (input.TimesheetSummary == null || !input.IsTimesheetLocked)
        {
            throw new DomainException(
                "Không có bảng công đã khóa trong kỳ tính lương.",
                "ERR_PAY_NO_LOCKED_TIMESHEET");
        }

        var ts = input.TimesheetSummary;
        decimal standardWorkDays = ts.StandardWorkDays > 0 ? ts.StandardWorkDays : 26m;

        // Ngày công ty trả = Công thực tế + Phép năm (P) + Lễ tết (L) + Nghỉ bù (NB) + Công tác (CT) + Đào tạo (ĐT)
        decimal companyPaidDays = ts.ActualWorkDays + ts.PaidLeaveDays + ts.HolidayDays + ts.CompensatoryLeaveDays + ts.BusinessTripDays + ts.TrainingDays;

        var result = new PayrollResult
        {
            EmployeeId = input.EmployeeId,
            EmployeeCode = input.EmployeeCode,
            FullName = input.FullName,
            PayrollPeriod = input.PayrollPeriod,
            StandardWorkDays = standardWorkDays,
            ActualWorkDays = ts.ActualWorkDays,
            CompanyPaidDays = companyPaidDays
        };

        var lines = new List<PayrollLine>();

        // 2. LƯƠNG THỜI GIAN (P1 + PHỤ CẤP CỐ ĐỊNH)
        decimal earnedTimeSalary;
        string timeSalaryTrace;

        if (input.SalarySegments != null && input.SalarySegments.Count > 0)
        {
            // Trường hợp có thay đổi lương giữa tháng: tính theo số ngày từng mức
            earnedTimeSalary = 0m;
            var traceParts = new List<string>();
            foreach (var segment in input.SalarySegments)
            {
                decimal segRate = segment.P1BaseSalary + segment.FixedAllowances;
                decimal segDays = Math.Round(companyPaidDays * segment.EffectiveCalendarDays / segment.TotalCalendarDaysInPeriod, 2, MidpointRounding.AwayFromZero);
                decimal segEarned = Math.Round(segRate * (segDays / standardWorkDays), 0, MidpointRounding.AwayFromZero);
                earnedTimeSalary += segEarned;
                traceParts.Add($"({FormatMoney(segRate)} × {segDays}/{standardWorkDays})");
            }
            timeSalaryTrace = string.Join(" + ", traceParts);
        }
        else
        {
            decimal basePlusFixed = input.P1BaseSalary + input.FixedAllowances;
            earnedTimeSalary = Math.Round(basePlusFixed * (companyPaidDays / standardWorkDays), 0, MidpointRounding.AwayFromZero);
            timeSalaryTrace = $"{FormatMoney(basePlusFixed)} × {companyPaidDays}/{standardWorkDays}";
        }

        lines.Add(new PayrollLine(
            "LUONG_THOI_GIAN",
            "Lương thời gian (P1 + Phụ cấp cố định)",
            earnedTimeSalary,
            IsTaxable: true,
            IsInsurable: true,
            timeSalaryTrace));

        // 3. TIỀN LƯƠNG GIỜ VÀ LÀM THÊM GIỜ (OVERTIME - 6 NHÓM)
        decimal hourlyWage = Math.Round((input.P1BaseSalary + input.FixedAllowances) / (standardWorkDays * 8m), 4);
        result.HourlyWage = hourlyWage;

        decimal totalOtPay = 0m;
        decimal exemptOtPay = 0m;

        // Nhóm 1: Ngày thường ban ngày (150%)
        if (ts.RegularOtHours > 0)
        {
            decimal otPay = Math.Round(ts.RegularOtHours * hourlyWage * input.OvertimeWeekdayMultiplier, 0, MidpointRounding.AwayFromZero);
            decimal exempt = Math.Round(ts.RegularOtHours * hourlyWage * (input.OvertimeWeekdayMultiplier - 1.0m), 0, MidpointRounding.AwayFromZero);
            totalOtPay += otPay;
            exemptOtPay += exempt;
            lines.Add(new PayrollLine(
                "OT_NGAY_THUONG",
                "Làm thêm giờ ngày thường (150%)",
                otPay,
                IsTaxable: true,
                IsInsurable: false,
                $"{FormatMoney(hourlyWage)} × {ts.RegularOtHours}h × {input.OvertimeWeekdayMultiplier:P0}"));
        }

        // Nhóm 2: Ngày thường ban đêm (200% hoặc 210%)
        if (ts.NightOtHours > 0)
        {
            decimal otPay = Math.Round(ts.NightOtHours * hourlyWage * input.OvertimeWeekdayNightMultiplier, 0, MidpointRounding.AwayFromZero);
            decimal exempt = Math.Round(ts.NightOtHours * hourlyWage * (input.OvertimeWeekdayNightMultiplier - 1.0m), 0, MidpointRounding.AwayFromZero);
            totalOtPay += otPay;
            exemptOtPay += exempt;
            lines.Add(new PayrollLine(
                "OT_NGAY_THUONG_DEM",
                "Làm thêm giờ ngày thường ban đêm",
                otPay,
                IsTaxable: true,
                IsInsurable: false,
                $"{FormatMoney(hourlyWage)} × {ts.NightOtHours}h × {input.OvertimeWeekdayNightMultiplier:P0}"));
        }

        // Nhóm 3: Ngày nghỉ tuần (200%)
        if (ts.RestDayOtHours > 0)
        {
            decimal otPay = Math.Round(ts.RestDayOtHours * hourlyWage * input.OvertimeWeekendMultiplier, 0, MidpointRounding.AwayFromZero);
            decimal exempt = Math.Round(ts.RestDayOtHours * hourlyWage * (input.OvertimeWeekendMultiplier - 1.0m), 0, MidpointRounding.AwayFromZero);
            totalOtPay += otPay;
            exemptOtPay += exempt;
            lines.Add(new PayrollLine(
                "OT_NGHI_TUAN",
                "Làm thêm giờ ngày nghỉ tuần (200%)",
                otPay,
                IsTaxable: true,
                IsInsurable: false,
                $"{FormatMoney(hourlyWage)} × {ts.RestDayOtHours}h × {input.OvertimeWeekendMultiplier:P0}"));
        }

        // Nhóm 5: Ngày Lễ, Tết (300%)
        if (ts.HolidayOtHours > 0)
        {
            decimal otPay = Math.Round(ts.HolidayOtHours * hourlyWage * input.OvertimeHolidayMultiplier, 0, MidpointRounding.AwayFromZero);
            decimal exempt = Math.Round(ts.HolidayOtHours * hourlyWage * (input.OvertimeHolidayMultiplier - 1.0m), 0, MidpointRounding.AwayFromZero);
            totalOtPay += otPay;
            exemptOtPay += exempt;
            lines.Add(new PayrollLine(
                "OT_LE_TET",
                "Làm thêm giờ ngày Lễ, Tết (300%)",
                otPay,
                IsTaxable: true,
                IsInsurable: false,
                $"{FormatMoney(hourlyWage)} × {ts.HolidayOtHours}h × {input.OvertimeHolidayMultiplier:P0}"));
        }

        // 4. PHỤ CẤP CA ĐÊM (30% LƯƠNG GIỜ - KHÔNG PHẢI TĂNG CA)
        decimal nightShiftPay = 0m;
        if (ts.NightHours > 0)
        {
            nightShiftPay = Math.Round(ts.NightHours * hourlyWage * input.NightShiftAllowanceMultiplier, 0, MidpointRounding.AwayFromZero);
            lines.Add(new PayrollLine(
                "PHU_CAP_CA_DEM",
                "Phụ cấp làm việc ban đêm (30%)",
                nightShiftPay,
                IsTaxable: true,
                IsInsurable: false,
                $"{FormatMoney(hourlyWage)} × {ts.NightHours}h × {input.NightShiftAllowanceMultiplier:P0}"));
        }

        // 5. PHỤ CẤP NĂNG LỰC (P2)
        decimal earnedP2 = 0m;
        if (input.CompetencyAllowanceP2 > 0)
        {
            earnedP2 = Math.Round(input.CompetencyAllowanceP2 * (ts.ActualWorkDays / standardWorkDays), 0, MidpointRounding.AwayFromZero);
            lines.Add(new PayrollLine(
                "P2_NANG_LUC",
                "Phụ cấp năng lực (P2)",
                earnedP2,
                IsTaxable: true,
                IsInsurable: false,
                $"{FormatMoney(input.CompetencyAllowanceP2)} × {ts.ActualWorkDays}/{standardWorkDays}"));
        }

        // 6. THƯỞNG HIỆU SUẤT (P3)
        // P3 = P1 × tỷ lệ thưởng mục tiêu theo LeadershipLevel × hệ số KPI × min(1, tỷ lệ công)
        decimal targetBonusRate = input.GetTargetBonusRate();
        decimal kpiCoeff = 0m;
        if (input.KpiResult != null)
        {
            kpiCoeff = input.KpiResult.IsUnrated ? 0m : input.KpiResult.BonusCoefficient;
        }

        decimal workRatio = Math.Min(1m, companyPaidDays / standardWorkDays);
        decimal p3Bonus = Math.Round(input.P1BaseSalary * targetBonusRate * kpiCoeff * workRatio, 0, MidpointRounding.AwayFromZero);

        lines.Add(new PayrollLine(
            "P3_HIEU_SUAT",
            "Thưởng hiệu suất (P3)",
            p3Bonus,
            IsTaxable: true,
            IsInsurable: false,
            $"{FormatMoney(input.P1BaseSalary)} × {targetBonusRate:P0} × {kpiCoeff:F1} × {workRatio:F4}"));

        // 7. PHỤ CẤP CHUYÊN CẦN (Mất khi KP > 0 hoặc đi muộn vượt ngưỡng)
        decimal attendanceAllowanceEarned = 0m;
        if (input.AttendanceAllowance > 0)
        {
            if (ts.UnexcusedAbsenceDays > 0 || ts.LateTimes > input.MaxLateTimesForAttendance)
            {
                lines.Add(new PayrollLine(
                    "PHU_CAP_CHUYEN_CAN",
                    "Phụ cấp chuyên cần",
                    0m,
                    IsTaxable: true,
                    IsInsurable: false,
                    $"Không đạt (KP={ts.UnexcusedAbsenceDays}, Đi muộn={ts.LateTimes}/{input.MaxLateTimesForAttendance})"));
            }
            else
            {
                attendanceAllowanceEarned = input.AttendanceAllowance;
                lines.Add(new PayrollLine(
                    "PHU_CAP_CHUYEN_CAN",
                    "Phụ cấp chuyên cần",
                    attendanceAllowanceEarned,
                    IsTaxable: true,
                    IsInsurable: false,
                    $"{FormatMoney(attendanceAllowanceEarned)} (Đạt chuẩn chuyên cần)"));
            }
        }

        // 8. THANH TOÁN PHÉP NĂM CHƯA NGHỈ (KHI THÔI VIỆC)
        decimal untakenLeavePay = 0m;
        if (input.IsResignedInPeriod && input.UntakenAnnualLeaveDays > 0)
        {
            untakenLeavePay = Math.Round(input.UntakenAnnualLeaveDays * (input.P1BaseSalary / standardWorkDays), 0, MidpointRounding.AwayFromZero);
            lines.Add(new PayrollLine(
                "THANH_TOAN_PHEP_NAM",
                "Thanh toán tiền phép năm chưa nghỉ",
                untakenLeavePay,
                IsTaxable: true,
                IsInsurable: false,
                $"{input.UntakenAnnualLeaveDays} ngày × ({FormatMoney(input.P1BaseSalary)}/{standardWorkDays})"));
        }

        // TỔNG THU NHẬP GROSS
        decimal grossSalary = earnedTimeSalary + totalOtPay + nightShiftPay + earnedP2 + p3Bonus + attendanceAllowanceEarned + untakenLeavePay;
        result.GrossSalary = grossSalary;

        // 9. TRÍCH NỘP BẢO HIỂM BẮT BUỘC & KINH PHÍ CÔNG ĐOÀN
        // Tháng có >= 14 ngày làm việc không hưởng lương -> không trích BHXH
        decimal nonPaidDays = ts.UnpaidLeaveDays + ts.UnexcusedAbsenceDays;
        bool isExemptFromInsurance = nonPaidDays >= 14m;
        result.IsExemptFromInsurance = isExemptFromInsurance;

        decimal insurableSalaryBase = input.P1BaseSalary + input.InsurableAllowances;
        // Kiểm tra sàn lương tối thiểu vùng
        if (insurableSalaryBase < input.RegionalMinimumWage)
        {
            result.Warnings.Add($"Lương đóng bảo hiểm ({FormatMoney(insurableSalaryBase)}) thấp hơn Lương tối thiểu vùng ({FormatMoney(input.RegionalMinimumWage)}).");
        }

        decimal maxBhxhBhyt = input.MaxInsuranceBaseMultiplier * input.BaseSalary;
        decimal maxBhtn = input.MaxInsuranceBaseMultiplier * input.RegionalMinimumWage;

        decimal cappedBhxhBase = Math.Min(insurableSalaryBase, maxBhxhBhyt);
        decimal cappedBhtnBase = Math.Min(insurableSalaryBase, maxBhtn);
        result.InsurableSalary = cappedBhxhBase;

        decimal empBhxh = 0m;
        decimal empBhyt = 0m;
        decimal empBhtn = 0m;
        decimal empUnion = 0m;

        decimal erBhxh = 0m;
        decimal erBhyt = 0m;
        decimal erBhtn = 0m;
        decimal erUnion = 0m;

        if (isExemptFromInsurance)
        {
            lines.Add(new PayrollLine(
                "MIEN_BHXH",
                "Miễn trích nộp BHXH/BHYT/BHTN",
                0m,
                IsTaxable: false,
                IsInsurable: false,
                $"Có {nonPaidDays} ngày không hưởng lương (≥ 14 ngày)"));
        }
        else
        {
            empBhxh = Math.Round(cappedBhxhBase * input.EmployeeSocialInsuranceRate, 0, MidpointRounding.AwayFromZero);
            empBhyt = Math.Round(cappedBhxhBase * input.EmployeeHealthInsuranceRate, 0, MidpointRounding.AwayFromZero);
            empBhtn = Math.Round(cappedBhtnBase * input.EmployeeUnemploymentInsuranceRate, 0, MidpointRounding.AwayFromZero);

            lines.Add(new PayrollLine(
                "BHXH_NLD",
                "Bảo hiểm xã hội người lao động (8%)",
                empBhxh,
                IsTaxable: false,
                IsInsurable: false,
                $"{FormatMoney(cappedBhxhBase)} × {input.EmployeeSocialInsuranceRate:P1}"));

            lines.Add(new PayrollLine(
                "BHYT_NLD",
                "Bảo hiểm y tế người lao động (1.5%)",
                empBhyt,
                IsTaxable: false,
                IsInsurable: false,
                $"{FormatMoney(cappedBhxhBase)} × {input.EmployeeHealthInsuranceRate:P1}"));

            lines.Add(new PayrollLine(
                "BHTN_NLD",
                "Bảo hiểm thất nghiệp người lao động (1%)",
                empBhtn,
                IsTaxable: false,
                IsInsurable: false,
                $"{FormatMoney(cappedBhtnBase)} × {input.EmployeeUnemploymentInsuranceRate:P1}"));

            // Đoàn phí công đoàn (1% - trần 10% mức lương cơ sở)
            if (input.IsUnionMember)
            {
                decimal unionCap = 0.10m * input.BaseSalary;
                empUnion = Math.Min(Math.Round(cappedBhxhBase * input.EmployeeUnionRate, 0, MidpointRounding.AwayFromZero), unionCap);
                lines.Add(new PayrollLine(
                    "DOAN_PHI_NLD",
                    "Đoàn phí công đoàn (1%)",
                    empUnion,
                    IsTaxable: false,
                    IsInsurable: false,
                    $"min({FormatMoney(cappedBhxhBase)} × 1%, {FormatMoney(unionCap)})"));
            }

            // Chi phí công ty (Employer Cost)
            erBhxh = Math.Round(cappedBhxhBase * input.EmployerSocialInsuranceRate, 0, MidpointRounding.AwayFromZero);
            erBhyt = Math.Round(cappedBhxhBase * input.EmployerHealthInsuranceRate, 0, MidpointRounding.AwayFromZero);
            erBhtn = Math.Round(cappedBhtnBase * input.EmployerUnemploymentInsuranceRate, 0, MidpointRounding.AwayFromZero);
            erUnion = Math.Round(cappedBhxhBase * input.EmployerUnionRate, 0, MidpointRounding.AwayFromZero);
        }

        result.EmployeeSocialInsurance = empBhxh;
        result.EmployeeHealthInsurance = empBhyt;
        result.EmployeeUnemploymentInsurance = empBhtn;
        result.TotalEmployeeInsurance = empBhxh + empBhyt + empBhtn;
        result.EmployeeUnionFee = empUnion;

        result.EmployerSocialInsurance = erBhxh;
        result.EmployerHealthInsurance = erBhyt;
        result.EmployerUnemploymentInsurance = erBhtn;
        result.EmployerUnionFee = erUnion;
        result.TotalEmployerInsurance = erBhxh + erBhyt + erBhtn + erUnion;
        result.TotalEmployerCost = grossSalary + result.TotalEmployerInsurance;

        // 10. THUẾ THU NHẬP CÁ NHÂN (PIT)
        // Thu nhập chịu thuế = Gross - Phần tăng ca vượt lương thường - Các khoản miễn thuế trong hạn mức
        decimal exemptAllowances = 0m;
        if (input.TaxExemptAllowances > 0)
        {
            exemptAllowances = Math.Min(input.TaxExemptAllowances, input.LunchAllowanceTaxExemptCap);
        }

        decimal totalExemptFromTax = exemptOtPay + exemptAllowances;
        decimal taxableIncome = Math.Max(0m, grossSalary - totalExemptFromTax);
        result.TaxExemptAmount = totalExemptFromTax;
        result.TaxableIncome = taxableIncome;

        decimal selfRelief = input.SelfDeduction;
        decimal dependentRelief = input.DependentCount * input.DependentDeductionPerPerson;
        result.SelfRelief = selfRelief;
        result.DependentRelief = dependentRelief;

        decimal totalReliefAndInsurance = result.TotalEmployeeInsurance + selfRelief + dependentRelief;
        decimal taxAssessedIncome = Math.Max(0m, taxableIncome - totalReliefAndInsurance);
        result.TaxAssessedIncome = taxAssessedIncome;

        decimal pit = 0m;
        string pitTrace = "0";

        if (input.IsNonResidentOrShortTerm)
        {
            // Hợp đồng dưới 3 tháng / Không cư trú: Khấu trừ theo tỷ lệ cố định
            pit = Math.Round(taxableIncome * input.FlatWithholdingRate, 0, MidpointRounding.AwayFromZero);
            pitTrace = $"{FormatMoney(taxableIncome)} × {input.FlatWithholdingRate:P0}";
        }
        else if (taxAssessedIncome > 0)
        {
            pit = CalculateProgressiveTax(taxAssessedIncome, input.TaxBrackets, out pitTrace);
        }

        result.PersonalIncomeTax = pit;
        if (pit > 0)
        {
            lines.Add(new PayrollLine(
                "THUE_TNCN",
                "Thuế thu nhập cá nhân",
                pit,
                IsTaxable: false,
                IsInsurable: false,
                pitTrace));
        }

        // 11. KHẤU TRỪ BỒI THƯỜNG THIỆT HẠI (KHÔNG QUÁ 30% LƯƠNG THỰC TRẢ SAU BH VÀ THUẾ)
        decimal netBeforeDamage = Math.Max(0m, grossSalary - result.TotalEmployeeInsurance - empUnion - pit);
        decimal maxAllowedDamage = Math.Round(netBeforeDamage * 0.30m, 0, MidpointRounding.AwayFromZero);

        decimal damageDeduction = 0m;
        if (input.DamageCompensationAmount > 0)
        {
            damageDeduction = Math.Min(input.DamageCompensationAmount, maxAllowedDamage);
            lines.Add(new PayrollLine(
                "KHAU_TRU_BOI_THUONG",
                "Khấu trừ bồi thường thiệt hại (tối đa 30% Net)",
                damageDeduction,
                IsTaxable: false,
                IsInsurable: false,
                $"min({FormatMoney(input.DamageCompensationAmount)}, {FormatMoney(netBeforeDamage)} × 30%)"));
        }

        result.DamageCompensationDeduction = damageDeduction;
        result.TotalDeductions = result.TotalEmployeeInsurance + empUnion + pit + damageDeduction;
        result.NetSalary = netBeforeDamage - damageDeduction;

        result.Lines = lines;
        result.CompactFormulaTrace = $"Gross: {FormatMoney(grossSalary)} - BH: {FormatMoney(result.TotalEmployeeInsurance)} - Thuế: {FormatMoney(pit)} = Net: {FormatMoney(result.NetSalary)}";

        // Cảnh báo nếu Gross < Lương tối thiểu vùng
        if (result.GrossSalary < input.RegionalMinimumWage)
        {
            result.Warnings.Add($"ERR_SAL_BELOW_MIN_WAGE: Thu nhập Gross ({FormatMoney(result.GrossSalary)}) thấp hơn Lương tối thiểu vùng ({FormatMoney(input.RegionalMinimumWage)}).");
        }

        return result;
    }

    /// <summary>
    /// Tính thuế TNCN lũy tiến từng phần theo biểu thuế truyền vào.
    /// Hỗ trợ cả biểu 5 bậc (từ 2026) và biểu 7 bậc (trước 2026).
    /// </summary>
    public static decimal CalculateProgressiveTax(decimal taxableIncome, IReadOnlyList<TaxBracket>? taxBrackets, out string formulaTrace)
    {
        if (taxableIncome <= 0)
        {
            formulaTrace = "0";
            return 0m;
        }

        if (taxBrackets != null && taxBrackets.Count > 0)
        {
            decimal totalTax = 0m;
            decimal previousUpper = 0m;
            var parts = new List<string>();

            foreach (var bracket in taxBrackets.OrderBy(b => b.Order))
            {
                if (taxableIncome <= previousUpper) break;

                decimal taxableInBracket = bracket.UpperBoundMonthly.HasValue
                    ? Math.Min(taxableIncome, bracket.UpperBoundMonthly.Value) - previousUpper
                    : taxableIncome - previousUpper;

                if (taxableInBracket > 0)
                {
                    decimal taxInBracket = Math.Round(taxableInBracket * bracket.Rate, 0, MidpointRounding.AwayFromZero);
                    totalTax += taxInBracket;
                    parts.Add($"{FormatMoney(taxableInBracket)} × {bracket.Rate:P0} = {FormatMoney(taxInBracket)}");
                }

                if (!bracket.UpperBoundMonthly.HasValue) break;
                previousUpper = bracket.UpperBoundMonthly.Value;
            }

            formulaTrace = string.Join(" + ", parts);
            return totalTax;
        }

        // Biểu 5 bậc mới mặc định nếu không truyền taxBrackets (từ 01/01/2026)
        if (taxableIncome <= 10_000_000m)
        {
            decimal tax = Math.Round(taxableIncome * 0.05m, 0, MidpointRounding.AwayFromZero);
            formulaTrace = $"{FormatMoney(taxableIncome)} × 5% - 0 = {FormatMoney(tax)}";
            return tax;
        }
        if (taxableIncome <= 30_000_000m)
        {
            decimal tax = Math.Round(taxableIncome * 0.10m - 500_000m, 0, MidpointRounding.AwayFromZero);
            formulaTrace = $"{FormatMoney(taxableIncome)} × 10% - 500.000 = {FormatMoney(tax)}";
            return tax;
        }
        if (taxableIncome <= 60_000_000m)
        {
            decimal tax = Math.Round(taxableIncome * 0.20m - 3_500_000m, 0, MidpointRounding.AwayFromZero);
            formulaTrace = $"{FormatMoney(taxableIncome)} × 20% - 3.500.000 = {FormatMoney(tax)}";
            return tax;
        }
        if (taxableIncome <= 100_000_000m)
        {
            decimal tax = Math.Round(taxableIncome * 0.30m - 9_500_000m, 0, MidpointRounding.AwayFromZero);
            formulaTrace = $"{FormatMoney(taxableIncome)} × 30% - 9.500.000 = {FormatMoney(tax)}";
            return tax;
        }
        else
        {
            decimal tax = Math.Round(taxableIncome * 0.35m - 14_500_000m, 0, MidpointRounding.AwayFromZero);
            formulaTrace = $"{FormatMoney(taxableIncome)} × 35% - 14.500.000 = {FormatMoney(tax)}";
            return tax;
        }
    }

    /// <summary>
    /// Tính thuế TNCN theo công thức rút gọn (số trừ nhanh) cho biểu 5 bậc 2026.
    /// Dùng để kiểm tra đối chiếu (Cross-check) với cách tính từng bậc.
    /// </summary>
    public static decimal CalculateTaxByQuickFormula5Brackets(decimal taxableIncome)
    {
        if (taxableIncome <= 0) return 0m;
        if (taxableIncome <= 10_000_000m) return Math.Round(taxableIncome * 0.05m, 0, MidpointRounding.AwayFromZero);
        if (taxableIncome <= 30_000_000m) return Math.Round(taxableIncome * 0.10m - 500_000m, 0, MidpointRounding.AwayFromZero);
        if (taxableIncome <= 60_000_000m) return Math.Round(taxableIncome * 0.20m - 3_500_000m, 0, MidpointRounding.AwayFromZero);
        if (taxableIncome <= 100_000_000m) return Math.Round(taxableIncome * 0.30m - 9_500_000m, 0, MidpointRounding.AwayFromZero);
        return Math.Round(taxableIncome * 0.35m - 14_500_000m, 0, MidpointRounding.AwayFromZero);
    }

    public static string FormatMoney(decimal amount) => amount.ToString("#,##0", ViCulture);
}
