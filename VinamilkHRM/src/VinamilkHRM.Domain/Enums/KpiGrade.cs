namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Xếp loại kết quả đánh giá hiệu suất (KPI Grade).
/// </summary>
public enum KpiGrade
{
    A = 1,       // Xuất sắc (>= 110 điểm, hệ số 1.2)
    B = 2,       // Tốt (95 - < 110 điểm, hệ số 1.0)
    C = 3,       // Đạt (80 - < 95 điểm, hệ số 0.8)
    D = 4,       // Cần cải thiện (60 - < 80 điểm, hệ số 0.5)
    E = 5,       // Không đạt (< 60 điểm, hệ số 0)
    Unrated = 6  // Không xếp loại (< 50% công chuẩn, P3 tính theo quy chế)
}
