using Xunit;
using VinamilkHRM.Domain.Entities.Kpis;
using VinamilkHRM.Domain.Enums;
using VinamilkHRM.Domain.Services.Kpi;

namespace VinamilkHRM.Domain.Tests.Kpis;

public class KpiCalculationEngineTests
{
    private readonly IWorkCalendar _calendar = new DefaultWorkCalendar();

    [Fact]
    public void CalculateCompletionRate_HigherIsBetter_ReturnsCorrectRate()
    {
        // 100 muc tieu, thuc hien 110 -> 110%
        decimal rate1 = KpiCalculationEngine.CalculateCompletionRate(KpiScoringMethod.HigherIsBetter, 100m, 110m);
        Assert.Equal(110.0m, rate1);

        // 100 muc tieu, thuc hien 40 -> 40%
        decimal rate2 = KpiCalculationEngine.CalculateCompletionRate(KpiScoringMethod.HigherIsBetter, 100m, 40m);
        Assert.Equal(40.0m, rate2);
    }

    [Fact]
    public void CalculateCompletionRate_LowerIsBetter_Returns120WhenActualIsZero()
    {
        // Quyết định quy chế: Nếu thực hiện = 0 (ví dụ 0 lỗi vi phạm) -> 120 điểm
        decimal rate = KpiCalculationEngine.CalculateCompletionRate(KpiScoringMethod.LowerIsBetter, 10m, 0m);
        Assert.Equal(120.0m, rate);
    }

    [Fact]
    public void CalculateCompletionRate_LowerIsBetter_CalculatesInvertedRatio()
    {
        // Muc tieu toi da 10, thuc hien 8 -> (10/8)*100 = 125%
        decimal rate = KpiCalculationEngine.CalculateCompletionRate(KpiScoringMethod.LowerIsBetter, 10m, 8m);
        Assert.Equal(125.0m, rate);
    }

    [Fact]
    public void CalculateCompletionRate_Binary_Returns100OrZero()
    {
        decimal ratePass = KpiCalculationEngine.CalculateCompletionRate(KpiScoringMethod.Binary, 1m, 1m);
        Assert.Equal(100.0m, ratePass);

        decimal rateFail = KpiCalculationEngine.CalculateCompletionRate(KpiScoringMethod.Binary, 1m, 0m);
        Assert.Equal(0.0m, rateFail);
    }

    [Fact]
    public void CalculateCompletionRate_ThresholdTable_MatchesRange()
    {
        var thresholds = new List<KpiThreshold>
        {
            new KpiThreshold(1, 0m, 70m, 0m),
            new KpiThreshold(1, 70.01m, 90m, 80m),
            new KpiThreshold(1, 90.01m, 100m, 100m),
            new KpiThreshold(1, 100.01m, 120m, 120m)
        };

        decimal score85 = KpiCalculationEngine.CalculateCompletionRate(KpiScoringMethod.ThresholdTable, 100m, 85m, thresholds);
        Assert.Equal(80.0m, score85);

        decimal score95 = KpiCalculationEngine.CalculateCompletionRate(KpiScoringMethod.ThresholdTable, 100m, 95m, thresholds);
        Assert.Equal(100.0m, score95);
    }

    [Fact]
    public void CalculateItemFinalScore_CompletionRateBelow50_YieldsZeroScore()
    {
        // TL = 45% (< 50%) -> Điểm chỉ tiêu = 0
        decimal score = KpiCalculationEngine.CalculateItemFinalScore(
            method: KpiScoringMethod.HigherIsBetter,
            target: 100m,
            actual: 45m,
            dueDate: null,
            extendedDueDate: null,
            completedDate: null,
            isExtended: false,
            adjustment: 0m,
            workPattern: WorkPattern.OfficeWeek,
            calendar: _calendar,
            isPeriodLocked: false);

        Assert.Equal(0.0m, score);
    }

    [Fact]
    public void CalculateItemFinalScore_LateDays1To5_Deducts5PercentPerWorkingDay()
    {
        // DueDate: 2026-10-12 (T2), Completed: 2026-10-14 (T4) -> trễ 2 ngày làm việc (T3, T4)
        // Trừ 2 * 5% = 10% -> factor = 0.90 -> TL = 100 * 0.90 = 90
        var due = new DateOnly(2026, 10, 12);
        var completed = new DateOnly(2026, 10, 14);

        decimal score = KpiCalculationEngine.CalculateItemFinalScore(
            method: KpiScoringMethod.HigherIsBetter,
            target: 100m,
            actual: 100m,
            dueDate: due,
            extendedDueDate: null,
            completedDate: completed,
            isExtended: false,
            adjustment: 0m,
            workPattern: WorkPattern.OfficeWeek,
            calendar: _calendar,
            isPeriodLocked: false);

        Assert.Equal(90.0m, score);
    }

    [Fact]
    public void CalculateItemFinalScore_LateDays6To10_Deducts10PercentPerWorkingDayForDays6Plus()
    {
        // Trễ 7 ngày làm việc:
        // 5 ngày đầu = 25% + 2 ngày tiếp (6,7) * 10% = 45% -> factor = 0.55 -> điểm = 100 * 0.55 = 55
        // Giả sử DueDate: 2026-10-01 (T5).
        // Ngày làm việc: T6 (02), T2 (05), T3 (06), T4 (07), T5 (08), T6 (09), T2 (12) -> 7 ngày làm việc
        var due = new DateOnly(2026, 10, 1);
        var completed = new DateOnly(2026, 10, 12);

        decimal score = KpiCalculationEngine.CalculateItemFinalScore(
            method: KpiScoringMethod.HigherIsBetter,
            target: 100m,
            actual: 100m,
            dueDate: due,
            extendedDueDate: null,
            completedDate: completed,
            isExtended: false,
            adjustment: 0m,
            workPattern: WorkPattern.OfficeWeek,
            calendar: _calendar,
            isPeriodLocked: false);

        Assert.Equal(55.0m, score);
    }

    [Fact]
    public void CalculateItemFinalScore_LateMoreThan10Days_YieldsZero()
    {
        // Trễ > 10 ngày làm việc -> trừ 100% (0 điểm)
        var due = new DateOnly(2026, 9, 1);
        var completed = new DateOnly(2026, 10, 1);

        decimal score = KpiCalculationEngine.CalculateItemFinalScore(
            method: KpiScoringMethod.HigherIsBetter,
            target: 100m,
            actual: 100m,
            dueDate: due,
            extendedDueDate: null,
            completedDate: completed,
            isExtended: false,
            adjustment: 0m,
            workPattern: WorkPattern.OfficeWeek,
            calendar: _calendar,
            isPeriodLocked: false);

        Assert.Equal(0.0m, score);
    }

    [Fact]
    public void CalculateItemFinalScore_UnfinishedWhenPeriodLocked_YieldsZero()
    {
        // Task chưa xong khi khóa kỳ -> 0 điểm
        var due = new DateOnly(2026, 10, 15);

        decimal score = KpiCalculationEngine.CalculateItemFinalScore(
            method: KpiScoringMethod.Task,
            target: 100m,
            actual: 100m,
            dueDate: due,
            extendedDueDate: null,
            completedDate: null,
            isExtended: false,
            adjustment: 0m,
            workPattern: WorkPattern.OfficeWeek,
            calendar: _calendar,
            isPeriodLocked: true);

        Assert.Equal(0.0m, score);
    }

    [Fact]
    public void CalculateItemFinalScore_EarlyBonus_Gives2PercentPerDayCappedAt10Percent()
    {
        // Sớm 3 ngày làm việc: T2 (12) -> hoàn thành T4 tuần trước (07).
        // Ngày làm việc sớm: T5 (08), T6 (09), T2 (12) -> 3 ngày làm việc
        // Thưởng 3 * 2% = 6% -> factor = 1.06 -> điểm = 100 * 1.06 = 106
        var due = new DateOnly(2026, 10, 12);
        var completed = new DateOnly(2026, 10, 7);

        decimal score = KpiCalculationEngine.CalculateItemFinalScore(
            method: KpiScoringMethod.HigherIsBetter,
            target: 100m,
            actual: 100m,
            dueDate: due,
            extendedDueDate: null,
            completedDate: completed,
            isExtended: false,
            adjustment: 0m,
            workPattern: WorkPattern.OfficeWeek,
            calendar: _calendar,
            isPeriodLocked: false);

        Assert.Equal(106.0m, score);
    }

    [Fact]
    public void CalculateItemFinalScore_ExtendedItem_DoesNotReceiveEarlyBonus()
    {
        // Ràng buộc quy chế: Chỉ tiêu đã được gia hạn (isExtended = true) thì KHÔNG được cộng điểm sớm!
        var due = new DateOnly(2026, 10, 12);
        var completed = new DateOnly(2026, 10, 7);

        decimal score = KpiCalculationEngine.CalculateItemFinalScore(
            method: KpiScoringMethod.HigherIsBetter,
            target: 100m,
            actual: 100m,
            dueDate: due,
            extendedDueDate: new DateOnly(2026, 10, 15),
            completedDate: completed,
            isExtended: true, // Đã xin gia hạn
            adjustment: 0m,
            workPattern: WorkPattern.OfficeWeek,
            calendar: _calendar,
            isPeriodLocked: false);

        // Vẫn giữ nguyên 100, không được cộng thêm 6% hay 10%
        Assert.Equal(100.0m, score);
    }

    [Fact]
    public void CalculateItemFinalScore_RespectsMaxScoreCapOf120()
    {
        // TL = 120, Thưởng sớm 10% = 132 -> Bị chặn trần 120 điểm
        decimal score = KpiCalculationEngine.CalculateItemFinalScore(
            method: KpiScoringMethod.HigherIsBetter,
            target: 100m,
            actual: 130m,
            dueDate: null,
            extendedDueDate: null,
            completedDate: null,
            isExtended: false,
            adjustment: 5m,
            workPattern: WorkPattern.OfficeWeek,
            calendar: _calendar,
            isPeriodLocked: false);

        Assert.Equal(120.0m, score);
    }

    [Fact]
    public void DetermineGradeAndBonus_RespectsScoreRangesAndCoefficients()
    {
        // A >= 110 (1.2)
        var (gradeA, coeffA) = KpiCalculationEngine.DetermineGradeAndBonus(112m, LeadershipLevel.None, false, true);
        Assert.Equal(KpiGrade.A, gradeA);
        Assert.Equal(1.2m, coeffA);

        // B [95, 110) (1.0)
        var (gradeB, coeffB) = KpiCalculationEngine.DetermineGradeAndBonus(102m, LeadershipLevel.None, false, true);
        Assert.Equal(KpiGrade.B, gradeB);
        Assert.Equal(1.0m, coeffB);

        // C [80, 95) (0.8)
        var (gradeC, coeffC) = KpiCalculationEngine.DetermineGradeAndBonus(88m, LeadershipLevel.None, false, true);
        Assert.Equal(KpiGrade.C, gradeC);
        Assert.Equal(0.8m, coeffC);

        // D [60, 80) (0.5)
        var (gradeD, coeffD) = KpiCalculationEngine.DetermineGradeAndBonus(65m, LeadershipLevel.None, false, true);
        Assert.Equal(KpiGrade.D, gradeD);
        Assert.Equal(0.5m, coeffD);

        // E < 60 (0.0)
        var (gradeE, coeffE) = KpiCalculationEngine.DetermineGradeAndBonus(52m, LeadershipLevel.None, false, true);
        Assert.Equal(KpiGrade.E, gradeE);
        Assert.Equal(0.0m, coeffE);
    }

    [Fact]
    public void DetermineGradeAndBonus_DirectorSevereViolation_GivesZeroBonus()
    {
        // Director điểm A (115) nhưng có vi phạm an toàn/kỷ luật nghiêm trọng (Cổng Compliance) -> Bonus = 0
        var (grade, coeff) = KpiCalculationEngine.DetermineGradeAndBonus(115m, LeadershipLevel.Director, true, true);
        Assert.Equal(KpiGrade.A, grade);
        Assert.Equal(0.0m, coeff);
    }

    [Fact]
    public void DetermineGradeAndBonus_WorkingDaysBelow50Percent_ReturnsUnrated()
    {
        // Làm việc < 50% công chuẩn -> Không xếp loại (Unrated), Bonus = 0
        var (grade, coeff) = KpiCalculationEngine.DetermineGradeAndBonus(100m, LeadershipLevel.None, false, false);
        Assert.Equal(KpiGrade.Unrated, grade);
        Assert.Equal(0.0m, coeff);
    }

    [Fact]
    public void CalculateSheetTotalScore_AppliesProcessPenalties()
    {
        var item1 = new KpiItem(1, KpiGroup.IndividualWork, "Task A", "%", KpiScoringMethod.HigherIsBetter, 100m, 50m);
        item1.SetFinalScore(100m); // 50% * 100 = 50 diem

        var item2 = new KpiItem(1, KpiGroup.IndividualWork, "Task B", "%", KpiScoringMethod.HigherIsBetter, 100m, 50m);
        item2.SetFinalScore(100m); // 50% * 100 = 50 diem

        // Nhân viên nộp tự đánh giá trễ (-2 điểm tổng)
        // Quản lý chấm trễ 3 cấp dưới (-3 điểm)
        var (total, _, ind, people, _) = KpiCalculationEngine.CalculateSheetTotalScore(
            new[] { item1, item2 },
            selfReviewPenalty: 2.0m,
            managerReviewPenalty: 3.0m);

        // Raw total = 100 -> trừ 2 điểm tự đánh giá trễ = 98 điểm
        Assert.Equal(98.0m, total);
    }
}
