using VinamilkHRM.Application.DTOs.Attendances;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.Interfaces;

public interface IShiftAssignmentService
{
    /// <summary>
    /// Lấy danh sách ca làm việc chuẩn hóa cho 3 mô hình: Văn phòng, Nhà máy Mega, Trang trại bò sữa.
    /// </summary>
    Task<IReadOnlyList<ShiftDto>> GetConfiguredShiftsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gán ca làm việc cho nhân viên theo đặc thù khối hoạt động Vinamilk.
    /// </summary>
    Task<ShiftDto> AssignShiftToEmployeeAsync(int employeeId, int shiftId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Quét mã / Chấm công vào ca (Check-in).
    /// </summary>
    Task<AttendanceRecordDto> ClockInAsync(int employeeId, int shiftId, TimeSpan checkInTime, DateTime attendanceDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Quét mã / Chấm công ra ca (Check-out) và tự động tính công chuẩn cùng giờ làm thêm OT.
    /// </summary>
    Task<AttendanceRecordDto> ClockOutAsync(int employeeId, TimeSpan checkOutTime, DateTime attendanceDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xử lý đơn nghỉ việc đột xuất:
    /// - Nghỉ có phép: Tự động trừ Quỹ phép năm (AnnualLeaveBalance).
    /// - Nghỉ ốm đau BHXH: Tự động gắn cờ hưởng chế độ BHXH 75% và sinh biểu mẫu C70a-HD.
    /// - Nghỉ không phép: Trừ công, trừ điểm chuyên cần, nếu nghỉ liên tục >= 5 ngày tự động tạo cảnh báo kỷ luật lao động.
    /// </summary>
    Task<string> ProcessUnplannedLeaveAsync(UnplannedLeaveDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách cảnh báo kỷ luật lao động do tự ý nghỉ việc liên tục >= 5 ngày.
    /// </summary>
    Task<IReadOnlyList<DisciplinaryWarningDto>> GetDisciplinaryWarningsAsync(bool onlyUnresolved = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xử lý giải trình / Quyết định kỷ luật lao động.
    /// </summary>
    Task ResolveDisciplinaryWarningAsync(int warningId, string resolutionNotes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy dữ liệu bảng chấm công đa mô hình phục vụ hiển thị trực quan Dashboard.
    /// </summary>
    Task<IReadOnlyList<AttendanceRecordDto>> GetAttendanceDashboardDataAsync(DateTime targetDate, int? unitId = null, CancellationToken cancellationToken = default);
}
