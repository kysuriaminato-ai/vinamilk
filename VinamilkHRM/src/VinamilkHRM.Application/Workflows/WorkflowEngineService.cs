using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Workflows;
using VinamilkHRM.Domain.Enums;
using VinamilkHRM.Domain.Interfaces.Workflows;

namespace VinamilkHRM.Application.Workflows;

/// <summary>
/// Dịch vụ thực thi Workflow Engine cho toàn bộ tập đoàn Vinamilk.
/// Đảm bảo tính toán rẽ nhánh theo dữ liệu, ủy quyền có thời hạn và tự động chuyển cấp SLA.
/// </summary>
public class WorkflowEngineService : IWorkflowEngine
{
    private readonly IApplicationDbContext _context;

    public WorkflowEngineService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<WorkflowInstance> StartWorkflowAsync(
        WorkflowType type,
        int requesterId,
        string entityType,
        int entityId,
        WorkflowEvaluationContext context,
        CancellationToken ct = default)
    {
        var requester = await _context.Employees
            .Include(e => e.OrganizationalUnit)
            .FirstOrDefaultAsync(e => e.Id == requesterId, ct);

        if (requester == null)
            throw new DomainException($"Không tìm thấy thông tin nhân viên với ID: {requesterId}.", "ERR_WF_EMP_NOT_FOUND");

        // Tìm cấu hình WorkflowDefinition trong DB hoặc tạo mặc định chuẩn Vinamilk
        var definition = await _context.WorkflowDefinitions
            .Include(w => w.Steps)
            .FirstOrDefaultAsync(w => w.WorkflowType == type && w.IsActive, ct);

        if (definition == null || !definition.Steps.Any())
        {
            definition = CreateDefaultDefinition(type);
        }

        var instance = new WorkflowInstance(definition.Id > 0 ? definition.Id : 1, entityType, entityId, requesterId);

        // Lọc các bước thỏa mãn điều kiện rẽ nhánh dữ liệu
        var eligibleSteps = definition.Steps
            .Where(s => s.ShouldExecute(context))
            .OrderBy(s => s.StepOrder)
            .ToList();

        if (!eligibleSteps.Any())
        {
            throw new DomainException($"Không có bước duyệt nào thỏa mãn điều kiện cho quy trình {type}.", "ERR_WF_NO_ELIGIBLE_STEPS");
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        int sequentialOrder = 1;

        foreach (var stepDef in eligibleSteps)
        {
            int? assignedApproverId = ResolveApproverId(stepDef, requester);

            var stepInstance = new WorkflowStepInstance(
                sequentialOrder,
                stepDef.StepName,
                stepDef.ApproverRole,
                assignedApproverId,
                stepDef.SlaHours,
                stepDef.IsAutoEscalate,
                stepDef.EscalateToRole);

            if (sequentialOrder == 1)
            {
                stepInstance.SetPending(now);
            }

            instance.AddStep(stepInstance);
            sequentialOrder++;
        }

        _context.WorkflowInstances.Add(instance);
        await _context.SaveChangesAsync(ct);

        return instance;
    }

    public async Task<bool> CanUserApproveAsync(
        int instanceId,
        int stepOrder,
        int userId,
        DateTimeOffset now,
        CancellationToken ct = default)
    {
        var instance = await _context.WorkflowInstances
            .Include(w => w.Steps)
            .Include(w => w.WorkflowDefinition)
            .FirstOrDefaultAsync(w => w.Id == instanceId, ct);

        if (instance == null) return false;

        var step = instance.Steps.FirstOrDefault(s => s.StepOrder == stepOrder && s.Status == WorkflowStepStatus.Pending);
        if (step == null) return false;

        // 1. Phê duyệt trực tiếp
        if (step.AssignedApproverId == userId)
            return true;

        // 2. Phê duyệt theo ủy quyền hợp lệ
        if (step.AssignedApproverId.HasValue)
        {
            int delegatorId = step.AssignedApproverId.Value;
            var workflowType = instance.WorkflowDefinition?.WorkflowType;

            var hasDelegation = await _context.ApprovalDelegations
                .AnyAsync(d => d.DelegatorId == delegatorId
                            && d.DelegateeId == userId
                            && d.IsActive
                            && now >= d.FromDate
                            && now <= d.ToDate
                            && (d.WorkflowType == null || d.WorkflowType == workflowType), ct);

            if (hasDelegation) return true;
        }

        return false;
    }

    public async Task<WorkflowStepInstance> ApproveStepAsync(
        int instanceId,
        int stepOrder,
        int approverId,
        string? comment,
        DateTimeOffset now,
        CancellationToken ct = default)
    {
        var instance = await _context.WorkflowInstances
            .Include(w => w.Steps)
            .Include(w => w.WorkflowDefinition)
            .FirstOrDefaultAsync(w => w.Id == instanceId, ct);

        if (instance == null)
            throw new DomainException($"Không tìm thấy phiên xử lý quy trình với ID: {instanceId}.", "ERR_WF_INSTANCE_NOT_FOUND");

        var step = instance.Steps.FirstOrDefault(s => s.StepOrder == stepOrder);
        if (step == null)
            throw new DomainException($"Không tìm thấy bước duyệt số {stepOrder} trong quy trình {instanceId}.", "ERR_WF_STEP_NOT_FOUND");

        bool canApprove = await CanUserApproveAsync(instanceId, stepOrder, approverId, now, ct);
        if (!canApprove)
            throw new DomainException($"Người dùng ID {approverId} không có quyền phê duyệt bước số {stepOrder}.", "ERR_WF_FORBIDDEN_APPROVER");

        int? delegatedFromId = (step.AssignedApproverId != approverId) ? step.AssignedApproverId : null;
        step.Approve(approverId, delegatedFromId, comment, now);

        instance.AdvanceToNextStep(now);

        // Đồng bộ trạng thái thực thể gốc nếu quy trình đã hoàn thành tất cả các bước
        if (instance.Status == WorkflowInstanceStatus.Approved)
        {
            await SyncEntityApprovalAsync(instance.EntityType, instance.EntityId, approverId, comment, ct);
        }

        await _context.SaveChangesAsync(ct);
        return step;
    }

    public async Task<WorkflowStepInstance> RejectStepAsync(
        int instanceId,
        int stepOrder,
        int approverId,
        string reason,
        DateTimeOffset now,
        CancellationToken ct = default)
    {
        var instance = await _context.WorkflowInstances
            .Include(w => w.Steps)
            .Include(w => w.WorkflowDefinition)
            .FirstOrDefaultAsync(w => w.Id == instanceId, ct);

        if (instance == null)
            throw new DomainException($"Không tìm thấy phiên xử lý quy trình với ID: {instanceId}.", "ERR_WF_INSTANCE_NOT_FOUND");

        var step = instance.Steps.FirstOrDefault(s => s.StepOrder == stepOrder);
        if (step == null)
            throw new DomainException($"Không tìm thấy bước duyệt số {stepOrder}.", "ERR_WF_STEP_NOT_FOUND");

        bool canApprove = await CanUserApproveAsync(instanceId, stepOrder, approverId, now, ct);
        if (!canApprove)
            throw new DomainException($"Người dùng ID {approverId} không có quyền từ chối bước số {stepOrder}.", "ERR_WF_FORBIDDEN_APPROVER");

        step.Reject(approverId, reason, now);
        instance.MarkRejected();

        await SyncEntityRejectionAsync(instance.EntityType, instance.EntityId, approverId, reason, ct);

        await _context.SaveChangesAsync(ct);
        return step;
    }

    public async Task<List<WorkflowStepInstance>> CheckAndEscalateOverdueStepsAsync(
        DateTimeOffset now,
        CancellationToken ct = default)
    {
        var overdueSteps = await _context.WorkflowStepInstances
            .Include(s => s.WorkflowInstance)
            .Where(s => s.Status == WorkflowStepStatus.Pending
                     && s.IsAutoEscalate
                     && s.SlaDeadline.HasValue
                     && s.SlaDeadline.Value <= now)
            .ToListAsync(ct);

        var escalatedSteps = new List<WorkflowStepInstance>();

        foreach (var step in overdueSteps)
        {
            int superiorId = 1; // Mặc định chuyển lên Ban Giám đốc (CEO/Director)
            if (step.AssignedApproverId.HasValue)
            {
                var currentApprover = await _context.Employees.FindAsync(new object[] { step.AssignedApproverId.Value }, ct);
                if (currentApprover?.ManagerId.HasValue == true)
                {
                    superiorId = currentApprover.ManagerId.Value;
                }
            }

            step.Escalate(superiorId, $"Quá hạn thời gian cam kết SLA ({step.SlaHours}h). Tự động chuyển cấp lên cấp quản lý cao hơn.", now);
            step.WorkflowInstance?.MarkEscalated();
            escalatedSteps.Add(step);
        }

        if (escalatedSteps.Any())
        {
            await _context.SaveChangesAsync(ct);
        }

        return escalatedSteps;
    }

    private int? ResolveApproverId(WorkflowStepDefinition stepDef, Domain.Entities.Employees.Employee requester)
    {
        return stepDef.ApproverRole switch
        {
            WorkflowApproverRole.DirectManager => requester.ManagerId ?? requester.OrganizationalUnit?.ManagerEmployeeId ?? 1,
            WorkflowApproverRole.UnitLeader => requester.OrganizationalUnit?.ManagerEmployeeId ?? requester.ManagerId ?? 1,
            WorkflowApproverRole.HrManager => 2, // Mã cán bộ HR phụ trách
            WorkflowApproverRole.Executive => 1, // Ban Giám đốc Vinamilk
            WorkflowApproverRole.SpecificEmployee => stepDef.SpecificApproverId ?? 1,
            _ => requester.ManagerId ?? 1
        };
    }

    private async Task SyncEntityApprovalAsync(string entityType, int entityId, int approverId, string? comment, CancellationToken ct)
    {
        if (entityType.Equals("LeaveRequest", StringComparison.OrdinalIgnoreCase))
        {
            var leave = await _context.LeaveRequests.FindAsync(new object[] { entityId }, ct);
            if (leave != null && leave.Status == LeaveStatus.Pending)
            {
                leave.Approve(approverId, comment);
            }
        }
        else if (entityType.Equals("OvertimeRequest", StringComparison.OrdinalIgnoreCase))
        {
            var ot = await _context.OvertimeRequests.FindAsync(new object[] { entityId }, ct);
            if (ot != null && ot.Status != OvertimeStatus.Approved)
            {
                ot.ApproveByUnitManager(approverId);
            }
        }
    }

    private async Task SyncEntityRejectionAsync(string entityType, int entityId, int approverId, string reason, CancellationToken ct)
    {
        if (entityType.Equals("LeaveRequest", StringComparison.OrdinalIgnoreCase))
        {
            var leave = await _context.LeaveRequests.FindAsync(new object[] { entityId }, ct);
            if (leave != null && leave.Status == LeaveStatus.Pending)
            {
                leave.Reject(approverId, reason);
            }
        }
        else if (entityType.Equals("OvertimeRequest", StringComparison.OrdinalIgnoreCase))
        {
            var ot = await _context.OvertimeRequests.FindAsync(new object[] { entityId }, ct);
            if (ot != null)
            {
                ot.Reject(reason);
            }
        }
    }

    public static WorkflowDefinition CreateDefaultDefinition(WorkflowType type)
    {
        switch (type)
        {
            case WorkflowType.LeaveRequest:
                var leaveWf = new WorkflowDefinition("WF_LEAVE", "Quy trình Phê duyệt Nghỉ phép Vinamilk", WorkflowType.LeaveRequest);
                // Bước 1: Quản lý trực tiếp (luôn có)
                leaveWf.AddStep(new WorkflowStepDefinition(1, "Quản lý trực tiếp phê duyệt", WorkflowApproverRole.DirectManager, slaHours: 48));
                // Bước 2: Trưởng đơn vị (khi nghỉ >= 3 ngày)
                leaveWf.AddStep(new WorkflowStepDefinition(2, "Trưởng đơn vị phê duyệt", WorkflowApproverRole.UnitLeader, slaHours: 48, minDays: 3m));
                // Bước 3: Phòng Nhân sự (khi nghỉ >= 5 ngày)
                leaveWf.AddStep(new WorkflowStepDefinition(3, "Phòng Nhân sự xác nhận & kiểm tra quỹ phép", WorkflowApproverRole.HrManager, slaHours: 48, minDays: 5m));
                return leaveWf;

            case WorkflowType.OvertimeRequest:
                var otWf = new WorkflowDefinition("WF_OVERTIME", "Quy trình Phê duyệt Làm thêm giờ", WorkflowType.OvertimeRequest);
                otWf.AddStep(new WorkflowStepDefinition(1, "Trưởng ca / Quản lý trực tiếp thẩm định", WorkflowApproverRole.DirectManager, slaHours: 24));
                otWf.AddStep(new WorkflowStepDefinition(2, "Giám đốc Đơn vị phê duyệt kế hoạch làm thêm", WorkflowApproverRole.UnitLeader, slaHours: 24));
                return otWf;

            case WorkflowType.SalaryAdjustment:
                var salWf = new WorkflowDefinition("WF_SALARY_INCREASE", "Quy trình Đề xuất & Phê duyệt Tăng lương", WorkflowType.SalaryAdjustment);
                salWf.AddStep(new WorkflowStepDefinition(1, "Quản lý trực tiếp đề xuất", WorkflowApproverRole.DirectManager, slaHours: 48));
                salWf.AddStep(new WorkflowStepDefinition(2, "Trưởng đơn vị xem xét ngân sách", WorkflowApproverRole.UnitLeader, slaHours: 48));
                salWf.AddStep(new WorkflowStepDefinition(3, "Ban Nhân sự thẩm định thị trường & compa-ratio", WorkflowApproverRole.HrManager, slaHours: 48));
                salWf.AddStep(new WorkflowStepDefinition(4, "Ban Tổng Giám đốc phê duyệt đặc biệt (Tăng >= 10%)", WorkflowApproverRole.Executive, slaHours: 48, minPercentage: 10m));
                return salWf;

            default:
                var genericWf = new WorkflowDefinition("WF_GENERIC", "Quy trình Phê duyệt Tiêu chuẩn", type);
                genericWf.AddStep(new WorkflowStepDefinition(1, "Quản lý trực tiếp phê duyệt", WorkflowApproverRole.DirectManager, slaHours: 48));
                return genericWf;
        }
    }
}