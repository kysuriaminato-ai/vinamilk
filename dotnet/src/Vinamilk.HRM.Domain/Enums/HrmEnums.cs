namespace Vinamilk.HRM.Domain.Enums;

public enum DataScopeType
{
    All = 1,
    UnitTree = 2,
    OwnUnit = 3,
    SelfOnly = 4
}

public enum UnitType
{
    HeadOffice = 1,
    Division = 2,
    Department = 3,
    Factory = 4,
    Branch = 5,
    Farm = 6,
    Team = 7
}

public enum PositionGroup
{
    Executive = 1,
    Director = 2,
    Manager = 3,
    Specialist = 4,
    Staff = 5,
    Worker = 6
}

public enum Gender
{
    Male = 1,
    Female = 2,
    Other = 3
}

public enum MaritalStatus
{
    Single = 1,
    Married = 2,
    Divorced = 3,
    Widowed = 4
}

public enum WorkStatus
{
    Probation = 1,
    Official = 2,
    MaternityLeave = 3,
    UnpaidLeave = 4,
    Resigned = 5,
    Retired = 6
}

public enum ContractType
{
    Probation = 1,
    Fixed12M = 2,
    Fixed36M = 3,
    Indefinite = 4,
    Seasonal = 5
}

public enum ContractStatus
{
    Draft = 1,
    Active = 2,
    Expired = 3,
    Renewed = 4,
    Terminated = 5
}

public enum RelationshipType
{
    Child = 1,
    Spouse = 2,
    Father = 3,
    Mother = 4,
    Other = 5
}

public enum CertificateType
{
    Degree = 1,
    ForeignLanguage = 2,
    IT = 3,
    Safety = 4,
    Professional = 5
}

public enum DocumentCategory
{
    Resume = 1,
    CitizenId = 2,
    Degree = 3,
    HealthCheck = 4,
    Contract = 5,
    Decision = 6
}

public enum TransferType
{
    Transfer = 1,
    Promotion = 2,
    Appointment = 3,
    Demotion = 4,
    Secondment = 5
}

public enum ApprovalStatus
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    Applied = 4,
    Rejected = 5,
    Cancelled = 6
}

public enum RewardDisciplineType
{
    Reward = 1,
    Discipline = 2
}

public enum PunchType
{
    CheckIn = 1,
    CheckOut = 2,
    Unknown = 3
}

public enum AttendanceSourceType
{
    BiometricDevice = 1,
    MobileGps = 2,
    WebQr = 3,
    ManualImport = 4
}

public enum OvertimeRateType
{
    OT150 = 1,
    OT200 = 2,
    OT300 = 3
}

public enum LeaveSession
{
    FullDay = 1,
    Morning = 2,
    Afternoon = 3
}

public enum SalaryComponentType
{
    Earning = 1,
    Allowance = 2,
    Bonus = 3,
    Deduction = 4,
    CompanyContribution = 5
}

public enum PayrollPeriodStatus
{
    Draft = 1,
    Calculated = 2,
    PendingApproval = 3,
    Approved = 4,
    Locked = 5,
    Paid = 6
}

public enum PaymentStatus
{
    Unpaid = 1,
    Paid = 2
}

public enum CandidateStage
{
    Applied = 1,
    Screening = 2,
    Interview1 = 3,
    Interview2 = 4,
    Offered = 5,
    Hired = 6,
    Rejected = 7
}

public enum InterviewMode
{
    InPerson = 1,
    OnlineTeams = 2,
    OnlineZoom = 3
}

public enum InterviewResult
{
    Pending = 1,
    Passed = 2,
    Failed = 3,
    Hold = 4,
    NoShow = 5
}

public enum OnboardingProcessType
{
    Onboarding = 1,
    Offboarding = 2
}

public enum AssetStatus
{
    Available = 1,
    Assigned = 2,
    UnderMaintenance = 3,
    Lost = 4,
    Liquidated = 5
}

public enum EvaluationCycleType
{
    Monthly = 1,
    Quarterly = 2,
    HalfYearly = 3,
    Annual = 4
}

public enum KpiRatingGrade
{
    A_Excellent = 1,
    B_Good = 2,
    C_Average = 3,
    D_Poor = 4
}

public enum WorkflowApproverType
{
    DirectManager = 1,
    UnitHead = 2,
    SpecificRole = 3,
    SpecificUser = 4
}

