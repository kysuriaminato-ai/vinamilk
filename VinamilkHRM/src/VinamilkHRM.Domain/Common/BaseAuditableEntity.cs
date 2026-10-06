namespace VinamilkHRM.Domain.Common;

/// <summary>
/// Lớp cơ sở trừu tượng hỗ trợ theo dõi dấu vết kiểm toán (Audit Trail) và xóa mềm (Soft Delete).
/// Kế thừa từ BaseEntity theo mô hình phân cấp đối tượng Clean Architecture.
/// </summary>
public abstract class BaseAuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public string CreatedBy { get; protected set; } = "SYSTEM";
    public DateTime? UpdatedAt { get; protected set; }
    public string? UpdatedBy { get; protected set; }
    public bool IsDeleted { get; protected set; }

    public void SetAuditCreated(string createdBy, DateTime? createdAt = null)
    {
        if (string.IsNullOrWhiteSpace(createdBy))
            throw new DomainException("Người tạo bản ghi (CreatedBy) không được để trống.", "ERR_AUDIT_CREATEDBY_EMPTY");

        CreatedBy = createdBy.Trim();
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }

    public void SetAuditUpdated(string updatedBy, DateTime? updatedAt = null)
    {
        if (string.IsNullOrWhiteSpace(updatedBy))
            throw new DomainException("Người cập nhật bản ghi (UpdatedBy) không được để trống.", "ERR_AUDIT_UPDATEDBY_EMPTY");

        UpdatedBy = updatedBy.Trim();
        UpdatedAt = updatedAt ?? DateTime.UtcNow;
    }

    public void MarkAsDeleted(string deletedBy, DateTime? deletedAt = null)
    {
        if (string.IsNullOrWhiteSpace(deletedBy))
            throw new DomainException("Người thực hiện xóa bản ghi không được để trống.", "ERR_AUDIT_DELETEDBY_EMPTY");

        IsDeleted = true;
        UpdatedBy = deletedBy.Trim();
        UpdatedAt = deletedAt ?? DateTime.UtcNow;
    }

    public void Restore(string restoredBy)
    {
        if (string.IsNullOrWhiteSpace(restoredBy))
            throw new DomainException("Người khôi phục bản ghi không được để trống.", "ERR_AUDIT_RESTOREDBY_EMPTY");

        IsDeleted = false;
        UpdatedBy = restoredBy.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}
