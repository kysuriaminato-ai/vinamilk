using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Assets;

/// <summary>
/// Đại diện cho Trang thiết bị, tài sản do Vinamilk cấp phát cho người lao động.
/// </summary>
public class CompanyAsset : BaseAuditableEntity
{
    public string AssetCode { get; private set; } = string.Empty;
    public string AssetName { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty; // Thiết bị CNTT, Thiết bị an toàn, Phương tiện di chuyển,...
    public string SerialNumber { get; private set; } = string.Empty;
    
    public int? AssignedToEmployeeId { get; private set; }
    public virtual Employee? AssignedToEmployee { get; private set; }

    public DateTime? AssignedDate { get; private set; }
    public decimal OriginalValue { get; private set; }
    public AssetStatus Status { get; private set; } = AssetStatus.Available;
    public string? Notes { get; private set; }

    protected CompanyAsset()
    {
    }

    public CompanyAsset(
        string assetCode,
        string assetName,
        string category,
        string serialNumber,
        decimal originalValue,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(assetCode))
            throw new DomainException("Mã tài sản Vinamilk không được để trống.", "ERR_ASSET_CODE_EMPTY");

        if (string.IsNullOrWhiteSpace(assetName))
            throw new DomainException("Tên tài sản không được để trống.", "ERR_ASSET_NAME_EMPTY");

        if (originalValue < 0)
            throw new DomainException("Nguyên giá tài sản không thể âm.", "ERR_ASSET_VALUE_NEGATIVE");

        AssetCode = assetCode.Trim().ToUpperInvariant();
        AssetName = assetName.Trim();
        Category = category?.Trim() ?? "Thiết bị chung";
        SerialNumber = serialNumber?.Trim() ?? string.Empty;
        OriginalValue = originalValue;
        Status = AssetStatus.Available;
        Notes = notes?.Trim();
    }

    /// <summary>
    /// Bàn giao tài sản cho nhân viên sử dụng phục vụ công việc.
    /// </summary>
    public void AssignToEmployee(int employeeId)
    {
        if (employeeId <= 0)
            throw new DomainException("Mã nhân viên nhận bàn giao không hợp lệ.", "ERR_ASSET_EMP_INVALID");

        if (Status == AssetStatus.InUse && AssignedToEmployeeId == employeeId)
            throw new DomainException("Tài sản này hiện đã được giao cho nhân viên này.", "ERR_ASSET_ALREADY_ASSIGNED");

        if (Status == AssetStatus.UnderMaintenance || Status == AssetStatus.Disposed || Status == AssetStatus.Lost)
            throw new DomainException($"Không thể bàn giao tài sản đang ở trạng thái {Status}.", "ERR_ASSET_UNAVAILABLE");

        AssignedToEmployeeId = employeeId;
        AssignedDate = DateTime.UtcNow;
        Status = AssetStatus.InUse;
    }

    /// <summary>
    /// Thu hồi tài sản khi nhân sự điều chuyển hoặc thôi việc.
    /// </summary>
    public void ReturnAsset(string returnConditionNotes)
    {
        if (Status != AssetStatus.InUse)
            throw new DomainException("Chỉ tài sản đang trong sử dụng mới có thể thực hiện thu hồi.", "ERR_ASSET_NOT_IN_USE");

        AssignedToEmployeeId = null;
        AssignedDate = null;
        Status = AssetStatus.Available;
        Notes = $"[Thu hồi {DateTime.UtcNow:dd/MM/yyyy}]: {returnConditionNotes.Trim()}";
    }

    public void MarkUnderMaintenance(string issueDescription)
    {
        Status = AssetStatus.UnderMaintenance;
        Notes = $"[Bảo dưỡng {DateTime.UtcNow:dd/MM/yyyy}]: {issueDescription?.Trim()}";
    }

    public void MarkDisposed(string reason)
    {
        Status = AssetStatus.Disposed;
        AssignedToEmployeeId = null;
        Notes = $"[Thanh lý {DateTime.UtcNow:dd/MM/yyyy}]: {reason?.Trim()}";
    }
}
