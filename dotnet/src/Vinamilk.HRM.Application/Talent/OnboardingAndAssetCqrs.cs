using MediatR;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Application.Workflows;
using Vinamilk.HRM.Domain.Entities.Talent;

namespace Vinamilk.HRM.Application.Talent;

// ============================================================================
// 1. DTOs CHO ONBOARDING / OFFBOARDING CHECKLIST & BÀN GIAO TÀI SẢN
// ============================================================================

public record OnboardingTemplateDto(
    int Id,
    string TemplateCode,
    string TaskName,
    string ProcessType,
    string AssignedDepartmentRole,
    int DueDaysOffset,
    int SortOrder);

public record EmployeeOnboardingTaskDto(
    int Id,
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string UnitName,
    string ProcessType,
    string TaskName,
    int? PicEmployeeId,
    string PicName,
    DateOnly DueDate,
    bool IsCompleted,
    DateTime? CompletedAt,
    string? Notes);

public record AssetItemDto(
    int Id,
    string AssetCode,
    string AssetName,
    string AssetCategory,
    string? SerialNumber,
    decimal PurchaseValue,
    DateOnly? PurchaseDate,
    string Status,
    int? AssignedToEmployeeId,
    string? AssignedToEmployeeName,
    DateOnly? AssignedDate);

public record OnboardingAndAssetOverviewDto(
    IReadOnlyList<OnboardingTemplateDto> Templates,
    IReadOnlyList<EmployeeOnboardingTaskDto> Tasks,
    IReadOnlyList<AssetItemDto> Assets,
    IReadOnlyList<SimpleLookupDto> ActiveEmployees,
    int TotalOnboardingTasks,
    int CompletedTasksCount,
    int AvailableAssetsCount,
    int AssignedAssetsCount);

// ============================================================================
// 2. AUTO-SEED MẪU CHECKLIST ONBOARDING/OFFBOARDING & TÀI SẢN DOANH NGHIỆP
// ============================================================================

public static class OnboardingAssetSeeder
{
    public static async Task EnsureSeededAsync(IApplicationDbContext context, CancellationToken cancellationToken)
    {
        if (!await context.OnboardingTemplates.AnyAsync(cancellationToken))
        {
            var templates = new List<OnboardingTemplate>
            {
                new() { TemplateCode = "ONB_01", TaskName = "Tiếp nhận hồ sơ gốc, CCCD & Ký Hợp đồng Thử việc", ProcessType = "Onboarding", AssignedDepartmentRole = "HR", DueDaysOffset = 1, SortOrder = 1 },
                new() { TemplateCode = "ONB_02", TaskName = "Cấp phát Email @vinamilk.com.vn, tài khoản ERP/HRM & 2FA", ProcessType = "Onboarding", AssignedDepartmentRole = "IT", DueDaysOffset = 1, SortOrder = 2 },
                new() { TemplateCode = "ONB_03", TaskName = "Cấp phát Laptop, Thẻ nhân viên & Đồng phục bảo hộ", ProcessType = "Onboarding", AssignedDepartmentRole = "Admin", DueDaysOffset = 2, SortOrder = 3 },
                new() { TemplateCode = "ONB_04", TaskName = "Đào tạo Hội nhập Văn hóa Vinamilk & An toàn lao động", ProcessType = "Onboarding", AssignedDepartmentRole = "HR", DueDaysOffset = 5, SortOrder = 4 },
                new() { TemplateCode = "OFF_01", TaskName = "Bàn giao công việc, tài liệu dự án cho Quản lý trực tiếp", ProcessType = "Offboarding", AssignedDepartmentRole = "Manager", DueDaysOffset = 3, SortOrder = 1 },
                new() { TemplateCode = "OFF_02", TaskName = "Thu hồi Laptop, Thẻ nhân viên & Tài sản công ty", ProcessType = "Offboarding", AssignedDepartmentRole = "Admin", DueDaysOffset = 5, SortOrder = 2 },
                new() { TemplateCode = "OFF_03", TaskName = "Khóa tài khoản Email, VPN & hệ thống HRM", ProcessType = "Offboarding", AssignedDepartmentRole = "IT", DueDaysOffset = 5, SortOrder = 3 },
                new() { TemplateCode = "OFF_04", TaskName = "Chốt sổ BHXH & Quyết toán lương nghỉ việc", ProcessType = "Offboarding", AssignedDepartmentRole = "C&B", DueDaysOffset = 7, SortOrder = 4 }
            };
            await context.OnboardingTemplates.AddRangeAsync(templates, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        if (!await context.Assets.AnyAsync(cancellationToken))
        {
            var emps = await context.Employees.AsNoTracking().OrderBy(e => e.EmployeeCode).Take(4).ToListAsync(cancellationToken);
            var assets = new List<Asset>
            {
                new() { AssetCode = "TS-LAP-001", AssetName = "Dell Latitude 7440 Core i7", AssetCategory = "Laptop", SerialNumber = "DL7440-VN01", PurchaseValue = 32000000m, PurchaseDate = new DateOnly(2025, 2, 10), Status = "Assigned" },
                new() { AssetCode = "TS-LAP-002", AssetName = "MacBook Pro 14 M3 Pro", AssetCategory = "Laptop", SerialNumber = "MBP-M3-088", PurchaseValue = 48000000m, PurchaseDate = new DateOnly(2025, 6, 15), Status = "Assigned" },
                new() { AssetCode = "TS-LAP-003", AssetName = "ThinkPad X1 Carbon Gen 12", AssetCategory = "Laptop", SerialNumber = "TPX1-2026-05", PurchaseValue = 38500000m, PurchaseDate = new DateOnly(2026, 1, 20), Status = "Available" },
                new() { AssetCode = "TS-CARD-010", AssetName = "Thẻ từ ra vào Vinamilk Tower & Smart Parking", AssetCategory = "AccessCard", SerialNumber = "RFID-9910", PurchaseValue = 250000m, PurchaseDate = new DateOnly(2026, 1, 5), Status = "Available" },
                new() { AssetCode = "TS-PPE-015", AssetName = "Bộ Đồng phục & Bảo hộ Phòng sạch Nhà máy Sữa", AssetCategory = "Uniform", SerialNumber = "PPE-NM-15", PurchaseValue = 1200000m, PurchaseDate = new DateOnly(2026, 3, 1), Status = "Available" }
            };
            await context.Assets.AddRangeAsync(assets, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            if (emps.Count >= 2)
            {
                await context.EmployeeAssetAssignments.AddRangeAsync(new List<EmployeeAssetAssignment>
                {
                    new() { AssetId = assets[0].Id, EmployeeId = emps[0].Id, AssignedDate = new DateOnly(2025, 2, 15), ConditionOnAssign = "Mới 100%, nguyên hộp", Status = "Active" },
                    new() { AssetId = assets[1].Id, EmployeeId = emps[1].Id, AssignedDate = new DateOnly(2025, 6, 20), ConditionOnAssign = "Mới 100%, kèm sạc Type-C", Status = "Active" }
                }, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
            }
        }

        if (!await context.EmployeeOnboardingTasks.AnyAsync(cancellationToken))
        {
            var probationEmp = await context.Employees.AsNoTracking()
                .Where(e => e.WorkStatus == "Probation" || e.WorkStatus == "Official")
                .OrderByDescending(e => e.Id)
                .FirstOrDefaultAsync(cancellationToken);

            var templates = await context.OnboardingTemplates.AsNoTracking()
                .Where(t => t.ProcessType == "Onboarding")
                .OrderBy(t => t.SortOrder)
                .ToListAsync(cancellationToken);

            if (probationEmp != null)
            {
                foreach (var tpl in templates)
                {
                    await context.EmployeeOnboardingTasks.AddAsync(new EmployeeOnboardingTask
                    {
                        EmployeeId = probationEmp.Id,
                        OnboardingTemplateId = tpl.Id,
                        ProcessType = "Onboarding",
                        TaskName = tpl.TaskName,
                        DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(tpl.DueDaysOffset)),
                        IsCompleted = tpl.SortOrder == 1,
                        CompletedAt = tpl.SortOrder == 1 ? DateTime.UtcNow : null,
                        Notes = $"Bộ phận phụ trách: {tpl.AssignedDepartmentRole}"
                    }, cancellationToken);
                }
                await context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}

// ============================================================================
// 3. QUERIES & COMMANDS
// ============================================================================

public record GetOnboardingAndAssetOverviewQuery() : IRequest<OnboardingAndAssetOverviewDto>;

public class GetOnboardingAndAssetOverviewQueryHandler : IRequestHandler<GetOnboardingAndAssetOverviewQuery, OnboardingAndAssetOverviewDto>
{
    private readonly IApplicationDbContext _context;

    public GetOnboardingAndAssetOverviewQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<OnboardingAndAssetOverviewDto> Handle(GetOnboardingAndAssetOverviewQuery request, CancellationToken cancellationToken)
    {
        await OnboardingAssetSeeder.EnsureSeededAsync(_context, cancellationToken);

        var activeEmployees = await _context.Employees.AsNoTracking()
            .Where(e => e.WorkStatus == "Official" || e.WorkStatus == "Probation")
            .OrderBy(e => e.EmployeeCode)
            .Select(e => new SimpleLookupDto(e.Id, e.EmployeeCode, $"{e.EmployeeCode} - {e.FullName}"))
            .ToListAsync(cancellationToken);

        var empNameMap = activeEmployees.ToDictionary(e => e.Id, e => e.Name);

        var templates = await _context.OnboardingTemplates.AsNoTracking()
            .OrderBy(t => t.ProcessType).ThenBy(t => t.SortOrder)
            .Select(t => new OnboardingTemplateDto(
                t.Id,
                t.TemplateCode,
                t.TaskName,
                t.ProcessType,
                t.AssignedDepartmentRole,
                t.DueDaysOffset,
                t.SortOrder))
            .ToListAsync(cancellationToken);

        var tasksRaw = await _context.EmployeeOnboardingTasks.AsNoTracking()
            .Include(t => t.Employee).ThenInclude(e => e.OrganizationUnit)
            .OrderBy(t => t.IsCompleted)
            .ThenBy(t => t.DueDate)
            .ToListAsync(cancellationToken);

        var tasks = tasksRaw.Select(t => new EmployeeOnboardingTaskDto(
            t.Id,
            t.EmployeeId,
            t.Employee.EmployeeCode,
            t.Employee.FullName,
            t.Employee.OrganizationUnit.UnitName,
            t.ProcessType,
            t.TaskName,
            t.PicEmployeeId,
            t.PicEmployeeId.HasValue && empNameMap.TryGetValue(t.PicEmployeeId.Value, out var pn) ? pn : "Bộ phận phụ trách",
            t.DueDate,
            t.IsCompleted,
            t.CompletedAt,
            t.Notes)).ToList();

        var assetsRaw = await _context.Assets.AsNoTracking()
            .Include(a => a.Assignments).ThenInclude(asg => asg.Employee)
            .OrderBy(a => a.AssetCode)
            .ToListAsync(cancellationToken);

        var assets = assetsRaw.Select(a =>
        {
            var activeAsg = a.Assignments.FirstOrDefault(x => x.Status == "Active");
            return new AssetItemDto(
                a.Id,
                a.AssetCode,
                a.AssetName,
                a.AssetCategory,
                a.SerialNumber,
                a.PurchaseValue,
                a.PurchaseDate,
                a.Status,
                activeAsg?.EmployeeId,
                activeAsg != null ? $"{activeAsg.Employee.EmployeeCode} - {activeAsg.Employee.FullName}" : null,
                activeAsg?.AssignedDate);
        }).ToList();

        return new OnboardingAndAssetOverviewDto(
            templates,
            tasks,
            assets,
            activeEmployees,
            tasks.Count,
            tasks.Count(t => t.IsCompleted),
            assets.Count(a => a.Status == "Available"),
            assets.Count(a => a.Status == "Assigned"));
    }
}

public record GenerateEmployeeChecklistFromTemplateCommand(
    int EmployeeId,
    string ProcessType,
    int? PicEmployeeId) : IRequest<int>;

public class GenerateEmployeeChecklistFromTemplateCommandHandler : IRequestHandler<GenerateEmployeeChecklistFromTemplateCommand, int>
{
    private readonly IApplicationDbContext _context;

    public GenerateEmployeeChecklistFromTemplateCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(GenerateEmployeeChecklistFromTemplateCommand request, CancellationToken cancellationToken)
    {
        var templates = await _context.OnboardingTemplates.AsNoTracking()
            .Where(t => t.ProcessType == request.ProcessType)
            .OrderBy(t => t.SortOrder)
            .ToListAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.Today);
        int count = 0;

        foreach (var tpl in templates)
        {
            await _context.EmployeeOnboardingTasks.AddAsync(new EmployeeOnboardingTask
            {
                EmployeeId = request.EmployeeId,
                OnboardingTemplateId = tpl.Id,
                ProcessType = request.ProcessType,
                TaskName = tpl.TaskName,
                PicEmployeeId = request.PicEmployeeId,
                DueDate = today.AddDays(tpl.DueDaysOffset),
                IsCompleted = false,
                Notes = $"Giao bộ phận: {tpl.AssignedDepartmentRole}"
            }, cancellationToken);
            count++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return count;
    }
}

public record ToggleOnboardingTaskCommand(int TaskId) : IRequest<bool>;

public class ToggleOnboardingTaskCommandHandler : IRequestHandler<ToggleOnboardingTaskCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public ToggleOnboardingTaskCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(ToggleOnboardingTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _context.EmployeeOnboardingTasks.FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);
        if (task == null) return false;

        task.IsCompleted = !task.IsCompleted;
        task.CompletedAt = task.IsCompleted ? DateTime.UtcNow : null;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public record AssignAssetToEmployeeCommand(
    int AssetId,
    int EmployeeId,
    string? ConditionOnAssign) : IRequest<string>;

public class AssignAssetToEmployeeCommandHandler : IRequestHandler<AssignAssetToEmployeeCommand, string>
{
    private readonly IApplicationDbContext _context;

    public AssignAssetToEmployeeCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string> Handle(AssignAssetToEmployeeCommand request, CancellationToken cancellationToken)
    {
        var asset = await _context.Assets.FirstOrDefaultAsync(a => a.Id == request.AssetId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy tài sản.");

        if (asset.Status == "Assigned")
            throw new InvalidOperationException($"Tài sản '{asset.AssetName}' hiện đang được bàn giao cho người khác.");

        var emp = await _context.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy nhân viên.");

        asset.Status = "Assigned";
        await _context.EmployeeAssetAssignments.AddAsync(new EmployeeAssetAssignment
        {
            AssetId = asset.Id,
            EmployeeId = emp.Id,
            AssignedDate = DateOnly.FromDateTime(DateTime.Today),
            ConditionOnAssign = request.ConditionOnAssign ?? "Hoạt động bình thường",
            Status = "Active"
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return $"Đã bàn giao tài sản '{asset.AssetCode} - {asset.AssetName}' cho {emp.FullName}.";
    }
}

public record ReturnAssetCommand(int AssetId, string? ConditionOnReturn) : IRequest<string>;

public class ReturnAssetCommandHandler : IRequestHandler<ReturnAssetCommand, string>
{
    private readonly IApplicationDbContext _context;

    public ReturnAssetCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string> Handle(ReturnAssetCommand request, CancellationToken cancellationToken)
    {
        var asset = await _context.Assets
            .Include(a => a.Assignments)
            .FirstOrDefaultAsync(a => a.Id == request.AssetId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy tài sản.");

        var activeAsg = asset.Assignments.FirstOrDefault(x => x.Status == "Active");
        if (activeAsg != null)
        {
            activeAsg.Status = "Returned";
            activeAsg.ReturnedDate = DateOnly.FromDateTime(DateTime.Today);
            activeAsg.ConditionOnReturn = request.ConditionOnReturn ?? "Đã kiểm tra thu hồi nguyên vẹn";
        }

        asset.Status = "Available";
        await _context.SaveChangesAsync(cancellationToken);
        return $"Đã thu hồi tài sản '{asset.AssetCode} - {asset.AssetName}' về kho sẵn sàng cấp phát.";
    }
}

