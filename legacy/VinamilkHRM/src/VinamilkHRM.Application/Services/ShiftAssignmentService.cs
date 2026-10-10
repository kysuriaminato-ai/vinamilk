using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Application.DTOs.Attendances;
using VinamilkHRM.Application.Interfaces;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Attendances;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Entities.LeaveRequests;
using VinamilkHRM.Domain.Entities.Shifts;
using VinamilkHRM.Domain.Enums;
using VinamilkHRM.Domain.Interfaces.Repositories;

namespace VinamilkHRM.Application.Services;

/// <summary>
/// Hiện thực Dịch vụ Phân bổ Ca kíp & Chấm công Đa mô hình tại Tập đoàn Vinamilk:
/// 1. Khối Hành chính Văn phòng (08h00 - 17h30).
/// 2. Khối Nhà máy Mega Factory (Ca 1: 06h-14h, Ca 2: 14h-22h, Ca 3: 22h-06h phụ cấp đêm 30%).
/// 3. Khối Trang trại Bò sữa sinh thái (Ca sớm 04h30-08h30 và Ca chiều 13h30-17h30).
/// Xử lý nghỉ đột xuất (trừ quỹ phép năm, ốm đau BHXH 75% mẫu C70a-HD, nghỉ không phép trừ điểm chuyên cần & cảnh báo vi phạm Kỷ luật lao động khi >= 5 ngày).
/// </summary>
public class ShiftAssignmentService : IShiftAssignmentService
{
    private readonly IApplicationDbContext _context;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ShiftAssignmentService(
        IApplicationDbContext context,
        IEmployeeRepository employeeRepository,
        IAttendanceRepository attendanceRepository,
        IUnitOfWork unitOfWork)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _employeeRepository = employeeRepository ?? throw new ArgumentNullException(nameof(employeeRepository));
        _attendanceRepository = attendanceRepository ?? throw new ArgumentNullException(nameof(attendanceRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<IReadOnlyList<ShiftDto>> GetConfiguredShiftsAsync(CancellationToken cancellationToken = default)
    {
        var shifts = await _context.Shifts
            .Include(s => s.OrganizationalUnit)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Nếu chưa có ca trong DB, tự động khởi tạo 3 mô hình chuẩn Vinamilk
        if (shifts.Count == 0)
        {
            await SeedDefaultMultiShiftModelsAsync(cancellationToken);
            shifts = await _context.Shifts
                .Include(s => s.OrganizationalUnit)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        return shifts.Select(s => new ShiftDto(
            s.Id,
            s.ShiftCode,
            s.ShiftName,
            s.ShiftType,
            s.StartTime,
            s.EndTime,
            s.BreakMinutes,
            s.MultiplierRate,
            s.OrganizationalUnitId,
            s.OrganizationalUnit?.UnitName,
            GetShiftCategoryName(s.ShiftType)
        )).ToList();
    }

    public async Task<ShiftDto> AssignShiftToEmployeeAsync(int employeeId, int shiftId, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy nhân viên Id={employeeId}.", "ERR_EMP_NOT_FOUND");

        var shift = await _context.Shifts.FindAsync(new object[] { shiftId }, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy ca làm việc Id={shiftId}.", "ERR_SHIFT_NOT_FOUND");

        // Kiểm tra tính tương thích giữa kiểu nhân sự và ca làm việc
        ValidateShiftCompatibility(employee.EmployeeType, shift.ShiftType);

        return new ShiftDto(
            shift.Id,
            shift.ShiftCode,
            shift.ShiftName,
            shift.ShiftType,
            shift.StartTime,
            shift.EndTime,
            shift.BreakMinutes,
            shift.MultiplierRate,
            shift.OrganizationalUnitId,
            shift.OrganizationalUnit?.UnitName,
            GetShiftCategoryName(shift.ShiftType));
    }

    public async Task<AttendanceRecordDto> ClockInAsync(int employeeId, int shiftId, TimeSpan checkInTime, DateTime attendanceDate, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetWithDetailsAsync(employeeId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy nhân viên Id={employeeId}.", "ERR_EMP_NOT_FOUND");

        var shift = await _context.Shifts.FindAsync(new object[] { shiftId }, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy ca làm việc Id={shiftId}.", "ERR_SHIFT_NOT_FOUND");

        DateTime dateOnly = attendanceDate.Date;
        var attendance = await _context.Attendances
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.AttendanceDate == dateOnly, cancellationToken);

        if (attendance == null)
        {
            attendance = new Attendance(employeeId, shiftId, dateOnly);
            await _context.Attendances.AddAsync(attendance, cancellationToken);
        }

        // Ghi nhận giờ Check-In
        attendance.RecordCheckIn(checkInTime, shift.StartTime, lateToleranceMinutes: 15);

        // Trừ điểm chuyên cần nếu đi muộn
        if (attendance.Status == AttendanceStatus.Late)
        {
            employee.DeductDiligenceScore(2.0m);
            _employeeRepository.Update(employee);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapAttendanceToDto(attendance, employee, shift);
    }

    public async Task<AttendanceRecordDto> ClockOutAsync(int employeeId, TimeSpan checkOutTime, DateTime attendanceDate, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetWithDetailsAsync(employeeId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy nhân viên Id={employeeId}.", "ERR_EMP_NOT_FOUND");

        DateTime dateOnly = attendanceDate.Date;
        var attendance = await _context.Attendances
            .Include(a => a.Shift)
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.AttendanceDate == dateOnly, cancellationToken)
            ?? throw new DomainException($"Chưa tìm thấy bản ghi Check-In ngày {dateOnly:dd/MM/yyyy} để thực hiện Check-Out.", "ERR_ATT_NO_RECORD");

        var shift = attendance.Shift ?? (await _context.Shifts.FindAsync(new object[] { attendance.ShiftId }, cancellationToken))
            ?? throw new DomainException("Không tìm thấy ca làm việc liên kết.", "ERR_SHIFT_NOT_FOUND");

        // Tính toán giờ làm chuẩn dựa trên loại hình ca
        decimal standardShiftHours = (shift.ShiftType == ShiftType.FarmEarlyCare || shift.ShiftType == ShiftType.FarmLateCare) ? 4.0m : 8.0m;

        attendance.RecordCheckOut(checkOutTime, shift.EndTime, standardShiftHours);

        // Nếu về sớm, trừ điểm chuyên cần
        if (attendance.Status == AttendanceStatus.EarlyDeparture || attendance.Status == AttendanceStatus.LateAndEarly)
        {
            employee.DeductDiligenceScore(2.0m);
            _employeeRepository.Update(employee);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapAttendanceToDto(attendance, employee, shift);
    }

    public async Task<string> ProcessUnplannedLeaveAsync(UnplannedLeaveDto dto, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetWithDetailsAsync(dto.EmployeeId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy nhân viên Id={dto.EmployeeId}.", "ERR_EMP_NOT_FOUND");

        DateTime start = dto.StartDate.Date;
        DateTime end = dto.EndDate.Date;

        var leaveRequest = new LeaveRequest(dto.EmployeeId, dto.LeaveType, start, end, dto.TotalDays, dto.Reason);

        string resultMessage;

        // Mô hình 1: Nghỉ phép năm có hưởng lương -> Tự động trừ Quỹ phép năm
        if (dto.LeaveType == LeaveType.AnnualLeave)
        {
            employee.DeductAnnualLeave(dto.TotalDays);
            leaveRequest.Approve(employee.ManagerId ?? employee.Id, "Hệ thống tự động phê duyệt và trừ Quỹ phép năm.");
            resultMessage = $"Đã chấp thuận nghỉ phép năm {dto.TotalDays} ngày. Quỹ phép năm còn lại của nhân viên: {employee.AnnualLeaveBalance} ngày.";
        }
        // Mô hình 2: Nghỉ ốm đau hưởng chế độ BHXH 75% -> Sinh biểu mẫu C70a-HD
        else if (dto.LeaveType == LeaveType.SickLeave)
        {
            leaveRequest.Approve(employee.ManagerId ?? employee.Id, "Đã tiếp nhận hồ sơ y tế, chuyển đề nghị thanh toán BHXH.");
            string certNo = string.IsNullOrWhiteSpace(dto.HospitalCertificateNumber) ? "GIAY-RA-VIEN-CHUA-CAP-SO" : dto.HospitalCertificateNumber;
            leaveRequest.GenerateSocialInsuranceForm(
                employee.FullName,
                employee.CitizenId,
                employee.OrganizationalUnit?.UnitName ?? "Tập đoàn Vinamilk",
                certNo);

            resultMessage = $"Đã phê duyệt chế độ ốm đau BHXH 75% ({dto.TotalDays} ngày) và tự động lập Mẫu C70a-HD gửi cơ quan BHXH.";
        }
        // Mô hình 3: Nghỉ việc không phép -> Trừ công, trừ điểm chuyên cần, kiểm tra kỷ luật liên tục >= 5 ngày
        else if (dto.LeaveType == LeaveType.UnpaidLeave && !dto.IsEmergencyNotification)
        {
            // Trừ điểm chuyên cần nặng
            employee.DeductDiligenceScore(dto.TotalDays * 10.0m);
            leaveRequest.Reject(employee.ManagerId ?? employee.Id, "Tự ý nghỉ việc không có lý do chính đáng hoặc không báo trước.");

            // Kiểm tra số ngày nghỉ không phép liên tục
            int consecutiveDays = await CountConsecutiveAbsentDaysAsync(dto.EmployeeId, start, cancellationToken) + (int)dto.TotalDays;

            if (consecutiveDays >= 5)
            {
                var warning = new DisciplinaryWarning(
                    dto.EmployeeId,
                    consecutiveDays,
                    "CẢNH BÁO VI PHẠM KỶ LUẬT LAO ĐỘNG (NGHỈ LIÊN TỤC >= 5 NGÀY)",
                    $"Cán bộ/Nhân sự {employee.FullName} ({employee.EmployeeCode}) đã tự ý nghỉ việc không phép liên tục {consecutiveDays} ngày làm việc từ ngày {start:dd/MM/yyyy}. Căn cứ Điều 125 Bộ luật Lao động số 45/2019/QH14, sự vụ được chuyển trực tiếp lên Hội đồng Kỷ luật Tập đoàn Vinamilk để xem xét xử lý theo quy chế.");

                await _context.DisciplinaryWarnings.AddAsync(warning, cancellationToken);
                resultMessage = $"[CẢNH BÁO KỶ LUẬT NGHIÊM TRỌNG]: Nhân sự nghỉ không phép liên tục {consecutiveDays} ngày. Đã tự động tạo hồ sơ vi phạm Kỷ luật lao động Điều 125 Bộ luật Lao động và trừ {dto.TotalDays * 10:0} điểm chuyên cần.";
            }
            else
            {
                resultMessage = $"Đã ghi nhận nghỉ không phép {dto.TotalDays} ngày. Trừ công và trừ {dto.TotalDays * 10:0} điểm chuyên cần. Tổng số ngày vắng mặt liên tục hiện tại: {consecutiveDays} ngày.";
            }
        }
        else
        {
            leaveRequest.Approve(employee.ManagerId ?? employee.Id, "Đã phê duyệt đơn nghỉ chế độ việc riêng.");
            resultMessage = $"Đã ghi nhận đơn nghỉ chế độ {dto.LeaveType} ({dto.TotalDays} ngày).";
        }

        // Cập nhật trạng thái chấm công cho các ngày nghỉ
        for (var day = start; day <= end; day = day.AddDays(1))
        {
            var existingAtt = await _context.Attendances.FirstOrDefaultAsync(a => a.EmployeeId == dto.EmployeeId && a.AttendanceDate == day, cancellationToken);
            if (existingAtt == null)
            {
                // Tìm ca mặc định
                var defaultShift = await _context.Shifts.FirstOrDefaultAsync(cancellationToken);
                existingAtt = new Attendance(dto.EmployeeId, defaultShift?.Id ?? 1, day);
                await _context.Attendances.AddAsync(existingAtt, cancellationToken);
            }

            if (dto.LeaveType == LeaveType.AnnualLeave || dto.LeaveType == LeaveType.SickLeave || dto.LeaveType == LeaveType.CompassionateLeave)
            {
                existingAtt.MarkAsLeave(dto.LeaveType);
            }
            else
            {
                existingAtt.ManualAdjustment(null, null, 0m, 0m, AttendanceStatus.AbsentUnexcused, "Nghỉ việc không phép");
            }
        }

        await _context.LeaveRequests.AddAsync(leaveRequest, cancellationToken);
        _employeeRepository.Update(employee);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return resultMessage;
    }

    public async Task<IReadOnlyList<DisciplinaryWarningDto>> GetDisciplinaryWarningsAsync(bool onlyUnresolved = true, CancellationToken cancellationToken = default)
    {
        var query = _context.DisciplinaryWarnings
            .Include(d => d.Employee)
            .ThenInclude(e => e!.OrganizationalUnit)
            .AsNoTracking();

        if (onlyUnresolved)
        {
            query = query.Where(d => !d.IsResolved);
        }

        var list = await query.OrderByDescending(d => d.WarningDate).ToListAsync(cancellationToken);

        return list.Select(d => new DisciplinaryWarningDto(
            d.Id,
            d.EmployeeId,
            d.Employee?.EmployeeCode ?? "N/A",
            d.Employee?.FullName ?? "N/A",
            d.Employee?.OrganizationalUnit?.UnitName ?? "N/A",
            d.WarningDate,
            d.ConsecutiveAbsentDays,
            d.ViolationTitle,
            d.Details,
            d.IsResolved,
            d.ResolutionNotes
        )).ToList();
    }

    public async Task ResolveDisciplinaryWarningAsync(int warningId, string resolutionNotes, CancellationToken cancellationToken = default)
    {
        var warning = await _context.DisciplinaryWarnings.FindAsync(new object[] { warningId }, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy cảnh báo kỷ luật Id={warningId}.", "ERR_DISC_NOT_FOUND");

        warning.Resolve(resolutionNotes);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AttendanceRecordDto>> GetAttendanceDashboardDataAsync(DateTime targetDate, int? unitId = null, CancellationToken cancellationToken = default)
    {
        DateTime date = targetDate.Date;

        var query = _context.Attendances
            .Include(a => a.Employee)
            .ThenInclude(e => e!.OrganizationalUnit)
            .Include(a => a.Shift)
            .Where(a => a.AttendanceDate == date)
            .AsNoTracking();

        if (unitId.HasValue)
        {
            query = query.Where(a => a.Employee != null && a.Employee.OrganizationalUnitId == unitId.Value);
        }

        var records = await query.ToListAsync(cancellationToken);

        return records.Select(a => MapAttendanceToDto(a, a.Employee!, a.Shift!)).ToList();
    }

    private async Task<int> CountConsecutiveAbsentDaysAsync(int employeeId, DateTime targetDate, CancellationToken cancellationToken)
    {
        int count = 0;
        DateTime checkDate = targetDate.AddDays(-1);

        while (checkDate >= targetDate.AddDays(-15))
        {
            // Bỏ qua Thứ Bảy và Chủ Nhật nếu khối văn phòng
            if (checkDate.DayOfWeek == DayOfWeek.Saturday || checkDate.DayOfWeek == DayOfWeek.Sunday)
            {
                checkDate = checkDate.AddDays(-1);
                continue;
            }

            var att = await _context.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.AttendanceDate == checkDate, cancellationToken);

            if (att != null && att.Status == AttendanceStatus.AbsentUnexcused)
            {
                count++;
                checkDate = checkDate.AddDays(-1);
            }
            else
            {
                break;
            }
        }

        return count;
    }

    private async Task SeedDefaultMultiShiftModelsAsync(CancellationToken cancellationToken)
    {
        var shifts = new List<Shift>
        {
            // Mô hình 1: Khối Hành chính Văn phòng (8h - 17h30)
            new Shift("SH-OFFICE", "Ca Hành chính Văn phòng Vinamilk Tower (08h00 - 17h30)", ShiftType.OfficeStandard, new TimeSpan(8, 0, 0), new TimeSpan(17, 30, 0), 90, 1.00m),

            // Mô hình 2: Khối Nhà máy sữa Mega Factory (3 ca)
            new Shift("SH-FACTORY-1", "Ca 1 Sáng Nhà máy Mega (06h00 - 14h00)", ShiftType.FactoryMorning, new TimeSpan(6, 0, 0), new TimeSpan(14, 0, 0), 45, 1.00m),
            new Shift("SH-FACTORY-2", "Ca 2 Chiều Nhà máy Mega (14h00 - 22h00)", ShiftType.FactoryAfternoon, new TimeSpan(14, 0, 0), new TimeSpan(22, 0, 0), 45, 1.10m),
            new Shift("SH-FACTORY-3", "Ca 3 Đêm Nhà máy Mega (22h00 - 06h00) [Phụ cấp đêm +30%]", ShiftType.FactoryNight, new TimeSpan(22, 0, 0), new TimeSpan(6, 0, 0), 45, 1.30m),

            // Mô hình 3: Khối Trang trại Bò sữa sinh thái (Ca gãy 2 đợt)
            new Shift("SH-FARM-EARLY", "Ca Sớm Trang trại Tây Ninh - Vắt sữa 1 (04h30 - 08h30)", ShiftType.FarmEarlyCare, new TimeSpan(4, 30, 0), new TimeSpan(8, 30, 0), 0, 1.15m),
            new Shift("SH-FARM-LATE", "Ca Chiều Trang trại Tây Ninh - Vắt sữa 2 (13h30 - 17h30)", ShiftType.FarmLateCare, new TimeSpan(13, 30, 0), new TimeSpan(17, 30, 0), 0, 1.10m)
        };

        await _context.Shifts.AddRangeAsync(shifts, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static void ValidateShiftCompatibility(EmployeeType empType, ShiftType shiftType)
    {
        if (empType == EmployeeType.Office && (shiftType == ShiftType.FactoryNight || shiftType == ShiftType.FarmEarlyCare))
        {
            throw new DomainException("Nhân viên văn phòng không được gán ca kíp nhà máy hoặc ca sáng sớm trang trại bò sữa.", "ERR_SHIFT_INCOMPATIBLE");
        }
    }

    private static string GetShiftCategoryName(ShiftType type) => type switch
    {
        ShiftType.OfficeStandard => "Mô hình 1: Khối Hành chính Văn phòng (08h00 - 17h30)",
        ShiftType.FactoryMorning => "Mô hình 2: Nhà máy Sữa - Ca 1 (06h - 14h)",
        ShiftType.FactoryAfternoon => "Mô hình 2: Nhà máy Sữa - Ca 2 (14h - 22h)",
        ShiftType.FactoryNight => "Mô hình 2: Nhà máy Sữa - Ca 3 Đêm (+30% Phụ cấp đêm)",
        ShiftType.FarmEarlyCare => "Mô hình 3: Trang trại Bò sữa - Ca Sớm (04h30 - 08h30)",
        ShiftType.FarmLateCare => "Mô hình 3: Trang trại Bò sữa - Ca Chiều (13h30 - 17h30)",
        _ => "Ca tiêu chuẩn"
    };

    private static AttendanceRecordDto MapAttendanceToDto(Attendance a, Employee emp, Shift shift)
    {
        return new AttendanceRecordDto(
            a.Id,
            a.EmployeeId,
            emp?.EmployeeCode ?? "N/A",
            emp?.FullName ?? "N/A",
            emp?.OrganizationalUnit?.UnitName ?? "Vinamilk",
            emp?.EmployeeType ?? EmployeeType.Office,
            a.ShiftId,
            shift?.ShiftName ?? "Ca làm việc",
            a.AttendanceDate,
            a.CheckInTime,
            a.CheckOutTime,
            a.WorkHours,
            a.OvertimeHours,
            a.Status,
            a.Note,
            emp?.DiligenceScore ?? 100m
        );
    }
}
