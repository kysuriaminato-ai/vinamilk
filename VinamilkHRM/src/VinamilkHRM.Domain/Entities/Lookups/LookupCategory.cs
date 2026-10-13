using VinamilkHRM.Domain.Common;

namespace VinamilkHRM.Domain.Entities.Lookups;

/// <summary>
/// Danh mục phân loại dùng chung chuyển đổi từ các bảng dm_* của MySQL
/// (Dân tộc, Tôn giáo, Trình độ học vấn, Ngành đào tạo, Trường đào tạo, Ngoại ngữ, Ngạch công chức, Chức danh bổ nhiệm, Khen thưởng/Kỷ luật...).
/// </summary>
public class LookupCategory : BaseAuditableEntity
{
    public string Code { get; private set; } = string.Empty; // ETHNICITY, RELIGION, EDUCATION_LEVEL,...
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsSystem { get; private set; } = false;

    private readonly List<LookupItem> _items = new();
    public virtual IReadOnlyCollection<LookupItem> Items => _items.AsReadOnly();

    protected LookupCategory()
    {
    }

    public LookupCategory(string code, string name, string? description = null, bool isSystem = false)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Mã nhóm danh mục không được để trống.", "ERR_LOOKUP_CAT_CODE_EMPTY");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Tên nhóm danh mục không được để trống.", "ERR_LOOKUP_CAT_NAME_EMPTY");

        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Description = description?.Trim();
        IsSystem = isSystem;
    }
}
