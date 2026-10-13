using VinamilkHRM.Domain.Common;

namespace VinamilkHRM.Domain.Entities.Employees;

/// <summary>
/// Hồ sơ tài liệu đính kèm của nhân viên (file lưu trên đĩa/storage ngoài DB, DB giữ metadata, đường dẫn và mã băm SHA-256).
/// </summary>
public class EmployeeAttachment : BaseAuditableEntity
{
    public int EmployeeId { get; private set; }
    public virtual Employee? Employee { get; private set; }

    public string FileName { get; private set; } = string.Empty;
    public string DocumentType { get; private set; } = string.Empty; // CCCD, HopDong, BangCap, GiayKhamSucKhoe...
    public string FilePath { get; private set; } = string.Empty;     // Đường dẫn tương đối lưu trữ file
    public long FileSizeBytes { get; private set; }
    public string ChecksumSha256 { get; private set; } = string.Empty; // Mã kiểm tra toàn vẹn tệp tin
    public DateTime UploadedAt { get; private set; } = DateTime.UtcNow;

    protected EmployeeAttachment()
    {
    }

    public EmployeeAttachment(
        int employeeId,
        string fileName,
        string documentType,
        string filePath,
        long fileSizeBytes,
        string checksumSha256)
    {
        if (employeeId <= 0)
            throw new DomainException("Mã nhân viên tài liệu không hợp lệ.", "ERR_ATTACH_EMP_INVALID");

        if (string.IsNullOrWhiteSpace(fileName))
            throw new DomainException("Tên tệp tin không được để trống.", "ERR_ATTACH_NAME_EMPTY");

        if (string.IsNullOrWhiteSpace(filePath))
            throw new DomainException("Đường dẫn tệp tin không được để trống.", "ERR_ATTACH_PATH_EMPTY");

        if (string.IsNullOrWhiteSpace(checksumSha256) || checksumSha256.Length != 64)
            throw new DomainException("Mã băm SHA-256 tệp tin không hợp lệ.", "ERR_ATTACH_CHECKSUM_INVALID");

        EmployeeId = employeeId;
        FileName = fileName.Trim();
        DocumentType = documentType?.Trim() ?? "General";
        FilePath = filePath.Trim();
        FileSizeBytes = fileSizeBytes >= 0 ? fileSizeBytes : 0;
        ChecksumSha256 = checksumSha256.Trim().ToLowerInvariant();
        UploadedAt = DateTime.UtcNow;
    }
}
