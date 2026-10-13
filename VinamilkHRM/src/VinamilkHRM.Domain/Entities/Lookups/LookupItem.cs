using VinamilkHRM.Domain.Common;

namespace VinamilkHRM.Domain.Entities.Lookups;

/// <summary>
/// Chi tiết phần tử danh mục dùng chung thuộc LookupCategory.
/// </summary>
public class LookupItem : BaseAuditableEntity
{
    public int CategoryId { get; private set; }
    public virtual LookupCategory? Category { get; private set; }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public int SortOrder { get; private set; } = 0;
    public bool IsActive { get; private set; } = true;
    public string? Description { get; private set; }

    protected LookupItem()
    {
    }

    public LookupItem(
        int categoryId,
        string code,
        string name,
        int sortOrder = 0,
        bool isActive = true,
        string? description = null)
    {
        if (categoryId <= 0)
            throw new DomainException("Mã nhóm danh mục không hợp lệ.", "ERR_LOOKUP_ITEM_CAT_INVALID");

        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Mã phần tử danh mục không được để trống.", "ERR_LOOKUP_ITEM_CODE_EMPTY");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Tên phần tử danh mục không được để trống.", "ERR_LOOKUP_ITEM_NAME_EMPTY");

        CategoryId = categoryId;
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        SortOrder = sortOrder;
        IsActive = isActive;
        Description = description?.Trim();
    }
}
