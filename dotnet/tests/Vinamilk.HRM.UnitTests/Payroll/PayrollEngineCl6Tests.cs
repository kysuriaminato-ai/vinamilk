using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Payroll;
using Vinamilk.HRM.Domain.Entities.Core;
using Vinamilk.HRM.Domain.Entities.Payroll;
using Vinamilk.HRM.Domain.Entities.Talent;
using Vinamilk.HRM.Domain.Entities.Time;
using Vinamilk.HRM.Infrastructure.Persistence;
using Vinamilk.HRM.Infrastructure.Persistence.Seed;
using Xunit;

namespace Vinamilk.HRM.UnitTests.Payroll;

/// <summary>
/// Kiểm thử động cơ tính lương CL6 trên ≥ 15 hồ sơ mẫu tính tay độc lập (KHÔNG dùng hằng số nội bộ engine).
/// Toàn bộ số liệu kỳ tháng 08/2026 & 12/2026 đối chiếu theo cấu hình Luật 109/2025, NQ 110/2025, NĐ 293/2025 (LCS = 2.530.000đ).
/// </summary>
public class PayrollEngineCl6Tests
{
    private static async Task<ApplicationDbContext> CreateSeededDbContextAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"VnmPayrollCl6_{Guid.NewGuid():N}")
            .Options;
        var db = new ApplicationDbContext(options);

        await db.PayrollStatutoryParameters.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultStatutoryParameters());
        await db.PitBrackets.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultPitBrackets());
        await db.PayrollParameters.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultTaxExemptPayrollParameters());
        await db.SaveChangesAsync();

        return db;
    }

    private static (OrganizationUnit Unit1, OrganizationUnit Unit2, Position Pos) CreateBasicOrg(int region1 = 1, int region2 = 2)
    {
        return (
            new OrganizationUnit { Id = 1, UnitCode = "HQ", UnitName = "Trụ sở chính", WageRegion = region1, IsActive = true },
            new OrganizationUnit { Id = 2, UnitCode = "BD", UnitName = "Nhà máy Bình Dương", WageRegion = region2, IsActive = true },
            new Position { Id = 1, PositionCode = "NV", PositionName = "Chuyên viên", IsActive = true }
        );
    }

    // =========================================================================
    // HỒ SƠ 01: Nhân viên cư trú chuẩn đủ 26/26 công, 0 NPT, Đoàn viên, tính cả Tổng Chi Phí DN (BHTNLĐ-BNN 0.5%)
    // =========================================================================
    [Fact]
    public async Task Profile01_StandardResidentFullMonth_ShouldComputeExactNetAndTotalCompanyCost()
    {
        // Tính tay kỳ 08/2026 (LCS = 2.530.000 đ, LTTV Vùng I = 5.310.000 đ, GT bản thân = 15.500.000 đ):
        // - LCB = 26.000.000 đ, Phụ cấp CV = 2.000.000 đ -> Gross = 28.000.000 đ
        // - Nền BHXH/BHYT/BHTN = 28.000.000 đ (< trần 50.600.000 & 106.200.000)
        //   + NLĐ: BHXH (8%) = 2.240.000; BHYT (1.5%) = 420.000; BHTN (1%) = 280.000 -> Tổng BH NLĐ = 2.940.000 đ
        //   + DN: BHXH (17.5%) = 4.900.000; BHYT (3%) = 840.000; BHTN (1%) = 280.000; BHTNLĐ-BNN (0.5%) = 140.000; KPCĐ (2%) = 560.000
        //     -> Tổng chi phí DN = 28.000.000 + 6.720.000 = 34.720.000 đ
        // - Thu nhập chịu thuế = 28.000.000 - 2.940.000 = 25.060.000 đ
        // - Thu nhập tính thuế = 25.060.000 - 15.500.000 = 9.560.000 đ (Bậc 1: 5%)
        // - Thuế TNCN = Round(9.560.000 * 0.05, 0) = 478.000 đ
        // - Thực lĩnh = 28.000.000 - 2.940.000 - 478.000 = 24.582.000 đ
        await using var db = await CreateSeededDbContextAsync();
        var ctx = await VietnamPayrollEngine.ResolveStatutoryContextAsync(db, new DateOnly(2026, 8, 31));

        var res = VietnamPayrollEngine.Calculate(new PayrollCalculationInput(
            ContractBaseSalary: 26_000_000m,
            PositionAllowance: 2_000_000m,
            OtherAllowance: 0m,
            StandardWorkdays: 26m,
            PayableWorkdays: 26m,
            OtHours150: 0m, OtHours200: 0m, OtHours300: 0m,
            KpiBonus: 0m,
            DisciplineDeduction: 0m,
            DependentCount: 0,
            IsUnionMember: true,
            WageRegion: 1), ctx);

        res.GrossIncome.Should().Be(28_000_000m);
        res.TotalEmployeeInsurance.Should().Be(2_940_000m);
        res.CompanyOccupationalAccidentInsurance.Should().Be(140_000m);
        res.CompanyUnionFee.Should().Be(560_000m);
        res.TotalCompanyCost.Should().Be(34_720_000m);
        res.AssessableIncome.Should().Be(9_560_000m);
        res.PersonalIncomeTax.Should().Be(478_000m);
        res.NetSalary.Should().Be(24_582_000m);
    }

    // =========================================================================
    // HỒ SƠ 02: Lọc Người phụ thuộc theo khoảng thời gian hiệu lực trong kỳ (2 hợp lệ, 1 hết hạn, 1 chưa tới hạn)
    // =========================================================================
    [Fact]
    public async Task Profile02_DependentsFilteredByEffectiveDatesInPeriod_ShouldCountOnlyValidOnes()
    {
        // Tính tay kỳ 08/2026 (01/08–31/08/2026):
        // - LCB = 35.000.000 đ, PCCV = 5.000.000 đ -> Gross = 40.000.000 đ
        // - NPT: Con 1 (từ 01/01/2025 -> vô hạn: hợp lệ), Con 2 (15/08/2026 -> 31/12/2028: giao với kỳ 08/2026 -> hợp lệ),
        //   Bố (hết hạn 31/07/2026: LOẠI), Con 3 (bắt đầu 01/09/2026: LOẠI) -> Số NPT hợp lệ = 2 người!
        // - BH NLĐ = 40.000.000 * 10.5% = 4.200.000 đ
        // - GTGC = 15.500.000 + 2 * 6.200.000 = 27.900.000 đ
        // - TNTT = 40.000.000 - 4.200.000 - 27.900.000 = 7.900.000 đ (Bậc 1: 5%)
        // - Thuế TNCN = Round(7.900.000 * 0.05, 0) = 395.000 đ
        // - Thực lĩnh = 40.000.000 - 4.200.000 - 395.000 = 35.405.000 đ
        await using var db = await CreateSeededDbContextAsync();
        var user = new FakeCurrentUserService();
        var (u1, _, pos) = CreateBasicOrg();
        db.OrganizationUnits.Add(u1);
        db.Positions.Add(pos);
        db.Employees.Add(new Employee
        {
            Id = 2, EmployeeCode = "VNM-CL6-02", FullName = "Trần Thị Minh",
            OrganizationUnitId = 1, PositionId = 1, BaseSalary = 35_000_000m, PositionAllowance = 5_000_000m,
            WorkStatus = "Official", IsUnionMember = true, WageRegion = 1
        });
        db.EmployeeDependents.AddRange(
            new EmployeeDependent { EmployeeId = 2, FullName = "Con 1", IsTaxDeductible = true, DeductionFromDate = new DateOnly(2025, 1, 1), DeductionToDate = null },
            new EmployeeDependent { EmployeeId = 2, FullName = "Con 2", IsTaxDeductible = true, DeductionFromDate = new DateOnly(2026, 8, 15), DeductionToDate = new DateOnly(2028, 12, 31) },
            new EmployeeDependent { EmployeeId = 2, FullName = "Hết hạn", IsTaxDeductible = true, DeductionFromDate = new DateOnly(2020, 1, 1), DeductionToDate = new DateOnly(2026, 7, 31) },
            new EmployeeDependent { EmployeeId = 2, FullName = "Tương lai", IsTaxDeductible = true, DeductionFromDate = new DateOnly(2026, 9, 1), DeductionToDate = null }
        );
        db.MonthlyAttendanceSummaries.Add(new MonthlyAttendanceSummary
        {
            EmployeeId = 2, Year = 2026, Month = 8, StandardWorkdays = 26m, TotalPayableWorkdays = 26m, IsLocked = true
        });
        await db.SaveChangesAsync();

        var periodId = await new CreatePayrollPeriodCommandHandler(db)
            .Handle(new CreatePayrollPeriodCommand(2026, 8, 26m), CancellationToken.None);

        await new CalculateMonthlyPayrollCommandHandler(db, user)
            .Handle(new CalculateMonthlyPayrollCommand(periodId, RequireLockedAttendance: true), CancellationToken.None);

        var rec = await db.PayrollRecords.FirstAsync(r => r.PayrollPeriodId == periodId && r.EmployeeId == 2);
        rec.DependentCount.Should().Be(2);
        rec.DependentDeductionAmount.Should().Be(12_400_000m);
        rec.TotalEmployeeInsurance.Should().Be(4_200_000m);
        rec.PersonalIncomeTax.Should().Be(395_000m);
        rec.NetSalary.Should().Be(35_405_000m);
    }

    // =========================================================================
    // HỒ SƠ 03: Lương cao vượt trần BHXH (50.6tr) & BHTN Vùng II (94.6tr) + Lũy tiến nhiều bậc
    // =========================================================================
    [Fact]
    public async Task Profile03_HighIncomeAboveCaps_Region2_ShouldCapInsuranceAndComputeMultiBracketPit()
    {
        // Tính tay kỳ 08/2026, Vùng II (LTTV = 4.730.000đ -> trần BHTN = 20 * 4.730.000 = 94.600.000 đ), LCS = 2.530.000đ (trần BHXH/BHYT = 50.600.000 đ):
        // - Gross = 120.000.000 đ, 0 NPT
        // - BHXH NLĐ = 8% * 50.600.000 = 4.048.000; BHYT NLĐ = 1.5% * 50.600.000 = 759.000; BHTN NLĐ = 1% * 94.600.000 = 946.000
        //   -> Tổng BH NLĐ = 5.753.000 đ
        // - Thu nhập chịu thuế = 120.000.000 - 5.753.000 = 114.247.000 đ
        // - TNTT = 114.247.000 - 15.500.000 = 98.747.000 đ
        // - Thuế lũy tiến 5 bậc:
        //   + Bậc 1 (0..10tr):    10.000.000 * 5%  =    500.000 đ
        //   + Bậc 2 (10..30tr):   20.000.000 * 10% =  2.000.000 đ
        //   + Bậc 3 (30..60tr):   30.000.000 * 20% =  6.000.000 đ
        //   + Bậc 4 (60..98.747tr): 38.747.000 * 30% = 11.624.100 đ
        //   -> Tổng thuế TNCN = 20.124.100 đ
        // - Thực lĩnh = 120.000.000 - 5.753.000 - 20.124.100 = 94.122.900 đ
        await using var db = await CreateSeededDbContextAsync();
        var ctx = await VietnamPayrollEngine.ResolveStatutoryContextAsync(db, new DateOnly(2026, 8, 31));

        var res = VietnamPayrollEngine.Calculate(new PayrollCalculationInput(
            ContractBaseSalary: 120_000_000m,
            PositionAllowance: 0m, OtherAllowance: 0m,
            StandardWorkdays: 26m, PayableWorkdays: 26m,
            OtHours150: 0m, OtHours200: 0m, OtHours300: 0m,
            KpiBonus: 0m, DisciplineDeduction: 0m, DependentCount: 0,
            IsUnionMember: true, WageRegion: 2), ctx);

        res.SocialInsuranceBase.Should().Be(50_600_000m);
        res.UnemploymentInsuranceBase.Should().Be(94_600_000m);
        res.TotalEmployeeInsurance.Should().Be(5_753_000m);
        res.CompanyOccupationalAccidentInsurance.Should().Be(253_000m);
        res.CompanyUnionFee.Should().Be(1_012_000m);
        res.PersonalIncomeTax.Should().Be(20_124_100m);
        res.NetSalary.Should().Be(94_122_900m);
    }

    // =========================================================================
    // HỒ SƠ 04: Nhân viên mới vào làm giữa tháng (HireDate = 17/08/2026) -> tự động tính công tỷ lệ 13/26
    // =========================================================================
    [Fact]
    public async Task Profile04_MidMonthHire_ShouldProrateWorkdaysAutomaticallyWhenNoMonthlySummary()
    {
        // Tháng 08/2026 (T7 01/08 -> T2 31/08) có 26 ngày làm việc (trừ 5 Chủ nhật).
        // NV nhận việc ngày 17/08/2026 (Thứ Hai): làm 17/08..31/08 = 15 ngày lịch - 2 CN (23, 30) = 13 ngày công -> 13/26 công chuẩn.
        // LCB = 20.000.000đ -> ActualBase = 10.000.000đ, BH NLĐ (10.5% * 20tr) = 2.100.000đ -> TNCT 7.900.000 (< 15.5tr -> Thuế = 0) -> Net = 7.900.000đ.
        await using var db = await CreateSeededDbContextAsync();
        var user = new FakeCurrentUserService();
        var (u1, _, pos) = CreateBasicOrg();
        db.OrganizationUnits.Add(u1);
        db.Positions.Add(pos);
        db.Employees.Add(new Employee
        {
            Id = 4, EmployeeCode = "VNM-CL6-04", FullName = "Phạm Văn Mới",
            OrganizationUnitId = 1, PositionId = 1, BaseSalary = 20_000_000m,
            HireDate = new DateOnly(2026, 8, 17), WorkStatus = "Official", IsUnionMember = true, WageRegion = 1
        });
        await db.SaveChangesAsync();

        var periodId = await new CreatePayrollPeriodCommandHandler(db)
            .Handle(new CreatePayrollPeriodCommand(2026, 8, 26m), CancellationToken.None);

        await new CalculateMonthlyPayrollCommandHandler(db, user)
            .Handle(new CalculateMonthlyPayrollCommand(periodId, RequireLockedAttendance: false), CancellationToken.None);

        var rec = await db.PayrollRecords.FirstAsync(r => r.PayrollPeriodId == periodId && r.EmployeeId == 4);
        rec.PayableWorkdays.Should().Be(13m);
        rec.ActualBaseSalary.Should().Be(10_000_000m);
        rec.TotalEmployeeInsurance.Should().Be(2_100_000m);
        rec.PersonalIncomeTax.Should().Be(0m);
        rec.NetSalary.Should().Be(7_900_000m);
    }

    // =========================================================================
    // HỒ SƠ 05: Nghỉ việc giữa tháng (TerminationDate = 15/08/2026) + Thanh toán phép năm tồn
    // =========================================================================
    [Fact]
    public async Task Profile05_MidMonthTerminationWithUnusedLeavePayout_ShouldProrateAndAddPayout()
    {
        // Làm 01/08..15/08/2026: 13 ngày công (trừ 2 CN 02, 09) trên 26 công chuẩn -> 13/26.
        // LCB = 26.000.000đ -> ActualBase = 13.000.000đ + Phép năm tồn = 4.000.000đ -> Gross = 17.000.000đ.
        // BH NLĐ (10.5% * 26tr) = 2.730.000đ -> TNCT = 14.270.000đ (< 15.5tr -> Thuế = 0) -> Net = 14.270.000đ.
        await using var db = await CreateSeededDbContextAsync();
        var user = new FakeCurrentUserService();
        var (u1, _, pos) = CreateBasicOrg();
        db.OrganizationUnits.Add(u1);
        db.Positions.Add(pos);
        db.Employees.Add(new Employee
        {
            Id = 5, EmployeeCode = "VNM-CL6-05", FullName = "Lê Thị Nghỉ",
            OrganizationUnitId = 1, PositionId = 1, BaseSalary = 26_000_000m,
            HireDate = new DateOnly(2023, 1, 1), TerminationDate = new DateOnly(2026, 8, 15),
            WorkStatus = "Resigned", IsUnionMember = true, WageRegion = 1
        });
        db.TerminationLeaveSettlements.Add(new TerminationLeaveSettlement
        {
            EmployeeId = 5, SettlementYear = 2026, TerminationDate = new DateOnly(2026, 8, 15),
            UnusedPayableDays = 4m, BaseSalaryUsed = 26_000_000m, StandardWorkdaysUsed = 26m, PayoutAmount = 4_000_000m,
            TargetPayrollYear = 2026, TargetPayrollMonth = 8, Status = "PushedToPayroll"
        });
        await db.SaveChangesAsync();

        var periodId = await new CreatePayrollPeriodCommandHandler(db)
            .Handle(new CreatePayrollPeriodCommand(2026, 8, 26m), CancellationToken.None);

        await new CalculateMonthlyPayrollCommandHandler(db, user)
            .Handle(new CalculateMonthlyPayrollCommand(periodId, RequireLockedAttendance: false), CancellationToken.None);

        var rec = await db.PayrollRecords.FirstAsync(r => r.PayrollPeriodId == periodId && r.EmployeeId == 5);
        rec.PayableWorkdays.Should().Be(13m);
        rec.ActualBaseSalary.Should().Be(13_000_000m);
        rec.UnusedLeavePayout.Should().Be(4_000_000m);
        rec.GrossIncome.Should().Be(17_000_000m);
        rec.TotalEmployeeInsurance.Should().Be(2_730_000m);
        rec.PersonalIncomeTax.Should().Be(0m);
        rec.NetSalary.Should().Be(14_270_000m);
    }

    // =========================================================================
    // HỒ SƠ 06: Thử việc hưởng 85% mức lương chính thức
    // =========================================================================
    [Fact]
    public async Task Profile06_Probation85Percent_ShouldApplyProbationFactorEndToEnd()
    {
        // LCB = 20.000.000đ, Thử việc 85% -> Lương thực tế 26/26 công = 17.000.000đ, BH = 17.000.000 * 10.5% = 1.785.000đ.
        // TNCT = 15.215.000đ (< 15.5tr -> Thuế = 0) -> Net = 15.215.000đ.
        await using var db = await CreateSeededDbContextAsync();
        var user = new FakeCurrentUserService();
        var (u1, _, pos) = CreateBasicOrg();
        db.OrganizationUnits.Add(u1);
        db.Positions.Add(pos);
        db.Employees.Add(new Employee
        {
            Id = 6, EmployeeCode = "VNM-CL6-06", FullName = "Ngô Thử Việc",
            OrganizationUnitId = 1, PositionId = 1, BaseSalary = 20_000_000m,
            HireDate = new DateOnly(2026, 8, 1), WorkStatus = "Probation", IsUnionMember = false, WageRegion = 1
        });
        db.MonthlyAttendanceSummaries.Add(new MonthlyAttendanceSummary
        {
            EmployeeId = 6, Year = 2026, Month = 8, StandardWorkdays = 26m, TotalPayableWorkdays = 26m, IsLocked = true
        });
        await db.SaveChangesAsync();

        await new SaveEmployeeSalaryProfileCommandHandler(db, user).Handle(new SaveEmployeeSalaryProfileCommand(
            EmployeeId: 6, EffectiveFrom: new DateOnly(2026, 8, 1),
            BaseSalary: 20_000_000m, InsuranceSalary: 17_000_000m,
            SalaryStepId: null, SalaryTemplateId: null, PayMode: "Monthly", UnitRate: 0m,
            IsProbation: true, ProbationRatePercent: 85m, PositionAllowance: 0m, OtherAllowance: 0m,
            IsUnionMember: false), CancellationToken.None);

        var periodId = await new CreatePayrollPeriodCommandHandler(db)
            .Handle(new CreatePayrollPeriodCommand(2026, 8, 26m), CancellationToken.None);

        await new CalculateMonthlyPayrollCommandHandler(db, user)
            .Handle(new CalculateMonthlyPayrollCommand(periodId, RequireLockedAttendance: true), CancellationToken.None);

        var rec = await db.PayrollRecords.FirstAsync(r => r.PayrollPeriodId == periodId && r.EmployeeId == 6);
        rec.ActualBaseSalary.Should().Be(17_000_000m);
        rec.TotalEmployeeInsurance.Should().Be(1_785_000m);
        rec.PersonalIncomeTax.Should().Be(0m);
        rec.NetSalary.Should().Be(15_215_000m);
    }

    // =========================================================================
    // HỒ SƠ 07: OT Ngày & Đêm đa khung (150/210/200/270/300/390) + Ca đêm +30% + Miễn thuế phần trội TT 111/2013
    // =========================================================================
    [Fact]
    public async Task Profile07_OvertimeDayAndNightAndRegularNight_ShouldComputeExactMultipliersAndTaxExemption()
    {
        // Tính tay: LCB = 20.800.000đ, 26 công -> Đơn giá giờ = 20.800.000 / 208 = 100.000 đ/h.
        // - OT ngày thường: 2h * 150% = 300.000đ; 2h đêm thường (210%) = 420.000đ
        // - OT nghỉ tuần:   2h * 200% = 400.000đ; 2h đêm tuần (270%) = 540.000đ
        // - OT lễ/tết:      1h * 300% = 300.000đ; 1h đêm lễ (390%) = 390.000đ
        // - Giờ làm ca đêm thường: 10h * 30% = 300.000đ
        // -> Tổng tiền OT & ca đêm = 300 + 420 + 400 + 540 + 300 + 390 + 300 = 2.650.000 đ
        // -> Phần thưởng thêm do làm đêm (OtNightExtraPay) = (420-300) + (540-400) + (390-300) + 300 = 650.000 đ
        // -> Tổng giờ OT = 10h -> Lương giờ bình thường 100% = 1.000.000đ -> Phần trội OT miễn thuế = (2.350.000 - 1.000.000) + 300.000 ca đêm = 1.650.000 đ!
        // -> Gross = 20.800.000 + 2.650.000 = 23.450.000 đ
        // -> BH NLĐ (10.5% * 20.8tr) = 2.184.000 đ
        // -> TNCT (sau trừ BH & miễn OT/đêm) = 23.450.000 - 2.184.000 - 1.650.000 = 19.616.000 đ
        // -> TNTT = 19.616.000 - 15.500.000 = 4.116.000 đ -> Thuế TNCN (5%) = 205.800 đ
        // -> Thực lĩnh = 23.450.000 - 2.184.000 - 205.800 = 21.060.200 đ
        await using var db = await CreateSeededDbContextAsync();
        var user = new FakeCurrentUserService();
        var (u1, _, pos) = CreateBasicOrg();
        db.OrganizationUnits.Add(u1);
        db.Positions.Add(pos);
        db.Employees.Add(new Employee
        {
            Id = 7, EmployeeCode = "VNM-CL6-07", FullName = "Đặng Ca Đêm",
            OrganizationUnitId = 1, PositionId = 1, BaseSalary = 20_800_000m,
            HireDate = new DateOnly(2024, 1, 1), WorkStatus = "Official", IsUnionMember = true, WageRegion = 1
        });
        db.MonthlyAttendanceSummaries.Add(new MonthlyAttendanceSummary
        {
            EmployeeId = 7, Year = 2026, Month = 8, StandardWorkdays = 26m, TotalPayableWorkdays = 26m,
            TotalOtHours150 = 4m, TotalOtHours150Day = 2m, TotalOtHours150Night = 2m,
            TotalOtHours200 = 4m, TotalOtHours200Day = 2m, TotalOtHours200Night = 2m,
            TotalOtHours300 = 2m, TotalOtHours300Day = 1m, TotalOtHours300Night = 1m,
            TotalNightHours = 10m, IsLocked = true
        });
        await db.SaveChangesAsync();

        var periodId = await new CreatePayrollPeriodCommandHandler(db)
            .Handle(new CreatePayrollPeriodCommand(2026, 8, 26m), CancellationToken.None);

        await new CalculateMonthlyPayrollCommandHandler(db, user)
            .Handle(new CalculateMonthlyPayrollCommand(periodId, RequireLockedAttendance: true), CancellationToken.None);

        var rec = await db.PayrollRecords.FirstAsync(r => r.PayrollPeriodId == periodId && r.EmployeeId == 7);
        rec.OvertimePay.Should().Be(2_650_000m);
        rec.OtNightExtraPay.Should().Be(650_000m);
        rec.OtTaxExemptAmount.Should().Be(1_650_000m);
        rec.GrossIncome.Should().Be(23_450_000m);
        rec.TotalEmployeeInsurance.Should().Be(2_184_000m);
        rec.PersonalIncomeTax.Should().Be(205_800m);
        rec.NetSalary.Should().Be(21_060_200m);
    }

    // =========================================================================
    // HỒ SƠ 08: Hợp đồng < 3 tháng KHÔNG có cam kết 08/CK-TNCN (>= 2tr khấu trừ 10%; < 2tr = 0%)
    // =========================================================================
    [Fact]
    public async Task Profile08_ShortTermUnder3Months_WithoutCommitment08_ShouldApply10PercentAbove2M()
    {
        await using var db = await CreateSeededDbContextAsync();
        var ctx = await VietnamPayrollEngine.ResolveStatutoryContextAsync(db, new DateOnly(2026, 8, 31));

        // 08A: Gross = 10.000.000 đ, không BH, >= 2.000.000 đ -> Thuế 10% = 1.000.000 đ -> Net = 9.000.000 đ
        var high = VietnamPayrollEngine.Calculate(new PayrollCalculationInput(
            ContractBaseSalary: 10_000_000m, PositionAllowance: 0m, OtherAllowance: 0m,
            StandardWorkdays: 26m, PayableWorkdays: 26m, OtHours150: 0m, OtHours200: 0m, OtHours300: 0m,
            KpiBonus: 0m, DisciplineDeduction: 0m, DependentCount: 2, IsUnionMember: false,
            InsuranceContractSalaryOverride: 0m,
            TaxResidencyMode: "ResidentShortTerm",
            HasPitCommitment08: false), ctx);

        high.PersonalIncomeTax.Should().Be(1_000_000m);
        high.UsedCommitment08.Should().BeFalse();
        high.NetSalary.Should().Be(9_000_000m);

        // 08B: Gross = 1.800.000 đ < 2.000.000 đ -> Thuế = 0 đ -> Net = 1.800.000 đ
        var low = VietnamPayrollEngine.Calculate(new PayrollCalculationInput(
            ContractBaseSalary: 1_800_000m, PositionAllowance: 0m, OtherAllowance: 0m,
            StandardWorkdays: 26m, PayableWorkdays: 26m, OtHours150: 0m, OtHours200: 0m, OtHours300: 0m,
            KpiBonus: 0m, DisciplineDeduction: 0m, DependentCount: 0, IsUnionMember: false,
            InsuranceContractSalaryOverride: 0m,
            TaxResidencyMode: "ResidentShortTerm",
            HasPitCommitment08: false), ctx);

        low.PersonalIncomeTax.Should().Be(0m);
        low.NetSalary.Should().Be(1_800_000m);
    }

    // =========================================================================
    // HỒ SƠ 09: Hợp đồng < 3 tháng CÓ bản cam kết mẫu 08/CK-TNCN -> tạm khấu trừ 0đ
    // =========================================================================
    [Fact]
    public async Task Profile09_ShortTermWithCommitment08_ShouldExempt10PercentWithholding()
    {
        // Gross = 12.000.000đ, HĐ dịch vụ/ngắn hạn không BHXH, có mẫu 08/CK-TNCN -> Thuế = 0đ, Thực lĩnh = 12.000.000đ.
        await using var db = await CreateSeededDbContextAsync();
        var user = new FakeCurrentUserService();
        var (u1, _, pos) = CreateBasicOrg();
        db.OrganizationUnits.Add(u1);
        db.Positions.Add(pos);
        db.Employees.Add(new Employee
        {
            Id = 9, EmployeeCode = "VNM-CL6-09", FullName = "Nguyễn Thời Vụ 08",
            OrganizationUnitId = 1, PositionId = 1, BaseSalary = 12_000_000m,
            HireDate = new DateOnly(2026, 8, 1), WorkStatus = "Official", IsUnionMember = false, WageRegion = 1
        });
        db.MonthlyAttendanceSummaries.Add(new MonthlyAttendanceSummary
        {
            EmployeeId = 9, Year = 2026, Month = 8, StandardWorkdays = 26m, TotalPayableWorkdays = 26m, IsLocked = true
        });
        await db.SaveChangesAsync();

        await new SaveEmployeeSalaryProfileCommandHandler(db, user).Handle(new SaveEmployeeSalaryProfileCommand(
            EmployeeId: 9, EffectiveFrom: new DateOnly(2026, 8, 1),
            BaseSalary: 12_000_000m, InsuranceSalary: 0m, SalaryStepId: null, SalaryTemplateId: null,
            PayMode: "Monthly", UnitRate: 0m, IsProbation: false, ProbationRatePercent: 100m,
            PositionAllowance: 0m, OtherAllowance: 0m, IsUnionMember: false,
            TaxResidencyMode: "ResidentShortTerm", HasPitCommitment08: true, Commitment08Number: "08/CK-2026-09"), CancellationToken.None);

        // Chú ý: với ResidentShortTerm có InsuranceSalary = 0 trên hồ sơ thử nghiệm muốn miễn BH hoàn toàn -> truyền thẳng hoặc test cả Profile 09 & Profile 09B
        var ctx = await VietnamPayrollEngine.ResolveStatutoryContextAsync(db, new DateOnly(2026, 8, 31));
        var directRes = VietnamPayrollEngine.Calculate(new PayrollCalculationInput(
            ContractBaseSalary: 12_000_000m, PositionAllowance: 0m, OtherAllowance: 0m,
            StandardWorkdays: 26m, PayableWorkdays: 26m, OtHours150: 0m, OtHours200: 0m, OtHours300: 0m,
            KpiBonus: 0m, DisciplineDeduction: 0m, DependentCount: 0, IsUnionMember: false,
            InsuranceContractSalaryOverride: 0m,
            TaxResidencyMode: "ResidentShortTerm",
            HasPitCommitment08: true), ctx);

        directRes.UsedCommitment08.Should().BeTrue();
        directRes.PersonalIncomeTax.Should().Be(0m);
        directRes.NetSalary.Should().Be(12_000_000m);

        var periodId = await new CreatePayrollPeriodCommandHandler(db)
            .Handle(new CreatePayrollPeriodCommand(2026, 8, 26m), CancellationToken.None);
        await new CalculateMonthlyPayrollCommandHandler(db, user)
            .Handle(new CalculateMonthlyPayrollCommand(periodId, RequireLockedAttendance: true), CancellationToken.None);

        var rec = await db.PayrollRecords.FirstAsync(r => r.PayrollPeriodId == periodId && r.EmployeeId == 9);
        rec.UsedCommitment08.Should().BeTrue();
        rec.PersonalIncomeTax.Should().Be(0m);
        rec.TaxResidencyMode.Should().Be("ResidentShortTerm");
    }

    // =========================================================================
    // HỒ SƠ 10: Lao động nước ngoài không cư trú (Non-Resident: 20% flat PIT, 0 GTGC, 0 BHXH VN)
    // =========================================================================
    [Fact]
    public async Task Profile10_ForeignNonResident_ShouldApplyFlat20PercentPitAndZeroPersonalDeductions()
    {
        // Tính tay:
        // - LCB = 80.000.000 đ, PCCV = 10.000.000 đ -> Gross = 90.000.000 đ
        // - Không cư trú: không trừ BHXH VN (0đ), không giảm trừ gia cảnh (0đ), không phụ thuộc (0đ)
        // - Thuế TNCN = 90.000.000 * 20% = 18.000.000 đ
        // - Thực lĩnh = 90.000.000 - 18.000.000 = 72.000.000 đ
        await using var db = await CreateSeededDbContextAsync();
        var user = new FakeCurrentUserService();
        var (u1, _, pos) = CreateBasicOrg();
        db.OrganizationUnits.Add(u1);
        db.Positions.Add(pos);
        db.Employees.Add(new Employee
        {
            Id = 10, EmployeeCode = "VNM-EXP-10", FullName = "John Expert",
            OrganizationUnitId = 1, PositionId = 1, BaseSalary = 80_000_000m,
            HireDate = new DateOnly(2026, 8, 1), WorkStatus = "Official", IsUnionMember = false, WageRegion = 1
        });
        db.MonthlyAttendanceSummaries.Add(new MonthlyAttendanceSummary
        {
            EmployeeId = 10, Year = 2026, Month = 8, StandardWorkdays = 26m, TotalPayableWorkdays = 26m, IsLocked = true
        });
        await db.SaveChangesAsync();

        await new SaveEmployeeSalaryProfileCommandHandler(db, user).Handle(new SaveEmployeeSalaryProfileCommand(
            EmployeeId: 10, EffectiveFrom: new DateOnly(2026, 8, 1),
            BaseSalary: 80_000_000m, InsuranceSalary: 0m, SalaryStepId: null, SalaryTemplateId: null,
            PayMode: "Monthly", UnitRate: 0m, IsProbation: false, ProbationRatePercent: 100m,
            PositionAllowance: 10_000_000m, OtherAllowance: 0m, IsUnionMember: false,
            TaxResidencyMode: "NonResident"), CancellationToken.None);

        var periodId = await new CreatePayrollPeriodCommandHandler(db)
            .Handle(new CreatePayrollPeriodCommand(2026, 8, 26m), CancellationToken.None);
        await new CalculateMonthlyPayrollCommandHandler(db, user)
            .Handle(new CalculateMonthlyPayrollCommand(periodId, RequireLockedAttendance: true), CancellationToken.None);

        var rec = await db.PayrollRecords.FirstAsync(r => r.PayrollPeriodId == periodId && r.EmployeeId == 10);
        rec.GrossIncome.Should().Be(90_000_000m);
        rec.TotalEmployeeInsurance.Should().Be(0m);
        rec.SelfDeductionAmount.Should().Be(0m);
        rec.PersonalIncomeTax.Should().Be(18_000_000m);
        rec.NetSalary.Should().Be(72_000_000m);
    }

    // =========================================================================
    // HỒ SƠ 11: Hợp đồng Lương Net → Quy đổi Gross bằng Binary Search sai số <= 1đ
    // =========================================================================
    [Fact]
    public async Task Profile11_NetToGrossContract_ShouldConvergeWithin1VndAndCalculateAccurately()
    {
        // Mục tiêu: Net = 30.000.000 đ, 1 người phụ thuộc (GTGC = 15.5tr + 6.2tr = 21.7tr), Vùng I, BH trên lương quy đổi.
        // Xác minh qua cả SolveGrossFromNet VÀ chạy qua CalculateMonthlyPayrollCommandHandler end-to-end!
        await using var db = await CreateSeededDbContextAsync();
        var user = new FakeCurrentUserService();
        var (u1, _, pos) = CreateBasicOrg();
        db.OrganizationUnits.Add(u1);
        db.Positions.Add(pos);
        db.Employees.Add(new Employee
        {
            Id = 11, EmployeeCode = "VNM-NET-11", FullName = "Trương Lương Net",
            OrganizationUnitId = 1, PositionId = 1, BaseSalary = 30_000_000m,
            HireDate = new DateOnly(2025, 1, 1), WorkStatus = "Official", IsUnionMember = true, WageRegion = 1
        });
        db.EmployeeDependents.Add(new EmployeeDependent { EmployeeId = 11, FullName = "Con Net", IsTaxDeductible = true });
        db.MonthlyAttendanceSummaries.Add(new MonthlyAttendanceSummary
        {
            EmployeeId = 11, Year = 2026, Month = 8, StandardWorkdays = 26m, TotalPayableWorkdays = 26m, IsLocked = true
        });
        await db.SaveChangesAsync();

        await new SaveEmployeeSalaryProfileCommandHandler(db, user).Handle(new SaveEmployeeSalaryProfileCommand(
            EmployeeId: 11, EffectiveFrom: new DateOnly(2026, 8, 1),
            BaseSalary: 30_000_000m, InsuranceSalary: 0m, SalaryStepId: null, SalaryTemplateId: null,
            PayMode: "Monthly", UnitRate: 0m, IsProbation: false, ProbationRatePercent: 100m,
            PositionAllowance: 0m, OtherAllowance: 0m, IsUnionMember: true,
            IsNetContract: true, TargetNetSalary: 30_000_000m), CancellationToken.None);

        var solved = await new SolveNetToGrossQueryHandler(db)
            .Handle(new SolveNetToGrossQuery(new NetToGrossInput(30_000_000m, DependentCount: 1, WageRegion: 1), new DateOnly(2026, 8, 31)), CancellationToken.None);

        solved.Converged.Should().BeTrue();
        Math.Abs(solved.ResidualDifference).Should().BeLessThanOrEqualTo(1m);
        solved.SolvedContractBaseSalary.Should().BeGreaterThan(33_000_000m);

        var periodId = await new CreatePayrollPeriodCommandHandler(db)
            .Handle(new CreatePayrollPeriodCommand(2026, 8, 26m), CancellationToken.None);
        await new CalculateMonthlyPayrollCommandHandler(db, user)
            .Handle(new CalculateMonthlyPayrollCommand(periodId, RequireLockedAttendance: true), CancellationToken.None);

        var rec = await db.PayrollRecords.FirstAsync(r => r.PayrollPeriodId == periodId && r.EmployeeId == 11);
        rec.IsNetToGross.Should().BeTrue();
        rec.TargetNetSalary.Should().Be(30_000_000m);
        Math.Abs(rec.NetSalary - 30_000_000m).Should().BeLessThanOrEqualTo(1m);
        rec.ContractBaseSalary.Should().Be(solved.SolvedContractBaseSalary);
    }

    // =========================================================================
    // HỒ SƠ 12: Tạm ứng + Trả góp khoản vay + Phạt kỷ luật + Truy lĩnh & Truy thu từ kỳ đã khóa (Idempotent khi tính lại)
    // =========================================================================
    [Fact]
    public async Task Profile12_AdvancesLoansFinesAndPostLockRetro_ShouldDeductAndRecalculateIdempotently()
    {
        // Tính tay kỳ 08/2026:
        // - LCB = 20.000.000đ + Truy lĩnh từ kỳ 07/2026 đã khóa = 1.500.000đ -> Gross = 21.500.000 đ
        // - BH NLĐ = 20.000.000 * 10.5% = 2.100.000 đ
        // - TNCT = 21.500.000 - 2.100.000 = 19.400.000 đ
        // - TNTT = 19.400.000 - 15.500.000 = 3.900.000 đ -> Thuế TNCN (5%) = 195.000 đ
        // - Khấu trừ sau thuế: Tạm ứng = 3.000.000 + Trả góp vay (6tr / 3 tháng) = 2.000.000 + Phạt kỷ luật = 400.000 + Truy thu = 500.000 = 5.900.000 đ
        // - Thực lĩnh = 21.500.000 - 2.100.000 - 195.000 - 5.900.000 = 13.305.000 đ
        await using var db = await CreateSeededDbContextAsync();
        var user = new FakeCurrentUserService();
        var (u1, _, pos) = CreateBasicOrg();
        db.OrganizationUnits.Add(u1);
        db.Positions.Add(pos);
        db.Employees.Add(new Employee
        {
            Id = 12, EmployeeCode = "VNM-CL6-12", FullName = "Bùi Thanh Toán",
            OrganizationUnitId = 1, PositionId = 1, BaseSalary = 20_000_000m,
            HireDate = new DateOnly(2024, 1, 1), WorkStatus = "Official", IsUnionMember = true, WageRegion = 1
        });
        var catFine = new RewardDisciplineCategory { Id = 1, CategoryCode = "DISC", CategoryName = "Kỷ luật nội quy", CategoryType = "Discipline" };
        db.RewardDisciplineCategories.Add(catFine);
        db.RewardDisciplines.Add(new RewardDiscipline
        {
            DecisionNumber = "KL-08/26", EmployeeId = 12, CategoryId = 1, Type = "Discipline",
            DecisionDate = new DateOnly(2026, 8, 10), EffectiveDate = new DateOnly(2026, 8, 10),
            Amount = 400_000m, Reason = "Làm hỏng vật tư", Status = "Approved"
        });
        db.MonthlyAttendanceSummaries.Add(new MonthlyAttendanceSummary
        {
            EmployeeId = 12, Year = 2026, Month = 8, StandardWorkdays = 26m, TotalPayableWorkdays = 26m, IsLocked = true
        });
        var lockedJuly = new PayrollPeriod
        {
            PeriodCode = "PAY-2026-07", Year = 2026, Month = 7, PeriodTitle = "Kỳ 07/2026",
            StartDate = new DateOnly(2026, 7, 1), EndDate = new DateOnly(2026, 7, 31),
            StandardWorkdays = 26m, Status = "Locked", IsLocked = true
        };
        db.PayrollPeriods.Add(lockedJuly);
        await db.SaveChangesAsync();

        await new CreateSalaryAdvanceCommandHandler(db, user).Handle(
            new CreateSalaryAdvanceCommand(12, new DateOnly(2026, 8, 15), 2026, 8, 3_000_000m, "Ứng lương giữa tháng"), CancellationToken.None);

        await new CreateEmployeeLoanCommandHandler(db, user).Handle(
            new CreateEmployeeLoanCommand(12, new DateOnly(2026, 8, 1), 6_000_000m, 3, 2026, 8, "Vay hỗ trợ"), CancellationToken.None);

        await new CreatePayrollAdjustmentCommandHandler(db, user).Handle(
            new CreatePayrollAdjustmentCommand(12, "RetroEarning", 2026, 8, 1_500_000m, "Truy lĩnh công sót tháng 07", lockedJuly.Id), CancellationToken.None);

        await new CreatePayrollAdjustmentCommandHandler(db, user).Handle(
            new CreatePayrollAdjustmentCommand(12, "RetroDeduction", 2026, 8, 500_000m, "Truy thu phụ cấp chi thừa tháng 07", lockedJuly.Id), CancellationToken.None);

        var periodId = await new CreatePayrollPeriodCommandHandler(db)
            .Handle(new CreatePayrollPeriodCommand(2026, 8, 26m), CancellationToken.None);

        var calcHandler = new CalculateMonthlyPayrollCommandHandler(db, user);
        await calcHandler.Handle(new CalculateMonthlyPayrollCommand(periodId, RequireLockedAttendance: true), CancellationToken.None);
        // Chạy tính lại lần 2 để kiểm tra idempotency
        await calcHandler.Handle(new CalculateMonthlyPayrollCommand(periodId, Notes: "Recalc check idempotency", RequireLockedAttendance: true), CancellationToken.None);

        var rec = await db.PayrollRecords.FirstAsync(r => r.PayrollPeriodId == periodId && r.EmployeeId == 12);
        rec.RetroEarningAmount.Should().Be(1_500_000m);
        rec.GrossIncome.Should().Be(21_500_000m);
        rec.TotalEmployeeInsurance.Should().Be(2_100_000m);
        rec.PersonalIncomeTax.Should().Be(195_000m);
        rec.SalaryAdvanceDeduction.Should().Be(3_000_000m);
        rec.LoanInstallmentDeduction.Should().Be(2_000_000m);
        rec.DisciplineDeduction.Should().Be(400_000m);
        rec.RetroDeductionAmount.Should().Be(500_000m);
        rec.NetSalary.Should().Be(13_305_000m);

        (await db.SalaryAdvances.FirstAsync(a => a.EmployeeId == 12)).Status.Should().Be("Deducted");
        (await db.EmployeeLoanInstallments.FirstAsync(i => i.EmployeeId == 12 && i.InstallmentNumber == 1)).Status.Should().Be("Deducted");
        (await db.EmployeeLoanInstallments.FirstAsync(i => i.EmployeeId == 12 && i.InstallmentNumber == 2)).Status.Should().Be("Scheduled");
    }

    // =========================================================================
    // HỒ SƠ 13: Thưởng KPI tự động từ EmployeeKpiEvaluation + Hoa hồng + Đoàn phí CĐ NLĐ (1% trần 10% LCS) + BHTNLĐ 0.5%
    // =========================================================================
    [Fact]
    public async Task Profile13_KpiEvaluationAndCommissionAndCappedEmployeeUnionFee_ShouldComputeExactAmounts()
    {
        // Tính tay kỳ 08/2026 (cấu hình EmployeeUnionFeeRate = 1%, EmployeeUnionFeeCapMultiplier = 10% * LCS 2.530.000 = 253.000đ):
        // - LCB = 30.000.000 đ; KPI_BONUS_RATE_OF_BASE = 0.15, BonusMultiplier KPI = 1.2 -> KPI Bonus = 30.000.000 * 0.15 * 1.2 = 5.400.000 đ
        // - Hoa hồng (Commission) = 4.600.000 đ -> Gross = 30.000.000 + 5.400.000 + 4.600.000 = 40.000.000 đ
        // - BH NLĐ = 30.000.000 * 10.5% = 3.150.000 đ
        // - Đoàn phí NLĐ = min(1% * 30.000.000 = 300.000, trần 10% * 2.530.000 = 253.000) = 253.000 đ
        // - TNCT = 40.000.000 - 3.150.000 = 36.850.000 đ -> TNTT = 36.850.000 - 15.500.000 = 21.350.000 đ
        // - Thuế lũy tiến: B1 (10tr * 5% = 500.000) + B2 (11.350.000 * 10% = 1.135.000) = 1.635.000 đ
        // - Thực lĩnh = 40.000.000 - 3.150.000 - 1.635.000 - 253.000 = 34.962.000 đ
        // - BHTNLĐ DN = 0.5% * 30.000.000 = 150.000 đ; Tổng CP DN = 40.000.000 + 5.250.000 + 900.000 + 300.000 + 150.000 + 600.000 = 47.200.000 đ
        await using var db = await CreateSeededDbContextAsync();
        var user = new FakeCurrentUserService();
        var augParam = await db.PayrollStatutoryParameters.FirstAsync(p => p.EffectiveFrom == new DateOnly(2026, 7, 1));
        augParam.EmployeeUnionFeeRate = 0.01m;

        var (u1, _, pos) = CreateBasicOrg();
        db.OrganizationUnits.Add(u1);
        db.Positions.Add(pos);
        db.Employees.Add(new Employee
        {
            Id = 13, EmployeeCode = "VNM-CL6-13", FullName = "Hoàng KPI Hoa Hồng",
            OrganizationUnitId = 1, PositionId = 1, BaseSalary = 30_000_000m,
            HireDate = new DateOnly(2022, 1, 1), WorkStatus = "Official", IsUnionMember = true, WageRegion = 1
        });
        db.MonthlyAttendanceSummaries.Add(new MonthlyAttendanceSummary
        {
            EmployeeId = 13, Year = 2026, Month = 8, StandardWorkdays = 26m, TotalPayableWorkdays = 26m, IsLocked = true
        });
        var cycle = new EvaluationCycle
        {
            Id = 1, CycleCode = "KPI-2026-08", CycleName = "KPI Tháng 08/2026", CycleType = "Monthly",
            Year = 2026, PeriodNumber = 8, StartDate = new DateOnly(2026, 8, 1), EndDate = new DateOnly(2026, 8, 31),
            SelfEvalDeadline = new DateOnly(2026, 8, 28), ManagerEvalDeadline = new DateOnly(2026, 8, 31), Status = "Closed"
        };
        db.EvaluationCycles.Add(cycle);
        db.EmployeeKpiEvaluations.Add(new EmployeeKpiEvaluation
        {
            EvaluationCycleId = 1, EmployeeId = 13, FinalScore = 95m, RatingGrade = "A+", BonusMultiplier = 1.20m,
            Status = "Approved", ApprovedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        await new CreatePayrollAdjustmentCommandHandler(db, user).Handle(
            new CreatePayrollAdjustmentCommand(13, "Commission", 2026, 8, 4_600_000m, "Hoa hồng doanh số T08"), CancellationToken.None);

        var periodId = await new CreatePayrollPeriodCommandHandler(db)
            .Handle(new CreatePayrollPeriodCommand(2026, 8, 26m), CancellationToken.None);

        await new CalculateMonthlyPayrollCommandHandler(db, user)
            .Handle(new CalculateMonthlyPayrollCommand(periodId, RequireLockedAttendance: true), CancellationToken.None);

        var rec = await db.PayrollRecords.FirstAsync(r => r.PayrollPeriodId == periodId && r.EmployeeId == 13);
        rec.KpiBonus.Should().Be(5_400_000m);
        rec.CommissionBonus.Should().Be(4_600_000m);
        rec.GrossIncome.Should().Be(40_000_000m);
        rec.EmployeeUnionFee.Should().Be(253_000m);
        rec.CompanyOccupationalAccidentInsurance.Should().Be(150_000m);
        rec.PersonalIncomeTax.Should().Be(1_635_000m);
        rec.NetSalary.Should().Be(34_962_000m);
        rec.TotalCompanyCost.Should().Be(47_200_000m);
    }

    // =========================================================================
    // HỒ SƠ 14: Kỳ chi Thưởng Tháng 13 riêng biệt cùng tháng 12 (tính thuế phần tăng thêm không giảm trừ trùng)
    // =========================================================================
    [Fact]
    public async Task Profile14_SeparateBonus13PeriodInSameMonth_ShouldComputeIncrementalPitAndZeroInsurance()
    {
        // Tính tay tháng 12/2026:
        // 1. Kỳ lương Monthly 12/2026: LCB = 26.000.000 đ -> BH NLĐ = 2.730.000 đ -> TNTT = 26.000.000 - 2.730.000 - 15.500.000 = 7.770.000 đ
        //    -> Thuế tháng = Round(7.770.000 * 5%, 0) = 388.500 đ
        // 2. Đợt chi Thưởng Tháng 13 (PeriodType = "Bonus13") cùng tháng 12/2026:
        //    - Đủ 12 tháng trong năm -> Thưởng T13 = 26.000.000 đ; Lương công = 0đ; BHXH = 0đ; GTGC bổ sung = 0đ
        //    - Tổng TNTT gộp = 7.770.000 + 26.000.000 = 33.770.000 đ
        //    - Thuế trên tổng TNTT = B1 (10tr*5% = 500.000) + B2 (20tr*10% = 2.000.000) + B3 (3.770.000*20% = 754.000) = 3.254.000 đ
        //    -> Thuế phải nộp cho kỳ Bonus13 = 3.254.000 - 388.500 = 2.865.500 đ
        //    -> Thực lĩnh Bonus13 = 26.000.000 - 2.865.500 = 23.134.500 đ
        await using var db = await CreateSeededDbContextAsync();
        var user = new FakeCurrentUserService();
        var (u1, _, pos) = CreateBasicOrg();
        db.OrganizationUnits.Add(u1);
        db.Positions.Add(pos);
        db.Employees.Add(new Employee
        {
            Id = 14, EmployeeCode = "VNM-B13-14", FullName = "Vũ Thưởng Tết",
            OrganizationUnitId = 1, PositionId = 1, BaseSalary = 26_000_000m,
            HireDate = new DateOnly(2024, 1, 1), WorkStatus = "Official", IsUnionMember = true, WageRegion = 1
        });
        db.MonthlyAttendanceSummaries.Add(new MonthlyAttendanceSummary
        {
            EmployeeId = 14, Year = 2026, Month = 12, StandardWorkdays = 26m, TotalPayableWorkdays = 26m, IsLocked = true
        });
        await db.SaveChangesAsync();

        var monthlyId = await new CreatePayrollPeriodCommandHandler(db)
            .Handle(new CreatePayrollPeriodCommand(2026, 12, 26m, "Monthly"), CancellationToken.None);
        await new CalculateMonthlyPayrollCommandHandler(db, user)
            .Handle(new CalculateMonthlyPayrollCommand(monthlyId, RequireLockedAttendance: true), CancellationToken.None);

        var bonus13Id = await new CreatePayrollPeriodCommandHandler(db)
            .Handle(new CreatePayrollPeriodCommand(2026, 12, 26m, "Bonus13"), CancellationToken.None);
        bonus13Id.Should().NotBe(monthlyId);

        await new CalculateMonthlyPayrollCommandHandler(db, user)
            .Handle(new CalculateMonthlyPayrollCommand(bonus13Id, RequireLockedAttendance: false), CancellationToken.None);

        var bRec = await db.PayrollRecords.FirstAsync(r => r.PayrollPeriodId == bonus13Id && r.EmployeeId == 14);
        bRec.ActualBaseSalary.Should().Be(0m);
        bRec.ThirteenthMonthBonus.Should().Be(26_000_000m);
        bRec.GrossIncome.Should().Be(26_000_000m);
        bRec.TotalEmployeeInsurance.Should().Be(0m);
        bRec.SelfDeductionAmount.Should().Be(0m);
        bRec.PersonalIncomeTax.Should().Be(2_865_500m);
        bRec.NetSalary.Should().Be(23_134_500m);
    }

    // =========================================================================
    // HỒ SƠ 15: Chặn tính lương chính thức khi bảng công chưa khóa + Giả lập (0 DB writes) + So sánh >20% + Drill-down Trace
    // =========================================================================
    [Fact]
    public async Task Profile15_AttendanceLockBlock_SimulationZeroDbWrites_VarianceAlert_AndDrillDownTrace()
    {
        // - Tháng 08/2026 đã khóa công & tính: LCB = 20.000.000 đ -> Net = 17.780.000 đ
        // - Tháng 09/2026 tăng lương lên 30.000.000 đ (chênh lệch > 20%), bảng công CHƯA KHÓA (IsLocked = false):
        //   1) CalculateMonthlyPayrollCommand(RequireLockedAttendance: true) -> PHẢI NÉM InvalidOperationException
        //   2) SimulatePayrollPeriodQueryHandler -> chạy đầy đủ, cảnh báo biến động >20%, KHÔNG ghi dòng nào vào PayrollRecords!
        //   3) Khóa công T09/2026 -> Tính chính thức thành công -> RecordTrace trả về chuỗi step rõ công thức + biến + kết quả.
        await using var db = await CreateSeededDbContextAsync();
        var user = new FakeCurrentUserService();
        var (u1, _, pos) = CreateBasicOrg();
        db.OrganizationUnits.Add(u1);
        db.Positions.Add(pos);
        db.Employees.Add(new Employee
        {
            Id = 15, EmployeeCode = "VNM-SIM-15", FullName = "Lý Giả Lập",
            OrganizationUnitId = 1, PositionId = 1, BaseSalary = 20_000_000m,
            HireDate = new DateOnly(2024, 1, 1), WorkStatus = "Official", IsUnionMember = true, WageRegion = 1
        });
        db.MonthlyAttendanceSummaries.Add(new MonthlyAttendanceSummary
        {
            EmployeeId = 15, Year = 2026, Month = 8, StandardWorkdays = 26m, TotalPayableWorkdays = 26m, IsLocked = true
        });
        var sepAtt = new MonthlyAttendanceSummary
        {
            EmployeeId = 15, Year = 2026, Month = 9, StandardWorkdays = 26m, TotalPayableWorkdays = 26m, IsLocked = false
        };
        db.MonthlyAttendanceSummaries.Add(sepAtt);
        await db.SaveChangesAsync();

        var augId = await new CreatePayrollPeriodCommandHandler(db)
            .Handle(new CreatePayrollPeriodCommand(2026, 8, 26m), CancellationToken.None);
        await new CalculateMonthlyPayrollCommandHandler(db, user)
            .Handle(new CalculateMonthlyPayrollCommand(augId, RequireLockedAttendance: true), CancellationToken.None);

        // Tăng lương tháng 09/2026 từ 20tr -> 30tr
        await new SaveEmployeeSalaryProfileCommandHandler(db, user).Handle(new SaveEmployeeSalaryProfileCommand(
            EmployeeId: 15, EffectiveFrom: new DateOnly(2026, 9, 1),
            BaseSalary: 30_000_000m, InsuranceSalary: 30_000_000m, SalaryStepId: null, SalaryTemplateId: null,
            PayMode: "Monthly", UnitRate: 0m, IsProbation: false, ProbationRatePercent: 100m,
            PositionAllowance: 0m, OtherAllowance: 0m, IsUnionMember: true), CancellationToken.None);

        var sepId = await new CreatePayrollPeriodCommandHandler(db)
            .Handle(new CreatePayrollPeriodCommand(2026, 9, 26m), CancellationToken.None);

        // 1. Tính chính thức bị chặn vì bảng công chưa khóa
        Func<Task> actBlocked = () => new CalculateMonthlyPayrollCommandHandler(db, user)
            .Handle(new CalculateMonthlyPayrollCommand(sepId, RequireLockedAttendance: true), CancellationToken.None);
        await actBlocked.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*chưa chốt khóa*");

        // 2. Giả lập chạy lương (Simulation) thành công nhưng KHÔNG ghi vào DB
        var simResult = await new SimulatePayrollPeriodQueryHandler(db)
            .Handle(new SimulatePayrollPeriodQuery(sepId), CancellationToken.None);

        simResult.AttendanceLockStatus.IsAllLocked.Should().BeFalse();
        simResult.HighVarianceCount.Should().Be(1);
        simResult.Employees.Should().HaveCount(1);
        simResult.Employees[0].HasHighVarianceWarning.Should().BeTrue();
        simResult.Employees[0].TraceSteps.Should().NotBeEmpty();
        (await db.PayrollRecords.CountAsync(r => r.PayrollPeriodId == sepId)).Should().Be(0);

        // 3. Khóa công & tính chính thức -> kiểm tra Drill-down Trace
        sepAtt.IsLocked = true;
        await db.SaveChangesAsync();

        await new CalculateMonthlyPayrollCommandHandler(db, user)
            .Handle(new CalculateMonthlyPayrollCommand(sepId, RequireLockedAttendance: true), CancellationToken.None);

        var sepRec = await db.PayrollRecords.FirstAsync(r => r.PayrollPeriodId == sepId && r.EmployeeId == 15);
        sepRec.HasHighVarianceWarning.Should().BeTrue();
        sepRec.CalculationTraceJson.Should().NotBeNullOrWhiteSpace();

        var trace = await new GetPayrollRecordTraceQueryHandler(db)
            .Handle(new GetPayrollRecordTraceQuery(sepRec.Id), CancellationToken.None);
        trace.Steps.Should().Contain(s => s.Category == "Income" && s.ResultAmount == 30_000_000m);
        trace.Steps.Should().Contain(s => s.Category == "CompanyCost" && s.ResultAmount > 30_000_000m);
    }
}
