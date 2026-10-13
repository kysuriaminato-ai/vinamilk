using System;
using System.Collections.Generic;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Jobs;
using VinamilkHRM.Domain.Entities.Kpis;
using VinamilkHRM.Domain.Entities.Salaries;
using VinamilkHRM.Domain.Enums;
using VinamilkHRM.Domain.Services.TotalRewards;
using Xunit;

namespace VinamilkHRM.Domain.Tests.TotalRewards;

/// <summary>
/// Kiểm thử Động cơ Quản trị Lương & Xét tăng lương Total Rewards (Phiên 7).
/// Bao phủ toàn bộ 5 Tiêu chí nghiệm thu cốt lõi theo đề bài.
/// </summary>
public class SalaryReviewTests
{
    /// <summary>
    /// TIÊU CHÍ NGHIỆM THU 1:
    /// P1 12.000.000, Mid 12.500.000, KPI năm B -> gợi ý 6% -> 12.720.000 đ.
    /// </summary>
    [Fact]
    public void Criterion1_P1_12M_Mid_12_5M_Kpi_B_ShouldSuggest_6Percent_And_12_720_000()
    {
        // Arrange
        decimal currentP1 = 12_000_000m;
        decimal mid = 12_500_000m;
        var band = new SalaryBand(
            jobGradeId: 3,
            min: 10_000_000m,
            mid: mid,
            max: 16_000_000m,
            effectiveFrom: new DateOnly(2026, 1, 1));

        // Compa-ratio = 12.000.000 / 12.500.000 = 0.96 (96%)
        decimal compaRatio = band.CalculateCompaRatio(currentP1);
        Assert.Equal(0.96m, compaRatio);

        // Giả lập 12 kỳ KPI đạt điểm trung bình xếp loại B
        var kpiResults = new List<KpiResult>();
        for (int i = 1; i <= 12; i++)
        {
            kpiResults.Add(new KpiResult(
                employeeId: 101,
                period: $"2026-{i:D2}",
                totalScore: 100m, // Điểm B (95 - <110)
                grade: KpiGrade.B,
                bonusCoefficient: 1.0m,
                unitResultScore: 25m,
                individualWorkScore: 50m,
                peopleManagementScore: 15m,
                complianceScore: 10m,
                isUnrated: false,
                lockedBy: "SYSTEM"));
        }

        var (annualScore, grade) = TotalRewardsEngine.CalculateAnnualKpi(kpiResults);
        Assert.Equal(100m, annualScore);
        Assert.Equal(KpiGrade.B, grade);

        // Act: Tra ma trận gợi ý % tăng lương
        decimal suggestedPct = TotalRewardsEngine.GetMeritIncreasePercentage(grade, compaRatio);
        decimal suggestedIncrease = Math.Round(currentP1 * suggestedPct, 0, MidpointRounding.AwayFromZero);
        decimal proposedP1 = currentP1 + suggestedIncrease;

        // Assert: Gợi ý đúng 6% và mức lương mới đúng 12.720.000 đ
        Assert.Equal(0.06m, suggestedPct);
        Assert.Equal(720_000m, suggestedIncrease);
        Assert.Equal(12_720_000m, proposedP1);
    }

    /// <summary>
    /// TIÊU CHÍ NGHIỆM THU 2:
    /// Đơn vị ngân sách 100.000.000 đ, tổng gợi ý 125.000.000 đ -> mọi mức nhân 0,8.
    /// </summary>
    [Fact]
    public void Criterion2_Budget_100M_TotalSuggested_125M_ShouldProrateBy_0_8()
    {
        // Arrange
        decimal unitBudget = 100_000_000m;
        decimal totalSuggested = 125_000_000m;

        // Act: Tính hệ số co giãn ngân sách (Proration Factor)
        decimal factor = TotalRewardsEngine.CalculateProrationFactor(unitBudget, totalSuggested);

        // Assert: Hệ số co giãn chính xác 0.8
        Assert.Equal(0.8000m, factor);

        // Ví dụ 1 nhân viên được gợi ý 6% (tăng 720.000 đ) khi co giãn:
        decimal individualSuggestedPct = 0.06m;
        decimal proratedPct = individualSuggestedPct * factor;
        decimal proratedIncrease = Math.Round(12_000_000m * proratedPct, 0, MidpointRounding.AwayFromZero);

        Assert.Equal(0.048m, proratedPct); // 4.8%
        Assert.Equal(576_000m, proratedIncrease); // 720.000 * 0.8 = 576.000
    }

    /// <summary>
    /// TIÊU CHÍ NGHIỆM THU 3:
    /// Trưởng đơn vị nộp vượt ngân sách -> 422 ERR_SAL_BUDGET_EXCEEDED.
    /// </summary>
    [Fact]
    public void Criterion3_UnitSubmission_ExceedingBudget_ShouldThrow_ERR_SAL_BUDGET_EXCEEDED()
    {
        // Arrange
        decimal unitBudget = 100_000_000m;
        decimal totalProposed = 105_000_000m; // Vượt hạn mức 5.000.000 đ

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
        {
            TotalRewardsEngine.ValidateUnitSubmission(unitBudget, totalProposed);
        });

        Assert.Equal("ERR_SAL_BUDGET_EXCEEDED", ex.ErrorCode);
    }

    /// <summary>
    /// TIÊU CHÍ NGHIỆM THU 4:
    /// Đặc cách +35% -> bắt buộc thêm bước Hội đồng quản trị; người khởi tạo tự duyệt -> 403 (ERR_SAL_PROPOSER_CANNOT_APPROVE).
    /// </summary>
    [Fact]
    public void Criterion4_SpecialIncrease_35Percent_RequiresBoardOfDirectors_And_ProposerSelfApprovalThrows403()
    {
        // Arrange
        decimal currentP1 = 20_000_000m;
        decimal proposedP1 = 27_000_000m; // +35% (7.000.000 / 20.000.000 = 0.35)
        var band = new SalaryBand(5, 15_000_000m, 22_000_000m, 30_000_000m, new DateOnly(2026, 1, 1));

        // Act 1: Đánh giá trần đặc cách 30%
        var (requiresBoard, pct, reason) = TotalRewardsEngine.EvaluateSpecialAdjustment(currentP1, proposedP1, band);

        // Assert 1: Bắt buộc thêm bước Hội đồng quản trị
        Assert.True(requiresBoard);
        Assert.Equal(0.35m, pct);
        Assert.Contains("vượt trần đặc cách 30%", reason);

        // Act 2: Tạo đề xuất đặc cách và cấu hình Route
        int employeeId = 501;
        int proposerId = 601; // Người khởi tạo (Giám đốc đơn vị)

        var proposal = new SalaryProposal(
            employeeId: employeeId,
            proposedById: proposerId,
            currentSalary: currentP1,
            proposedSalary: proposedP1,
            currentAllowance: 2_000_000m,
            proposedAllowance: 2_000_000m,
            reason: "Đặc cách giữ chuyên gia kỹ thuật cao trước lời mời cạnh tranh",
            effectiveDate: DateTime.UtcNow,
            adjustmentType: SalaryAdjustmentType.Special,
            fundSource: SalaryFundSource.SpecialFund);

        proposal.ConfigureRoute(requiresBoardOfDirectors: requiresBoard, ceoId: 999);

        // Kiểm tra danh sách các bước duyệt: Phải có Hội đồng quản trị
        Assert.Contains(proposal.ApprovalSteps, s => s.RoleLevel == ApprovalRoleLevel.BoardOfDirectors);

        // Act 3: Người khởi tạo (proposerId = 601) cố tình tự duyệt đề xuất
        var ex = Assert.Throws<DomainException>(() =>
        {
            proposal.ProcessStepApproval(reviewerId: proposerId, notes: "Tự phê duyệt đề xuất do mình lập");
        });

        // Assert 3: Bị chặn với mã lỗi ERR_SAL_PROPOSER_CANNOT_APPROVE (HTTP 403)
        Assert.Equal("ERR_SAL_PROPOSER_CANNOT_APPROVE", ex.ErrorCode);

        // Kiểm tra thêm: Người được tăng (employeeId = 501) cố tình tự duyệt
        var exSelf = Assert.Throws<DomainException>(() =>
        {
            proposal.ProcessStepApproval(reviewerId: employeeId, notes: "Tự phê duyệt tăng lương cho chính mình");
        });
        Assert.Equal("ERR_SAL_APPROVAL_CONFLICT_SELF", exSelf.ErrorCode);
    }

    /// <summary>
    /// TIÊU CHÍ NGHIỆM THU 5:
    /// Quyết định duyệt ngày 15/03, hiệu lực 01/01 -> sinh truy lĩnh cho kỳ 01 và 02 đã khóa.
    /// </summary>
    [Fact]
    public void Criterion5_Approval_March15_Effective_Jan1_ShouldGenerateArrearsFor_Jan_And_Feb()
    {
        // Arrange
        int employeeId = 101;
        int approverId = 999;
        decimal currentP1 = 12_000_000m;
        decimal proposedP1 = 12_720_000m; // Tăng thêm 720.000 đ/tháng
        var effectiveDate = new DateOnly(2026, 1, 1);
        var approvalDate = new DateOnly(2026, 3, 15);

        // Các kỳ lương đã khóa trên hệ thống: Kỳ 2026-01 và Kỳ 2026-02
        var runJan = new PayrollRun("2026-01", 1);
        runJan.UpdateTotals(100_000_000m, 80_000_000m, 20_000_000m, 5_000_000m, 10_000_000m, 20_000_000m, 1);
        runJan.Submit(1);
        runJan.Approve(approverId, approverId); // Đã khóa/Approved

        var runFeb = new PayrollRun("2026-02", 1);
        runFeb.UpdateTotals(100_000_000m, 80_000_000m, 20_000_000m, 5_000_000m, 10_000_000m, 20_000_000m, 1);
        runFeb.Submit(1);
        runFeb.Approve(approverId, approverId); // Đã khóa/Approved

        var lockedRuns = new List<PayrollRun> { runJan, runFeb };

        // Act: Sinh danh sách truy lĩnh tự động
        var arrears = TotalRewardsEngine.GenerateRetroactiveAdjustments(
            employeeId: employeeId,
            currentP1: currentP1,
            proposedP1: proposedP1,
            effectiveDate: effectiveDate,
            approvalDate: approvalDate,
            lockedRuns: lockedRuns,
            approverId: approverId);

        // Assert: Phải sinh đúng 2 khoản truy lĩnh cho kỳ 01 và kỳ 02
        Assert.Equal(2, arrears.Count);

        // Khoản 1: Kỳ 2026-01
        var adj1 = arrears[0];
        Assert.Equal(employeeId, adj1.EmployeeId);
        Assert.Equal("2026-01", adj1.SourcePeriod);
        Assert.Equal("2026-03", adj1.TargetPeriod); // Áp dụng vào kỳ lương tháng 3
        Assert.Equal("ARREARS", adj1.AdjustmentType);
        Assert.Equal(720_000m, adj1.Amount);
        Assert.Contains("2026-01", adj1.Reason);

        // Khoản 2: Kỳ 2026-02
        var adj2 = arrears[1];
        Assert.Equal(employeeId, adj2.EmployeeId);
        Assert.Equal("2026-02", adj2.SourcePeriod);
        Assert.Equal("2026-03", adj2.TargetPeriod); // Áp dụng vào kỳ lương tháng 3
        Assert.Equal("ARREARS", adj2.AdjustmentType);
        Assert.Equal(720_000m, adj2.Amount);
        Assert.Contains("2026-02", adj2.Reason);
    }
}