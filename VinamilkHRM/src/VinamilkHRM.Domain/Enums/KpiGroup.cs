namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Bốn nhóm chỉ tiêu KPI cốt lõi trong cơ cấu đánh giá Vinamilk.
/// </summary>
public enum KpiGroup
{
    UnitResult = 1,        // Kết quả đơn vị (Doanh số, sản lượng, lợi nhuận)
    IndividualWork = 2,    // Công việc cá nhân / Chuyên môn
    PeopleManagement = 3,  // Quản lý con người / Phát triển đội ngũ
    Compliance = 4         // Tuân thủ quy trình, an toàn, kỷ luật
}
