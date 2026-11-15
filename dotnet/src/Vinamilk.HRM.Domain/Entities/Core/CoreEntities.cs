using Vinamilk.HRM.Domain.Common;
using Vinamilk.HRM.Domain.Entities.Identity;
using Vinamilk.HRM.Domain.Entities.Payroll;
using Vinamilk.HRM.Domain.Entities.Talent;
using Vinamilk.HRM.Domain.Entities.Time;

namespace Vinamilk.HRM.Domain.Entities.Core;

public class OrganizationUnit : BaseAuditableEntity
{
    public string UnitCode { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public string UnitType { get; set; } = "Department";
    public int UnitLevel { get; set; } = 3;
    public int? ParentUnitId { get; set; }
    public string? TreePath { get; set; }
    public int? ManagerEmployeeId { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? CostCenterCode { get; set; }
    public int PlannedHeadcount { get; set; } = 0;
    public int WageRegion { get; set; } = 1; // Vùng lương tối thiểu: 1..4 (theo NĐ vùng)
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    public OrganizationUnit? ParentUnit { get; set; }
    public ICollection<OrganizationUnit> SubUnits { get; set; } = new List<OrganizationUnit>();
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}

public class Position : BaseAuditableEntity
{
    public string PositionCode { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string PositionGroup { get; set; } = "Staff";
    public int PositionLevel { get; set; } = 5;
    public decimal DefaultPositionAllowance { get; set; } = 0m;
    public string? JobDescription { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}

public class SalaryScale : BaseAuditableEntity
{
    public string ScaleCode { get; set; } = string.Empty;
    public string ScaleName { get; set; } = string.Empty;
    public int ReviewCycleMonths { get; set; } = 12;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<SalaryStep> Steps { get; set; } = new List<SalaryStep>();
}

public class SalaryStep : BaseAuditableEntity
{
    public int SalaryScaleId { get; set; }
    public int StepOrder { get; set; }
    public string StepName { get; set; } = string.Empty;
    public decimal StandardSalary { get; set; }
    public decimal SalaryCoefficient { get; set; } = 1.0m;

    public SalaryScale SalaryScale { get; set; } = null!;
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}

public class EducationLevel : BaseAuditableEntity
{
    public string LevelCode { get; set; } = string.Empty;
    public string LevelName { get; set; } = string.Empty;
    public int RankOrder { get; set; }

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}

public class Employee : BaseAuditableEntity
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Gender { get; set; } = "Nam";
    public DateOnly DateOfBirth { get; set; }
    public string CitizenIdEncrypted { get; set; } = string.Empty;
    public string CitizenIdHash { get; set; } = string.Empty;
    public DateOnly? CitizenIdIssueDate { get; set; }
    public string? CitizenIdIssuePlace { get; set; }
    public string Ethnicity { get; set; } = "Kinh";
    public string Religion { get; set; } = "Không";
    public string MaritalStatus { get; set; } = "Single";
    public string? PlaceOfOrigin { get; set; }
    public string? PermanentAddress { get; set; }
    public string? CurrentAddress { get; set; }
    public string? PhoneNumber { get; set; }
    public string? WorkEmail { get; set; }
    public string? PersonalEmail { get; set; }
    public string? BankAccountEncrypted { get; set; }
    public string? BankName { get; set; }
    public string? BankBranch { get; set; }
    public string? TaxCodeEncrypted { get; set; }
    public string? SocialInsuranceNumber { get; set; }
    public string? HealthInsuranceNumber { get; set; }
    public string? HealthInsurancePlace { get; set; }
    public int OrganizationUnitId { get; set; }
    public int PositionId { get; set; }
    public int? DirectManagerId { get; set; }
    public int? EducationLevelId { get; set; }
    public string? Major { get; set; }
    public int? SalaryStepId { get; set; }
    public decimal BaseSalary { get; set; }
    public decimal PositionAllowance { get; set; }
    public decimal OtherAllowance { get; set; }
    public int? WageRegion { get; set; } // Null: kế thừa WageRegion của OrganizationUnit
    public DateOnly HireDate { get; set; }
    public DateOnly? OfficialDate { get; set; }
    public DateOnly? TerminationDate { get; set; }
    public string WorkStatus { get; set; } = "Official";
    public string CurrentContractType { get; set; } = "Indefinite";
    public string? AvatarUrl { get; set; }
    public bool IsUnionMember { get; set; } = true;

    public OrganizationUnit OrganizationUnit { get; set; } = null!;
    public Position Position { get; set; } = null!;
    public Employee? DirectManager { get; set; }
    public EducationLevel? EducationLevel { get; set; }
    public SalaryStep? SalaryStep { get; set; }
    public User? UserAccount { get; set; }

    public ICollection<EmployeeDependent> Dependents { get; set; } = new List<EmployeeDependent>();
    public ICollection<EmployeeCertificate> Certificates { get; set; } = new List<EmployeeCertificate>();
    public ICollection<EmployeeDocument> Documents { get; set; } = new List<EmployeeDocument>();
    public ICollection<LaborContract> Contracts { get; set; } = new List<LaborContract>();
    public ICollection<EmploymentHistory> EmploymentHistories { get; set; } = new List<EmploymentHistory>();
    public ICollection<PersonnelTransfer> Transfers { get; set; } = new List<PersonnelTransfer>();
    public ICollection<SalaryProgression> SalaryProgressions { get; set; } = new List<SalaryProgression>();
    public ICollection<RewardDiscipline> RewardDisciplines { get; set; } = new List<RewardDiscipline>();
    public ICollection<DailyAttendance> DailyAttendances { get; set; } = new List<DailyAttendance>();
    public ICollection<MonthlyAttendanceSummary> MonthlyAttendanceSummaries { get; set; } = new List<MonthlyAttendanceSummary>();
    public ICollection<LeaveBalance> LeaveBalances { get; set; } = new List<LeaveBalance>();
    public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    public ICollection<PayrollRecord> PayrollRecords { get; set; } = new List<PayrollRecord>();
    public ICollection<EmployeeKpiEvaluation> KpiEvaluations { get; set; } = new List<EmployeeKpiEvaluation>();
}

public class EmployeeDependent : BaseAuditableEntity
{
    public int EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Relationship { get; set; } = "Child";
    public DateOnly? DateOfBirth { get; set; }
    public string? CitizenIdEncrypted { get; set; }
    public string? TaxCodeEncrypted { get; set; }
    public bool IsTaxDeductible { get; set; } = true;
    public DateOnly? DeductionFromDate { get; set; }
    public DateOnly? DeductionToDate { get; set; }
    public string? ProofDocumentUrl { get; set; }
    public bool IsEmergencyContact { get; set; } = false;
    public string? EmergencyPhone { get; set; }

    public Employee Employee { get; set; } = null!;
}

public class EmployeeCertificate : BaseAuditableEntity
{
    public int EmployeeId { get; set; }
    public string CertificateName { get; set; } = string.Empty;
    public string CertificateType { get; set; } = "Professional";
    public string? IssuedBy { get; set; }
    public DateOnly? IssueDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public string? ScoreOrGrade { get; set; }
    public string? AttachmentUrl { get; set; }

    public Employee Employee { get; set; } = null!;
}

public class EmployeeDocument : BaseAuditableEntity
{
    public int EmployeeId { get; set; }
    public string DocumentCategory { get; set; } = "Resume";
    public string DocumentTitle { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long FileSizeBytes { get; set; }
    public DateTime? ExpiryDate { get; set; }

    public Employee Employee { get; set; } = null!;
}

public class LaborContract : BaseAuditableEntity
{
    public string ContractNumber { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public string ContractType { get; set; } = "Fixed12M";
    public DateOnly SignDate { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public int? SalaryStepId { get; set; }
    public decimal BaseSalary { get; set; }
    public decimal PositionAllowance { get; set; }
    public decimal OtherAllowance { get; set; }
    public decimal ProbationSalaryRate { get; set; } = 100m;
    public int? SignerEmployeeId { get; set; }
    public string Status { get; set; } = "Active";
    public string? SignedFileUrl { get; set; }
    public string? Notes { get; set; }

    public Employee Employee { get; set; } = null!;
    public SalaryStep? SalaryStep { get; set; }
    public ICollection<ContractAddendum> Addendums { get; set; } = new List<ContractAddendum>();
}

public class ContractAddendum : BaseAuditableEntity
{
    public int LaborContractId { get; set; }
    public string AddendumNumber { get; set; } = string.Empty;
    public DateOnly SignDate { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public string ChangeType { get; set; } = "SalaryAdjustment";
    public decimal? NewBaseSalary { get; set; }
    public decimal? NewAllowance { get; set; }
    public string? ContentSummary { get; set; }
    public string? SignedFileUrl { get; set; }

    public LaborContract LaborContract { get; set; } = null!;
}

public class EmploymentHistory : BaseAuditableEntity
{
    public int EmployeeId { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string CompanyName { get; set; } = "Công ty Cổ phần Sữa Việt Nam (Vinamilk)";
    public string? DepartmentName { get; set; }
    public string? PositionName { get; set; }
    public bool IsInternal { get; set; } = true;
    public string? Notes { get; set; }

    public Employee Employee { get; set; } = null!;
}

public class PersonnelTransfer : BaseAuditableEntity
{
    public string DecisionNumber { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public string TransferType { get; set; } = "Transfer";
    public int FromUnitId { get; set; }
    public int ToUnitId { get; set; }
    public int FromPositionId { get; set; }
    public int ToPositionId { get; set; }
    public DateOnly DecisionDate { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public string? Reason { get; set; }
    public int? ApprovedByEmployeeId { get; set; }
    public int? WorkflowInstanceId { get; set; }
    public string Status { get; set; } = "Approved";
    public DateTime? AppliedAt { get; set; }

    public Employee Employee { get; set; } = null!;
    public OrganizationUnit FromUnit { get; set; } = null!;
    public OrganizationUnit ToUnit { get; set; } = null!;
    public Position FromPosition { get; set; } = null!;
    public Position ToPosition { get; set; } = null!;
}

public class SalaryProgression : BaseAuditableEntity
{
    public string DecisionNumber { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public int? OldSalaryStepId { get; set; }
    public int? NewSalaryStepId { get; set; }
    public decimal OldBaseSalary { get; set; }
    public decimal NewBaseSalary { get; set; }
    public decimal OldAllowance { get; set; }
    public decimal NewAllowance { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public DateOnly? NextReviewDate { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = "Applied";

    public Employee Employee { get; set; } = null!;
    public SalaryStep? OldSalaryStep { get; set; }
    public SalaryStep? NewSalaryStep { get; set; }
}

public class RewardDisciplineCategory : BaseAuditableEntity
{
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryType { get; set; } = "Reward";
    public decimal DefaultAmount { get; set; }
    public string? Description { get; set; }

    public ICollection<RewardDiscipline> RewardDisciplines { get; set; } = new List<RewardDiscipline>();
}

public class RewardDiscipline : BaseAuditableEntity
{
    public string DecisionNumber { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public int CategoryId { get; set; }
    public string Type { get; set; } = "Reward";
    public DateOnly DecisionDate { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    public int? SignedByEmployeeId { get; set; }
    public int? PayrollPeriodId { get; set; }
    public string Status { get; set; } = "Approved";

    public Employee Employee { get; set; } = null!;
    public RewardDisciplineCategory Category { get; set; } = null!;
}

