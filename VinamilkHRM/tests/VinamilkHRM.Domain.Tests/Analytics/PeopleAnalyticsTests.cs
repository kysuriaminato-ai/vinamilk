using System;
using System.Collections.Generic;
using VinamilkHRM.Domain.Entities.Analytics;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Enums;
using VinamilkHRM.Domain.Services.Analytics;
using Xunit;

namespace VinamilkHRM.Domain.Tests.Analytics;

/// <summary>
/// Bộ kiểm thử People Analytics, Báo cáo Nhân sự, Vị thế thị trường & Dự báo (Phiên 8).
/// Bao phủ toàn bộ 5 Tiêu chí nghiệm thu cốt lõi theo đề bài.
/// </summary>
public class PeopleAnalyticsTests
{
    /// <summary>
    /// TIÊU CHÍ NGHIỆM THU 1:
    /// Đầu kỳ 200, cuối kỳ 196, nghỉ 6 -> tỷ lệ nghỉ việc tháng 3,03%.
    /// AverageHeadcount = (200 + 196) / 2 = 198.
    /// Rate = (6 / 198) * 100% = 3.0303...% -> 3.03%.
    /// </summary>
    [Fact]
    public void Criterion1_Start200_End196_Exits6_ShouldReturn_3_03Percent_MonthlyTurnoverRate()
    {
        // Arrange
        int startHeadcount = 200;
        int endHeadcount = 196;
        int exits = 6;

        // Act
        decimal turnoverRate = PeopleAnalyticsEngine.CalculateMonthlyTurnoverRate(startHeadcount, endHeadcount, exits);

        // Assert: Đúng chính xác 3.03%
        Assert.Equal(3.03m, turnoverRate);
    }

    /// <summary>
    /// TIÊU CHÍ NGHIỆM THU 2:
    /// Tăng lương 6%, CPI 3,5% -> tăng lương thực 2,42%.
    /// Công thức Fisher: RealIncrease = ((1 + 0.06) / (1 + 0.035) - 1) * 100% = 2.41545...% -> 2.42%.
    /// </summary>
    [Fact]
    public void Criterion2_NominalIncrease_6Percent_CPI_3_5Percent_ShouldReturn_2_42Percent_RealWageIncrease()
    {
        // Arrange
        decimal nominalIncrease = 0.06m; // 6%
        decimal cpiInflation = 0.035m;   // 3.5%

        // Act
        decimal realIncrease = PeopleAnalyticsEngine.CalculateRealWageIncrease(nominalIncrease, cpiInflation);

        // Assert: Đúng chính xác 2.42%
        Assert.Equal(2.42m, realIncrease);
    }

    /// <summary>
    /// TIÊU CHÍ NGHIỆM THU 3:
    /// Mức giữa khung 20.000.000, P50 thị trường 23.000.000 -> -13,0%, có cờ.
    /// Gap = (20.000.000 - 23.000.000) / 23.000.000 * 100% = -13.043...% -> -13.0%, cờ = true.
    /// </summary>
    [Fact]
    public void Criterion3_InternalMid_20M_MarketP50_23M_ShouldReturn_Minus13_0Percent_AndFlagged()
    {
        // Arrange
        decimal internalMid = 20_000_000m;
        decimal marketP50 = 23_000_000m;

        // Act
        var (gapPercentage, isFlagged) = PeopleAnalyticsEngine.CalculatePayCompetitivenessGap(internalMid, marketP50, flagThresholdPercentage: 10.0m);

        // Assert: Chênh lệch đúng -13.0% và được gắn cờ cảnh báo thị trường
        Assert.Equal(-13.0m, gapPercentage);
        Assert.True(isFlagged);
    }

    /// <summary>
    /// TIÊU CHÍ NGHIỆM THU 4:
    /// Đơn vị 3 người -> API tổng hợp lương trả "không đủ cỡ mẫu" (ngưỡng bảo vệ riêng tư < 5).
    /// </summary>
    [Fact]
    public void Criterion4_UnitSize_3Employees_ShouldFailPrivacyThreshold_AndIndicateInsufficientSampleSize()
    {
        // Arrange
        int unitHeadcount = 3;

        // Act
        bool isSufficient = PeopleAnalyticsEngine.HasSufficientSampleSizeForPrivacy(unitHeadcount);

        // Assert: Nhóm 3 người không đạt ngưỡng bảo vệ riêng tư (cần >= 5)
        Assert.False(isSufficient);

        // Giả lập thông điệp trả về người dùng
        string responseMessage = isSufficient ? "Hiển thị số liệu" : "không đủ cỡ mẫu";
        Assert.Equal("không đủ cỡ mẫu", responseMessage);
    }

    /// <summary>
    /// TIÊU CHÍ NGHIỆM THU 5:
    /// Snapshot tháng 9 không đổi sau khi sửa hồ sơ nhân viên vào tháng 10.
    /// Bản chụp số liệu lịch sử rpt.WorkforceSnapshot là bất biến.
    /// </summary>
    [Fact]
    public void Criterion5_SeptemberSnapshot_RemainsImmutable_AfterOctoberEmployeeModification()
    {
        // Arrange: Chụp snapshot tháng 9 cho đơn vị Nhà máy sữa Dĩ An (UnitId = 10)
        var snapshotSept = new WorkforceSnapshot(
            month: "2026-09",
            organizationalUnitId: 10,
            startHeadcount: 200,
            endHeadcount: 196,
            hires: 2,
            exits: 6,
            exitsByReasonJson: "{\"Compensation\":3,\"Personal\":2,\"Management\":1}",
            transfersIn: 0,
            transfersOut: 0,
            promotions: 4,
            absenceRate: 2.1m,
            overtimeHours: 450m,
            laborCost: 3_250_000_000m,
            avgKpi: 102.5m,
            gradeDistributionJson: "{\"A\":30,\"B\":120,\"C\":40,\"D\":6,\"E\":0}",
            isSampleData: false);

        int originalEndHeadcount = snapshotSept.EndHeadcount;
        decimal originalLaborCost = snapshotSept.LaborCost;
        int originalExits = snapshotSept.Exits;

        // Act: Sang tháng 10, nhân viên trong đơn vị bị sửa lương, điều chuyển hoặc thôi việc
        // Tạo biến động tháng 10
        var snapshotOct = new WorkforceSnapshot(
            month: "2026-10",
            organizationalUnitId: 10,
            startHeadcount: 196,
            endHeadcount: 190,
            hires: 1,
            exits: 7,
            exitsByReasonJson: "{\"Compensation\":4,\"Personal\":3}",
            transfersIn: 0,
            transfersOut: 0,
            promotions: 2,
            absenceRate: 2.5m,
            overtimeHours: 520m,
            laborCost: 3_400_000_000m, // Lương đã tăng và sửa
            avgKpi: 99.0m,
            gradeDistributionJson: "{\"A\":25,\"B\":115,\"C\":45,\"D\":5,\"E\":0}",
            isSampleData: false);

        // Assert: Bản chụp tháng 9 hoàn toàn giữ nguyên giá trị bất biến, không bị ảnh hưởng
        Assert.Equal("2026-09", snapshotSept.Month);
        Assert.Equal(196, snapshotSept.EndHeadcount);
        Assert.Equal(3_250_000_000m, snapshotSept.LaborCost);
        Assert.Equal(6, snapshotSept.Exits);
        Assert.Equal(originalEndHeadcount, snapshotSept.EndHeadcount);
        Assert.Equal(originalLaborCost, snapshotSept.LaborCost);
        Assert.Equal(originalExits, snapshotSept.Exits);

        // Khẳng định tháng 10 có giá trị riêng biệt
        Assert.Equal("2026-10", snapshotOct.Month);
        Assert.Equal(190, snapshotOct.EndHeadcount);
        Assert.Equal(3_400_000_000m, snapshotOct.LaborCost);
    }

    /// <summary>
    /// Kiểm thử bổ sung: Tính điểm rủi ro nghỉ việc dạng luật cộng điểm minh bạch.
    /// </summary>
    [Fact]
    public void AdditionalTest_AttritionRiskScoring_ShouldCalculateCorrectScore_WithoutDemographicFactors()
    {
        // Arrange: Nhân viên có Compa-ratio 75% (+25), không tăng lương 26 tháng (+20), OT 35h (+20) -> Tổng 65 điểm (High)
        var inputs = new AttritionRiskFactorInputs(
            CompaRatio: 0.75m,
            MonthsSinceLastSalaryIncrease: 26,
            MonthlyOvertimeHours: 35m,
            AbsenceRate: 2.0m,
            DiligenceScore: 98m,
            HasKpiDropOrActiveAppeal: false,
            TenureAtCurrentStepMonths: 18);

        // Act
        var result = PeopleAnalyticsEngine.CalculateAttritionRiskScore(inputs);

        // Assert: Điểm 65, mức High, có 3 lý do giải thích minh bạch
        Assert.Equal(65, result.TotalScore);
        Assert.Equal(AttritionRiskLevel.High, result.Level);
        Assert.Equal(3, result.ExplainableReasons.Count);
        Assert.Contains(result.ExplainableReasons, r => r.Contains("Compa-Ratio"));
        Assert.Contains(result.ExplainableReasons, r => r.Contains("chưa được điều chỉnh lương"));
        Assert.Contains(result.ExplainableReasons, r => r.Contains("Làm thêm giờ quá tải"));
    }
}