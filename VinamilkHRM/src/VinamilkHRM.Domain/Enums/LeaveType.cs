namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Chế độ loại hình nghỉ phép của cán bộ nhân viên Vinamilk.
/// </summary>
public enum LeaveType
{
    AnnualLeave = 1,        // Nghỉ phép năm có hưởng lương
    SickLeave = 2,          // Nghỉ ốm đau / điều trị theo chế độ BHXH
    MaternityLeave = 3,     // Nghỉ thai sản / chế độ nuôi con nhỏ
    CompassionateLeave = 4, // Nghỉ việc riêng có lương (kết hôn, tang gia)
    UnpaidLeave = 5         // Nghỉ không hưởng lương
}
