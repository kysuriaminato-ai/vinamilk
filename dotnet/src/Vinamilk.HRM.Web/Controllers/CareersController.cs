using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Helpers;
using Vinamilk.HRM.Application.Common.Interfaces;

namespace Vinamilk.HRM.Web.Controllers;

[AllowAnonymous]
public class CareersController : Controller
{
    private readonly IApplicationDbContext _context;

    public CareersController(IApplicationDbContext context) => _context = context;

    [HttpGet("/gioi-thieu")]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public IActionResult About()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        ViewData["Title"] = "Giới thiệu Vinamilk — Văn hóa & Cơ hội Nghề nghiệp";
        ViewData["MetaDescription"] = "Khám phá môi trường làm việc chuẩn quốc tế tại Vinamilk, phúc lợi toàn diện và lộ trình phát triển nhân tài bền vững.";
        ViewData["CanonicalUrl"] = $"{baseUrl}/gioi-thieu";
        ViewData["OgType"] = "website";
        ViewData["JsonLd"] = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Organization",
            ["name"] = "Công ty Cổ phần Sữa Việt Nam (Vinamilk)",
            ["url"] = baseUrl,
            ["logo"] = $"{baseUrl}/favicon.ico",
            ["description"] = "Doanh nghiệp sữa hàng đầu Việt Nam với hơn 45 năm phát triển bền vững.",
            ["address"] = new Dictionary<string, string>
            {
                ["@type"] = "PostalAddress",
                ["streetAddress"] = "Số 10 Tân Trào, Phường Tân Phú, Quận 7",
                ["addressLocality"] = "TP. Hồ Chí Minh",
                ["addressCountry"] = "VN"
            }
        });

        return View();
    }

    [HttpGet("/tuyen-dung")]
    public async Task<IActionResult> Index(string? q = null, int page = 1, CancellationToken cancellationToken = default)
    {
        const int pageSize = 9;
        page = Math.Max(1, page);

        var query = _context.JobPostings
            .AsNoTracking()
            .Include(j => j.OrganizationUnit)
            .Include(j => j.Position)
            .Where(j => j.Status == "Open");

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(j => j.Title.Contains(term) || j.OrganizationUnit.UnitName.Contains(term));
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(j => j.PublishDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new PublicJobCardVm(
                j.Id,
                j.PostingCode,
                j.Title,
                SlugHelper.ToSlug(j.Title),
                j.OrganizationUnit.UnitName,
                j.Position.PositionName,
                j.WorkLocation ?? "TP. Hồ Chí Minh",
                j.SalaryRangeText ?? "Thỏa thuận cạnh tranh",
                j.Vacancies,
                j.PublishDate,
                j.ClosingDate))
            .ToListAsync(cancellationToken);

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        ViewData["Title"] = "Tuyển dụng Vinamilk — Cơ hội Nghề nghiệp (Careers)";
        ViewData["MetaDescription"] = $"Hiện có {totalItems} vị trí tuyển dụng đang mở tại Vinamilk trên toàn quốc. Ứng tuyển trực tuyến ngay hôm nay.";
        ViewData["CanonicalUrl"] = $"{baseUrl}/tuyen-dung";
        ViewData["JsonLd"] = BuildBreadcrumbJsonLd(baseUrl, ("Trang chủ", "/gioi-thieu"), ("Tuyển dụng", "/tuyen-dung"));

        ViewBag.SearchQuery = q;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
        ViewBag.TotalItems = totalItems;

        return View(items);
    }

    [HttpGet("/tuyen-dung/{slug}-{id:int}")]
    public async Task<IActionResult> Detail(string slug, int id, CancellationToken cancellationToken = default)
    {
        var job = await _context.JobPostings
            .AsNoTracking()
            .Include(j => j.OrganizationUnit)
            .Include(j => j.Position)
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);

        if (job is null)
            return NotFound();

        var canonicalSlug = SlugHelper.ToSlug(job.Title);
        if (!string.Equals(slug, canonicalSlug, StringComparison.OrdinalIgnoreCase))
            return RedirectToActionPermanent(nameof(Detail), new { slug = canonicalSlug, id = job.Id });

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var canonicalUrl = $"{baseUrl}/tuyen-dung/{canonicalSlug}-{job.Id}";

        ViewData["Title"] = $"{job.Title} — Tuyển dụng Vinamilk";
        ViewData["MetaDescription"] = $"Ứng tuyển vị trí {job.Title} tại {job.OrganizationUnit.UnitName} ({job.WorkLocation ?? "TP. HCM"}). Hạn nộp: {job.ClosingDate:dd/MM/yyyy}.";
        ViewData["CanonicalUrl"] = canonicalUrl;
        ViewData["OgType"] = "article";

        var jobPostingSchema = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org/",
            ["@type"] = "JobPosting",
            ["title"] = job.Title,
            ["description"] = job.JobDescription ?? job.JobRequirements ?? job.Title,
            ["datePosted"] = job.PublishDate.ToString("yyyy-MM-dd"),
            ["validThrough"] = job.ClosingDate.ToString("yyyy-MM-dd"),
            ["employmentType"] = "FULL_TIME",
            ["hiringOrganization"] = new Dictionary<string, object?>
            {
                ["@type"] = "Organization",
                ["name"] = "Vinamilk",
                ["sameAs"] = baseUrl
            },
            ["jobLocation"] = new Dictionary<string, object?>
            {
                ["@type"] = "Place",
                ["address"] = new Dictionary<string, string>
                {
                    ["@type"] = "PostalAddress",
                    ["addressLocality"] = job.WorkLocation ?? "TP. Hồ Chí Minh",
                    ["addressCountry"] = "VN"
                }
            }
        };

        var breadcrumbSchema = BuildBreadcrumbDict(
            baseUrl,
            ("Giới thiệu", "/gioi-thieu"),
            ("Tuyển dụng", "/tuyen-dung"),
            (job.Title, $"/tuyen-dung/{canonicalSlug}-{job.Id}"));

        ViewData["JsonLd"] = JsonSerializer.Serialize(new object[] { jobPostingSchema, breadcrumbSchema });

        return View(new PublicJobDetailVm(
            job.Id,
            job.PostingCode,
            job.Title,
            canonicalSlug,
            job.OrganizationUnit.UnitName,
            job.Position.PositionName,
            job.WorkLocation ?? "TP. Hồ Chí Minh",
            job.SalaryRangeText ?? "Thỏa thuận",
            job.Vacancies,
            job.PublishDate,
            job.ClosingDate,
            job.JobDescription ?? "Trao đổi chi tiết khi phỏng vấn.",
            job.JobRequirements ?? "Tốt nghiệp Đại học trở lên các chuyên ngành liên quan.",
            job.Benefits ?? "Thưởng hiệu suất KPI, BHXH đầy đủ, Bảo hiểm sức khỏe cao cấp."));
    }

    [HttpGet("/sitemap.xml")]
    [ResponseCache(Duration = 600, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Sitemap(CancellationToken cancellationToken = default)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var jobs = await _context.JobPostings
            .AsNoTracking()
            .Where(j => j.Status == "Open")
            .OrderByDescending(j => j.PublishDate)
            .Select(j => new { j.Id, j.Title, j.PublishDate })
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
        sb.AppendLine($"  <url><loc>{baseUrl}/gioi-thieu</loc><changefreq>monthly</changefreq><priority>0.8</priority></url>");
        sb.AppendLine($"  <url><loc>{baseUrl}/tuyen-dung</loc><changefreq>daily</changefreq><priority>0.9</priority></url>");

        foreach (var j in jobs)
        {
            var slug = SlugHelper.ToSlug(j.Title);
            sb.AppendLine($"  <url><loc>{baseUrl}/tuyen-dung/{slug}-{j.Id}</loc><lastmod>{j.PublishDate:yyyy-MM-dd}</lastmod><changefreq>weekly</changefreq><priority>0.8</priority></url>");
        }

        sb.AppendLine("</urlset>");
        return Content(sb.ToString(), "application/xml", Encoding.UTF8);
    }

    [HttpGet("/robots.txt")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public IActionResult Robots()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var content = $"""
            User-agent: *
            Allow: /gioi-thieu
            Allow: /tuyen-dung
            Allow: /sitemap.xml
            Disallow: /Auth/
            Disallow: /Employees/
            Disallow: /Payroll/
            Disallow: /Reports/
            Disallow: /AiExpert/
            Disallow: /ESS/
            Disallow: /api/

            Sitemap: {baseUrl}/sitemap.xml
            """;
        return Content(content, "text/plain", Encoding.UTF8);
    }

    private static string BuildBreadcrumbJsonLd(string baseUrl, params (string Name, string Path)[] items)
        => JsonSerializer.Serialize(BuildBreadcrumbDict(baseUrl, items));

    private static Dictionary<string, object?> BuildBreadcrumbDict(string baseUrl, params (string Name, string Path)[] items)
    {
        return new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "BreadcrumbList",
            ["itemListElement"] = items.Select((item, idx) => new Dictionary<string, object?>
            {
                ["@type"] = "ListItem",
                ["position"] = idx + 1,
                ["name"] = item.Name,
                ["item"] = $"{baseUrl}{item.Path}"
            }).ToArray()
        };
    }
}

public sealed record PublicJobCardVm(
    int Id,
    string PostingCode,
    string Title,
    string Slug,
    string UnitName,
    string PositionName,
    string WorkLocation,
    string SalaryRangeText,
    int Vacancies,
    DateOnly PublishDate,
    DateOnly ClosingDate);

public sealed record PublicJobDetailVm(
    int Id,
    string PostingCode,
    string Title,
    string Slug,
    string UnitName,
    string PositionName,
    string WorkLocation,
    string SalaryRangeText,
    int Vacancies,
    DateOnly PublishDate,
    DateOnly ClosingDate,
    string JobDescription,
    string JobRequirements,
    string Benefits);
