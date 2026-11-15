using Vinamilk.HRM.Domain.Common;
using Vinamilk.HRM.Domain.Entities.Core;
using Vinamilk.HRM.Domain.Entities.Time;

namespace Vinamilk.HRM.Domain.Entities.Payroll;

public class PitBracket : BaseAuditableEntity
{
    public int BracketLevel { get; set; }
    public decimal LowerBound { get; set; }
    public decimal? UpperBound { get; set; }
    public decimal TaxRate { get; set; } // Thuế suất dạng thập phân (0.05 = 5%)
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string? LegalReference { get; set; }
}

public class PayrollStatutoryParameter : BaseAuditableEntity
{
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string? LegalReference { get; set; }
    public string? Notes { get; set; }
    public decimal StatutoryBaseSalary { get; set; } // Lương cơ sở Nhà nước
    public decimal MinWageRegion1 { get; set; }
    public decimal MinWageRegion2 { get; set; }
    public decimal MinWageRegion3 { get; set; }
    public decimal MinWageRegion4 { get; set; }
    public decimal SocialInsuranceCapMultiplier { get; set; } = 20m;
    public decimal UnemploymentInsuranceCapMultiplier { get; set; } = 20m;
    public decimal EmployeeSiRate { get; set; } = 0.08m;
    public decimal EmployeeHiRate { get; set; } = 0.015m;
    public decimal EmployeeUiRate { get; set; } = 0.01m;
    public decimal CompanySiRate { get; set; } = 0.175m;
    public decimal CompanyHiRate { get; set; } = 0.03m;
    public decimal CompanyUiRate { get; set; } = 0.01m;
    public decimal CompanyUnionFeeRate { get; set; } = 0.02m;
    public decimal SelfDeduction { get; set; }
    public decimal DependentDeduction { get; set; }
    /// <summary>Tỷ lệ lương thử việc tối thiểu (% - mặc định 85% theo Điều 26 BLLĐ 2019).</summary>
    public decimal MinProbationSalaryRatePercent { get; set; } = 85.00m;
    public bool NeedsHrVerification { get; set; } = true;

    public decimal GetRegionalMinWage(int wageRegion) => wageRegion switch
    {
        1 => MinWageRegion1,
        2 => MinWageRegion2,
        3 => MinWageRegion3,
        4 => MinWageRegion4,
        _ => throw new ArgumentOutOfRangeException(nameof(wageRegion), $"Vùng lương '{wageRegion}' không hợp lệ (chỉ hỗ trợ 1..4).")
    };
}

public class PayrollParameter : BaseAuditableEntity
{
    public string ParamCode { get; set; } = string.Empty;
    public string ParamName { get; set; } = string.Empty;
    public decimal ParamValue { get; set; }
    public string ValueUnit { get; set; } = "VND";
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string? LegalReference { get; set; }
}

public class TaxBracket : BaseAuditableEntity
{
    public int BracketLevel { get; set; }
    public decimal FromIncome { get; set; }
    public decimal? ToIncome { get; set; }
    public decimal TaxRatePercent { get; set; }
    public decimal QuickSubtractionAmount { get; set; }
    public DateOnly EffectiveFrom { get; set; }
}

public class SalaryComponent : BaseAuditableEntity
{
    public string ComponentCode { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    /// <summary>Earning | Allowance | Bonus | Deduction | Info</summary>
    public string ComponentType { get; set; } = "Earning";
    /// <summary>Fixed | Formula | System</summary>
    public string CalculationType { get; set; } = "Formula";
    public decimal DefaultAmount { get; set; } = 0m;
    public bool IsTaxable { get; set; } = true;
    public decimal TaxExemptLimit { get; set; } = 0m;
    public string? TaxExemptLimitParameterCode { get; set; }
    public bool IsSocialInsuranceBase { get; set; } = false;
    public bool ShowOnPayslip { get; set; } = true;
    public string? FormulaExpression { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string? LegalReference { get; set; }
    public bool NeedsHrVerification { get; set; } = false;
    public string? Description { get; set; }

    public ICollection<PayrollRecordDetail> RecordDetails { get; set; } = new List<PayrollRecordDetail>();
    public ICollection<SalaryTemplateComponent> TemplateComponents { get; set; } = new List<SalaryTemplateComponent>();
}

public class SalaryTemplate : BaseAuditableEntity
{
    public string TemplateCode { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    /// <summary>Office | Factory | Sales | Management | General</summary>
    public string StaffGroup { get; set; } = "Office";
    /// <summary>Monthly | Daily | Hourly | PieceRate</summary>
    public string DefaultPayFrequency { get; set; } = "Monthly";
    public decimal DefaultProbationSalaryRatePercent { get; set; } = 85.00m;
    /// <summary>RoundToDong | RoundHalfToEven | RoundToThousand</summary>
    public string RoundingRule { get; set; } = "RoundToDong";
    public bool IsDefaultForCompany { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public string? LegalReference { get; set; }
    public bool NeedsHrVerification { get; set; } = true;
    public string? Description { get; set; }

    public ICollection<SalaryTemplateComponent> Components { get; set; } = new List<SalaryTemplateComponent>();
    public ICollection<EmployeeSalaryProfile> EmployeeProfiles { get; set; } = new List<EmployeeSalaryProfile>();
}

public class SalaryTemplateComponent : BaseAuditableEntity
{
    public int SalaryTemplateId { get; set; }
    public int SalaryComponentId { get; set; }
    public int SortOrder { get; set; }
    public string? OverrideFormulaExpression { get; set; }
    public decimal? DefaultFixedAmount { get; set; }
    public bool IsRequired { get; set; } = true;

    public SalaryTemplate SalaryTemplate { get; set; } = null!;
    public SalaryComponent SalaryComponent { get; set; } = null!;
}

public class EmployeeSalaryProfile : BaseAuditableEntity
{
    public int EmployeeId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public decimal BaseSalary { get; set; }
    public decimal InsuranceSalary { get; set; }
    public int? SalaryScaleId { get; set; }
    public int? SalaryStepId { get; set; }
    public int? SalaryTemplateId { get; set; }
    /// <summary>Monthly | Daily | Hourly | PieceRate</summary>
    public string PayMode { get; set; } = "Monthly";
    public decimal UnitRate { get; set; } = 0m;
    public bool IsProbation { get; set; } = false;
    public decimal ProbationRatePercent { get; set; } = 100.00m;
    public decimal PositionAllowance { get; set; } = 0m;
    public decimal OtherAllowance { get; set; } = 0m;
    public bool IsUnionMember { get; set; } = true;
    public string? DecisionNumber { get; set; }
    public string? Reason { get; set; }
    public bool IsActive { get; set; } = true;

    public Employee Employee { get; set; } = null!;
    public SalaryScale? SalaryScale { get; set; }
    public SalaryStep? SalaryStep { get; set; }
    public SalaryTemplate? SalaryTemplate { get; set; }
    public ICollection<EmployeeSalaryProfileAllowance> FixedAllowances { get; set; } = new List<EmployeeSalaryProfileAllowance>();
}

public class EmployeeSalaryProfileAllowance : BaseAuditableEntity
{
    public int EmployeeSalaryProfileId { get; set; }
    public int SalaryComponentId { get; set; }
    public string ComponentCode { get; set; } = string.Empty;
    public decimal Amount { get; set; } = 0m;
    public string? Notes { get; set; }

    public EmployeeSalaryProfile EmployeeSalaryProfile { get; set; } = null!;
    public SalaryComponent SalaryComponent { get; set; } = null!;
}

public static class PayrollPeriodStatuses
{
    public const string Draft = "Draft";
    public const string Calculated = "Calculated";
    public const string Submitted = "Submitted";
    public const string Approved = "Approved";
    public const string Locked = "Locked";
    public const string Paid = "Paid";
}

public class PayrollPeriod : BaseAuditableEntity
{
    public string PeriodCode { get; set; } = string.Empty;
    public int Month { get; set; }
    public int Year { get; set; }
    public string PeriodTitle { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal StandardWorkdays { get; set; } = 26m;
    public string Status { get; set; } = PayrollPeriodStatuses.Draft;
    public int CalculationVersion { get; set; } = 0;
    public int? CalculatedByUserId { get; set; }
    public DateTime? CalculatedAt { get; set; }
    public int? SubmittedByUserId { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public bool IsLocked { get; set; } = false;
    public int? LockedByUserId { get; set; }
    public DateTime? LockedAt { get; set; }
    public int? UnlockedByUserId { get; set; }
    public DateTime? UnlockedAt { get; set; }
    public string? LastUnlockReason { get; set; }
    public int? PaidByUserId { get; set; }
    public DateTime? PaidAt { get; set; }
    public int? WorkflowInstanceId { get; set; }
    public DateTime? PayslipSentAt { get; set; }

    public ICollection<PayrollRecord> PayrollRecords { get; set; } = new List<PayrollRecord>();
    public ICollection<PayrollCalculationRevision> CalculationRevisions { get; set; } = new List<PayrollCalculationRevision>();
    public ICollection<PayslipDispatchLog> PayslipDispatchLogs { get; set; } = new List<PayslipDispatchLog>();
}

public class PayrollRecord : BaseLongAuditableEntity
{
    public int PayrollPeriodId { get; set; }
    public int EmployeeId { get; set; }
    public int OrganizationUnitId { get; set; }
    public int PositionId { get; set; }
    public int? MonthlyAttendanceSummaryId { get; set; }
    public decimal StandardWorkdays { get; set; } = 26m;
    public decimal PayableWorkdays { get; set; }
    public decimal OtHours150 { get; set; }
    public decimal OtHours200 { get; set; }
    public decimal OtHours300 { get; set; }
    public decimal TotalOtHours { get; set; }
    public decimal ContractBaseSalary { get; set; }
    public decimal ActualBaseSalary { get; set; }
    public decimal PositionAllowance { get; set; }
    public decimal OtherAllowance { get; set; }
    public decimal OvertimePay { get; set; }
    public decimal KpiBonus { get; set; }
    public decimal UnusedLeavePayout { get; set; } = 0m;
    public decimal GrossIncome { get; set; }
    public decimal InsuranceSalaryBase { get; set; }
    public decimal UnemploymentInsuranceBase { get; set; }
    public decimal EmployeeSocialInsurance { get; set; }
    public decimal EmployeeHealthInsurance { get; set; }
    public decimal EmployeeUnemploymentInsurance { get; set; }
    public decimal TotalEmployeeInsurance { get; set; }
    public decimal CompanySocialInsurance { get; set; }
    public decimal CompanyHealthInsurance { get; set; }
    public decimal CompanyUnemploymentInsurance { get; set; }
    public decimal CompanyUnionFee { get; set; }
    public int DependentCount { get; set; }
    public decimal SelfDeductionAmount { get; set; }
    public decimal DependentDeductionAmount { get; set; }
    public decimal TaxableIncome { get; set; }
    public decimal AssessableIncome { get; set; }
    public decimal PersonalIncomeTax { get; set; }
    public string? PitBracketsJson { get; set; }
    public decimal DisciplineDeduction { get; set; }
    public decimal NetSalary { get; set; }
    public decimal? PreviousRevisionNetSalary { get; set; }
    public decimal? PreviousPeriodNetSalary { get; set; }
    public decimal? VariancePercentVsPreviousPeriod { get; set; }
    public bool HasHighVarianceWarning { get; set; } = false;
    public string PaymentStatus { get; set; } = "Paid";
    public DateTime? PaidAt { get; set; }
    public string? Notes { get; set; }

    public PayrollPeriod PayrollPeriod { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
    public OrganizationUnit OrganizationUnit { get; set; } = null!;
    public Position Position { get; set; } = null!;
    public MonthlyAttendanceSummary? MonthlyAttendanceSummary { get; set; }
    public ICollection<PayrollRecordDetail> Details { get; set; } = new List<PayrollRecordDetail>();
}

public class PayrollCalculationRevision : BaseLongAuditableEntity
{
    public int PayrollPeriodId { get; set; }
    public int? EmployeeId { get; set; }
    public int RevisionNumber { get; set; }
    public string Scope { get; set; } = "FullPeriod";
    public int AffectedEmployeeCount { get; set; }
    public decimal TotalGrossIncome { get; set; }
    public decimal TotalPersonalIncomeTax { get; set; }
    public decimal TotalNetSalary { get; set; }
    public decimal NetDeltaFromPreviousRevision { get; set; }
    public int HighVarianceWarningCount { get; set; }
    public string? SnapshotJson { get; set; }
    public string? CalculatedByUsername { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }

    public PayrollPeriod PayrollPeriod { get; set; } = null!;
}

public class PayslipDispatchLog : BaseLongAuditableEntity
{
    public Guid BatchId { get; set; }
    public int PayrollPeriodId { get; set; }
    public long PayrollRecordId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public string Status { get; set; } = "Queued";
    public int AttemptCount { get; set; } = 0;
    public string? ErrorMessage { get; set; }
    public string? HangfireJobId { get; set; }
    public DateTime? SentAt { get; set; }

    public PayrollPeriod PayrollPeriod { get; set; } = null!;
}

public class PayrollRecordDetail : BaseLongAuditableEntity
{
    public long PayrollRecordId { get; set; }
    public int SalaryComponentId { get; set; }
    public string ComponentCode { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? CalculationNote { get; set; }

    public PayrollRecord PayrollRecord { get; set; } = null!;
    public SalaryComponent SalaryComponent { get; set; } = null!;
}

