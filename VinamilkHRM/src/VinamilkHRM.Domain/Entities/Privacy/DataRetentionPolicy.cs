using VinamilkHRM.Domain.Common;

namespace VinamilkHRM.Domain.Entities.Privacy;

/// <summary>
/// Chính sách thời hạn lưu trữ theo loại dữ liệu (Data Retention Policy).
/// </summary>
public class DataRetentionPolicy : BaseAuditableEntity
{
    public string DataType { get; private set; } = string.Empty; // "UnsuccessfulCandidateResume", "TerminatedEmployeeProfile", "AuditLogs"
    public int RetentionMonths { get; private set; } = 12;
    public string ActionOnExpiry { get; private set; } = "Anonymize"; // "Anonymize", "HardDelete"
    public string Description { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    protected DataRetentionPolicy() { }

    public DataRetentionPolicy(
        string dataType,
        int retentionMonths,
        string actionOnExpiry,
        string description)
    {
        if (string.IsNullOrWhiteSpace(dataType))
            throw new DomainException("Loại dữ liệu lưu trữ không được để trống.", "ERR_RETENTION_TYPE_EMPTY");

        if (retentionMonths <= 0)
            throw new DomainException("Thời hạn lưu trữ phải lớn hơn 0 tháng.", "ERR_RETENTION_MONTHS_INVALID");

        DataType = dataType.Trim();
        RetentionMonths = retentionMonths;
        ActionOnExpiry = actionOnExpiry.Trim();
        Description = description?.Trim() ?? string.Empty;
        IsActive = true;
    }
}