using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Payroll;
using Vinamilk.HRM.Domain.Entities.Core;
using Vinamilk.HRM.Domain.Entities.Payroll;
using Vinamilk.HRM.Domain.Entities.Time;
using Vinamilk.HRM.Infrastructure.Persistence;
using Vinamilk.HRM.Infrastructure.Persistence.Seed;
using Xunit;

namespace Vinamilk.HRM.UnitTests.Payroll;

public class PayrollCalculationTests
{
    private static async Task<ApplicationDbContext> CreateSeededDbContextAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"VnmPayroll_{Guid.NewGuid():N}")
            .Options;
        var db = new ApplicationDbContext(options);
        await db.PayrollStatutoryParameters.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultStatutoryParameters());
        await db.PitBrackets.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultPitBrackets());
        await db.SaveChangesAsync();
        return db;
    }

    [Fact]
    public async Task ReferenceCase_25MillionGross_0Dependents_Year2026_ShouldMatchExactStatutoryNumbers()
    {
        // Đối chiếu tay mẫu chuẩn 2026 (Luật 109/2025/QH15, NQ 110/2025/UBTVQH15, NĐ 293/2025):
        // 1. Tổng thu nhập (Gross) = 25.000.000 đ (26/26 công, không OT/thưởng)
        // 2. Lương đóng BH = 25.000.000 đ (< trần BHXH 46.800.000 đ và < trần BHTN vùng I 106.200.000 đ)
        //    - BHXH NLĐ (8%)   = 25.000.000 * 8%   = 2.000.000 đ
        //    - BHYT NLĐ (1.5%) = 25.000.000 * 1.5% =   375.000 đ
        //    - BHTN NLĐ (1%)   = 25.000.000 * 1%   =   250.000 đ
        //    -> Tổng BH NLĐ (10.5%)                = 2.625.000 đ
        // 3. Thu nhập chịu thuế = 25.000.000 - 2.625.000 = 22.375.000 đ
        // 4. Giảm trừ bản thân 2026 = 15.500.000 đ; 0 người phụ thuộc = 0 đ
        // 5. Thu nhập tính thuế (TNTT) = 22.375.000 - 15.500.000 = 6.875.000 đ
        // 6. Biểu 5 bậc 2026: TNTT 6.875.000 đ <= 10.000.000 đ -> Bậc 1 (5%)
        //    -> Thuế TNCN = 6.875.000 * 5% = 343.750 đ
        // 7. Thực lĩnh = 25.000.000 - 2.625.000 - 343.750 = 22.031.250 đ
        await using var db = await CreateSeededDbContextAsync();
        var ctx2026 = await VietnamPayrollEngine.ResolveStatutoryContextAsync(db, new DateOnly(2026, 3, 31));

        var result = VietnamPayrollEngine.Calculate(new PayrollCalculationInput(
            ContractBaseSalary: 25_000_000m,
            PositionAllowance: 0m,
            OtherAllowance: 0m,
            StandardWorkdays: 26m,
            PayableWorkdays: 26m,
            OtHours150: 0m,
            OtHours200: 0m,
            OtHours300: 0m,
            KpiBonus: 0m,
            DisciplineDeduction: 0m,
            DependentCount: 0,
            IsUnionMember: true,
            WageRegion: 1), ctx2026);

        result.GrossIncome.Should().Be(25_000_000m);
        result.EmployeeSocialInsurance.Should().Be(2_000_000m);
        result.EmployeeHealthInsurance.Should().Be(375_000m);
        result.EmployeeUnemploymentInsurance.Should().Be(250_000m);
        result.TotalEmployeeInsurance.Should().Be(2_625_000m);
        result.SelfDeductionAmount.Should().Be(15_500_000m);
        result.DependentDeductionAmount.Should().Be(0m);
        result.AssessableIncome.Should().Be(6_875_000m);
        result.PersonalIncomeTax.Should().Be(343_750m);
        result.NetSalary.Should().Be(22_031_250m);
    }

    [Fact]
    public async Task SameEmployee_Period12_2025_Vs_11_2026_ShouldSwitchFrom7BracketsTo5BracketsAndUpdateDeductions()
    {
        // Cùng một nhân viên: Lương HĐ 40.000.000 + Phụ cấp chức vụ 5.000.000 = 45.000.000 đ, 1 NPT, Vùng I.
        //
        // === KỲ 12/2025 (cuối kỳ 31/12/2025) ===
        // - LCS = 2.340.000 -> Trần BHXH/BHYT = 46.800.000 đ; LTTV Vùng I = 4.960.000 -> Trần BHTN = 99.200.000 đ
        // - Lương đóng BH = 45.000.000 đ -> BHXH 3.600.000 + BHYT 675.000 + BHTN 450.000 = 4.725.000 đ
        // - Giảm trừ: Bản thân 11.000.000 + 1 NPT 4.400.000 = 15.400.000 đ
        // - TNTT = 45.000.000 - 4.725.000 - 15.400.000 = 24.875.000 đ
        // - Thuế 7 bậc:
        //   + Bậc 1 (0 - 5tr @ 5%):       250.000 đ
        //   + Bậc 2 (5 - 10tr @ 10%):     500.000 đ
        //   + Bậc 3 (10 - 18tr @ 15%):  1.200.000 đ
        //   + Bậc 4 (18 - 24,875tr @ 20% = 6.875.000 * 20%): 1.375.000 đ
        //   -> Tổng thuế 12/2025 = 3.325.000 đ
        // - Thực lĩnh 12/2025 = 45.000.000 - 4.725.000 - 3.325.000 = 36.950.000 đ
        //
        // === KỲ 11/2026 (cuối kỳ 30/11/2026) ===
        // - LCS = 2.530.000 -> Trần BHXH/BHYT = 50.600.000 đ; LTTV Vùng I = 5.310.000 -> Trần BHTN = 106.200.000 đ
        // - Lương đóng BH = 45.000.000 đ -> Tổng BH vẫn = 4.725.000 đ
        // - Giảm trừ mới 2026: Bản thân 15.500.000 + 1 NPT 6.200.000 = 21.700.000 đ
        // - TNTT = 45.000.000 - 4.725.000 - 21.700.000 = 18.575.000 đ
        // - Thuế 5 bậc 2026:
        //   + Bậc 1 (0 - 10tr @ 5%): 500.000 đ
        //   + Bậc 2 (10 - 18,575tr @ 10% = 8.575.000 * 10%): 857.500 đ
        //   -> Tổng thuế 11/2026 = 1.357.500 đ
        // - Thực lĩnh 11/2026 = 45.000.000 - 4.725.000 - 1.357.500 = 38.917.500 đ
        await using var db = await CreateSeededDbContextAsync();
        var input = new PayrollCalculationInput(
            ContractBaseSalary: 40_000_000m,
            PositionAllowance: 5_000_000m,
            OtherAllowance: 0m,
            StandardWorkdays: 26m,
            PayableWorkdays: 26m,
            OtHours150: 0m,
            OtHours200: 0m,
            OtHours300: 0m,
            KpiBonus: 0m,
            DisciplineDeduction: 0m,
            DependentCount: 1,
            IsUnionMember: true,
            WageRegion: 1);

        var ctx2025 = await VietnamPayrollEngine.ResolveStatutoryContextAsync(db, new DateOnly(2025, 12, 31));
        var res2025 = VietnamPayrollEngine.Calculate(input, ctx2025);

        res2025.TotalEmployeeInsurance.Should().Be(4_725_000m);
        res2025.SelfDeductionAmount.Should().Be(11_000_000m);
        res2025.DependentDeductionAmount.Should().Be(4_400_000m);
        res2025.AssessableIncome.Should().Be(24_875_000m);
        res2025.PersonalIncomeTax.Should().Be(3_325_000m);
        res2025.NetSalary.Should().Be(36_950_000m);

        var ctx2026 = await VietnamPayrollEngine.ResolveStatutoryContextAsync(db, new DateOnly(2026, 11, 30));
        var res2026 = VietnamPayrollEngine.Calculate(input, ctx2026);

        res2026.TotalEmployeeInsurance.Should().Be(4_725_000m);
        res2026.SelfDeductionAmount.Should().Be(15_500_000m);
        res2026.DependentDeductionAmount.Should().Be(6_200_000m);
        res2026.AssessableIncome.Should().Be(18_575_000m);
        res2026.PersonalIncomeTax.Should().Be(1_357_500m);
        res2026.NetSalary.Should().Be(38_917_500m);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(10_000_000, 500_000)]     // Ngưỡng bậc 1: 10tr * 5% = 500.000
    [InlineData(30_000_000, 2_500_000)]   // Ngưỡng bậc 2: 500k + (30-10)tr * 10% = 2.500.000
    [InlineData(60_000_000, 8_500_000)]   // Ngưỡng bậc 3: 2.5tr + (60-30)tr * 20% = 8.500.000
    [InlineData(100_000_000, 20_500_000)] // Ngưỡng bậc 4: 8.5tr + (100-60)tr * 30% = 20.500.000
    [InlineData(120_000_000, 27_500_000)] // Bậc 5 (>100tr): 20.5tr + (120-100)tr * 35% = 27.500.000
    public async Task CalculateProgressivePit_2026FiveBrackets_ShouldMatchBoundaryThresholds(
        decimal assessableIncome,
        decimal expectedTax)
    {
        await using var db = await CreateSeededDbContextAsync();
        var ctx2026 = await VietnamPayrollEngine.ResolveStatutoryContextAsync(db, new DateOnly(2026, 6, 30));

        ctx2026.PitBrackets.Should().HaveCount(5);
        VietnamPayrollEngine.CalculateProgressivePit(assessableIncome, ctx2026.PitBrackets).Should().Be(expectedTax);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(4_000_000, 200_000)]      // Bậc 1: 4tr * 5%
    [InlineData(8_000_000, 550_000)]      // Bậc 2: 250k + 3tr * 10%
    [InlineData(15_000_000, 1_500_000)]   // Bậc 3: 250k + 500k + 5tr * 15%
    [InlineData(25_000_000, 3_350_000)]   // Bậc 4: 1.95tr + 7tr * 20%
    [InlineData(40_000_000, 6_750_000)]   // Bậc 5: 4.75tr + 8tr * 25%
    [InlineData(60_000_000, 12_150_000)]  // Bậc 6: 9.75tr + 8tr * 30%
    [InlineData(100_000_000, 25_150_000)] // Bậc 7: 18.15tr + 20tr * 35%
    public async Task CalculateProgressivePit_Pre2026SevenBrackets_ShouldMatchLegacyThresholds(
        decimal assessableIncome,
        decimal expectedTax)
    {
        await using var db = await CreateSeededDbContextAsync();
        var ctx2025 = await VietnamPayrollEngine.ResolveStatutoryContextAsync(db, new DateOnly(2025, 12, 31));

        ctx2025.PitBrackets.Should().HaveCount(7);
        VietnamPayrollEngine.CalculateProgressivePit(assessableIncome, ctx2025.PitBrackets).Should().Be(expectedTax);
    }

    [Fact]
    public async Task InsuranceCaps_ShouldUpdateSocialCapFromJuly2026_AndUseEmployeeWageRegionForUiCap()
    {
        // Kiểm tra:
        // Lương HĐ = 120.000.000 đ, OT = 4h*1.5 + 2h*2.0 = 10h quy đổi trên (26*8=208h) -> 120.000.000 / 208 * 10 = 5.769.231 đ
        //
        // Case A: Tháng 06/2026 (30/06/2026), Vùng I:
        //   - LCS = 2.340.000 -> Trần BHXH/BHYT = 46.800.000 đ (BHXH 3.744.000, BHYT 702.000)
        //   - LTTV Vùng I = 5.310.000 -> Trần BHTN = 106.200.000 đ (BHTN 1.062.000)
        //   - Tổng BH NLĐ = 5.508.000 đ
        //
        // Case B: Tháng 07/2026 (31/07/2026), Vùng I:
        //   - LCS mới = 2.530.000 -> Trần BHXH/BHYT = 50.600.000 đ (BHXH 4.048.000, BHYT 759.000)
        //   - Trần BHTN Vùng I = 106.200.000 đ (BHTN 1.062.000)
        //   - Tổng BH NLĐ = 5.869.000 đ
        //
        // Case C: Tháng 07/2026 (31/07/2026), Vùng II:
        //   - Trần BHXH/BHYT = 50.600.000 đ (BHXH 4.048.000, BHYT 759.000)
        //   - LTTV Vùng II = 4.730.000 -> Trần BHTN Vùng II = 94.600.000 đ (BHTN 946.000)
        //   - Tổng BH NLĐ = 5.753.000 đ
        await using var db = await CreateSeededDbContextAsync();

        var ctxJun2026 = await VietnamPayrollEngine.ResolveStatutoryContextAsync(db, new DateOnly(2026, 6, 30));
        var ctxJul2026 = await VietnamPayrollEngine.ResolveStatutoryContextAsync(db, new DateOnly(2026, 7, 31));

        var region1Jun = VietnamPayrollEngine.Calculate(new PayrollCalculationInput(
            ContractBaseSalary: 120_000_000m,
            PositionAllowance: 0m,
            OtherAllowance: 0m,
            StandardWorkdays: 26m,
            PayableWorkdays: 26m,
            OtHours150: 4m,
            OtHours200: 2m,
            OtHours300: 0m,
            KpiBonus: 0m,
            DisciplineDeduction: 0m,
            DependentCount: 0,
            IsUnionMember: true,
            WageRegion: 1), ctxJun2026);

        region1Jun.OvertimePay.Should().Be(5_769_231m);
        region1Jun.SocialInsuranceBase.Should().Be(46_800_000m);
        region1Jun.UnemploymentInsuranceBase.Should().Be(106_200_000m);
        region1Jun.EmployeeSocialInsurance.Should().Be(3_744_000m);
        region1Jun.EmployeeHealthInsurance.Should().Be(702_000m);
        region1Jun.EmployeeUnemploymentInsurance.Should().Be(1_062_000m);
        region1Jun.TotalEmployeeInsurance.Should().Be(5_508_000m);

        var region1Jul = VietnamPayrollEngine.Calculate(new PayrollCalculationInput(
            ContractBaseSalary: 120_000_000m,
            PositionAllowance: 0m,
            OtherAllowance: 0m,
            StandardWorkdays: 26m,
            PayableWorkdays: 26m,
            OtHours150: 4m,
            OtHours200: 2m,
            OtHours300: 0m,
            KpiBonus: 0m,
            DisciplineDeduction: 0m,
            DependentCount: 0,
            IsUnionMember: true,
            WageRegion: 1), ctxJul2026);

        region1Jul.SocialInsuranceBase.Should().Be(50_600_000m);
        region1Jul.UnemploymentInsuranceBase.Should().Be(106_200_000m);
        region1Jul.EmployeeSocialInsurance.Should().Be(4_048_000m);
        region1Jul.EmployeeHealthInsurance.Should().Be(759_000m);
        region1Jul.EmployeeUnemploymentInsurance.Should().Be(1_062_000m);
        region1Jul.TotalEmployeeInsurance.Should().Be(5_869_000m);

        var region2Jul = VietnamPayrollEngine.Calculate(new PayrollCalculationInput(
            ContractBaseSalary: 120_000_000m,
            PositionAllowance: 0m,
            OtherAllowance: 0m,
            StandardWorkdays: 26m,
            PayableWorkdays: 26m,
            OtHours150: 4m,
            OtHours200: 2m,
            OtHours300: 0m,
            KpiBonus: 0m,
            DisciplineDeduction: 0m,
            DependentCount: 0,
            IsUnionMember: true,
            WageRegion: 2), ctxJul2026);

        region2Jul.SocialInsuranceBase.Should().Be(50_600_000m);
        region2Jul.UnemploymentInsuranceBase.Should().Be(94_600_000m);
        region2Jul.EmployeeUnemploymentInsurance.Should().Be(946_000m);
        region2Jul.TotalEmployeeInsurance.Should().Be(5_753_000m);
    }

    [Fact]
    public async Task CalculateMonthlyPayrollCommand_ShouldResolveEffectivePeriodAndUnitRegion_AndRejectLockedPeriod()
    {
        await using var db = await CreateSeededDbContextAsync();

        db.OrganizationUnits.Add(new OrganizationUnit
        {
            Id = 1,
            UnitCode = "NM_BD",
            UnitName = "Nhà máy Bình Dương",
            WageRegion = 2,
            IsActive = true
        });
        db.Positions.Add(new Position { Id = 1, PositionCode = "OP", PositionName = "Vận hành", IsActive = true });
        db.Employees.Add(new Employee
        {
            Id = 1,
            EmployeeCode = "VNM-100",
            FullName = "Lê Văn Lương",
            OrganizationUnitId = 1,
            PositionId = 1,
            BaseSalary = 25_000_000m,
            PositionAllowance = 0m,
            WorkStatus = "Official",
            IsUnionMember = true,
            WageRegion = null // kế thừa từ đơn vị (Vùng II)
        });
        db.MonthlyAttendanceSummaries.Add(new MonthlyAttendanceSummary
        {
            Id = 1,
            EmployeeId = 1,
            Month = 11,
            Year = 2026,
            StandardWorkdays = 26m,
            TotalPayableWorkdays = 26m
        });
        await db.SaveChangesAsync();

        var currentUser = new FakeCurrentUserService();

        var periodId = await new CreatePayrollPeriodCommandHandler(db)
            .Handle(new CreatePayrollPeriodCommand(2026, 11, 26m), CancellationToken.None);

        var count = await new CalculateMonthlyPayrollCommandHandler(db, currentUser)
            .Handle(new CalculateMonthlyPayrollCommand(periodId), CancellationToken.None);
        count.Should().Be(1);

        var rec = await db.PayrollRecords.FirstAsync(r => r.EmployeeId == 1);
        rec.TotalEmployeeInsurance.Should().Be(2_625_000m);
        rec.SelfDeductionAmount.Should().Be(15_500_000m);
        rec.AssessableIncome.Should().Be(6_875_000m);
        rec.PersonalIncomeTax.Should().Be(343_750m);
        rec.NetSalary.Should().Be(22_031_250m);

        // Chưa qua bước Approved -> không được phép khóa kỳ lương
        Func<Task> lockBeforeApproved = () => new LockPayrollPeriodCommandHandler(db, currentUser)
            .Handle(new LockPayrollPeriodCommand(periodId), CancellationToken.None);
        await lockBeforeApproved.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Approved*");

        var periodToApprove = await db.PayrollPeriods.FirstAsync(p => p.Id == periodId);
        periodToApprove.Status = PayrollPeriodStatuses.Approved;
        await db.SaveChangesAsync();

        // Khóa kỳ lương 11/2026
        await new LockPayrollPeriodCommandHandler(db, currentUser).Handle(new LockPayrollPeriodCommand(periodId), CancellationToken.None);

        Func<Task> recalcLocked = () => new CalculateMonthlyPayrollCommandHandler(db, currentUser)
            .Handle(new CalculateMonthlyPayrollCommand(periodId), CancellationToken.None);
        await recalcLocked.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Locked*");

        // Cố sửa tham số hoặc biểu thuế bao trùm kỳ lương 11/2026 đã khóa -> phải bị chặn
        var activeParam2026Jul = await db.PayrollStatutoryParameters
            .FirstAsync(p => p.EffectiveFrom == new DateOnly(2026, 7, 1));

        Func<Task> editLockedParam = () => new SavePayrollStatutoryParameterCommandHandler(db, currentUser).Handle(
            new SavePayrollStatutoryParameterCommand(
                activeParam2026Jul.Id,
                activeParam2026Jul.EffectiveFrom,
                activeParam2026Jul.EffectiveTo,
                "Thử sửa kỳ đã khóa",
                null,
                2_700_000m,
                activeParam2026Jul.MinWageRegion1,
                activeParam2026Jul.MinWageRegion2,
                activeParam2026Jul.MinWageRegion3,
                activeParam2026Jul.MinWageRegion4,
                activeParam2026Jul.SocialInsuranceCapMultiplier,
                activeParam2026Jul.UnemploymentInsuranceCapMultiplier,
                activeParam2026Jul.EmployeeSiRate,
                activeParam2026Jul.EmployeeHiRate,
                activeParam2026Jul.EmployeeUiRate,
                activeParam2026Jul.CompanySiRate,
                activeParam2026Jul.CompanyHiRate,
                activeParam2026Jul.CompanyUiRate,
                activeParam2026Jul.CompanyUnionFeeRate,
                activeParam2026Jul.SelfDeduction,
                activeParam2026Jul.DependentDeduction),
            CancellationToken.None);
        await editLockedParam.Should().ThrowAsync<InvalidOperationException>().WithMessage("*khóa*");

        Func<Task> editLockedPit = () => new SavePitBracketGroupCommandHandler(db, currentUser).Handle(
            new SavePitBracketGroupCommand(
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 1, 1),
                null,
                "Thử sửa biểu thuế đã khóa",
                new[] { new PitBracketInputItem(1, 0m, null, 5m) }),
            CancellationToken.None);
        await editLockedPit.Should().ThrowAsync<InvalidOperationException>().WithMessage("*khóa*");
    }

    [Fact]
    public async Task HrAdmin_ShouldAddAndUpdateFutureStatutoryParametersAndPitBrackets_AndWriteAuditLogs()
    {
        await using var db = await CreateSeededDbContextAsync();
        var currentUser = new FakeCurrentUserService();

        // Thêm bộ tham số hiệu lực tương lai (từ 01/01/2027) -> thành công + ghi AuditLog
        var paramHandler = new SavePayrollStatutoryParameterCommandHandler(db, currentUser);
        var createdId = await paramHandler.Handle(new SavePayrollStatutoryParameterCommand(
            Id: null,
            EffectiveFrom: new DateOnly(2027, 1, 1),
            EffectiveTo: null,
            LegalReference: "Dự thảo 2027",
            Notes: "Kịch bản giả định tăng LCS",
            StatutoryBaseSalary: 2_800_000m,
            MinWageRegion1: 5_600_000m,
            MinWageRegion2: 5_000_000m,
            MinWageRegion3: 4_400_000m,
            MinWageRegion4: 3_950_000m,
            SocialInsuranceCapMultiplier: 20m,
            UnemploymentInsuranceCapMultiplier: 20m,
            EmployeeSiRate: 0.08m,
            EmployeeHiRate: 0.015m,
            EmployeeUiRate: 0.01m,
            CompanySiRate: 0.17m,
            CompanyHiRate: 0.03m,
            CompanyUiRate: 0.01m,
            CompanyUnionFeeRate: 0.02m,
            SelfDeduction: 16_000_000m,
            DependentDeduction: 6_500_000m), CancellationToken.None);

        createdId.Should().BeGreaterThan(0);

        await paramHandler.Handle(new SavePayrollStatutoryParameterCommand(
            Id: createdId,
            EffectiveFrom: new DateOnly(2027, 1, 1),
            EffectiveTo: new DateOnly(2027, 12, 31),
            LegalReference: "Dự thảo 2027 - Đã điều chỉnh",
            Notes: "Cập nhật ngày hết hiệu lực",
            StatutoryBaseSalary: 2_850_000m,
            MinWageRegion1: 5_600_000m,
            MinWageRegion2: 5_000_000m,
            MinWageRegion3: 4_400_000m,
            MinWageRegion4: 3_950_000m,
            SocialInsuranceCapMultiplier: 20m,
            UnemploymentInsuranceCapMultiplier: 20m,
            EmployeeSiRate: 0.08m,
            EmployeeHiRate: 0.015m,
            EmployeeUiRate: 0.01m,
            CompanySiRate: 0.17m,
            CompanyHiRate: 0.03m,
            CompanyUiRate: 0.01m,
            CompanyUnionFeeRate: 0.02m,
            SelfDeduction: 16_000_000m,
            DependentDeduction: 6_500_000m), CancellationToken.None);

        var pitHandler = new SavePitBracketGroupCommandHandler(db, currentUser);
        var bracketCount = await pitHandler.Handle(new SavePitBracketGroupCommand(
            OriginalEffectiveFrom: null,
            EffectiveFrom: new DateOnly(2027, 1, 1),
            EffectiveTo: new DateOnly(2027, 12, 31),
            LegalReference: "Biểu thuế thử nghiệm 2027",
            Brackets: new[]
            {
                new PitBracketInputItem(1, 0m, 15_000_000m, 5m),
                new PitBracketInputItem(2, 15_000_000m, null, 15m)
            }), CancellationToken.None);

        bracketCount.Should().Be(2);

        var auditActions = await db.AuditLogs.Select(a => a.ActionType).ToListAsync();
        auditActions.Should().Contain(new[]
        {
            "PayrollStatutoryParameter_Create",
            "PayrollStatutoryParameter_Update",
            "PitBracketGroup_Create"
        });

        var overview = await new GetPayrollManagementOverviewQueryHandler(db)
            .Handle(new GetPayrollManagementOverviewQuery(), CancellationToken.None);
        overview.StatutoryParameters.Should().Contain(p => p.StatutoryBaseSalary == 2_850_000m);
        overview.PitBracketGroups.Should().Contain(g => g.EffectiveFrom == new DateOnly(2027, 1, 1));
    }

    [Fact]
    public async Task EmployerContributions_On10MillionSalaryBase_ShouldMatchStatutory21_5PercentPlus2PercentUnion()
    {
        // Kiểm tra tính tay theo đúng yêu cầu đối chiếu:
        // Lương đóng BH = 10.000.000 đ:
        // - DN BHXH = 17% (1.700.000) + TNLĐ-BNN 0.5% (50.000) = 1.750.000 đ (17.5%)
        // - DN BHYT = 3% = 300.000 đ
        // - DN BHTN = 1% = 100.000 đ
        // -> Tổng bảo hiểm DN đóng = 2.150.000 đ (21.5%)
        // -> KPCĐ DN tách riêng = 2% = 200.000 đ
        // -> Tổng chi phí DN = 10.000.000 + 2.150.000 + 200.000 = 12.350.000 đ
        await using var db = await CreateSeededDbContextAsync();
        var ctx = await VietnamPayrollEngine.ResolveStatutoryContextAsync(db, new DateOnly(2026, 9, 30));

        var res = VietnamPayrollEngine.Calculate(new PayrollCalculationInput(
            ContractBaseSalary: 10_000_000m,
            PositionAllowance: 0m,
            OtherAllowance: 0m,
            StandardWorkdays: 22m,
            PayableWorkdays: 22m,
            OtHours150: 0m,
            OtHours200: 0m,
            OtHours300: 0m,
            KpiBonus: 0m,
            DisciplineDeduction: 0m,
            DependentCount: 0,
            IsUnionMember: true,
            WageRegion: 1), ctx);

        res.SocialInsuranceBase.Should().Be(10_000_000m);
        res.CompanySocialInsurance.Should().Be(1_700_000m);
        res.CompanyOccupationalAccidentInsurance.Should().Be(50_000m);
        (res.CompanySocialInsurance + res.CompanyOccupationalAccidentInsurance).Should().Be(1_750_000m);
        res.CompanyHealthInsurance.Should().Be(300_000m);
        res.CompanyUnemploymentInsurance.Should().Be(100_000m);
        (res.CompanySocialInsurance + res.CompanyOccupationalAccidentInsurance + res.CompanyHealthInsurance + res.CompanyUnemploymentInsurance)
            .Should().Be(2_150_000m);
        res.CompanyUnionFee.Should().Be(200_000m);
        res.TotalCompanyCost.Should().Be(12_350_000m);
    }
}
