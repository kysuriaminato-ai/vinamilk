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
    bool IsActive);

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

        var unitDtos = units.Select(u =>
        {
            empCountByUnit.TryGetValue(u.Id, out var actualCount);
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
                u.IsActive);
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
            .OrderBy(e => e.EmployeeCode)
            .Take(25)
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
    string? Address) : IRequest<int>;

public sealed class SaveOrganizationUnitCommandHandler : IRequestHandler<SaveOrganizationUnitCommand, int>
{
    private readonly IApplicationDbContext _context;

    public SaveOrganizationUnitCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(SaveOrganizationUnitCommand request, CancellationToken cancellationToken)
    {
        OrganizationUnit? parent = null;
        if (request.ParentUnitId.HasValue && request.ParentUnitId.Value > 0)
        {
            parent = await _context.OrganizationUnits
                .FirstOrDefaultAsync(u => u.Id == request.ParentUnitId.Value, cancellationToken);
        }

        OrganizationUnit unit;
        if (request.Id.HasValue && request.Id.Value > 0)
        {
            unit = await _context.OrganizationUnits
                       .FirstOrDefaultAsync(u => u.Id == request.Id.Value, cancellationToken)
                   ?? throw new InvalidOperationException("Không tìm thấy đơn vị cần cập nhật.");
        }
        else
        {
            unit = new OrganizationUnit();
            await _context.OrganizationUnits.AddAsync(unit, cancellationToken);
        }

        unit.UnitCode = request.UnitCode.Trim().ToUpperInvariant();
        unit.UnitName = request.UnitName.Trim();
        unit.UnitType = request.UnitType;
        unit.ParentUnitId = parent?.Id;
        unit.UnitLevel = parent is null ? 1 : parent.UnitLevel + 1;
        unit.ManagerEmployeeId = request.ManagerEmployeeId;
        unit.CostCenterCode = request.CostCenterCode?.Trim();
        unit.PlannedHeadcount = Math.Max(1, request.PlannedHeadcount);
        unit.SortOrder = request.SortOrder;
        unit.Address = request.Address?.Trim();
        unit.IsActive = true;

        await _context.SaveChangesAsync(cancellationToken);

        // Cập nhật đường dẫn cây phân cấp TreePath (/1/5/12/)
        unit.TreePath = parent is null
            ? $"/{unit.Id}/"
            : $"{(parent.TreePath ?? $"/{parent.Id}/")}{unit.Id}/";

        await _context.SaveChangesAsync(cancellationToken);
        return unit.Id;
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

