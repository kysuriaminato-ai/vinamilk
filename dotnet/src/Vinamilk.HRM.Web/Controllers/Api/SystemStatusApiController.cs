using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Application.Common.Security;

namespace Vinamilk.HRM.Web.Controllers.Api;

[ApiController]
[Authorize(Policy = "Dashboard.View")]
[Route("api/v1/system")]
public class SystemStatusApiController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public SystemStatusApiController(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var stats = new
        {
            SystemName = "Vinamilk Enterprise HRM (.NET 8 Clean Architecture)",
            TimestampUtc = DateTime.UtcNow,
            OrganizationUnits = await _context.OrganizationUnits.CountAsync(cancellationToken),
            Positions = await _context.Positions.CountAsync(cancellationToken),
            SalaryScales = await _context.SalaryScales.CountAsync(cancellationToken),
            SalarySteps = await _context.SalarySteps.CountAsync(cancellationToken),
            Employees = await _context.Employees.CountAsync(cancellationToken),
            LaborContracts = await _context.LaborContracts.CountAsync(cancellationToken),
            Roles = await _context.Roles.CountAsync(cancellationToken),
            Permissions = await _context.Permissions.CountAsync(cancellationToken),
            Users = await _context.Users.CountAsync(cancellationToken),
            PayrollRecords = await _context.PayrollRecords.CountAsync(cancellationToken)
        };

        return Ok(stats);
    }

    [HttpGet("search")]
    [Authorize(Policy = "Employees.View")]
    public async Task<IActionResult> GlobalSearch([FromQuery] string? q, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Ok(new { employees = Array.Empty<object>(), units = Array.Empty<object>() });
        }

        var term = q.Trim();
        var scope = await DataScopeResolver.ResolveAsync(_context, _currentUser, cancellationToken);

        var empQuery = _context.Employees
            .AsNoTracking()
            .Include(e => e.OrganizationUnit)
            .Include(e => e.Position)
            .Where(e => e.EmployeeCode.Contains(term) || e.FullName.Contains(term) || (e.WorkEmail != null && e.WorkEmail.Contains(term)));

        if (!scope.IsUnrestricted)
        {
            empQuery = empQuery.Where(e =>
                scope.AllowedUnitIdList.Contains(e.OrganizationUnitId)
                || (scope.CurrentEmployeeId.HasValue && e.Id == scope.CurrentEmployeeId.Value));
        }

        var employees = await empQuery
            .OrderBy(e => e.EmployeeCode)
            .Take(8)
            .Select(e => new
            {
                employeeCode = e.EmployeeCode,
                fullName = e.FullName,
                unitName = e.OrganizationUnit.UnitName,
                positionName = e.Position.PositionName,
                status = e.WorkStatus
            })
            .ToListAsync(cancellationToken);

        var unitQuery = _context.OrganizationUnits
            .AsNoTracking()
            .Where(u => u.UnitCode.Contains(term) || u.UnitName.Contains(term));

        if (!scope.IsUnrestricted)
        {
            unitQuery = unitQuery.Where(u => scope.AllowedUnitIdList.Contains(u.Id));
        }

        var units = await unitQuery
            .OrderBy(u => u.UnitCode)
            .Take(6)
            .Select(u => new
            {
                code = u.UnitCode,
                name = u.UnitName
            })
            .ToListAsync(cancellationToken);

        return Ok(new { employees, units });
    }
}
