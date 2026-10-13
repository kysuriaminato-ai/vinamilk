using VinamilkHRM.Domain.Entities.Kpis;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Services.Kpi;

/// <summary>
/// Tham số động cơ tính điểm KPI nạp từ cấu hình (không hard-code).
/// </summary>
public record KpiEngineParameters
{
    public decimal MinCompletionRateThreshold { get; init; } = 50.0m; // TL < 50% -> 0 diem
    public decimal MaxItemScore { get; init; } = 120.0m;              // Tran diem 120
    public decimal MaxEarlyBonusPercent { get; init; } = 0.10m;       // Thuong som toi da 10%
    public decimal EarlyBonusRatePerDay { get; init; } = 0.02m;       // 2%/ngay
    public decimal LatePenaltyDays1To5PerDay { get; init; } = 0.05m;  // 5%/ngay cho 1-5 ngay
    public decimal LatePenaltyDays6To10PerDay { get; init; } = 0.10m; // 10%/ngay cho 6-10 ngay
    public int MaxLateDaysAllowed { get; init; } = 10;                // > 10 ngay -> 0 diem
    public decimal SelfReviewLatePenalty { get; init; } = 2.0m;       // -2 diem tong neu tu danh gia tre
    public decimal ManagerReviewLatePenaltyPerPerson { get; init; } = 1.0m; // -1 diem/cap duoi
    public decimal MaxManagerReviewLatePenalty { get; init; } = 10.0m;      // Toi da -10 diem
    public decimal MinStandardWorkDaysRatio { get; init; } = 0.50m;   // < 50% cong chuan -> Unrated

    // Nguong xep loai
    public decimal GradeAMinScore { get; init; } = 110.0m;
    public decimal GradeBMinScore { get; init; } = 95.0m;
    public decimal GradeCMinScore { get; init; } = 80.0m;
    public decimal GradeDMinScore { get; init; } = 60.0m;

    // He so thuong
    public decimal BonusCoeffA { get; init; } = 1.2m;
    public decimal BonusCoeffB { get; init; } = 1.0m;
    public decimal BonusCoeffC { get; init; } = 0.8m;
    public decimal BonusCoeffD { get; init; } = 0.5m;
    public decimal BonusCoeffE { get; init; } = 0.0m;
}

/// <summary>
/// Động cơ tính điểm hiệu suất công việc (KPI Calculation Engine).
/// Hoàn toàn thuần Domain (Pure C#), 0% phụ thuộc CSDL, EF Core hay ASP.NET.
/// </summary>
public static class KpiCalculationEngine
{
    private static readonly KpiEngineParameters DefaultParams = new();

    /// <summary>
    /// 1. Tính Tỷ lệ hoàn thành (TL) theo từng phương pháp tính điểm.
    /// </summary>
    public static decimal CalculateCompletionRate(
        KpiScoringMethod method,
        decimal target,
        decimal actual,
        IEnumerable<KpiThreshold>? thresholds = null)
    {
        return method switch
        {
            KpiScoringMethod.HigherIsBetter => CalculateHigherIsBetter(target, actual),
            KpiScoringMethod.LowerIsBetter => CalculateLowerIsBetter(target, actual),
            KpiScoringMethod.Binary => CalculateBinary(target, actual),
            KpiScoringMethod.ThresholdTable => CalculateThresholdTable(actual, thresholds),
            KpiScoringMethod.Task => CalculateTask(target, actual),
            _ => CalculateHigherIsBetter(target, actual)
        };
    }

    private static decimal CalculateHigherIsBetter(decimal target, decimal actual)
    {
        if (target <= 0m)
            return actual > 0m ? 120.0m : 0.0m;

        decimal rate = (actual / target) * 100.0m;
        return Math.Round(rate, 2);
    }

    private static decimal CalculateLowerIsBetter(decimal target, decimal actual)
    {
        // Theo quy chế: Thực hiện = 0 thì 120 điểm
        if (actual <= 0m)
            return 120.0m;

        if (target <= 0m)
            return 0.0m;

        decimal rate = (target / actual) * 100.0m;
        return Math.Round(rate, 2);
    }

    private static decimal CalculateBinary(decimal target, decimal actual)
    {
        // Binary 100 / 0
        return actual >= target && target > 0m || target == 0m && actual >= 1m
            ? 100.0m
            : 0.0m;
    }

    private static decimal CalculateThresholdTable(decimal actual, IEnumerable<KpiThreshold>? thresholds)
    {
        if (thresholds == null || !thresholds.Any())
            return 0.0m;

        var matched = thresholds.FirstOrDefault(t => actual >= t.MinThreshold && actual <= t.MaxThreshold);
        if (matched != null)
            return matched.Score;

        // Nếu vượt trần cao nhất
        var maxThreshold = thresholds.OrderByDescending(t => t.MaxThreshold).First();
        if (actual > maxThreshold.MaxThreshold)
            return maxThreshold.Score;

        // Nếu dưới sàn thấp nhất
        var minThreshold = thresholds.OrderBy(t => t.MinThreshold).First();
        if (actual < minThreshold.MinThreshold)
            return 0.0m;

        return 0.0m;
    }

    private static decimal CalculateTask(decimal target, decimal actual)
    {
        if (target <= 0m)
            return actual >= 1.0m ? 100.0m : 0.0m;

        decimal rate = (actual / target) * 100.0m;
        return Math.Round(rate, 2);
    }

    /// <summary>
    /// 2. Tính hệ số thưởng/phạt tiến độ (trễ hạn / sớm hạn) theo ngày làm việc.
    /// </summary>
    public static (decimal latePenalty, decimal earlyBonus, decimal timelineFactor) CalculateTimelineFactor(
        DateOnly? effectiveDueDate,
        DateOnly? completedDate,
        bool isExtended,
        decimal completionRate,
        WorkPattern workPattern,
        IWorkCalendar calendar,
        bool isPeriodLocked,
        KpiEngineParameters? p = null)
    {
        var parameters = p ?? DefaultParams;

        if (!effectiveDueDate.HasValue)
            return (0m, 0m, 1.0m);

        // Trường hợp Task chưa xong khi khóa kỳ hoặc không có CompletedDate khi đã khóa
        if (!completedDate.HasValue)
        {
            if (isPeriodLocked)
            {
                // Task chưa xong khi khóa kỳ -> phạt 100% (điểm về 0)
                return (1.0m, 0m, 0m);
            }
            return (0m, 0m, 1.0m);
        }

        var dueDate = effectiveDueDate.Value;
        var finishDate = completedDate.Value;

        if (finishDate > dueDate)
        {
            // Trễ hạn: đếm số ngày làm việc trễ
            int lateDays = calendar.CountWorkingDaysBetween(dueDate.AddDays(1), finishDate, workPattern);
            if (lateDays <= 0)
                return (0m, 0m, 1.0m);

            if (lateDays > parameters.MaxLateDaysAllowed)
            {
                // > 10 ngày làm việc trễ -> trừ 100% (về 0 điểm)
                return (1.0m, 0m, 0m);
            }

            decimal latePenalty;
            if (lateDays <= 5)
            {
                // Ngày 1–5: trừ 5%/ngày
                latePenalty = lateDays * parameters.LatePenaltyDays1To5PerDay;
            }
            else
            {
                // Ngày 6–10: 5 ngày đầu (25%) + (lateDays - 5) * 10%/ngày
                latePenalty = (5 * parameters.LatePenaltyDays1To5PerDay) + ((lateDays - 5) * parameters.LatePenaltyDays6To10PerDay);
            }

            decimal factor = Math.Max(0m, 1.0m - latePenalty);
            return (latePenalty, 0m, factor);
        }
        else if (finishDate < dueDate)
        {
            // Sớm hạn: Sớm hạn và TL >= 100 thì cộng 2%/ngày, tối đa 10%
            // Ràng buộc: chỉ tiêu đã được gia hạn không được cộng điểm sớm!
            if (isExtended || completionRate < 100.0m)
            {
                return (0m, 0m, 1.0m);
            }

            int earlyDays = calendar.CountWorkingDaysBetween(finishDate, dueDate.AddDays(-1), workPattern);
            if (earlyDays <= 0)
                return (0m, 0m, 1.0m);

            decimal earlyBonus = Math.Min(parameters.MaxEarlyBonusPercent, earlyDays * parameters.EarlyBonusRatePerDay);
            decimal factor = 1.0m + earlyBonus;
            return (0m, earlyBonus, factor);
        }

        // Hoàn thành đúng ngày DueDate
        return (0m, 0m, 1.0m);
    }

    /// <summary>
    /// 3. Tính điểm chỉ tiêu cụ thể:
    /// Điểm chỉ tiêu = min(120, TL * (1 - trễ + sớm)) + Điều chỉnh (±10).
    /// TL < 50 -> 0 điểm.
    /// </summary>
    public static decimal CalculateItemFinalScore(
        KpiScoringMethod method,
        decimal target,
        decimal actual,
        DateOnly? dueDate,
        DateOnly? extendedDueDate,
        DateOnly? completedDate,
        bool isExtended,
        decimal adjustment,
        WorkPattern workPattern,
        IWorkCalendar calendar,
        bool isPeriodLocked,
        IEnumerable<KpiThreshold>? thresholds = null,
        KpiEngineParameters? p = null)
    {
        var parameters = p ?? DefaultParams;

        decimal tl = CalculateCompletionRate(method, target, actual, thresholds);

        // Quy tắc: TL < 50 -> điểm chỉ tiêu = 0
        if (tl < parameters.MinCompletionRateThreshold)
        {
            decimal adjustedScoreIfBelowMin = Math.Clamp(0m + adjustment, 0m, parameters.MaxItemScore);
            return adjustedScoreIfBelowMin;
        }

        var effectiveDue = extendedDueDate ?? dueDate;
        var (_, _, timelineFactor) = CalculateTimelineFactor(
            effectiveDue,
            completedDate,
            isExtended,
            tl,
            workPattern,
            calendar,
            isPeriodLocked,
            parameters);

        decimal scoreAfterTimeline = tl * timelineFactor;
        decimal clampedScore = Math.Min(parameters.MaxItemScore, scoreAfterTimeline);

        // Cộng điểm điều chỉnh của lãnh đạo (±10)
        decimal finalScore = Math.Clamp(clampedScore + adjustment, 0m, parameters.MaxItemScore);
        return Math.Round(finalScore, 2);
    }

    /// <summary>
    /// 4. Tính điểm tổng kết phiếu KPI tháng (KPI tháng = Σ trọng số × điểm - phạt quy trình).
    /// </summary>
    public static (decimal totalScore, decimal unitScore, decimal individualScore, decimal peopleScore, decimal complianceScore) CalculateSheetTotalScore(
        IEnumerable<KpiItem> items,
        decimal selfReviewPenalty,
        decimal managerReviewPenalty)
    {
        decimal unitWeighted = 0m;
        decimal individualWeighted = 0m;
        decimal peopleWeighted = 0m;
        decimal complianceWeighted = 0m;

        foreach (var item in items)
        {
            decimal itemContribution = (item.WeightPercentage / 100.0m) * item.FinalScore;
            switch (item.Group)
            {
                case KpiGroup.UnitResult:
                    unitWeighted += itemContribution;
                    break;
                case KpiGroup.IndividualWork:
                    individualWeighted += itemContribution;
                    break;
                case KpiGroup.PeopleManagement:
                    peopleWeighted += itemContribution;
                    break;
                case KpiGroup.Compliance:
                    complianceWeighted += itemContribution;
                    break;
            }
        }

        // Lãnh đạo chấm trễ phạt tối đa -10 điểm ghi vào nhóm PeopleManagement
        peopleWeighted = Math.Max(0m, peopleWeighted - managerReviewPenalty);

        decimal rawTotal = unitWeighted + individualWeighted + peopleWeighted + complianceWeighted;

        // Trừ phạt tự đánh giá trễ (-2 điểm tổng)
        decimal finalTotal = Math.Max(0m, rawTotal - selfReviewPenalty);
        finalTotal = Math.Min(120.0m, Math.Round(finalTotal, 2));

        return (finalTotal,
                Math.Round(unitWeighted, 2),
                Math.Round(individualWeighted, 2),
                Math.Round(peopleWeighted, 2),
                Math.Round(complianceWeighted, 2));
    }

    /// <summary>
    /// 5. Xác định Xếp loại (Grade) và Hệ số thưởng (BonusCoefficient).
    /// </summary>
    public static (KpiGrade grade, decimal bonusCoefficient) DetermineGradeAndBonus(
        decimal totalScore,
        LeadershipLevel level,
        bool hasSevereViolation,
        bool meetsStandardWorkDays,
        KpiEngineParameters? p = null)
    {
        var parameters = p ?? DefaultParams;

        // Điều kiện 1: < 50% công chuẩn trong kỳ -> Không xếp loại (Unrated), P3 tính theo quy chế
        if (!meetsStandardWorkDays)
        {
            return (KpiGrade.Unrated, 0.0m);
        }

        // Điều kiện 2: Cổng tuân thủ của Director (hoặc Executive)
        // Nếu có vi phạm an toàn/kỷ luật nghiêm trọng trong kỳ -> hệ số thưởng 0
        if ((level == LeadershipLevel.Director || level == LeadershipLevel.Executive) && hasSevereViolation)
        {
            var regularGrade = GetGradeFromScore(totalScore, parameters);
            return (regularGrade, 0.0m);
        }

        var grade = GetGradeFromScore(totalScore, parameters);
        decimal bonus = grade switch
        {
            KpiGrade.A => parameters.BonusCoeffA,
            KpiGrade.B => parameters.BonusCoeffB,
            KpiGrade.C => parameters.BonusCoeffC,
            KpiGrade.D => parameters.BonusCoeffD,
            _ => parameters.BonusCoeffE
        };

        return (grade, bonus);
    }

    private static KpiGrade GetGradeFromScore(decimal score, KpiEngineParameters parameters)
    {
        if (score >= parameters.GradeAMinScore)
            return KpiGrade.A;

        if (score >= parameters.GradeBMinScore)
            return KpiGrade.B;

        if (score >= parameters.GradeCMinScore)
            return KpiGrade.C;

        if (score >= parameters.GradeDMinScore)
            return KpiGrade.D;

        return KpiGrade.E;
    }
}
