namespace VinamilkHRM.Application.Common.Security;

/// <summary>
/// 10 Vai trò chuẩn doanh nghiệp theo phân cấp quản trị của Tập đoàn Vinamilk.
/// </summary>
public static class Roles
{
    public const string SystemAdmin = "SystemAdmin";
    public const string Executive = "Executive"; // Ban Tổng Giám Đốc (CEO, Phó TGĐ)
    public const string HRDirector = "HRDirector"; // Giám đốc Nhân sự Tập đoàn
    public const string HRManager = "HRManager"; // Trưởng phòng HR / HR Nhà máy / Trang trại
    public const string HRSpecialist = "HRSpecialist"; // Chuyên viên C&B / Tuyển dụng
    public const string ChiefAccountant = "ChiefAccountant"; // Kế toán trưởng
    public const string PayrollAccountant = "PayrollAccountant"; // Kế toán tiền lương
    public const string UnitManager = "UnitManager"; // Giám đốc Nhà máy, Giám đốc Trang trại, Trưởng phòng
    public const string TeamLeader = "TeamLeader"; // Tổ trưởng, Trưởng ca sản xuất
    public const string Employee = "Employee"; // Nhân viên, Công nhân trực tiếp

    public static readonly IReadOnlyList<string> All = new[]
    {
        SystemAdmin,
        Executive,
        HRDirector,
        HRManager,
        HRSpecialist,
        ChiefAccountant,
        PayrollAccountant,
        UnitManager,
        TeamLeader,
        Employee
    };
}
