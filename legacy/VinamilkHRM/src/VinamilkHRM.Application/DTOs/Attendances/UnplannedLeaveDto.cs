using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.DTOs.Attendances;

public record UnplannedLeaveDto(
    int EmployeeId,
    LeaveType LeaveType,
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalDays,
    string Reason,
    string? HospitalCertificateNumber,
    bool IsEmergencyNotification);

public record DisciplinaryWarningDto(
    int Id,
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string UnitName,
    DateTime WarningDate,
    int ConsecutiveAbsentDays,
    string ViolationTitle,
    string Details,
    bool IsResolved,
    string? ResolutionNotes);
