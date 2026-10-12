using VinamilkHRM.Domain.Common;

namespace VinamilkHRM.Domain.Entities.Auditing;

/// <summary>
/// Đại diện cho Nhật ký kiểm toán hệ thống (Audit Log), lưu lại mọi thay đổi dữ liệu nhạy cảm.
/// Kế thừa trực tiếp từ BaseEntity.
/// </summary>
public class AuditLog : BaseEntity
{
    public string TableName { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty; // INSERT, UPDATE, DELETE, SALARY_CHANGE, CONTRACT_TERMINATED
    public string EntityId { get; private set; } = string.Empty;
    public string? OldValues { get; private set; }
    public string? NewValues { get; private set; }
    public string PerformedBy { get; private set; } = "SYSTEM";
    public DateTime PerformedAt { get; private set; } = DateTime.UtcNow;
    public string? IpAddress { get; private set; }

    protected AuditLog()
    {
    }

    public AuditLog(
        string tableName,
        string action,
        string entityId,
        string? oldValues,
        string? newValues,
        string performedBy,
        string? ipAddress = null)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new DomainException("Tên bảng kiểm toán không được để trống.", "ERR_AUDIT_TABLE_EMPTY");

        if (string.IsNullOrWhiteSpace(action))
            throw new DomainException("Hành động kiểm toán không được để trống.", "ERR_AUDIT_ACTION_EMPTY");

        TableName = tableName.Trim();
        Action = action.Trim().ToUpperInvariant();
        EntityId = entityId?.Trim() ?? string.Empty;
        OldValues = oldValues;
        NewValues = newValues;
        PerformedBy = string.IsNullOrWhiteSpace(performedBy) ? "SYSTEM" : performedBy.Trim();
        PerformedAt = DateTime.UtcNow;
        IpAddress = ipAddress?.Trim();
    }
}
