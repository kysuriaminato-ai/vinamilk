using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Jobs;

/// <summary>
/// Chức danh công việc tiêu chuẩn tại Vinamilk.
/// Liên kết với Khối ngành (JobFamily), Ngạch bậc tiêu chuẩn (JobGrade), và Cấp lãnh đạo (LeadershipLevel).
/// </summary>
public class JobTitle : BaseAuditableEntity
{
    public string JobTitleCode { get; private set; } = string.Empty;
    public string JobTitleName { get; private set; } = string.Empty;
    public int JobFamilyId { get; private set; }
    public virtual JobFamily? JobFamily { get; private set; }

    public LeadershipLevel LeadershipLevel { get; private set; } = LeadershipLevel.None;
    public int? StandardJobGradeId { get; private set; }
    public virtual JobGrade? StandardJobGrade { get; private set; }

    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? LegacyId { get; private set; }

    protected JobTitle()
    {
    }

    public JobTitle(
        string jobTitleCode,
        string jobTitleName,
        int jobFamilyId,
        LeadershipLevel leadershipLevel = LeadershipLevel.None,
        int? standardJobGradeId = null,
        string? description = null,
        string? legacyId = null)
    {
        if (string.IsNullOrWhiteSpace(jobTitleCode))
            throw new DomainException("Mã chức danh công việc không được để trống.", "ERR_JOB_TITLE_CODE_EMPTY");

        if (string.IsNullOrWhiteSpace(jobTitleName))
            throw new DomainException("Tên chức danh công việc không được để trống.", "ERR_JOB_TITLE_NAME_EMPTY");

        if (jobFamilyId <= 0)
            throw new DomainException("Mã khối chức danh không hợp lệ.", "ERR_JOB_FAMILY_INVALID");

        JobTitleCode = jobTitleCode.Trim().ToUpperInvariant();
        JobTitleName = jobTitleName.Trim();
        JobFamilyId = jobFamilyId;
        LeadershipLevel = leadershipLevel;
        StandardJobGradeId = standardJobGradeId;
        Description = description?.Trim();
        LegacyId = legacyId?.Trim();
        IsActive = true;
    }
}
