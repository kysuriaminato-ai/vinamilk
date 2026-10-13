using VinamilkHRM.Domain.Services.Payroll;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Entities.Statutory;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Salaries;

/// <summary>
/// Đại diện cho Phiếu lương chi tiết hàng tháng của cán bộ nhân viên Vinamilk.
/// Hỗ trợ tính toán bảo hiểm, giảm trừ gia cảnh và thuế TNCN nạp động từ tham số pháp lý.
/// </summary>
public class PaySlip : BaseAuditableEntity
{
    public int? PayrollRunId { get; private set; }
    public virtual PayrollRun? PayrollRun { get; private set; }
    public decimal EmployerCost { get; private set; }
    public string? FormulaTraceJson { get; private set; }
    public int EmployeeId { get; private set; }
    public virtual Employee? Employee { get; private set; }

    public string PayrollPeriod { get; private set; } = string.Empty; // Format: "YYYY-MM"
    public decimal StandardWorkDays { get; private set; } = 22m;
    public decimal ActualWorkDays { get; private set; }
    public decimal BaseSalary { get; private set; }
    public decimal Allowance { get; private set; }
    public decimal LunchAllowance { get; private set; }
    public decimal OvertimeHours { get; private set; }
    public decimal OvertimePay { get; private set; }
    public decimal NightShiftHours { get; private set; }
    public decimal NightShiftPay { get; private set; }                  // Phụ cấp làm ca đêm (+30%)
    public decimal HazardousAllowance { get; private set; }             // Phụ cấp nặng nhọc/độc hại nhà máy
    public decimal SalesCommission { get; private set; }                 // Hoa hồng doanh số bán lẻ chuỗi cửa hàng
    public decimal KpiBonus { get; private set; }
    public decimal OtherBonuses { get; private set; }

    public decimal GrossSalary { get; private set; }
    public decimal SocialInsuranceDeduction { get; private set; }       // BHXH NLĐ
    public decimal HealthInsuranceDeduction { get; private set; }       // BHYT NLĐ
    public decimal UnemploymentInsuranceDeduction { get; private set; } // BHTN NLĐ
    public int DependentCount { get; private set; }                     // Số người phụ thuộc
    public decimal PersonalDeduction { get; private set; }              // Giảm trừ bản thân
    public decimal DependentDeduction { get; private set; }             // Giảm trừ người phụ thuộc
    public decimal TaxableIncome { get; private set; }                  // Thu nhập tính thuế TNCN
    public decimal PersonalIncomeTax { get; private set; }              // Thuế TNCN lũy tiến
    public decimal TotalDeductions { get; private set; }
    public decimal NetSalary { get; private set; }

    public PaySlipStatus Status { get; private set; } = PaySlipStatus.Draft;
    public DateTime? PaymentDate { get; private set; }
    public string? TransactionReference { get; private set; }
    public bool IsImported { get; private set; } = false;
    public int? LegacyId { get; private set; }

    protected PaySlip()
    {
    }

    public PaySlip(
        int employeeId,
        string payrollPeriod,
        decimal baseSalary,
        decimal allowance,
        decimal actualWorkDays,
        decimal overtimeHours,
        decimal overtimePay,
        decimal kpiBonus,
        decimal otherBonuses,
        decimal insuranceSalary,
        decimal standardWorkDays = 22m,
        int dependentCount = 0,
        decimal nightShiftHours = 0m,
        decimal nightShiftPay = 0m,
        decimal hazardousAllowance = 0m,
        decimal salesCommission = 0m,
        decimal lunchAllowance = 0m,
        decimal? personalDeduction = null,
        decimal? dependentDeductionPerPerson = null,
        IReadOnlyList<TaxBracket>? taxBrackets = null,
        decimal socialInsuranceRate = 0.08m,
        decimal healthInsuranceRate = 0.015m,
        decimal unemploymentInsuranceRate = 0.01m)
    {
        if (employeeId <= 0)
            throw new DomainException("Mã nhân viên phiếu lương không hợp lệ.", "ERR_PAY_EMP_INVALID");

        if (string.IsNullOrWhiteSpace(payrollPeriod))
            throw new DomainException("Kỳ trả lương (YYYY-MM) không được để trống.", "ERR_PAY_PERIOD_EMPTY");

        if (baseSalary < 0 || allowance < 0 || insuranceSalary < 0)
            throw new DomainException("Số tiền lương cơ bản, phụ cấp và lương bảo hiểm không thể âm.", "ERR_PAY_SALARY_NEGATIVE");

        EmployeeId = employeeId;
        PayrollPeriod = payrollPeriod.Trim();
        BaseSalary = baseSalary;
        Allowance = allowance;
        LunchAllowance = lunchAllowance >= 0 ? lunchAllowance : 0m;
        StandardWorkDays = standardWorkDays > 0 ? standardWorkDays : 22m;
        ActualWorkDays = actualWorkDays >= 0 ? actualWorkDays : 0m;
        OvertimeHours = overtimeHours >= 0 ? overtimeHours : 0m;
        OvertimePay = overtimePay >= 0 ? overtimePay : 0m;
        NightShiftHours = nightShiftHours >= 0 ? nightShiftHours : 0m;
        NightShiftPay = nightShiftPay >= 0 ? nightShiftPay : 0m;
        HazardousAllowance = hazardousAllowance >= 0 ? hazardousAllowance : 0m;
        SalesCommission = salesCommission >= 0 ? salesCommission : 0m;
        KpiBonus = kpiBonus >= 0 ? kpiBonus : 0m;
        OtherBonuses = otherBonuses >= 0 ? otherBonuses : 0m;
        DependentCount = Math.Max(0, dependentCount);
        IsImported = false;

        CalculateNetSalary(
            insuranceSalary,
            personalDeduction,
            dependentDeductionPerPerson,
            taxBrackets,
            socialInsuranceRate,
            healthInsuranceRate,
            unemploymentInsuranceRate);
    }

    /// <summary>
    /// Phương thức khởi tạo đặc biệt dành cho dữ liệu di sản chuyển đổi từ MySQL (bangluong).
    /// Phiếu lương được đánh dấu IsImported = true, trạng thái Paid (chỉ đọc).
    /// </summary>
    public static PaySlip CreateImported(
        int employeeId,
        string payrollPeriod,
        decimal baseSalary,
        decimal allowance,
        decimal actualWorkDays,
        decimal grossSalary,
        decimal totalDeductions,
        decimal netSalary,
        int legacyId,
        DateTime paymentDate,
        string transactionRef = "MIGRATED_LEGACY")
    {
        var slip = new PaySlip
        {
            EmployeeId = employeeId,
            PayrollPeriod = payrollPeriod,
            BaseSalary = baseSalary,
            Allowance = allowance,
            ActualWorkDays = actualWorkDays,
            GrossSalary = grossSalary,
            TotalDeductions = totalDeductions,
            NetSalary = netSalary,
            Status = PaySlipStatus.Paid,
            IsImported = true,
            LegacyId = legacyId,
            PaymentDate = paymentDate,
            TransactionReference = transactionRef
        };
        return slip;
    }

    /// <summary>
    /// Tính toán thu nhập Gross, trích nộp bảo hiểm, giảm trừ gia cảnh, thuế TNCN và thực lĩnh Net.
    /// Cho phép nạp động các tham số pháp lý theo ngày hiệu lực.
    /// </summary>
    public void CalculateNetSalary(
        decimal insuranceSalary,
        decimal? personalDeduction = null,
        decimal? dependentDeductionPerPerson = null,
        IReadOnlyList<TaxBracket>? taxBrackets = null,
        decimal socialInsuranceRate = 0.08m,
        decimal healthInsuranceRate = 0.015m,
        decimal unemploymentInsuranceRate = 0.01m)
    {
        if (IsImported)
            throw new DomainException("Phiếu lương di sản đã khóa không thể tính toán lại.", "ERR_PAY_IMPORTED_READONLY");

        decimal workRatio = StandardWorkDays > 0 ? Math.Min(1.5m, ActualWorkDays / StandardWorkDays) : 1m;
        decimal earnedBaseSalary = Math.Round(BaseSalary * workRatio, 0, MidpointRounding.AwayFromZero);

        GrossSalary = earnedBaseSalary + Allowance + LunchAllowance + OvertimePay + NightShiftPay + HazardousAllowance + SalesCommission + KpiBonus + OtherBonuses;

        // Trích nộp bảo hiểm bắt buộc trên lương đóng bảo hiểm
        SocialInsuranceDeduction = Math.Round(insuranceSalary * socialInsuranceRate, 0, MidpointRounding.AwayFromZero);
        HealthInsuranceDeduction = Math.Round(insuranceSalary * healthInsuranceRate, 0, MidpointRounding.AwayFromZero);
        UnemploymentInsuranceDeduction = Math.Round(insuranceSalary * unemploymentInsuranceRate, 0, MidpointRounding.AwayFromZero);

        decimal statutoryInsuranceTotal = SocialInsuranceDeduction + HealthInsuranceDeduction + UnemploymentInsuranceDeduction;

        // Giảm trừ gia cảnh theo tham số pháp lý
        PersonalDeduction = personalDeduction ?? 11_000_000m;
        decimal depPerPerson = dependentDeductionPerPerson ?? 4_400_000m;
        DependentDeduction = DependentCount * depPerPerson;
        decimal totalRelief = PersonalDeduction + DependentDeduction;

        TaxableIncome = Math.Max(0m, GrossSalary - statutoryInsuranceTotal - totalRelief);

        // Tính thuế TNCN lũy tiến theo biểu thuế được truyền vào (hoặc mặc định)
        PersonalIncomeTax = CalculateTax(TaxableIncome, taxBrackets);

        TotalDeductions = statutoryInsuranceTotal + PersonalIncomeTax;
        NetSalary = Math.Max(0m, GrossSalary - TotalDeductions);
    }

    private static decimal CalculateTax(decimal taxableIncome, IReadOnlyList<TaxBracket>? taxBrackets)
    {
        if (taxableIncome <= 0) return 0m;

        if (taxBrackets != null && taxBrackets.Count > 0)
        {
            decimal tax = 0m;
            decimal previousUpper = 0m;

            foreach (var bracket in taxBrackets.OrderBy(b => b.Order))
            {
                if (taxableIncome <= previousUpper) break;

                decimal taxableInThisBracket = bracket.UpperBoundMonthly.HasValue
                    ? Math.Min(taxableIncome, bracket.UpperBoundMonthly.Value) - previousUpper
                    : taxableIncome - previousUpper;

                if (taxableInThisBracket > 0)
                {
                    tax += Math.Round(taxableInThisBracket * bracket.Rate, 0, MidpointRounding.AwayFromZero);
                }

                if (!bracket.UpperBoundMonthly.HasValue) break;
                previousUpper = bracket.UpperBoundMonthly.Value;
            }

            return Math.Round(tax, 0, MidpointRounding.AwayFromZero);
        }

        // Biểu thuế lũy tiến 7 bậc tiêu chuẩn trước 2026
        if (taxableIncome <= 5_000_000m) return Math.Round(taxableIncome * 0.05m, 0, MidpointRounding.AwayFromZero);
        if (taxableIncome <= 10_000_000m) return Math.Round(250_000m + (taxableIncome - 5_000_000m) * 0.10m, 0, MidpointRounding.AwayFromZero);
        if (taxableIncome <= 18_000_000m) return Math.Round(750_000m + (taxableIncome - 10_000_000m) * 0.15m, 0, MidpointRounding.AwayFromZero);
        if (taxableIncome <= 32_000_000m) return Math.Round(1_950_000m + (taxableIncome - 18_000_000m) * 0.20m, 0, MidpointRounding.AwayFromZero);
        if (taxableIncome <= 52_000_000m) return Math.Round(4_750_000m + (taxableIncome - 32_000_000m) * 0.25m, 0, MidpointRounding.AwayFromZero);
        if (taxableIncome <= 80_000_000m) return Math.Round(9_750_000m + (taxableIncome - 52_000_000m) * 0.30m, 0, MidpointRounding.AwayFromZero);
        return Math.Round(18_150_000m + (taxableIncome - 80_000_000m) * 0.35m, 0, MidpointRounding.AwayFromZero);
    }

    public void Approve()
    {
        if (Status != PaySlipStatus.Draft && Status != PaySlipStatus.PendingApproval)
            throw new DomainException($"Phiếu lương đang ở trạng thái {Status}, không thể phê duyệt lại.", "ERR_PAY_CANNOT_APPROVE");

        Status = PaySlipStatus.Approved;
    }

    public void MarkAsPaid(string transactionReference, DateTime? paymentDate = null)
    {
        if (Status != PaySlipStatus.Approved && !IsImported)
            throw new DomainException("Chỉ phiếu lương đã được phê duyệt mới có thể chuyển trạng thái Đã thanh toán.", "ERR_PAY_NOT_APPROVED");

        if (string.IsNullOrWhiteSpace(transactionReference))
            throw new DomainException("Mã giao dịch chi lương ngân hàng không được để trống.", "ERR_PAY_REF_EMPTY");

        Status = PaySlipStatus.Paid;
        TransactionReference = transactionReference.Trim();
        PaymentDate = paymentDate ?? DateTime.UtcNow;
    }

    public void SetPayrollRun(int payrollRunId)
    {
        PayrollRunId = payrollRunId;
    }

    public void UpdateFromCalculation(PayrollResult result, int? payrollRunId = null)
    {
        if (Status >= PaySlipStatus.Approved)
            throw new DomainException("Phiếu lương đã được duyệt hoặc chi trả, không thể cập nhật.", "ERR_PAY_PERIOD_LOCKED");

        if (payrollRunId.HasValue) PayrollRunId = payrollRunId.Value;
        StandardWorkDays = result.StandardWorkDays;
        ActualWorkDays = result.ActualWorkDays;
        GrossSalary = result.GrossSalary;
        SocialInsuranceDeduction = result.EmployeeSocialInsurance;
        HealthInsuranceDeduction = result.EmployeeHealthInsurance;
        UnemploymentInsuranceDeduction = result.EmployeeUnemploymentInsurance;
        PersonalDeduction = result.SelfRelief;
        DependentDeduction = result.DependentRelief;
        TaxableIncome = result.TaxAssessedIncome;
        PersonalIncomeTax = result.PersonalIncomeTax;
        TotalDeductions = result.TotalDeductions;
        NetSalary = result.NetSalary;
        EmployerCost = result.TotalEmployerCost;
        FormulaTraceJson = System.Text.Json.JsonSerializer.Serialize(result.Lines);
    }

    public static PaySlip CreateFromCalculation(PayrollResult result, int? payrollRunId = null)
    {
        var slip = new PaySlip
        {
            EmployeeId = result.EmployeeId,
            PayrollPeriod = result.PayrollPeriod,
            PayrollRunId = payrollRunId,
            StandardWorkDays = result.StandardWorkDays,
            ActualWorkDays = result.ActualWorkDays,
            GrossSalary = result.GrossSalary,
            SocialInsuranceDeduction = result.EmployeeSocialInsurance,
            HealthInsuranceDeduction = result.EmployeeHealthInsurance,
            UnemploymentInsuranceDeduction = result.EmployeeUnemploymentInsurance,
            PersonalDeduction = result.SelfRelief,
            DependentDeduction = result.DependentRelief,
            TaxableIncome = result.TaxAssessedIncome,
            PersonalIncomeTax = result.PersonalIncomeTax,
            TotalDeductions = result.TotalDeductions,
            NetSalary = result.NetSalary,
            EmployerCost = result.TotalEmployerCost,
            FormulaTraceJson = System.Text.Json.JsonSerializer.Serialize(result.Lines),
            Status = PaySlipStatus.Draft
        };
        return slip;
    }
}
