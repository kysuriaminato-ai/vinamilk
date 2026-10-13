using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Application.DTOs.Kpis;
using VinamilkHRM.Application.Interfaces;
using VinamilkHRM.Domain.Entities.Kpis;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.Services;

public class KpiMetricService : IKpiMetricService
{
    private readonly IApplicationDbContext _context;

    public KpiMetricService(IApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task RecordMetricAsync(PostKpiMetricRequest request, CancellationToken ct = default)
    {
        var metric = new KpiMetric(
            request.MetricCode,
            request.Period,
            request.Value,
            request.Unit,
            request.Source,
            request.OrganizationalUnitId,
            request.EmployeeId,
            request.BatchReference,
            request.Notes);

        await _context.KpiMetrics.AddAsync(metric, ct);

        // Tự động gán số liệu vào các chỉ tiêu KPI đang thực hiện
        if (request.EmployeeId.HasValue)
        {
            var sheet = await _context.KpiSheets
                .Include(s => s.Items)
                    .ThenInclude(i => i.KpiDefinition)
                .FirstOrDefaultAsync(s => s.EmployeeId == request.EmployeeId.Value && s.Period == request.Period, ct);

            if (sheet != null)
            {
                var matchingItem = sheet.Items.FirstOrDefault(i =>
                    (i.KpiDefinition != null && i.KpiDefinition.Code == request.MetricCode) ||
                    i.KpiDefinition?.DataSource == request.Source);

                if (matchingItem != null)
                {
                    matchingItem.UpdateActualValue(request.Value, DateOnly.FromDateTime(DateTime.UtcNow), null, $"Ghi nhận từ {request.Source}: {request.Notes}");
                }
            }
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task SyncAttendanceMetricsAsync(string period, int? unitId = null, CancellationToken ct = default)
    {
        var query = _context.MonthlyTimesheetSummaries
            .AsNoTracking()
            .Where(m => m.Period == period);

        if (unitId.HasValue)
        {
            query = query.Where(m => m.OrganizationalUnitId == unitId.Value);
        }

        var summaries = await query.ToListAsync(ct);
        if (!summaries.Any())
            return;

        var sheets = await _context.KpiSheets
            .Include(s => s.Items)
                .ThenInclude(i => i.KpiDefinition)
            .Where(s => s.Period == period)
            .ToListAsync(ct);

        foreach (var sum in summaries)
        {
            var sheet = sheets.FirstOrDefault(s => s.EmployeeId == sum.EmployeeId);
            if (sheet != null)
            {
                sheet.SetActualWorkDays(sum.ActualWorkDays);

                var attendItem = sheet.Items.FirstOrDefault(i => i.KpiDefinition?.DataSource == KpiDataSource.Attendance);
                if (attendItem != null)
                {
                    attendItem.UpdateActualValue(
                        sum.ActualWorkDays,
                        DateOnly.FromDateTime(DateTime.UtcNow),
                        null,
                        $"Đồng bộ từ bảng công {period}: Thực tế {sum.ActualWorkDays}/{sum.StandardWorkDays} công chuẩn");
                }
            }
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task ImportMetricsFromExcelAsync(Stream fileStream, string period, CancellationToken ct = default)
    {
        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet == null) return;

        var rows = worksheet.RowsUsed().Skip(1); // Bo qua dong tieu de

        var employees = await _context.Employees
            .AsNoTracking()
            .Select(e => new { e.Id, e.EmployeeCode })
            .ToListAsync(ct);

        var empMap = employees.ToDictionary(e => e.EmployeeCode.Trim().ToUpperInvariant(), e => e.Id);

        foreach (var row in rows)
        {
            string metricCode = row.Cell(1).GetString().Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(metricCode)) continue;

            string empCode = row.Cell(2).GetString().Trim().ToUpperInvariant();
            decimal val = decimal.TryParse(row.Cell(3).GetString(), out var v) ? v : 0m;
            string unit = row.Cell(4).GetString().Trim();
            string sourceStr = row.Cell(5).GetString().Trim();
            string notes = row.Cell(6).GetString().Trim();

            var source = Enum.TryParse<KpiDataSource>(sourceStr, true, out var parsedSource)
                ? parsedSource
                : KpiDataSource.Production;

            int? empId = empMap.TryGetValue(empCode, out var id) ? id : null;

            await RecordMetricAsync(new PostKpiMetricRequest
            {
                MetricCode = metricCode,
                Period = period,
                Value = val,
                Unit = string.IsNullOrWhiteSpace(unit) ? "%" : unit,
                Source = source,
                EmployeeId = empId,
                BatchReference = $"Excel_Import_{DateTime.UtcNow:yyyyMMddHHmmss}",
                Notes = notes
            }, ct);
        }
    }
}
