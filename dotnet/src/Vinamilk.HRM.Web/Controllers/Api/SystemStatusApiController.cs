using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Interfaces;

namespace Vinamilk.HRM.Web.Controllers.Api;

[ApiController]
[Authorize]
[Route("api/v1/system")]
public class SystemStatusApiController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public SystemStatusApiController(IApplicationDbContext context)
    {
        _context = context;
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
    public async Task<IActionResult> GlobalSearch([FromQuery] string? q, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Ok(new { employees = Array.Empty<object>(), units = Array.Empty<object>() });
        }

        var term = q.Trim();

        var employees = await _context.Employees
            .AsNoTracking()
            .Include(e => e.OrganizationUnit)
            .Include(e => e.Position)
            .Where(e => e.EmployeeCode.Contains(term) || e.FullName.Contains(term) || (e.WorkEmail != null && e.WorkEmail.Contains(term)))
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

        var units = await _context.OrganizationUnits
            .AsNoTracking()
            .Where(u => u.UnitCode.Contains(term) || u.UnitName.Contains(term))
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
