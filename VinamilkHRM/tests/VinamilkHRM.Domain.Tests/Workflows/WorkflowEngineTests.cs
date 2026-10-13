using System;
using System.Linq;
using VinamilkHRM.Domain.Entities.Workflows;
using VinamilkHRM.Domain.Enums;
using Xunit;

namespace VinamilkHRM.Domain.Tests.Workflows;

public class WorkflowEngineTests
{
    [Fact]
    public void LeaveRequest_5Days_ShouldAutomaticallyRequire3Steps()
    {
        // ARRANGE: Cấu hình quy trình nghỉ phép chuẩn Vinamilk
        var definition = new WorkflowDefinition("WF_LEAVE", "Quy trình Phê duyệt Nghỉ phép Vinamilk", WorkflowType.LeaveRequest);
        definition.AddStep(new WorkflowStepDefinition(1, "Quản lý trực tiếp phê duyệt", WorkflowApproverRole.DirectManager, slaHours: 48));
        definition.AddStep(new WorkflowStepDefinition(2, "Trưởng đơn vị phê duyệt", WorkflowApproverRole.UnitLeader, slaHours: 48, minDays: 3m));
        definition.AddStep(new WorkflowStepDefinition(3, "Phòng Nhân sự xác nhận", WorkflowApproverRole.HrManager, slaHours: 48, minDays: 5m));

        var context5Days = new WorkflowEvaluationContext(days: 5m);
        var context2Days = new WorkflowEvaluationContext(days: 2m);
        var context3Days = new WorkflowEvaluationContext(days: 3.5m);

        // ACT
        var steps5Days = definition.Steps.Where(s => s.ShouldExecute(context5Days)).OrderBy(s => s.StepOrder).ToList();
        var steps2Days = definition.Steps.Where(s => s.ShouldExecute(context2Days)).OrderBy(s => s.StepOrder).ToList();
        var steps3Days = definition.Steps.Where(s => s.ShouldExecute(context3Days)).OrderBy(s => s.StepOrder).ToList();

        // ASSERT: Đơn nghỉ 5 ngày tự động đi qua 3 bước theo cấu hình (Tiêu chí 1)
        Assert.Equal(3, steps5Days.Count);
        Assert.Equal(WorkflowApproverRole.DirectManager, steps5Days[0].ApproverRole);
        Assert.Equal(WorkflowApproverRole.UnitLeader, steps5Days[1].ApproverRole);
        Assert.Equal(WorkflowApproverRole.HrManager, steps5Days[2].ApproverRole);

        // Đơn nghỉ 2 ngày chỉ qua 1 bước quản lý trực tiếp
        Assert.Single(steps2Days);
        Assert.Equal(WorkflowApproverRole.DirectManager, steps2Days[0].ApproverRole);

        // Đơn nghỉ 3.5 ngày qua 2 bước (Quản lý trực tiếp + Trưởng đơn vị)
        Assert.Equal(2, steps3Days.Count);
    }

    [Fact]
    public void ApprovalDelegation_WhenActive_AllowsDelegateeToApproveOnBehalf()
    {
        // ARRANGE: Quản lý trực tiếp (Id: 10) vắng mặt, ủy quyền cho Phó quản lý (Id: 20)
        var now = DateTimeOffset.UtcNow;
        var delegation = new ApprovalDelegation(
            delegatorId: 10,
            delegateeId: 20,
            fromDate: now.AddDays(-1),
            toDate: now.AddDays(7),
            reason: "Ủy quyền duyệt đơn nghỉ phép do đi công tác tại Nhà máy Tiên Sơn",
            workflowType: WorkflowType.LeaveRequest);

        // ACT & ASSERT: Kiểm tra hiệu lực ủy quyền
        Assert.True(delegation.IsValid(now, WorkflowType.LeaveRequest));
        Assert.False(delegation.IsValid(now.AddDays(8), WorkflowType.LeaveRequest)); // Quá hạn
        Assert.False(delegation.IsValid(now, WorkflowType.SalaryAdjustment));       // Khác loại luồng

        // Tạo bước duyệt gán cho Delegator (Id: 10)
        var step = new WorkflowStepInstance(
            stepOrder: 1,
            stepName: "Quản lý trực tiếp phê duyệt",
            requiredRole: WorkflowApproverRole.DirectManager,
            assignedApproverId: 10,
            slaHours: 48);

        // Khi người duyệt vắng có ủy quyền thì người được ủy quyền (Id: 20) duyệt thay
        step.Approve(actualApproverId: 20, delegatedFromId: 10, comment: "Đồng ý phê duyệt thay Quản lý trực tiếp (ủy quyền)", now: now);

        // ASSERT: Bản ghi ghi nhận rõ ràng người duyệt thực tế và người ủy quyền
        Assert.Equal(WorkflowStepStatus.Approved, step.Status);
        Assert.Equal(20, step.ActualApproverId);
        Assert.Equal(10, step.DelegatedFromId);
        Assert.Contains("ủy quyền", step.Comment);
    }

    [Fact]
    public void WorkflowStep_Exceeding48HoursSla_ShouldAutoEscalate()
    {
        // ARRANGE: Bước duyệt có thời hạn SLA 48 giờ (Tiêu chí 2)
        var startTime = DateTimeOffset.UtcNow;
        var step = new WorkflowStepInstance(
            stepOrder: 1,
            stepName: "Quản lý trực tiếp phê duyệt",
            requiredRole: WorkflowApproverRole.DirectManager,
            assignedApproverId: 10,
            slaHours: 48,
            isAutoEscalate: true,
            escalateToRole: WorkflowApproverRole.UnitLeader);

        step.SetPending(startTime);

        // Thời điểm kiểm tra: 50 giờ sau khi bắt đầu (đã quá SLA 48 giờ)
        var checkTime = startTime.AddHours(50);

        // ACT: Kiểm tra điều kiện quá hạn SLA và thực hiện chuyển cấp
        bool isOverdue = step.Status == WorkflowStepStatus.Pending 
                      && step.IsAutoEscalate 
                      && step.SlaDeadline.HasValue 
                      && step.SlaDeadline.Value <= checkTime;

        Assert.True(isOverdue, "Bước duyệt phải được nhận diện là quá hạn SLA.");

        int superiorApproverId = 5; // Cấp trên tiếp theo (Trưởng đơn vị)
        step.Escalate(superiorApproverId, "Quá hạn SLA 48 giờ không xử lý. Chuyển cấp lên Trưởng đơn vị.", checkTime);

        // ASSERT: Trạng thái và nội dung ghi nhận chuyển cấp
        Assert.Equal(WorkflowStepStatus.Pending, step.Status); // Vẫn ở Pending cho người duyệt mới
        Assert.Equal(superiorApproverId, step.AssignedApproverId);
        Assert.Equal(checkTime, step.EscalatedAt);
        Assert.Contains("TỰ ĐỘNG CHUYỂN CẤP QUÁ HẠN SLA 48h", step.Comment);
        // Deadline mới được gia hạn thêm 48 giờ kể từ lúc chuyển cấp
        Assert.Equal(checkTime.AddHours(48), step.SlaDeadline);
    }

    [Fact]
    public void SalaryAdjustment_Branching_ShouldRequireExecutiveApprovalWhenOver10Percent()
    {
        // ARRANGE: Quy trình tăng lương với điều kiện Executive duyệt khi tăng >= 10%
        var definition = new WorkflowDefinition("WF_SALARY", "Quy trình Tăng lương", WorkflowType.SalaryAdjustment);
        definition.AddStep(new WorkflowStepDefinition(1, "Quản lý đề xuất", WorkflowApproverRole.DirectManager));
        definition.AddStep(new WorkflowStepDefinition(2, "Trưởng đơn vị duyệt", WorkflowApproverRole.UnitLeader));
        definition.AddStep(new WorkflowStepDefinition(3, "Nhân sự thẩm định", WorkflowApproverRole.HrManager));
        definition.AddStep(new WorkflowStepDefinition(4, "Tổng Giám đốc phê duyệt", WorkflowApproverRole.Executive, minPercentage: 10m));

        // ACT
        var stepsNormalIncrease = definition.Steps.Where(s => s.ShouldExecute(new WorkflowEvaluationContext(percentage: 6m))).ToList();
        var stepsHighIncrease = definition.Steps.Where(s => s.ShouldExecute(new WorkflowEvaluationContext(percentage: 12m))).ToList();

        // ASSERT
        Assert.Equal(3, stepsNormalIncrease.Count);
        Assert.False(stepsNormalIncrease.Any(s => s.ApproverRole == WorkflowApproverRole.Executive));

        Assert.Equal(4, stepsHighIncrease.Count);
        Assert.True(stepsHighIncrease.Any(s => s.ApproverRole == WorkflowApproverRole.Executive));
    }
}