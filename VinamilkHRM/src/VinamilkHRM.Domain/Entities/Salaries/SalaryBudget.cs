using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.OrganizationalUnits;

namespace VinamilkHRM.Domain.Entities.Salaries;

/// <summary>
/// Ngân sách xét tăng lương của đơn vị trong chu kỳ: Amount, Used.
/// Tổng Giám đốc có quyền điều chỉnh ngân sách.
/// </summary>
public class SalaryBudget : BaseAuditableEntity
{
    public int SalaryReviewCycleId { get; private set; }
    public virtual SalaryReviewCycle? SalaryReviewCycle { get; private set; }

    public int OrganizationalUnitId { get; private set; }
    public virtual OrganizationalUnit? OrganizationalUnit { get; private set; }

    public decimal Amount { get; private set; } // Ngân sách được phân bổ (VND)
    public decimal Used { get; private set; }   // Ngân sách đã đề xuất sử dụng (VND)
    public string? Notes { get; private set; }

    public decimal Remaining => Amount - Used;

    protected SalaryBudget() { }

    public SalaryBudget(
        int salaryReviewCycleId,
        int organizationalUnitId,
        decimal amount,
        string? notes = null)
    {
        if (amount < 0)
            throw new DomainException("Ngân sách tăng lương không thể âm.", "ERR_SAL_BUDGET_NEGATIVE");

        SalaryReviewCycleId = salaryReviewCycleId;
        OrganizationalUnitId = organizationalUnitId;
        Amount = amount;
        Used = 0;
        Notes = notes?.Trim();
    }

    public void UpdateUsedAmount(decimal newUsed)
    {
        if (newUsed < 0)
            throw new DomainException("Số tiền ngân sách đã sử dụng không thể âm.", "ERR_SAL_BUDGET_NEGATIVE");

        Used = newUsed;
    }

    public void AdjustAmount(decimal newAmount, int adjustedBy, string reason)
    {
        if (newAmount < 0)
            throw new DomainException("Hạn mức ngân sách không thể âm.", "ERR_SAL_BUDGET_NEGATIVE");

        Amount = newAmount;
        Notes = $"[CEO Điều chỉnh bởi EmpId:{adjustedBy}] {reason}".Trim();
    }
}