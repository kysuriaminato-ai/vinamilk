using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Application.DTOs.Attendances;
using VinamilkHRM.Application.Interfaces;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Attendances;
using VinamilkHRM.Domain.Entities.OrganizationalUnits;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.Services;

public class TimeEventService : ITimeEventService
{
    private readonly IApplicationDbContext _context;

    public TimeEventService(IApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<BatchTimeEventsResponse> BatchCreateAsync(BatchTimeEventsRequest request, CancellationToken cancellationToken = default)
    {
        if (request?.Events == null || request.Events.Count == 0)
        {
            return new BatchTimeEventsResponse(0, 0, 0);
        }

        int totalReceived = request.Events.Count;
        int insertedCount = 0;
        int skippedCount = 0;

        // Tải các lần quẹt hiện có của thiết bị để kiểm tra idempotent
        var minTime = request.Events.Min(e => e.Timestamp);
        var maxTime = request.Events.Max(e => e.Timestamp);

        var existingPunches = await _context.TimeEvents
            .Where(t => t.DeviceId == request.DeviceId &&
                        t.Timestamp >= minTime &&
                        t.Timestamp <= maxTime)
            .Select(t => new { t.EmployeeId, t.Timestamp })
            .ToListAsync(cancellationToken);

        var existingSet = new HashSet<(int EmployeeId, DateTimeOffset Timestamp)>(
            existingPunches.Select(p => (p.EmployeeId, p.Timestamp)));

        foreach (var evt in request.Events)
        {
            if (existingSet.Contains((evt.EmployeeId, evt.Timestamp)))
            {
                skippedCount++;
                continue;
            }

            var newEvent = new TimeEvent(
                evt.EmployeeId,
                evt.Timestamp,
                request.DeviceId,
                evt.Method,
                evt.LocationLat,
                evt.LocationLong,
                evt.IpAddress);

            _context.TimeEvents.Add(newEvent);
            existingSet.Add((evt.EmployeeId, evt.Timestamp));
            insertedCount++;
        }

        if (insertedCount > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new BatchTimeEventsResponse(totalReceived, insertedCount, skippedCount);
    }

    public async Task<TimeEventDto> MobilePunchAsync(MobilePunchRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy nhân viên Id={request.EmployeeId}.", "ERR_EMP_NOT_FOUND");

        var unit = await _context.OrganizationalUnits
            .FirstOrDefaultAsync(u => u.Id == request.TargetUnitId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy đơn vị Id={request.TargetUnitId}.", "ERR_UNIT_NOT_FOUND");

        // Tọa độ mẫu của đơn vị (Trụ sở: 10.7297, 106.7219; Cửa hàng / Nhà máy)
        // Mặc định nếu đơn vị chưa cấu hình tọa độ cụ thể thì lấy tọa độ trung tâm cửa hàng
        double unitLat = 10.7297;
        double unitLong = 106.7219;
        double allowedRadiusMeters = 200.0; // Bán kính 200m

        if (unit is RetailStoreUnit)
        {
            allowedRadiusMeters = 150.0; // Cửa hàng bán lẻ bán kính 150m
        }

        double distanceMeters = CalculateDistanceMeters(request.LocationLat, request.LocationLong, unitLat, unitLong);

        if (distanceMeters > allowedRadiusMeters)
        {
            throw new DomainException(
                $"Vị trí chấm công ({distanceMeters:F0}m) nằm ngoài phạm vi cho phép ({allowedRadiusMeters:F0}m) của đơn vị {unit.UnitName}.",
                "ERR_ATT_OUT_OF_GEOFENCE");
        }

        var timeEvent = new TimeEvent(
            request.EmployeeId,
            request.Timestamp,
            deviceId: $"MOBILE_{unit.UnitCode}",
            method: PunchMethod.MobileGps,
            locationLat: request.LocationLat,
            locationLong: request.LocationLong);

        _context.TimeEvents.Add(timeEvent);
        await _context.SaveChangesAsync(cancellationToken);

        return new TimeEventDto(
            timeEvent.Id,
            timeEvent.EmployeeId,
            employee.EmployeeCode,
            employee.FullName,
            timeEvent.Timestamp,
            timeEvent.DeviceId,
            timeEvent.Method,
            timeEvent.LocationLat,
            timeEvent.LocationLong,
            timeEvent.IsProcessed);
    }

    public async Task<BatchTimeEventsResponse> ImportFromExcelAsync(Stream fileStream, CancellationToken cancellationToken = default)
    {
        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheet(1);
        var range = worksheet.RangeUsed();
        if (range == null)
        {
            return new BatchTimeEventsResponse(0, 0, 0);
        }
        var rows = range.RowsUsed().Skip(1); // Bỏ qua header

        var events = new List<TimeEventInputDto>();

        foreach (var row in rows)
        {
            int empId = row.Cell(1).GetValue<int>();
            var timeStr = row.Cell(2).GetString();
            var deviceId = row.Cell(3).GetString();

            if (DateTimeOffset.TryParse(timeStr, out var timestamp))
            {
                events.Add(new TimeEventInputDto(
                    empId,
                    timestamp,
                    string.IsNullOrWhiteSpace(deviceId) ? "EXCEL_IMPORT" : deviceId,
                    PunchMethod.Card));
            }
        }

        return await BatchCreateAsync(new BatchTimeEventsRequest("EXCEL_IMPORT", events), cancellationToken);
    }

    /// <summary>
    /// Tính khoảng cách giữa hai tọa độ GPS theo công thức Haversine (mét).
    /// </summary>
    private static double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double r = 6371000.0; // Bán kính trái đất (mét)
        double dLat = ToRadians(lat2 - lat1);
        double dLon = ToRadians(lon2 - lon1);

        double a = Math.Sin(dLat / 2.0) * Math.Sin(dLat / 2.0) +
                   Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                   Math.Sin(dLon / 2.0) * Math.Sin(dLon / 2.0);

        double c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
        return r * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
