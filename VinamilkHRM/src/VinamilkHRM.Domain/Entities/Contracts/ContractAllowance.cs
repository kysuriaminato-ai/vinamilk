using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;

namespace VinamilkHRM.Domain.Entities.Contracts;

/// <summary>
/// Khoản phụ cấp được quy định cụ thể theo từng hợp đồng lao động.
/// </summary>
public class ContractAllowance : BaseAuditableEntity
{
    public int LaborContractId { get; private set; }
    public virtual LaborContract? LaborContract { get; private set; }

    public int AllowanceTypeId { get; private set; }
    public virtual AllowanceType? AllowanceType { get; private set; }

    public decimal Amount { get; private set; }
    public string? Note { get; private set; }

    protected ContractAllowance()
    {
    }

    public ContractAllowance(int laborContractId, int allowanceTypeId, decimal amount, string? note = null)
    {
        if (laborContractId <= 0)
            throw new DomainException("Mã hợp đồng lao động không hợp lệ.", "ERR_CONT_ALLOW_CONTRACT_INVALID");

        if (allowanceTypeId <= 0)
            throw new DomainException("Mã loại phụ cấp không hợp lệ.", "ERR_CONT_ALLOW_TYPE_INVALID");

        if (amount < 0)
            throw new DomainException("Số tiền phụ cấp không thể âm.", "ERR_CONT_ALLOW_AMOUNT_NEGATIVE");

        LaborContractId = laborContractId;
        AllowanceTypeId = allowanceTypeId;
        Amount = amount;
        Note = note?.Trim();
    }
}
