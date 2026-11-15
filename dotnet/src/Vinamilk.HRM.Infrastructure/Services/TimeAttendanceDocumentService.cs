using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Application.TimeAttendance;
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
    // 2. IMPORT DỮ LIỆU CHẤM CÔNG TỪ MÁY CHẤM CÔNG / FILE EXCEL (.XLSX)
    // ========================================================================
    public async Task<AttendanceImportSummaryResult> ImportAttendanceFromExcelAsync(
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
        var touchedMonths = new HashSet<(int Year, int Month)>();
        var results = new List<AttendanceImportRowResult>();

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
                results.Add(new AttendanceImportRowResult(rowNum, empCode, empName, dateStr, false, $"Mã nhân viên '{empCode}' không tồn tại trong hệ thống."));
                continue;
            }

            if (!DateOnly.TryParse(dateStr, out var attDate))
            {
                results.Add(new AttendanceImportRowResult(rowNum, empCode, emp.FullName, dateStr, false, $"Ngày chấm công '{dateStr}' không đúng định dạng YYYY-MM-DD."));
                continue;
            }

            if (lockedSet.Contains((attDate.Year, attDate.Month)))
            {
                results.Add(new AttendanceImportRowResult(rowNum, empCode, emp.FullName, dateStr, false, $"Bảng công tháng {attDate.Month:D2}/{attDate.Year} đã KHÓA, không thể import đè."));
                continue;
            }

            shiftDict.TryGetValue(string.IsNullOrEmpty(shiftCode) ? "HC" : shiftCode, out var shift);
            var shiftStart = shift?.StartTime ?? new TimeOnly(8, 0);
            var shiftEnd = shift?.EndTime ?? new TimeOnly(17, 0);
            var graceMins = shift?.GraceLateMinutes ?? 5;

            DateTime? checkInDt = null;
            DateTime? checkOutDt = null;
            int lateMins = 0;
            int earlyMins = 0;

            if (TimeOnly.TryParse(checkInStr, out var inTime))
            {
                checkInDt = attDate.ToDateTime(inTime);
                if (inTime > shiftStart.AddMinutes(graceMins))
                    lateMins = (int)(inTime - shiftStart).TotalMinutes;

                await _context.AttendanceLogs.AddAsync(new AttendanceLog
                {
                    EmployeeId = emp.Id,
                    PunchTime = checkInDt.Value,
                    PunchType = "CheckIn",
                    SourceType = "BiometricDevice",
                    DeviceId = string.IsNullOrEmpty(deviceNote) ? "EXCEL-IMPORT" : deviceNote,
                    LocationAddress = "Import từ máy chấm công / Excel"
                }, cancellationToken);
            }

            if (TimeOnly.TryParse(checkOutStr, out var outTime))
            {
                checkOutDt = attDate.ToDateTime(outTime);
                if (!shift?.IsOvernight == true && outTime < shiftEnd)
                    earlyMins = (int)(shiftEnd - outTime).TotalMinutes;
            }

            if (string.IsNullOrEmpty(symbol))
                symbol = "X";

            var existingDaily = await _context.DailyAttendances
                .FirstOrDefaultAsync(d => d.EmployeeId == emp.Id && d.AttendanceDate == attDate, cancellationToken);

            if (existingDaily == null)
            {
                await _context.DailyAttendances.AddAsync(new DailyAttendance
                {
                    EmployeeId = emp.Id,
                    AttendanceDate = attDate,
                    WorkShiftId = shift?.Id,
                    CheckInTime = checkInDt,
                    CheckOutTime = checkOutDt,
                    AttendanceSymbol = symbol,
                    WorkingHours = shift?.StandardHours ?? 8.0m,
                    WorkdayCount = symbol is "KL" or "V" or "OFF" ? 0m : 1.0m,
                    LateMinutes = lateMins,
                    EarlyLeaveMinutes = earlyMins,
                    OvertimeHours150 = ot150,
                    OvertimeHours200 = ot200,
                    OvertimeHours300 = ot300,
                    NightShiftHours = shift?.IsOvernight == true ? 8.0m : 0m,
                    EditReason = string.IsNullOrEmpty(deviceNote) ? "Import Excel" : deviceNote
                }, cancellationToken);
            }
            else
            {
                existingDaily.WorkShiftId = shift?.Id ?? existingDaily.WorkShiftId;
                existingDaily.CheckInTime = checkInDt ?? existingDaily.CheckInTime;
                existingDaily.CheckOutTime = checkOutDt ?? existingDaily.CheckOutTime;
                existingDaily.AttendanceSymbol = symbol;
                existingDaily.LateMinutes = lateMins;
                existingDaily.EarlyLeaveMinutes = earlyMins;
                existingDaily.OvertimeHours150 = ot150;
                existingDaily.OvertimeHours200 = ot200;
                existingDaily.OvertimeHours300 = ot300;
                existingDaily.EditReason = string.IsNullOrEmpty(deviceNote) ? "Cập nhật từ Import Excel" : deviceNote;
            }

            touchedMonths.Add((attDate.Year, attDate.Month));
            results.Add(new AttendanceImportRowResult(
                rowNum,
                empCode,
                emp.FullName,
                attDate.ToString("dd/MM/yyyy"),
                true,
                $"Đã lưu công '{symbol}' ({checkInStr}-{checkOutStr}, Trễ: {lateMins}p, OT: {ot150 + ot200 + ot300:0.#}h)"));
        }

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
    // 3. XUẤT BẢNG CHẤM CÔNG THÁNG 31 NGÀY RA EXCEL (.XLSX)
    // ========================================================================
    public byte[] ExportMonthlyTimesheetToExcel(MonthlyAttendanceBoardDto board)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add($"BangCong_T{board.Month:D2}_{board.Year}");

        ws.Cell(1, 1).Value = $"CÔNG TY CỔ PHẦN SỮA VIỆT NAM (VINAMILK) — BẢNG CHẤM CÔNG TỔNG HỢP THÁNG {board.Month:D2}/{board.Year}";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 13;
        ws.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#003DA5");

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
        ws.Cell(3, col++).Value = "Nghỉ phép (P)";
        ws.Cell(3, col++).Value = "Nghỉ lễ (L)";
        ws.Cell(3, col++).Value = "Số lần trễ";
        ws.Cell(3, col++).Value = "Phút trễ";
        ws.Cell(3, col++).Value = "OT 150%";
        ws.Cell(3, col++).Value = "OT 200%";
        ws.Cell(3, col++).Value = "OT 300%";
        ws.Cell(3, col++).Value = "Tổng công tính lương";

        var headerRange = ws.Range(3, 1, 3, col - 1);
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
            ws.Cell(rowIdx, c++).Value = empRow.TotalLateCount;
            ws.Cell(rowIdx, c++).Value = empRow.TotalLateMinutes;
            ws.Cell(rowIdx, c++).Value = empRow.TotalOtHours150;
            ws.Cell(rowIdx, c++).Value = empRow.TotalOtHours200;
            ws.Cell(rowIdx, c++).Value = empRow.TotalOtHours300;
            ws.Cell(rowIdx, c++).Value = empRow.TotalPayableWorkdays;

            rowIdx++;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }
}

