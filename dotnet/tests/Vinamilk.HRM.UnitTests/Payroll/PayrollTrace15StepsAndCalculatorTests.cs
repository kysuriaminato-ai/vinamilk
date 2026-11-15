using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Vinamilk.HRM.Application.Payroll;
using Vinamilk.HRM.Domain.Entities.Core;
using Vinamilk.HRM.Domain.Entities.Payroll;
using Vinamilk.HRM.Infrastructure.Persistence;
using Vinamilk.HRM.Infrastructure.Persistence.Interceptors;
using Vinamilk.HRM.Infrastructure.Persistence.Seed;
using Vinamilk.HRM.Infrastructure.Security;
using Xunit;

namespace Vinamilk.HRM.UnitTests.Payroll;

public class PayrollTrace15StepsAndCalculatorTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var interceptor = new AuditableEntityInterceptor(new FakeCurrentUserService());
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"PayrollTrace15_{Guid.NewGuid():N}")
            .AddInterceptors(interceptor)
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task BuildCalculationTrace_Nov2026_ShouldReturnExact15StepsMatchingBreakdown()
    {
        await using var db = CreateDbContext();
        await db.PayrollStatutoryParameters.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultStatutoryParameters());
        await db.PitBrackets.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultPitBrackets());
        await db.SaveChangesAsync();

        var ctx = await VietnamPayrollEngine.ResolveStatutoryContextAsync(db, new DateOnly(2026, 11, 30));
        var input = new PayrollCalculationInput(
            ContractBaseSalary: 25_000_000m,
            PositionAllowance: 3_000_000m,
            OtherAllowance: 1_500_000m,
            StandardWorkdays: 21m,
            PayableWorkdays: 20m, // 1 unpaid missing day
            OtHours150: 4m,
            OtHours200: 2m,
            OtHours300: 0m,
            KpiBonus: 5_000_000m,
            DisciplineDeduction: 200_000m,
            DependentCount: 1,
            IsUnionMember: true,
            WageRegion: 1,
            ExtraTaxExemptAmount: 730_000m);

        var breakdown = VietnamPayrollEngine.Calculate(input, ctx);
        var slices = VietnamPayrollEngine.CalculateProgressivePitBreakdown(breakdown.AssessableIncome, ctx.PitBrackets);
        var steps = VietnamPayrollEngine.BuildCalculationTrace(input, breakdown, ctx.Parameter, slices);

        steps.Should().HaveCount(15);
        steps.Select(s => s.StepOrder).Should().Equal(Enumerable.Range(1, 15));

        steps[0].ResultAmount.Should().Be(25_000_000m); // 1. LCB hợp đồng
        steps[1].ResultAmount.Should().Be(breakdown.ActualBaseSalary); // 2. Lương theo công
        steps[2].ResultAmount.Should().Be(4_500_000m); // 3. Phụ cấp
        steps[3].ResultAmount.Should().BeGreaterThan(0m); // 4. Khấu trừ công thiếu (1 ngày)
        steps[4].ResultAmount.Should().Be(breakdown.OvertimePay); // 5. OT
        steps[5].ResultAmount.Should().Be(5_000_000m); // 6. Thưởng KPI
        steps[6].ResultAmount.Should().Be(breakdown.GrossIncome); // 7. Gross
        steps[7].ResultAmount.Should().Be(breakdown.SocialInsuranceBase); // 8. Nền BH
        steps[7].SecondaryAmount.Should().Be(breakdown.UnemploymentInsuranceBase);
        steps[8].ResultAmount.Should().Be(breakdown.TotalEmployeeInsurance); // 9. BH NLĐ
        steps[9].ResultAmount.Should().Be(breakdown.OtTaxExemptAmount + 730_000m); // 10. Miễn thuế
        steps[10].ResultAmount.Should().Be(breakdown.TaxableIncome); // 11. TN chịu thuế
        steps[11].ResultAmount.Should().Be(15_500_000m + 6_200_000m); // 12. Giảm trừ gia cảnh 2026
        steps[12].ResultAmount.Should().Be(breakdown.PersonalIncomeTax); // 13. Thuế TNCN
        steps[12].SecondaryAmount.Should().Be(breakdown.AssessableIncome);
        steps[13].ResultAmount.Should().Be(200_000m + breakdown.EmployeeUnionFee); // 14. Khấu trừ khác
        steps[14].ResultAmount.Should().Be(breakdown.TotalCompanyCost); // 15. Net & Chi phí DN
        steps[14].SecondaryAmount.Should().Be(breakdown.NetSalary);
    }

    [Fact]
    public async Task Initializer_ShouldCleanLegacyTaxSelfDeduction_AndRepairPay202608()
    {
        await using var db = CreateDbContext();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:PiiEncryptionKey"] = "RepairTest_Key_32Chars_Vinamilk!",
                ["Security:DevSamplePassword"] = "Vinamilk@2026"
            })
            .Build();

        var initializer = new ApplicationDbContextInitializer(
            NullLogger<ApplicationDbContextInitializer>.Instance,
            db,
            new PiiEncryptionService(config),
            new PasswordSecurityService(),
            new StubHostEnvironment(),
            config);

        await initializer.SeedAsync();

        // 1. Legacy TAX_SELF_DEDUCTION & TAX_DEPENDENT_DEDUCTION must not be seeded
        var legacyCodes = await db.PayrollParameters
            .Where(p => p.ParamCode == "TAX_SELF_DEDUCTION" || p.ParamCode == "TAX_DEPENDENT_DEDUCTION")
            .ToListAsync();
        legacyCodes.Should().BeEmpty();
        (await db.TaxBrackets.CountAsync()).Should().Be(0);

        // 2. Verify fresh PAY-2026-08 VNM-0001 uses 31/08/2026 rules (15.5M/6.2M, 5-bracket PIT, TotalCompanyCost > 0, 15 steps, Details)
        var vnm01 = await db.PayrollRecords
            .Include(r => r.PayrollPeriod)
            .Include(r => r.Employee)
            .Include(r => r.Details)
            .FirstAsync(r => r.PayrollPeriod.PeriodCode == "PAY-2026-08" && r.Employee.EmployeeCode == "VNM-0001");

        vnm01.SelfDeductionAmount.Should().Be(15_500_000m);
        vnm01.TotalCompanyCost.Should().BeGreaterThan(vnm01.GrossIncome);
        vnm01.Details.Should().NotBeEmpty();
        var steps = JsonSerializer.Deserialize<List<CalculationTraceStepDto>>(vnm01.CalculationTraceJson!);
        steps.Should().HaveCount(15);
        var brackets = JsonSerializer.Deserialize<List<PitBracketTaxSliceDto>>(vnm01.PitBracketsJson!);
        brackets.Should().HaveCount(5);

        // 3. Simulate pre-existing legacy DB corruption (TotalCompanyCost = 0, SelfDeduction = 11M, null JSON, Details wiped, legacy row present)
        await db.PayrollParameters.AddAsync(new PayrollParameter
        {
            ParamCode = "TAX_SELF_DEDUCTION",
            ParamName = "Old",
            ParamValue = 11_000_000m,
            ValueUnit = "VND",
            EffectiveFrom = new DateOnly(2020, 7, 1)
        });
        db.PayrollRecordDetails.RemoveRange(vnm01.Details);
        vnm01.TotalCompanyCost = 0m;
        vnm01.SelfDeductionAmount = 11_000_000m;
        vnm01.CalculationTraceJson = null;
        vnm01.PitBracketsJson = null;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        await initializer.EnsureSamplePayroll202608RepairedAsync();

        var repaired = await db.PayrollRecords
            .AsNoTracking()
            .Include(r => r.Details)
            .FirstAsync(r => r.Id == vnm01.Id);
        repaired.SelfDeductionAmount.Should().Be(15_500_000m);
        repaired.TotalCompanyCost.Should().BeGreaterThan(0m);
        repaired.Details.Should().NotBeEmpty();
        JsonSerializer.Deserialize<List<CalculationTraceStepDto>>(repaired.CalculationTraceJson!).Should().HaveCount(15);
        (await db.PayrollParameters.AnyAsync(p => p.ParamCode == "TAX_SELF_DEDUCTION")).Should().BeFalse();
    }

    [Fact]
    public async Task TraceQuery_WhenRecordMissingSavedJson_ShouldRecomputeReferenceNotice_AndAdhocCalculatorMatchesEngine()
    {
        await using var db = CreateDbContext();
        await db.PayrollStatutoryParameters.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultStatutoryParameters());
        await db.PitBrackets.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultPitBrackets());

        var unit = new OrganizationUnit { UnitCode = "HQ", UnitName = "VP", WageRegion = 1, IsActive = true };
        var pos = new Position { PositionCode = "CV", PositionName = "Chuyên viên", IsActive = true };
        await db.OrganizationUnits.AddAsync(unit);
        await db.Positions.AddAsync(pos);
        await db.SaveChangesAsync();

        var emp = new Employee
        {
            EmployeeCode = "VNM-OLD",
            FullName = "Lê Minh Cũ",
            DateOfBirth = new DateOnly(1992, 1, 1),
            Gender = "Nam",
            OrganizationUnitId = unit.Id,
            PositionId = pos.Id,
            HireDate = new DateOnly(2024, 1, 1),
            WorkStatus = "Active"
        };
        var period = new PayrollPeriod
        {
            PeriodCode = "PAY-LEGACY-2026-07",
            Month = 7,
            Year = 2026,
            PeriodTitle = "Tháng 07/2026",
            StartDate = new DateOnly(2026, 7, 1),
            EndDate = new DateOnly(2026, 7, 31),
            StandardWorkdays = 23m
        };
        await db.Employees.AddAsync(emp);
        await db.PayrollPeriods.AddAsync(period);
        await db.SaveChangesAsync();

        var legacyRecord = new PayrollRecord
        {
            PayrollPeriodId = period.Id,
            EmployeeId = emp.Id,
            OrganizationUnitId = unit.Id,
            PositionId = pos.Id,
            StandardWorkdays = 23m,
            PayableWorkdays = 23m,
            ContractBaseSalary = 30_000_000m,
            ActualBaseSalary = 30_000_000m,
            GrossIncome = 30_000_000m,
            TotalEmployeeInsurance = 3_150_000m,
            PersonalIncomeTax = 635_000m,
            NetSalary = 26_215_000m,
            TotalCompanyCost = 37_050_000m,
            TaxResidencyMode = "ResidentStandard",
            CalculationTraceJson = null,
            PitBracketsJson = null
        };
        await db.PayrollRecords.AddAsync(legacyRecord);
        await db.SaveChangesAsync();

        var traceHandler = new GetPayrollRecordTraceQueryHandler(db);
        var trace = await traceHandler.Handle(new GetPayrollRecordTraceQuery(legacyRecord.Id), CancellationToken.None);

        trace.IsRecomputedReference.Should().BeTrue();
        trace.ReferenceNotice.Should().Contain("Tính lại để tham chiếu");
        trace.Steps.Should().HaveCount(15);

        // Verify CalculateAdhocPayrollQuery (/Payroll/Calculator) produces identical Net & 15 steps
        var calcHandler = new CalculateAdhocPayrollQueryHandler(db);
        var calcResult = await calcHandler.Handle(new CalculateAdhocPayrollQuery(new AdhocPayrollCalculatorInput(
            ContractBaseSalary: 30_000_000m,
            StandardWorkdays: 23m,
            PayableWorkdays: 23m,
            EffectiveDate: new DateOnly(2026, 7, 31),
            IsUnionMember: false,
            WageRegion: 1)), CancellationToken.None);

        calcResult.Trace.Steps.Should().HaveCount(15);
        calcResult.Breakdown.NetSalary.Should().Be(26_215_000m);
        calcResult.Breakdown.PersonalIncomeTax.Should().Be(635_000m);
    }
}
