namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Mô hình ca kíp và lịch làm việc đặc thù của đơn vị tổ chức Vinamilk.
/// </summary>
public enum WorkPattern
{
    OfficeWeek = 1,          // Khối Văn phòng/Trụ sở: Thứ 2 - Thứ 6 (8h - 17h)
    ThreeShiftRotation = 2,  // Nhà máy sản xuất: 3 ca 4 kíp (Sáng, Chiều, Đêm luân phiên)
    FarmSplitShift = 3,      // Trang trại bò sữa: Ca gãy chăm sóc & vắt sữa (Sáng sớm & Chiều muộn)
    RetailRotation = 4       // Cửa hàng Giấc Mơ Sữa Việt: Xoay ca bán lẻ (Ca sáng & Ca tối)
}
