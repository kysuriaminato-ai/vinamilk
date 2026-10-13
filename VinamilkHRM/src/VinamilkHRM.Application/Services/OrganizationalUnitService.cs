using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Application.DTOs.OrganizationalUnits;
using VinamilkHRM.Application.Interfaces;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.OrganizationalUnits;

namespace VinamilkHRM.Application.Services;

public class OrganizationalUnitService : IOrganizationalUnitService
{
    private readonly IApplicationDbContext _context;

    public OrganizationalUnitService(IApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<List<OrganizationalUnitTreeDto>> GetTreeAsync(CancellationToken cancellationToken = default)
    {
        var units = await _context.OrganizationalUnits
            .AsNoTracking()
            .Include(u => u.Employees)
            .Include(u => u.ManagerEmployee)
            .OrderBy(u => u.Path)
            .ToListAsync(cancellationToken);

        var lookup = units.ToLookup(u => u.ParentUnitId);

        List<OrganizationalUnitTreeDto> BuildSubTree(int? parentId)
        {
            var result = new List<OrganizationalUnitTreeDto>();
            foreach (var u in lookup[parentId])
            {
                var children = BuildSubTree(u.Id);
                result.Add(new OrganizationalUnitTreeDto(
                    Id: u.Id,
                    UnitCode: u.UnitCode,
                    UnitName: u.UnitName,
                    UnitType: u.UnitType,
                    Path: u.Path?.ToString(),
                    ParentUnitId: u.ParentUnitId,
                    CostCenterCode: u.CostCenterCode,
                    WorkPattern: u.WorkPattern,
                    MinimumWageRegion: u.MinimumWageRegion,
                    PlannedHeadcount: u.PlannedHeadcount,
                    CurrentHeadcount: u.Employees.Count,
                    ManagerEmployeeId: u.ManagerEmployeeId,
                    ManagerEmployeeName: u.ManagerEmployee?.FullName,
                    Children: children
                ));
            }
            return result;
        }

        return BuildSubTree(null);
    }

    public async Task<OrganizationalUnitDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var u = await _context.OrganizationalUnits
            .AsNoTracking()
            .Include(x => x.ParentUnit)
            .Include(x => x.ManagerEmployee)
            .Include(x => x.Employees)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (u == null) return null;

        return new OrganizationalUnitDto(
            u.Id,
            u.UnitCode,
            u.UnitName,
            u.UnitType,
            u.Address,
            u.PhoneNumber,
            u.Email,
            u.TaxCode,
            u.ParentUnitId,
            u.ParentUnit?.UnitName,
            u.ManagerEmployeeId,
            u.ManagerEmployee?.FullName,
            u.Employees.Count
        );
    }

    public async Task MoveUnitAsync(int unitId, int? newParentUnitId, CancellationToken cancellationToken = default)
    {
        var unit = await _context.OrganizationalUnits
            .FirstOrDefaultAsync(u => u.Id == unitId, cancellationToken);

        if (unit == null)
            throw new DomainException($"Không tìm thấy đơn vị với ID {unitId}.", "ERR_UNIT_NOT_FOUND");

        if (unit.ParentUnitId == newParentUnitId)
            return; // Đã ở đúng vị trí cha

        var oldPath = unit.Path;
        HierarchyId newPath;

        if (!newParentUnitId.HasValue)
        {
            // Di chuyển lên làm node gốc
            var rootCount = await _context.OrganizationalUnits.CountAsync(u => u.ParentUnitId == null, cancellationToken);
            newPath = HierarchyId.Parse($"/{rootCount + 1}/");
        }
        else
        {
            var newParent = await _context.OrganizationalUnits
                .FirstOrDefaultAsync(u => u.Id == newParentUnitId.Value, cancellationToken);

            if (newParent == null)
                throw new DomainException($"Không tìm thấy đơn vị cha mới với ID {newParentUnitId.Value}.", "ERR_PARENT_UNIT_NOT_FOUND");

            if (unit.Id == newParent.Id)
                throw new DomainException("Đơn vị không thể chuyển trực thuộc chính nó.", "ERR_CIRCULAR_UNIT_REFERENCE");

            if (oldPath != null && newParent.Path != null && newParent.Path.IsDescendantOf(oldPath))
                throw new DomainException("Không thể chuyển đơn vị vào một đơn vị con cháu trực thuộc nó.", "ERR_CIRCULAR_UNIT_REFERENCE");

            var childCount = await _context.OrganizationalUnits.CountAsync(u => u.ParentUnitId == newParent.Id, cancellationToken);
            newPath = HierarchyId.Parse($"{newParent.Path}{childCount + 1}/");
        }

        // Cập nhật đơn vị hiện tại
        unit.MoveTo(newParentUnitId, newPath);

        // Cập nhật tất cả các đơn vị con cháu nếu có
        if (oldPath != null)
        {
            var descendants = await _context.OrganizationalUnits
                .Where(u => u.Path != null && u.Path.IsDescendantOf(oldPath) && u.Id != unit.Id)
                .ToListAsync(cancellationToken);

            foreach (var desc in descendants)
            {
                var reparented = desc.Path!.GetReparentedValue(oldPath, newPath);
                desc.SetPath(reparented);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AssignManagerAsync(int unitId, AssignManagerRequest request, CancellationToken cancellationToken = default)
    {
        var unit = await _context.OrganizationalUnits
            .Include(u => u.UnitManagerHistories)
            .FirstOrDefaultAsync(u => u.Id == unitId, cancellationToken);

        if (unit == null)
            throw new DomainException($"Không tìm thấy đơn vị với ID {unitId}.", "ERR_UNIT_NOT_FOUND");

        var empExists = await _context.Employees.AnyAsync(e => e.Id == request.ManagerEmployeeId, cancellationToken);
        if (!empExists)
            throw new DomainException($"Không tìm thấy nhân viên với ID {request.ManagerEmployeeId}.", "ERR_EMP_NOT_FOUND");

        unit.AssignManager(request.ManagerEmployeeId, request.AppointmentDecisionNumber, request.Note, request.EffectiveFrom);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ChangeWageRegionAsync(int unitId, ChangeWageRegionRequest request, CancellationToken cancellationToken = default)
    {
        var unit = await _context.OrganizationalUnits
            .Include(u => u.UnitMinimumWageRegionHistories)
            .FirstOrDefaultAsync(u => u.Id == unitId, cancellationToken);

        if (unit == null)
            throw new DomainException($"Không tìm thấy đơn vị với ID {unitId}.", "ERR_UNIT_NOT_FOUND");

        unit.ChangeMinimumWageRegion(request.NewRegion, request.EffectiveFrom, request.Reason, request.LegalReference);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UnitManagerHistoryDto>> GetManagerHistoriesAsync(int unitId, CancellationToken cancellationToken = default)
    {
        var histories = await _context.UnitManagerHistories
            .AsNoTracking()
            .Include(h => h.Employee)
            .Where(h => h.OrganizationalUnitId == unitId)
            .OrderByDescending(h => h.FromDate)
            .ToListAsync(cancellationToken);

        return histories.Select(h => new UnitManagerHistoryDto(
            h.Id,
            h.OrganizationalUnitId,
            h.EmployeeId,
            h.Employee?.FullName,
            h.FromDate,
            h.ToDate,
            h.AppointmentDecisionNumber,
            h.Note
        )).ToList();
    }
}
