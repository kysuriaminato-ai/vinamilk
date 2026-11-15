using Vinamilk.HRM.Domain.Common;
using Vinamilk.HRM.Domain.Entities.Core;

namespace Vinamilk.HRM.Domain.Entities.Talent;

public class RecruitmentRequest : BaseAuditableEntity
{
    public string RequestCode { get; set; } = string.Empty;
    public int OrganizationUnitId { get; set; }
    public int PositionId { get; set; }
    public int? RequestedByEmployeeId { get; set; }
    public int Quantity { get; set; } = 1;
    public int HiredCount { get; set; } = 0;
    public string EmploymentType { get; set; } = "FullTime";
    public string ReasonType { get; set; } = "NewHeadcount";
    public decimal BudgetMinSalary { get; set; }
    public decimal BudgetMaxSalary { get; set; }
    public DateOnly? ExpectedOnboardDate { get; set; }
    public string? JobRequirements { get; set; }
    public int? WorkflowInstanceId { get; set; }
    public string Status { get; set; } = "Approved";

    public OrganizationUnit OrganizationUnit { get; set; } = null!;
    public Position Position { get; set; } = null!;
    public ICollection<JobPosting> JobPostings { get; set; } = new List<JobPosting>();
}

public class JobPosting : BaseAuditableEntity
{
    public string PostingCode { get; set; } = string.Empty;
    public int? RecruitmentRequestId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int OrganizationUnitId { get; set; }
    public int PositionId { get; set; }
    public int Vacancies { get; set; } = 1;
    public string? SalaryRangeText { get; set; }
    public string? WorkLocation { get; set; }
    public DateOnly PublishDate { get; set; }
    public DateOnly ClosingDate { get; set; }
    public string? JobDescription { get; set; }
    public string? JobRequirements { get; set; }
    public string? Benefits { get; set; }
    public string Status { get; set; } = "Open";

    public RecruitmentRequest? RecruitmentRequest { get; set; }
    public OrganizationUnit OrganizationUnit { get; set; } = null!;
    public Position Position { get; set; } = null!;
    public ICollection<Candidate> Candidates { get; set; } = new List<Candidate>();
}

public class Candidate : BaseAuditableEntity
{
    public string CandidateCode { get; set; } = string.Empty;
    public int JobPostingId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public string Gender { get; set; } = "Nam";
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int? EducationLevelId { get; set; }
    public string? Major { get; set; }
    public decimal YearsOfExperience { get; set; } = 0m;
    public decimal ExpectedSalary { get; set; } = 0m;
    public string SourceChannel { get; set; } = "WebsiteVNM";
    public string? ResumeUrl { get; set; }
    public string Stage { get; set; } = "Applied";
    public decimal OverallScore { get; set; } = 0m;
    public int? ConvertedEmployeeId { get; set; }
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

    public JobPosting JobPosting { get; set; } = null!;
    public EducationLevel? EducationLevel { get; set; }
    public Employee? ConvertedEmployee { get; set; }
    public ICollection<Interview> Interviews { get; set; } = new List<Interview>();
    public ICollection<JobOffer> Offers { get; set; } = new List<JobOffer>();
}

public class Interview : BaseAuditableEntity
{
    public int CandidateId { get; set; }
    public int RoundNumber { get; set; } = 1;
    public string RoundTitle { get; set; } = "Phỏng vấn Chuyên môn";
    public DateTime ScheduledStartTime { get; set; }
    public DateTime ScheduledEndTime { get; set; }
    public string InterviewMode { get; set; } = "InPerson";
    public string? LocationOrLink { get; set; }
    public int? InterviewerEmployeeId { get; set; }
    public decimal TechnicalScore { get; set; }
    public decimal CultureFitScore { get; set; }
    public decimal TotalScore { get; set; }
    public string? EvaluationNotes { get; set; }
    public string Result { get; set; } = "Pending";

    public Candidate Candidate { get; set; } = null!;
    public Employee? Interviewer { get; set; }
}

public class JobOffer : BaseAuditableEntity
{
    public string OfferCode { get; set; } = string.Empty;
    public int CandidateId { get; set; }
    public int ProposedPositionId { get; set; }
    public int ProposedUnitId { get; set; }
    public decimal OfferedProbationSalary { get; set; }
    public decimal OfferedOfficialSalary { get; set; }
    public DateOnly ExpectedStartDate { get; set; }
    public DateOnly OfferExpiryDate { get; set; }
    public string Status { get; set; } = "Sent";
    public string? CandidateFeedback { get; set; }

    public Candidate Candidate { get; set; } = null!;
    public Position ProposedPosition { get; set; } = null!;
    public OrganizationUnit ProposedUnit { get; set; } = null!;
}

public class OnboardingTemplate : BaseAuditableEntity
{
    public string TemplateCode { get; set; } = string.Empty;
    public string TaskName { get; set; } = string.Empty;
    public string ProcessType { get; set; } = "Onboarding";
    public string AssignedDepartmentRole { get; set; } = "HR";
    public int DueDaysOffset { get; set; } = 3;
    public int SortOrder { get; set; } = 1;
}

public class EmployeeOnboardingTask : BaseAuditableEntity
{
    public int EmployeeId { get; set; }
    public int? OnboardingTemplateId { get; set; }
    public string ProcessType { get; set; } = "Onboarding";
    public string TaskName { get; set; } = string.Empty;
    public int? PicEmployeeId { get; set; }
    public DateOnly DueDate { get; set; }
    public bool IsCompleted { get; set; } = false;
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }

    public Employee Employee { get; set; } = null!;
    public OnboardingTemplate? OnboardingTemplate { get; set; }
}

public class Asset : BaseAuditableEntity
{
    public string AssetCode { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string AssetCategory { get; set; } = "Laptop";
    public string? SerialNumber { get; set; }
    public decimal PurchaseValue { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public string Status { get; set; } = "Available";

    public ICollection<EmployeeAssetAssignment> Assignments { get; set; } = new List<EmployeeAssetAssignment>();
}

public class EmployeeAssetAssignment : BaseAuditableEntity
{
    public int AssetId { get; set; }
    public int EmployeeId { get; set; }
    public DateOnly AssignedDate { get; set; }
    public DateOnly? ReturnedDate { get; set; }
    public string? ConditionOnAssign { get; set; }
    public string? ConditionOnReturn { get; set; }
    public string Status { get; set; } = "Active";

    public Asset Asset { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
}

public class TrainingCourse : BaseAuditableEntity
{
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string CourseCategory { get; set; } = "Technical";
    public int DurationHours { get; set; } = 16;
    public decimal StandardCostPerPerson { get; set; }
    public int CommitmentMonths { get; set; } = 0;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<TrainingClass> Classes { get; set; } = new List<TrainingClass>();
}

public class TrainingClass : BaseAuditableEntity
{
    public string ClassCode { get; set; } = string.Empty;
    public int TrainingCourseId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string? InstructorName { get; set; }
    public string? Location { get; set; }
    public int MaxCapacity { get; set; } = 30;
    public decimal TotalActualCost { get; set; }
    public string Status { get; set; } = "Completed";

    public TrainingCourse TrainingCourse { get; set; } = null!;
    public ICollection<TrainingEnrollment> Enrollments { get; set; } = new List<TrainingEnrollment>();
}

public class TrainingEnrollment : BaseAuditableEntity
{
    public int TrainingClassId { get; set; }
    public int EmployeeId { get; set; }
    public DateOnly EnrolledDate { get; set; }
    public decimal AttendanceRatePercent { get; set; } = 100m;
    public decimal FinalScore { get; set; }
    public string Result { get; set; } = "Passed";
    public string? CertificateNumber { get; set; }
    public int? FeedbackRating { get; set; }
    public string? FeedbackComment { get; set; }

    public TrainingClass TrainingClass { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
}

public class EvaluationCycle : BaseAuditableEntity
{
    public string CycleCode { get; set; } = string.Empty;
    public string CycleName { get; set; } = string.Empty;
    public string CycleType { get; set; } = "Quarterly";
    public int Year { get; set; }
    public int PeriodNumber { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateOnly SelfEvalDeadline { get; set; }
    public DateOnly ManagerEvalDeadline { get; set; }
    public string Status { get; set; } = "Closed";

    public ICollection<EmployeeKpiEvaluation> Evaluations { get; set; } = new List<EmployeeKpiEvaluation>();
}

public class EmployeeKpiEvaluation : BaseAuditableEntity
{
    public int EvaluationCycleId { get; set; }
    public int EmployeeId { get; set; }
    public int? ReviewerEmployeeId { get; set; }
    public decimal TotalWeight { get; set; } = 100m;
    public decimal SelfTotalScore { get; set; }
    public decimal ManagerTotalScore { get; set; }
    public decimal FinalScore { get; set; }
    public string RatingGrade { get; set; } = "A_Excellent";
    public decimal BonusMultiplier { get; set; } = 1.0m;
    public string? EmployeeComment { get; set; }
    public string? ReviewerComment { get; set; }
    public string Status { get; set; } = "Approved";
    public DateTime? ApprovedAt { get; set; }

    public EvaluationCycle EvaluationCycle { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
    public Employee? Reviewer { get; set; }
    public ICollection<EmployeeKpiItem> Items { get; set; } = new List<EmployeeKpiItem>();
}

public class EmployeeKpiItem : BaseAuditableEntity
{
    public int EmployeeKpiEvaluationId { get; set; }
    public string GoalTitle { get; set; } = string.Empty;
    public string Perspective { get; set; } = "InternalProcess";
    public string MeasureUnit { get; set; } = "%";
    public decimal WeightPercent { get; set; }
    public decimal TargetValue { get; set; }
    public decimal ActualValue { get; set; }
    public decimal CompletionRatePercent { get; set; }
    public decimal SelfScore { get; set; }
    public decimal ManagerScore { get; set; }
    public string? EvidenceNote { get; set; }

    public EmployeeKpiEvaluation Evaluation { get; set; } = null!;
}

