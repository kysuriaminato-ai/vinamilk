using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Application.DTOs.Kpis;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Kpis;
using VinamilkHRM.Domain.Enums;
using VinamilkHRM.Domain.Services.Kpi;

namespace VinamilkHRM.Application.Services;

public partial class KpiService
{
    public async Task SubmitSelfReviewAsync(int sheetId, SubmitSelfReviewRequest request, CancellationToken ct = default)
    {
        var sheet = await _context.KpiSheets
            .Include(s => s.Items)
            .Include(s => s.KpiPeriod)
            .FirstOrDefaultAsync(s => s.Id == sheetId, ct)
            ?? throw new DomainException($"Không tìm thấy phiếu KPI Id = {sheetId}.", "ERR_KPI_SHEET_NOT_FOUND");

        var period = sheet.KpiPeriod
            ?? await _context.KpiPeriods.FirstOrDefaultAsync(p => p.Period == sheet.Period, ct)
            ?? throw new DomainException($"Không tìm thấy thông tin kỳ {sheet.Period}.", "ERR_PERIOD_NOT_FOUND");

        // Cập nhật giá trị tự chấm và minh chứng cho từng chỉ tiêu
        foreach (var itemReview in request.ItemReviews)
        {
            var item = sheet.Items.FirstOrDefault(i => i.Id == itemReview.ItemId);
            if (item != null)
            {
                item.UpdateActualValue(
                    itemReview.ActualValue,
                    itemReview.CompletedDate,
                    itemReview.EvidenceUrl,
                    itemReview.EvidenceDescription);

                item.SetSelfScore(itemReview.SelfScore);
            }
        }

        // Đệ trình tự đánh giá
        sheet.SubmitSelfReview(DateTime.UtcNow, period.SelfReviewDeadline);
        await _context.SaveChangesAsync(ct);
    }

    public async Task SubmitManagerReviewAsync(int sheetId, int managerId, SubmitManagerReviewRequest request, CancellationToken ct = default)
    {
        var sheet = await _context.KpiSheets
            .Include(s => s.Items)
                .ThenInclude(i => i.KpiDefinition)
                    .ThenInclude(d => d!.Thresholds)
            .Include(s => s.Items)
                .ThenInclude(i => i.ExtensionRequests)
            .Include(s => s.KpiPeriod)
            .Include(s => s.OrganizationalUnit)
            .FirstOrDefaultAsync(s => s.Id == sheetId, ct)
            ?? throw new DomainException($"Không tìm thấy phiếu KPI Id = {sheetId}.", "ERR_KPI_SHEET_NOT_FOUND");

        var period = sheet.KpiPeriod
            ?? await _context.KpiPeriods.FirstOrDefaultAsync(p => p.Period == sheet.Period, ct)
            ?? throw new DomainException($"Không tìm thấy thông tin kỳ {sheet.Period}.", "ERR_PERIOD_NOT_FOUND");

        // Đếm số cấp dưới của quản lý trong kỳ để phạt trễ nếu quá hạn
        int subordinateCount = await _context.KpiSheets
            .CountAsync(s => s.EvaluatorId == managerId && s.Period == sheet.Period, ct);

        // Áp dụng điểm chấm & điều chỉnh của quản lý cho từng chỉ tiêu
        foreach (var eval in request.ItemEvaluations)
        {
            var item = sheet.Items.FirstOrDefault(i => i.Id == eval.ItemId);
            if (item != null)
            {
                item.SetManagerEvaluation(eval.ManagerScore, eval.Adjustment, eval.AdjustmentReason);
            }
        }

        // Chuyển trạng thái phiếu sang thẩm định
        sheet.SubmitManagerReview(managerId, DateTime.UtcNow, period.ManagerReviewDeadline, subordinateCount, request.Feedback);

        // Lấy WorkPattern của đơn vị
        var workPattern = sheet.OrganizationalUnit?.WorkPattern ?? WorkPattern.OfficeWeek;

        // Tính điểm chỉ tiêu chi tiết
        foreach (var item in sheet.Items)
        {
            bool isExtended = item.ExtendedDueDate.HasValue || item.ExtensionRequests.Any(r => r.Status == KpiExtensionStatus.Approved);

            decimal finalScore = KpiCalculationEngine.CalculateItemFinalScore(
                method: item.ScoringMethod,
                target: item.TargetValue,
                actual: item.ActualValue,
                dueDate: item.DueDate,
                extendedDueDate: item.ExtendedDueDate,
                completedDate: item.CompletedDate,
                isExtended: isExtended,
                adjustment: item.Adjustment,
                workPattern: workPattern,
                calendar: _workCalendar,
                isPeriodLocked: false,
                thresholds: item.KpiDefinition?.Thresholds);

            item.SetFinalScore(finalScore);
        }

        // Tính điểm tổng phiếu và xác định xếp loại + hệ số thưởng
        var (totalScore, unitScore, indScore, peopleScore, compScore) = KpiCalculationEngine.CalculateSheetTotalScore(
            sheet.Items,
            sheet.SelfReviewLatePenalty,
            sheet.ManagerReviewLatePenalty);

        var (grade, bonusCoeff) = KpiCalculationEngine.DetermineGradeAndBonus(
            totalScore,
            sheet.LeadershipLevel,
            sheet.HasSevereViolation,
            sheet.IsStandardWorkingDaysMet);

        sheet.SetCalculatedScores(unitScore, indScore, peopleScore, compScore, totalScore, grade, bonusCoeff);

        await _context.SaveChangesAsync(ct);
    }
}
