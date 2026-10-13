namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Mức độ rủi ro nghỉ việc dự báo theo mô hình luật cộng điểm (Attrition Risk Level).
/// </summary>
public enum AttritionRiskLevel
{
    Low = 1,        // Dưới 30 điểm: Rủi ro thấp, ổn định
    Medium = 2,     // 30 - 59 điểm: Cần quan tâm theo dõi
    High = 3,       // 60 - 79 điểm: Rủi ro cao, cần hành động giữ chân
    Critical = 4    // >= 80 điểm: Báo động đỏ, nguy cơ rời đi tức thì
}