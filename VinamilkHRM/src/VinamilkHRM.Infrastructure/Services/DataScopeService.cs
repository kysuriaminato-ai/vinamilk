using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Application.Common.Security;
using VinamilkHRM.Domain.Entities.OrganizationalUnits;
using VinamilkHRM.Infrastructure.Data;

namespace VinamilkHRM.Infrastructure.Services;

/// <summary>
/// Triển khai IDataScope kiểm soát phạm vi truy cập dữ liệu phân cấp cây đơn vị qua HierarchyId.
/// </summary>
public class DataScopeService : IDataScope
{
    private readonly ICurrentUser _currentUser;
    private readonly VinamilkHRMDbContext _dbContext;

    public DataScopeService(ICurrentUser currentUser, VinamilkHRMDbContext dbContext)
    {
        _currentUser = currentUser;
        _dbContext = dbContext;
    }

    public bool IsGlobalAccess =>
        _currentUser.IsInRole(Roles.SystemAdmin) ||
        _currentUser.IsInRole(Roles.Executive) ||
        _currentUser.IsInRole(Roles.HRDirector) ||
        _currentUser.IsInRole(Roles.ChiefAccountant);

    public int? CurrentEmployeeId => _currentUser.EmployeeId;

    public async Task<IReadOnlyCollection<int>> GetAuthorizedUnitIdsAsync(CancellationToken cancellationToken = default)
    {
        if (IsGlobalAccess)
        {
            return await _dbContext.OrganizationalUnits
                .AsNoTracking()
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);
        }

        if (!_currentUser.EmployeeId.HasValue)
        {
            return Array.Empty<int>();
        }

        var employee = await _dbContext.Employees
            .AsNoTracking()
            .Include(e => e.OrganizationalUnit)
            .FirstOrDefaultAsync(e => e.Id == _currentUser.EmployeeId.Value, cancellationToken);

        if (employee?.OrganizationalUnit == null)
        {
            return Array.Empty<int>();
        }

        var parentUnit = employee.OrganizationalUnit;
        var parentPath = parentUnit.Path;

        if (parentPath != null)
        {
            // Sử dụng HierarchyId IsDescendantOf để lấy đơn vị hiện tại và toàn bộ đơn vị con cháu
            return await _dbContext.OrganizationalUnits
                .AsNoTracking()
                .Where(u => u.Path != null && u.Path.IsDescendantOf(parentPath))
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);
        }

        // Fallback đệ quy theo ParentUnitId nếu Path chưa sinh
        return await GetDescendantUnitIdsRecursiveAsync(parentUnit.Id, cancellationToken);
    }

    public async Task<bool> CanAccessUnitAsync(int unitId, CancellationToken cancellationToken = default)
    {
        if (IsGlobalAccess) return true;

        var allowedUnits = await GetAuthorizedUnitIdsAsync(cancellationToken);
        return allowedUnits.Contains(unitId);
    }

    public async Task<bool> CanAccessEmployeeAsync(int targetEmployeeId, CancellationToken cancellationToken = default)
    {
        if (IsGlobalAccess) return true;

        // Vai trò Employee (ESS) chỉ được xem chính mình
        if (_currentUser.IsInRole(Roles.Employee) &&
            !_currentUser.IsInRole(Roles.UnitManager) &&
            !_currentUser.IsInRole(Roles.TeamLeader) &&
            !_currentUser.IsInRole(Roles.HRManager) &&
            !_currentUser.IsInRole(Roles.HRSpecialist) &&
            !_currentUser.IsInRole(Roles.PayrollAccountant))
        {
            return CurrentEmployeeId.HasValue && CurrentEmployeeId.Value == targetEmployeeId;
        }

        // Người dùng quản lý/nhân sự: Kiểm tra nhân viên đích có thuộc cây đơn vị được phép hay không
        var targetEmployee = await _dbContext.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == targetEmployeeId, cancellationToken);

        if (targetEmployee == null) return false;

        var allowedUnits = await GetAuthorizedUnitIdsAsync(cancellationToken);
        return allowedUnits.Contains(targetEmployee.OrganizationalUnitId);
    }

    private async Task<List<int>> GetDescendantUnitIdsRecursiveAsync(int rootUnitId, CancellationToken cancellationToken)
    {
        var result = new HashSet<int> { rootUnitId };
        var queue = new Queue<int>();
        queue.Enqueue(rootUnitId);

        var allUnits = await _dbContext.OrganizationalUnits
            .AsNoTracking()
            .Select(u => new { u.Id, u.ParentUnitId })
            .ToListAsync(cancellationToken);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            var children = allUnits.Where(u => u.ParentUnitId == currentId).Select(u => u.Id);
            foreach (var childId in children)
            {
                if (result.Add(childId))
                {
                    queue.Enqueue(childId);
                }
            }
        }

        return result.ToList();
    }
}
