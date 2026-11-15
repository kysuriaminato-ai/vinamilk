using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Domain.Entities.Core;
using Vinamilk.HRM.Domain.Entities.Identity;
using Vinamilk.HRM.Domain.Entities.Payroll;
using Vinamilk.HRM.Domain.Entities.SystemEntities;
using Vinamilk.HRM.Domain.Entities.Talent;
using Vinamilk.HRM.Domain.Entities.Time;

namespace Vinamilk.HRM.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    // Identity Schema
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<UserDataScope> UserDataScopes { get; }
    DbSet<UserSession> UserSessions { get; }
    DbSet<DataPrivacyConsent> DataPrivacyConsents { get; }

    // Core HR Schema
    DbSet<OrganizationUnit> OrganizationUnits { get; }
    DbSet<Position> Positions { get; }
    DbSet<SalaryScale> SalaryScales { get; }
    DbSet<SalaryStep> SalarySteps { get; }
    DbSet<EducationLevel> EducationLevels { get; }
    DbSet<Employee> Employees { get; }
    DbSet<EmployeeDependent> EmployeeDependents { get; }
    DbSet<EmployeeCertificate> EmployeeCertificates { get; }
    DbSet<EmployeeDocument> EmployeeDocuments { get; }
    DbSet<LaborContract> LaborContracts { get; }
    DbSet<ContractAddendum> ContractAddendums { get; }
    DbSet<EmploymentHistory> EmploymentHistories { get; }
    DbSet<PersonnelTransfer> PersonnelTransfers { get; }
    DbSet<SalaryProgression> SalaryProgressions { get; }
    DbSet<RewardDisciplineCategory> RewardDisciplineCategories { get; }
    DbSet<RewardDiscipline> RewardDisciplines { get; }

    // Time & Attendance Schema
    DbSet<WorkShift> WorkShifts { get; }
    DbSet<ShiftSchedule> ShiftSchedules { get; }
    DbSet<Holiday> Holidays { get; }
    DbSet<AttendanceLog> AttendanceLogs { get; }
    DbSet<DailyAttendance> DailyAttendances { get; }
    DbSet<MonthlyAttendanceSummary> MonthlyAttendanceSummaries { get; }
    DbSet<OvertimeRequest> OvertimeRequests { get; }
    DbSet<LeaveType> LeaveTypes { get; }
    DbSet<LeaveBalance> LeaveBalances { get; }
    DbSet<LeaveRequest> LeaveRequests { get; }
    DbSet<AttendanceStatutoryParameter> AttendanceStatutoryParameters { get; }
    DbSet<PolicyGroup> PolicyGroups { get; }
    DbSet<PolicyGroupAssignment> PolicyGroupAssignments { get; }
    DbSet<AttendancePolicy> AttendancePolicies { get; }
    DbSet<ShiftRotationPattern> ShiftRotationPatterns { get; }
    DbSet<ShiftSwapRequest> ShiftSwapRequests { get; }
    DbSet<GeofenceLocation> GeofenceLocations { get; }
    DbSet<AttendanceDevice> AttendanceDevices { get; }
    DbSet<EmployeeDeviceMapping> EmployeeDeviceMappings { get; }
    DbSet<UnmappedAttendanceLog> UnmappedAttendanceLogs { get; }
    DbSet<EmployeeDeviceBinding> EmployeeDeviceBindings { get; }
    DbSet<AttendanceSymbolDefinition> AttendanceSymbolDefinitions { get; }
    DbSet<PostLockAttendanceAdjustment> PostLockAttendanceAdjustments { get; }
    DbSet<AttendanceRecalculationBatch> AttendanceRecalculationBatches { get; }
    DbSet<EmployeeTimeRequest> EmployeeTimeRequests { get; }
    DbSet<RequestCancellationRequest> RequestCancellationRequests { get; }
    DbSet<TerminationLeaveSettlement> TerminationLeaveSettlements { get; }

    // Payroll Schema
    DbSet<PayrollStatutoryParameter> PayrollStatutoryParameters { get; }
    DbSet<PitBracket> PitBrackets { get; }
    DbSet<PayrollParameter> PayrollParameters { get; }
    DbSet<TaxBracket> TaxBrackets { get; }
    DbSet<SalaryComponent> SalaryComponents { get; }
    DbSet<SalaryTemplate> SalaryTemplates { get; }
    DbSet<SalaryTemplateComponent> SalaryTemplateComponents { get; }
    DbSet<EmployeeSalaryProfile> EmployeeSalaryProfiles { get; }
    DbSet<EmployeeSalaryProfileAllowance> EmployeeSalaryProfileAllowances { get; }
    DbSet<PayrollPeriod> PayrollPeriods { get; }
    DbSet<PayrollRecord> PayrollRecords { get; }
    DbSet<PayrollRecordDetail> PayrollRecordDetails { get; }
    DbSet<PayrollCalculationRevision> PayrollCalculationRevisions { get; }
    DbSet<PayslipDispatchLog> PayslipDispatchLogs { get; }
    DbSet<SalaryAdvance> SalaryAdvances { get; }
    DbSet<EmployeeLoan> EmployeeLoans { get; }
    DbSet<EmployeeLoanInstallment> EmployeeLoanInstallments { get; }
    DbSet<PayrollAdjustment> PayrollAdjustments { get; }
    DbSet<PayrollPolicy> PayrollPolicies { get; }
    DbSet<EmployeeTerminationSettlement> EmployeeTerminationSettlements { get; }
    DbSet<PayrollAccountingAccountConfig> PayrollAccountingAccountConfigs { get; }
    DbSet<SocialInsuranceReportTemplate> SocialInsuranceReportTemplates { get; }
    DbSet<PitWithholdingCertificate> PitWithholdingCertificates { get; }

    // Talent Schema
    DbSet<RecruitmentRequest> RecruitmentRequests { get; }
    DbSet<JobPosting> JobPostings { get; }
    DbSet<Candidate> Candidates { get; }
    DbSet<Interview> Interviews { get; }
    DbSet<JobOffer> JobOffers { get; }
    DbSet<OnboardingTemplate> OnboardingTemplates { get; }
    DbSet<EmployeeOnboardingTask> EmployeeOnboardingTasks { get; }
    DbSet<Asset> Assets { get; }
    DbSet<EmployeeAssetAssignment> EmployeeAssetAssignments { get; }
    DbSet<TrainingCourse> TrainingCourses { get; }
    DbSet<TrainingClass> TrainingClasses { get; }
    DbSet<TrainingEnrollment> TrainingEnrollments { get; }
    DbSet<EvaluationCycle> EvaluationCycles { get; }
    DbSet<EmployeeKpiEvaluation> EmployeeKpiEvaluations { get; }
    DbSet<EmployeeKpiItem> EmployeeKpiItems { get; }

    // System Schema
    DbSet<WorkflowDefinition> WorkflowDefinitions { get; }
    DbSet<WorkflowStep> WorkflowSteps { get; }
    DbSet<WorkflowInstance> WorkflowInstances { get; }
    DbSet<WorkflowApprovalLog> WorkflowApprovalLogs { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<AiRecommendation> AiRecommendations { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<MenuItem> MenuItems { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

