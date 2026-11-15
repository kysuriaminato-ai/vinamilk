using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Domain.Entities.Core;
using Vinamilk.HRM.Domain.Entities.Identity;
using Vinamilk.HRM.Domain.Entities.Payroll;
using Vinamilk.HRM.Domain.Entities.SystemEntities;
using Vinamilk.HRM.Domain.Entities.Talent;
using Vinamilk.HRM.Domain.Entities.Time;
using Vinamilk.HRM.Infrastructure.Persistence.Interceptors;

namespace Vinamilk.HRM.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Identity Schema
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserDataScope> UserDataScopes => Set<UserDataScope>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<DataPrivacyConsent> DataPrivacyConsents => Set<DataPrivacyConsent>();

    // Core HR Schema
    public DbSet<OrganizationUnit> OrganizationUnits => Set<OrganizationUnit>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<SalaryScale> SalaryScales => Set<SalaryScale>();
    public DbSet<SalaryStep> SalarySteps => Set<SalaryStep>();
    public DbSet<EducationLevel> EducationLevels => Set<EducationLevel>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeDependent> EmployeeDependents => Set<EmployeeDependent>();
    public DbSet<EmployeeCertificate> EmployeeCertificates => Set<EmployeeCertificate>();
    public DbSet<EmployeeDocument> EmployeeDocuments => Set<EmployeeDocument>();
    public DbSet<LaborContract> LaborContracts => Set<LaborContract>();
    public DbSet<ContractAddendum> ContractAddendums => Set<ContractAddendum>();
    public DbSet<EmploymentHistory> EmploymentHistories => Set<EmploymentHistory>();
    public DbSet<PersonnelTransfer> PersonnelTransfers => Set<PersonnelTransfer>();
    public DbSet<SalaryProgression> SalaryProgressions => Set<SalaryProgression>();
    public DbSet<RewardDisciplineCategory> RewardDisciplineCategories => Set<RewardDisciplineCategory>();
    public DbSet<RewardDiscipline> RewardDisciplines => Set<RewardDiscipline>();

    // Time & Attendance Schema
    public DbSet<WorkShift> WorkShifts => Set<WorkShift>();
    public DbSet<ShiftSchedule> ShiftSchedules => Set<ShiftSchedule>();
    public DbSet<Holiday> Holidays => Set<Holiday>();
    public DbSet<AttendanceLog> AttendanceLogs => Set<AttendanceLog>();
    public DbSet<DailyAttendance> DailyAttendances => Set<DailyAttendance>();
    public DbSet<MonthlyAttendanceSummary> MonthlyAttendanceSummaries => Set<MonthlyAttendanceSummary>();
    public DbSet<OvertimeRequest> OvertimeRequests => Set<OvertimeRequest>();
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<AttendanceStatutoryParameter> AttendanceStatutoryParameters => Set<AttendanceStatutoryParameter>();
    public DbSet<PolicyGroup> PolicyGroups => Set<PolicyGroup>();
    public DbSet<PolicyGroupAssignment> PolicyGroupAssignments => Set<PolicyGroupAssignment>();
    public DbSet<AttendancePolicy> AttendancePolicies => Set<AttendancePolicy>();
    public DbSet<ShiftRotationPattern> ShiftRotationPatterns => Set<ShiftRotationPattern>();
    public DbSet<ShiftSwapRequest> ShiftSwapRequests => Set<ShiftSwapRequest>();
    public DbSet<GeofenceLocation> GeofenceLocations => Set<GeofenceLocation>();
    public DbSet<AttendanceDevice> AttendanceDevices => Set<AttendanceDevice>();
    public DbSet<EmployeeDeviceMapping> EmployeeDeviceMappings => Set<EmployeeDeviceMapping>();
    public DbSet<UnmappedAttendanceLog> UnmappedAttendanceLogs => Set<UnmappedAttendanceLog>();
    public DbSet<EmployeeDeviceBinding> EmployeeDeviceBindings => Set<EmployeeDeviceBinding>();
    public DbSet<AttendanceSymbolDefinition> AttendanceSymbolDefinitions => Set<AttendanceSymbolDefinition>();
    public DbSet<PostLockAttendanceAdjustment> PostLockAttendanceAdjustments => Set<PostLockAttendanceAdjustment>();
    public DbSet<AttendanceRecalculationBatch> AttendanceRecalculationBatches => Set<AttendanceRecalculationBatch>();
    public DbSet<EmployeeTimeRequest> EmployeeTimeRequests => Set<EmployeeTimeRequest>();
    public DbSet<RequestCancellationRequest> RequestCancellationRequests => Set<RequestCancellationRequest>();
    public DbSet<TerminationLeaveSettlement> TerminationLeaveSettlements => Set<TerminationLeaveSettlement>();

    // Payroll Schema
    public DbSet<PayrollStatutoryParameter> PayrollStatutoryParameters => Set<PayrollStatutoryParameter>();
    public DbSet<PitBracket> PitBrackets => Set<PitBracket>();
    public DbSet<PayrollParameter> PayrollParameters => Set<PayrollParameter>();
    public DbSet<TaxBracket> TaxBrackets => Set<TaxBracket>();
    public DbSet<SalaryComponent> SalaryComponents => Set<SalaryComponent>();
    public DbSet<SalaryTemplate> SalaryTemplates => Set<SalaryTemplate>();
    public DbSet<SalaryTemplateComponent> SalaryTemplateComponents => Set<SalaryTemplateComponent>();
    public DbSet<EmployeeSalaryProfile> EmployeeSalaryProfiles => Set<EmployeeSalaryProfile>();
    public DbSet<EmployeeSalaryProfileAllowance> EmployeeSalaryProfileAllowances => Set<EmployeeSalaryProfileAllowance>();
    public DbSet<PayrollPeriod> PayrollPeriods => Set<PayrollPeriod>();
    public DbSet<PayrollRecord> PayrollRecords => Set<PayrollRecord>();
    public DbSet<PayrollRecordDetail> PayrollRecordDetails => Set<PayrollRecordDetail>();
    public DbSet<PayrollCalculationRevision> PayrollCalculationRevisions => Set<PayrollCalculationRevision>();
    public DbSet<PayslipDispatchLog> PayslipDispatchLogs => Set<PayslipDispatchLog>();

    // Talent Schema
    public DbSet<RecruitmentRequest> RecruitmentRequests => Set<RecruitmentRequest>();
    public DbSet<JobPosting> JobPostings => Set<JobPosting>();
    public DbSet<Candidate> Candidates => Set<Candidate>();
    public DbSet<Interview> Interviews => Set<Interview>();
    public DbSet<JobOffer> JobOffers => Set<JobOffer>();
    public DbSet<OnboardingTemplate> OnboardingTemplates => Set<OnboardingTemplate>();
    public DbSet<EmployeeOnboardingTask> EmployeeOnboardingTasks => Set<EmployeeOnboardingTask>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<EmployeeAssetAssignment> EmployeeAssetAssignments => Set<EmployeeAssetAssignment>();
    public DbSet<TrainingCourse> TrainingCourses => Set<TrainingCourse>();
    public DbSet<TrainingClass> TrainingClasses => Set<TrainingClass>();
    public DbSet<TrainingEnrollment> TrainingEnrollments => Set<TrainingEnrollment>();
    public DbSet<EvaluationCycle> EvaluationCycles => Set<EvaluationCycle>();
    public DbSet<EmployeeKpiEvaluation> EmployeeKpiEvaluations => Set<EmployeeKpiEvaluation>();
    public DbSet<EmployeeKpiItem> EmployeeKpiItems => Set<EmployeeKpiItem>();

    // System Schema
    public DbSet<WorkflowDefinition> WorkflowDefinitions => Set<WorkflowDefinition>();
    public DbSet<WorkflowStep> WorkflowSteps => Set<WorkflowStep>();
    public DbSet<WorkflowInstance> WorkflowInstances => Set<WorkflowInstance>();
    public DbSet<WorkflowApprovalLog> WorkflowApprovalLogs => Set<WorkflowApprovalLog>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AiRecommendation> AiRecommendations => Set<AiRecommendation>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}

