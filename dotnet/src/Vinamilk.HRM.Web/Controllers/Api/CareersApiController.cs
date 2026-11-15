using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Helpers;
using Vinamilk.HRM.Application.Common.Interfaces;

namespace Vinamilk.HRM.Web.Controllers.Api;

[ApiController]
[Route("api/v1/careers")]
[Produces("application/json")]
public class CareersApiController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public CareersApiController(IApplicationDbContext context) => _context = context;

    /// <summary>
    /// Lấy danh sách tin tuyển dụng công khai (phân trang server-side, AsNoTracking).
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOpenJobs(
        [FromQuery] string? q = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _context.JobPostings
            .AsNoTracking()
            .Include(j => j.OrganizationUnit)
            .Where(j => j.Status == "Open");

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(j => j.Title.Contains(term) || j.OrganizationUnit.UnitName.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var data = await query
            .OrderByDescending(j => j.PublishDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new
            {
                j.Id,
                j.PostingCode,
                j.Title,
                Slug = SlugHelper.ToSlug(j.Title),
                UnitName = j.OrganizationUnit.UnitName,
                j.WorkLocation,
                j.SalaryRangeText,
                j.Vacancies,
                j.PublishDate,
                j.ClosingDate
            })
            .ToListAsync(cancellationToken);

        return Ok(new { page, pageSize, total, items = data });
    }
}
