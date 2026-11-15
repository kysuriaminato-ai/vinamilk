using System.Diagnostics;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Analytics;
using Vinamilk.HRM.Application.Common.Interfaces;

namespace Vinamilk.HRM.Web.Controllers;

[Authorize(Policy = "Dashboard.View")]
public class HomeController : Controller
{
    private readonly IMediator _mediator;
    private readonly IApplicationDbContext _context;

    public HomeController(IMediator mediator, IApplicationDbContext context)
    {
        _mediator = mediator;
        _context = context;
    }

    public async Task<IActionResult> Index(
        string roleView = "Executive",
        int? year = null,
        int? month = null,
        int? unitId = null,
        bool refresh = false,
        CancellationToken cancellationToken = default)
    {
        var overview = await _mediator.Send(
            new GetRoleDashboardOverviewQuery(roleView, year, month, unitId, refresh),
            cancellationToken);

        ViewBag.Units = await _context.OrganizationUnits
            .AsNoTracking()
            .Where(u => u.IsActive)
            .OrderBy(u => u.UnitLevel)
            .ThenBy(u => u.UnitName)
            .Select(u => new { u.Id, u.UnitName })
            .ToListAsync(cancellationToken);

        return View(overview);
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View();
    }
}
