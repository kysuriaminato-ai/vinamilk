using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Application.DTOs.Kpis;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Kpis;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.Services;

public partial class KpiService
{
    public async Task<List<KpiPeriodDto>> GetPeriodsAsync(CancellationToken ct = default)
    {
        var periods = await _context.KpiPeriods
            .AsNoTracking()
            .Include(p => p.Sheets)
            .OrderByDescending(p => p.Period)
            .ToListAsync(ct);

        return periods.Select(p => new KpiPeriodDto
        {
            Id = p.Id,
            Period = p.Period,
            Year = p.Year,
            Month = p.Month,
            AssignStartDate = p.AssignStartDate,
            AssignDeadline = p.AssignDeadline,
            MidTermDate = p.MidTermDate,
            SelfReviewDeadline = p.SelfReviewDeadline,
            ManagerReviewDeadline = p.ManagerReviewDeadline,
            CalibrationDeadline = p.CalibrationDeadline,
            Status = p.Status,
            ClosedAt = p.ClosedAt,
            ClosedBy = p.ClosedBy,
            TotalSheetsCount = p.Sheets.Count
        }).ToList();
    }

    public async Task<KpiPeriodDto> GetPeriodByCodeAsync(string period, CancellationToken ct = default)
    {
        var p = await _context.KpiPeriods
            .AsNoTracking()
            .Include(x => x.Sheets)
            .FirstOrDefaultAsync(x => x.Period == period, ct)
            ?? throw new DomainException($"Không tìm thấy kỳ đánh giá KPI {period}.", "ERR_KPI_PERIOD_NOT_FOUND");

        return new KpiPeriodDto
        {
            Id = p.Id,
            Period = p.Period,
            Year = p.Year,
            Month = p.Month,
            AssignStartDate = p.AssignStartDate,
            AssignDeadline = p.AssignDeadline,
            MidTermDate = p.MidTermDate,
            SelfReviewDeadline = p.SelfReviewDeadline,
            ManagerReviewDeadline = p.ManagerReviewDeadline,
            CalibrationDeadline = p.CalibrationDeadline,
            Status = p.Status,
            ClosedAt = p.ClosedAt,
            ClosedBy = p.ClosedBy,
            TotalSheetsCount = p.Sheets.Count
        };
    }

    public async Task<KpiPeriodDto> CreatePeriodAsync(int year, int month, CancellationToken ct = default)
    {
        string code = $"{year:D4}-{month:D2}";
        if (await _context.KpiPeriods.AnyAsync(p => p.Period == code, ct))
            throw new DomainException($"Kỳ đánh giá {code} đã tồn tại trong hệ thống.", "ERR_KPI_PERIOD_EXISTS");

        var period = KpiPeriod.CreateDefault(year, month);
        await _context.KpiPeriods.AddAsync(period, ct);
        await _context.SaveChangesAsync(ct);

        return await GetPeriodByCodeAsync(code, ct);
    }

    public async Task LockPeriodAsync(string period, string lockedBy, CancellationToken ct = default)
    {
        var p = await _context.KpiPeriods
            .Include(x => x.Sheets)
                .ThenInclude(s => s.Items)
            .FirstOrDefaultAsync(x => x.Period == period, ct)
            ?? throw new DomainException($"Không tìm thấy kỳ đánh giá KPI {period}.", "ERR_KPI_PERIOD_NOT_FOUND");

        if (p.Status == KpiPeriodStatus.Locked)
            throw new DomainException($"Kỳ {period} đã được khóa trước đó.", "ERR_KPI_PERIOD_ALREADY_LOCKED");

        p.LockPeriod(lockedBy);

        var existingResults = await _context.KpiResults
            .Where(r => r.Period == period)
            .ToListAsync(ct);

        foreach (var sheet in p.Sheets)
        {
            if (sheet.Status != KpiStatus.Locked)
            {
                var kpiResult = sheet.Lock(lockedBy);

                var existingResult = existingResults.FirstOrDefault(r => r.EmployeeId == sheet.EmployeeId);
                if (existingResult == null)
                {
                    await _context.KpiResults.AddAsync(kpiResult, ct);
                }
            }
        }

        await _context.SaveChangesAsync(ct);
    }
}
