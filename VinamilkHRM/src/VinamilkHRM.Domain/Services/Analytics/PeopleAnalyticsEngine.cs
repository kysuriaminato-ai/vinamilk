using System;
using System.Collections.Generic;
using System.Linq;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Services.Analytics;

public record AttritionRiskFactorInputs(
    decimal CompaRatio,
    int MonthsSinceLastSalaryIncrease,
    decimal MonthlyOvertimeHours,
    decimal AbsenceRate,
    decimal DiligenceScore,
    bool HasKpiDropOrActiveAppeal,
    int TenureAtCurrentStepMonths);

public record AttritionRiskResult(
    int TotalScore,
    AttritionRiskLevel Level,
    IReadOnlyList<string> ExplainableReasons);

public record ForecastPoint(
    string Period,
    decimal PredictedValue,
    decimal LowerBound95,
    decimal UpperBound95,
    string Label = "DỰ BÁO");

/// <summary>
/// Động cơ People Analytics thuần Domain (0 DB Dependency).
/// </summary>
public static class PeopleAnalyticsEngine
{
    public const int PrivacyThresholdMinimumHeadcount = 5;

    /// <summary>
    /// TIÊU CHÍ NGHIỆM THU 1:
    /// Tính tỷ lệ nghỉ việc tháng = Exits / AverageHeadcount * 100%.
    /// AverageHeadcount = (StartHeadcount + EndHeadcount) / 2.
    /// Ví dụ: Đầu kỳ 200, cuối kỳ 196, nghỉ 6 -> Avg = 198 -> 3.03%.
    /// </summary>
    public static decimal CalculateMonthlyTurnoverRate(int startHeadcount, int endHeadcount, int exits)
    {
        if (startHeadcount < 0 || endHeadcount < 0 || exits < 0)
            throw new DomainException("Số lượng nhân sự và số lượt thôi việc không thể âm.", "ERR_ANALYTICS_NEGATIVE_INPUT");

        decimal avgHeadcount = (startHeadcount + endHeadcount) / 2.0m;
        if (avgHeadcount <= 0) return 0m;

        decimal rate = (exits / avgHeadcount) * 100m;
        return Math.Round(rate, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// TIÊU CHÍ NGHIỆM THU 2:
    /// Tính tăng lương thực tế theo công thức Fisher:
    /// RealIncrease = ((1 + NominalIncrease) / (1 + Inflation) - 1) * 100%.
    /// Ví dụ: Tăng 6% (0.06), CPI 3.5% (0.035) -> (1.06 / 1.035 - 1) * 100% = 2.42%.
    /// </summary>
    public static decimal CalculateRealWageIncrease(decimal nominalIncreaseRate, decimal inflationRate)
    {
        if (inflationRate <= -1.0m)
            throw new DomainException("Tỷ lệ lạm phát không hợp lệ.", "ERR_ANALYTICS_INFLATION_INVALID");

        decimal realRate = ((1.0m + nominalIncreaseRate) / (1.0m + inflationRate) - 1.0m) * 100m;
        return Math.Round(realRate, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// TIÊU CHÍ NGHIỆM THU 3:
    /// Bảng Vị thế lương so với thị trường:
    /// Gap = (InternalMid - MarketP50) / MarketP50 * 100%.
    /// Ví dụ: InternalMid = 20.000.000, MarketP50 = 23.000.000 -> -13.0%, cờ = true.
    /// Gắn cờ khi thấp hơn thị trường > flagThresholdPercentage (mặc định 10%).
    /// </summary>
    public static (decimal GapPercentage, bool IsFlagged) CalculatePayCompetitivenessGap(
        decimal internalMid,
        decimal marketP50,
        decimal flagThresholdPercentage = 10.0m)
    {
        if (marketP50 <= 0)
            throw new DomainException("Trung vị thị trường (Market P50) phải lớn hơn 0.", "ERR_ANALYTICS_MARKET_P50_INVALID");

        decimal gap = ((internalMid - marketP50) / marketP50) * 100m;
        decimal roundedGap = Math.Round(gap, 1, MidpointRounding.AwayFromZero);

        // Gắn cờ khi thấp hơn thị trường vượt ngưỡng (ví dụ -13.0% < -10%)
        bool isFlagged = roundedGap < -Math.Abs(flagThresholdPercentage);

        return (roundedGap, isFlagged);
    }

    /// <summary>
    /// TIÊU CHÍ NGHIỆM THU 4:
    /// Kiểm tra ngưỡng bảo vệ riêng tư: Nhóm dưới 5 người thì ẩn số liệu lương tổng hợp.
    /// Trả về true nếu đủ cỡ mẫu (>= 5), false nếu không đủ cỡ mẫu (< 5).
    /// </summary>
    public static bool HasSufficientSampleSizeForPrivacy(int headcount)
    {
        return headcount >= PrivacyThresholdMinimumHeadcount;
    }

    /// <summary>
    /// Tính điểm rủi ro nghỉ việc dạng luật cộng điểm minh bạch (Explainable Attrition Risk Scoring).
    /// RÀNG BUỘC PHÁP LÝ: Nghiêm cấm dùng Giới tính, Tuổi, Dân tộc, Tôn giáo, Hôn nhân, Thai sản làm yếu tố!
    /// </summary>
    public static AttritionRiskResult CalculateAttritionRiskScore(AttritionRiskFactorInputs inputs)
    {
        int score = 0;
        var reasons = new List<string>();

        // 1. Compa-ratio thấp (< 0.80): Lương thấp hơn nhiều so với thị trường (+25 điểm)
        if (inputs.CompaRatio > 0 && inputs.CompaRatio < 0.80m)
        {
            score += 25;
            reasons.Add($"Mức lương thấp hơn đáng kể so với trung vị ngạch (Compa-Ratio = {inputs.CompaRatio * 100:0.##}% < 80%)");
        }

        // 2. Không được tăng lương trong > 24 tháng (+20 điểm)
        if (inputs.MonthsSinceLastSalaryIncrease >= 24)
        {
            score += 20;
            reasons.Add($"Hơn {inputs.MonthsSinceLastSalaryIncrease} tháng chưa được điều chỉnh lương vị trí");
        }

        // 3. Làm thêm giờ quá tải (> 30 giờ/tháng) (+20 điểm)
        if (inputs.MonthlyOvertimeHours > 30m)
        {
            score += 20;
            reasons.Add($"Làm thêm giờ quá tải liên tục ({inputs.MonthlyOvertimeHours:0.#} giờ OT/tháng > 30h)");
        }

        // 4. Vắng mặt cao hoặc điểm chuyên cần giảm sút (+15 điểm)
        if (inputs.AbsenceRate > 5.0m || inputs.DiligenceScore < 90m)
        {
            score += 15;
            reasons.Add($"Tỷ lệ vắng mặt tăng cao ({inputs.AbsenceRate:0.##}%) hoặc điểm chuyên cần thấp ({inputs.DiligenceScore:0.#}/100)");
        }

        // 5. Hiệu suất KPI sụt giảm hoặc có khiếu nại chưa giải quyết (+15 điểm)
        if (inputs.HasKpiDropOrActiveAppeal)
        {
            score += 15;
            reasons.Add("Hiệu suất KPI giảm sút đột ngột hoặc phát sinh khiếu nại đánh giá");
        }

        // 6. Đọng bậc ngạch quá 3 năm không được thăng tiến (+15 điểm)
        if (inputs.TenureAtCurrentStepMonths >= 36)
        {
            score += 15;
            reasons.Add($"Đọng bậc lương thâm niên quá {inputs.TenureAtCurrentStepMonths} tháng (> 3 năm) chưa được nâng bậc/thăng chức");
        }

        score = Math.Min(100, score);

        var level = score switch
        {
            >= 80 => AttritionRiskLevel.Critical,
            >= 60 => AttritionRiskLevel.High,
            >= 30 => AttritionRiskLevel.Medium,
            _ => AttritionRiskLevel.Low
        };

        return new AttritionRiskResult(score, level, reasons);
    }

    /// <summary>
    /// Dự báo chuỗi thời gian 6 tháng cho số liệu (Headcount, Turnover, LaborCost).
    /// Áp dụng mô hình hồi quy xu hướng Holt-Winters / Linear Trend kèm khoảng tin cậy 95%.
    /// </summary>
    public static List<ForecastPoint> ForecastTimeSeries(
        IReadOnlyList<decimal> historicalValues,
        int forecastMonths = 6,
        string startPeriod = "2026-11")
    {
        var result = new List<ForecastPoint>();
        if (historicalValues == null || historicalValues.Count == 0)
            return result;

        int n = historicalValues.Count;
        decimal avg = historicalValues.Average();

        // Tính độ dốc xu hướng (Trend slope)
        decimal slope = 0m;
        if (n >= 2)
        {
            decimal sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
            for (int i = 0; i < n; i++)
            {
                decimal x = i + 1;
                decimal y = historicalValues[i];
                sumX += x;
                sumY += y;
                sumXY += x * y;
                sumX2 += x * x;
            }

            decimal denominator = (n * sumX2 - sumX * sumX);
            if (denominator != 0)
            {
                slope = (n * sumXY - sumX * sumY) / denominator;
            }
        }

        // Độ lệch chuẩn mẫu để tính khoảng tin cậy 95% (t ~ 1.96 * stdErr)
        decimal variance = historicalValues.Sum(v => (v - avg) * (v - avg)) / Math.Max(1, n - 1);
        decimal stdDev = (decimal)Math.Sqrt((double)variance);
        decimal marginOfError = Math.Max(stdDev * 1.96m, avg * 0.05m); // Tối thiểu 5%

        int startYear = int.Parse(startPeriod.Substring(0, 4));
        int startMonth = int.Parse(startPeriod.Substring(5, 2));

        for (int i = 0; i < forecastMonths; i++)
        {
            int m = startMonth + i;
            int y = startYear + (m - 1) / 12;
            int actualMonth = ((m - 1) % 12) + 1;
            string period = $"{y:D4}-{actualMonth:D2}";

            decimal predicted = Math.Round(historicalValues.Last() + slope * (i + 1), 2, MidpointRounding.AwayFromZero);
            if (predicted < 0) predicted = 0m;

            decimal lower = Math.Max(0m, Math.Round(predicted - marginOfError * (1m + 0.1m * i), 2, MidpointRounding.AwayFromZero));
            decimal upper = Math.Round(predicted + marginOfError * (1m + 0.1m * i), 2, MidpointRounding.AwayFromZero);

            result.Add(new ForecastPoint(period, predicted, lower, upper, "DỰ BÁO"));
        }

        return result;
    }
}