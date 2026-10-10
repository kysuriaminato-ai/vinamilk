namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Trạng thái sử dụng tài sản doanh nghiệp cấp phát cho nhân viên.
/// </summary>
public enum AssetStatus
{
    Available = 1,        // Sẵn sàng trong kho cấp phát
    InUse = 2,            // Đang được bàn giao cho nhân viên sử dụng
    UnderMaintenance = 3, // Đang bảo dưỡng / sửa chữa kỹ thuật
    Disposed = 4,         // Đã thanh lý thu hồi vốn
    Lost = 5              // Thất lạc / hư hỏng chờ bồi thường
}
