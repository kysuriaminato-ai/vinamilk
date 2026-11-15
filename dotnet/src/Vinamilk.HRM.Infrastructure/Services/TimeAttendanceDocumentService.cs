using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Application.TimeAttendance;
using Vinamilk.HRM.Domain.Entities.SystemEntities;
using Vinamilk.HRM.Domain.Entities.Time;

namespace Vinamilk.HRM.Infrastructure.Services;

public record AttendanceImportRowResult(
    int RowNumber,
    string EmployeeCode,
    string FullName,
    string WorkDateText,
    bool IsSuccess,
    string Message);

public record AttendanceImportSummaryResult(
    int TotalRows,
    int SuccessCount,
    int ErrorCount,
    IReadOnlyList<AttendanceImportRowResult> Details);

public record AttendanceImportPreviewRow(
    int RowNumber,
    int? EmployeeId,
    string EmployeeCode,
    string FullName,
    DateOnly? WorkDate,
    string WorkDateText,
    int? WorkShiftId,
    string ShiftCode,
    TimeOnly? CheckInTime,
    TimeOnly? CheckOutTime,
    string AttendanceSymbol,
    decimal OvertimeHours150,
    decimal OvertimeHours200,
    decimal OvertimeHours300,
    string DeviceNote,
    bool IsValid,
    string ValidationMessage);

public record AttendanceImportPreviewResult(
    int TotalRows,
    int ValidCount,
    int ErrorCount,
    IReadOnlyList<AttendanceImportPreviewRow> Rows);

public class TimeAttendanceDocumentService
{
    private readonly IApplicationDbContext _context;

    public TimeAttendanceDocumentService(IApplicationDbContext context)
    {
        _context = context;
    }

    // ========================================================================
    // 1. TẠO FILE EXCEL MẪU IMPORT CHẤM CÔNG TỪ MÁY VÂN TAY / EXCEL (.XLSX)
    // ========================================================================
    public byte[] GenerateAttendanceImportTemplateExcel()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Import_ChamCong");

        var headers = new[]
        {
            "Mã NV (*)",
            "Họ và tên",
            "Ngày chấm công (YYYY-MM-DD) (*)",
            "Mã ca (HC/CA1/CA2/CA3)",
            "Giờ vào (HH:mm)",
            "Giờ ra (HH:mm)",
            "Ký hiệu công (X/CT/P/Om)",
            "Giờ OT 150%",
            "Giờ OT 200%",
            "Giờ OT 300%",
            "Ghi chú / Máy chấm công"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#003DA5");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        var todayStr = DateTime.Today.ToString("yyyy-MM-dd");

        // Dòng mẫu 1: Chấm công hành chính bình thường
        ws.Cell(2, 1).Value = "VNM-0001";
        ws.Cell(2, 2).Value = "Nguyễn Văn Hùng";
        ws.Cell(2, 3).Value = todayStr;
        ws.Cell(2, 4).Value = "HC";
        ws.Cell(2, 5).Value = "07:55";
        ws.Cell(2, 6).Value = "17:05";
        ws.Cell(2, 7).Value = "X";
        ws.Cell(2, 8).Value = 0;
        ws.Cell(2, 9).Value = 0;
        ws.Cell(2, 10).Value = 0;
        ws.Cell(2, 11).Value = "ZKTECO-F22-GATE1";

        // Dòng mẫu 2: Đi muộn 15 phút + Tăng ca 2h
        ws.Cell(3, 1).Value = "VNM-0002";
        ws.Cell(3, 2).Value = "Trần Thị Mai";
        ws.Cell(3, 3).Value = todayStr;
        ws.Cell(3, 4).Value = "CA1";
        ws.Cell(3, 5).Value = "06:15";
        ws.Cell(3, 6).Value = "16:00";
        ws.Cell(3, 7).Value = "X";
        ws.Cell(3, 8).Value = 2.0;
        ws.Cell(3, 9).Value = 0;
        ws.Cell(3, 10).Value = 0;
        ws.Cell(3, 11).Value = "RONALD-JACK-PRO-NM1";

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    // ========================================================================
    // 2A. XEM TRƯỚC IMPORT EXCEL BẢNG CÔNG & BÁO LỖI TỪNG DÒNG (CHƯA GHI DB)
    // ========================================================================
    public async Task<AttendanceImportPreviewResult> PreviewAttendanceImportFromExcelAsync(
        Stream excelStream,
        CancellationToken cancellationToken = default)
    {
        using var workbook = new XLWorkbook(excelStream);
        var ws = workbook.Worksheets.First();
        var rows = ws.RangeUsed()?.RowsUsed().Skip(1).ToList() ?? new List<IXLRangeRow>();

        var empDict = await _context.Employees
            .AsNoTracking()
            .ToDictionaryAsync(e => e.EmployeeCode.ToUpperInvariant(), e => e, cancellationToken);

        var shiftDict = await _context.WorkShifts
            .AsNoTracking()
            .ToDictionaryAsync(s => s.ShiftCode.ToUpperInvariant(), s => s, cancellationToken);

        var lockedMonths = await _context.MonthlyAttendanceSummaries
            .AsNoTracking()
            .Where(m => m.IsLocked)
            .Select(m => new { m.Month, m.Year })
            .Distinct()
            .ToListAsync(cancellationToken);

        var lockedSet = lockedMonths.Select(x => (x.Year, x.Month)).ToHashSet();
        var seenInFile = new HashSet<(int EmpId, DateOnly Date)>();
        var previewRows = new List<AttendanceImportPreviewRow>();

        foreach (var row in rows)
        {
            int rowNum = row.RowNumber();
            string empCode = row.Cell(1).GetString().Trim().ToUpperInvariant();
            string empName = row.Cell(2).GetString().Trim();
            string dateStr = row.Cell(3).GetString().Trim();
            string shiftCode = row.Cell(4).GetString().Trim().ToUpperInvariant();
            string checkInStr = row.Cell(5).GetString().Trim();
            string checkOutStr = row.Cell(6).GetString().Trim();
            string symbol = row.Cell(7).GetString().Trim().ToUpperInvariant();
            decimal.TryParse(row.Cell(8).GetString().Trim(), out var ot150);
            decimal.TryParse(row.Cell(9).GetString().Trim(), out var ot200);
            decimal.TryParse(row.Cell(10).GetString().Trim(), out var ot300);
            string deviceNote = row.Cell(11).GetString().Trim();

            if (string.IsNullOrEmpty(empCode) && string.IsNullOrEmpty(dateStr))
                continue;

            if (!empDict.TryGetValue(empCode, out var emp))
            {
                previewRows.Add(new(rowNum, null, empCode, empName, null, dateStr, null, shiftCode, null, null, symbol, ot150, ot200, ot300, deviceNote,
                    false, $"Mã nhân viên '{empCode}' không tồn tại trong hệ thống."));
                continue;
            }

            if (!DateOnly.TryParse(dateStr, out var attDate))
            {
                previewRows.Add(new(rowNum, emp.Id, empCode, emp.FullName, null, dateStr, null, shiftCode, null, null, symbol, ot150, ot200, ot300, deviceNote,
                    false, $"Ngày chấm công '{dateStr}' không đúng định dạng YYYY-MM-DD."));
                continue;
            }

            if (lockedSet.Contains((attDate.Year, attDate.Month)))
            {
                previewRows.Add(new(rowNum, emp.Id, empCode, emp.FullName, attDate, attDate.ToString("dd/MM/yyyy"), null, shiftCode, null, null, symbol, ot150, ot200, ot300, deviceNote,
                    false, $"Bảng công tháng {attDate.Month:D2}/{attDate.Year} đã KHÓA, không thể import đè."));
                continue;
            }

            if (!seenInFile.Add((emp.Id, attDate)))
            {
                previewRows.Add(new(rowNum, emp.Id, empCode, emp.FullName, attDate, attDate.ToString("dd/MM/yyyy"), null, shiftCode, null, null, symbol, ot150, ot200, ot300, deviceNote,
                    false, $"Trùng dòng cho NV {empCode} ngày {attDate:dd/MM/yyyy} trong cùng file."));
                continue;
            }

            var targetShiftCode = string.IsNullOrEmpty(shiftCode) ? "HC" : shiftCode;
            if (!shiftDict.TryGetValue(targetShiftCode, out var shift))
            {
                previewRows.Add(new(rowNum, emp.Id, empCode, emp.FullName, attDate, attDate.ToString("dd/MM/yyyy"), null, targetShiftCode, null, null, symbol, ot150, ot200, ot300, deviceNote,
                    false, $"Mã ca '{targetShiftCode}' không tồn tại."));
                continue;
            }

            TimeOnly? checkIn = null;
            TimeOnly? checkOut = null;
            if (!string.IsNullOrEmpty(checkInStr))
            {
                if (!TimeOnly.TryParse(checkInStr, out var parsedIn))
                {
                    previewRows.Add(new(rowNum, emp.Id, empCode, emp.FullName, attDate, attDate.ToString("dd/MM/yyyy"), shift.Id, targetShiftCode, null, null, symbol, ot150, ot200, ot300, deviceNote,
                        false, $"Giờ vào '{checkInStr}' không đúng định dạng HH:mm."));
                    continue;
                }
                checkIn = parsedIn;
            }

            if (!string.IsNullOrEmpty(checkOutStr))
            {
                if (!TimeOnly.TryParse(checkOutStr, out var parsedOut))
                {
                    previewRows.Add(new(rowNum, emp.Id, empCode, emp.FullName, attDate, attDate.ToString("dd/MM/yyyy"), shift.Id, targetShiftCode, checkIn, null, symbol, ot150, ot200, ot300, deviceNote,
                        false, $"Giờ ra '{checkOutStr}' không đúng định dạng HH:mm."));
                    continue;
                }
                checkOut = parsedOut;
            }

            var effSymbol = string.IsNullOrEmpty(symbol) ? "X" : symbol;
            previewRows.Add(new(
                rowNum, emp.Id, empCode, emp.FullName, attDate, attDate.ToString("dd/MM/yyyy"),
                shift.Id, targetShiftCode, checkIn, checkOut, effSymbol,
                ot150, ot200, ot300, string.IsNullOrEmpty(deviceNote) ? "EXCEL-IMPORT" : deviceNote,
                true, $"Hợp lệ (Ca {targetShiftCode}: {checkInStr}-{checkOutStr}, ký hiệu '{effSymbol}')"));
        }

        return new AttendanceImportPreviewResult(
            previewRows.Count,
            previewRows.Count(r => r.IsValid),
            previewRows.Count(r => !r.IsValid),
            previewRows);
    }

    // ========================================================================
    // 2B. XÁC NHẬN GHI DỮ LIỆU HỢP LỆ TỪ BẢN XEM TRƯỚC EXCEL
    // ========================================================================
    public async Task<AttendanceImportSummaryResult> ConfirmAttendanceImportAsync(
        IReadOnlyList<AttendanceImportPreviewRow> previewRows,
        int actorUserId,
        string actorUsername,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<AttendanceImportRowResult>();
        var touchedMonths = new HashSet<(int Year, int Month)>();
        var shiftsById = await _context.WorkShifts.AsNoTracking().ToDictionaryAsync(s => s.Id, cancellationToken);

        foreach (var row in previewRows)
        {
            if (!row.IsValid || !row.EmployeeId.HasValue || !row.WorkDate.HasValue)
            {
                results.Add(new(row.RowNumber, row.EmployeeCode, row.FullName, row.WorkDateText, false, row.ValidationMessage));
                continue;
            }

            var empId = row.EmployeeId.Value;
            var attDate = row.WorkDate.Value;
            var isLocked = await _context.MonthlyAttendanceSummaries.AsNoTracking()
                .AnyAsync(m => m.Month == attDate.Month && m.Year == attDate.Year && m.IsLocked, cancellationToken);
            if (isLocked)
            {
                results.Add(new(row.RowNumber, row.EmployeeCode, row.FullName, row.WorkDateText, false, $"Bảng công tháng {attDate.Month:D2}/{attDate.Year} đã khóa."));
                continue;
            }

            WorkShift? shift = row.WorkShiftId.HasValue ? shiftsById.GetValueOrDefault(row.WorkShiftId.Value) : null;
            DateTime? checkInDt = row.CheckInTime.HasValue ? attDate.ToDateTime(row.CheckInTime.Value) : null;
            DateTime? checkOutDt = null;
            if (row.CheckOutTime.HasValue)
            {
                bool nextDay = shift?.IsOvernight == true || (row.CheckInTime.HasValue && row.CheckOutTime.Value <= row.CheckInTime.Value);
                checkOutDt = (nextDay ? attDate.AddDays(1) : attDate).ToDateTime(row.CheckOutTime.Value);
            }

            if (checkInDt.HasValue)
            {
                var inKey = AttendanceDeviceIngestionEngine.BuildExternalLogKey("EXCEL", row.EmployeeCode, checkInDt.Value, "CheckIn");
                if (!await _context.AttendanceLogs.AsNoTracking().AnyAsync(l => l.ExternalLogKey == inKey, cancellationToken))
                {
                    await _context.AttendanceLogs.AddAsync(new AttendanceLog
                    {
                        EmployeeId = empId,
                        PunchTime = checkInDt.Value,
                        PunchType = "CheckIn",
                        SourceType = "ExcelImport",
                        DeviceId = row.DeviceNote,
                        LocationAddress = "Import Excel đã xác nhận",
                        IpAddress = ipAddress,
                        EnteredByUserId = actorUserId,
                        EnteredByUsername = actorUsername,
                        GeofenceStatus = "NotApplicable",
                        ExternalLogKey = inKey
                    }, cancellationToken);
                }
            }

            if (checkOutDt.HasValue)
            {
                var outKey = AttendanceDeviceIngestionEngine.BuildExternalLogKey("EXCEL", row.EmployeeCode, checkOutDt.Value, "CheckOut");
                if (!await _context.AttendanceLogs.AsNoTracking().AnyAsync(l => l.ExternalLogKey == outKey, cancellationToken))
                {
                    await _context.AttendanceLogs.AddAsync(new AttendanceLog
                    {
                        EmployeeId = empId,
                        PunchTime = checkOutDt.Value,
                        PunchType = "CheckOut",
                        SourceType = "ExcelImport",
                        DeviceId = row.DeviceNote,
                        LocationAddress = "Import Excel đã xác nhận",
                        IpAddress = ipAddress,
                        EnteredByUserId = actorUserId,
                        EnteredByUsername = actorUsername,
                        GeofenceStatus = "NotApplicable",
                        ExternalLogKey = outKey
                    }, cancellationToken);
                }
            }

            var statutory = await AttendancePolicyAndShiftEngine.ResolveStatutoryParameterAsync(_context, attDate, cancellationToken);
            var resolvedPolicy = await AttendancePolicyAndShiftEngine.ResolveAttendancePolicyAsync(_context, empId, attDate, cancellationToken);

            decimal workingHours = shift?.StandardHours ?? 8.0m;
            decimal workdayCount = row.AttendanceSymbol is "KL" or "V" or "OFF" ? 0m : 1.0m;
            decimal nightHours = 0m;
            int lateMins = 0, earlyMins = 0;

            if (shift != null && (checkInDt.HasValue || checkOutDt.HasValue))
            {
                var calc = AttendancePolicyAndShiftEngine.CalculatePunchAttendance(attDate, shift, checkInDt, checkOutDt, resolvedPolicy.Policy, statutory);
                workingHours = calc.WorkingHours > 0m ? calc.WorkingHours : shift.StandardHours;
                nightHours = calc.NightShiftHours;
                lateMins = calc.LateMinutes;
                earlyMins = calc.EarlyLeaveMinutes;
                workdayCount = row.AttendanceSymbol is "KL" or "V" or "OFF" ? 0m : (calc.WorkdayCount > 0m ? calc.WorkdayCount : shift.WorkdayValue);
            }

            var existingDaily = await _context.DailyAttendances
                .FirstOrDefaultAsync(d => d.EmployeeId == empId && d.AttendanceDate == attDate, cancellationToken);

            if (existingDaily == null)
            {
                await _context.DailyAttendances.AddAsync(new DailyAttendance
                {
                    EmployeeId = empId,
                    AttendanceDate = attDate,
                    WorkShiftId = shift?.Id,
                    CheckInTime = checkInDt,
                    CheckOutTime = checkOutDt,
                    AttendanceSymbol = row.AttendanceSymbol,
                    WorkingHours = workingHours,
                    WorkdayCount = workdayCount,
                    LateMinutes = lateMins,
                    EarlyLeaveMinutes = earlyMins,
                    OvertimeHours150 = row.OvertimeHours150,
                    OvertimeHours200 = row.OvertimeHours200,
                    OvertimeHours300 = row.OvertimeHours300,
                    NightShiftHours = nightHours,
                    EditReason = $"[ExcelImport: {actorUsername}] {row.DeviceNote}"
                }, cancellationToken);
            }
            else
            {
                existingDaily.WorkShiftId = shift?.Id ?? existingDaily.WorkShiftId;
                existingDaily.CheckInTime = checkInDt ?? existingDaily.CheckInTime;
                existingDaily.CheckOutTime = checkOutDt ?? existingDaily.CheckOutTime;
                existingDaily.AttendanceSymbol = row.AttendanceSymbol;
                existingDaily.WorkingHours = workingHours;
                existingDaily.WorkdayCount = workdayCount;
                existingDaily.LateMinutes = lateMins;
                existingDaily.EarlyLeaveMinutes = earlyMins;
                existingDaily.OvertimeHours150 = row.OvertimeHours150;
                existingDaily.OvertimeHours200 = row.OvertimeHours200;
                existingDaily.OvertimeHours300 = row.OvertimeHours300;
                existingDaily.NightShiftHours = nightHours;
                existingDaily.EditReason = $"[ExcelImport: {actorUsername}] {row.DeviceNote}";
            }

            touchedMonths.Add((attDate.Year, attDate.Month));
            results.Add(new(row.RowNumber, row.EmployeeCode, row.FullName, row.WorkDateText, true,
                $"Đã lưu công '{row.AttendanceSymbol}' (Trễ: {lateMins}p, Đêm: {nightHours:0.#}h, OT: {row.OvertimeHours150 + row.OvertimeHours200 + row.OvertimeHours300:0.#}h)"));
        }

        await _context.AuditLogs.AddAsync(new AuditLog
        {
            UserId = actorUserId,
            Username = actorUsername,
            ActionType = "ExcelImportConfirm",
            ModuleName = "TimeAttendance",
            EntityName = "AttendanceLog",
            EntityId = "BatchExcel",
            NewValuesJson = $"{{\"total\":{results.Count},\"success\":{results.Count(r => r.IsSuccess)},\"errors\":{results.Count(r => !r.IsSuccess)}}}",
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        foreach (var (yr, mo) in touchedMonths)
        {
            await AttendanceBoardSeeder.RecalculateSummariesInternalAsync(_context, mo, yr, cancellationToken);
        }

        return new AttendanceImportSummaryResult(
            results.Count,
            results.Count(r => r.IsSuccess),
            results.Count(r => !r.IsSuccess),
            results);
    }

    // ========================================================================
    // 2C. WRAPPER IMPORT TRỰC TIẾP (TƯƠNG THÍCH NGƯỢC)
    // ========================================================================
    public async Task<AttendanceImportSummaryResult> ImportAttendanceFromExcelAsync(
        Stream excelStream,
        int actorUserId,
        string actorUsername,
        CancellationToken cancellationToken = default)
    {
        var preview = await PreviewAttendanceImportFromExcelAsync(excelStream, cancellationToken);
        return await ConfirmAttendanceImportAsync(preview.Rows, actorUserId, actorUsername, null, cancellationToken);
    }

    // ========================================================================
    // 3. XUẤT BẢNG CHẤM CÔNG THÁNG 31 NGÀY RA EXCEL (.XLSX) CÓ KÝ HIỆU, TỔNG HỢP & CHỮ KÝ
    // ========================================================================
    public byte[] ExportMonthlyTimesheetToExcel(MonthlyAttendanceBoardDto board)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add($"BangCong_T{board.Month:D2}_{board.Year}");

        ws.Cell(1, 1).Value = $"CÔNG TY CỔ PHẦN SỮA VIỆT NAM (VINAMILK) — BẢNG CHẤM CÔNG TỔNG HỢP THÁNG {board.Month:D2}/{board.Year}";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 13;
        ws.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#003DA5");

        var legendText = board.Symbols != null && board.Symbols.Count > 0
            ? string.Join(" | ", board.Symbols.Select(s => $"{s.SymbolCode}: {s.SymbolName}"))
            : "X: Đi làm (1.0) | ½: Nửa công (0.5) | P: Phép năm | L: Nghỉ lễ | Ô: Ốm BHXH | TS: Thai sản | CT: Công tác | R: Nghỉ riêng có lương | Ro: Nghỉ không lương | KL: Vắng không phép | OFF: Nghỉ tuần | TC: Thiếu chấm công";
        ws.Cell(2, 1).Value = $"Ký hiệu: {legendText} | Trạng thái chốt: {board.ClosingStatus}";
        ws.Cell(2, 1).Style.Font.Italic = true;
        ws.Cell(2, 1).Style.Font.FontSize = 9;

        int col = 1;
        ws.Cell(3, col++).Value = "STT";
        ws.Cell(3, col++).Value = "Mã NV";
        ws.Cell(3, col++).Value = "Họ và tên";
        ws.Cell(3, col++).Value = "Đơn vị";

        for (int d = 1; d <= board.DaysInMonth; d++)
        {
            ws.Cell(3, col++).Value = d.ToString("D2");
        }

        ws.Cell(3, col++).Value = "Công chuẩn";
        ws.Cell(3, col++).Value = "Công thực tế";
        ws.Cell(3, col++).Value = "Phép năm (P)";
        ws.Cell(3, col++).Value = "Nghỉ lễ (L)";
        ws.Cell(3, col++).Value = "Nghỉ riêng có lương (R)";
        ws.Cell(3, col++).Value = "Ốm/TS (Ô/TS)";
        ws.Cell(3, col++).Value = "Không lương (Ro)";
        ws.Cell(3, col++).Value = "Vắng KP (KL)";
        ws.Cell(3, col++).Value = "Số lần trễ";
        ws.Cell(3, col++).Value = "Phút trễ";
        ws.Cell(3, col++).Value = "Về sớm (lần)";
        ws.Cell(3, col++).Value = "Quên chấm";
        ws.Cell(3, col++).Value = "Giờ đêm";
        ws.Cell(3, col++).Value = "OT 150% Ngày";
        ws.Cell(3, col++).Value = "OT 150% Đêm";
        ws.Cell(3, col++).Value = "OT 200% Ngày";
        ws.Cell(3, col++).Value = "OT 200% Đêm";
        ws.Cell(3, col++).Value = "OT 300% Ngày";
        ws.Cell(3, col++).Value = "OT 300% Đêm";
        ws.Cell(3, col++).Value = "Tổng công tính lương";

        int totalCols = col - 1;
        var headerRange = ws.Range(3, 1, 3, totalCols);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#003DA5");
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        int rowIdx = 4;
        int stt = 1;
        foreach (var empRow in board.MatrixRows)
        {
            int c = 1;
            ws.Cell(rowIdx, c++).Value = stt++;
            ws.Cell(rowIdx, c++).Value = empRow.EmployeeCode;
            ws.Cell(rowIdx, c++).Value = empRow.FullName;
            ws.Cell(rowIdx, c++).Value = empRow.UnitName;

            foreach (var dayCell in empRow.Days)
            {
                var cell = ws.Cell(rowIdx, c++);
                cell.Value = dayCell.Symbol;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            ws.Cell(rowIdx, c++).Value = empRow.StandardWorkdays;
            ws.Cell(rowIdx, c++).Value = empRow.ActualWorkdays;
            ws.Cell(rowIdx, c++).Value = empRow.PaidLeaveDays;
            ws.Cell(rowIdx, c++).Value = empRow.HolidayDays;
            ws.Cell(rowIdx, c++).Value = empRow.TotalPaidPersonalLeaveDays;
            ws.Cell(rowIdx, c++).Value = empRow.TotalSocialInsuranceLeaveDays;
            ws.Cell(rowIdx, c++).Value = empRow.TotalUnpaidPersonalLeaveDays;
            ws.Cell(rowIdx, c++).Value = empRow.TotalUnexcusedAbsentDays;
            ws.Cell(rowIdx, c++).Value = empRow.TotalLateCount;
            ws.Cell(rowIdx, c++).Value = empRow.TotalLateMinutes;
            ws.Cell(rowIdx, c++).Value = empRow.TotalEarlyLeaveCount;
            ws.Cell(rowIdx, c++).Value = empRow.TotalMissedPunchCount;
            ws.Cell(rowIdx, c++).Value = empRow.TotalNightHours;
            ws.Cell(rowIdx, c++).Value = empRow.TotalOtHours150Day > 0m ? empRow.TotalOtHours150Day : empRow.TotalOtHours150;
            ws.Cell(rowIdx, c++).Value = empRow.TotalOtHours150Night;
            ws.Cell(rowIdx, c++).Value = empRow.TotalOtHours200Day > 0m ? empRow.TotalOtHours200Day : empRow.TotalOtHours200;
            ws.Cell(rowIdx, c++).Value = empRow.TotalOtHours200Night;
            ws.Cell(rowIdx, c++).Value = empRow.TotalOtHours300Day > 0m ? empRow.TotalOtHours300Day : empRow.TotalOtHours300;
            ws.Cell(rowIdx, c++).Value = empRow.TotalOtHours300Night;
            ws.Cell(rowIdx, c++).Value = empRow.TotalPayableWorkdays;

            rowIdx++;
        }

        var bodyRange = ws.Range(3, 1, Math.Max(3, rowIdx - 1), totalCols);
        bodyRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        bodyRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        // Khối chữ ký 3 cấp chốt công ở cuối bảng công
        int sigRow = rowIdx + 2;
        ws.Cell(sigRow, 2).Value = "NGƯỜI LẬP BIỂU / HR ĐƠN VỊ";
        ws.Cell(sigRow, 2).Style.Font.Bold = true;
        ws.Cell(sigRow + 1, 2).Value = !string.IsNullOrWhiteSpace(board.UnitSubmittedBy) ? $"(Đã chốt: {board.UnitSubmittedBy})" : "(Ký, ghi rõ họ tên)";

        int midCol = Math.Max(6, totalCols / 2);
        ws.Cell(sigRow, midCol).Value = "TRƯỞNG BỘ PHẬN XÁC NHẬN";
        ws.Cell(sigRow, midCol).Style.Font.Bold = true;
        ws.Cell(sigRow + 1, midCol).Value = !string.IsNullOrWhiteSpace(board.ManagerConfirmedBy) ? $"(Đã duyệt: {board.ManagerConfirmedBy})" : "(Ký, ghi rõ họ tên)";

        int rightCol = Math.Max(10, totalCols - 4);
        ws.Cell(sigRow, rightCol).Value = "HR TỔNG HỢP / KHÓA CÔNG";
        ws.Cell(sigRow, rightCol).Style.Font.Bold = true;
        ws.Cell(sigRow + 1, rightCol).Value = !string.IsNullOrWhiteSpace(board.LockedBy) ? $"(Đã khóa: {board.LockedBy})" : "(Ký, ghi rõ họ tên)";

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }
}

