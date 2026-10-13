using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Application.DTOs.Kpis;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Kpis;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.Services;

public partial class KpiService
{
    public async Task<KpiKanbanDto> GetKanbanBoardAsync(string period, int? unitId = null, CancellationToken ct = default)
    {
        var query = _context.KpiSheets
            .AsNoTracking()
            .Include(s => s.Employee)
            .Include(s => s.OrganizationalUnit)
            .Include(s => s.Items)
                .ThenInclude(i => i.ExtensionRequests)
            .Where(s => s.Period == period);

        if (unitId.HasValue)
        {
            query = query.Where(s => s.OrganizationalUnitId == unitId.Value);
        }

        var sheets = await query.ToListAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var soonDeadline = today.AddDays(2);

        var kanban = new KpiKanbanDto { Period = period };

        foreach (var s in sheets)
        {
            var card = new KpiKanbanCardDto
            {
                SheetId = s.Id,
                EmployeeId = s.EmployeeId,
                EmployeeCode = s.Employee?.EmployeeCode ?? string.Empty,
                EmployeeName = s.Employee?.FullName ?? string.Empty,
                UnitName = s.OrganizationalUnit?.UnitName ?? string.Empty,
                TotalItems = s.Items.Count,
                TotalScore = s.TotalScore,
                Grade = s.FinalGrade.ToString(),
                HasPendingExtension = s.Items.Any(i => i.ExtensionRequests.Any(r => r.Status == KpiExtensionStatus.Pending)),
                IsDueSoon = s.Items.Any(i => i.DueDate.HasValue && i.DueDate.Value >= today && i.DueDate.Value <= soonDeadline && !i.CompletedDate.HasValue)
            };

            switch (s.Status)
            {
                case KpiStatus.Draft:
                case KpiStatus.Assigned:
                    kanban.Assigned.Add(card);
                    break;
                case KpiStatus.InProgress:
                    kanban.InProgress.Add(card);
                    break;
                case KpiStatus.SelfReview:
                    kanban.SelfReview.Add(card);
                    break;
                case KpiStatus.ManagerReview:
                    kanban.ManagerReview.Add(card);
                    break;
                case KpiStatus.Calibration:
                    kanban.Calibrated.Add(card);
                    break;
                case KpiStatus.Locked:
                    kanban.Locked.Add(card);
                    break;
            }
        }

        return kanban;
    }

    public async Task<KpiHeatmapDto> GetUnitHeatmapAsync(int unitId, int months = 6, CancellationToken ct = default)
    {
        var unit = await _context.OrganizationalUnits
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == unitId, ct)
            ?? throw new DomainException($"Không tìm thấy đơn vị Id = {unitId}.", "ERR_UNIT_NOT_FOUND");

        var periods = await _context.KpiPeriods
            .AsNoTracking()
            .OrderByDescending(p => p.Period)
            .Take(months)
            .Select(p => p.Period)
            .ToListAsync(ct);

        periods.Reverse();

        var sheets = await _context.KpiSheets
            .AsNoTracking()
            .Where(s => s.OrganizationalUnitId == unitId && periods.Contains(s.Period))
            .ToListAsync(ct);

        var heatmap = new KpiHeatmapDto
        {
            UnitId = unit.Id,
            UnitName = unit.UnitName,
            Periods = periods
        };

        foreach (var p in periods)
        {
            var periodSheets = sheets.Where(s => s.Period == p).ToList();
            int total = periodSheets.Count;
            decimal avgScore = total > 0 ? Math.Round(periodSheets.Average(s => s.TotalScore), 2) : 0m;
            int gradeACount = periodSheets.Count(s => s.FinalGrade == KpiGrade.A);
            decimal gradeAPercent = total > 0 ? Math.Round((decimal)gradeACount / total * 100m, 2) : 0m;
            decimal payoutBudget = periodSheets.Sum(s => s.BonusCoefficient * 10_000_000m); // Dự toán quỹ thưởng

            heatmap.Cells.Add(new KpiHeatmapCellDto
            {
                Period = p,
                TotalEmployees = total,
                AverageScore = avgScore,
                GradeAPercentage = gradeAPercent,
                BonusPayoutBudget = payoutBudget
            });
        }

        return heatmap;
    }

    public async Task<List<KpiTrendDto>> Get12MonthTrendAsync(int? unitId = null, CancellationToken ct = default)
    {
        var recentPeriods = await _context.KpiPeriods
            .AsNoTracking()
            .OrderByDescending(p => p.Period)
            .Take(12)
            .Select(p => p.Period)
            .ToListAsync(ct);

        recentPeriods.Reverse();

        var query = _context.KpiSheets
            .AsNoTracking()
            .Where(s => recentPeriods.Contains(s.Period));

        if (unitId.HasValue)
        {
            query = query.Where(s => s.OrganizationalUnitId == unitId.Value);
        }

        var sheets = await query.ToListAsync(ct);
        var result = new List<KpiTrendDto>();

        foreach (var p in recentPeriods)
        {
            var periodSheets = sheets.Where(s => s.Period == p).ToList();
            int total = periodSheets.Count;
            decimal avgScore = total > 0 ? Math.Round(periodSheets.Average(s => s.TotalScore), 2) : 0m;

            result.Add(new KpiTrendDto
            {
                Period = p,
                AverageScore = avgScore,
                TotalEvaluated = total,
                GradeACount = periodSheets.Count(s => s.FinalGrade == KpiGrade.A),
                GradeBCount = periodSheets.Count(s => s.FinalGrade == KpiGrade.B),
                GradeCCount = periodSheets.Count(s => s.FinalGrade == KpiGrade.C),
                GradeDCount = periodSheets.Count(s => s.FinalGrade == KpiGrade.D),
                GradeECount = periodSheets.Count(s => s.FinalGrade == KpiGrade.E)
            });
        }

        return result;
    }
}
