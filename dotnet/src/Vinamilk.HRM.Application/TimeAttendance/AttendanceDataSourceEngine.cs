using System.Globalization;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Domain.Entities.SystemEntities;
using Vinamilk.HRM.Domain.Entities.Time;

namespace Vinamilk.HRM.Application.TimeAttendance;

// ============================================================================
// 1. INTERFACE CONNECTOR MÁY CHẤM CÔNG & ĐỒNG BỘ HÀNG CHỜ CHƯA ÁNH XẠ
// ============================================================================

public record DeviceRawPunchRecord(
    string DeviceCode,
    string DeviceUserCode,
    DateTime PunchTime,
    string PunchType,
    string RawPayload);

public record DeviceSyncSummaryDto(
    string DeviceCode,
    int TotalRead,
    int ImportedCount,
    int SkippedDuplicateCount,
    int UnmappedCount,
    string Message);

public interface IAttendanceDeviceConnector
{
    string ConnectorType { get; }
    IReadOnlyList<DeviceRawPunchRecord> ParseRawPunches(Stream stream, string deviceCode);
    Task<IReadOnlyList<DeviceRawPunchRecord>> PullPunchesAsync(AttendanceDevice device, CancellationToken cancellationToken = default);
}

public interface IAttendanceDeviceSyncJob
{
    Task<IReadOnlyList<DeviceSyncSummaryDto>> SyncAllActiveDevicesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Cài đặt đọc file xuất chuẩn từ máy chấm công (CSV, TXT, ZKTeco DAT).
/// Cấu trúc sẵn IAttendanceDeviceConnector để mở rộng SDK ZKTeco / Hikvision API sau này.
/// </summary>
public sealed class FileAttendanceDeviceConnector : IAttendanceDeviceConnector
{
    public string ConnectorType => "FileExport";

    public IReadOnlyList<DeviceRawPunchRecord> ParseRawPunches(Stream stream, string deviceCode)
    {
        var results = new List<DeviceRawPunchRecord>();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            var parsed = TryParsePunchLine(trimmed, deviceCode);
            if (parsed != null)
                results.Add(parsed);
        }
        return results;
    }

    public async Task<IReadOnlyList<DeviceRawPunchRecord>> PullPunchesAsync(AttendanceDevice device, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(device.EndpointOrFolderPath))
            return Array.Empty<DeviceRawPunchRecord>();

        var all = new List<DeviceRawPunchRecord>();
        if (File.Exists(device.EndpointOrFolderPath))
        {
            await using var fs = File.OpenRead(device.EndpointOrFolderPath);
            all.AddRange(ParseRawPunches(fs, device.DeviceCode));
            return all;
        }

        if (Directory.Exists(device.EndpointOrFolderPath))
        {
            foreach (var file in Directory.EnumerateFiles(device.EndpointOrFolderPath)
                .Where(f => f.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".dat", StringComparison.OrdinalIgnoreCase)))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await using var fs = File.OpenRead(file);
                all.AddRange(ParseRawPunches(fs, device.DeviceCode));
            }
        }

        return all;
    }

    private static DeviceRawPunchRecord? TryParsePunchLine(string line, string deviceCode)
    {
        // Hỗ trợ tab-separated (ZKTeco .dat), comma/semicolon (.csv), hoặc khoảng trắng chuẩn
        string[] parts;
        if (line.Contains('\t'))
        {
            parts = line.Split('\t', StringSplitOptions.TrimEntries);
        }
        else if (line.Contains(','))
        {
            parts = line.Split(',', StringSplitOptions.TrimEntries);
        }
        else if (line.Contains(';'))
        {
            parts = line.Split(';', StringSplitOptions.TrimEntries);
        }
        else
        {
            // Định dạng khoảng trắng: "<userCode> <yyyy-MM-dd> <HH:mm:ss> [CheckIn|CheckOut]"
            var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokens.Length >= 3 && DateTime.TryParse($"{tokens[1]} {tokens[2]}", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtSpace))
            {
                var pType = tokens.Length >= 4 ? NormalizePunchType(tokens[3], dtSpace) : InferPunchTypeByTime(dtSpace);
                return new DeviceRawPunchRecord(deviceCode, tokens[0], dtSpace, pType, line);
            }
            return null;
        }

        if (parts.Length < 2)
            return null;

        // Bỏ qua dòng header nếu cột 2 không phải thời gian
        var userCode = parts[0].Trim();
        if (string.IsNullOrWhiteSpace(userCode)
            || userCode.Equals("DeviceUserCode", StringComparison.OrdinalIgnoreCase)
            || userCode.Equals("UserCode", StringComparison.OrdinalIgnoreCase)
            || userCode.Equals("EnrollNo", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!TryParseDateTime(parts[1], out var punchTime))
            return null;

        string? rawState = parts.Length switch
        {
            >= 4 when !string.IsNullOrWhiteSpace(parts[3]) && !int.TryParse(parts[2], out _) => parts[2],
            >= 4 when int.TryParse(parts[3], out _) => parts[3],
            >= 3 => parts[2],
            _ => null
        };

        // ZKTeco DAT chuẩn: EnrollNo \t DateTime \t VerifyMode \t InOutMode (0=In, 1=Out)
        if (line.Contains('\t') && parts.Length >= 4)
        {
            rawState = parts[3];
        }

        var normalizedType = !string.IsNullOrWhiteSpace(rawState)
            ? NormalizePunchType(rawState, punchTime)
            : InferPunchTypeByTime(punchTime);

        return new DeviceRawPunchRecord(deviceCode, userCode, punchTime, normalizedType, line);
    }

    private static bool TryParseDateTime(string text, out DateTime dt)
    {
        var formats = new[]
        {
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm",
            "dd/MM/yyyy HH:mm:ss",
            "dd/MM/yyyy HH:mm",
            "yyyy/MM/dd HH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss"
        };
        return DateTime.TryParseExact(text.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt)
            || DateTime.TryParse(text.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);
    }

    private static string NormalizePunchType(string raw, DateTime punchTime)
    {
        var token = raw.Trim().ToUpperInvariant();
        return token switch
        {
            "0" or "IN" or "I" or "CHECKIN" or "CHECK-IN" or "VAO" => "CheckIn",
            "1" or "OUT" or "O" or "CHECKOUT" or "CHECK-OUT" or "RA" => "CheckOut",
            "2" or "BREAKOUT" => "BreakOut",
            "3" or "BREAKIN" => "BreakIn",
            "4" => "CheckIn",
            "5" => "CheckOut",
            _ => InferPunchTypeByTime(punchTime)
        };
    }

    private static string InferPunchTypeByTime(DateTime dt) => dt.Hour < 12 ? "CheckIn" : "CheckOut";
}

// ============================================================================
// 2. SERVICE XỬ LÝ INGEST LOG MÁY CHẤM CÔNG + CHỐNG TRÙNG + HÀNG CHỜ CHƯA ÁNH XẠ
// ============================================================================

public static class AttendanceDeviceIngestionEngine
{
    public static string BuildExternalLogKey(string deviceCode, string deviceUserCode, DateTime punchTime, string punchType)
        => $"{deviceCode.Trim().ToUpperInvariant()}|{deviceUserCode.Trim().ToUpperInvariant()}|{punchTime:yyyyMMddHHmmss}|{punchType.Trim()}";

    public static async Task<DeviceSyncSummaryDto> IngestDevicePunchesAsync(
        IApplicationDbContext context,
        string deviceCode,
        IReadOnlyList<DeviceRawPunchRecord> rawRecords,
        string actorUsername,
        CancellationToken cancellationToken)
    {
        var normalizedDeviceCode = deviceCode.Trim().ToUpperInvariant();
        if (rawRecords.Count == 0)
        {
            return new DeviceSyncSummaryDto(normalizedDeviceCode, 0, 0, 0, 0, "Không có dữ liệu chấm công mới.");
        }

        var mappings = await context.EmployeeDeviceMappings
            .AsNoTracking()
            .Where(m => m.IsActive && (m.DeviceCode == null || m.DeviceCode == "" || m.DeviceCode.ToUpper() == normalizedDeviceCode))
            .ToListAsync(cancellationToken);

        var candidateKeys = rawRecords
            .Select(r => BuildExternalLogKey(normalizedDeviceCode, r.DeviceUserCode, r.PunchTime, r.PunchType))
            .Distinct()
            .ToList();

        var existingKeys = (await context.AttendanceLogs
            .AsNoTracking()
            .Where(l => l.ExternalLogKey != null && candidateKeys.Contains(l.ExternalLogKey))
            .Select(l => l.ExternalLogKey!)
            .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var existingUnmapped = (await context.UnmappedAttendanceLogs
            .AsNoTracking()
            .Where(u => u.DeviceCode == normalizedDeviceCode)
            .Select(u => new { u.DeviceUserCode, u.PunchTime, u.PunchType })
            .ToListAsync(cancellationToken))
            .Select(x => BuildExternalLogKey(normalizedDeviceCode, x.DeviceUserCode, x.PunchTime, x.PunchType))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var batchSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var touchedDailyKeys = new HashSet<(int EmpId, DateOnly WorkDate)>();
        int imported = 0, skippedDup = 0, unmapped = 0;

        foreach (var rec in rawRecords)
        {
            var extKey = BuildExternalLogKey(normalizedDeviceCode, rec.DeviceUserCode, rec.PunchTime, rec.PunchType);
            if (!batchSeen.Add(extKey) || existingKeys.Contains(extKey) || existingUnmapped.Contains(extKey))
            {
                skippedDup++;
                continue;
            }

            var mapping = mappings
                .Where(m => string.Equals(m.DeviceUserCode, rec.DeviceUserCode.Trim(), StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(m => string.Equals(m.DeviceCode, normalizedDeviceCode, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            if (mapping == null)
            {
                await context.UnmappedAttendanceLogs.AddAsync(new UnmappedAttendanceLog
                {
                    DeviceCode = normalizedDeviceCode,
                    DeviceUserCode = rec.DeviceUserCode.Trim(),
                    PunchTime = rec.PunchTime,
                    PunchType = rec.PunchType,
                    RawPayload = rec.RawPayload.Length > 400 ? rec.RawPayload[..400] : rec.RawPayload,
                    Status = "PendingMapping"
                }, cancellationToken);
                unmapped++;
                continue;
            }

            var resolvedShift = await AttendancePolicyAndShiftEngine.ResolveShiftForPunchAsync(
                context, mapping.EmployeeId, rec.PunchTime, cancellationToken);
            var workDate = resolvedShift?.AttributedDate ?? DateOnly.FromDateTime(rec.PunchTime);

            var isLocked = await context.MonthlyAttendanceSummaries
                .AsNoTracking()
                .AnyAsync(m => m.EmployeeId == mapping.EmployeeId && m.Month == workDate.Month && m.Year == workDate.Year && m.IsLocked, cancellationToken);
            if (isLocked)
            {
                skippedDup++;
                continue;
            }

            await context.AttendanceLogs.AddAsync(new AttendanceLog
            {
                EmployeeId = mapping.EmployeeId,
                PunchTime = rec.PunchTime,
                PunchType = rec.PunchType,
                SourceType = "BiometricDevice",
                DeviceId = normalizedDeviceCode,
                LocationAddress = $"Đồng bộ máy chấm công {normalizedDeviceCode} (Mã máy: {rec.DeviceUserCode})",
                GeofenceStatus = "NotApplicable",
                EnteredByUsername = actorUsername,
                ExternalLogKey = extKey
            }, cancellationToken);

            touchedDailyKeys.Add((mapping.EmployeeId, workDate));
            imported++;
        }

        await context.SaveChangesAsync(cancellationToken);

        await RecomputeDailyFromLogsAsync(context, touchedDailyKeys, "BiometricDevice", cancellationToken);

        var summaryMsg = $"Thiết bị {normalizedDeviceCode}: Đọc {rawRecords.Count}, Nhập {imported}, Bỏ qua trùng {skippedDup}, Chờ ánh xạ {unmapped}.";
        var device = await context.AttendanceDevices.FirstOrDefaultAsync(d => d.DeviceCode == normalizedDeviceCode, cancellationToken);
        if (device != null)
        {
            device.LastSyncAt = DateTime.UtcNow;
            device.LastSyncSummary = summaryMsg;
            await context.SaveChangesAsync(cancellationToken);
        }

        return new DeviceSyncSummaryDto(normalizedDeviceCode, rawRecords.Count, imported, skippedDup, unmapped, summaryMsg);
    }

    public static async Task RecomputeDailyFromLogsAsync(
        IApplicationDbContext context,
        IEnumerable<(int EmpId, DateOnly WorkDate)> keys,
        string sourceLabel,
        CancellationToken cancellationToken)
    {
        var keyList = keys.Distinct().ToList();
        if (keyList.Count == 0)
            return;

        var touchedMonths = new HashSet<(int Year, int Month)>();
        foreach (var (empId, workDate) in keyList)
        {
            var isLocked = await context.MonthlyAttendanceSummaries
                .AsNoTracking()
                .AnyAsync(m => m.EmployeeId == empId && m.Month == workDate.Month && m.Year == workDate.Year && m.IsLocked, cancellationToken);
            if (isLocked)
                continue;

            var windowStart = workDate.ToDateTime(TimeOnly.MinValue).AddHours(-4);
            var windowEnd = workDate.AddDays(1).ToDateTime(new TimeOnly(12, 0));
            var candidateLogs = await context.AttendanceLogs
                .AsNoTracking()
                .Where(l => l.EmployeeId == empId && l.PunchTime >= windowStart && l.PunchTime <= windowEnd)
                .OrderBy(l => l.PunchTime)
                .ToListAsync(cancellationToken);

            if (candidateLogs.Count == 0)
                continue;

            var matchedLogs = new List<AttendanceLog>();
            WorkShift? matchedShift = null;
            foreach (var log in candidateLogs)
            {
                var res = await AttendancePolicyAndShiftEngine.ResolveShiftForPunchAsync(context, empId, log.PunchTime, cancellationToken);
                if (res.HasValue && res.Value.AttributedDate == workDate)
                {
                    matchedLogs.Add(log);
                    matchedShift ??= res.Value.Shift;
                }
            }

            if (matchedLogs.Count == 0)
                matchedLogs = candidateLogs.Where(l => DateOnly.FromDateTime(l.PunchTime) == workDate).ToList();

            var checkIn = matchedLogs.FirstOrDefault()?.PunchTime;
            var checkOut = matchedLogs.Count > 1 ? matchedLogs.Last().PunchTime : (DateTime?)null;

            var daily = await context.DailyAttendances
                .FirstOrDefaultAsync(d => d.EmployeeId == empId && d.AttendanceDate == workDate, cancellationToken);

            if (daily == null)
            {
                daily = new DailyAttendance
                {
                    EmployeeId = empId,
                    AttendanceDate = workDate,
                    WorkShiftId = matchedShift?.Id,
                    CheckInTime = checkIn,
                    CheckOutTime = checkOut
                };
                await context.DailyAttendances.AddAsync(daily, cancellationToken);
            }
            else if (!daily.IsManualEdited)
            {
                daily.WorkShiftId = matchedShift?.Id ?? daily.WorkShiftId;
                daily.CheckInTime = checkIn ?? daily.CheckInTime;
                daily.CheckOutTime = checkOut ?? daily.CheckOutTime;
            }

            if (matchedShift != null && !daily.IsManualEdited)
            {
                var statutory = await AttendancePolicyAndShiftEngine.ResolveStatutoryParameterAsync(context, workDate, cancellationToken);
                var policy = await AttendancePolicyAndShiftEngine.ResolveAttendancePolicyAsync(context, empId, workDate, cancellationToken);
                var calc = AttendancePolicyAndShiftEngine.CalculatePunchAttendance(workDate, matchedShift, daily.CheckInTime, daily.CheckOutTime, policy.Policy, statutory);
                daily.AttendanceSymbol = calc.AttendanceSymbol;
                daily.WorkingHours = calc.WorkingHours;
                daily.NightShiftHours = calc.NightShiftHours;
                daily.WorkdayCount = calc.WorkdayCount;
                daily.LateMinutes = calc.LateMinutes;
                daily.EarlyLeaveMinutes = calc.EarlyLeaveMinutes;
                daily.EditReason = $"[{sourceLabel}] {calc.CalculationNotes}";
            }
            else if (!daily.IsManualEdited)
            {
                daily.AttendanceSymbol = "X";
                daily.WorkingHours = 8.0m;
                daily.WorkdayCount = 1.0m;
                daily.EditReason = $"[{sourceLabel}] Đồng bộ tự động";
            }

            touchedMonths.Add((workDate.Year, workDate.Month));
        }

        await context.SaveChangesAsync(cancellationToken);
        foreach (var (yr, mo) in touchedMonths)
        {
            await AttendanceBoardSeeder.RecalculateSummariesInternalAsync(context, mo, yr, cancellationToken);
        }
    }
}

// ============================================================================
// 3. COMMANDS: IMPORT TỪ FILE MÁY CHẤM CÔNG, ÁNH XẠ MÃ, ĐỒNG BỘ HÀNG CHỜ & NHẬP TAY HR
// ============================================================================

public record IngestBiometricFileCommand(string DeviceCode, byte[] FileBytes) : IRequest<DeviceSyncSummaryDto>;

public class IngestBiometricFileCommandHandler : IRequestHandler<IngestBiometricFileCommand, DeviceSyncSummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IAttendanceDeviceConnector _connector;
    private readonly ICurrentUserService _currentUser;

    public IngestBiometricFileCommandHandler(
        IApplicationDbContext context,
        IAttendanceDeviceConnector connector,
        ICurrentUserService currentUser)
    {
        _context = context;
        _connector = connector;
        _currentUser = currentUser;
    }

    public async Task<DeviceSyncSummaryDto> Handle(IngestBiometricFileCommand request, CancellationToken cancellationToken)
    {
        var actor = _currentUser.RequireUsername();
        using var ms = new MemoryStream(request.FileBytes);
        var punches = _connector.ParseRawPunches(ms, request.DeviceCode);
        return await AttendanceDeviceIngestionEngine.IngestDevicePunchesAsync(_context, request.DeviceCode, punches, actor, cancellationToken);
    }
}

public record MapEmployeeDeviceAndReplayCommand(
    int EmployeeId,
    string DeviceUserCode,
    string? DeviceCode = null,
    string? Notes = null) : IRequest<int>;

public class MapEmployeeDeviceAndReplayCommandHandler : IRequestHandler<MapEmployeeDeviceAndReplayCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public MapEmployeeDeviceAndReplayCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(MapEmployeeDeviceAndReplayCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();
        var username = _currentUser.RequireUsername();

        var normUserCode = request.DeviceUserCode.Trim();
        var normDevCode = string.IsNullOrWhiteSpace(request.DeviceCode) ? null : request.DeviceCode.Trim().ToUpperInvariant();

        var existingMapping = await _context.EmployeeDeviceMappings
            .FirstOrDefaultAsync(m => m.DeviceUserCode == normUserCode && m.DeviceCode == normDevCode, cancellationToken);

        if (existingMapping == null)
        {
            await _context.EmployeeDeviceMappings.AddAsync(new EmployeeDeviceMapping
            {
                EmployeeId = request.EmployeeId,
                DeviceCode = normDevCode,
                DeviceUserCode = normUserCode,
                Notes = request.Notes?.Trim(),
                IsActive = true
            }, cancellationToken);
        }
        else
        {
            existingMapping.EmployeeId = request.EmployeeId;
            existingMapping.IsActive = true;
            existingMapping.Notes = request.Notes?.Trim();
        }

        // Tìm toàn bộ log đang nằm hàng chờ "chưa ánh xạ" khớp với mã máy chấm công này để đẩy vào AttendanceLogs
        var pendingQuery = _context.UnmappedAttendanceLogs
            .Where(u => u.Status == "PendingMapping" && u.DeviceUserCode == normUserCode);

        if (!string.IsNullOrEmpty(normDevCode))
        {
            pendingQuery = pendingQuery.Where(u => u.DeviceCode == normDevCode);
        }

        var pendingLogs = await pendingQuery.OrderBy(u => u.PunchTime).ToListAsync(cancellationToken);
        var touchedDailyKeys = new HashSet<(int EmpId, DateOnly WorkDate)>();
        int replayed = 0;

        foreach (var item in pendingLogs)
        {
            var extKey = AttendanceDeviceIngestionEngine.BuildExternalLogKey(item.DeviceCode, item.DeviceUserCode, item.PunchTime, item.PunchType);
            var alreadyExists = await _context.AttendanceLogs
                .AsNoTracking()
                .AnyAsync(l => l.ExternalLogKey == extKey, cancellationToken);

            var resolvedShift = await AttendancePolicyAndShiftEngine.ResolveShiftForPunchAsync(_context, request.EmployeeId, item.PunchTime, cancellationToken);
            var workDate = resolvedShift?.AttributedDate ?? DateOnly.FromDateTime(item.PunchTime);

            if (!alreadyExists)
            {
                await _context.AttendanceLogs.AddAsync(new AttendanceLog
                {
                    EmployeeId = request.EmployeeId,
                    PunchTime = item.PunchTime,
                    PunchType = item.PunchType,
                    SourceType = "BiometricDevice",
                    DeviceId = item.DeviceCode,
                    LocationAddress = $"Đồng bộ lại từ hàng chờ ({item.DeviceUserCode})",
                    GeofenceStatus = "NotApplicable",
                    EnteredByUserId = userId,
                    EnteredByUsername = username,
                    ExternalLogKey = extKey
                }, cancellationToken);
                touchedDailyKeys.Add((request.EmployeeId, workDate));
                replayed++;
            }

            item.Status = "Resolved";
            item.ResolvedEmployeeId = request.EmployeeId;
            item.ResolvedAt = DateTime.UtcNow;
            item.ResolvedBy = username;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await AttendanceDeviceIngestionEngine.RecomputeDailyFromLogsAsync(_context, touchedDailyKeys, "BiometricReplay", cancellationToken);
        return replayed;
    }
}

public record UpsertManualAttendanceLogCommand(
    int EmployeeId,
    DateOnly WorkDate,
    TimeOnly CheckInTime,
    TimeOnly? CheckOutTime,
    string AttendanceSymbol,
    string Reason,
    string? PhotoUrl = null) : IRequest<long>;

public class UpsertManualAttendanceLogCommandHandler : IRequestHandler<UpsertManualAttendanceLogCommand, long>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpsertManualAttendanceLogCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<long> Handle(UpsertManualAttendanceLogCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();
        var username = _currentUser.RequireUsername();

        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new InvalidOperationException("Nhập tay hoặc chỉnh sửa công bởi HR bắt buộc phải nhập lý do.");

        var isLocked = await _context.MonthlyAttendanceSummaries
            .AsNoTracking()
            .AnyAsync(m => m.Month == request.WorkDate.Month && m.Year == request.WorkDate.Year && m.IsLocked, cancellationToken);
        if (isLocked)
            throw new InvalidOperationException($"Bảng công tháng {request.WorkDate.Month:D2}/{request.WorkDate.Year} đã khóa, không thể sửa trực tiếp (cần lập điều chỉnh kỳ sau).");

        var emp = await _context.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy nhân viên.");

        var checkInDt = request.WorkDate.ToDateTime(request.CheckInTime);
        DateTime? checkOutDt = null;
        if (request.CheckOutTime.HasValue)
        {
            var outDate = request.CheckOutTime.Value <= request.CheckInTime ? request.WorkDate.AddDays(1) : request.WorkDate;
            checkOutDt = outDate.ToDateTime(request.CheckOutTime.Value);
        }

        var manualReason = request.Reason.Trim();
        var inLog = new AttendanceLog
        {
            EmployeeId = emp.Id,
            PunchTime = checkInDt,
            PunchType = "CheckIn",
            SourceType = "ManualHr",
            DeviceId = "HR-MANUAL-ENTRY",
            LocationAddress = "Nhập tay / Điều chỉnh bởi HR",
            IpAddress = _currentUser.IpAddress,
            PhotoUrl = request.PhotoUrl,
            EnteredByUserId = userId,
            EnteredByUsername = username,
            ManualReason = manualReason,
            GeofenceStatus = "NotApplicable",
            IsManualOverride = true
        };
        await _context.AttendanceLogs.AddAsync(inLog, cancellationToken);

        if (checkOutDt.HasValue)
        {
            await _context.AttendanceLogs.AddAsync(new AttendanceLog
            {
                EmployeeId = emp.Id,
                PunchTime = checkOutDt.Value,
                PunchType = "CheckOut",
                SourceType = "ManualHr",
                DeviceId = "HR-MANUAL-ENTRY",
                LocationAddress = "Nhập tay / Điều chỉnh bởi HR",
                IpAddress = _currentUser.IpAddress,
                PhotoUrl = request.PhotoUrl,
                EnteredByUserId = userId,
                EnteredByUsername = username,
                ManualReason = manualReason,
                GeofenceStatus = "NotApplicable",
                IsManualOverride = true
            }, cancellationToken);
        }

        var daily = await _context.DailyAttendances
            .FirstOrDefaultAsync(d => d.EmployeeId == emp.Id && d.AttendanceDate == request.WorkDate, cancellationToken);

        var oldSnapshot = daily == null
            ? "null"
            : $"{{\"symbol\":\"{daily.AttendanceSymbol}\",\"in\":\"{daily.CheckInTime:HH:mm}\",\"out\":\"{daily.CheckOutTime:HH:mm}\",\"workday\":{daily.WorkdayCount}}}";

        var symbol = string.IsNullOrWhiteSpace(request.AttendanceSymbol) ? "X" : request.AttendanceSymbol.Trim().ToUpperInvariant();
        decimal workdayValue = symbol switch
        {
            "KL" or "V" or "OFF" or "CGD" => 0m,
            "X/2" or "P/2" => 0.5m,
            _ => 1.0m
        };
        decimal workingHours = checkOutDt.HasValue
            ? Math.Round((decimal)(checkOutDt.Value - checkInDt).TotalHours, 2)
            : workdayValue * 8m;

        if (daily == null)
        {
            daily = new DailyAttendance
            {
                EmployeeId = emp.Id,
                AttendanceDate = request.WorkDate,
                CheckInTime = checkInDt,
                CheckOutTime = checkOutDt,
                AttendanceSymbol = symbol,
                WorkingHours = workingHours,
                WorkdayCount = workdayValue,
                IsManualEdited = true,
                EditReason = $"[HR: {username}] {manualReason}"
            };
            await _context.DailyAttendances.AddAsync(daily, cancellationToken);
        }
        else
        {
            daily.CheckInTime = checkInDt;
            daily.CheckOutTime = checkOutDt ?? daily.CheckOutTime;
            daily.AttendanceSymbol = symbol;
            daily.WorkingHours = workingHours;
            daily.WorkdayCount = workdayValue;
            daily.IsManualEdited = true;
            daily.EditReason = $"[HR: {username}] {manualReason}";
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _context.AuditLogs.AddAsync(new AuditLog
        {
            UserId = userId,
            Username = username,
            ActionType = "ManualAttendanceOverride",
            ModuleName = "TimeAttendance",
            EntityName = "AttendanceLog",
            EntityId = daily.Id.ToString(),
            OldValuesJson = oldSnapshot,
            NewValuesJson = $"{{\"empId\":{emp.Id},\"date\":\"{request.WorkDate:yyyy-MM-dd}\",\"symbol\":\"{symbol}\",\"reason\":\"{manualReason}\"}}",
            IpAddress = _currentUser.IpAddress,
            UserAgent = _currentUser.UserAgent,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await AttendanceBoardSeeder.RecalculateSummariesInternalAsync(_context, request.WorkDate.Month, request.WorkDate.Year, cancellationToken);
        return daily.Id;
    }
}
