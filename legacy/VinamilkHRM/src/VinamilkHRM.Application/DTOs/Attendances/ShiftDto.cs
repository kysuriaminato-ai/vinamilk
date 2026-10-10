using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.DTOs.Attendances;

public record ShiftDto(
    int Id,
    string ShiftCode,
    string ShiftName,
    ShiftType ShiftType,
    TimeSpan StartTime,
    TimeSpan EndTime,
    int BreakMinutes,
    decimal MultiplierRate,
    int? OrganizationalUnitId,
    string? UnitName,
    string ModelCategoryDescription);
