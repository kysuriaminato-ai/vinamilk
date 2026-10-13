namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Vùng lương tối thiểu theo quy định của Chính phủ Việt Nam (Vùng I, II, III, IV).
/// Căn cứ Nghị định về lương tối thiểu vùng theo địa giới hành chính.
/// </summary>
public enum MinimumWageRegion
{
    RegionI = 1,   // Vùng I (Hà Nội, TP.HCM, Bình Dương, Đồng Nai, Hải Phòng,...)
    RegionII = 2,  // Vùng II (Các thành phố trực thuộc tỉnh, Cần Thơ, Đà Nẵng,...)
    RegionIII = 3, // Vùng III (Các huyện, thị xã vùng nông thôn/bán sơn địa,...)
    RegionIV = 4   // Vùng IV (Các địa bàn còn lại, vùng sâu vùng xa)
}
