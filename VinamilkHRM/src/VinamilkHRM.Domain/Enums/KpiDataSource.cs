namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Nguồn thu thập dữ liệu chỉ tiêu KPI.
/// </summary>
public enum KpiDataSource
{
    Manual = 1,      // Nhập tay / Báo cáo tự nộp
    Attendance = 2,  // Tự động từ hệ thống chấm công (Phiên 3)
    Production = 3,  // Sản lượng sản xuất chế biến / Trang trại
    Quality = 4,     // Tỷ lệ lỗi / Chất lượng KCS
    Sales = 5        // Doanh số bán hàng / Doanh thu chuỗi cửa hàng
}
