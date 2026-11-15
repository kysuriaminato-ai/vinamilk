using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Interfaces;

namespace Vinamilk.HRM.Web.Controllers;

[Route("dev/styleguide")]
[Authorize]
public class StyleGuideController : Controller
{
    private readonly IApplicationDbContext _context;

    public StyleGuideController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Vinamilk HRM Design System & Component Showcase";
        ViewData["BreadcrumbParent"] = "UI/UX Architecture (Phiên 3)";

        var sampleEmployees = await _context.Employees
            .AsNoTracking()
            .Include(e => e.OrganizationUnit)
            .Include(e => e.Position)
            .OrderBy(e => e.EmployeeCode)
            .Take(8)
            .ToListAsync(cancellationToken);

        return View(sampleEmployees);
    }

    [HttpGet("print-preview")]
    public async Task<IActionResult> PrintPreview(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Quyết định Điều chuyển & Bổ nhiệm Cán bộ (Mẫu A4)";
        var emp = await _context.Employees
            .AsNoTracking()
            .Include(e => e.OrganizationUnit)
            .Include(e => e.Position)
            .FirstOrDefaultAsync(e => e.EmployeeCode == "VNM-0005", cancellationToken);

        return View(emp);
    }
}

