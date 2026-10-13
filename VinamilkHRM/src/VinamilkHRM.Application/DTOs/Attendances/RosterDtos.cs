using System;
using System.Collections.Generic;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.DTOs.Attendances;

public record RosterEntryDto(
    int Id,
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    DateOnly WorkDate,
    int? ShiftDefinitionId,
    string? ShiftCode,
    string? ShiftName,
    bool IsOff,
    RosterSource Source,
    RosterStatus Status,
    string? Note
);

public record RosterGridItemDto(
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    string? JobTitle,
    Dictionary<string, string> DayShiftCodes // "2026-10-01" => "CA_1", "2026-10-07" => "OFF"
);

public record RosterGridResponse(
    int OrganizationalUnitId,
    string UnitName,
    int Year,
    int Month,
    int TotalDays,
    List<RosterGridItemDto> Employees
);

public record GenerateRosterRequest(
    int OrganizationalUnitId,
    int Year,
    int Month,
    int RotationPatternId
);

public record UpdateRosterEntryRequest(
    int EmployeeId,
    DateOnly WorkDate,
    int? ShiftDefinitionId,
    bool IsOff,
    string? Note
);

public record PublishRosterRequest(
    int OrganizationalUnitId,
    int Year,
    int Month
);

public record ShiftSwapRequestDto(
    int Id,
    int RequesterEmployeeId,
    string RequesterName,
    DateOnly RequesterDate,
    int TargetEmployeeId,
    string TargetName,
    DateOnly TargetDate,
    string Reason,
    ShiftSwapStatus Status,
    int? ApproverId,
    string? ApproverName,
    DateTimeOffset? ApprovedAt
);

public record CreateShiftSwapRequest(
    int RequesterEmployeeId,
    DateOnly RequesterDate,
    int TargetEmployeeId,
    DateOnly TargetDate,
    string Reason
);
