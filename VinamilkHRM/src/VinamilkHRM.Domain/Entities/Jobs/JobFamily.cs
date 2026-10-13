using VinamilkHRM.Domain.Common;

namespace VinamilkHRM.Domain.Entities.Jobs;

/// <summary>
/// Khối ngành chức danh công việc (Kỹ thuật/Bảo trì, Sản xuất chế biến, Chăn nuôi trang trại, Kinh doanh/Bán lẻ, Tài chính/Kế toán, Nhân sự...).
/// </summary>
public class JobFamily : BaseAuditableEntity
{
    public string FamilyCode { get; private set; } = string.Empty;
    public string FamilyName { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    private readonly List<JobTitle> _jobTitles = new();
    public virtual IReadOnlyCollection<JobTitle> JobTitles => _jobTitles.AsReadOnly();

    protected JobFamily()
    {
    }

    public JobFamily(string familyCode, string familyName, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(familyCode))
            throw new DomainException("Mã khối chức danh không được để trống.", "ERR_JOB_FAMILY_CODE_EMPTY");

        if (string.IsNullOrWhiteSpace(familyName))
            throw new DomainException("Tên khối chức danh không được để trống.", "ERR_JOB_FAMILY_NAME_EMPTY");

        FamilyCode = familyCode.Trim().ToUpperInvariant();
        FamilyName = familyName.Trim();
        Description = description?.Trim();
        IsActive = true;
    }
}
