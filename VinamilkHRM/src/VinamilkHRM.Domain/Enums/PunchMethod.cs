namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Phương thức ghi nhận lần quẹt chấm công (TimeEvent).
/// </summary>
public enum PunchMethod
{
    Fingerprint = 1, // Vân tay
    Face = 2,        // Nhận diện khuôn mặt AI
    Card = 3,        // Thẻ từ RFID / NFC
    MobileGps = 4,   // Ứng dụng di động kiểm tra tọa độ Geofence
    Manual = 5       // Nhập bù hoặc điều chỉnh thủ công
}
