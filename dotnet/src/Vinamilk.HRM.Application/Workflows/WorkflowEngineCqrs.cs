using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Domain.Entities.SystemEntities;
using Vinamilk.HRM.Domain.Entities.Time;

namespace Vinamilk.HRM.Application.Workflows;

// ============================================================================
// 1. DTOs CHO WORKFLOW ENGINE DÙNG CHUNG
// ============================================================================

public record WorkflowStepDto(
    int Id,
    int StepOrder,
    string StepName,
    string ApproverType,
    int? ApproverRoleId,
    string? ApproverRoleName,
    int? ApproverUserId,
    string? ApproverUserName,
    string? ConditionExpression,
    int SlaHours);

public record WorkflowDefinitionDto(
    int Id,
    string WorkflowCode,
    string WorkflowName,
    string EntityType,
    string? Description,
    bool IsActive,
    int TotalInstancesCount,
    IReadOnlyList<WorkflowStepDto> Steps);

public record WorkflowApprovalLogDto(
    long Id,
    int StepOrder,
    string StepName,
    int ApproverUserId,
    string ApproverFullName,
    string Action,
    string? Comment,
    DateTime ActionAt);

public record WorkflowInstanceItemDto(
    int Id,
    string WorkflowCode,
    string WorkflowName,
    string EntityType,
    string EntityTypeLabel,
    int EntityId,
    string DocumentTitle,
    string DocumentSummary,
    int InitiatorEmployeeId,
    string InitiatorCode,
    string InitiatorName,
    string InitiatorUnitName,
    int CurrentStepOrder,
    int TotalStepsCount,
    string CurrentStepName,
    int? CurrentApproverUserId,
    string CurrentApproverName,
    string Status,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    int SlaHours,
    bool IsOverdueSla,
    string TargetDetailUrl,
    IReadOnlyList<WorkflowApprovalLogDto> Logs);

public record SimpleLookupDto(int Id, string Code, string Name);

public record WorkflowCenterOverviewDto(
    IReadOnlyList<WorkflowDefinitionDto> Definitions,
    IReadOnlyList<WorkflowInstanceItemDto> Instances,
    IReadOnlyList<SimpleLookupDto> Roles,
    IReadOnlyList<SimpleLookupDto> Users,
    IReadOnlyList<SentEmailRecordDto> RecentEmails,
    int TotalInProgressCount,
    int MyPendingCount,
    int TotalApprovedCount,
    int TotalRejectedCount,
    int TotalDelegatedCount);

// ============================================================================
// 2. HELPER KHỞI TẠO QUY TRÌNH MẪU DOANH NGHIỆP (AUTO-SEED)
// ============================================================================

public static class WorkflowSeederHelper
{
    public static async Task EnsureDefaultWorkflowsAsync(
        IApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var hasAny = await context.WorkflowDefinitions.AnyAsync(cancellationToken);
        var hasPayrollWf = await context.WorkflowDefinitions.AnyAsync(d => d.EntityType == "PayrollPeriod", cancellationToken);
        if (hasAny && hasPayrollWf)
            return;

        var roles = await context.Roles.AsNoTracking().ToDictionaryAsync(r => r.RoleCode, r => r.Id, cancellationToken);
        var users = await context.Users.AsNoTracking().ToDictionaryAsync(u => u.Username, u => u.Id, cancellationToken);

        int? hrManagerRoleId = roles.TryGetValue("HR_MANAGER", out var rHrMgr) ? rHrMgr : null;
        int? hrStaffRoleId = roles.TryGetValue("HR_STAFF", out var rHrStaff) ? rHrStaff : null;
        int? lineManagerUserId = users.TryGetValue("hung.nv", out var uLm) ? uLm : null;
        int? chroUserId = users.TryGetValue("huyen.ntt", out var uChro) ? uChro : null;

        if (!hasAny)
        {

        var definitions = new List<WorkflowDefinition>
        {
            new()
            {
                WorkflowCode = "WF_LEAVE",
                WorkflowName = "Quy trình Phê duyệt Đơn Nghỉ phép & Nghỉ chế độ",
                EntityType = "LeaveRequest",
                Description = "Áp dụng cho nghỉ phép năm, nghỉ ốm BHXH, thai sản, hiếu hỷ, không lương và đi công tác. Đơn >= 3 ngày qua thêm bước C&B thẩm định.",
                IsActive = true,
                Steps = new List<WorkflowStep>
                {
                    new()
                    {
                        StepOrder = 1,
                        StepName = "Bước 1: Quản lý trực tiếp phê duyệt",
                        ApproverType = "DirectManager",
                        ApproverUserId = lineManagerUserId,
                        ConditionExpression = "Always",
                        SlaHours = 12
                    },
                    new()
                    {
                        StepOrder = 2,
                        StepName = "Bước 2: Trưởng phòng Nhân sự / C&B xác nhận quỹ phép",
                        ApproverType = "SpecificRole",
                        ApproverRoleId = hrManagerRoleId,
                        ApproverUserId = chroUserId,
                        ConditionExpression = "TotalDays >= 3",
                        SlaHours = 24
                    }
                }
            },
            new()
            {
                WorkflowCode = "WF_OVERTIME",
                WorkflowName = "Quy trình Phê duyệt Làm thêm giờ (Overtime - OT)",
                EntityType = "OvertimeRequest",
                Description = "Kiểm soát trần OT tối đa 40 giờ/tháng và 200-300 giờ/năm theo Điều 107 Bộ luật Lao động 2019.",
                IsActive = true,
                Steps = new List<WorkflowStep>
                {
                    new()
                    {
                        StepOrder = 1,
                        StepName = "Bước 1: Trưởng ca / Quản lý trực tiếp xác nhận nhu cầu OT",
                        ApproverType = "DirectManager",
                        ApproverUserId = lineManagerUserId,
                        ConditionExpression = "Always",
                        SlaHours = 8
                    },
                    new()
                    {
                        StepOrder = 2,
                        StepName = "Bước 2: Giám đốc Nhà máy / Trưởng đơn vị phê duyệt",
                        ApproverType = "UnitHead",
                        ApproverRoleId = hrManagerRoleId,
                        ApproverUserId = chroUserId,
                        ConditionExpression = "RequestedHours >= 4",
                        SlaHours = 16
                    }
                }
            },
            new()
            {
                WorkflowCode = "WF_ATT_EXPLAIN",
                WorkflowName = "Quy trình Giải trình Chấm công & Bổ sung Công",
                EntityType = "AttendanceExplanation",
                Description = "Áp dụng khi quên chấm vân tay/khuôn mặt, đi gặp khách hàng bên ngoài hoặc lỗi thiết bị chấm công.",
                IsActive = true,
                Steps = new List<WorkflowStep>
                {
                    new()
                    {
                        StepOrder = 1,
                        StepName = "Bước 1: Quản lý trực tiếp xác nhận giờ làm thực tế",
                        ApproverType = "DirectManager",
                        ApproverUserId = lineManagerUserId,
                        ConditionExpression = "Always",
                        SlaHours = 24
                    },
                    new()
                    {
                        StepOrder = 2,
                        StepName = "Bước 2: Chuyên viên C&B hậu kiểm & cập nhật bảng công",
                        ApproverType = "SpecificRole",
                        ApproverRoleId = hrStaffRoleId,
                        ApproverUserId = chroUserId,
                        ConditionExpression = "Always",
                        SlaHours = 24
                    }
                }
            },
            new()
            {
                WorkflowCode = "WF_TRANSFER",
                WorkflowName = "Quy trình Tờ trình Điều chuyển & Bổ nhiệm Nhân sự",
                EntityType = "PersonnelTransfer",
                Description = "Quy trình 2 cấp phê duyệt quyết định điều chuyển, bổ nhiệm, thăng chức và điều chỉnh lương.",
                IsActive = true,
                Steps = new List<WorkflowStep>
                {
                    new()
                    {
                        StepOrder = 1,
                        StepName = "Bước 1: Trưởng đơn vị đề xuất",
                        ApproverType = "UnitHead",
                        ApproverUserId = lineManagerUserId,
                        ConditionExpression = "Always",
                        SlaHours = 24
                    },
                    new()
                    {
                        StepOrder = 2,
                        StepName = "Bước 2: Giám đốc Nhân sự (CHRO) phê duyệt quyết định",
                        ApproverType = "SpecificUser",
                        ApproverRoleId = hrManagerRoleId,
                        ApproverUserId = chroUserId,
                        ConditionExpression = "Always",
                        SlaHours = 48
                    }
                }
            }
        };

        await context.WorkflowDefinitions.AddRangeAsync(definitions, cancellationToken);
        }

        if (!hasPayrollWf)
        {
            await context.WorkflowDefinitions.AddAsync(new WorkflowDefinition
            {
                WorkflowCode = "WF_PAYROLL",
                WorkflowName = "Quy trình Phê duyệt Kỳ tính lương Hàng tháng",
                EntityType = "PayrollPeriod",
                Description = "Kiểm soát phân tách nhiệm vụ: Chuyên viên C&B/Kế toán tính và trình duyệt; Giám đốc Nhân sự (CHRO) / Ban Giám đốc phê duyệt trước khi Khóa và Chi trả.",
                IsActive = true,
                Steps = new List<WorkflowStep>
                {
                    new()
                    {
                        StepOrder = 1,
                        StepName = "Bước 1: Giám đốc Nhân sự / Lãnh đạo phê duyệt Bảng lương",
                        ApproverType = "SpecificRole",
                        ApproverRoleId = hrManagerRoleId,
                        ApproverUserId = chroUserId,
                        ConditionExpression = "Always",
                        SlaHours = 24
                    }
                }
            }, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public static async Task<int> ResolveApproverUserIdAsync(
        IApplicationDbContext context,
        WorkflowStep step,
        int initiatorEmployeeId,
        CancellationToken cancellationToken)
    {
        if (step.ApproverType == "SpecificUser" && step.ApproverUserId.HasValue)
            return step.ApproverUserId.Value;

        if (step.ApproverType == "SpecificRole" && step.ApproverRoleId.HasValue)
        {
            var roleUserId = await context.UserRoles
                .AsNoTracking()
                .Where(ur => ur.RoleId == step.ApproverRoleId.Value && ur.User.IsActive)
                .Select(ur => ur.UserId)
                .FirstOrDefaultAsync(cancellationToken);
            if (roleUserId > 0)
                return roleUserId;
        }

        // Tìm quản lý trực tiếp hoặc trưởng đơn vị của nhân viên khởi tạo
        var initiator = await context.Employees
            .AsNoTracking()
            .Include(e => e.OrganizationUnit)
            .FirstOrDefaultAsync(e => e.Id == initiatorEmployeeId, cancellationToken);

        if (initiator != null)
        {
            if (initiator.DirectManagerId.HasValue)
            {
                var mgrUser = await context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.EmployeeId == initiator.DirectManagerId.Value && u.IsActive, cancellationToken);
                if (mgrUser != null)
                    return mgrUser.Id;
            }

            if (initiator.OrganizationUnit?.ManagerEmployeeId != null &&
                initiator.OrganizationUnit.ManagerEmployeeId != initiator.Id)
            {
                var unitHeadUser = await context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.EmployeeId == initiator.OrganizationUnit.ManagerEmployeeId && u.IsActive, cancellationToken);
                if (unitHeadUser != null)
                    return unitHeadUser.Id;
            }
        }

        if (step.ApproverUserId.HasValue)
            return step.ApproverUserId.Value;

        // Fallback về tài khoản Line Manager hoặc HR Manager
        var fallbackUser = await context.Users
            .AsNoTracking()
            .OrderByDescending(u => u.Username == "hung.nv")
            .ThenByDescending(u => u.Username == "huyen.ntt")
            .FirstOrDefaultAsync(u => u.IsActive, cancellationToken);

        return fallbackUser?.Id ?? 1;
    }

    public static bool EvaluateStepCondition(string? conditionExpression, decimal numericMetric)
    {
        if (string.IsNullOrWhiteSpace(conditionExpression) ||
            conditionExpression.Equals("Always", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var parts = conditionExpression.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 3 && decimal.TryParse(parts[2], out var threshold))
        {
            var op = parts[1];
            return op switch
            {
                ">=" => numericMetric >= threshold,
                ">" => numericMetric > threshold,
                "<=" => numericMetric <= threshold,
                "<" => numericMetric < threshold,
                "==" or "=" => numericMetric == threshold,
                _ => true
            };
        }

        return true;
    }
}

// ============================================================================
// 3. QUERY: TRUNG TÂM QUY TRÌNH PHÊ DUYỆT ĐA BƯỚC (WORKFLOW CENTER)
// ============================================================================

public record GetWorkflowCenterQuery(
    string? EntityTypeFilter = null,
    string? StatusFilter = null) : IRequest<WorkflowCenterOverviewDto>;

public class GetWorkflowCenterQueryHandler : IRequestHandler<GetWorkflowCenterQuery, WorkflowCenterOverviewDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IEmailNotificationService _emailService;

    public GetWorkflowCenterQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IEmailNotificationService emailService)
    {
        _context = context;
        _currentUser = currentUser;
        _emailService = emailService;
    }

    public async Task<WorkflowCenterOverviewDto> Handle(GetWorkflowCenterQuery request, CancellationToken cancellationToken)
    {
        await WorkflowSeederHelper.EnsureDefaultWorkflowsAsync(_context, cancellationToken);

        var roles = await _context.Roles
            .AsNoTracking()
            .OrderBy(r => r.RoleCode)
            .Select(r => new SimpleLookupDto(r.Id, r.RoleCode, r.RoleName))
            .ToListAsync(cancellationToken);

        var usersRaw = await _context.Users
            .AsNoTracking()
            .Include(u => u.Employee)
            .Where(u => u.IsActive)
            .OrderBy(u => u.Username)
            .ToListAsync(cancellationToken);

        var users = usersRaw
            .Select(u => new SimpleLookupDto(
                u.Id,
                u.Username,
                u.Employee != null ? $"{u.Employee.FullName} ({u.Username})" : $"Quản trị hệ thống ({u.Username})"))
            .ToList();

        var roleMap = roles.ToDictionary(r => r.Id, r => r.Name);
        var userMap = users.ToDictionary(u => u.Id, u => u.Name);

        var definitionsRaw = await _context.WorkflowDefinitions
            .AsNoTracking()
            .Include(d => d.Steps)
            .Include(d => d.Instances)
            .OrderBy(d => d.WorkflowCode)
            .ToListAsync(cancellationToken);

        var definitions = definitionsRaw.Select(d => new WorkflowDefinitionDto(
            d.Id,
            d.WorkflowCode,
            d.WorkflowName,
            d.EntityType,
            d.Description,
            d.IsActive,
            d.Instances.Count,
            d.Steps.OrderBy(s => s.StepOrder).Select(s => new WorkflowStepDto(
                s.Id,
                s.StepOrder,
                s.StepName,
                s.ApproverType,
                s.ApproverRoleId,
                s.ApproverRoleId.HasValue && roleMap.TryGetValue(s.ApproverRoleId.Value, out var rn) ? rn : null,
                s.ApproverUserId,
                s.ApproverUserId.HasValue && userMap.TryGetValue(s.ApproverUserId.Value, out var un) ? un : null,
                s.ConditionExpression,
                s.SlaHours)).ToList()
        )).ToList();

        var instancesQuery = _context.WorkflowInstances
            .AsNoTracking()
            .Include(i => i.WorkflowDefinition)
                .ThenInclude(d => d.Steps)
            .Include(i => i.InitiatorEmployee)
                .ThenInclude(e => e.OrganizationUnit)
            .Include(i => i.ApprovalLogs)
                .ThenInclude(l => l.ApproverUser)
                    .ThenInclude(u => u.Employee)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.EntityTypeFilter))
            instancesQuery = instancesQuery.Where(i => i.EntityType == request.EntityTypeFilter);

        if (!string.IsNullOrWhiteSpace(request.StatusFilter))
            instancesQuery = instancesQuery.Where(i => i.Status == request.StatusFilter);

        var instancesRaw = await instancesQuery
            .OrderByDescending(i => i.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        // Tra cứu thông tin tóm tắt của các loại đơn
        var leaveDict = await _context.LeaveRequests
            .AsNoTracking()
            .Include(l => l.LeaveType)
            .ToDictionaryAsync(l => l.Id, cancellationToken);

        var otDict = await _context.OvertimeRequests
            .AsNoTracking()
            .ToDictionaryAsync(o => o.Id, cancellationToken);

        var dailyAttDict = await _context.DailyAttendances
            .AsNoTracking()
            .ToDictionaryAsync(d => (int)d.Id, cancellationToken);

        var transferDict = await _context.PersonnelTransfers
            .AsNoTracking()
            .ToDictionaryAsync(t => t.Id, cancellationToken);

        var payrollPeriodDict = await _context.PayrollPeriods
            .AsNoTracking()
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var instanceDtos = new List<WorkflowInstanceItemDto>();

        foreach (var inst in instancesRaw)
        {
            var stepsOrdered = inst.WorkflowDefinition.Steps.OrderBy(s => s.StepOrder).ToList();
            var currentStep = stepsOrdered.FirstOrDefault(s => s.StepOrder == inst.CurrentStepOrder)
                              ?? stepsOrdered.LastOrDefault();

            string docTitle = $"Đơn #{inst.EntityId}";
            string docSummary = inst.EntityType;
            string entityLabel = inst.EntityType;
            string targetUrl = "/Workflows";

            switch (inst.EntityType)
            {
                case "LeaveRequest":
                    entityLabel = "Đơn Nghỉ phép / Công tác";
                    targetUrl = "/Leaves";
                    if (leaveDict.TryGetValue(inst.EntityId, out var lv))
                    {
                        docTitle = $"{lv.LeaveType.LeaveTypeName} ({lv.TotalDays:0.#} ngày)";
                        docSummary = $"Từ {lv.StartDate:dd/MM/yyyy} đến {lv.EndDate:dd/MM/yyyy} • Lý do: {lv.Reason ?? "Theo kế hoạch cá nhân"}";
                    }
                    break;

                case "OvertimeRequest":
                    entityLabel = "Đơn Làm thêm giờ (OT)";
                    targetUrl = "/Attendance?tab=overtime";
                    if (otDict.TryGetValue(inst.EntityId, out var ot))
                    {
                        docTitle = $"Đăng ký OT {ot.OvertimeRateType} ({ot.RequestedHours:0.#} giờ)";
                        docSummary = $"Ngày {ot.OvertimeDate:dd/MM/yyyy} ({ot.StartTime:HH\\:mm} - {ot.EndTime:HH\\:mm}) • {ot.Reason ?? "Tăng ca sản xuất"}";
                    }
                    break;

                case "AttendanceExplanation":
                    entityLabel = "Giải trình Chấm công";
                    targetUrl = "/Attendance?tab=explanations";
                    if (dailyAttDict.TryGetValue(inst.EntityId, out var da))
                    {
                        docTitle = $"Giải trình công ngày {da.AttendanceDate:dd/MM/yyyy}";
                        docSummary = da.EditReason ?? "Bổ sung giờ vào/ra do quên chấm công hoặc đi công tác ngoài";
                    }
                    break;

                case "PersonnelTransfer":
                    entityLabel = "Tờ trình Điều chuyển / Bổ nhiệm";
                    targetUrl = "/Transfers";
                    if (transferDict.TryGetValue(inst.EntityId, out var tr))
                    {
                        docTitle = $"Quyết định {tr.DecisionNumber} ({tr.TransferType})";
                        docSummary = $"Hiệu lực {tr.EffectiveDate:dd/MM/yyyy} • Lý do: {tr.Reason ?? "Theo yêu cầu công tác"}";
                    }
                    break;

                case "PayrollPeriod":
                    entityLabel = "Phê duyệt Bảng lương";
                    targetUrl = $"/Payroll?periodId={inst.EntityId}";
                    if (payrollPeriodDict.TryGetValue(inst.EntityId, out var pp))
                    {
                        docTitle = $"{pp.PeriodTitle} ({pp.PeriodCode})";
                        docSummary = $"Trạng thái: {pp.Status} • Phiên bản tính: v{pp.CalculationVersion} • Chốt kỳ {pp.EndDate:dd/MM/yyyy}";
                    }
                    break;
            }

            var slaHours = currentStep?.SlaHours ?? 24;
            var isOverdue = inst.Status == "InProgress" &&
                            (DateTime.UtcNow - inst.CreatedAt).TotalHours > slaHours;

            var approverName = inst.CurrentApproverUserId.HasValue && userMap.TryGetValue(inst.CurrentApproverUserId.Value, out var apName)
                ? apName
                : "Ban Phê duyệt";

            var logs = inst.ApprovalLogs
                .OrderBy(l => l.ActionAt)
                .Select(l => new WorkflowApprovalLogDto(
                    l.Id,
                    l.StepOrder,
                    l.StepName,
                    l.ApproverUserId,
                    l.ApproverUser?.Employee?.FullName ?? l.ApproverUser?.Username ?? "Hệ thống",
                    l.Action,
                    l.Comment,
                    l.ActionAt))
                .ToList();

            instanceDtos.Add(new WorkflowInstanceItemDto(
                inst.Id,
                inst.WorkflowDefinition.WorkflowCode,
                inst.WorkflowDefinition.WorkflowName,
                inst.EntityType,
                entityLabel,
                inst.EntityId,
                docTitle,
                docSummary,
                inst.InitiatorEmployeeId,
                inst.InitiatorEmployee.EmployeeCode,
                inst.InitiatorEmployee.FullName,
                inst.InitiatorEmployee.OrganizationUnit?.UnitName ?? "Vinamilk",
                inst.CurrentStepOrder,
                Math.Max(1, stepsOrdered.Count),
                currentStep?.StepName ?? "Hoàn tất",
                inst.CurrentApproverUserId,
                approverName,
                inst.Status,
                inst.CreatedAt,
                inst.CompletedAt,
                slaHours,
                isOverdue,
                targetUrl,
                logs));
        }

        var currentUserId = _currentUser.UserId;
        var allInstancesStats = await _context.WorkflowInstances
            .AsNoTracking()
            .Select(x => new { x.Status, x.CurrentApproverUserId })
            .ToListAsync(cancellationToken);

        var delegatedCount = await _context.WorkflowApprovalLogs
            .AsNoTracking()
            .CountAsync(l => l.Action == "Delegated", cancellationToken);

        return new WorkflowCenterOverviewDto(
            definitions,
            instanceDtos,
            roles,
            users,
            _emailService.GetRecentSentEmails(),
            allInstancesStats.Count(x => x.Status == "InProgress"),
            allInstancesStats.Count(x => x.Status == "InProgress" && (!currentUserId.HasValue || x.CurrentApproverUserId == currentUserId.Value)),
            allInstancesStats.Count(x => x.Status == "Approved"),
            allInstancesStats.Count(x => x.Status == "Rejected"),
            delegatedCount);
    }
}

// ============================================================================
// 4. COMMAND: LƯU ĐỊNH NGHĨA QUY TRÌNH & CÁC BƯỚC DUYỆT
// ============================================================================

public record SaveWorkflowStepInput(
    int StepOrder,
    string StepName,
    string ApproverType,
    int? ApproverRoleId,
    int? ApproverUserId,
    string? ConditionExpression,
    int SlaHours);

public record SaveWorkflowDefinitionCommand(
    int? Id,
    string WorkflowCode,
    string WorkflowName,
    string EntityType,
    string? Description,
    bool IsActive,
    IReadOnlyList<SaveWorkflowStepInput> Steps) : IRequest<int>;

public class SaveWorkflowDefinitionCommandValidator : AbstractValidator<SaveWorkflowDefinitionCommand>
{
    public SaveWorkflowDefinitionCommandValidator()
    {
        RuleFor(x => x.WorkflowCode).NotEmpty().WithMessage("Mã quy trình không được để trống.");
        RuleFor(x => x.WorkflowName).NotEmpty().WithMessage("Tên quy trình không được để trống.");
        RuleFor(x => x.EntityType).NotEmpty().WithMessage("Loại chứng từ áp dụng không được để trống.");
        RuleFor(x => x.Steps).NotEmpty().WithMessage("Quy trình phải có ít nhất 1 bước phê duyệt.");
    }
}

public class SaveWorkflowDefinitionCommandHandler : IRequestHandler<SaveWorkflowDefinitionCommand, int>
{
    private readonly IApplicationDbContext _context;

    public SaveWorkflowDefinitionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(SaveWorkflowDefinitionCommand request, CancellationToken cancellationToken)
    {
        WorkflowDefinition definition;
        if (request.Id.HasValue && request.Id.Value > 0)
        {
            definition = await _context.WorkflowDefinitions
                .Include(d => d.Steps)
                .FirstOrDefaultAsync(d => d.Id == request.Id.Value, cancellationToken)
                ?? throw new KeyNotFoundException("Không tìm thấy định nghĩa quy trình.");

            definition.WorkflowName = request.WorkflowName.Trim();
            definition.EntityType = request.EntityType.Trim();
            definition.Description = request.Description?.Trim();
            definition.IsActive = request.IsActive;

            foreach (var oldStep in definition.Steps.ToList())
            {
                oldStep.IsDeleted = true;
            }
        }
        else
        {
            definition = new WorkflowDefinition
            {
                WorkflowCode = request.WorkflowCode.Trim().ToUpperInvariant(),
                WorkflowName = request.WorkflowName.Trim(),
                EntityType = request.EntityType.Trim(),
                Description = request.Description?.Trim(),
                IsActive = request.IsActive
            };
            await _context.WorkflowDefinitions.AddAsync(definition, cancellationToken);
        }

        int order = 1;
        foreach (var stepInput in request.Steps.OrderBy(s => s.StepOrder))
        {
            definition.Steps.Add(new WorkflowStep
            {
                StepOrder = order++,
                StepName = stepInput.StepName.Trim(),
                ApproverType = stepInput.ApproverType,
                ApproverRoleId = stepInput.ApproverRoleId,
                ApproverUserId = stepInput.ApproverUserId,
                ConditionExpression = string.IsNullOrWhiteSpace(stepInput.ConditionExpression) ? "Always" : stepInput.ConditionExpression.Trim(),
                SlaHours = Math.Max(1, stepInput.SlaHours)
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        return definition.Id;
    }
}

// ============================================================================
// 5. COMMAND: KHỞI TẠO LUỒNG PHÊ DUYỆT (START WORKFLOW INSTANCE)
// ============================================================================

public record StartWorkflowInstanceCommand(
    string EntityType,
    int EntityId,
    int InitiatorEmployeeId,
    string DocumentTitle,
    decimal NumericConditionValue = 1m,
    string? TargetUrl = "/Workflows") : IRequest<int>;

public class StartWorkflowInstanceCommandHandler : IRequestHandler<StartWorkflowInstanceCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IRealtimeNotificationDispatcher _realtimeDispatcher;
    private readonly IEmailNotificationService _emailService;

    public StartWorkflowInstanceCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IRealtimeNotificationDispatcher realtimeDispatcher,
        IEmailNotificationService emailService)
    {
        _context = context;
        _currentUser = currentUser;
        _realtimeDispatcher = realtimeDispatcher;
        _emailService = emailService;
    }

    public async Task<int> Handle(StartWorkflowInstanceCommand request, CancellationToken cancellationToken)
    {
        await WorkflowSeederHelper.EnsureDefaultWorkflowsAsync(_context, cancellationToken);

        var definition = await _context.WorkflowDefinitions
            .Include(d => d.Steps)
            .FirstOrDefaultAsync(d => d.EntityType == request.EntityType && d.IsActive, cancellationToken)
            ?? await _context.WorkflowDefinitions
                .Include(d => d.Steps)
                .FirstAsync(cancellationToken);

        var firstStep = definition.Steps.OrderBy(s => s.StepOrder).First();
        var approverUserId = await WorkflowSeederHelper.ResolveApproverUserIdAsync(
            _context, firstStep, request.InitiatorEmployeeId, cancellationToken);

        var initiatorEmp = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.InitiatorEmployeeId, cancellationToken);

        var submitterUserId = _currentUser.UserId ?? approverUserId;

        var instance = new WorkflowInstance
        {
            WorkflowDefinitionId = definition.Id,
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            InitiatorEmployeeId = request.InitiatorEmployeeId,
            CurrentStepOrder = firstStep.StepOrder,
            CurrentApproverUserId = approverUserId,
            Status = "InProgress"
        };

        instance.ApprovalLogs.Add(new WorkflowApprovalLog
        {
            StepOrder = 0,
            StepName = "Khởi tạo & Trình duyệt đơn",
            ApproverUserId = submitterUserId,
            Action = "Submitted",
            Comment = $"Đệ trình: {request.DocumentTitle}",
            ActionAt = DateTime.UtcNow
        });

        await _context.WorkflowInstances.AddAsync(instance, cancellationToken);

        // Tạo thông báo DB cho người phê duyệt bước 1
        var notificationTitle = $"[Cần duyệt] {request.DocumentTitle}";
        var notificationMsg = $"{initiatorEmp?.FullName ?? "Nhân viên"} ({initiatorEmp?.EmployeeCode}) vừa gửi đơn chờ bạn phê duyệt tại {firstStep.StepName}.";

        await _context.Notifications.AddAsync(new Notification
        {
            RecipientUserId = approverUserId,
            Title = notificationTitle,
            Message = notificationMsg,
            NotificationType = "WorkflowApproval",
            TargetUrl = request.TargetUrl ?? "/Workflows",
            IsRead = false
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        // Gửi thông báo Realtime SignalR + Email cho người duyệt
        var approverUser = await _context.Users
            .AsNoTracking()
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Id == approverUserId, cancellationToken);

        await _realtimeDispatcher.DispatchToUserAsync(
            approverUserId,
            notificationTitle,
            notificationMsg,
            "WorkflowApproval",
            request.TargetUrl ?? "/Workflows",
            cancellationToken);

        await _emailService.SendWorkflowEmailAsync(
            approverUser?.Email ?? "approver@vinamilk.com.vn",
            approverUser?.Employee?.FullName ?? approverUser?.Username ?? "Cấp phê duyệt",
            notificationTitle,
            request.DocumentTitle,
            initiatorEmp?.FullName ?? "Nhân viên Vinamilk",
            firstStep.StepName,
            "Chờ phê duyệt",
            request.TargetUrl ?? "/Workflows",
            notificationMsg,
            cancellationToken);

        return instance.Id;
    }
}

// ============================================================================
// 6. COMMAND: XỬ LÝ DUYỆT / TỪ CHỐI / ỦY QUYỀN / HỦY ĐƠN
// ============================================================================

public record ProcessWorkflowActionCommand(
    int WorkflowInstanceId,
    string Action, // Approved, Rejected, Delegated, Cancelled
    string? Comment,
    int? DelegateToUserId = null) : IRequest<string>;

public class ProcessWorkflowActionCommandHandler : IRequestHandler<ProcessWorkflowActionCommand, string>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IRealtimeNotificationDispatcher _realtimeDispatcher;
    private readonly IEmailNotificationService _emailService;

    public ProcessWorkflowActionCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IRealtimeNotificationDispatcher realtimeDispatcher,
        IEmailNotificationService emailService)
    {
        _context = context;
        _currentUser = currentUser;
        _realtimeDispatcher = realtimeDispatcher;
        _emailService = emailService;
    }

    public async Task<string> Handle(ProcessWorkflowActionCommand request, CancellationToken cancellationToken)
    {
        var instance = await _context.WorkflowInstances
            .Include(i => i.WorkflowDefinition)
                .ThenInclude(d => d.Steps)
            .Include(i => i.InitiatorEmployee)
            .Include(i => i.ApprovalLogs)
            .FirstOrDefaultAsync(i => i.Id == request.WorkflowInstanceId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy luồng phê duyệt.");

        if (instance.Status != "InProgress")
            throw new InvalidOperationException($"Đơn này đã ở trạng thái '{instance.Status}', không thể xử lý tiếp.");

        if (!request.Action.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) &&
            _currentUser.EmployeeId.HasValue &&
            _currentUser.EmployeeId.Value == instance.InitiatorEmployeeId)
        {
            throw new InvalidOperationException("Bạn không thể tự phê duyệt, từ chối hoặc ủy quyền đơn do chính mình khởi tạo.");
        }

        if (instance.EntityType == "PayrollPeriod" &&
            !request.Action.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) &&
            _currentUser.UserId.HasValue)
        {
            var payrollPeriodGuard = await _context.PayrollPeriods
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == instance.EntityId, cancellationToken);

            if (payrollPeriodGuard is not null &&
                (payrollPeriodGuard.CalculatedByUserId == _currentUser.UserId.Value ||
                 payrollPeriodGuard.SubmittedByUserId == _currentUser.UserId.Value))
            {
                throw new InvalidOperationException("Vi phạm phân tách nhiệm vụ: người tính hoặc trình bảng lương không được tự phê duyệt kỳ lương.");
            }
        }

        var actorUserId = _currentUser.UserId ?? instance.CurrentApproverUserId ?? 1;
        var orderedSteps = instance.WorkflowDefinition.Steps.OrderBy(s => s.StepOrder).ToList();
        var currentStep = orderedSteps.FirstOrDefault(s => s.StepOrder == instance.CurrentStepOrder)
                          ?? orderedSteps.First();

        // 1. Trường hợp ỦY QUYỀN DUYỆT (DELEGATED)
        if (request.Action.Equals("Delegated", StringComparison.OrdinalIgnoreCase))
        {
            if (!request.DelegateToUserId.HasValue || request.DelegateToUserId.Value <= 0)
                throw new InvalidOperationException("Vui lòng chọn người nhận ủy quyền phê duyệt.");

            var delegateUser = await _context.Users
                .AsNoTracking()
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.Id == request.DelegateToUserId.Value, cancellationToken)
                ?? throw new KeyNotFoundException("Không tìm thấy người dùng nhận ủy quyền.");

            instance.CurrentApproverUserId = delegateUser.Id;
            var delegateName = delegateUser.Employee?.FullName ?? delegateUser.Username;

            instance.ApprovalLogs.Add(new WorkflowApprovalLog
            {
                StepOrder = currentStep.StepOrder,
                StepName = $"{currentStep.StepName} (Ủy quyền)",
                ApproverUserId = actorUserId,
                Action = "Delegated",
                Comment = $"Ủy quyền cho {delegateName}. Ghi chú: {request.Comment ?? "Ủy quyền xử lý thay"}",
                ActionAt = DateTime.UtcNow
            });

            var notifyTitle = $"[Ủy quyền duyệt] Đơn #{instance.EntityId} ({instance.EntityType})";
            var notifyMsg = $"Bạn vừa được ủy quyền phê duyệt đơn của {instance.InitiatorEmployee.FullName} tại {currentStep.StepName}.";

            await _context.Notifications.AddAsync(new Notification
            {
                RecipientUserId = delegateUser.Id,
                Title = notifyTitle,
                Message = notifyMsg,
                NotificationType = "WorkflowDelegation",
                TargetUrl = "/Workflows",
                IsRead = false
            }, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            await _realtimeDispatcher.DispatchToUserAsync(
                delegateUser.Id, notifyTitle, notifyMsg, "WorkflowDelegation", "/Workflows", cancellationToken);

            await _emailService.SendWorkflowEmailAsync(
                delegateUser.Email,
                delegateName,
                notifyTitle,
                $"Đơn #{instance.EntityId} ({instance.WorkflowDefinition.WorkflowName})",
                instance.InitiatorEmployee.FullName,
                currentStep.StepName,
                "Được ủy quyền duyệt",
                "/Workflows",
                request.Comment,
                cancellationToken);

            return $"Đã ủy quyền phê duyệt thành công cho {delegateName}.";
        }

        // 2. Trường hợp TỪ CHỐI (REJECTED) hoặc HỦY (CANCELLED)
        if (request.Action.Equals("Rejected", StringComparison.OrdinalIgnoreCase) ||
            request.Action.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
        {
            var isReject = request.Action.Equals("Rejected", StringComparison.OrdinalIgnoreCase);
            instance.Status = isReject ? "Rejected" : "Cancelled";
            instance.CompletedAt = DateTime.UtcNow;

            instance.ApprovalLogs.Add(new WorkflowApprovalLog
            {
                StepOrder = currentStep.StepOrder,
                StepName = currentStep.StepName,
                ApproverUserId = actorUserId,
                Action = instance.Status,
                Comment = request.Comment ?? (isReject ? "Không đồng ý phê duyệt" : "Người tạo hủy đơn"),
                ActionAt = DateTime.UtcNow
            });

            await ApplyEntityRejectionAsync(instance, request.Comment, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            await NotifyInitiatorAsync(
                instance,
                isReject ? "Bị từ chối" : "Đã hủy",
                request.Comment ?? "Đơn không được phê duyệt.",
                cancellationToken);

            return isReject ? "Đã từ chối đơn và hoàn trả hạn mức liên quan." : "Đã hủy đơn thành công.";
        }

        // 3. Trường hợp PHÊ DUYỆT (APPROVED)
        instance.ApprovalLogs.Add(new WorkflowApprovalLog
        {
            StepOrder = currentStep.StepOrder,
            StepName = currentStep.StepName,
            ApproverUserId = actorUserId,
            Action = "Approved",
            Comment = request.Comment ?? "Đồng ý phê duyệt",
            ActionAt = DateTime.UtcNow
        });

        decimal metricValue = await GetEntityMetricValueAsync(instance, cancellationToken);

        var nextStep = orderedSteps
            .Where(s => s.StepOrder > instance.CurrentStepOrder)
            .FirstOrDefault(s => WorkflowSeederHelper.EvaluateStepCondition(s.ConditionExpression, metricValue));

        if (nextStep != null)
        {
            var nextApproverUserId = await WorkflowSeederHelper.ResolveApproverUserIdAsync(
                _context, nextStep, instance.InitiatorEmployeeId, cancellationToken);

            instance.CurrentStepOrder = nextStep.StepOrder;
            instance.CurrentApproverUserId = nextApproverUserId;

            var nextTitle = $"[Chuyển bước {nextStep.StepOrder}] Đơn của {instance.InitiatorEmployee.FullName}";
            var nextMsg = $"Bước {currentStep.StepOrder} đã duyệt. Mời bạn phê duyệt tiếp tại {nextStep.StepName}.";

            await _context.Notifications.AddAsync(new Notification
            {
                RecipientUserId = nextApproverUserId,
                Title = nextTitle,
                Message = nextMsg,
                NotificationType = "WorkflowApproval",
                TargetUrl = "/Workflows",
                IsRead = false
            }, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            var nextUser = await _context.Users
                .AsNoTracking()
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.Id == nextApproverUserId, cancellationToken);

            await _realtimeDispatcher.DispatchToUserAsync(
                nextApproverUserId, nextTitle, nextMsg, "WorkflowApproval", "/Workflows", cancellationToken);

            await _emailService.SendWorkflowEmailAsync(
                nextUser?.Email ?? "hr@vinamilk.com.vn",
                nextUser?.Employee?.FullName ?? nextUser?.Username ?? "Cấp phê duyệt tiếp theo",
                nextTitle,
                $"{instance.WorkflowDefinition.WorkflowName} (#{instance.EntityId})",
                instance.InitiatorEmployee.FullName,
                nextStep.StepName,
                $"Chờ duyệt Bước {nextStep.StepOrder}",
                "/Workflows",
                request.Comment,
                cancellationToken);

            return $"Đã duyệt Bước {currentStep.StepOrder}. Đơn đã chuyển sang {nextStep.StepName}.";
        }

        // Hoàn tất toàn bộ các bước trong quy trình
        instance.Status = "Approved";
        instance.CompletedAt = DateTime.UtcNow;

        await ApplyEntityFinalApprovalAsync(instance, actorUserId, request.Comment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await NotifyInitiatorAsync(
            instance,
            "Đã phê duyệt hoàn tất",
            request.Comment ?? "Đơn của bạn đã được phê duyệt đầy đủ các cấp và cập nhật vào hệ thống.",
            cancellationToken);

        return "Đã phê duyệt hoàn tất quy trình và tự động cập nhật dữ liệu nghiệp vụ!";
    }

    private async Task<decimal> GetEntityMetricValueAsync(WorkflowInstance instance, CancellationToken cancellationToken)
    {
        if (instance.EntityType == "LeaveRequest")
        {
            var lv = await _context.LeaveRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == instance.EntityId, cancellationToken);
            return lv?.TotalDays ?? 1m;
        }
        if (instance.EntityType == "OvertimeRequest")
        {
            var ot = await _context.OvertimeRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == instance.EntityId, cancellationToken);
            return ot?.RequestedHours ?? 1m;
        }
        return 1m;
    }

    private async Task ApplyEntityFinalApprovalAsync(
        WorkflowInstance instance,
        int approverUserId,
        string? comment,
        CancellationToken cancellationToken)
    {
        var approverEmpId = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == approverUserId)
            .Select(u => u.EmployeeId)
            .FirstOrDefaultAsync(cancellationToken);

        switch (instance.EntityType)
        {
            case "LeaveRequest":
                var leaveReq = await _context.LeaveRequests
                    .Include(l => l.LeaveType)
                    .FirstOrDefaultAsync(l => l.Id == instance.EntityId, cancellationToken);

                if (leaveReq != null && leaveReq.Status != "Approved")
                {
                    leaveReq.Status = "Approved";
                    leaveReq.ApprovedByEmployeeId = approverEmpId;
                    leaveReq.ApproverComment = comment ?? "Đã duyệt qua Workflow";
                    leaveReq.ApprovedAt = DateTime.UtcNow;

                    // Khấu trừ quỹ phép nếu là phép năm
                    var balance = await _context.LeaveBalances
                        .FirstOrDefaultAsync(b =>
                            b.EmployeeId == leaveReq.EmployeeId &&
                            b.LeaveTypeId == leaveReq.LeaveTypeId &&
                            b.Year == leaveReq.StartDate.Year,
                            cancellationToken);

                    if (balance != null)
                    {
                        balance.PendingDays = Math.Max(0m, balance.PendingDays - leaveReq.TotalDays);
                        balance.UsedDays += leaveReq.TotalDays;
                        balance.RemainingDays = Math.Max(0m,
                            balance.StandardEntitlement + balance.SeniorityExtraDays + balance.CarriedForwardDays - balance.UsedDays - balance.PendingDays);
                    }

                    // Tự động điền ký hiệu nghỉ phép/công tác vào Bảng chấm công ngày (DailyAttendances)
                    var symbol = leaveReq.LeaveType?.AttendanceSymbol ?? "P";
                    for (var dt = leaveReq.StartDate; dt <= leaveReq.EndDate; dt = dt.AddDays(1))
                    {
                        if (dt.DayOfWeek == DayOfWeek.Sunday)
                            continue;

                        var daily = await _context.DailyAttendances
                            .FirstOrDefaultAsync(d => d.EmployeeId == leaveReq.EmployeeId && d.AttendanceDate == dt, cancellationToken);

                        var isHalfDay = (dt == leaveReq.StartDate && leaveReq.StartSession != "FullDay") ||
                                        (dt == leaveReq.EndDate && leaveReq.EndSession != "FullDay");

                        if (daily == null)
                        {
                            await _context.DailyAttendances.AddAsync(new DailyAttendance
                            {
                                EmployeeId = leaveReq.EmployeeId,
                                AttendanceDate = dt,
                                AttendanceSymbol = isHalfDay ? $"{symbol}/2" : symbol,
                                WorkingHours = isHalfDay ? 4m : 0m,
                                WorkdayCount = leaveReq.LeaveType?.IsPaidByCompany == true ? (isHalfDay ? 0.5m : 1.0m) : 0m,
                                EditReason = $"Đơn nghỉ #{leaveReq.Id}: {leaveReq.LeaveType?.LeaveTypeName}"
                            }, cancellationToken);
                        }
                        else
                        {
                            daily.AttendanceSymbol = isHalfDay ? $"{symbol}/2" : symbol;
                            daily.WorkdayCount = leaveReq.LeaveType?.IsPaidByCompany == true ? 1.0m : 0m;
                            daily.LateMinutes = 0;
                            daily.EarlyLeaveMinutes = 0;
                            daily.EditReason = $"Đơn nghỉ #{leaveReq.Id}: {leaveReq.LeaveType?.LeaveTypeName}";
                        }
                    }
                }
                break;

            case "OvertimeRequest":
                var otReq = await _context.OvertimeRequests
                    .FirstOrDefaultAsync(o => o.Id == instance.EntityId, cancellationToken);

                if (otReq != null && otReq.Status != "Approved")
                {
                    otReq.Status = "Approved";
                    otReq.ApprovedHours = otReq.RequestedHours;
                    otReq.ApprovedByEmployeeId = approverEmpId;

                    // Tự động cộng giờ OT vào DailyAttendance
                    var daily = await _context.DailyAttendances
                        .FirstOrDefaultAsync(d => d.EmployeeId == otReq.EmployeeId && d.AttendanceDate == otReq.OvertimeDate, cancellationToken);

                    if (daily == null)
                    {
                        daily = new DailyAttendance
                        {
                            EmployeeId = otReq.EmployeeId,
                            AttendanceDate = otReq.OvertimeDate,
                            AttendanceSymbol = "OT",
                            WorkingHours = 8m,
                            WorkdayCount = 1m
                        };
                        await _context.DailyAttendances.AddAsync(daily, cancellationToken);
                    }

                    if (otReq.OvertimeRateType == "OT300")
                        daily.OvertimeHours300 = otReq.ApprovedHours;
                    else if (otReq.OvertimeRateType == "OT200")
                        daily.OvertimeHours200 = otReq.ApprovedHours;
                    else
                        daily.OvertimeHours150 = otReq.ApprovedHours;
                }
                break;

            case "AttendanceExplanation":
                var attRecord = await _context.DailyAttendances
                    .FirstOrDefaultAsync(d => d.Id == instance.EntityId, cancellationToken);

                if (attRecord != null)
                {
                    attRecord.IsManualEdited = true;
                    attRecord.LateMinutes = 0;
                    attRecord.EarlyLeaveMinutes = 0;
                    attRecord.WorkingHours = 8.0m;
                    attRecord.WorkdayCount = 1.0m;
                    if (attRecord.AttendanceSymbol is "V" or "M" or "X/2")
                        attRecord.AttendanceSymbol = "X";
                    attRecord.CheckInTime ??= attRecord.AttendanceDate.ToDateTime(new TimeOnly(8, 0));
                    attRecord.CheckOutTime ??= attRecord.AttendanceDate.ToDateTime(new TimeOnly(17, 0));
                    attRecord.EditReason = $"[Đã duyệt giải trình] {attRecord.EditReason}";
                }
                break;

            case "PersonnelTransfer":
                var transfer = await _context.PersonnelTransfers
                    .FirstOrDefaultAsync(t => t.Id == instance.EntityId, cancellationToken);
                if (transfer != null && transfer.Status == "PendingApproval")
                {
                    transfer.Status = "Approved";
                    transfer.ApprovedByEmployeeId = approverEmpId;
                    transfer.AppliedAt = DateTime.UtcNow;
                }
                break;

            case "PayrollPeriod":
                var approvedPeriod = await _context.PayrollPeriods
                    .FirstOrDefaultAsync(p => p.Id == instance.EntityId, cancellationToken);
                if (approvedPeriod != null && approvedPeriod.Status == "Submitted")
                {
                    approvedPeriod.Status = "Approved";
                    approvedPeriod.ApprovedByUserId = approverUserId;
                    approvedPeriod.ApprovedAt = DateTime.UtcNow;
                    await _context.AuditLogs.AddAsync(new AuditLog
                    {
                        UserId = _currentUser.UserId ?? approverUserId,
                        Username = string.IsNullOrWhiteSpace(_currentUser.Username) ? "SYSTEM" : _currentUser.Username,
                        ModuleName = "Payroll",
                        ActionType = "PayrollPeriod_Approve",
                        EntityName = nameof(Domain.Entities.Payroll.PayrollPeriod),
                        EntityId = approvedPeriod.Id.ToString(),
                        OldValuesJson = "{\"Status\":\"Submitted\"}",
                        NewValuesJson = $"{{\"Status\":\"Approved\",\"ApprovedByUserId\":{approverUserId}}}",
                        AffectedColumns = "Status,ApprovedByUserId,ApprovedAt",
                        IpAddress = _currentUser.IpAddress ?? "127.0.0.1",
                        UserAgent = _currentUser.UserAgent ?? "WorkflowEngine",
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = string.IsNullOrWhiteSpace(_currentUser.Username) ? "SYSTEM" : _currentUser.Username
                    }, cancellationToken);
                }
                break;
        }
    }

    private async Task ApplyEntityRejectionAsync(
        WorkflowInstance instance,
        string? comment,
        CancellationToken cancellationToken)
    {
        switch (instance.EntityType)
        {
            case "LeaveRequest":
                var leaveReq = await _context.LeaveRequests
                    .FirstOrDefaultAsync(l => l.Id == instance.EntityId, cancellationToken);
                if (leaveReq != null && leaveReq.Status == "Pending")
                {
                    leaveReq.Status = instance.Status;
                    leaveReq.ApproverComment = comment;

                    var balance = await _context.LeaveBalances
                        .FirstOrDefaultAsync(b =>
                            b.EmployeeId == leaveReq.EmployeeId &&
                            b.LeaveTypeId == leaveReq.LeaveTypeId &&
                            b.Year == leaveReq.StartDate.Year,
                            cancellationToken);

                    if (balance != null)
                    {
                        balance.PendingDays = Math.Max(0m, balance.PendingDays - leaveReq.TotalDays);
                        balance.RemainingDays = Math.Max(0m,
                            balance.StandardEntitlement + balance.SeniorityExtraDays + balance.CarriedForwardDays - balance.UsedDays - balance.PendingDays);
                    }
                }
                break;

            case "OvertimeRequest":
                var otReq = await _context.OvertimeRequests
                    .FirstOrDefaultAsync(o => o.Id == instance.EntityId, cancellationToken);
                if (otReq != null)
                {
                    otReq.Status = instance.Status;
                }
                break;

            case "AttendanceExplanation":
                var attRecord = await _context.DailyAttendances
                    .FirstOrDefaultAsync(d => d.Id == instance.EntityId, cancellationToken);
                if (attRecord != null)
                {
                    attRecord.EditReason = $"[Từ chối giải trình: {comment}] {attRecord.EditReason}";
                }
                break;

            case "PayrollPeriod":
                var rejectedPeriod = await _context.PayrollPeriods
                    .FirstOrDefaultAsync(p => p.Id == instance.EntityId, cancellationToken);
                if (rejectedPeriod != null && rejectedPeriod.Status == "Submitted")
                {
                    rejectedPeriod.Status = "Calculated";
                    await _context.AuditLogs.AddAsync(new AuditLog
                    {
                        UserId = _currentUser.UserId,
                        Username = string.IsNullOrWhiteSpace(_currentUser.Username) ? "SYSTEM" : _currentUser.Username,
                        ModuleName = "Payroll",
                        ActionType = "PayrollPeriod_Reject",
                        EntityName = nameof(Domain.Entities.Payroll.PayrollPeriod),
                        EntityId = rejectedPeriod.Id.ToString(),
                        OldValuesJson = "{\"Status\":\"Submitted\"}",
                        NewValuesJson = "{\"Status\":\"Calculated\"}",
                        AffectedColumns = "Status",
                        IpAddress = _currentUser.IpAddress ?? "127.0.0.1",
                        UserAgent = _currentUser.UserAgent ?? "WorkflowEngine",
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = string.IsNullOrWhiteSpace(_currentUser.Username) ? "SYSTEM" : _currentUser.Username
                    }, cancellationToken);
                }
                break;
        }
    }

    private async Task NotifyInitiatorAsync(
        WorkflowInstance instance,
        string statusText,
        string detailMessage,
        CancellationToken cancellationToken)
    {
        var initiatorUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.EmployeeId == instance.InitiatorEmployeeId, cancellationToken);

        var recipientUserId = initiatorUser?.Id ?? 1;
        var title = $"[{statusText}] {instance.WorkflowDefinition.WorkflowName} (#{instance.EntityId})";

        await _context.Notifications.AddAsync(new Notification
        {
            RecipientUserId = recipientUserId,
            Title = title,
            Message = detailMessage,
            NotificationType = "WorkflowResult",
            TargetUrl = "/Workflows",
            IsRead = false
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        await _realtimeDispatcher.DispatchToUserAsync(
            recipientUserId, title, detailMessage, "WorkflowResult", "/Workflows", cancellationToken);

        await _realtimeDispatcher.BroadcastWorkflowUpdateAsync(
            instance.Id, instance.EntityType, instance.EntityId, instance.Status, title, cancellationToken);

        await _emailService.SendWorkflowEmailAsync(
            instance.InitiatorEmployee.WorkEmail ?? "employee@vinamilk.com.vn",
            instance.InitiatorEmployee.FullName,
            title,
            $"{instance.WorkflowDefinition.WorkflowName} (#{instance.EntityId})",
            instance.InitiatorEmployee.FullName,
            "Kết quả phê duyệt cuối cùng",
            statusText,
            "/Workflows",
            detailMessage,
            cancellationToken);
    }
}
