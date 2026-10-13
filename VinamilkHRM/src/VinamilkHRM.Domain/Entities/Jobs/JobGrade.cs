using VinamilkHRM.Domain.Common;

namespace VinamilkHRM.Domain.Entities.Jobs;

/// <summary>
/// Bậc ngạch lương chức danh từ bậc 1 đến bậc 15 của tập đoàn Vinamilk.
/// Bậc 1-5: Lao động phổ thông, công nhân, nhân viên tác nghiệp.
/// Bậc 6-9: Chuyên viên, kỹ sư, giám sát, trưởng ca.
/// Bậc 10-12: Quản lý, trưởng phòng, quản đốc xưởng, giám đốc trang trại.
/// Bậc 13-15: Giám đốc khối/nhà máy, Ban Điều hành, C-level.
/// </summary>
public class JobGrade : BaseAuditableEntity
{
    public int GradeNumber { get; private set; } // 1 - 15
    public string GradeCode { get; private set; } = string.Empty;
    public string GradeName { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? LegacyId { get; private set; }

    private readonly List<SalaryScale> _salaryScales = new();
    public virtual IReadOnlyCollection<SalaryScale> SalaryScales => _salaryScales.AsReadOnly();

    protected JobGrade()
    {
    }

    public JobGrade(int gradeNumber, string gradeCode, string gradeName, string? description = null, string? legacyId = null)
    {
        if (gradeNumber < 1 || gradeNumber > 15)
            throw new DomainException("Bậc ngạch chức danh (JobGrade) phải từ bậc 1 đến bậc 15.", "ERR_JOB_GRADE_NUMBER_INVALID");

        if (string.IsNullOrWhiteSpace(gradeCode))
            throw new DomainException("Mã bậc ngạch không được để trống.", "ERR_JOB_GRADE_CODE_EMPTY");

        if (string.IsNullOrWhiteSpace(gradeName))
            throw new DomainException("Tên bậc ngạch không được để trống.", "ERR_JOB_GRADE_NAME_EMPTY");

        GradeNumber = gradeNumber;
        GradeCode = gradeCode.Trim().ToUpperInvariant();
        GradeName = gradeName.Trim();
        Description = description?.Trim();
        LegacyId = legacyId?.Trim();
        IsActive = true;
    }
}
