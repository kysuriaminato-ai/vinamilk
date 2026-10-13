using VinamilkHRM.Domain.Entities.Kpis;
using VinamilkHRM.Domain.Enums;
using Xunit;

namespace VinamilkHRM.Domain.Tests;

public class KpiSlaCalculationTests
{
    [Fact]
    public void KpiStatus_EnumValues_ShouldBeUniqueAndNormalized()
    {
        // Kiểm tra enum KpiStatus có đủ 7 trạng thái chuẩn và không trùng lặp
        Assert.Equal(1, (int)KpiStatus.Draft);
        Assert.Equal(2, (int)KpiStatus.Assigned);
        Assert.Equal(3, (int)KpiStatus.InProgress);
        Assert.Equal(4, (int)KpiStatus.Submitted);
        Assert.Equal(5, (int)KpiStatus.Approved);
        Assert.Equal(6, (int)KpiStatus.Rejected);
        Assert.Equal(7, (int)KpiStatus.Cancelled);
    }

    [Fact]
    public void KpiAssignment_SubmitTask_EarlyCompletion_ShouldAwardBonusPoints()
    {
        var dueDate = new DateTime(2026, 10, 20);
        var kpi = new KpiAssignment(
            employeeId: 1,
            evaluatorId: 2,
            period: "2026-10",
            title: "Hoàn thành định mức sản lượng sữa tươi 50.000 lít",
            description: "Chỉ tiêu sản xuất tháng 10",
            targetMetric: "50.000 lít",
            weightPercentage: 30.0m,
            dueDate: dueDate,
            baseScore: 100.0m
        );

        kpi.StartProgress();
        Assert.Equal(KpiStatus.InProgress, kpi.Status);

        // Nộp sớm 3 ngày (ngày 17/10 so với hạn 20/10) -> Thưởng +2đ/ngày = +6đ
        var completionDate = new DateTime(2026, 10, 17);
        kpi.SubmitTask("Đã đạt 51.500 lít sữa tươi đạt chuẩn ISO", completionDate);

        Assert.Equal(KpiStatus.Submitted, kpi.Status);
        Assert.Equal(6.0m, kpi.SlaBonusPenaltyScore);

        // Lãnh đạo thẩm định và duyệt
        kpi.LeaderApproveKpi(2, 0.0m, "Kết quả xuất sắc");
        Assert.Equal(KpiStatus.Approved, kpi.Status);
        Assert.Equal(106.0m, kpi.FinalScore);
    }

    [Fact]
    public void KpiAssignment_SubmitTask_LateCompletion_ShouldDeductPenaltyPoints()
    {
        var dueDate = new DateTime(2026, 10, 15);
        var kpi = new KpiAssignment(
            employeeId: 1,
            evaluatorId: 2,
            period: "2026-10",
            title: "Kiểm tra chất lượng mẫu sữa bột",
            description: "Kiểm nghiệm tiêu chuẩn an toàn thực phẩm",
            targetMetric: "100% mẫu đạt chuẩn",
            weightPercentage: 20.0m,
            dueDate: dueDate,
            baseScore: 100.0m
        );

        kpi.StartProgress();

        // Nộp trễ 2 ngày (ngày 17/10 so với hạn 15/10) -> Phạt -5đ/ngày = -10đ
        var completionDate = new DateTime(2026, 10, 17);
        kpi.SubmitTask("Hoàn thành báo cáo kiểm nghiệm mẫu", completionDate);

        Assert.Equal(KpiStatus.Submitted, kpi.Status);
        Assert.Equal(-10.0m, kpi.SlaBonusPenaltyScore);

        // Lãnh đạo duyệt
        kpi.LeaderApproveKpi(2, 0.0m, "Nộp trễ theo SLA");
        Assert.Equal(KpiStatus.Approved, kpi.Status);
        Assert.Equal(90.0m, kpi.FinalScore);
    }
}
