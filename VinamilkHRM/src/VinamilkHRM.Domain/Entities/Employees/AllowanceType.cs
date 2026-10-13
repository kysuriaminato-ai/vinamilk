using VinamilkHRM.Domain.Common;

namespace VinamilkHRM.Domain.Entities.Employees;

/// <summary>
/// Danh mục loại phụ cấp theo hợp đồng lao động và quy chế tiền lương Vinamilk
/// (Phụ cấp trách nhiệm, độc hại/nặng nhọc, làm đêm, ăn ca, xăng xe, điện thoại, nhà ở...).
/// </summary>
public class AllowanceType : BaseAuditableEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsInsurable { get; private set; } = false;     // Tính vào mức lương đóng BHXH/BHYT/BHTN
    public bool IsTaxable { get; private set; } = true;        // Chịu thuế TNCN
    public decimal TaxExemptCap { get; private set; } = 0m;    // Hạn mức miễn thuế TNCN (VND)
    public bool IsActive { get; private set; } = true;

    protected AllowanceType()
    {
    }

    public AllowanceType(
        string code,
        string name,
        bool isInsurable = false,
        bool isTaxable = true,
        decimal taxExemptCap = 0m,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Mã loại phụ cấp không được để trống.", "ERR_ALLOWANCE_CODE_EMPTY");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Tên loại phụ cấp không được để trống.", "ERR_ALLOWANCE_NAME_EMPTY");

        if (taxExemptCap < 0)
            throw new DomainException("Hạn mức miễn thuế phụ cấp không thể âm.", "ERR_ALLOWANCE_CAP_NEGATIVE");

        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        IsInsurable = isInsurable;
        IsTaxable = isTaxable;
        TaxExemptCap = taxExemptCap;
        Description = description?.Trim();
        IsActive = true;
    }
}
