using System;
using System.Collections.Generic;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.DTOs.Attendances;

public record TimeEventDto(
    int Id,
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    DateTimeOffset Timestamp,
    string DeviceId,
    PunchMethod Method,
    double? LocationLat,
    double? LocationLong,
    bool IsProcessed
);

public record TimeEventInputDto(
    int EmployeeId,
    DateTimeOffset Timestamp,
    string DeviceId,
    PunchMethod Method = PunchMethod.Fingerprint,
    double? LocationLat = null,
    double? LocationLong = null,
    string? IpAddress = null
);

public record BatchTimeEventsRequest(
    string DeviceId,
    List<TimeEventInputDto> Events
);

public record BatchTimeEventsResponse(
    int TotalReceived,
    int InsertedCount,
    int SkippedDuplicatesCount
);

public record MobilePunchRequest(
    int EmployeeId,
    DateTimeOffset Timestamp,
    double LocationLat,
    double LocationLong,
    int TargetUnitId
);
