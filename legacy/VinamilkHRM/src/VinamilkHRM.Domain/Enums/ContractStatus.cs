namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Trạng thái hiệu lực của hợp đồng lao động.
/// </summary>
public enum ContractStatus
{
    Draft = 1,          // Bản nháp / Chờ ký kết
    Active = 2,         // Đang có hiệu lực
    Expired = 3,        // Hết hạn hợp đồng
    Terminated = 4      // Đã chấm dứt / Thanh lý
}
