using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VinamilkHRM.Domain.Entities.Workflows;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Interfaces.Workflows;

/// <summary>
/// Giao diện Workflow Engine đa luồng, hỗ trợ rẽ nhánh động, ủy quyền và tự động chuyển cấp SLA.
/// </summary>
public interface IWorkflowEngine
{
    Task<WorkflowInstance> StartWorkflowAsync(
        WorkflowType type,
        int requesterId,
        string entityType,
        int entityId,
        WorkflowEvaluationContext context,
        CancellationToken ct = default);

    Task<bool> CanUserApproveAsync(
        int instanceId,
        int stepOrder,
        int userId,
        DateTimeOffset now,
        CancellationToken ct = default);

    Task<WorkflowStepInstance> ApproveStepAsync(
        int instanceId,
        int stepOrder,
        int approverId,
        string? comment,
        DateTimeOffset now,
        CancellationToken ct = default);

    Task<WorkflowStepInstance> RejectStepAsync(
        int instanceId,
        int stepOrder,
        int approverId,
        string reason,
        DateTimeOffset now,
        CancellationToken ct = default);

    Task<List<WorkflowStepInstance>> CheckAndEscalateOverdueStepsAsync(
        DateTimeOffset now,
        CancellationToken ct = default);
}