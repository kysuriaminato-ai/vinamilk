using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.DTOs.Attendances;

public record AttendanceRecordDto(
    int Id,
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string UnitName,
    EmployeeType EmployeeType,
    int ShiftId,
    string ShiftName,
    DateTime AttendanceDate,
    TimeSpan? CheckInTime,
    TimeSpan? CheckOutTime,
    decimal WorkHours,
    decimal OvertimeHours,
    AttendanceStatus Status,
    string? Note,
    decimal DiligenceScore);
