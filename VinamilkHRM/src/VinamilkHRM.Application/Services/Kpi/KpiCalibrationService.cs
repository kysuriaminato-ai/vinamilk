using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Application.DTOs.Kpis;
using VinamilkHRM.Application.Interfaces;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.Services;

public class KpiCalibrationService : IKpiCalibrationService
{
    private readonly IApplicationDbContext _context;
    private readonly IKpiService _kpiService;

    public KpiCalibrationService(IApplicationDbContext context, IKpiService kpiService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _kpiService = kpiService ?? throw new ArgumentNullException(nameof(kpiService));
    }

    public async Task<KpiCalibrationSummaryDto> GetCalibrationSummaryAsync(int unitId, string period, CancellationToken ct = default)
    {
        var unit = await _context.OrganizationalUnits
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == unitId, ct)
            ?? throw new DomainException($"Không tìm thấy đơn vị Id = {unitId}.", "ERR_UNIT_NOT_FOUND");

        var sheetsDto = await _kpiService.GetSheetsByUnitAsync(unitId, period, ct);
        int total = sheetsDto.Count;

        int aCount = sheetsDto.Count(s => s.FinalGrade == KpiGrade.A);
        int bCount = sheetsDto.Count(s => s.FinalGrade == KpiGrade.B);
        int cCount = sheetsDto.Count(s => s.FinalGrade == KpiGrade.C);
        int dCount = sheetsDto.Count(s => s.FinalGrade == KpiGrade.D);
        int eCount = sheetsDto.Count(s => s.FinalGrade == KpiGrade.E);
        int unratedCount = sheetsDto.Count(s => s.FinalGrade == KpiGrade.Unrated);

        decimal aPct = total > 0 ? Math.Round((decimal)aCount / total * 100m, 2) : 0m;
        decimal bPct = total > 0 ? Math.Round((decimal)bCount / total * 100m, 2) : 0m;
        decimal cPct = total > 0 ? Math.Round((decimal)cCount / total * 100m, 2) : 0m;
        decimal dPct = total > 0 ? Math.Round((decimal)dCount / total * 100m, 2) : 0m;
        decimal ePct = total > 0 ? Math.Round((decimal)eCount / total * 100m, 2) : 0m;

        // Cảnh báo A > 15% (Chỉ cảnh báo, không ép cứng)
        bool highAWarning = aPct > 15.0m;
        string? highAMsg = highAWarning
            ? $"Cảnh báo: Tỷ lệ xếp loại A đạt {aPct}%, vượt định mức khuyến nghị 15% của Tập đoàn."
            : null;

        // Cảnh báo E = 0% trong 3 kỳ liên tiếp
        var past3Periods = await _context.KpiPeriods
            .AsNoTracking()
            .Where(p => string.Compare(p.Period, period) <= 0)
            .OrderByDescending(p => p.Period)
            .Take(3)
            .Select(p => p.Period)
            .ToListAsync(ct);

        bool zeroEWarning = false;
        string? zeroEMsg = null;

        if (past3Periods.Count == 3)
        {
            var pastSheets = await _context.KpiSheets
                .AsNoTracking()
                .Where(s => s.OrganizationalUnitId == unitId && past3Periods.Contains(s.Period))
                .ToListAsync(ct);

            bool all3PeriodsZeroE = true;
            foreach (var p in past3Periods)
            {
                int countE = pastSheets.Count(s => s.Period == p && s.FinalGrade == KpiGrade.E);
                if (countE > 0)
                {
                    all3PeriodsZeroE = false;
                    break;
                }
            }

            if (all3PeriodsZeroE)
            {
                zeroEWarning = true;
                zeroEMsg = "Cảnh báo: Đơn vị liên tiếp 3 kỳ không có nhân sự xếp loại E (0%). Khuyến nghị xem xét lại tính nghiêm minh trong đánh giá.";
            }
        }

        return new KpiCalibrationSummaryDto
        {
            OrganizationalUnitId = unit.Id,
            OrganizationalUnitName = unit.UnitName,
            Period = period,
            TotalEmployees = total,
            GradeACount = aCount,
            GradeAPercentage = aPct,
            GradeBCount = bCount,
            GradeBPercentage = bPct,
            GradeCCount = cCount,
            GradeCPercentage = cPct,
            GradeDCount = dCount,
            GradeDPercentage = dPct,
            GradeECount = eCount,
            GradeEPercentage = ePct,
            UnratedCount = unratedCount,
            HasHighAWarning = highAWarning,
            HighAWarningMessage = highAMsg,
            HasZeroEWarning = zeroEWarning,
            ZeroEWarningMessage = zeroEMsg,
            Sheets = sheetsDto
        };
    }

    public async Task ApplyCalibrationAsync(List<ApplyCalibrationInput> inputs, string reviewer, CancellationToken ct = default)
    {
        var sheetIds = inputs.Select(i => i.SheetId).ToList();
        var sheets = await _context.KpiSheets
            .Where(s => sheetIds.Contains(s.Id))
            .ToListAsync(ct);

        foreach (var input in inputs)
        {
            var sheet = sheets.FirstOrDefault(s => s.Id == input.SheetId);
            if (sheet != null)
            {
                sheet.ApplyCalibration(
                    input.CalibratedScore,
                    input.CalibratedGrade,
                    input.CalibratedBonusCoefficient,
                    $"{input.CalibrationNotes} (Hiệu chuẩn bởi: {reviewer})");
            }
        }

        await _context.SaveChangesAsync(ct);
    }
}
