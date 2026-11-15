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

public class SalaryStructureCl5Tests
{
    private static async Task<ApplicationDbContext> CreateSeededDbContextAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"VnmSalaryStructCl5_{Guid.NewGuid():N}")
            .Options;
        var db = new ApplicationDbContext(options);

        await db.PayrollStatutoryParameters.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultStatutoryParameters());
        await db.PitBrackets.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultPitBrackets());
        await db.PayrollParameters.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultTaxExemptPayrollParameters());

        var components = ApplicationDbContextInitializer.BuildDefaultSalaryComponentsCl5();
        await db.SalaryComponents.AddRangeAsync(components);
        await db.SaveChangesAsync();

        var compMap = await db.SalaryComponents.ToDictionaryAsync(c => c.ComponentCode, StringComparer.OrdinalIgnoreCase);
        var templates = ApplicationDbContextInitializer.BuildDefaultSalaryTemplatesWithComponents(compMap);
        await db.SalaryTemplates.AddRangeAsync(templates);
        await db.SaveChangesAsync();

        return db;
    }

    [Fact]
    public async Task NCalc_MultiLevelFormula_ShouldComputeInfoNightOtAndMealTaxExemption_Correctly()
    {
        // Tính tay đối chiếu (không dùng hằng số engine):
        // - LCB hiện tại = 20.000.000 đ, Công chuẩn = 26, Giờ làm ngày = 8 -> 208 giờ/tháng
        // - INFO_HOURLY_RATE (Info) = 20.000.000 / 208 = 96.153,8462 (làm tròn 4 chữ số thập phân trung gian; KHÔNG cộng vào Gross)
        // - BASIC_PAY = Round(20.000.000 * 26 / 26, 0) = 20.000.000 đ
        // - NIGHT_ALLOWANCE = Round(96.153,8462 * 16 * 0.30, 0) = Round(461.538,46176, 0) = 461.538 đ (miễn thuế 100% -> Taxable = 0)
        // - OT_PAY = Round(96.153,8462 * (4 * 1.5), 0) = Round(576.923,0772, 0) = 576.923 đ (chịu thuế 100%)
        // - MEAL_ALLOWANCE = 1.000.000 đ (trần miễn thuế PIT_EXEMPT_MEAL = 730.000 đ -> Taxable = 270.000 đ; Miễn thuế = 730.000 đ)
        // -> Tổng thu nhập Gross (bỏ qua dòng Info) = 20.000.000 + 461.538 + 576.923 + 1.000.000 = 22.038.461 đ
        // -> Tổng miễn thuế các khoản = 461.538 + 730.000 = 1.191.538 đ
        await using var db = await CreateSeededDbContextAsync();

        var specs = await db.SalaryComponents
            .AsNoTracking()
            .Where(c => c.ComponentCode == "INFO_HOURLY_RATE"
                || c.ComponentCode == "BASIC_PAY"
                || c.ComponentCode == "NIGHT_ALLOWANCE"
                || c.ComponentCode == "OT_PAY"
                || c.ComponentCode == "MEAL_ALLOWANCE")
            .Select(c => new FormulaComponentSpec(
                c.Id, c.ComponentCode, c.ComponentName, c.ComponentType, c.CalculationType,
                c.DefaultAmount, c.FormulaExpression, c.SortOrder, c.IsTaxable, c.TaxExemptLimit,
                c.TaxExemptLimitParameterCode, c.IsSocialInsuranceBase, c.ShowOnPayslip))
            .ToListAsync();

        var paramValues = await db.PayrollParameters
            .AsNoTracking()
            .ToDictionaryAsync(p => p.ParamCode, p => p.ParamValue, StringComparer.OrdinalIgnoreCase);

        var vars = NCalcSalaryFormulaEngine.BuildDefaultSampleVariables();
        vars["LUONG_CO_BAN_HIEN_TAI"] = 20_000_000m;
        vars["CONG_CHUAN"] = 26m;
        vars["CONG_THUC_TE"] = 26m;
        vars["GIO_DEM"] = 16m;
        vars["OT150"] = 4m;
        vars["OT200"] = 0m;
        vars["OT300"] = 0m;
        vars["MEAL_ALLOWANCE"] = 1_000_000m;

        var result = NCalcSalaryFormulaEngine.EvaluateComponents(specs, vars, "RoundToDong", paramValues);

        result.Lines.Should().HaveCount(5);
        result.Lines.Single(l => l.ComponentCode == "INFO_HOURLY_RATE").FinalAmount.Should().Be(96_153.8462m);
        result.Lines.Single(l => l.ComponentCode == "BASIC_PAY").FinalAmount.Should().Be(20_000_000m);

        var nightLine = result.Lines.Single(l => l.ComponentCode == "NIGHT_ALLOWANCE");
        nightLine.FinalAmount.Should().Be(461_538m);
        nightLine.TaxableAmount.Should().Be(0m);

        result.Lines.Single(l => l.ComponentCode == "OT_PAY").FinalAmount.Should().Be(576_923m);

        var mealLine = result.Lines.Single(l => l.ComponentCode == "MEAL_ALLOWANCE");
        mealLine.FinalAmount.Should().Be(1_000_000m);
        mealLine.TaxableAmount.Should().Be(270_000m);

        result.TotalEarningsAndAllowances.Should().Be(22_038_461m);
        result.TotalTaxExemptAmount.Should().Be(1_191_538m);
    }

    [Fact]
    public async Task FormulaValidation_ShouldBlockCycles_And_SyntaxErrors_InPreviewAndSave()
    {
        await using var db = await CreateSeededDbContextAsync();
        var user = new FakeCurrentUserService();
        var queryHandler = new ValidateAndTestSalaryFormulaQueryHandler(db);
        var saveHandler = new SaveSalaryComponentCommandHandler(db, user);

        // 1. Tự tham chiếu: COMP_SELF = COMP_SELF + 100000
        var selfCheck = await queryHandler.Handle(new ValidateAndTestSalaryFormulaQuery(
            "COMP_SELF + 100000", "COMP_SELF"), CancellationToken.None);
        selfCheck.IsValid.Should().BeFalse();
        selfCheck.HasCircularDependency.Should().BeTrue();
        selfCheck.CyclePath.Should().Contain("COMP_SELF");

        // 2. Tạo A -> 100000, B -> A + 50000, sau đó thử cập nhật A -> B + 10000 (vòng lặp A -> B -> A)
        await saveHandler.Handle(new SaveSalaryComponentCommand(
            null, "CYCLE_A", "Thành phần A", "Allowance", "Fixed", 100_000m,
            true, 0m, null, false, true, null, 50, true), CancellationToken.None);

        await saveHandler.Handle(new SaveSalaryComponentCommand(
            null, "CYCLE_B", "Thành phần B", "Allowance", "Formula", 0m,
            true, 0m, null, false, true, "CYCLE_A + 50000", 51, true), CancellationToken.None);

        var cycleABCheck = await queryHandler.Handle(new ValidateAndTestSalaryFormulaQuery(
            "CYCLE_B + 10000", "CYCLE_A"), CancellationToken.None);
        cycleABCheck.IsValid.Should().BeFalse();
        cycleABCheck.HasCircularDependency.Should().BeTrue();
        cycleABCheck.CyclePath.Should().Contain("CYCLE_A");
        cycleABCheck.CyclePath.Should().Contain("CYCLE_B");

        // Lưu CYCLE_A vòng lặp bị chặn & ném InvalidOperationException
        var cycleAEntity = await db.SalaryComponents.FirstAsync(c => c.ComponentCode == "CYCLE_A");
        Func<Task> actCycle = () => saveHandler.Handle(new SaveSalaryComponentCommand(
            cycleAEntity.Id, "CYCLE_A", "Thành phần A", "Allowance", "Formula", 0m,
            true, 0m, null, false, true, "CYCLE_B * 2", 50, true), CancellationToken.None);
        await actCycle.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*vòng lặp*");

        // 3. Lỗi cú pháp NCalc bị chặn ở cả màn hình kiểm tra và lưu
        var badSyntaxCheck = await queryHandler.Handle(new ValidateAndTestSalaryFormulaQuery(
            "Round(LUONG_CO_BAN * , 0)", "BAD_SYNTAX"), CancellationToken.None);
        badSyntaxCheck.IsValid.Should().BeFalse();
        badSyntaxCheck.HasSyntaxError.Should().BeTrue();

        Func<Task> actBadSyntax = () => saveHandler.Handle(new SaveSalaryComponentCommand(
            null, "BAD_SYNTAX", "Công thức lỗi", "Allowance", "Formula", 0m,
            true, 0m, null, false, true, "Round(LUONG_CO_BAN * , 0)", 60, true), CancellationToken.None);
        await actBadSyntax.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task MidMonthSalaryChange_Day16_WithDailyLogsAndDecoupledInsurance_ShouldSplitAccurately()
    {
        // Tính tay kỳ 06/2026 (01/06/2026 - 30/06/2026), Công chuẩn = 26, Luật 2026 (GTGC 15.5tr, biểu 5 bậc):
        // - Mốc 1 (01/06 - 15/06): LCB = 20.000.000 đ, Lương BHXH = 15.000.000 đ, PCCV = 0
        // - Mốc 2 (16/06 - 30/06): LCB = 26.000.000 đ, Lương BHXH = 15.000.000 đ (giữ tách riêng!), PCCV = 2.000.000 đ
        // - Nhật ký chấm công: Nửa đầu 13 công + 2h OT150; Nửa sau 13 công + 4h OT150 -> Tổng 26 công, 6h OT150
        //
        // Đoạn 1:
        //   - Lương công = 20.000.000 * (13 / 26) = 10.000.000 đ
        //   - PCCV = 0
        //   - OT = (20.000.000 / 208) * (2 * 1.5) = 96.153,84615... * 3 = 288.461,538 -> 288.462 đ
        // Đoạn 2:
        //   - Lương công = 26.000.000 * (13 / 26) = 13.000.000 đ
        //   - PCCV = 2.000.000 * (13 / 26) = 1.000.000 đ
        //   - OT = (26.000.000 / 208) * (4 * 1.5) = 125.000 * 6 = 750.000 đ
        // Cộng gộp 2 đoạn:
        //   - ActualBaseSalary = 10.000.000 + 13.000.000 = 23.000.000 đ
        //   - PositionAllowance = 1.000.000 đ
        //   - OvertimePay = 288.462 + 750.000 = 1.038.462 đ
        //   - GrossIncome = 23.000.000 + 1.000.000 + 1.038.462 = 25.038.462 đ
        // Bảo hiểm dựa trên Lương đóng BHXH tách riêng = 15.000.000 đ (vùng I):
        //   - BHXH NLĐ (8%) = 1.200.000 đ; BHYT (1.5%) = 225.000 đ; BHTN (1%) = 150.000 đ -> Tổng = 1.575.000 đ
        //   - Kinh phí Công đoàn (2%) = 300.000 đ
        // Thuế TNCN (Luật 2026, 0 NPT, miễn thuế phần trả trội OT >100% theo TT 111/2013):
        //   - Phần OT miễn thuế đoạn 1 = 288.462 - Round(96.153,8462 * 2, 0) = 288.462 - 192.308 = 96.154 đ
        //   - Phần OT miễn thuế đoạn 2 = 750.000 - (125.000 * 4) = 250.000 đ -> Tổng miễn OT = 346.154 đ
        //   - Thu nhập chịu thuế = 25.038.462 - 1.575.000 - 346.154 = 23.117.308 đ
        //   - Thu nhập tính thuế = 23.117.308 - 15.500.000 = 7.617.308 đ (Bậc 1: 5%)
        //   - Thuế TNCN = Round(7.617.308 * 0.05, 0) = 380.865 đ
        // Thực lĩnh = 25.038.462 - 1.575.000 - 380.865 = 23.082.597 đ
        await using var db = await CreateSeededDbContextAsync();
        var user = new FakeCurrentUserService();

        db.OrganizationUnits.Add(new OrganizationUnit { Id = 1, UnitCode = "HQ_FIN", UnitName = "Khối Tài chính", WageRegion = 1, IsActive = true });
        db.Positions.Add(new Position { Id = 1, PositionCode = "ACC", PositionName = "Kế toán tổng hợp", IsActive = true });
        db.Employees.Add(new Employee
        {
            Id = 1,
            EmployeeCode = "VNM-CL5-01",
            FullName = "Nguyễn Thị Lan",
            OrganizationUnitId = 1,
            PositionId = 1,
            BaseSalary = 20_000_000m,
            WorkStatus = "Official",
            IsUnionMember = true,
            WageRegion = 1
        });
        db.MonthlyAttendanceSummaries.Add(new MonthlyAttendanceSummary
        {
            Id = 1,
            EmployeeId = 1,
            Year = 2026,
            Month = 6,
            StandardWorkdays = 26m,
            TotalPayableWorkdays = 26m,
            TotalOtHours150 = 6m
        });
        // Daily logs: 13 công + 2h OT trước ngày 16, 13 công + 4h OT từ ngày 16
        db.DailyAttendances.AddRange(
            new DailyAttendance
            {
                EmployeeId = 1,
                AttendanceDate = new DateOnly(2026, 6, 5),
                WorkdayCount = 13m,
                OvertimeHours150 = 2m
            },
            new DailyAttendance
            {
                EmployeeId = 1,
                AttendanceDate = new DateOnly(2026, 6, 20),
                WorkdayCount = 13m,
                OvertimeHours150 = 4m
            });
        await db.SaveChangesAsync();

        var officeTpl = await db.SalaryTemplates.FirstAsync(t => t.TemplateCode == "OFFICE_STD");
        var profileHandler = new SaveEmployeeSalaryProfileCommandHandler(db, user);

        await profileHandler.Handle(new SaveEmployeeSalaryProfileCommand(
            EmployeeId: 1,
            EffectiveFrom: new DateOnly(2026, 6, 1),
            BaseSalary: 20_000_000m,
            InsuranceSalary: 15_000_000m,
            SalaryStepId: null,
            SalaryTemplateId: officeTpl.Id,
            PayMode: "Monthly",
            UnitRate: 0m,
            IsProbation: false,
            ProbationRatePercent: 100m,
            PositionAllowance: 0m,
            OtherAllowance: 0m,
            IsUnionMember: true,
            DecisionNumber: "HĐ-0601"), CancellationToken.None);

        await profileHandler.Handle(new SaveEmployeeSalaryProfileCommand(
            EmployeeId: 1,
            EffectiveFrom: new DateOnly(2026, 6, 16),
            BaseSalary: 26_000_000m,
            InsuranceSalary: 15_000_000m,
            SalaryStepId: null,
            SalaryTemplateId: officeTpl.Id,
            PayMode: "Monthly",
            UnitRate: 0m,
            IsProbation: false,
            ProbationRatePercent: 100m,
            PositionAllowance: 2_000_000m,
            OtherAllowance: 0m,
            IsUnionMember: true,
            DecisionNumber: "QĐ-0616",
            Reason: "Thăng chức giữa tháng"), CancellationToken.None);

        var periodId = await new CreatePayrollPeriodCommandHandler(db)
            .Handle(new CreatePayrollPeriodCommand(2026, 6, 26m), CancellationToken.None);

        await new CalculateMonthlyPayrollCommandHandler(db, user)
            .Handle(new CalculateMonthlyPayrollCommand(periodId), CancellationToken.None);

        var rec = await db.PayrollRecords
            .Include(r => r.Details)
            .FirstAsync(r => r.PayrollPeriodId == periodId && r.EmployeeId == 1);

        rec.ActualBaseSalary.Should().Be(23_000_000m);
        rec.PositionAllowance.Should().Be(1_000_000m);
        rec.OvertimePay.Should().Be(1_038_462m);
        rec.GrossIncome.Should().Be(25_038_462m);
        rec.InsuranceSalaryBase.Should().Be(15_000_000m);
        rec.TotalEmployeeInsurance.Should().Be(1_575_000m);
        rec.CompanyUnionFee.Should().Be(300_000m);
        rec.PersonalIncomeTax.Should().Be(380_865m);
        rec.NetSalary.Should().Be(23_082_597m);
        rec.Notes.Should().Contain("Tách lương theo hiệu lực");
        rec.Details.Should().Contain(d => d.ComponentCode == "BASIC_PAY" && d.Amount == 23_000_000m);
        rec.Details.Should().NotContain(d => d.ComponentCode == "INFO_HOURLY_RATE");
    }

    [Fact]
    public async Task MidMonthSplit_MonthlyExemptAllowance_ShouldNotDoubleCountMonthlyCapAcrossSegments()
    {
        // Khi tách làm 2 đoạn trong tháng 06/2026: 13 ngày làm việc mỗi đoạn (01-15/06 và 16-30/06, nghỉ CN):
        // OTHER_ALLOWANCE (hoặc Fixed Allowance gắn trần PIT_EXEMPT_MEAL = 730.000đ/tháng) = 1.000.000 đ/tháng
        // -> Đoạn 1 = 500.000 đ, Đoạn 2 = 500.000 đ -> Tổng tháng = 1.000.000 đ
        // -> Miễn thuế tối đa toàn tháng = 730.000 đ (KHÔNG được nhân đôi lên 1.000.000 đ dù tách 2 đoạn!)
        await using var db = await CreateSeededDbContextAsync();
        var ctx = await VietnamPayrollEngine.ResolveStatutoryContextAsync(db, new DateOnly(2026, 6, 30));

        var mealComp = await db.SalaryComponents.AsNoTracking().FirstAsync(c => c.ComponentCode == "MEAL_ALLOWANCE");
        var basicComp = await db.SalaryComponents.AsNoTracking().FirstAsync(c => c.ComponentCode == "BASIC_PAY");

        var specs = new List<FormulaComponentSpec>
        {
            new(basicComp.Id, basicComp.ComponentCode, basicComp.ComponentName, basicComp.ComponentType, basicComp.CalculationType, 0m, basicComp.FormulaExpression, 1, true, 0m, null, true, true),
            new(mealComp.Id, mealComp.ComponentCode, mealComp.ComponentName, mealComp.ComponentType, "Fixed", 1_000_000m, null, 2, true, 730_000m, "PIT_EXEMPT_MEAL", false, true)
        };

        var p1 = new EmployeeSalaryProfile
        {
            EmployeeId = 1, EffectiveFrom = new DateOnly(2026, 6, 1), EffectiveTo = new DateOnly(2026, 6, 15),
            BaseSalary = 20_000_000m, InsuranceSalary = 20_000_000m, PayMode = "Monthly", IsUnionMember = true,
            SalaryTemplateId = 99
        };
        p1.FixedAllowances.Add(new EmployeeSalaryProfileAllowance { SalaryComponentId = mealComp.Id, ComponentCode = "MEAL_ALLOWANCE", Amount = 1_000_000m });

        var p2 = new EmployeeSalaryProfile
        {
            EmployeeId = 1, EffectiveFrom = new DateOnly(2026, 6, 16), EffectiveTo = null,
            BaseSalary = 26_000_000m, InsuranceSalary = 26_000_000m, PayMode = "Monthly", IsUnionMember = true,
            SalaryTemplateId = 99
        };
        p2.FixedAllowances.Add(new EmployeeSalaryProfileAllowance { SalaryComponentId = mealComp.Id, ComponentCode = "MEAL_ALLOWANCE", Amount = 1_000_000m });

        var slices = MidMonthSalarySplitter.BuildPeriodSlices(
            new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30),
            26m, 0m, 0m, 0m, 0m,
            new[] { p1, p2 },
            new Dictionary<int, IReadOnlyList<FormulaComponentSpec>> { [99] = specs },
            dailyAttendances: null);

        slices.Should().HaveCount(2);
        var paramValues = new Dictionary<string, decimal> { ["PIT_EXEMPT_MEAL"] = 730_000m };

        var agg = VietnamPayrollEngine.CalculateFromMidMonthSlices(
            slices, ctx, 26m, 26m, 0m, 0m, 0m, 0, 1,
            taxExemptParamValues: paramValues);

        var mealLine = agg.AggregatedLines.Single(l => l.ComponentCode == "MEAL_ALLOWANCE");
        mealLine.FinalAmount.Should().Be(1_000_000m);
        mealLine.TaxableAmount.Should().Be(270_000m);
        agg.TaxExemptAmount.Should().Be(730_000m);
    }

    [Fact]
    public async Task EmployeeSalaryProfile_ShouldEnforceProbationFloor_Timeline_PayModes_AndLockedPeriodGuard()
    {
        await using var db = await CreateSeededDbContextAsync();
        var user = new FakeCurrentUserService();

        db.OrganizationUnits.Add(new OrganizationUnit { Id = 1, UnitCode = "NM_BD", UnitName = "Nhà máy Bình Dương", WageRegion = 2, IsActive = true });
        db.Positions.Add(new Position { Id = 1, PositionCode = "WK", PositionName = "Công nhân", IsActive = true });
        db.Employees.Add(new Employee
        {
            Id = 1, EmployeeCode = "VNM-WK01", FullName = "Trần Văn Công",
            OrganizationUnitId = 1, PositionId = 1, BaseSalary = 20_000_000m, WorkStatus = "Probation", IsUnionMember = false
        });
        await db.SaveChangesAsync();

        var tpl = await db.SalaryTemplates.FirstAsync(t => t.TemplateCode == "OFFICE_STD");
        var handler = new SaveEmployeeSalaryProfileCommandHandler(db, user);

        // 1. Tỷ lệ thử việc < 85% (sàn cấu hình từ PayrollStatutoryParameter) bị chặn
        Func<Task> belowFloor = () => handler.Handle(new SaveEmployeeSalaryProfileCommand(
            1, new DateOnly(2026, 7, 1), 20_000_000m, 20_000_000m, null, tpl.Id,
            "Monthly", 0m, IsProbation: true, ProbationRatePercent: 80m, 0m, 0m, IsUnionMember: false), CancellationToken.None);
        await belowFloor.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*85%*");

        // 2. Lưu hồ sơ thử việc 85% từ 01/07/2026 hợp lệ (20tr * 85% = 17.000.000đ)
        await handler.Handle(new SaveEmployeeSalaryProfileCommand(
            1, new DateOnly(2026, 7, 1), 20_000_000m, 17_000_000m, null, tpl.Id,
            "Monthly", 0m, IsProbation: true, ProbationRatePercent: 85m, 0m, 0m, IsUnionMember: false), CancellationToken.None);

        // 3. Lưu hồ sơ trả lương theo Ngày (Daily: 600.000đ/ngày * 20 công = 12.000.000đ) hiệu lực từ 01/08/2026
        //    -> Tự động đóng hồ sơ cũ về EffectiveTo = 31/07/2026
        await handler.Handle(new SaveEmployeeSalaryProfileCommand(
            1, new DateOnly(2026, 8, 1), 15_000_000m, 12_000_000m, null, tpl.Id,
            "Daily", UnitRate: 600_000m, IsProbation: false, ProbationRatePercent: 100m, 0m, 0m, IsUnionMember: false), CancellationToken.None);

        var profiles = await db.EmployeeSalaryProfiles
            .Where(p => p.EmployeeId == 1)
            .OrderBy(p => p.EffectiveFrom)
            .ToListAsync();
        profiles.Should().HaveCount(2);
        profiles[0].EffectiveTo.Should().Be(new DateOnly(2026, 7, 31));
        profiles[1].EffectiveTo.Should().BeNull();

        // Kiểm tra hình thức Daily: 20 công * 600.000đ = 12.000.000đ; IsUnionMember = false -> Phí CĐ = 0
        var ctxAug = await VietnamPayrollEngine.ResolveStatutoryContextAsync(db, new DateOnly(2026, 8, 31));
        var dailySpecs = await db.SalaryTemplates
            .Where(t => t.Id == tpl.Id)
            .SelectMany(t => t.Components)
            .Select(tc => new FormulaComponentSpec(
                tc.SalaryComponentId, tc.SalaryComponent.ComponentCode, tc.SalaryComponent.ComponentName,
                tc.SalaryComponent.ComponentType, tc.SalaryComponent.CalculationType, tc.SalaryComponent.DefaultAmount,
                tc.SalaryComponent.FormulaExpression, tc.SortOrder, tc.SalaryComponent.IsTaxable,
                tc.SalaryComponent.TaxExemptLimit, tc.SalaryComponent.TaxExemptLimitParameterCode,
                tc.SalaryComponent.IsSocialInsuranceBase, tc.SalaryComponent.ShowOnPayslip))
            .ToListAsync();

        var dailySlices = MidMonthSalarySplitter.BuildPeriodSlices(
            new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31),
            20m, 0m, 0m, 0m, 0m,
            new[] { profiles[1] },
            new Dictionary<int, IReadOnlyList<FormulaComponentSpec>> { [tpl.Id] = dailySpecs });

        var dailyAgg = VietnamPayrollEngine.CalculateFromMidMonthSlices(
            dailySlices, ctxAug, 26m, 20m, 0m, 0m, 0m, 0, 2);
        dailyAgg.Breakdown.ActualBaseSalary.Should().Be(12_000_000m);
        dailyAgg.Breakdown.CompanyUnionFee.Should().Be(0m);

        // 4. Không cho ghi đè lùi vào kỳ lương đã Locked
        db.PayrollPeriods.Add(new PayrollPeriod
        {
            PeriodCode = "PAY-2026-07", Month = 7, Year = 2026,
            StartDate = new DateOnly(2026, 7, 1), EndDate = new DateOnly(2026, 7, 31),
            StandardWorkdays = 26m, Status = PayrollPeriodStatuses.Locked, IsLocked = true
        });
        await db.SaveChangesAsync();

        Func<Task> backdateLocked = () => handler.Handle(new SaveEmployeeSalaryProfileCommand(
            1, new DateOnly(2026, 7, 15), 22_000_000m, 22_000_000m, null, tpl.Id,
            "Monthly", 0m, false, 100m, 0m, 0m, true), CancellationToken.None);
        await backdateLocked.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*khóa*");
    }

    [Fact]
    public async Task SeededTemplates_FactoryAndSales_ShouldEvaluateAllComponentsWithAccurateMath()
    {
        await using var db = await CreateSeededDbContextAsync();
        var ctx = await VietnamPayrollEngine.ResolveStatutoryContextAsync(db, new DateOnly(2026, 7, 31));

        var templates = await db.SalaryTemplates
            .Include(t => t.Components)
                .ThenInclude(tc => tc.SalaryComponent)
            .ToListAsync();
        templates.Should().Contain(t => t.TemplateCode == "OFFICE_STD");
        templates.Should().Contain(t => t.TemplateCode == "FACTORY_SHIFT");
        templates.Should().Contain(t => t.TemplateCode == "SALES_COMMISSION");

        // Kiểm tra FACTORY_SHIFT: LCB 20.000.000đ, 26/26 công, 16 giờ đêm
        // -> HAZARD_ALLOWANCE = 20.000.000 * 5% = 1.000.000 đ
        // -> NIGHT_ALLOWANCE = Round((20.000.000 / 208) * 16 * 0.30, 0) = 461.538 đ (miễn thuế)
        var factoryTpl = templates.Single(t => t.TemplateCode == "FACTORY_SHIFT");
        var factorySpecs = factoryTpl.Components
            .Select(tc => new FormulaComponentSpec(
                tc.SalaryComponentId, tc.SalaryComponent.ComponentCode, tc.SalaryComponent.ComponentName,
                tc.SalaryComponent.ComponentType, tc.SalaryComponent.CalculationType, tc.SalaryComponent.DefaultAmount,
                tc.SalaryComponent.FormulaExpression, tc.SortOrder, tc.SalaryComponent.IsTaxable,
                tc.SalaryComponent.TaxExemptLimit, tc.SalaryComponent.TaxExemptLimitParameterCode,
                tc.SalaryComponent.IsSocialInsuranceBase, tc.SalaryComponent.ShowOnPayslip))
            .ToList();

        var profile = new EmployeeSalaryProfile
        {
            EmployeeId = 99, EffectiveFrom = new DateOnly(2026, 7, 1),
            BaseSalary = 20_000_000m, InsuranceSalary = 20_000_000m,
            SalaryTemplateId = factoryTpl.Id, PayMode = "Monthly", IsUnionMember = true
        };

        var slice = new PayrollPeriodProfileSliceInput(
            new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31),
            26m, 0m, 0m, 0m, 16m, profile, factorySpecs);

        var agg = VietnamPayrollEngine.CalculateFromMidMonthSlices(
            new[] { slice }, ctx, 26m, 26m, 0m, 0m, 0m, 0, 1);

        agg.AggregatedLines.Single(l => l.ComponentCode == "HAZARD_ALLOWANCE").FinalAmount.Should().Be(1_000_000m);
        agg.AggregatedLines.Single(l => l.ComponentCode == "NIGHT_ALLOWANCE").FinalAmount.Should().Be(461_538m);
        agg.TaxExemptAmount.Should().Be(461_538m);
        agg.Breakdown.GrossIncome.Should().Be(21_461_538m);

        // Kiểm tra SALES_COMMISSION: HOA_HONG_DOANH_SO = 10.000.000 * DIEM_KPI (1.0 mặc định) = 10.000.000 đ
        var salesTpl = templates.Single(t => t.TemplateCode == "SALES_COMMISSION");
        var salesSpecs = salesTpl.Components
            .Select(tc => new FormulaComponentSpec(
                tc.SalaryComponentId, tc.SalaryComponent.ComponentCode, tc.SalaryComponent.ComponentName,
                tc.SalaryComponent.ComponentType, tc.SalaryComponent.CalculationType, tc.SalaryComponent.DefaultAmount,
                tc.SalaryComponent.FormulaExpression, tc.SortOrder, tc.SalaryComponent.IsTaxable,
                tc.SalaryComponent.TaxExemptLimit, tc.SalaryComponent.TaxExemptLimitParameterCode,
                tc.SalaryComponent.IsSocialInsuranceBase, tc.SalaryComponent.ShowOnPayslip))
            .ToList();

        var vars = NCalcSalaryFormulaEngine.BuildDefaultSampleVariables();
        vars["LUONG_CO_BAN_HIEN_TAI"] = 18_000_000m;
        vars["HOA_HONG_DOANH_SO"] = 10_000_000m;
        vars["DIEM_KPI"] = 1.2m;
        vars["OT150"] = 0m;
        var salesBatch = NCalcSalaryFormulaEngine.EvaluateComponents(salesSpecs, vars, "RoundToDong");
        salesBatch.Lines.Single(l => l.ComponentCode == "SALES_COMMISSION").FinalAmount.Should().Be(12_000_000m);
    }
}
