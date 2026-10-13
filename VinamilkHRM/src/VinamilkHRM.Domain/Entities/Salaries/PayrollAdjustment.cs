using System;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;

namespace VinamilkHRM.Domain.Entities.Salaries;

/// <summary>
/// Khoản điều chỉnh tiền lương (PayrollAdjustment) áp dụng vào kỳ sau.
/// Giải pháp sửa sai sót sau khi kỳ lương đã bị khóa hoặc phê duyệt mà vẫn đảm bảo tính bất biến.
/// </summary>
public class PayrollAdjustment : BaseAuditableEntity
{
    public int EmployeeId { get; private set; }
    public virtual Employee? Employee { get; private set; }

    public string SourcePeriod { get; private set; } = string.Empty; // Kỳ gốc phát sinh sai sót (YYYY-MM)
    public string TargetPeriod { get; private set; } = string.Empty; // Kỳ áp dụng điều chỉnh (YYYY-MM)

    public string AdjustmentType { get; private set; } = "ARREARS"; // ARREARS (Truy lĩnh), DEDUCTION (Khấu trừ)
    public decimal Amount { get; private set; }
    public string Reason { get; private set; } = string.Empty;

    public int? ApprovedBy { get; private set; }
    public virtual Employee? ApprovedByEmployee { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }

    public bool IsApplied { get; private set; } = false;

    protected PayrollAdjustment()
    {
    }

    public PayrollAdjustment(
        int employeeId,
        string sourcePeriod,
        string targetPeriod,
        string adjustmentType,
        decimal amount,
        string reason,
        int approvedBy)
    {
        if (employeeId <= 0)
            throw new DomainException("Mã nhân viên không hợp lệ.", "ERR_ADJ_EMP_INVALID");

        if (string.IsNullOrWhiteSpace(sourcePeriod) || string.IsNullOrWhiteSpace(targetPeriod))
            throw new DomainException("Kỳ lương điều chỉnh không được để trống.", "ERR_ADJ_PERIOD_EMPTY");

        if (amount <= 0)
            throw new DomainException("Số tiền điều chỉnh phải lớn hơn 0.", "ERR_ADJ_AMOUNT_INVALID");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Lý do điều chỉnh lương là bắt buộc.", "ERR_ADJ_REASON_EMPTY");

        EmployeeId = employeeId;
        SourcePeriod = sourcePeriod.Trim();
        TargetPeriod = targetPeriod.Trim();
        AdjustmentType = adjustmentType.Trim().ToUpperInvariant();
        Amount = amount;
        Reason = reason.Trim();
        ApprovedBy = approvedBy;
        ApprovedAt = DateTimeOffset.UtcNow;
        IsApplied = false;
    }

    public void MarkApplied()
    {
        IsApplied = true;
    }
}
