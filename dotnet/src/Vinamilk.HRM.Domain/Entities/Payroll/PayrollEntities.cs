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
    /// <summary>BHTNLĐ-BNN doanh nghiệp đóng (mặc định 0.5% theo NĐ 58/2020 & NĐ 88/2020).</summary>
    public decimal CompanyOccupationalAccidentRate { get; set; } = 0.005m;
    /// <summary>Đoàn phí công đoàn phần NLĐ (cấu hình VD 0.01 = 1% theo QĐ 1908/QĐ-TLĐ; mặc định 0 nếu chưa kích hoạt thu qua lương).</summary>
    public decimal EmployeeUnionFeeRate { get; set; } = 0m;
    /// <summary>Trần đoàn phí NLĐ tối đa tính theo bội số Lương cơ sở Nhà nước (mặc định 0.10 = 10% LCS theo QĐ 1908/QĐ-TLĐ).</summary>
    public decimal EmployeeUnionFeeCapMultiplier { get; set; } = 0.10m;
    public decimal SelfDeduction { get; set; }
    public decimal DependentDeduction { get; set; }
    /// <summary>Thuế suất TNCN cá nhân không cư trú (mặc định 20% theo Luật Thuế TNCN).</summary>
    public decimal PitNonResidentRate { get; set; } = 0.20m;
    /// <summary>Thuế suất khấu trừ nguồn HĐ &lt; 3 tháng / không HĐ (mặc định 10% theo TT 111/2013/TT-BTC Điều 25).</summary>
    public decimal PitShortTermRate { get; set; } = 0.10m;
    /// <summary>Ngưỡng chi trả bắt đầu khấu trừ 10% (mặc định 2.000.000 đ/lần theo TT 111/2013/TT-BTC Điều 25).</summary>
    public decimal PitShortTermThreshold { get; set; } = 2_000_000m;
    /// <summary>Hệ số OT ngày thường (1.50), ngày nghỉ tuần (2.00), ngày Lễ/Tết (3.00).</summary>
    public decimal OtMultiplier150 { get; set; } = 1.50m;
    public decimal OtMultiplier200 { get; set; } = 2.00m;
    public decimal OtMultiplier300 { get; set; } = 3.00m;
    /// <summary>Phụ cấp làm đêm (+30% Điều 98.2 BLLĐ 2019) và phụ trội OT ban đêm (+20% Điều 98.3 BLLĐ 2019).</summary>
    public decimal OtNightShiftBonusRate { get; set; } = 0.30m;
    public decimal OtNightOvertimeExtraRate { get; set; } = 0.20m;
    /// <summary>Phần đơn giá chịu thuế TNCN của giờ OT (mặc định 1.00 = giờ công bình thường; phần trả vượt được miễn thuế theo TT 111/2013 Điều 3.1.i).</summary>
    public decimal OtTaxableBaseRate { get; set; } = 1.00m;
    public bool IsOtExcessTaxExempt { get; set; } = true;
    /// <summary>Ngưỡng cảnh báo biến động thu nhập ròng so với kỳ trước (%).</summary>
    public decimal PayrollVarianceWarningThresholdPercent { get; set; } = 20.00m;
    /// <summary>Tỷ lệ lương thử việc tối thiểu (% - mặc định 85% theo Điều 26 BLLĐ 2019).</summary>
    public decimal MinProbationSalaryRatePercent { get; set; } = 85.00m;
    /// <summary>Mức hưởng trợ cấp thôi việc luật định mỗi năm làm việc (mặc định 0.5 tháng lương theo Điều 46.1 BLLĐ 2019).</summary>
    public decimal SeveranceAllowanceMonthsPerYear { get; set; } = 0.5m;
    /// <summary>Số tháng làm việc thường xuyên tối thiểu để hưởng trợ cấp thôi việc (mặc định 12 tháng theo Điều 46.1 BLLĐ 2019).</summary>
    public int SeveranceMinTenureMonths { get; set; } = 12;
    /// <summary>Ngưỡng tháng lẻ làm tròn thành 1 năm trợ cấp thôi việc (mặc định >= 6 tháng tính 1 năm, dưới 6 tháng tính 0.5 năm theo Điều 8 NĐ 145/2020/NĐ-CP).</summary>
    public int SeveranceRoundingHalfYearMonths { get; set; } = 6;
    /// <summary>Miễn thuế TNCN cho khoản trợ cấp thôi việc theo đúng quy định BLLĐ (Điều 2.2.b TT 111/2013/TT-BTC).</summary>
    public bool IsSeveranceStatutoryTaxExempt { get; set; } = true;
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
    /// <summary>ResidentStandard (Cư trú >= 3T) | ResidentShortTerm (HĐ < 3T / không HĐ) | NonResident (Không cư trú 20%)</summary>
    public string TaxResidencyMode { get; set; } = "ResidentStandard";
    /// <summary>Cam kết thu nhập chưa đến mức chịu thuế mẫu 08/CK-TNCN (dành cho HĐ < 3 tháng).</summary>
    public bool HasPitCommitment08 { get; set; } = false;
    public string? Commitment08Number { get; set; }
    /// <summary>Hợp đồng cam kết lương Net (dùng binary search giải ngược ra Gross).</summary>
    public bool IsNetContract { get; set; } = false;
    public decimal? TargetNetSalary { get; set; }
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
    /// <summary>Monthly (Lương tháng) | MidMonthAdvance (Tạm ứng giữa tháng) | Bonus13 (Kỳ chi Thưởng tháng 13) | HolidayBonus (Kỳ chi Thưởng Lễ/Tết)</summary>
    public string PeriodType { get; set; } = "Monthly";
    /// <summary>Thứ tự đợt chi trong cùng tháng (1, 2, 3...) để tính gộp thuế TNCN tháng đúng thứ tự.</summary>
    public int DisbursementSequence { get; set; } = 1;
    public DateOnly? DisbursementDate { get; set; }
    public string PeriodTitle { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal StandardWorkdays { get; set; } = 26m;
    public string Status { get; set; } = PayrollPeriodStatuses.Draft;
    public int CalculationVersion { get; set; } = 0;
    public bool HasAttendanceLockWarning { get; set; } = false;
    public string? AttendanceWarningMessage { get; set; }
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
    public decimal OtNightExtraPay { get; set; } = 0m;
    public decimal OtTaxExemptAmount { get; set; } = 0m;
    public decimal KpiBonus { get; set; }
    public decimal CommissionBonus { get; set; } = 0m;
    public decimal ThirteenthMonthBonus { get; set; } = 0m;
    public decimal RetroEarningAmount { get; set; } = 0m;
    public decimal UnusedLeavePayout { get; set; } = 0m;
    public decimal GrossIncome { get; set; }
    public decimal InsuranceSalaryBase { get; set; }
    public decimal UnemploymentInsuranceBase { get; set; }
    public decimal EmployeeSocialInsurance { get; set; }
    public decimal EmployeeHealthInsurance { get; set; }
    public decimal EmployeeUnemploymentInsurance { get; set; }
    public decimal TotalEmployeeInsurance { get; set; }
    public decimal EmployeeUnionFee { get; set; } = 0m;
    public decimal CompanySocialInsurance { get; set; }
    public decimal CompanyHealthInsurance { get; set; }
    public decimal CompanyUnemploymentInsurance { get; set; }
    public decimal CompanyOccupationalAccidentInsurance { get; set; } = 0m;
    public decimal CompanyUnionFee { get; set; }
    public decimal TotalCompanyCost { get; set; } = 0m;
    public string TaxResidencyMode { get; set; } = "ResidentStandard";
    public bool UsedCommitment08 { get; set; } = false;
    public bool IsNetToGross { get; set; } = false;
    public decimal? TargetNetSalary { get; set; }
    public int DependentCount { get; set; }
    public decimal SelfDeductionAmount { get; set; }
    public decimal DependentDeductionAmount { get; set; }
    public decimal TaxableIncome { get; set; }
    public decimal AssessableIncome { get; set; }
    public decimal PersonalIncomeTax { get; set; }
    public string? PitBracketsJson { get; set; }
    public decimal DisciplineDeduction { get; set; }
    public decimal SalaryAdvanceDeduction { get; set; } = 0m;
    public decimal LoanInstallmentDeduction { get; set; } = 0m;
    public decimal RetroDeductionAmount { get; set; } = 0m;
    public decimal NetSalary { get; set; }
    public decimal? PreviousRevisionNetSalary { get; set; }
    public decimal? PreviousPeriodNetSalary { get; set; }
    public decimal? VariancePercentVsPreviousPeriod { get; set; }
    public bool HasHighVarianceWarning { get; set; } = false;
    public string PaymentStatus { get; set; } = "Paid";
    public DateTime? PaidAt { get; set; }
    public string? Notes { get; set; }
    public string? CalculationTraceJson { get; set; }

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

public class SalaryAdvance : BaseAuditableEntity
{
    public int EmployeeId { get; set; }
    public string AdvanceNumber { get; set; } = string.Empty;
    public DateOnly RequestDate { get; set; }
    public int TargetYear { get; set; }
    public int TargetMonth { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    /// <summary>Pending | Approved | Rejected | Cancelled | Deducted</summary>
    public string Status { get; set; } = "Approved";
    public int? WorkflowInstanceId { get; set; }
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApproverComment { get; set; }
    public decimal MaxAllowedAmountSnapshot { get; set; } = 0m;
    public int? PolicyGroupIdUsed { get; set; }
    public int? DisbursementPayrollPeriodId { get; set; }
    public int? DeductedPayrollPeriodId { get; set; }
    public int? TerminationSettlementId { get; set; }

    public Employee Employee { get; set; } = null!;
}

public class EmployeeLoan : BaseAuditableEntity
{
    public int EmployeeId { get; set; }
    public string LoanCode { get; set; } = string.Empty;
    /// <summary>Loan (Khoản vay) | Installment (Khấu trừ trả góp đồng phục/đền bù)</summary>
    public string LoanType { get; set; } = "Loan";
    public decimal PrincipalAmount { get; set; }
    public decimal MonthlyInstallmentAmount { get; set; }
    public int TotalInstallments { get; set; }
    public int StartYear { get; set; }
    public int StartMonth { get; set; }
    public string Reason { get; set; } = string.Empty;
    /// <summary>Active | PaidOff | Paused | Cancelled</summary>
    public string Status { get; set; } = "Active";

    public Employee Employee { get; set; } = null!;
    public ICollection<EmployeeLoanInstallment> Installments { get; set; } = new List<EmployeeLoanInstallment>();
}

public class EmployeeLoanInstallment : BaseAuditableEntity
{
    public int EmployeeLoanId { get; set; }
    public int EmployeeId { get; set; }
    public int InstallmentNumber { get; set; }
    public int TargetYear { get; set; }
    public int TargetMonth { get; set; }
    public decimal Amount { get; set; }
    /// <summary>Scheduled | Deducted | SettledOnTermination | Waived</summary>
    public string Status { get; set; } = "Scheduled";
    public int? DeductedPayrollPeriodId { get; set; }
    public int? TerminationSettlementId { get; set; }

    public EmployeeLoan EmployeeLoan { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
}

public class PayrollAdjustment : BaseAuditableEntity
{
    public string AdjustmentCode { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public int TargetYear { get; set; }
    public int TargetMonth { get; set; }
    public int? SourceLockedPeriodId { get; set; }
    /// <summary>RetroEarning (Truy lĩnh) | RetroDeduction (Truy thu) | Commission (Hoa hồng) | Bonus13 (Thưởng T13) | HolidayBonus (Thưởng Lễ/Tết)</summary>
    public string AdjustmentType { get; set; } = "RetroEarning";
    public bool IsTaxable { get; set; } = true;
    public bool IsSocialInsuranceBase { get; set; } = false;
    public decimal Amount { get; set; }
    public bool RecalculateTaxInSourcePeriod { get; set; } = false;
    public decimal SourcePeriodTaxDelta { get; set; } = 0m;
    public string? TaxCalculationNote { get; set; }
    public string Reason { get; set; } = string.Empty;
    /// <summary>Approved | Applied | Cancelled</summary>
    public string Status { get; set; } = "Approved";
    public int? AppliedPayrollPeriodId { get; set; }
    public int? TerminationSettlementId { get; set; }

    public Employee Employee { get; set; } = null!;
}

/// <summary>
/// Chính sách lương & tạm ứng nội bộ gắn với PolicyGroup (ưu tiên: Nhân viên > Chức danh > Đơn vị > Mặc định công ty).
/// </summary>
public class PayrollPolicy : BaseAuditableEntity
{
    public int PolicyGroupId { get; set; }
    /// <summary>Hạn mức tạm ứng lương tối đa qua ESS (% trên lương cơ bản hàng tháng).</summary>
    public decimal MaxAdvanceLimitPercent { get; set; } = 50.00m;
    /// <summary>Thâm niên tối thiểu (ngày) để được gửi đơn tạm ứng lương.</summary>
    public int AdvanceMinTenureDays { get; set; } = 30;
    /// <summary>Ngày chốt trong tháng nhận đơn tạm ứng lương.</summary>
    public int AdvanceCutoffDayOfMonth { get; set; } = 25;
    /// <summary>Tự động tất toán toàn bộ dư nợ vay/trả góp khi quyết toán nghỉ việc.</summary>
    public bool SettleLoansOnTermination { get; set; } = true;
    /// <summary>RecalculateSourcePeriod (Tính lại thuế đúng kỳ gốc phát sinh) | ApplyInTargetPeriod (Gộp thuế vào kỳ kế tiếp).</summary>
    public string RetroTaxMode { get; set; } = "RecalculateSourcePeriod";
    /// <summary>Mức trợ cấp thôi việc công ty hỗ trợ thêm ngoài mức luật định (tháng lương / năm làm việc).</summary>
    public decimal ExtraSeveranceMonthsPerYear { get; set; } = 0m;
    /// <summary>Đợt chi tạm ứng giữa tháng không khấu trừ tạm thuế, quyết toán thuế cuối tháng.</summary>
    public bool PayMidMonthAdvanceWithoutPit { get; set; } = true;
    public string? LegalReference { get; set; }
    public string? Notes { get; set; }
    public bool NeedsHrVerification { get; set; } = true;

    public PolicyGroup PolicyGroup { get; set; } = null!;
}

/// <summary>
/// Biên bản Quyết toán nghỉ việc (Final Termination Settlement): lương ngày làm cuối, phép tồn, trợ cấp thôi việc, trừ tạm ứng/vay.
/// </summary>
public class EmployeeTerminationSettlement : BaseAuditableEntity
{
    public string SettlementCode { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public DateOnly HireDate { get; set; }
    public DateOnly LastWorkingDate { get; set; }
    /// <summary>Resignation | MutualAgreement | ContractExpiry | Dismissal | IllegalUnilateralTermination</summary>
    public string TerminationType { get; set; } = "Resignation";
    public bool IsEligibleForSeverance { get; set; } = true;
    public decimal StandardWorkdaysUsed { get; set; } = 26m;
    public decimal WorkedDaysInFinalMonth { get; set; }
    public decimal BaseSalaryUsed { get; set; }
    public decimal PositionAllowanceUsed { get; set; }
    public decimal FinalMonthSalaryAmount { get; set; }
    public decimal AverageSalaryLast6Months { get; set; }
    public decimal TotalTenureMonths { get; set; }
    public decimal UnemploymentInsuranceCoveredMonths { get; set; }
    public decimal EligibleSeveranceYears { get; set; }
    public decimal StatutorySeveranceRatePerYear { get; set; }
    public decimal CompanyExtraSeveranceRatePerYear { get; set; }
    public decimal StatutorySeveranceAmount { get; set; }
    public decimal CompanyExtraSeveranceAmount { get; set; }
    public decimal UnusedLeaveDays { get; set; }
    public decimal UnusedLeavePayoutAmount { get; set; }
    public decimal OtherBonusOrRetroAmount { get; set; }
    public decimal TotalGrossSettlement { get; set; }
    public decimal EmployeeInsuranceDeduction { get; set; }
    public decimal PersonalIncomeTaxDeduction { get; set; }
    public decimal SalaryAdvanceDeduction { get; set; }
    public decimal LoanPayoffDeduction { get; set; }
    public decimal OtherDeduction { get; set; }
    public decimal NetSettlementAmount { get; set; }
    public decimal RemainingDebtToRecover { get; set; }
    public string? CalculationBreakdownJson { get; set; }
    /// <summary>Draft | Approved | Paid | Cancelled</summary>
    public string Status { get; set; } = "Draft";
    public int TargetPayrollYear { get; set; }
    public int TargetPayrollMonth { get; set; }
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? Notes { get; set; }

    public Employee Employee { get; set; } = null!;
}

/// <summary>
/// Cấu hình tài khoản hạch toán kế toán tiền lương, BHXH, thuế TNCN (dễ điều chỉnh theo TT 200 / TT 133 hoặc ERP).
/// </summary>
public class PayrollAccountingAccountConfig : BaseAuditableEntity
{
    /// <summary>EXPENSE_SALARY_ADMIN | EXPENSE_SALARY_PROD | EXPENSE_INSURANCE | EXPENSE_UNION | PAYABLE_EMPLOYEE | PAYABLE_SI | PAYABLE_HI | PAYABLE_UI | PAYABLE_OA | PAYABLE_UNION | PAYABLE_PIT | RECEIVABLE_ADVANCE | OTHER_RECOVERY</summary>
    public string AccountKey { get; set; } = string.Empty;
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    /// <summary>Debit | Credit</summary>
    public string NormalSide { get; set; } = "Credit";
    /// <summary>Khớp CostCenterCode (VD: PROD*, MFG*, *). Null hoặc * áp dụng mặc định.</summary>
    public string? CostCenterPattern { get; set; }
    public DateOnly EffectiveFrom { get; set; } = new(2024, 1, 1);
    public DateOnly? EffectiveTo { get; set; }
    public string? LegalReference { get; set; }
    public bool NeedsHrVerification { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;
}

/// <summary>
/// Cấu hình mẫu biểu xuất Excel BHXH (VD: D02-LT) cho phép HR cập nhật cột/ánh xạ khi cơ quan BHXH thay đổi biểu mẫu.
/// </summary>
public class SocialInsuranceReportTemplate : BaseAuditableEntity
{
    public string TemplateCode { get; set; } = "D02-LT";
    public string TemplateName { get; set; } = string.Empty;
    public string SoftwareVendor { get; set; } = "VNPT-BHXH / eBHXH";
    public DateOnly EffectiveFrom { get; set; } = new(2024, 1, 1);
    public DateOnly? EffectiveTo { get; set; }
    public string ColumnMappingsJson { get; set; } = "[]";
    public string? LegalReference { get; set; }
    public bool NeedsHrVerification { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Chứng từ khấu trừ thuế TNCN phát hành theo năm/kỳ cho nhân viên (NĐ 123/2020, NĐ 70/2025, TT 80/2021).
/// </summary>
public class PitWithholdingCertificate : BaseAuditableEntity
{
    public string CertificateCode { get; set; } = string.Empty;
    public int TaxYear { get; set; }
    public int EmployeeId { get; set; }
    public decimal GrossIncome { get; set; }
    public decimal TotalInsuranceDeducted { get; set; }
    public decimal TotalFamilyDeductions { get; set; }
    public decimal TaxableIncome { get; set; }
    public decimal AssessableIncome { get; set; }
    public decimal TotalPitWithheld { get; set; }
    public decimal AnnualFinalTaxLiability { get; set; }
    public decimal PitOverpaidOrUnderpaid { get; set; }
    public DateOnly IssuedDate { get; set; }
    public int IssuedByUserId { get; set; }
    public string IssuedByUsername { get; set; } = string.Empty;
    /// <summary>Issued | Cancelled</summary>
    public string Status { get; set; } = "Issued";
    public string? LegalReference { get; set; }
    public bool NeedsHrVerification { get; set; } = true;

    public Employee Employee { get; set; } = null!;
}

