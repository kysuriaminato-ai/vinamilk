using VinamilkHRM.Domain.Entities.Salaries;
using System;
using System.Collections.Generic;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Attendances;
using VinamilkHRM.Domain.Entities.Kpis;
using VinamilkHRM.Domain.Entities.Statutory;
using VinamilkHRM.Domain.Enums;
using VinamilkHRM.Domain.Services.Payroll;
using Xunit;

namespace VinamilkHRM.Domain.Tests.Payroll;

public class PayrollCalculatorTests
{
    private static List<TaxBracket> Get5Brackets2026() => new()
    {
        new("PIT_REGULAR", 1, 10_000_000m, 0.05m, new DateOnly(2026, 1, 1), null, "Bậc 1: Đến 10 triệu"),
        new("PIT_REGULAR", 2, 30_000_000m, 0.10m, new DateOnly(2026, 1, 1), null, "Bậc 2: Trên 10 đến 30 triệu"),
        new("PIT_REGULAR", 3, 60_000_000m, 0.20m, new DateOnly(2026, 1, 1), null, "Bậc 3: Trên 30 đến 60 triệu"),
        new("PIT_REGULAR", 4, 100_000_000m, 0.30m, new DateOnly(2026, 1, 1), null, "Bậc 4: Trên 60 đến 100 triệu"),
        new("PIT_REGULAR", 5, null, 0.35m, new DateOnly(2026, 1, 1), null, "Bậc 5: Trên 100 triệu")
    };

    private static List<TaxBracket> Get7Brackets2025() => new()
    {
        new("PIT_REGULAR", 1, 5_000_000m, 0.05m, new DateOnly(2009, 1, 1), new DateOnly(2025, 12, 31), "Đến 5 triệu"),
        new("PIT_REGULAR", 2, 10_000_000m, 0.10m, new DateOnly(2009, 1, 1), new DateOnly(2025, 12, 31), "Trên 5 đến 10 triệu"),
        new("PIT_REGULAR", 3, 18_000_000m, 0.15m, new DateOnly(2009, 1, 1), new DateOnly(2025, 12, 31), "Trên 10 đến 18 triệu"),
        new("PIT_REGULAR", 4, 32_000_000m, 0.20m, new DateOnly(2009, 1, 1), new DateOnly(2025, 12, 31), "Trên 18 đến 32 triệu"),
        new("PIT_REGULAR", 5, 52_000_000m, 0.25m, new DateOnly(2009, 1, 1), new DateOnly(2025, 12, 31), "Trên 32 đến 52 triệu"),
        new("PIT_REGULAR", 6, 80_000_000m, 0.30m, new DateOnly(2009, 1, 1), new DateOnly(2025, 12, 31), "Trên 52 đến 80 triệu"),
        new("PIT_REGULAR", 7, null, 0.35m, new DateOnly(2009, 1, 1), new DateOnly(2025, 12, 31), "Trên 80 triệu")
    };

    [Fact]
    public void Criterion1_Worker_Payroll_MatchesExactAmounts()
    {
        // TIÊU CHÍ 1:
        // Công nhân: P1 12.000.000 + phụ cấp 1.000.000, 24/26 ngày, 6 giờ tăng ca ngày thường,
        // 48 giờ đêm, KPI B, 1 người phụ thuộc, kỳ 2026-10
        // -> gross 15.124.038, BH 1.365.000, thuế 0, net 13.759.038.

        var timesheet = new MonthlyTimesheetSummary("2026-10", 101, 10, standardWorkDays: 26m);
        timesheet.UpdateMetrics(
            actualWorkDays: 24m,
            paidLeaveDays: 0m,
            sickLeaveDays: 0m,
            maternityLeaveDays: 0m,
            holidayDays: 0m,
            compensatoryLeaveDays: 0m,
            businessTripDays: 0m,
            trainingDays: 0m,
            unpaidLeaveDays: 0m,
            unexcusedAbsenceDays: 0m,
            consecutiveUnexcusedDays: 0,
            totalWorkHours: 192m,
            nightHours: 48m,
            regularOtHours: 6m,
            restDayOtHours: 0m,
            holidayOtHours: 0m,
            nightOtHours: 0m,
            lateTimes: 0,
            earlyLeaveTimes: 0);

        var kpiResult = new KpiResult(
            employeeId: 101,
            period: "2026-10",
            totalScore: 98m,
            grade: KpiGrade.B,
            bonusCoefficient: 1.0m,
            unitResultScore: 20m,
            individualWorkScore: 50m,
            peopleManagementScore: 10m,
            complianceScore: 18m,
            isUnrated: false,
            lockedBy: "SYSTEM");

        var input = new PayrollInput
        {
            EmployeeId = 101,
            EmployeeCode = "NM-001",
            FullName = "Nguyễn Văn Công Nhân",
            PayrollPeriod = "2026-10",
            PeriodEndDate = new DateOnly(2026, 10, 31),
            LeadershipLevel = LeadershipLevel.None,
            Region = MinimumWageRegion.RegionI,
            IsUnionMember = false, // Không tính đoàn phí riêng trong tiêu chí
            DependentCount = 1,
            P1BaseSalary = 12_000_000m,
            FixedAllowances = 1_000_000m,
            InsurableAllowances = 1_000_000m,
            TimesheetSummary = timesheet,
            IsTimesheetLocked = true,
            KpiResult = kpiResult,
            BaseSalary = 2_530_000m,
            RegionalMinimumWage = 5_310_000m,
            SelfDeduction = 15_500_000m,
            DependentDeductionPerPerson = 6_200_000m,
            TaxBrackets = Get5Brackets2026()
        };

        var result = PayrollCalculator.Calculate(input);

        Assert.Equal(15_124_038m, result.GrossSalary);
        Assert.Equal(1_365_000m, result.TotalEmployeeInsurance);
        Assert.Equal(0m, result.PersonalIncomeTax);
        Assert.Equal(13_759_038m, result.NetSalary);

        // Kiểm tra FormulaTrace dễ hiểu
        var timeSalaryLine = result.Lines.Find(l => l.Code == "LUONG_THOI_GIAN");
        Assert.NotNull(timeSalaryLine);
        Assert.Equal("13.000.000 × 24/26", timeSalaryLine.FormulaTrace);
    }

    [Fact]
    public void Criterion2_Manager_Payroll_MatchesExactAmounts()
    {
        // TIÊU CHÍ 2:
        // Trưởng phòng: P1 30.000.000 + phụ cấp 3.000.000, đủ 22/22, KPI B, không người phụ thuộc
        // -> gross 40.500.000, thuế 1.653.500, net 35.381.500.

        var timesheet = new MonthlyTimesheetSummary("2026-10", 201, 1, standardWorkDays: 22m);
        timesheet.UpdateMetrics(
            actualWorkDays: 22m,
            paidLeaveDays: 0m,
            sickLeaveDays: 0m,
            maternityLeaveDays: 0m,
            holidayDays: 0m,
            compensatoryLeaveDays: 0m,
            businessTripDays: 0m,
            trainingDays: 0m,
            unpaidLeaveDays: 0m,
            unexcusedAbsenceDays: 0m,
            consecutiveUnexcusedDays: 0,
            totalWorkHours: 176m,
            nightHours: 0m,
            regularOtHours: 0m,
            restDayOtHours: 0m,
            holidayOtHours: 0m,
            nightOtHours: 0m,
            lateTimes: 0,
            earlyLeaveTimes: 0);

        var kpiResult = new KpiResult(
            employeeId: 201,
            period: "2026-10",
            totalScore: 100m,
            grade: KpiGrade.B,
            bonusCoefficient: 1.0m,
            unitResultScore: 40m,
            individualWorkScore: 35m,
            peopleManagementScore: 15m,
            complianceScore: 10m,
            isUnrated: false,
            lockedBy: "SYSTEM");

        var input = new PayrollInput
        {
            EmployeeId = 201,
            EmployeeCode = "HQ-002",
            FullName = "Trần Trưởng Phòng",
            PayrollPeriod = "2026-10",
            PeriodEndDate = new DateOnly(2026, 10, 31),
            LeadershipLevel = LeadershipLevel.Manager, // Tỷ lệ thưởng mục tiêu = 25%
            Region = MinimumWageRegion.RegionI,
            IsUnionMember = false,
            DependentCount = 0,
            P1BaseSalary = 30_000_000m,
            FixedAllowances = 3_000_000m,
            InsurableAllowances = 3_000_000m,
            TimesheetSummary = timesheet,
            IsTimesheetLocked = true,
            KpiResult = kpiResult,
            BaseSalary = 2_530_000m,
            RegionalMinimumWage = 5_310_000m,
            SelfDeduction = 15_500_000m,
            DependentDeductionPerPerson = 6_200_000m,
            TaxBrackets = Get5Brackets2026()
        };

        var result = PayrollCalculator.Calculate(input);

        Assert.Equal(40_500_000m, result.GrossSalary);
        Assert.Equal(3_465_000m, result.TotalEmployeeInsurance);
        Assert.Equal(21_535_000m, result.TaxAssessedIncome); // 40.5M - 3.465M - 15.5M
        Assert.Equal(1_653_500m, result.PersonalIncomeTax);
        Assert.Equal(35_381_500m, result.NetSalary);
    }

    [Theory]
    [InlineData(10_000_000, 500_000)]
    [InlineData(30_000_000, 2_500_000)]
    [InlineData(60_000_000, 8_500_000)]
    [InlineData(100_000_000, 20_500_000)]
    public void Criterion3_QuickFormulaAndBracketCalculation_YieldIdenticalResults(decimal income, decimal expectedTax)
    {
        // TIÊU CHÍ 3:
        // Công thức rút gọn và cách tính từng bậc cho cùng kết quả tại các mốc 10, 30, 60, 100 triệu.

        var brackets = Get5Brackets2026();
        decimal bracketTax = PayrollCalculator.CalculateProgressiveTax(income, brackets, out string trace);
        decimal quickTax = PayrollCalculator.CalculateTaxByQuickFormula5Brackets(income);

        Assert.Equal(expectedTax, bracketTax);
        Assert.Equal(expectedTax, quickTax);
    }

    [Fact]
    public void Criterion4_InsuranceSalary60M_CappedAt50M600()
    {
        // TIÊU CHÍ 4:
        // Lương đóng BH 60.000.000 kỳ 2026-10 -> BHXH tính trên 50.600.000 (20 × lương cơ sở 2.530.000).

        var timesheet = new MonthlyTimesheetSummary("2026-10", 301, 1, standardWorkDays: 22m);
        timesheet.UpdateMetrics(22m, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 176m, 0, 0, 0, 0, 0, 0, 0);

        var input = new PayrollInput
        {
            EmployeeId = 301,
            EmployeeCode = "DIR-001",
            FullName = "Lê Giám Đốc",
            PayrollPeriod = "2026-10",
            PeriodEndDate = new DateOnly(2026, 10, 31),
            LeadershipLevel = LeadershipLevel.Director,
            P1BaseSalary = 60_000_000m,
            TimesheetSummary = timesheet,
            IsTimesheetLocked = true,
            BaseSalary = 2_530_000m, // 20 × 2.530.000 = 50.600.000
            RegionalMinimumWage = 5_310_000m,
            SelfDeduction = 15_500_000m,
            DependentDeductionPerPerson = 6_200_000m,
            TaxBrackets = Get5Brackets2026(),
            IsUnionMember = false
        };

        var result = PayrollCalculator.Calculate(input);

        Assert.Equal(50_600_000m, result.InsurableSalary);
        Assert.Equal(4_048_000m, result.EmployeeSocialInsurance); // 50.600.000 × 8%
        Assert.Equal(759_000m, result.EmployeeHealthInsurance);   // 50.600.000 × 1.5%
        Assert.Equal(600_000m, result.EmployeeUnemploymentInsurance); // 60M < trần BHTN (20 × 5.31M = 106.2M)
    }

    [Fact]
    public void Criterion5_Period2025_Uses11MSelfDeduction_And7Brackets()
    {
        // TIÊU CHÍ 5:
        // Kỳ 2025-12 dùng giảm trừ 11.000.000 và biểu 7 bậc cũ.

        var timesheet = new MonthlyTimesheetSummary("2025-12", 401, 1, standardWorkDays: 22m);
        timesheet.UpdateMetrics(22m, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 176m, 0, 0, 0, 0, 0, 0, 0);

        var input = new PayrollInput
        {
            EmployeeId = 401,
            EmployeeCode = "EMP-2025",
            FullName = "Hoàng Nhân Viên",
            PayrollPeriod = "2025-12",
            PeriodEndDate = new DateOnly(2025, 12, 31),
            LeadershipLevel = LeadershipLevel.None,
            P1BaseSalary = 25_000_000m,
            TimesheetSummary = timesheet,
            IsTimesheetLocked = true,
            BaseSalary = 2_340_000m,
            RegionalMinimumWage = 4_960_000m,
            SelfDeduction = 11_000_000m,
            DependentDeductionPerPerson = 4_400_000m,
            TaxBrackets = Get7Brackets2025(),
            IsUnionMember = false
        };

        var result = PayrollCalculator.Calculate(input);

        // BH: 25M × 10.5% = 2.625.000
        Assert.Equal(2_625_000m, result.TotalEmployeeInsurance);
        // TNTT: 25M - 2.625M - 11M = 11.375.000
        Assert.Equal(11_375_000m, result.TaxAssessedIncome);
        // Thuế theo biểu 7 bậc:
        // Bậc 1: 5M × 5% = 250k
        // Bậc 2: 5M × 10% = 500k
        // Bậc 3: (11.375M - 10M) × 15% = 1.375M × 15% = 206.250
        // Tổng thuế = 250k + 500k + 206.250 = 956.250
        Assert.Equal(956_250m, result.PersonalIncomeTax);
    }

    [Fact]
    public void Criterion6_UnlockedOrMissingTimesheet_ThrowsException()
    {
        // TIÊU CHÍ:
        // Không có bảng công đã khóa -> không tính và báo lỗi ERR_PAY_NO_LOCKED_TIMESHEET (bỏ hẳn mặc định 22 công).

        var inputMissing = new PayrollInput
        {
            EmployeeId = 501,
            TimesheetSummary = null,
            IsTimesheetLocked = false
        };

        var ex1 = Assert.Throws<DomainException>(() => PayrollCalculator.Calculate(inputMissing));
        Assert.Equal("ERR_PAY_NO_LOCKED_TIMESHEET", ex1.ErrorCode);

        var timesheetUnlocked = new MonthlyTimesheetSummary("2026-10", 501, 1, standardWorkDays: 22m);
        var inputUnlocked = new PayrollInput
        {
            EmployeeId = 501,
            TimesheetSummary = timesheetUnlocked,
            IsTimesheetLocked = false
        };

        var ex2 = Assert.Throws<DomainException>(() => PayrollCalculator.Calculate(inputUnlocked));
        Assert.Equal("ERR_PAY_NO_LOCKED_TIMESHEET", ex2.ErrorCode);
    }

    [Fact]
    public void NonPaidDays14OrMore_ExemptFromInsurance()
    {
        // Tháng có >= 14 ngày làm việc không hưởng lương -> không trích BHXH
        var timesheet = new MonthlyTimesheetSummary("2026-10", 601, 1, standardWorkDays: 26m);
        timesheet.UpdateMetrics(
            actualWorkDays: 12m,
            paidLeaveDays: 0m,
            sickLeaveDays: 0m,
            maternityLeaveDays: 0m,
            holidayDays: 0m,
            compensatoryLeaveDays: 0m,
            businessTripDays: 0m,
            trainingDays: 0m,
            unpaidLeaveDays: 14m, // >= 14 ngày nghỉ không lương
            unexcusedAbsenceDays: 0m,
            consecutiveUnexcusedDays: 0,
            totalWorkHours: 96m,
            nightHours: 0m,
            regularOtHours: 0m,
            restDayOtHours: 0m,
            holidayOtHours: 0m,
            nightOtHours: 0m,
            lateTimes: 0,
            earlyLeaveTimes: 0);

        var input = new PayrollInput
        {
            EmployeeId = 601,
            P1BaseSalary = 10_000_000m,
            TimesheetSummary = timesheet,
            IsTimesheetLocked = true,
            TaxBrackets = Get5Brackets2026()
        };

        var result = PayrollCalculator.Calculate(input);

        Assert.True(result.IsExemptFromInsurance);
        Assert.Equal(0m, result.TotalEmployeeInsurance);
        Assert.Equal(0m, result.TotalEmployerInsurance);
    }

    [Fact]
    public void AttendanceAllowance_LostWhenUnexcusedAbsenceOrExcessiveLate()
    {
        var timesheet = new MonthlyTimesheetSummary("2026-10", 701, 1, standardWorkDays: 22m);
        timesheet.UpdateMetrics(21m, 0, 0, 0, 0, 0, 0, 0, 0, 1m, 1, 168m, 0, 0, 0, 0, 0, 1, 0); // Có 1 ngày KP

        var input = new PayrollInput
        {
            EmployeeId = 701,
            P1BaseSalary = 15_000_000m,
            AttendanceAllowance = 500_000m,
            TimesheetSummary = timesheet,
            IsTimesheetLocked = true,
            TaxBrackets = Get5Brackets2026()
        };

        var result = PayrollCalculator.Calculate(input);
        var attendanceLine = result.Lines.Find(l => l.Code == "PHU_CAP_CHUYEN_CAN");

        Assert.NotNull(attendanceLine);
        Assert.Equal(0m, attendanceLine.Amount);
    }

    [Fact]
    public void DamageCompensation_CappedAt30PercentOfNet()
    {
        var timesheet = new MonthlyTimesheetSummary("2026-10", 801, 1, standardWorkDays: 22m);
        timesheet.UpdateMetrics(22m, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 176m, 0, 0, 0, 0, 0, 0, 0);

        var input = new PayrollInput
        {
            EmployeeId = 801,
            P1BaseSalary = 20_000_000m,
            DamageCompensationAmount = 10_000_000m, // Đòi bồi thường 10 triệu
            TimesheetSummary = timesheet,
            IsTimesheetLocked = true,
            IsUnionMember = false,
            TaxBrackets = Get5Brackets2026()
        };

        var result = PayrollCalculator.Calculate(input);

        // Net trước bồi thường = Gross (20M) - BH (2.1M) - Thuế (117.500) = 17.782.500
        // Trần 30% = 17.782.500 × 30% = 5.334.750
        Assert.Equal(5_334_000m, result.DamageCompensationDeduction);
        Assert.Equal(17_780_000m - 5_334_000m, result.NetSalary);
    }

    [Theory]
    [InlineData(PayrollRunStatus.Approved)]
    [InlineData(PayrollRunStatus.Locked)]
    [InlineData(PayrollRunStatus.Paid)]
    public void Recalculate_ApprovedOrLockedPeriod_ThrowsErrPayPeriodLocked(PayrollRunStatus status)
    {
        // TIÊU CHÍ 6:
        // Tính lại kỳ đã Approved -> 422 ERR_PAY_PERIOD_LOCKED.
        var run = new PayrollRun("2026-10");
        run.UpdateTotals(100M, 80M, 120M, 5M, 10M, 20M, 1);
        run.Submit(1);
        run.Approve(10, 20);

        if (status == PayrollRunStatus.Locked)
        {
            run.Lock(30);
        }
        else if (status == PayrollRunStatus.Paid)
        {
            run.MarkAsPaid(40, "VCB-123456");
        }

        var ex = Assert.Throws<DomainException>(() => run.EnsureCanRecalculate());
        Assert.Equal("ERR_PAY_PERIOD_LOCKED", ex.ErrorCode);
}



    [Fact]
    public void Criterion7_Benchmark_10000Employees_CalculatedUnder5Minutes()
    {
        // TIÊU CHÍ 7:
        // Tính 10.000 nhân viên dữ liệu giả lập không quá 5 phút trên máy dev; ghi thời gian chạy vào CHANGELOG.
        var brackets = Get5Brackets2026();
        var inputs = new List<PayrollInput>(10_000);

        for (int i = 1; i <= 10_000; i++)
        {
            var ts = new MonthlyTimesheetSummary("2026-10", i, 1, standardWorkDays: 26m);
            ts.UpdateMetrics(
                actualWorkDays: 24m,
                paidLeaveDays: 1m,
                sickLeaveDays: 0,
                maternityLeaveDays: 0,
                holidayDays: 1m,
                compensatoryLeaveDays: 0,
                businessTripDays: 0,
                trainingDays: 0,
                unpaidLeaveDays: 0,
                unexcusedAbsenceDays: 0,
                consecutiveUnexcusedDays: 0,
                totalWorkHours: 192m,
                nightHours: (i % 2 == 0) ? 24m : 0m,
                regularOtHours: (i % 3 == 0) ? 8m : 0m,
                restDayOtHours: 0,
                holidayOtHours: 0,
                nightOtHours: 0,
                lateTimes: 0,
                earlyLeaveTimes: 0);

            var kpi = new KpiResult(i, "2026-10", 95m, KpiGrade.B, 1.0m, 20m, 50m, 10m, 15m, false, "SYSTEM");

            inputs.Add(new PayrollInput
            {
                EmployeeId = i,
                EmployeeCode = $"VNM-{i:D5}",
                FullName = $"Nhân Viên Vinamilk #{i}",
                PayrollPeriod = "2026-10",
                PeriodEndDate = new DateOnly(2026, 10, 31),
                LeadershipLevel = (LeadershipLevel)(i % 5),
                Region = MinimumWageRegion.RegionI,
                IsUnionMember = (i % 2 == 0),
                DependentCount = i % 3,
                P1BaseSalary = 10_000_000m + (i % 20) * 1_000_000m,
                FixedAllowances = 1_000_000m,
                InsurableAllowances = 1_000_000m,
                TimesheetSummary = ts,
                IsTimesheetLocked = true,
                KpiResult = kpi,
                BaseSalary = 2_530_000m,
                RegionalMinimumWage = 5_310_000m,
                SelfDeduction = 15_500_000m,
                DependentDeductionPerPerson = 6_200_000m,
                TaxBrackets = brackets
            });
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var results = new List<PayrollResult>(10_000);

        foreach (var input in inputs)
        {
            results.Add(PayrollCalculator.Calculate(input));
        }

        sw.Stop();

        Assert.Equal(10_000, results.Count);
        // Kiểm tra thời gian thực thi: < 5 phút (300.000 ms)
        Assert.True(sw.ElapsedMilliseconds < 300_000, $"Thời gian chạy 10.000 nhân viên: {sw.ElapsedMilliseconds} ms (vượt quá 5 phút)");
    }
}


