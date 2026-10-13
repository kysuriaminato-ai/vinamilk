using System;
using System.Collections.Generic;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.DTOs.Attendances;

public record AttendanceSegmentDto(
    int SegmentIndex,
    DateTimeOffset? CheckInTime,
    DateTimeOffset? CheckOutTime,
    int LateMinutes,
    int EarlyLeaveMinutes,
    decimal WorkHours,
    decimal NightHours
);

public record DailyAttendanceDto(
    int Id,
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    DateOnly AttendanceDate,
    string? ShiftCode,
    string? ShiftName,
    decimal TotalWorkHours,
    int LateMinutes,
    int EarlyLeaveMinutes,
    decimal NightHours,
    decimal RegularOtHours,
    decimal RestDayOtHours,
    decimal HolidayOtHours,
    decimal NightOtHours,
    decimal UnapprovedOtHours,
    string AttendanceCode,
    decimal WorkDayCredit,
    bool IsComplete,
    AttendanceStatus Status,
    bool HasWarning,
    string? WarningReason,
    string? Remarks,
    List<AttendanceSegmentDto> Segments
);

public record MonthlyTimesheetSummaryDto(
    int Id,
    string Period,
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    int OrganizationalUnitId,
    string UnitName,
    decimal StandardWorkDays,
    decimal ActualWorkDays,
    decimal PaidLeaveDays,
    decimal SickLeaveDays,
    decimal MaternityLeaveDays,
    decimal HolidayDays,
    decimal CompensatoryLeaveDays,
    decimal BusinessTripDays,
    decimal TrainingDays,
    decimal UnpaidLeaveDays,
    decimal UnexcusedAbsenceDays,
    int ConsecutiveUnexcusedDays,
    decimal TotalWorkHours,
    decimal NightHours,
    decimal TotalOtHours,
    decimal RegularOtHours,
    decimal RestDayOtHours,
    decimal HolidayOtHours,
    decimal NightOtHours,
    int LateTimes,
    int EarlyLeaveTimes
);

public record TodayDashboardDto(
    DateOnly Today,
    int TotalHeadcount,
    int PresentCount,
    int LateCount,
    int AbsentCount,
    int OnLeaveCount,
    int OvertimeCount,
    decimal AttendanceRatePercent
);

public record OvertimeRequestDto(
    int Id,
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    DateOnly WorkDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    decimal PlannedHours,
    string Reason,
    OvertimeStatus Status,
    int? TeamLeaderId,
    string? TeamLeaderName,
    DateTimeOffset? TeamLeaderApprovedAt,
    int? UnitManagerId,
    string? UnitManagerName,
    DateTimeOffset? UnitManagerApprovedAt,
    string? RejectionReason
);

public record CreateOvertimeRequest(
    int EmployeeId,
    DateOnly WorkDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    decimal PlannedHours,
    string Reason
);

public record AttendanceExplanationDto(
    int Id,
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    DateOnly AttendanceDate,
    MissingPunchType MissingPunchType,
    TimeOnly? ActualCheckIn,
    TimeOnly? ActualCheckOut,
    string Reason,
    ExplanationStatus Status,
    int? ApproverId,
    string? ApproverName,
    DateTimeOffset? ApprovedAt,
    string? RejectionReason
);

public record CreateExplanationRequest(
    int EmployeeId,
    DateOnly AttendanceDate,
    MissingPunchType MissingPunchType,
    TimeOnly? ActualCheckIn,
    TimeOnly? ActualCheckOut,
    string Reason
);

public record LockTimesheetPeriodRequest(
    string Period,
    int OrganizationalUnitId
);

public record CreateAdjustmentRequest(
    string OriginalPeriod,
    string TargetPeriod,
    int EmployeeId,
    decimal AdjustedWorkDays,
    decimal AdjustedOtHours,
    decimal AdjustedNightHours,
    string Reason
);
