using Vinamilk.HRM.Domain.Common;
using Vinamilk.HRM.Domain.Entities.Core;

namespace Vinamilk.HRM.Domain.Entities.Time;

public class WorkShift : BaseAuditableEntity
{
    public string ShiftCode { get; set; } = string.Empty;
    public string ShiftName { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public TimeOnly? BreakStartTime { get; set; }
    public TimeOnly? BreakEndTime { get; set; }
    public bool IsOvernight { get; set; } = false;
    public decimal StandardHours { get; set; } = 8.0m;
    public decimal WorkdayValue { get; set; } = 1.0m;
    public decimal NightShiftAllowanceRate { get; set; } = 0m;
    public int GraceLateMinutes { get; set; } = 5;

    public ICollection<ShiftSchedule> ShiftSchedules { get; set; } = new List<ShiftSchedule>();
    public ICollection<DailyAttendance> DailyAttendances { get; set; } = new List<DailyAttendance>();
}

public class ShiftSchedule : BaseLongAuditableEntity
{
    public int EmployeeId { get; set; }
    public int WorkShiftId { get; set; }
    public DateOnly WorkDate { get; set; }
    public bool IsDayOff { get; set; } = false;
    public string? Notes { get; set; }

    public Employee Employee { get; set; } = null!;
    public WorkShift WorkShift { get; set; } = null!;
}

public class Holiday : BaseAuditableEntity
{
    public DateOnly HolidayDate { get; set; }
    public string HolidayName { get; set; } = string.Empty;
    public int Year { get; set; }
    public bool IsPaidHoliday { get; set; } = true;
    public decimal OvertimeMultiplier { get; set; } = 3.0m;
}

public class AttendanceLog : BaseLongAuditableEntity
{
    public int EmployeeId { get; set; }
    public DateTime PunchTime { get; set; }
    public string PunchType { get; set; } = "CheckIn";
    public string SourceType { get; set; } = "BiometricDevice";
    public string? DeviceId { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? LocationAddress { get; set; }

    public Employee Employee { get; set; } = null!;
}

public class DailyAttendance : BaseLongAuditableEntity
{
    public int EmployeeId { get; set; }
    public DateOnly AttendanceDate { get; set; }
    public int? WorkShiftId { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string AttendanceSymbol { get; set; } = "X";
    public decimal WorkingHours { get; set; } = 8.0m;
    public decimal WorkdayCount { get; set; } = 1.0m;
    public int LateMinutes { get; set; } = 0;
    public int EarlyLeaveMinutes { get; set; } = 0;
    public decimal OvertimeHours150 { get; set; } = 0m;
    public decimal OvertimeHours200 { get; set; } = 0m;
    public decimal OvertimeHours300 { get; set; } = 0m;
    public decimal NightShiftHours { get; set; } = 0m;
    public bool IsManualEdited { get; set; } = false;
    public string? EditReason { get; set; }

    public Employee Employee { get; set; } = null!;
    public WorkShift? WorkShift { get; set; }
}

public class MonthlyAttendanceSummary : BaseAuditableEntity
{
    public int EmployeeId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal StandardWorkdays { get; set; } = 26m;
    public decimal ActualWorkdays { get; set; }
    public decimal PaidLeaveDays { get; set; }
    public decimal HolidayDays { get; set; }
    public decimal SocialInsuranceLeaveDays { get; set; }
    public decimal UnpaidLeaveDays { get; set; }
    public int TotalLateCount { get; set; }
    public int TotalLateMinutes { get; set; }
    public decimal TotalOtHours150 { get; set; }
    public decimal TotalOtHours200 { get; set; }
    public decimal TotalOtHours300 { get; set; }
    public decimal TotalNightHours { get; set; }
    public decimal TotalPayableWorkdays { get; set; }
    public bool IsLocked { get; set; } = false;
    public DateTime? LockedAt { get; set; }
    public string? LockedBy { get; set; }

    public Employee Employee { get; set; } = null!;
}

public class OvertimeRequest : BaseAuditableEntity
{
    public int EmployeeId { get; set; }
    public DateOnly OvertimeDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public decimal RequestedHours { get; set; }
    public decimal ApprovedHours { get; set; }
    public string OvertimeRateType { get; set; } = "OT150";
    public string? Reason { get; set; }
    public int? ApprovedByEmployeeId { get; set; }
    public int? WorkflowInstanceId { get; set; }
    public string Status { get; set; } = "Pending";

    public Employee Employee { get; set; } = null!;
}

public class LeaveType : BaseAuditableEntity
{
    public string LeaveTypeCode { get; set; } = string.Empty;
    public string LeaveTypeName { get; set; } = string.Empty;
    public bool IsPaidByCompany { get; set; } = true;
    public bool IsPaidBySocialInsurance { get; set; } = false;
    public decimal MaxDaysPerYear { get; set; } = 12m;
    public bool AllowCarryForward { get; set; } = true;
    public string AttendanceSymbol { get; set; } = "P";

    public ICollection<LeaveBalance> LeaveBalances { get; set; } = new List<LeaveBalance>();
    public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
}

public class LeaveBalance : BaseAuditableEntity
{
    public int EmployeeId { get; set; }
    public int LeaveTypeId { get; set; }
    public int Year { get; set; }
    public decimal StandardEntitlement { get; set; } = 12m;
    public decimal SeniorityExtraDays { get; set; } = 0m;
    public decimal CarriedForwardDays { get; set; } = 0m;
    public decimal UsedDays { get; set; } = 0m;
    public decimal PendingDays { get; set; } = 0m;
    public decimal RemainingDays { get; set; } = 12m;
    public DateOnly? CarryForwardExpiryDate { get; set; }

    public Employee Employee { get; set; } = null!;
    public LeaveType LeaveType { get; set; } = null!;
}

public class LeaveRequest : BaseAuditableEntity
{
    public int EmployeeId { get; set; }
    public int LeaveTypeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string StartSession { get; set; } = "FullDay";
    public string EndSession { get; set; } = "FullDay";
    public decimal TotalDays { get; set; }
    public string? Reason { get; set; }
    public int? HandoverToEmployeeId { get; set; }
    public string? AttachmentUrl { get; set; }
    public int? ApprovedByEmployeeId { get; set; }
    public int? WorkflowInstanceId { get; set; }
    public string Status { get; set; } = "Pending";
    public string? ApproverComment { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public Employee Employee { get; set; } = null!;
    public LeaveType LeaveType { get; set; } = null!;
}

