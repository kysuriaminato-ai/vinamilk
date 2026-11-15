using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Vinamilk.HRM.Application.CoreHR.ContractsAndTransfers;
using Vinamilk.HRM.Application.CoreHR.Employees;
using Vinamilk.HRM.Application.CoreHR.Organization;
using Vinamilk.HRM.Domain.Entities.Core;
using Vinamilk.HRM.Domain.Entities.Payroll;
using Vinamilk.HRM.Infrastructure.Persistence;
using Vinamilk.HRM.Infrastructure.Security;
using Xunit;

namespace Vinamilk.HRM.UnitTests.CoreHR;

public class CoreHrHandlersTests
{
    private static (ApplicationDbContext Context, PiiEncryptionService Encryption) CreateTestEnv()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:PiiEncryptionKey"] = "CoreHrTestKey_32Chars_Vinamilk!"
            })
            .Build();

        return (new ApplicationDbContext(options), new PiiEncryptionService(config));
    }

    [Fact]
    public async Task OrganizationHandlers_ShouldCreateTreePaths_ReorderUnits_AndManagePositions()
    {
        var (context, _) = CreateTestEnv();

        var saveUnit = new SaveOrganizationUnitCommandHandler(context);
        var rootId = await saveUnit.Handle(new SaveOrganizationUnitCommand(
            null, "HQ", "Trụ sở chính", "Corporation", null, null, "CC00", 100, 1, "10 Tân Trào"), CancellationToken.None);
        var childId = await saveUnit.Handle(new SaveOrganizationUnitCommand(
            null, "IT", "Khối CNTT", "Department", rootId, null, "CC01", 30, 2, "10 Tân Trào"), CancellationToken.None);

        // Update existing unit
        await saveUnit.Handle(new SaveOrganizationUnitCommand(
            childId, "IT", "Khối Công nghệ Thông tin", "Department", rootId, null, "CC01", 35, 2, "Quận 7"), CancellationToken.None);

        var child = await context.OrganizationUnits.FirstAsync(u => u.Id == childId);
        child.UnitLevel.Should().Be(2);
        child.TreePath.Should().Be($"/{rootId}/{childId}/");

        var posHandler = new SavePositionCommandHandler(context);
        var posId = await posHandler.Handle(new SavePositionCommand(
            null, "SE", "Kỹ sư Phần mềm", "Specialist", 5, 3_000_000m, "Phát triển hệ thống"), CancellationToken.None);
        await posHandler.Handle(new SavePositionCommand(
            posId, "SE", "Kỹ sư Phần mềm Senior", "Specialist", 4, 5_000_000m, "Phát triển lõi"), CancellationToken.None);

        await context.Employees.AddAsync(new Employee
        {
            EmployeeCode = "VNM001",
            FullName = "Nguyễn Văn A",
            Gender = "Male",
            OrganizationUnitId = childId,
            PositionId = posId,
            WorkStatus = "Active"
        });
        await context.SaveChangesAsync();

        var reordered = await new ReorderOrganizationUnitsCommandHandler(context)
            .Handle(new ReorderOrganizationUnitsCommand(new[] { childId, rootId }), CancellationToken.None);
        reordered.Should().BeGreaterThan(0);

        var overview = await new GetOrganizationOverviewQueryHandler(context)
            .Handle(new GetOrganizationOverviewQuery(), CancellationToken.None);
        overview.Units.Should().HaveCount(2);
        overview.Units.First(u => u.Id == childId).ActualHeadcount.Should().Be(1);
        overview.Positions.Should().HaveCount(1);
    }

    [Fact]
    public async Task EmployeeHandlers_ShouldSaveEncryptQuery360Profile_AndAddSubEntities()
    {
        var (context, encryption) = CreateTestEnv();

        var unit = new OrganizationUnit { UnitCode = "FIN", UnitName = "Phòng Tài chính", UnitLevel = 1, TreePath = "/1/" };
        var pos = new Position { PositionCode = "ACC", PositionName = "Kế toán trưởng", PositionGroup = "Manager", PositionLevel = 3 };
        var edu = new EducationLevel { LevelCode = "UNI", LevelName = "Đại học", RankOrder = 1 };
        var scale = new SalaryScale { ScaleCode = "CHUYEN_VIEN", ScaleName = "Ngạch Chuyên viên", Steps = [new SalaryStep { StepOrder = 1, StepName = "Bậc 1", StandardSalary = 20_000_000m }] };
        await context.AddRangeAsync(unit, pos, edu, scale);
        await context.SaveChangesAsync();
        var stepId = scale.Steps.First().Id;

        var saveEmp = new SaveEmployeeCommandHandler(context, encryption);
        var empId = await saveEmp.Handle(new SaveEmployeeCommand(
            null,
            "VNM101",
            "Trần Thị Bình",
            "Female",
            new DateOnly(1992, 5, 10),
            "079192000123",
            "0909123456",
            "binh.tt@vinamilk.com.vn",
            "binh@gmail.com",
            "Q1, TP.HCM",
            "Q7, TP.HCM",
            unit.Id,
            pos.Id,
            edu.Id,
            "Kế toán",
            stepId,
            25_000_000m,
            4_000_000m,
            1_000_000m,
            "190020203030",
            "Vietcombank",
            "8392019283",
            "7912345678",
            new DateOnly(2020, 1, 15),
            "Active",
            "Indefinite"), CancellationToken.None);

        // Update existing employee
        await saveEmp.Handle(new SaveEmployeeCommand(
            empId,
            "VNM101",
            "Trần Thị Bình",
            "Female",
            new DateOnly(1992, 5, 10),
            "079192000123",
            "0909999888",
            "binh.tt@vinamilk.com.vn",
            null,
            null,
            null,
            unit.Id,
            pos.Id,
            edu.Id,
            "Kiểm toán",
            stepId,
            28_000_000m,
            4_500_000m,
            1_000_000m,
            "190020203030",
            "Vietcombank",
            "8392019283",
            "7912345678",
            new DateOnly(2020, 1, 15),
            "Active",
            "Indefinite"), CancellationToken.None);

        await new AddEmployeeDependentCommandHandler(context).Handle(
            new AddEmployeeDependentCommand(empId, "Nguyễn An", "Child", new DateOnly(2021, 3, 1), true, false, null), CancellationToken.None);
        await new AddEmployeeCertificateCommandHandler(context).Handle(
            new AddEmployeeCertificateCommand(empId, "ACCA", "Professional", "ACCA Global", new DateOnly(2022, 6, 1), "Pass"), CancellationToken.None);
        await new AddEmployeeDocumentCommandHandler(context).Handle(
            new AddEmployeeDocumentCommand(empId, "Contract", "HDLD.pdf", "/uploads/hdld.pdf", 102400), CancellationToken.None);
        var avatarUpdated = await new UpdateEmployeeAvatarCommandHandler(context).Handle(
            new UpdateEmployeeAvatarCommand(empId, "data:image/png;base64,AAA"), CancellationToken.None);
        avatarUpdated.Should().BeTrue();

        var listResult = await new GetEmployeesAdvancedQueryHandler(context).Handle(
            new GetEmployeesAdvancedQuery(Keyword: "Bình", OrganizationUnitId: unit.Id, MinSalary: 20_000_000m, SortBy: "FullName"), CancellationToken.None);
        listResult.TotalCount.Should().Be(1);

        var profile = await new GetEmployee360ProfileQueryHandler(context, encryption)
            .Handle(new GetEmployee360ProfileQuery(empId), CancellationToken.None);
        profile.Should().NotBeNull();
        profile!.DecryptedCitizenId.Should().Be("079192000123");
        profile.DecryptedBankAccount.Should().Be("190020203030");
        profile.Employee.Dependents.Should().HaveCount(1);
        profile.Employee.Certificates.Should().HaveCount(1);
        profile.Employee.Documents.Should().HaveCount(1);
    }

    [Fact]
    public async Task ContractsAndTransfersHandlers_ShouldHandleAddendumTransferProbationAndOffboarding()
    {
        var (context, _) = CreateTestEnv();

        var unitA = new OrganizationUnit { UnitCode = "U1", UnitName = "Nhà máy 1", UnitLevel = 1, TreePath = "/1/" };
        var unitB = new OrganizationUnit { UnitCode = "U2", UnitName = "Nhà máy 2", UnitLevel = 1, TreePath = "/2/" };
        var pos1 = new Position { PositionCode = "OP", PositionName = "Vận hành", PositionLevel = 6 };
        var pos2 = new Position { PositionCode = "SUP", PositionName = "Giám sát", PositionLevel = 4 };
        await context.AddRangeAsync(unitA, unitB, pos1, pos2);
        await context.SaveChangesAsync();

        var emp = new Employee
        {
            EmployeeCode = "VNM200",
            FullName = "Lê Minh C",
            Gender = "Male",
            OrganizationUnitId = unitA.Id,
            PositionId = pos1.Id,
            BaseSalary = 12_000_000m,
            PositionAllowance = 1_000_000m,
            WorkStatus = "Probation",
            CurrentContractType = "Probation",
            HireDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-2))
        };
        await context.Employees.AddAsync(emp);
        await context.SaveChangesAsync();

        var contract = new LaborContract
        {
            EmployeeId = emp.Id,
            ContractNumber = "HD-2026-001",
            ContractType = "FixedTerm12M",
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-11)),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(15)),
            BaseSalary = 12_000_000m,
            PositionAllowance = 1_000_000m,
            Status = "Active"
        };
        await context.LaborContracts.AddAsync(contract);
        await context.SaveChangesAsync();

        var addendumId = await new CreateContractAddendumCommandHandler(context).Handle(
            new CreateContractAddendumCommand(
                contract.Id,
                "PL-001",
                "SalaryAdjustment",
                DateOnly.FromDateTime(DateTime.Today),
                DateOnly.FromDateTime(DateTime.Today.AddDays(20)),
                15_000_000m,
                2_000_000m,
                "Điều chỉnh lương"), CancellationToken.None);
        addendumId.Should().BeGreaterThan(0);

        var expiringCount = await new ScanExpiringContractsCommandHandler(context)
            .Handle(new ScanExpiringContractsCommand(), CancellationToken.None);
        expiringCount.Should().Be(1);

        var contractsOverview = await new GetContractsOverviewQueryHandler(context)
            .Handle(new GetContractsOverviewQuery(), CancellationToken.None);
        contractsOverview.AllContracts.Should().HaveCount(1);

        // Evaluate Probation -> Official
        var passed = await new EvaluateProbationCommandHandler(context)
            .Handle(new EvaluateProbationCommand(emp.Id, true), CancellationToken.None);
        passed.Should().BeTrue();
        (await context.Employees.FirstAsync(e => e.Id == emp.Id)).WorkStatus.Should().Be("Official");

        // Create & approve personnel transfer
        var transferId = await new CreatePersonnelTransferCommandHandler(context).Handle(
            new CreatePersonnelTransferCommand(
                "QD-DC-001",
                emp.Id,
                "UnitAndPosition",
                unitB.Id,
                pos2.Id,
                DateOnly.FromDateTime(DateTime.Today),
                18_000_000m,
                "Điều động tăng cường",
                ApproveImmediately: false), CancellationToken.None);
        transferId.Should().BeGreaterThan(0);

        var approvedTransfer = await new ApprovePersonnelTransferCommandHandler(context)
            .Handle(new ApprovePersonnelTransferCommand(transferId, true), CancellationToken.None);
        approvedTransfer.Should().BeTrue();

        var empAfterTransfer = await context.Employees.FirstAsync(e => e.Id == emp.Id);
        empAfterTransfer.OrganizationUnitId.Should().Be(unitB.Id);
        empAfterTransfer.PositionId.Should().Be(pos2.Id);
        empAfterTransfer.BaseSalary.Should().Be(15_000_000m); // Updated by salary adjustment addendum

        var lifecycleOverview = await new GetTransfersAndLifecycleQueryHandler(context)
            .Handle(new GetTransfersAndLifecycleQuery(), CancellationToken.None);
        lifecycleOverview.Transfers.Should().HaveCount(1);

        // Offboarding
        var offboarded = await new ProcessOffboardingCommandHandler(context).Handle(
            new ProcessOffboardingCommand(emp.Id, "Resigned", DateOnly.FromDateTime(DateTime.Today), "Nghỉ việc theo nguyện vọng"), CancellationToken.None);
        offboarded.Should().BeTrue();
        (await context.Employees.FirstAsync(e => e.Id == emp.Id)).WorkStatus.Should().Be("Resigned");
    }
}
