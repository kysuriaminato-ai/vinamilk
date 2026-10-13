namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Loại hình điều chỉnh / tăng lương (Total Rewards Adjustment Type).
/// </summary>
public enum SalaryAdjustmentType
{
    Merit = 1,              // Tăng lương định kỳ theo hiệu suất công việc & khung lương
    StepIncrease = 2,       // Nâng bậc lương định kỳ theo thâm niên
    Promotion = 3,          // Thăng chức, bổ nhiệm chức danh mới
    MarketAdjustment = 4,   // Điều chỉnh trượt giá / khảo sát lương thị trường theo JobFamily
    MinimumWage = 5,        // Điều chỉnh lương sàn theo Nghị định lương tối thiểu vùng mới
    Special = 6             // Đặc cách ngoại lệ (giữ nhân tài, thành tích đột xuất, nhiệm vụ trọng yếu)
}