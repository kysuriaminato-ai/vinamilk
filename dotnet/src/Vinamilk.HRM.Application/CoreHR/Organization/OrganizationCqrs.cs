using MediatR;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Domain.Entities.Core;

namespace Vinamilk.HRM.Application.CoreHR.Organization;

public sealed record OrgUnitNodeDto(
    int Id,
    string UnitCode,
    string UnitName,
    string UnitType,
    int UnitLevel,
    int? ParentUnitId,
    string? TreePath,
    string? ManagerName,
    string? CostCenterCode,
    int PlannedHeadcount,
    int ActualHeadcount,
    int SortOrder,
    bool IsActive,
    int? ManagerEmployeeId = null,
    int WageRegion = 1,
    string? Address = null,
    int ChildCount = 0);

public sealed record OrganizationOverviewDto(
    IReadOnlyList<OrgUnitNodeDto> Units,
    IReadOnlyList<Position> Positions,
    IReadOnlyList<SalaryScale> SalaryScales,
    IReadOnlyList<Employee> ManagerCandidates);

/* ----------------------------------------------------------------------------
 * QUERY: Lấy toàn bộ Cây Tổ chức 4 cấp, Chức danh & Thang bảng lương
 * ---------------------------------------------------------------------------- */
public sealed record GetOrganizationOverviewQuery : IRequest<OrganizationOverviewDto>;

public sealed class GetOrganizationOverviewQueryHandler
    : IRequestHandler<GetOrganizationOverviewQuery, OrganizationOverviewDto>
{
    private readonly IApplicationDbContext _context;

    public GetOrganizationOverviewQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<OrganizationOverviewDto> Handle(
        GetOrganizationOverviewQuery request,
        CancellationToken cancellationToken)
    {
        var units = await _context.OrganizationUnits
            .AsNoTracking()
            .OrderBy(u => u.UnitLevel)
            .ThenBy(u => u.SortOrder)
            .ThenBy(u => u.UnitCode)
            .ToListAsync(cancellationToken);

        var employees = await _context.Employees
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var empCountByUnit = employees
            .Where(e => e.WorkStatus != "Resigned" && e.WorkStatus != "Terminated")
            .GroupBy(e => e.OrganizationUnitId)
            .ToDictionary(g => g.Key, g => g.Count());

        var empNameById = employees.ToDictionary(e => e.Id, e => $"{e.EmployeeCode} - {e.FullName}");

        var childCountByParent = units
            .Where(u => u.ParentUnitId.HasValue)
            .GroupBy(u => u.ParentUnitId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var unitDtos = units.Select(u =>
        {
            empCountByUnit.TryGetValue(u.Id, out var actualCount);
            childCountByParent.TryGetValue(u.Id, out var childCount);
            string? managerName = null;
            if (u.ManagerEmployeeId.HasValue && empNameById.TryGetValue(u.ManagerEmployeeId.Value, out var mName))
            {
                managerName = mName;
            }

            return new OrgUnitNodeDto(
                u.Id,
                u.UnitCode,
                u.UnitName,
                u.UnitType,
                u.UnitLevel,
                u.ParentUnitId,
                u.TreePath,
                managerName,
                u.CostCenterCode,
                u.PlannedHeadcount,
                actualCount,
                u.SortOrder,
                u.IsActive,
                u.ManagerEmployeeId,
                u.WageRegion,
                u.Address,
                childCount);
        }).ToList();

        var positions = await _context.Positions
            .AsNoTracking()
            .OrderBy(p => p.PositionLevel)
            .ThenBy(p => p.PositionCode)
            .ToListAsync(cancellationToken);

        var scales = await _context.SalaryScales
            .AsNoTracking()
            .Include(s => s.Steps.OrderBy(st => st.StepOrder))
            .OrderBy(s => s.ScaleCode)
            .ToListAsync(cancellationToken);

        var managers = employees
            .Where(e => e.WorkStatus != "Resigned" && e.WorkStatus != "Terminated")
            .OrderBy(e => e.EmployeeCode)
            .ToList();

        return new OrganizationOverviewDto(unitDtos, positions, scales, managers);
    }
}

/* ----------------------------------------------------------------------------
 * COMMAND: Thêm mới / Cập nhật Đơn vị Tổ chức (Tự động tính UnitLevel & TreePath)
 * ---------------------------------------------------------------------------- */
public sealed record SaveOrganizationUnitCommand(
    int? Id,
    string UnitCode,
    string UnitName,
    string UnitType,
    int? ParentUnitId,
    int? ManagerEmployeeId,
    string? CostCenterCode,
    int PlannedHeadcount,
    int SortOrder,
    string? Address,
    int WageRegion = 1) : IRequest<int>;

public sealed class SaveOrganizationUnitCommandHandler : IRequestHandler<SaveOrganizationUnitCommand, int>
{
    private readonly IApplicationDbContext _context;

    public SaveOrganizationUnitCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(SaveOrganizationUnitCommand request, CancellationToken cancellationToken)
    {
        var normalizedCode = request.UnitCode.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalizedCode) || string.IsNullOrWhiteSpace(request.UnitName))
            throw new InvalidOperationException("Mã đơn vị và Tên đơn vị không được để trống.");

        var duplicateCode = await _context.OrganizationUnits
            .AnyAsync(u => u.UnitCode == normalizedCode && (!request.Id.HasValue || u.Id != request.Id.Value), cancellationToken);
        if (duplicateCode)
            throw new InvalidOperationException($"Mã đơn vị '{normalizedCode}' đã tồn tại trong hệ thống.");

        OrganizationUnit? parent = null;
        if (request.ParentUnitId.HasValue && request.ParentUnitId.Value > 0)
        {
            if (request.Id.HasValue && request.ParentUnitId.Value == request.Id.Value)
                throw new InvalidOperationException("Đơn vị không thể chọn chính nó làm đơn vị cấp trên.");

            parent = await _context.OrganizationUnits
                .FirstOrDefaultAsync(u => u.Id == request.ParentUnitId.Value, cancellationToken)
                ?? throw new InvalidOperationException("Không tìm thấy đơn vị cấp trên đã chọn.");
        }

        OrganizationUnit unit;
        if (request.Id.HasValue && request.Id.Value > 0)
        {
            unit = await _context.OrganizationUnits
                       .FirstOrDefaultAsync(u => u.Id == request.Id.Value, cancellationToken)
                   ?? throw new InvalidOperationException("Không tìm thấy đơn vị cần cập nhật.");

            if (parent != null && !string.IsNullOrEmpty(unit.TreePath) && (parent.TreePath ?? "").StartsWith(unit.TreePath, StringComparison.Ordinal))
                throw new InvalidOperationException("Không thể chọn đơn vị con làm đơn vị cấp trên (vòng lặp phân cấp).");
        }
        else
        {
            unit = new OrganizationUnit();
            await _context.OrganizationUnits.AddAsync(unit, cancellationToken);
        }

        var oldTreePath = unit.TreePath;

        unit.UnitCode = normalizedCode;
        unit.UnitName = request.UnitName.Trim();
        unit.UnitType = request.UnitType;
        unit.ParentUnitId = parent?.Id;
        unit.UnitLevel = parent is null ? 1 : parent.UnitLevel + 1;
        unit.ManagerEmployeeId = request.ManagerEmployeeId is > 0 ? request.ManagerEmployeeId : null;
        unit.CostCenterCode = request.CostCenterCode?.Trim();
        unit.PlannedHeadcount = Math.Max(1, request.PlannedHeadcount);
        unit.SortOrder = request.SortOrder;
        unit.Address = request.Address?.Trim();
        unit.WageRegion = Math.Clamp(request.WageRegion, 1, 4);
        unit.IsActive = true;

        await _context.SaveChangesAsync(cancellationToken);

        // Cập nhật đường dẫn cây phân cấp TreePath (/1/5/12/) cho bản thân và các đơn vị con nếu có
        var newTreePath = parent is null
            ? $"/{unit.Id}/"
            : $"{(parent.TreePath ?? $"/{parent.Id}/")}{unit.Id}/";
        unit.TreePath = newTreePath;

        if (!string.IsNullOrEmpty(oldTreePath) && oldTreePath != newTreePath)
        {
            var descendants = await _context.OrganizationUnits
                .Where(u => u.Id != unit.Id && u.TreePath != null && u.TreePath.StartsWith(oldTreePath))
                .ToListAsync(cancellationToken);
            foreach (var desc in descendants)
            {
                desc.TreePath = string.Concat(newTreePath, desc.TreePath!.AsSpan(oldTreePath.Length));
                desc.UnitLevel = desc.TreePath.Split('/', StringSplitOptions.RemoveEmptyEntries).Length;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return unit.Id;
    }
}

/* ----------------------------------------------------------------------------
 * COMMAND: Xóa Đơn vị Tổ chức (Chặn xóa nếu còn nhân sự hoặc đơn vị con)
 * ---------------------------------------------------------------------------- */
public sealed record DeleteOrganizationUnitCommand(int Id) : IRequest<string>;

public sealed class DeleteOrganizationUnitCommandHandler : IRequestHandler<DeleteOrganizationUnitCommand, string>
{
    private readonly IApplicationDbContext _context;

    public DeleteOrganizationUnitCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string> Handle(DeleteOrganizationUnitCommand request, CancellationToken cancellationToken)
    {
        var unit = await _context.OrganizationUnits
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy đơn vị cần xóa.");

        var hasChildren = await _context.OrganizationUnits
            .AnyAsync(u => u.ParentUnitId == request.Id, cancellationToken);
        if (hasChildren)
            throw new InvalidOperationException($"Không thể xóa đơn vị [{unit.UnitCode} - {unit.UnitName}] vì vẫn còn đơn vị con trực thuộc.");

        var empCount = await _context.Employees
            .CountAsync(e => e.OrganizationUnitId == request.Id, cancellationToken);
        if (empCount > 0)
            throw new InvalidOperationException($"Không thể xóa đơn vị [{unit.UnitCode} - {unit.UnitName}] vì đang có {empCount} nhân sự trực thuộc.");

        unit.IsDeleted = true;
        unit.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);

        return $"Đã xóa đơn vị [{unit.UnitCode} - {unit.UnitName}].";
    }
}

/* ----------------------------------------------------------------------------
 * COMMAND: Sắp xếp thứ tự Kéo & Thả các Đơn vị trong Cây tổ chức
 * ---------------------------------------------------------------------------- */
public sealed record ReorderOrganizationUnitsCommand(IReadOnlyList<int> OrderedUnitIds) : IRequest<int>;

public sealed class ReorderOrganizationUnitsCommandHandler : IRequestHandler<ReorderOrganizationUnitsCommand, int>
{
    private readonly IApplicationDbContext _context;

    public ReorderOrganizationUnitsCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(ReorderOrganizationUnitsCommand request, CancellationToken cancellationToken)
    {
        var units = await _context.OrganizationUnits.ToListAsync(cancellationToken);
        var map = units.ToDictionary(u => u.Id);
        var order = 1;
        foreach (var id in request.OrderedUnitIds)
        {
            if (map.TryGetValue(id, out var u))
            {
                u.SortOrder = order++;
            }
        }

        return await _context.SaveChangesAsync(cancellationToken);
    }
}

/* ----------------------------------------------------------------------------
 * COMMAND: Thêm / Cập nhật Chức danh (Position)
 * ---------------------------------------------------------------------------- */
public sealed record SavePositionCommand(
    int? Id,
    string PositionCode,
    string PositionName,
    string PositionGroup,
    int PositionLevel,
    decimal DefaultPositionAllowance,
    string? JobDescription) : IRequest<int>;

public sealed class SavePositionCommandHandler : IRequestHandler<SavePositionCommand, int>
{
    private readonly IApplicationDbContext _context;

    public SavePositionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(SavePositionCommand request, CancellationToken cancellationToken)
    {
        Position pos;
        if (request.Id.HasValue && request.Id.Value > 0)
        {
            pos = await _context.Positions.FirstAsync(p => p.Id == request.Id.Value, cancellationToken);
        }
        else
        {
            pos = new Position();
            await _context.Positions.AddAsync(pos, cancellationToken);
        }

        pos.PositionCode = request.PositionCode.Trim().ToUpperInvariant();
        pos.PositionName = request.PositionName.Trim();
        pos.PositionGroup = request.PositionGroup;
        pos.PositionLevel = request.PositionLevel;
        pos.DefaultPositionAllowance = request.DefaultPositionAllowance;
        pos.JobDescription = request.JobDescription;
        pos.IsActive = true;

        await _context.SaveChangesAsync(cancellationToken);
        return pos.Id;
    }
}

