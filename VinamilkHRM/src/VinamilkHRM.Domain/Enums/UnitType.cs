namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Loại đơn vị tổ chức trong cơ cấu tập đoàn Vinamilk (Trụ sở, Khối, Phòng ban, Nhà máy, Xưởng, Trang trại, Tổ nông trường, Chi nhánh, Cửa hàng).
/// </summary>
public enum UnitType
{
    Headquarters = 1,   // Trụ sở chính (Vinamilk Tower)
    Factory = 2,        // Nhà máy chế biến sữa (Mega Factory, Thống Nhất, Tiên Sơn,...)
    DairyFarm = 3,      // Trang trại bò sữa công nghệ cao (Tây Ninh, Lâm Đồng,...)
    RetailStore = 4,    // Hệ thống Cửa hàng Giấc Mơ Sữa Việt
    Division = 5,       // Khối chức năng (Khối Sản xuất, Khối Tài chính, Khối Chuỗi Cung ứng,...)
    Department = 6,     // Phòng ban trực thuộc (Phòng Kế toán, Ban Pháp chế,...)
    ProductionLine = 7, // Phân xưởng / Dây chuyền sản xuất / Tổ sản xuất
    FarmTeam = 8,       // Đội chăn nuôi / Tổ thú y / Đội đồng cỏ trang trại
    Branch = 9          // Chi nhánh khu vực (Hà Nội, Cần Thơ, Đà Nẵng,...)
}
