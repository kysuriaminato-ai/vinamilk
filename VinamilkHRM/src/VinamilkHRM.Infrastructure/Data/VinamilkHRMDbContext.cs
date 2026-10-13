using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Assets;
using VinamilkHRM.Domain.Entities.Attendances;
using VinamilkHRM.Domain.Entities.Auditing;
using VinamilkHRM.Domain.Entities.Contracts;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Entities.Jobs;
using VinamilkHRM.Domain.Entities.Kpis;
using VinamilkHRM.Domain.Entities.LeaveRequests;
using VinamilkHRM.Domain.Entities.Lookups;
using VinamilkHRM.Domain.Entities.OrganizationalUnits;
using VinamilkHRM.Domain.Entities.Salaries;
using VinamilkHRM.Domain.Entities.Shifts;
using VinamilkHRM.Domain.Entities.Statutory;
using VinamilkHRM.Domain.Entities.Workflows;
using VinamilkHRM.Domain.Entities.Notifications;
using VinamilkHRM.Infrastructure.Identity;

namespace VinamilkHRM.Infrastructure.Data;

/// <summary>
/// DbContext chính của Hệ thống Vinamilk Enterprise HRM kế thừa ASP.NET Core Identity.
/// Quản lý xác thực, phân quyền và toàn bộ thực thể nghiệp vụ nhân sự.
/// </summary>
public class VinamilkHRMDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>, IApplicationDbContext
{
    private readonly VinamilkHRM.Domain.Interfaces.IRegionScopeService? _regionScopeService;

    public VinamilkHRMDbContext(
        DbContextOptions<VinamilkHRMDbContext> options,
        VinamilkHRM.Domain.Interfaces.IRegionScopeService? regionScopeService = null) : base(options)
    {
        _regionScopeService = regionScopeService;
    }

    public DbSet<OrganizationalUnit> OrganizationalUnits => Set<OrganizationalUnit>();
    public DbSet<HeadquartersUnit> HeadquartersUnits => Set<HeadquartersUnit>();
    public DbSet<FactoryUnit> FactoryUnits => Set<FactoryUnit>();
    public DbSet<DairyFarmUnit> DairyFarmUnits => Set<DairyFarmUnit>();
    public DbSet<RetailStoreUnit> RetailStoreUnits => Set<RetailStoreUnit>();
    public DbSet<DivisionUnit> DivisionUnits => Set<DivisionUnit>();
    public DbSet<DepartmentUnit> DepartmentUnits => Set<DepartmentUnit>();
    public DbSet<ProductionLineUnit> ProductionLineUnits => Set<ProductionLineUnit>();
    public DbSet<FarmTeamUnit> FarmTeamUnits => Set<FarmTeamUnit>();
    public DbSet<BranchUnit> BranchUnits => Set<BranchUnit>();

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<OfficeEmployee> OfficeEmployees => Set<OfficeEmployee>();
    public DbSet<FactoryWorker> FactoryWorkers => Set<FactoryWorker>();
    public DbSet<FarmSpecialist> FarmSpecialists => Set<FarmSpecialist>();
    public DbSet<StoreAssociate> StoreAssociates => Set<StoreAssociate>();

    public DbSet<LaborContract> LaborContracts => Set<LaborContract>();
    public DbSet<ProbationContract> ProbationContracts => Set<ProbationContract>();
    public DbSet<DefiniteContract> DefiniteContracts => Set<DefiniteContract>();
    public DbSet<IndefiniteContract> IndefiniteContracts => Set<IndefiniteContract>();
    public DbSet<ElectronicContractAppendix> ElectronicContractAppendices => Set<ElectronicContractAppendix>();
    public DbSet<ContractAllowance> ContractAllowances => Set<ContractAllowance>();

    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<KpiAssignment> KpiAssignments => Set<KpiAssignment>();
    // Phase 7 Total Rewards & Salary Review DbSets
    // Phase 8 People Analytics & Reporting DbSets
    public DbSet<VinamilkHRM.Domain.Entities.Analytics.WorkforceSnapshot> WorkforceSnapshots => Set<VinamilkHRM.Domain.Entities.Analytics.WorkforceSnapshot>();
    public DbSet<VinamilkHRM.Domain.Entities.Analytics.ExitSurvey> ExitSurveys => Set<VinamilkHRM.Domain.Entities.Analytics.ExitSurvey>();
    public DbSet<VinamilkHRM.Domain.Entities.Analytics.MarketSalaryBenchmark> MarketSalaryBenchmarks => Set<VinamilkHRM.Domain.Entities.Analytics.MarketSalaryBenchmark>();
    public DbSet<VinamilkHRM.Domain.Entities.Analytics.MacroIndicator> MacroIndicators => Set<VinamilkHRM.Domain.Entities.Analytics.MacroIndicator>();
    public DbSet<VinamilkHRM.Domain.Entities.Analytics.CompanyMetric> CompanyMetrics => Set<VinamilkHRM.Domain.Entities.Analytics.CompanyMetric>();
    public DbSet<VinamilkHRM.Domain.Entities.Analytics.AiAuditLog> AiAuditLogs => Set<VinamilkHRM.Domain.Entities.Analytics.AiAuditLog>();
    public DbSet<SalaryBand> SalaryBands => Set<SalaryBand>();
    public DbSet<SalaryReviewCycle> SalaryReviewCycles => Set<SalaryReviewCycle>();
    public DbSet<SalaryBudget> SalaryBudgets => Set<SalaryBudget>();
    public DbSet<SalaryAdjustmentApprovalStep> SalaryAdjustmentApprovalSteps => Set<SalaryAdjustmentApprovalStep>();
    public DbSet<SalaryAdjustment> SalaryAdjustments => Set<SalaryAdjustment>();
    public DbSet<SalaryProposal> SalaryProposals => Set<SalaryProposal>();
    public DbSet<PaySlip> PaySlips => Set<PaySlip>();
    public DbSet<PayrollRun> PayrollRuns => Set<PayrollRun>();
    public DbSet<PayrollAdjustment> PayrollAdjustments => Set<PayrollAdjustment>();
    public DbSet<CompanyAsset> CompanyAssets => Set<CompanyAsset>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<DisciplinaryWarning> DisciplinaryWarnings => Set<DisciplinaryWarning>();
    public DbSet<VinamilkHRM.Domain.Entities.Careers.JobPosting> JobPostings => Set<VinamilkHRM.Domain.Entities.Careers.JobPosting>();
    public DbSet<VinamilkHRM.Domain.Entities.Careers.CandidateApplication> CandidateApplications => Set<VinamilkHRM.Domain.Entities.Careers.CandidateApplication>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Phase 2 DbSets
    public DbSet<StatutoryParameter> StatutoryParameters => Set<StatutoryParameter>();
    public DbSet<MinimumWage> MinimumWages => Set<MinimumWage>();
    public DbSet<TaxBracket> TaxBrackets => Set<TaxBracket>();
    public DbSet<InsuranceRate> InsuranceRates => Set<InsuranceRate>();
    public DbSet<OvertimeRate> OvertimeRates => Set<OvertimeRate>();
    public DbSet<PublicHoliday> PublicHolidays => Set<PublicHoliday>();

    public DbSet<JobFamily> JobFamilies => Set<JobFamily>();
    public DbSet<JobGrade> JobGrades => Set<JobGrade>();
    public DbSet<JobTitle> JobTitles => Set<JobTitle>();
    public DbSet<SalaryScale> SalaryScales => Set<SalaryScale>();

    public DbSet<LookupCategory> LookupCategories => Set<LookupCategory>();
    public DbSet<LookupItem> LookupItems => Set<LookupItem>();

    public DbSet<AllowanceType> AllowanceTypes => Set<AllowanceType>();
    public DbSet<Dependent> Dependents => Set<Dependent>();
    public DbSet<EmployeeEducation> EmployeeEducations => Set<EmployeeEducation>();
    public DbSet<EmployeeCertificate> EmployeeCertificates => Set<EmployeeCertificate>();
    public DbSet<EmployeeWorkHistory> EmployeeWorkHistories => Set<EmployeeWorkHistory>();
    public DbSet<EmployeeAttachment> EmployeeAttachments => Set<EmployeeAttachment>();

    public DbSet<UnitManagerHistory> UnitManagerHistories => Set<UnitManagerHistory>();
    public DbSet<UnitMinimumWageRegionHistory> UnitMinimumWageRegionHistories => Set<UnitMinimumWageRegionHistory>();

    // Phase 3 Time & Attendance DbSets
    public DbSet<ShiftDefinition> ShiftDefinitions => Set<ShiftDefinition>();
    public DbSet<ShiftSegment> ShiftSegments => Set<ShiftSegment>();
    public DbSet<Crew> Crews => Set<Crew>();
    public DbSet<CrewMember> CrewMembers => Set<CrewMember>();
    public DbSet<RotationPattern> RotationPatterns => Set<RotationPattern>();
    public DbSet<CrewRotation> CrewRotations => Set<CrewRotation>();
    public DbSet<RosterEntry> RosterEntries => Set<RosterEntry>();
    public DbSet<TimeEvent> TimeEvents => Set<TimeEvent>();
    public DbSet<DailyAttendance> DailyAttendances => Set<DailyAttendance>();
    public DbSet<AttendanceSegment> AttendanceSegments => Set<AttendanceSegment>();
    public DbSet<OvertimeRequest> OvertimeRequests => Set<OvertimeRequest>();
    public DbSet<ShiftSwapRequest> ShiftSwapRequests => Set<ShiftSwapRequest>();
    public DbSet<AttendanceExplanation> AttendanceExplanations => Set<AttendanceExplanation>();
    public DbSet<MonthlyTimesheetPeriod> MonthlyTimesheetPeriods => Set<MonthlyTimesheetPeriod>();
    public DbSet<MonthlyTimesheetSummary> MonthlyTimesheetSummaries => Set<MonthlyTimesheetSummary>();
    public DbSet<AttendanceAdjustment> AttendanceAdjustments => Set<AttendanceAdjustment>();

    // Phase 4 Performance Management (KPI) DbSets
    public DbSet<KpiDefinition> KpiDefinitions => Set<KpiDefinition>();
    public DbSet<KpiThreshold> KpiThresholds => Set<KpiThreshold>();
    public DbSet<KpiScheme> KpiSchemes => Set<KpiScheme>();
    public DbSet<KpiPeriod> KpiPeriods => Set<KpiPeriod>();
    public DbSet<KpiSheet> KpiSheets => Set<KpiSheet>();
    public DbSet<KpiItem> KpiItems => Set<KpiItem>();
    public DbSet<KpiExtensionRequest> KpiExtensionRequests => Set<KpiExtensionRequest>();
    public DbSet<KpiResult> KpiResults => Set<KpiResult>();
    public DbSet<KpiAppeal> KpiAppeals => Set<KpiAppeal>();
    public DbSet<KpiMetric> KpiMetrics => Set<KpiMetric>();
    public DbSet<KpiTemplate> KpiTemplates => Set<KpiTemplate>();
    public DbSet<KpiTemplateItem> KpiTemplateItems => Set<KpiTemplateItem>();
    public DbSet<KpiConfiguration> KpiConfigurations => Set<KpiConfiguration>();

    // Workflow Engine DbSets (Session 11 P1-1)
    public DbSet<WorkflowDefinition> WorkflowDefinitions => Set<WorkflowDefinition>();
    public DbSet<WorkflowStepDefinition> WorkflowStepDefinitions => Set<WorkflowStepDefinition>();
    public DbSet<WorkflowInstance> WorkflowInstances => Set<WorkflowInstance>();
    public DbSet<WorkflowStepInstance> WorkflowStepInstances => Set<WorkflowStepInstance>();
    public DbSet<ApprovalDelegation> ApprovalDelegations => Set<ApprovalDelegation>();

    // Notification System DbSets (Session 11 P1-2)
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<InAppNotification> InAppNotifications => Set<InAppNotification>();
    public DbSet<NotificationOutboxMessage> NotificationOutboxMessages => Set<NotificationOutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VinamilkHRMDbContext).Assembly);

        // Cấu hình RowVersion concurrency token cho tất cả root entities kế thừa BaseEntity
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType) && entityType.BaseType == null)
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property<byte[]>(nameof(BaseEntity.RowVersion))
                    .IsRowVersion();
            }
        }

        // Global Query Filter: Regional Scoping & Multi-Unit
        if (_regionScopeService != null)
        {
            modelBuilder.Entity<OrganizationalUnit>().HasQueryFilter(u =>
                !_regionScopeService.IsFilterActive ||
                ((_regionScopeService.CurrentRegionId == null || u.RegionId == _regionScopeService.CurrentRegionId) &&
                 (_regionScopeService.CurrentUnitType == null || u.UnitType == _regionScopeService.CurrentUnitType) &&
                 (_regionScopeService.CurrentUnitId == null || u.Id == _regionScopeService.CurrentUnitId))
            );

            modelBuilder.Entity<Employee>().HasQueryFilter(e =>
                !_regionScopeService.IsFilterActive ||
                ((_regionScopeService.CurrentRegionId == null || (e.OrganizationalUnit != null && e.OrganizationalUnit.RegionId == _regionScopeService.CurrentRegionId)) &&
                 (_regionScopeService.CurrentUnitType == null || (e.OrganizationalUnit != null && e.OrganizationalUnit.UnitType == _regionScopeService.CurrentUnitType)) &&
                 (_regionScopeService.CurrentUnitId == null || e.OrganizationalUnitId == _regionScopeService.CurrentUnitId))
            );
        }
    }
}


