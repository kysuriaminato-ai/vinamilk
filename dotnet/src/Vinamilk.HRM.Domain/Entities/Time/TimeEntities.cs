using Vinamilk.HRM.Domain.Common;
using Vinamilk.HRM.Domain.Entities.Core;

namespace Vinamilk.HRM.Domain.Entities.Time;

public class WorkShift : BaseAuditableEntity
{
    public string ShiftCode { get; set; } = string.Empty;
    public string ShiftName { get; set; } = string.Empty;
    /// <summary>Loại ca: Fixed | Flexible | Overnight | Split | Hourly</summary>
    public string ShiftType { get; set; } = "Fixed";
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    /// <summary>Giờ lõi (core time) bắt buộc có mặt cho ca Linh hoạt (Flexible).</summary>
    public TimeOnly? CoreStartTime { get; set; }
    public TimeOnly? CoreEndTime { get; set; }
    /// <summary>Khung giờ thứ 2 cho ca gãy (Split).</summary>
    public TimeOnly? SplitStartTime2 { get; set; }
    public TimeOnly? SplitEndTime2 { get; set; }
    public TimeOnly? BreakStartTime { get; set; }
    public TimeOnly? BreakEndTime { get; set; }
    public bool IsBreakPaid { get; set; } = false;
    public bool IsOvernight { get; set; } = false;
    public decimal StandardHours { get; set; } = 8.0m;
    public decimal WorkdayValue { get; set; } = 1.0m;
    public decimal NightShiftAllowanceRate { get; set; } = 0m;
    public int GraceLateMinutes { get; set; } = 5;
    /// <summary>Cửa sổ chấm công hợp lệ trước giờ bắt đầu và sau giờ kết thúc (phút).</summary>
    public int MaxEarlyCheckInMinutes { get; set; } = 120;
    public int MaxLateCheckOutMinutes { get; set; } = 180;

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
    public string? ConflictWarning { get; set; }

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
    public bool IsCompensatoryDay { get; set; } = false;
    public DateOnly? OriginalHolidayDate { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public int? PolicyGroupId { get; set; }
    public string? LegalReference { get; set; }
    public bool NeedsHrVerification { get; set; } = true;

    public PolicyGroup? PolicyGroup { get; set; }
}

public class AttendanceLog : BaseLongAuditableEntity
{
    public int EmployeeId { get; set; }
    public DateTime PunchTime { get; set; }
    /// <summary>CheckIn | CheckOut | BreakOut | BreakIn</summary>
    public string PunchType { get; set; } = "CheckIn";
    /// <summary>Thống nhất mọi nguồn: BiometricDevice | Web | MobileGps | WebQr | ExcelImport | ManualHr</summary>
    public string SourceType { get; set; } = "BiometricDevice";
    public string? DeviceId { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? LocationAddress { get; set; }
    public string? IpAddress { get; set; }
    public string? PhotoUrl { get; set; }
    public int? EnteredByUserId { get; set; }
    public string? EnteredByUsername { get; set; }
    public string? ManualReason { get; set; }
    public string? GeofenceLocationCode { get; set; }
    /// <summary>Inside | OutsidePendingApproval | OutsideRejected | NotApplicable</summary>
    public string? GeofenceStatus { get; set; }
    public double? DistanceMeters { get; set; }
    public string? ClientDeviceFingerprint { get; set; }
    public bool IsManualOverride { get; set; } = false;
    /// <summary>Khóa chống trùng log (EmployeeId/DeviceCode + PunchTime + PunchType)</summary>
    public string? ExternalLogKey { get; set; }

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
    public decimal StandardHours { get; set; } = 8.0m;
    public decimal WorkingHours { get; set; } = 8.0m;
    public decimal WorkdayCount { get; set; } = 1.0m;
    public decimal PaidLeaveDays { get; set; } = 0m;
    public decimal UnpaidLeaveDays { get; set; } = 0m;
    public int LateMinutes { get; set; } = 0;
    public int EarlyLeaveMinutes { get; set; } = 0;
    public decimal OvertimeHours150 { get; set; } = 0m;
    public decimal OvertimeHours200 { get; set; } = 0m;
    public decimal OvertimeHours300 { get; set; } = 0m;
    public decimal OvertimeHours150Day { get; set; } = 0m;
    public decimal OvertimeHours150Night { get; set; } = 0m;
    public decimal OvertimeHours200Day { get; set; } = 0m;
    public decimal OvertimeHours200Night { get; set; } = 0m;
    public decimal OvertimeHours300Day { get; set; } = 0m;
    public decimal OvertimeHours300Night { get; set; } = 0m;
    public decimal NightShiftHours { get; set; } = 0m;
    public bool IsMissedPunch { get; set; } = false;
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
    public decimal TotalPaidPersonalLeaveDays { get; set; } = 0m;
    public decimal TotalUnpaidPersonalLeaveDays { get; set; } = 0m;
    public decimal TotalUnexcusedAbsentDays { get; set; } = 0m;
    public int TotalLateCount { get; set; }
    public int TotalLateMinutes { get; set; }
    public int TotalEarlyLeaveCount { get; set; } = 0;
    public int TotalEarlyLeaveMinutes { get; set; } = 0;
    public int TotalMissedPunchCount { get; set; } = 0;
    public decimal TotalOtHours150 { get; set; }
    public decimal TotalOtHours200 { get; set; }
    public decimal TotalOtHours300 { get; set; }
    public decimal TotalOtHours150Day { get; set; } = 0m;
    public decimal TotalOtHours150Night { get; set; } = 0m;
    public decimal TotalOtHours200Day { get; set; } = 0m;
    public decimal TotalOtHours200Night { get; set; } = 0m;
    public decimal TotalOtHours300Day { get; set; } = 0m;
    public decimal TotalOtHours300Night { get; set; } = 0m;
    public decimal TotalNightHours { get; set; }
    public decimal TotalPayableWorkdays { get; set; }
    /// <summary>Quy trình chốt công: Open | UnitSubmitted | ManagerConfirmed | Locked</summary>
    public string ClosingStatus { get; set; } = "Open";
    public string? UnitSubmittedBy { get; set; }
    public DateTime? UnitSubmittedAt { get; set; }
    public string? ManagerConfirmedBy { get; set; }
    public DateTime? ManagerConfirmedAt { get; set; }
    public int ReviewWindowDays { get; set; } = 3;
    public DateTime? ReviewDeadlineAt { get; set; }
    public bool IsLocked { get; set; } = false;
    public DateTime? LockedAt { get; set; }
    public string? LockedBy { get; set; }
    public int MissedPunchOverQuotaCount { get; set; } = 0;
    public decimal MissedPunchPenaltyAmount { get; set; } = 0m;

    public Employee Employee { get; set; } = null!;
}

public class OvertimeRequest : BaseAuditableEntity
{
    public int EmployeeId { get; set; }
    public string? OvertimeBatchCode { get; set; }
    public DateOnly OvertimeDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public decimal RequestedHours { get; set; }
    public decimal ApprovedHours { get; set; }
    public decimal ActualWorkedOtHours { get; set; } = 0m;
    public decimal ReconciledOtHours { get; set; } = 0m;
    public string OvertimeRateType { get; set; } = "OT150";
    /// <summary>Paid (trả tiền OT) | CompensatoryLeave (quy đổi nghỉ bù)</summary>
    public string CompensationMode { get; set; } = "Paid";
    public decimal CompensatoryHoursCredited { get; set; } = 0m;
    public string? LimitWarningMessage { get; set; }
    public string? Reason { get; set; }
    public int? ApprovedByEmployeeId { get; set; }
    public int? WorkflowInstanceId { get; set; }
    public string Status { get; set; } = "Pending";
    public string? CancelReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelledByUsername { get; set; }

    public Employee Employee { get; set; } = null!;
}

public class LeaveType : BaseAuditableEntity
{
    public string LeaveTypeCode { get; set; } = string.Empty;
    public string LeaveTypeName { get; set; } = string.Empty;
    public bool IsPaidByCompany { get; set; } = true;
    public bool IsPaidBySocialInsurance { get; set; } = false;
    public bool CountsAgainstAnnualLeave { get; set; } = false;
    public bool AllowHalfDay { get; set; } = true;
    public bool AllowHourly { get; set; } = false;
    public bool RequiresAttachment { get; set; } = false;
    public decimal? MaxDaysPerRequest { get; set; }
    public decimal MaxDaysPerYear { get; set; } = 12m;
    public bool AllowCarryForward { get; set; } = true;
    public string AttendanceSymbol { get; set; } = "P";
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public string? LegalReference { get; set; }
    public bool NeedsHrVerification { get; set; } = true;
    public string? Description { get; set; }

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
    public decimal UsedCarriedForwardDays { get; set; } = 0m;
    public decimal ExpiredCarryForwardDays { get; set; } = 0m;
    public decimal UsedDays { get; set; } = 0m;
    public decimal PendingDays { get; set; } = 0m;
    public decimal AdvanceUsedDays { get; set; } = 0m;
    public decimal RemainingDays { get; set; } = 12m;
    public decimal CompensatoryHoursBalance { get; set; } = 0m;
    public DateOnly? CarryForwardExpiryDate { get; set; }
    public decimal TerminationPayoutDays { get; set; } = 0m;
    public decimal TerminationPayoutAmount { get; set; } = 0m;
    public bool IsTerminationSettled { get; set; } = false;
    public DateTime? TerminationSettledAt { get; set; }
    public int? TerminationPayrollPeriodId { get; set; }

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
    /// <summary>Days | HalfDay | Hours</summary>
    public string LeaveDurationMode { get; set; } = "Days";
    public decimal? RequestedHours { get; set; }
    public decimal TotalDays { get; set; }
    public string? Reason { get; set; }
    public int? HandoverToEmployeeId { get; set; }
    public string? AttachmentUrl { get; set; }
    public int? ApprovedByEmployeeId { get; set; }
    public int? WorkflowInstanceId { get; set; }
    public string Status { get; set; } = "Pending";
    public string? ApproverComment { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? CancelReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelledByUsername { get; set; }

    public Employee Employee { get; set; } = null!;
    public LeaveType LeaveType { get; set; } = null!;
}

/// <summary>
/// Tham số pháp lý về giờ làm việc, nghỉ ngơi, làm đêm và làm thêm giờ (không ghi cứng trong code).
/// </summary>
public class AttendanceStatutoryParameter : BaseAuditableEntity
{
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string LegalReference { get; set; } = string.Empty;
    public bool NeedsHrVerification { get; set; } = true;

    public TimeOnly NightWorkStartTime { get; set; } = new(22, 0);
    public TimeOnly NightWorkEndTime { get; set; } = new(6, 0);
    public decimal MinInterShiftRestHours { get; set; } = 12m;
    public decimal MaxNormalDailyHours { get; set; } = 8m;
    public decimal MaxNormalWeeklyHours { get; set; } = 48m;
    public decimal MaxDailyOvertimeHours { get; set; } = 4m;
    public decimal MaxTotalDailyWorkingAndOtHours { get; set; } = 12m;
    public decimal MaxMonthlyOvertimeHours { get; set; } = 40m;
    public decimal MaxYearlyOvertimeHours { get; set; } = 200m;
    public decimal MaxSpecialIndustryYearlyOvertimeHours { get; set; } = 300m;
    public decimal StandardAnnualLeaveDays { get; set; } = 12m;
    public int SeniorityBonusYearsInterval { get; set; } = 5;
    public decimal SeniorityBonusDaysPerInterval { get; set; } = 1m;
    public decimal WeekdayOtMultiplier { get; set; } = 1.5m;
    public decimal WeeklyDayOffOtMultiplier { get; set; } = 2.0m;
    public decimal HolidayOtMultiplier { get; set; } = 3.0m;
    public decimal NightShiftBonusRate { get; set; } = 0.30m;
    public decimal NightOtExtraBonusRate { get; set; } = 0.20m;
    public string? Notes { get; set; }
}

/// <summary>
/// Nhóm chính sách chấm công nội bộ (ưu tiên gán: Nhân viên > Chức danh > Đơn vị > Mặc định công ty).
/// </summary>
public class PolicyGroup : BaseAuditableEntity
{
    public string PolicyCode { get; set; } = string.Empty;
    public string PolicyName { get; set; } = string.Empty;
    public bool IsCompanyDefault { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }

    public ICollection<AttendancePolicy> AttendancePolicies { get; set; } = new List<AttendancePolicy>();
    public ICollection<Vinamilk.HRM.Domain.Entities.Payroll.PayrollPolicy> PayrollPolicies { get; set; } = new List<Vinamilk.HRM.Domain.Entities.Payroll.PayrollPolicy>();
    public ICollection<PolicyGroupAssignment> Assignments { get; set; } = new List<PolicyGroupAssignment>();
}

public class PolicyGroupAssignment : BaseAuditableEntity
{
    public int PolicyGroupId { get; set; }
    public int? EmployeeId { get; set; }
    public int? PositionId { get; set; }
    public int? OrganizationUnitId { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;

    public PolicyGroup PolicyGroup { get; set; } = null!;
    public Employee? Employee { get; set; }
    public Position? Position { get; set; }
    public OrganizationUnit? OrganizationUnit { get; set; }
}

/// <summary>
/// Quy tắc chấm công nội bộ thuộc PolicyGroup.
/// </summary>
public class AttendancePolicy : BaseAuditableEntity
{
    public int PolicyGroupId { get; set; }
    public int GraceLateMinutes { get; set; } = 5;
    public int GraceEarlyMinutes { get; set; } = 5;
    /// <summary>Bước làm tròn phút: 0 | 5 | 10 | 15</summary>
    public int RoundingIntervalMinutes { get; set; } = 5;
    /// <summary>Kiểu làm tròn: None | Ceiling | Floor | Nearest</summary>
    public string RoundingMode { get; set; } = "Nearest";
    /// <summary>Ngưỡng đi muộn tính nửa công (VD > 60 phút -> 0.5 công).</summary>
    public int HalfDayLateThresholdMinutes { get; set; } = 60;
    /// <summary>Ngưỡng đi muộn mất công cả ngày (VD > 240 phút -> 0 công).</summary>
    public int ZeroDayLateThresholdMinutes { get; set; } = 240;
    public int MaxMissedPunchExplanationsPerMonth { get; set; } = 3;
    /// <summary>Xử lý khi vượt quota giải trình quên chấm công: None | DeductHalfDay | DeductFullDay | FixedFine</summary>
    public string MissedPunchOverQuotaAction { get; set; } = "FixedFine";
    /// <summary>Tiền phạt mỗi lần vượt quota (decimal(18,0) VNĐ).</summary>
    public decimal MissedPunchPenaltyAmount { get; set; } = 100000m;
    public bool RequireApprovedOvertimeRequest { get; set; } = true;
    /// <summary>Ngày làm việc trong tuần (Mon,Tue,Wed,Thu,Fri hoặc Rotating...).</summary>
    public string WeeklyWorkdaysMask { get; set; } = "Mon,Tue,Wed,Thu,Fri";
    public decimal? MaxWeeklyWorkingHoursOverride { get; set; }
    /// <summary>Xử lý nhiều lần chấm trong ngày: FirstLast | PairedSegments</summary>
    public string PunchPairingMode { get; set; } = "FirstLast";
    /// <summary>Số công mặc định nếu thiếu vào/ra khi chưa có giải trình được duyệt (0 hoặc 0.5).</summary>
    public decimal MissedPunchCreditWorkdays { get; set; } = 0m;
    /// <summary>Số ngày X cho phép nhân viên xem & gửi giải trình trước khi khóa bảng công.</summary>
    public int EmployeeReviewWindowDays { get; set; } = 3;
    public decimal AnnualLeaveCarryForwardCapDays { get; set; } = 5m;
    public int CarryForwardExpiryMonth { get; set; } = 3;
    public int CarryForwardExpiryDay { get; set; } = 31;
    public bool AllowAdvanceAnnualLeave { get; set; } = false;
    public decimal MaxAdvanceAnnualLeaveDays { get; set; } = 3m;
    public int MidMonthHireCutoffDay { get; set; } = 15;
    /// <summary>MinOfApprovedAndActual | UseActualUpToRegistered | UseApprovedFixed</summary>
    public string OtReconciliationMode { get; set; } = "MinOfApprovedAndActual";
    public int OvertimeWarningThresholdPercent { get; set; } = 85;
    public bool BlockOvertimeWhenExceedingLimit { get; set; } = true;
    public bool AllowCompensatoryLeaveForOt { get; set; } = true;
    public bool UseSpecialIndustry300OtLimit { get; set; } = false;

    public PolicyGroup PolicyGroup { get; set; } = null!;
}

/// <summary>
/// Mẫu xoay ca tự động cho nhiều kíp (VD: 3 ca 4 kíp, sinh lịch N tuần).
/// </summary>
public class ShiftRotationPattern : BaseAuditableEntity
{
    public string PatternCode { get; set; } = string.Empty;
    public string PatternName { get; set; } = string.Empty;
    public int CycleDays { get; set; } = 8;
    public int CrewCount { get; set; } = 4;
    /// <summary>Chuỗi mã ca theo từng ngày trong chu kỳ (phân cách dấu phẩy, "OFF" là ngày nghỉ), VD: CA1,CA1,CA2,CA2,CA3,CA3,OFF,OFF</summary>
    public string ShiftSequenceCsv { get; set; } = "CA1,CA1,CA2,CA2,CA3,CA3,OFF,OFF";
    public int CrewOffsetDays { get; set; } = 2;
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

/// <summary>
/// Đơn đề nghị đổi ca giữa 2 nhân viên.
/// </summary>
public class ShiftSwapRequest : BaseAuditableEntity
{
    public int RequesterEmployeeId { get; set; }
    public int TargetEmployeeId { get; set; }
    public DateOnly RequesterWorkDate { get; set; }
    public DateOnly TargetWorkDate { get; set; }
    public int? RequesterOriginalShiftId { get; set; }
    public int? TargetOriginalShiftId { get; set; }
    public string Reason { get; set; } = string.Empty;
    /// <summary>Trạng thái: Pending | Approved | Rejected | Cancelled</summary>
    public string Status { get; set; } = "Pending";
    public int? WorkflowInstanceId { get; set; }
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApproverComment { get; set; }
    public string? CancelReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelledByUsername { get; set; }

    public Employee RequesterEmployee { get; set; } = null!;
    public Employee TargetEmployee { get; set; } = null!;
}

/// <summary>
/// Địa điểm chấm công Geofence (Nhà máy / Văn phòng / Trang trại) và chính sách ngoài vùng / QR động.
/// </summary>
public class GeofenceLocation : BaseAuditableEntity
{
    public string LocationCode { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public double AllowedRadiusMeters { get; set; } = 300d;
    /// <summary>Xử lý khi chấm ngoài vùng bán kính: Reject | PendingApproval</summary>
    public string OutsideGeofenceAction { get; set; } = "PendingApproval";
    /// <summary>Chu kỳ đổi mã QR động (30–60 giây).</summary>
    public int QrRotationSeconds { get; set; } = 30;
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Thiết bị máy chấm công (đọc file xuất CSV/TXT/DAT hoặc kết nối SDK/API sau này).
/// </summary>
public class AttendanceDevice : BaseAuditableEntity
{
    public string DeviceCode { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    /// <summary>FileExport | ZKTecoSdk | HikvisionApi</summary>
    public string ConnectorType { get; set; } = "FileExport";
    public string? LocationName { get; set; }
    public string? EndpointOrFolderPath { get; set; }
    public int SyncIntervalMinutes { get; set; } = 15;
    public bool IsActive { get; set; } = true;
    public DateTime? LastSyncAt { get; set; }
    public string? LastSyncSummary { get; set; }
}

/// <summary>
/// Bảng ánh xạ Mã chấm công trên máy ↔ Nhân viên.
/// </summary>
public class EmployeeDeviceMapping : BaseAuditableEntity
{
    public int EmployeeId { get; set; }
    public string? DeviceCode { get; set; }
    public string DeviceUserCode { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    public Employee Employee { get; set; } = null!;
}

/// <summary>
/// Hàng chờ các log từ máy chấm công chưa khớp mã nhân viên (không bị mất khi đồng bộ).
/// </summary>
public class UnmappedAttendanceLog : BaseLongAuditableEntity
{
    public string DeviceCode { get; set; } = string.Empty;
    public string DeviceUserCode { get; set; } = string.Empty;
    public DateTime PunchTime { get; set; }
    public string PunchType { get; set; } = "CheckIn";
    public string? RawPayload { get; set; }
    /// <summary>PendingMapping | Resolved | Ignored</summary>
    public string Status { get; set; } = "PendingMapping";
    public int? ResolvedEmployeeId { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedBy { get; set; }
}

/// <summary>
/// Gắn thiết bị chấm công Web/Mobile theo nguyên tắc 1 thiết bị / 1 tài khoản nhằm chặn chấm hộ.
/// </summary>
public class EmployeeDeviceBinding : BaseAuditableEntity
{
    public int EmployeeId { get; set; }
    public string DeviceFingerprint { get; set; } = string.Empty;
    public string? DeviceLabel { get; set; }
    public string? LastIpAddress { get; set; }
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public Employee Employee { get; set; } = null!;
}

/// <summary>
/// Danh mục ký hiệu công chuẩn hóa & cấu hình được (X, P, L, Ô, TS, CT, R, Ro, KL, ½...).
/// </summary>
public class AttendanceSymbolDefinition : BaseAuditableEntity
{
    /// <summary>Mã loại nghiệp vụ chuẩn cho engine: Work | HalfWork | AnnualLeave | Holiday | Sick | Maternity | BusinessTrip | PersonalPaid | PersonalUnpaid | UnexcusedAbsent | WeeklyOff | MissedPunch | PendingGeofence</summary>
    public string SymbolTypeCode { get; set; } = string.Empty;
    public string SymbolCode { get; set; } = string.Empty;
    public string SymbolName { get; set; } = string.Empty;
    public string Category { get; set; } = "Work";
    public decimal WorkdayMultiplier { get; set; } = 1.0m;
    public bool IsPaidByCompany { get; set; } = true;
    public bool IsPaidBySocialInsurance { get; set; } = false;
    public string ColorHex { get; set; } = "#198754";
    public string CssClass { get; set; } = "bg-success-subtle text-success fw-semibold";
    public int DisplayOrder { get; set; } = 1;
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Điều chỉnh công kỳ sau (truy lĩnh/truy thu) sau khi bảng công tháng đã bị khóa.
/// </summary>
public class PostLockAttendanceAdjustment : BaseLongAuditableEntity
{
    public int EmployeeId { get; set; }
    public int TargetMonth { get; set; }
    public int TargetYear { get; set; }
    public DateOnly AdjustmentDate { get; set; }
    public int AppliedPayrollMonth { get; set; }
    public int AppliedPayrollYear { get; set; }
    public string AdjustmentType { get; set; } = "WorkdayDelta";
    public decimal WorkdayDelta { get; set; } = 0m;
    public decimal OtHours150Delta { get; set; } = 0m;
    public decimal OtHours200Delta { get; set; } = 0m;
    public decimal OtHours300Delta { get; set; } = 0m;
    public decimal NightHoursDelta { get; set; } = 0m;
    public string Reason { get; set; } = string.Empty;
    /// <summary>Pending | Approved | Rejected</summary>
    public string Status { get; set; } = "Pending";
    public string? ApprovedByUsername { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApproverComment { get; set; }

    public Employee Employee { get; set; } = null!;
}

/// <summary>
/// Lô tính lại bảng công chạy nền có theo dõi tiến độ theo nhân viên / đơn vị / khoảng ngày.
/// </summary>
public class AttendanceRecalculationBatch : BaseAuditableEntity
{
    public string BatchCode { get; set; } = string.Empty;
    public int Month { get; set; }
    public int Year { get; set; }
    public int? OrganizationUnitId { get; set; }
    public int? EmployeeId { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    /// <summary>Queued | Running | Completed | Failed</summary>
    public string Status { get; set; } = "Queued";
    public int TotalEmployees { get; set; }
    public int ProcessedEmployees { get; set; }
    public int ProgressPercent { get; set; }
    public string RequestedByUsername { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Đơn chấm công tổng quát đi qua Workflow: Đi muộn/Về sớm (LateEarly), Công tác (BusinessTrip), Làm việc từ xa (RemoteWork), Giải trình công (AttendanceExplanation).
/// </summary>
public class EmployeeTimeRequest : BaseAuditableEntity
{
    public int EmployeeId { get; set; }
    /// <summary>LateEarly | BusinessTrip | RemoteWork | AttendanceExplanation</summary>
    public string RequestType { get; set; } = "LateEarly";
    /// <summary>LateArrival | EarlyDeparture | Domestic | Overseas | Home | ClientSite | MissedCheckIn | MissedCheckOut | DeviceError</summary>
    public string? SubType { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public TimeOnly? PlannedTime { get; set; }
    public TimeOnly? ProposedEndTime { get; set; }
    public int? RequestedMinutes { get; set; }
    public string? LocationOrDestination { get; set; }
    public string? AttachmentUrl { get; set; }
    public string Reason { get; set; } = string.Empty;
    /// <summary>Pending | Approved | Rejected | Cancelled</summary>
    public string Status { get; set; } = "Pending";
    public int? WorkflowInstanceId { get; set; }
    public int? ApprovedByEmployeeId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApproverComment { get; set; }
    public string? CancelReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelledByUsername { get; set; }

    public Employee Employee { get; set; } = null!;
}

/// <summary>
/// Đơn đề nghị hủy/thu hồi đơn đã được duyệt trước đó (đi qua Workflow phê duyệt thu hồi).
/// </summary>
public class RequestCancellationRequest : BaseAuditableEntity
{
    public int RequesterEmployeeId { get; set; }
    /// <summary>LeaveRequest | OvertimeRequest | EmployeeTimeRequest | ShiftSwapRequest</summary>
    public string OriginalEntityType { get; set; } = "LeaveRequest";
    public int OriginalEntityId { get; set; }
    public string Reason { get; set; } = string.Empty;
    /// <summary>Pending | Approved | Rejected</summary>
    public string Status { get; set; } = "Pending";
    public int? WorkflowInstanceId { get; set; }
    public int? ProcessedByUserId { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ApproverComment { get; set; }
    public string? RestoredBalanceSummary { get; set; }

    public Employee RequesterEmployee { get; set; } = null!;
}

/// <summary>
/// Quyết toán thanh toán tiền phép năm chưa nghỉ khi nghỉ việc (Điều 113 khoản 3 BLLĐ 2019), đẩy sang kỳ lương.
/// </summary>
public class TerminationLeaveSettlement : BaseAuditableEntity
{
    public int EmployeeId { get; set; }
    public int SettlementYear { get; set; }
    public DateOnly TerminationDate { get; set; }
    public decimal ProRatedEntitlementDays { get; set; }
    public decimal SeniorityExtraDays { get; set; }
    public decimal ValidCarryForwardDays { get; set; }
    public decimal UsedDays { get; set; }
    public decimal UnusedPayableDays { get; set; }
    public decimal BaseSalaryUsed { get; set; }
    public decimal StandardWorkdaysUsed { get; set; }
    public decimal PayoutAmount { get; set; }
    public int TargetPayrollMonth { get; set; }
    public int TargetPayrollYear { get; set; }
    public int? PayrollPeriodId { get; set; }
    /// <summary>Calculated | PushedToPayroll | Paid</summary>
    public string Status { get; set; } = "PushedToPayroll";
    public string? Notes { get; set; }

    public Employee Employee { get; set; } = null!;
}



