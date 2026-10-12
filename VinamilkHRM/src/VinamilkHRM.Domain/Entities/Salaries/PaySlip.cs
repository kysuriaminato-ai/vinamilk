using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Salaries;

/// <summary>
/// Đại diện cho Phiếu lương chi tiết hàng tháng của cán bộ nhân viên Vinamilk.
/// Đảm bảo tính đóng gói logic tính toán BHXH, BHYT, BHTN, thuế TNCN và tiền thực lĩnh (Net Salary).
/// </summary>
public class PaySlip : BaseAuditableEntity
{
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
    public decimal SocialInsuranceDeduction { get; private set; }       // 8% BHXH
    public decimal HealthInsuranceDeduction { get; private set; }       // 1.5% BHYT
    public decimal UnemploymentInsuranceDeduction { get; private set; } // 1% BHTN
    public int DependentCount { get; private set; }                     // Số người phụ thuộc
    public decimal PersonalDeduction { get; private set; }              // Giảm trừ bản thân (11tr)
    public decimal DependentDeduction { get; private set; }             // Giảm trừ người phụ thuộc (4.4tr/người)
    public decimal TaxableIncome { get; private set; }                  // Thu nhập tính thuế TNCN
    public decimal PersonalIncomeTax { get; private set; }              // Thuế TNCN lũy tiến 7 bậc
    public decimal TotalDeductions { get; private set; }
    public decimal NetSalary { get; private set; }

    public PaySlipStatus Status { get; private set; } = PaySlipStatus.Draft;
    public DateTime? PaymentDate { get; private set; }
    public string? TransactionReference { get; private set; }

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
        decimal lunchAllowance = 0m)
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

        CalculateNetSalary(insuranceSalary);
    }

    /// <summary>
    /// Tính toán tổng thu nhập trước thuế (Gross), các khoản trích nộp theo lương theo Luật VN,
    /// giảm trừ gia cảnh (bản thân 11tr, người phụ thuộc 4.4tr/người), biểu thuế TNCN lũy tiến 7 bậc, và lương Net.
    /// </summary>
    public void CalculateNetSalary(decimal insuranceSalary)
    {
        decimal workRatio = StandardWorkDays > 0 ? Math.Min(1.5m, ActualWorkDays / StandardWorkDays) : 1m;
        decimal earnedBaseSalary = Math.Round(BaseSalary * workRatio, 0);

        GrossSalary = earnedBaseSalary + Allowance + LunchAllowance + OvertimePay + NightShiftPay + HazardousAllowance + SalesCommission + KpiBonus + OtherBonuses;

        // Trích nộp BHXH (8%), BHYT (1.5%), BHTN (1%) trên lương đóng bảo hiểm
        SocialInsuranceDeduction = Math.Round(insuranceSalary * 0.08m, 0);
        HealthInsuranceDeduction = Math.Round(insuranceSalary * 0.015m, 0);
        UnemploymentInsuranceDeduction = Math.Round(insuranceSalary * 0.01m, 0);

        decimal statutoryInsuranceTotal = SocialInsuranceDeduction + HealthInsuranceDeduction + UnemploymentInsuranceDeduction;

        // Giảm trừ gia cảnh theo Luật Thuế TNCN Việt Nam:
        // - Bản thân: 11,000,000 đ/tháng
        // - Người phụ thuộc: 4,400,000 đ/người/tháng
        PersonalDeduction = 11_000_000m;
        DependentDeduction = DependentCount * 4_400_000m;
        decimal totalRelief = PersonalDeduction + DependentDeduction;

        TaxableIncome = Math.Max(0m, GrossSalary - statutoryInsuranceTotal - totalRelief);

        // Biểu thuế lũy tiến từng phần 7 bậc (Luật Thuế TNCN VN)
        PersonalIncomeTax = CalculateProgressiveTax(TaxableIncome);

        TotalDeductions = statutoryInsuranceTotal + PersonalIncomeTax;
        NetSalary = Math.Max(0m, GrossSalary - TotalDeductions);
    }

    private static decimal CalculateProgressiveTax(decimal taxableIncome)
    {
        if (taxableIncome <= 0) return 0m;
        if (taxableIncome <= 5_000_000m) return Math.Round(taxableIncome * 0.05m, 0);
        if (taxableIncome <= 10_000_000m) return Math.Round(250_000m + (taxableIncome - 5_000_000m) * 0.10m, 0);
        if (taxableIncome <= 18_000_000m) return Math.Round(750_000m + (taxableIncome - 10_000_000m) * 0.15m, 0);
        if (taxableIncome <= 32_000_000m) return Math.Round(1_950_000m + (taxableIncome - 18_000_000m) * 0.20m, 0);
        if (taxableIncome <= 52_000_000m) return Math.Round(4_750_000m + (taxableIncome - 32_000_000m) * 0.25m, 0);
        if (taxableIncome <= 80_000_000m) return Math.Round(9_750_000m + (taxableIncome - 52_000_000m) * 0.30m, 0);
        return Math.Round(18_150_000m + (taxableIncome - 80_000_000m) * 0.35m, 0);
    }

    public void Approve()
    {
        if (Status != PaySlipStatus.Draft && Status != PaySlipStatus.PendingApproval)
            throw new DomainException($"Phiếu lương đang ở trạng thái {Status}, không thể phê duyệt lại.", "ERR_PAY_CANNOT_APPROVE");

        Status = PaySlipStatus.Approved;
    }

    public void MarkAsPaid(string transactionReference, DateTime? paymentDate = null)
    {
        if (Status != PaySlipStatus.Approved)
            throw new DomainException("Chỉ phiếu lương đã được phê duyệt mới có thể chuyển trạng thái Đã thanh toán.", "ERR_PAY_NOT_APPROVED");

        if (string.IsNullOrWhiteSpace(transactionReference))
            throw new DomainException("Mã giao dịch chi lương ngân hàng không được để trống.", "ERR_PAY_REF_EMPTY");

        Status = PaySlipStatus.Paid;
        TransactionReference = transactionReference.Trim();
        PaymentDate = paymentDate ?? DateTime.UtcNow;
    }
}
