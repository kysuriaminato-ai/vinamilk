using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Vinamilk.HRM.Application.Talent;
using Vinamilk.HRM.Domain.Entities.Core;
using Vinamilk.HRM.Domain.Entities.Talent;
using Vinamilk.HRM.Infrastructure.Persistence;
using Vinamilk.HRM.Infrastructure.Security;
using Xunit;

namespace Vinamilk.HRM.UnitTests.Talent;

public class TalentHandlersTests
{
    private static (ApplicationDbContext Context, PiiEncryptionService Encryption) CreateTestEnv()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:PiiEncryptionKey"] = "TalentTestKey_32Chars_Vinamilk!"
            })
            .Build();

        return (new ApplicationDbContext(options), new PiiEncryptionService(config));
    }

    [Fact]
    public async Task RecruitmentAtsPipeline_ShouldCoverRequestToEmployeeConversion()
    {
        var (context, encryption) = CreateTestEnv();

        var unit = new OrganizationUnit { UnitCode = "TECH", UnitName = "Khối CNTT", UnitLevel = 1, TreePath = "/1/" };
        var pos = new Position { PositionCode = "DEV", PositionName = "Lập trình viên", PositionLevel = 5 };
        await context.AddRangeAsync(unit, pos);
        await context.SaveChangesAsync();

        var interviewer = new Employee
        {
            EmployeeCode = "VNM010",
            FullName = "Tech Lead",
            OrganizationUnitId = unit.Id,
            PositionId = pos.Id,
            WorkStatus = "Active"
        };
        await context.Employees.AddAsync(interviewer);
        await context.SaveChangesAsync();

        var reqId = await new CreateRecruitmentRequestCommandHandler(context).Handle(
            new CreateRecruitmentRequestCommand(
                unit.Id, pos.Id, 2, "Expansion", 20_000_000m, 35_000_000m, DateOnly.FromDateTime(DateTime.Today.AddMonths(1)), ".NET 8"),
            CancellationToken.None);

        var approveResult = await new ApproveRecruitmentRequestCommandHandler(context)
            .Handle(new ApproveRecruitmentRequestCommand(reqId, true), CancellationToken.None);
        approveResult.Should().Contain("Đã");

        var postingId = await new CreateJobPostingCommandHandler(context).Handle(
            new CreateJobPostingCommand(
                reqId,
                "Senior .NET Developer",
                unit.Id,
                pos.Id,
                2,
                "20 - 35 triệu",
                "TP.HCM",
                DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
                "Xây dựng HRM",
                "C# .NET 8",
                "Thưởng tháng 13"),
            CancellationToken.None);

        var candidateCode = await new ApplyCandidateCommandHandler(context).Handle(
            new ApplyCandidateCommand(
                postingId,
                "Phạm Hoàng Nam",
                new DateOnly(1996, 8, 20),
                "Male",
                "0912345678",
                "nam.ph@example.com",
                "Kỹ thuật Phần mềm",
                5m,
                28_000_000m),
            CancellationToken.None);
        candidateCode.Should().NotBeNullOrWhiteSpace();

        var candidate = await context.Candidates.FirstAsync();
        var moved = await new MoveCandidateStageCommandHandler(context)
            .Handle(new MoveCandidateStageCommand(candidate.Id, "Interview"), CancellationToken.None);
        moved.Should().BeTrue();

        var interviewId = await new ScheduleAndEvaluateInterviewCommandHandler(context).Handle(
            new ScheduleAndEvaluateInterviewCommand(
                candidate.Id,
                1,
                "Phỏng vấn Chuyên môn",
                DateTime.Now.AddDays(1),
                "Online",
                "https://meet.vinamilk.local/123",
                interviewer.Id,
                9.0m,
                8.5m,
                "Ứng viên tốt",
                "Passed"),
            CancellationToken.None);
        interviewId.Should().BeGreaterThan(0);

        var offerId = await new CreateJobOfferCommandHandler(context).Handle(
            new CreateJobOfferCommand(
                candidate.Id,
                23_000_000m,
                28_000_000m,
                DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
                DateOnly.FromDateTime(DateTime.Today.AddDays(3))),
            CancellationToken.None);
        offerId.Should().BeGreaterThan(0);

        var empCode = await new ConvertCandidateToEmployeeCommandHandler(context, encryption)
            .Handle(new ConvertCandidateToEmployeeCommand(candidate.Id), CancellationToken.None);
        empCode.Should().Contain("VNM-");
        (await context.Employees.CountAsync()).Should().Be(2);

        var atsOverview = await new GetAtsOverviewQueryHandler(context)
            .Handle(new GetAtsOverviewQuery(postingId), CancellationToken.None);
        atsOverview.JobPostings.Should().HaveCount(1);

        var careers = await new GetPublicCareersQueryHandler(context)
            .Handle(new GetPublicCareersQuery("Developer", unit.Id), CancellationToken.None);
        careers.OpenPostings.Should().HaveCount(1);
    }

    [Fact]
    public async Task TrainingHandlers_ShouldManageCoursesClassesEnrollmentsAndOverview()
    {
        var (context, _) = CreateTestEnv();

        var unit = new OrganizationUnit { UnitCode = "HR", UnitName = "Nhân sự", UnitLevel = 1, TreePath = "/1/" };
        var pos = new Position { PositionCode = "SPEC", PositionName = "Chuyên viên", PositionLevel = 5 };
        await context.AddRangeAsync(unit, pos);
        await context.SaveChangesAsync();

        var emp = new Employee { EmployeeCode = "VNM301", FullName = "Đỗ Thu Hà", OrganizationUnitId = unit.Id, PositionId = pos.Id, WorkStatus = "Active" };
        await context.Employees.AddAsync(emp);
        await context.SaveChangesAsync();

        var courseId = await new CreateTrainingCourseCommandHandler(context).Handle(
            new CreateTrainingCourseCommand("SEC101", "An toàn thông tin", "Compliance", 8, 500_000m, 6, "Bắt buộc"), CancellationToken.None);
        var classId = await new CreateTrainingClassCommandHandler(context).Handle(
            new CreateTrainingClassCommand(courseId, "Lớp K1", DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today.AddDays(2)), "Chuyên gia A", "Hội trường B", 30, 10_000_000m), CancellationToken.None);
        var enrollmentId = await new EnrollEmployeeToClassCommandHandler(context).Handle(
            new EnrollEmployeeToClassCommand(classId, emp.Id), CancellationToken.None);
        var evalMsg = await new EvaluateTrainingEnrollmentCommandHandler(context).Handle(
            new EvaluateTrainingEnrollmentCommand(enrollmentId, 100m, 92m, 5, "Khóa học hữu ích"), CancellationToken.None);
        evalMsg.Should().NotBeNullOrWhiteSpace();

        var overview = await new GetTrainingOverviewQueryHandler(context)
            .Handle(new GetTrainingOverviewQuery(DateTime.Today.Year), CancellationToken.None);
        overview.Courses.Should().HaveCount(1);
        overview.Classes.Should().HaveCount(1);
    }

    [Fact]
    public async Task KpiRewardsOnboardingAndAssetHandlers_ShouldExecuteLifecycleFlows()
    {
        var (context, _) = CreateTestEnv();

        var unit = new OrganizationUnit { UnitCode = "SLS", UnitName = "Kinh doanh", UnitLevel = 1, TreePath = "/1/" };
        var pos = new Position { PositionCode = "SM", PositionName = "Quản lý KV", PositionLevel = 4 };
        await context.AddRangeAsync(unit, pos);
        await context.SaveChangesAsync();

        var emp = new Employee { EmployeeCode = "VNM401", FullName = "Vũ Gia Bảo", OrganizationUnitId = unit.Id, PositionId = pos.Id, WorkStatus = "Active" };
        var rewardCat = new RewardDisciplineCategory { CategoryCode = "KT_XS", CategoryName = "Nhân viên xuất sắc", CategoryType = "Reward", DefaultAmount = 3_000_000m };
        var tpl = new OnboardingTemplate { ProcessType = "Onboarding", TaskName = "Cấp laptop và email", AssignedDepartmentRole = "IT", DueDaysOffset = 1, SortOrder = 1 };
        var asset = new Asset { AssetCode = "LAP-001", AssetName = "ThinkPad X1", AssetCategory = "Laptop", SerialNumber = "SN12345", PurchaseValue = 32_000_000m, Status = "Available" };
        await context.AddRangeAsync(emp, rewardCat, tpl, asset);
        await context.SaveChangesAsync();

        // KPI flow
        var cycleId = await new CreateEvaluationCycleCommandHandler(context).Handle(
            new CreateEvaluationCycleCommand("KPI2026Q1", "Quý 1/2026", "Quarterly", 2026, 1, new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31)), CancellationToken.None);
        var evalId = await new CreateEmployeeKpiCommandHandler(context).Handle(
            new CreateEmployeeKpiCommand(cycleId, emp.Id, null, "Doanh thu quý", "Financial", "Tỷ đồng", 10m, 11m, 95m, "Đạt vượt mức"), CancellationToken.None);
        var calibrated = await new CalibrateAndApproveKpiCommandHandler(context).Handle(
            new CalibrateAndApproveKpiCommand(evalId, 92m, 94m, "Xuất sắc"), CancellationToken.None);
        calibrated.Should().NotBeNullOrWhiteSpace();
        var kpiOverview = await new GetKpiOverviewQueryHandler(context).Handle(new GetKpiOverviewQuery(cycleId), CancellationToken.None);
        kpiOverview.Evaluations.Should().HaveCount(1);

        // Reward/Discipline flow
        var rdId = await new CreateRewardDisciplineCommandHandler(context).Handle(
            new CreateRewardDisciplineCommand(emp.Id, rewardCat.Id, DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today), 3_000_000m, "Vượt doanh số", true), CancellationToken.None);
        var rdDetail = await new GetRewardDisciplinePrintDetailQueryHandler(context).Handle(new GetRewardDisciplinePrintDetailQuery(rdId), CancellationToken.None);
        rdDetail.Should().NotBeNull();
        var rdOverview = await new GetRewardsAndBm03OverviewQueryHandler(context).Handle(new GetRewardsAndBm03OverviewQuery(DateTime.Today.Year), CancellationToken.None);
        rdOverview.Items.Should().HaveCount(1);

        // Onboarding & Asset flow
        var taskCount = await new GenerateEmployeeChecklistFromTemplateCommandHandler(context).Handle(
            new GenerateEmployeeChecklistFromTemplateCommand(emp.Id, "Onboarding", emp.Id), CancellationToken.None);
        taskCount.Should().Be(1);
        var task = await context.EmployeeOnboardingTasks.FirstAsync();
        (await new ToggleOnboardingTaskCommandHandler(context).Handle(new ToggleOnboardingTaskCommand(task.Id), CancellationToken.None)).Should().BeTrue();

        var assignMsg = await new AssignAssetToEmployeeCommandHandler(context).Handle(new AssignAssetToEmployeeCommand(asset.Id, emp.Id, "Mới 100%"), CancellationToken.None);
        assignMsg.Should().Contain("LAP-001");
        var returnMsg = await new ReturnAssetCommandHandler(context).Handle(new ReturnAssetCommand(asset.Id, "Nguyên vẹn"), CancellationToken.None);
        returnMsg.Should().Contain("LAP-001");

        var onboardOverview = await new GetOnboardingAndAssetOverviewQueryHandler(context).Handle(new GetOnboardingAndAssetOverviewQuery(), CancellationToken.None);
        onboardOverview.Assets.Should().HaveCount(1);
    }
}
