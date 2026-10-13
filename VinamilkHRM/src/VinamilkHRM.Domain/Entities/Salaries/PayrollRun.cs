using System;
using System.Collections.Generic;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Entities.OrganizationalUnits;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Salaries;

/// <summary>
/// Đại diện cho một đợt tính lương (PayrollRun) theo kỳ và phạm vi đơn vị.
/// Quy trình: Draft -> Submitted (Kế toán lương) -> Approved (Kế toán trưởng & GĐNS) -> Locked -> Paid.
/// RÀNG BUỘC PHÁP LÝ: Bất biến từ trạng thái Approved trở đi.
/// </summary>
public class PayrollRun : BaseAuditableEntity
{
    public string Period { get; private set; } = string.Empty; // YYYY-MM
    public int? OrganizationalUnitId { get; private set; }
    public virtual OrganizationalUnit? OrganizationalUnit { get; private set; }

    public PayrollRunStatus Status { get; private set; } = PayrollRunStatus.Draft;

    public decimal TotalGross { get; private set; }
    public decimal TotalNet { get; private set; }
    public decimal TotalEmployerCost { get; private set; }
    public decimal TotalPersonalIncomeTax { get; private set; }
    public decimal TotalEmployeeInsurance { get; private set; }
    public decimal TotalEmployerInsurance { get; private set; }
    public int EmployeeCount { get; private set; }

    public int? SubmittedBy { get; private set; }
    public virtual Employee? SubmittedByEmployee { get; private set; }
    public DateTimeOffset? SubmittedAt { get; private set; }

    public int? ApprovedByChiefAccountant { get; private set; }
    public virtual Employee? ApprovedByChiefAccountantEmployee { get; private set; }
    public DateTimeOffset? ApprovedAccountantAt { get; private set; }

    public int? ApprovedByHrDirector { get; private set; }
    public virtual Employee? ApprovedByHrDirectorEmployee { get; private set; }
    public DateTimeOffset? ApprovedDirectorAt { get; private set; }

    public int? LockedBy { get; private set; }
    public virtual Employee? LockedByEmployee { get; private set; }
    public DateTimeOffset? LockedAt { get; private set; }

    public int? PaidBy { get; private set; }
    public virtual Employee? PaidByEmployee { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public string? PaymentReference { get; private set; }

    private readonly List<PaySlip> _paySlips = new();
    public virtual IReadOnlyCollection<PaySlip> PaySlips => _paySlips.AsReadOnly();

    protected PayrollRun()
    {
    }

    public PayrollRun(string period, int? organizationalUnitId = null)
    {
        if (string.IsNullOrWhiteSpace(period))
            throw new DomainException("Kỳ tính lương không được để trống.", "ERR_PAY_PERIOD_EMPTY");

        Period = period.Trim();
        OrganizationalUnitId = organizationalUnitId;
        Status = PayrollRunStatus.Draft;
    }

    public void EnsureCanRecalculate()
    {
        if (Status >= PayrollRunStatus.Approved)
        {
            throw new DomainException(
                $"Kỳ lương {Period} đã ở trạng thái {Status} và là bất biến, không thể tính toán lại.",
                "ERR_PAY_PERIOD_LOCKED");
        }
    }

    public void UpdateTotals(
        decimal totalGross,
        decimal totalNet,
        decimal totalEmployerCost,
        decimal totalTax,
        decimal totalEmpInsurance,
        decimal totalErInsurance,
        int employeeCount)
    {
        EnsureCanRecalculate();

        TotalGross = totalGross;
        TotalNet = totalNet;
        TotalEmployerCost = totalEmployerCost;
        TotalPersonalIncomeTax = totalTax;
        TotalEmployeeInsurance = totalEmpInsurance;
        TotalEmployerInsurance = totalErInsurance;
        EmployeeCount = employeeCount;
    }

    public void Submit(int accountantUserId)
    {
        EnsureCanRecalculate();
        if (EmployeeCount <= 0)
            throw new DomainException("Kỳ lương chưa có dữ liệu nhân viên, không thể gửi duyệt.", "ERR_PAY_EMPTY_RUN");

        SubmittedBy = accountantUserId;
        SubmittedAt = DateTimeOffset.UtcNow;
        Status = PayrollRunStatus.Submitted;
    }

    public void Approve(int chiefAccountantId, int hrDirectorId)
    {
        if (Status != PayrollRunStatus.Submitted)
            throw new DomainException($"Chỉ có thể phê duyệt khi kỳ lương ở trạng thái Submitted (hiện tại: {Status}).", "ERR_PAY_NOT_SUBMITTED");

        ApprovedByChiefAccountant = chiefAccountantId;
        ApprovedAccountantAt = DateTimeOffset.UtcNow;

        ApprovedByHrDirector = hrDirectorId;
        ApprovedDirectorAt = DateTimeOffset.UtcNow;

        Status = PayrollRunStatus.Approved;
    }

    public void Lock(int hrUserId)
    {
        if (Status != PayrollRunStatus.Approved)
            throw new DomainException("Chỉ kỳ lương đã được Kế toán trưởng và Giám đốc phê duyệt mới có thể khóa sổ.", "ERR_PAY_NOT_APPROVED");

        LockedBy = hrUserId;
        LockedAt = DateTimeOffset.UtcNow;
        Status = PayrollRunStatus.Locked;
    }

    public void MarkAsPaid(int financeUserId, string paymentReference)
    {
        if (Status != PayrollRunStatus.Approved && Status != PayrollRunStatus.Locked)
            throw new DomainException("Kỳ lương phải được phê duyệt trước khi chuyển trạng thái Đã chi.", "ERR_PAY_NOT_APPROVED");

        if (string.IsNullOrWhiteSpace(paymentReference))
            throw new DomainException("Mã tham chiếu giao dịch ngân hàng không được để trống.", "ERR_PAY_REF_EMPTY");

        PaidBy = financeUserId;
        PaymentReference = paymentReference.Trim();
        PaidAt = DateTimeOffset.UtcNow;
        Status = PayrollRunStatus.Paid;
    }
}
