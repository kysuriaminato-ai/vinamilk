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
    public string? LegalReference { get; set; }
    public bool NeedsHrVerification { get; set; } = true;
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
    public int MissedPunchOverQuotaCount { get; set; } = 0;
    public decimal MissedPunchPenaltyAmount { get; set; } = 0m;

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
    public decimal MaxMonthlyOvertimeHours { get; set; } = 40m;
    public decimal MaxYearlyOvertimeHours { get; set; } = 200m;
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
    /// <summary>Trạng thái: Pending | Approved | Rejected</summary>
    public string Status { get; set; } = "Pending";
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApproverComment { get; set; }

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



