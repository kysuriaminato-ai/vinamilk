namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Danh mục lý do thôi việc chuẩn hóa của tập đoàn Vinamilk.
/// </summary>
public enum ExitReasonCategory
{
    Compensation = 1,      // Thu nhập, chính sách đãi ngộ, phúc lợi
    Management = 2,        // Phong cách quản lý, cấp trên trực tiếp
    WorkEnvironment = 3,   // Môi trường làm việc, văn hóa, áp lực công việc
    Personal = 4,          // Lý do cá nhân, gia đình, định cư, sức khỏe
    Dismissal = 5,         // Kỷ luật sa thải, chấm dứt do vi phạm
    ContractExpiry = 6,    // Hết hạn hợp đồng lao động, không gia hạn
    Retirement = 7         // Nghỉ hưu theo chế độ
}