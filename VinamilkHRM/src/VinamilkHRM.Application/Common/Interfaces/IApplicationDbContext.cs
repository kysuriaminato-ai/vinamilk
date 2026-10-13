using Microsoft.EntityFrameworkCore;
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
using VinamilkHRM.Domain.Entities.OnboardingOffboarding;

namespace VinamilkHRM.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<OrganizationalUnit> OrganizationalUnits { get; }
    DbSet<Employee> Employees { get; }
    DbSet<LaborContract> LaborContracts { get; }
    DbSet<ElectronicContractAppendix> ElectronicContractAppendices { get; }
    DbSet<ContractAllowance> ContractAllowances { get; }
    DbSet<Shift> Shifts { get; }
    DbSet<Attendance> Attendances { get; }
    DbSet<LeaveRequest> LeaveRequests { get; }
    DbSet<KpiAssignment> KpiAssignments { get; }
    DbSet<SalaryProposal> SalaryProposals { get; }
    DbSet<PaySlip> PaySlips { get; }
    DbSet<PayrollRun> PayrollRuns { get; }
    DbSet<PayrollAdjustment> PayrollAdjustments { get; }
    DbSet<CompanyAsset> CompanyAssets { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<DisciplinaryWarning> DisciplinaryWarnings { get; }
    DbSet<VinamilkHRM.Domain.Entities.Careers.JobPosting> JobPostings { get; }
    DbSet<VinamilkHRM.Domain.Entities.Careers.CandidateApplication> CandidateApplications { get; }

    // Phase 2 Entities
    DbSet<StatutoryParameter> StatutoryParameters { get; }
    DbSet<MinimumWage> MinimumWages { get; }
    DbSet<TaxBracket> TaxBrackets { get; }
    DbSet<InsuranceRate> InsuranceRates { get; }
    DbSet<OvertimeRate> OvertimeRates { get; }
    DbSet<PublicHoliday> PublicHolidays { get; }

    DbSet<JobFamily> JobFamilies { get; }
    DbSet<JobGrade> JobGrades { get; }
    DbSet<JobTitle> JobTitles { get; }
    DbSet<SalaryScale> SalaryScales { get; }

    DbSet<LookupCategory> LookupCategories { get; }
    DbSet<LookupItem> LookupItems { get; }

    DbSet<AllowanceType> AllowanceTypes { get; }
    DbSet<Dependent> Dependents { get; }
    DbSet<EmployeeEducation> EmployeeEducations { get; }
    DbSet<EmployeeCertificate> EmployeeCertificates { get; }
    DbSet<EmployeeWorkHistory> EmployeeWorkHistories { get; }
    DbSet<EmployeeAttachment> EmployeeAttachments { get; }

    DbSet<UnitManagerHistory> UnitManagerHistories { get; }
    DbSet<UnitMinimumWageRegionHistory> UnitMinimumWageRegionHistories { get; }

    // Phase 3 Time & Attendance Entities
    DbSet<ShiftDefinition> ShiftDefinitions { get; }
    DbSet<ShiftSegment> ShiftSegments { get; }
    DbSet<Crew> Crews { get; }
    DbSet<CrewMember> CrewMembers { get; }
    DbSet<RotationPattern> RotationPatterns { get; }
    DbSet<CrewRotation> CrewRotations { get; }
    DbSet<RosterEntry> RosterEntries { get; }
    DbSet<TimeEvent> TimeEvents { get; }
    DbSet<DailyAttendance> DailyAttendances { get; }
    DbSet<AttendanceSegment> AttendanceSegments { get; }
    DbSet<OvertimeRequest> OvertimeRequests { get; }
    DbSet<ShiftSwapRequest> ShiftSwapRequests { get; }
    DbSet<AttendanceExplanation> AttendanceExplanations { get; }
    DbSet<MonthlyTimesheetPeriod> MonthlyTimesheetPeriods { get; }
    DbSet<MonthlyTimesheetSummary> MonthlyTimesheetSummaries { get; }
    DbSet<AttendanceAdjustment> AttendanceAdjustments { get; }

    // Phase 4 Performance Management (KPI) DbSets
    DbSet<KpiDefinition> KpiDefinitions { get; }
    DbSet<KpiThreshold> KpiThresholds { get; }
    DbSet<KpiScheme> KpiSchemes { get; }
    DbSet<KpiPeriod> KpiPeriods { get; }
    DbSet<KpiSheet> KpiSheets { get; }
    DbSet<KpiItem> KpiItems { get; }
    DbSet<KpiExtensionRequest> KpiExtensionRequests { get; }
    DbSet<KpiResult> KpiResults { get; }
    DbSet<KpiAppeal> KpiAppeals { get; }
    DbSet<KpiMetric> KpiMetrics { get; }
    DbSet<KpiTemplate> KpiTemplates { get; }
    DbSet<KpiTemplateItem> KpiTemplateItems { get; }
    DbSet<KpiConfiguration> KpiConfigurations { get; }

    // Phase 7 Total Rewards & Salary Review DbSets
    DbSet<SalaryBand> SalaryBands { get; }
    DbSet<SalaryReviewCycle> SalaryReviewCycles { get; }
    DbSet<SalaryBudget> SalaryBudgets { get; }
    DbSet<SalaryAdjustmentApprovalStep> SalaryAdjustmentApprovalSteps { get; }
    DbSet<SalaryAdjustment> SalaryAdjustments { get; }
    // Phase 8 People Analytics & Reporting DbSets
    DbSet<VinamilkHRM.Domain.Entities.Analytics.WorkforceSnapshot> WorkforceSnapshots { get; }
    DbSet<VinamilkHRM.Domain.Entities.Analytics.ExitSurvey> ExitSurveys { get; }
    DbSet<VinamilkHRM.Domain.Entities.Analytics.MarketSalaryBenchmark> MarketSalaryBenchmarks { get; }
    DbSet<VinamilkHRM.Domain.Entities.Analytics.MacroIndicator> MacroIndicators { get; }
    DbSet<VinamilkHRM.Domain.Entities.Analytics.CompanyMetric> CompanyMetrics { get; }
    DbSet<VinamilkHRM.Domain.Entities.Analytics.AiAuditLog> AiAuditLogs { get; }
    // Workflow Engine DbSets (Session 11 P1-1)
    DbSet<WorkflowDefinition> WorkflowDefinitions { get; }
    DbSet<WorkflowStepDefinition> WorkflowStepDefinitions { get; }
    DbSet<WorkflowInstance> WorkflowInstances { get; }
    DbSet<WorkflowStepInstance> WorkflowStepInstances { get; }
    DbSet<ApprovalDelegation> ApprovalDelegations { get; }

    // Notification System DbSets (Session 11 P1-2)
    DbSet<NotificationTemplate> NotificationTemplates { get; }
    DbSet<NotificationPreference> NotificationPreferences { get; }
    DbSet<InAppNotification> InAppNotifications { get; }
    DbSet<NotificationOutboxMessage> NotificationOutboxMessages { get; }

    // Onboarding / Offboarding DbSets (Session 11 P1-4)
    DbSet<ChecklistTemplate> ChecklistTemplates { get; }
    DbSet<ChecklistTemplateItem> ChecklistTemplateItems { get; }
    DbSet<EmployeeChecklistTask> EmployeeChecklistTasks { get; }
    DbSet<SocialInsuranceDeclaration> SocialInsuranceDeclarations { get; }
    DbSet<FinalSettlement> FinalSettlements { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}


