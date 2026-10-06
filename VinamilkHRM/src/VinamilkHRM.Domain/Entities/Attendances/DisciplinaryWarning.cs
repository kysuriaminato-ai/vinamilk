using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;

namespace VinamilkHRM.Domain.Entities.Attendances;

/// <summary>
/// Đại diện cho Cảnh báo vi phạm Kỷ luật lao động tự động khi nhân viên nghỉ việc không phép liên tục >= 5 ngày
/// (Căn cứ quy định xử lý kỷ luật sa thải tại Điều 125 Bộ luật Lao động Việt Nam).
/// </summary>
public class DisciplinaryWarning : BaseAuditableEntity
{
    public int EmployeeId { get; private set; }
    public virtual Employee? Employee { get; private set; }

    public DateTime WarningDate { get; private set; } = DateTime.UtcNow;
    public int ConsecutiveAbsentDays { get; private set; }
    public string ViolationTitle { get; private set; } = string.Empty;
    public string Details { get; private set; } = string.Empty;
    public bool IsResolved { get; private set; }
    public string? ResolutionNotes { get; private set; }
    public DateTime? ResolvedAt { get; private set; }

    protected DisciplinaryWarning()
    {
    }

    public DisciplinaryWarning(
        int employeeId,
        int consecutiveAbsentDays,
        string violationTitle,
        string details)
    {
        if (employeeId <= 0)
            throw new DomainException("Mã nhân viên vi phạm không hợp lệ.", "ERR_DISC_EMP_INVALID");

        if (consecutiveAbsentDays < 5)
            throw new DomainException("Cảnh báo kỷ luật tự ý nghỉ việc chỉ kích hoạt khi nghỉ liên tục từ 5 ngày làm việc trở lên.", "ERR_DISC_DAYS_INVALID");

        EmployeeId = employeeId;
        ConsecutiveAbsentDays = consecutiveAbsentDays;
        ViolationTitle = violationTitle?.Trim() ?? "Cảnh báo Vi phạm Kỷ luật Lao động (Nghỉ việc không phép liên tục)";
        Details = details?.Trim() ?? string.Empty;
        WarningDate = DateTime.UtcNow;
        IsResolved = false;
    }

    public void Resolve(string resolutionNotes)
    {
        if (string.IsNullOrWhiteSpace(resolutionNotes))
            throw new DomainException("Phải ghi rõ kết luận xử lý hoặc giải trình của hội đồng kỷ luật.", "ERR_DISC_RES_EMPTY");

        IsResolved = true;
        ResolutionNotes = resolutionNotes.Trim();
        ResolvedAt = DateTime.UtcNow;
    }
}
