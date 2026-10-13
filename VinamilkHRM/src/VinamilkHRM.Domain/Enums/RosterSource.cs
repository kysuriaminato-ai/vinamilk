namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Nguồn gốc tạo ra lịch phân ca của nhân viên.
/// </summary>
public enum RosterSource
{
    Pattern = 1, // Sinh tự động theo chu kỳ kíp (RotationPattern)
    Manual = 2,  // Xếp tay hoặc điều chỉnh bởi TeamLeader
    Swap = 3,    // Đổi ca được duyệt giữa 2 nhân viên (ShiftSwap)
    Cover = 4    // Ca làm bù / thế ca cho người khác
}
