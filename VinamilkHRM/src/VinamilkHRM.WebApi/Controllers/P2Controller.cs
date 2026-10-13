using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Domain.Entities.P2;

namespace VinamilkHRM.WebApi.Controllers;

[ApiController]
[Route("api/v1/p2")]
[Authorize]
public class P2Controller : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public P2Controller(IApplicationDbContext context)
    {
        _context = context;
    }

    // 1. ATS
    [HttpGet("ats/requisitions")]
    public async Task<IActionResult> GetRequisitions(CancellationToken ct)
    {
        var items = await _context.JobRequisitions.OrderByDescending(r => r.CreatedAt).Take(50).ToListAsync(ct);
        return Ok(items);
    }

    [HttpPost("ats/requisitions")]
    [Authorize(Roles = "HR,HRAdmin,Admin")]
    public async Task<IActionResult> CreateRequisition([FromBody] JobRequisition req, CancellationToken ct)
    {
        _context.JobRequisitions.Add(req);
        await _context.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetRequisitions), new { id = req.Id }, req);
    }

    // 2. Training & Certifications
    [HttpGet("training/courses")]
    public async Task<IActionResult> GetCourses(CancellationToken ct)
    {
        var items = await _context.TrainingCourses.OrderBy(c => c.CourseCode).ToListAsync(ct);
        return Ok(items);
    }

    [HttpGet("training/certifications/expiring")]
    public async Task<IActionResult> GetExpiringCertifications([FromQuery] int withinDays = 30, CancellationToken ct = default)
    {
        DateOnly threshold = DateOnly.FromDateTime(DateTime.Today.AddDays(withinDays));
        var items = await _context.EmployeeCertifications
            .Include(c => c.Employee)
            .Where(c => c.ExpiryDate.HasValue && c.ExpiryDate.Value <= threshold && c.Status == "Active")
            .ToListAsync(ct);
        return Ok(items);
    }

    // 3. Talent & 9-Box Grid
    [HttpGet("talent/nine-box")]
    public async Task<IActionResult> GetNineBoxGrid([FromQuery] string? period, CancellationToken ct)
    {
        string p = string.IsNullOrWhiteSpace(period) ? DateTime.Today.Year.ToString() : period;
        var items = await _context.NineBoxPlacements
            .Include(n => n.Employee)
            .Where(n => n.Period == p)
            .ToListAsync(ct);
        return Ok(new { Period = p, Total = items.Count, Items = items });
    }

    // 4. Safety & OHS
    [HttpGet("safety/incidents")]
    public async Task<IActionResult> GetSafetyIncidents(CancellationToken ct)
    {
        var items = await _context.SafetyIncidents.OrderByDescending(s => s.IncidentDate).Take(50).ToListAsync(ct);
        return Ok(items);
    }

    // 5. Multi-Legal Entity
    [HttpGet("legal-entities")]
    public async Task<IActionResult> GetLegalEntities(CancellationToken ct)
    {
        var items = await _context.LegalEntities.ToListAsync(ct);
        return Ok(items);
    }

    // 6. Government Reports (BM01 - BM04)
    [HttpGet("reports/government/{reportCode}")]
    [Authorize(Roles = "HR,HRAdmin,Admin")]
    public IActionResult ExportGovernmentReport(string reportCode)
    {
        string code = reportCode.Trim().ToUpperInvariant();
        string reportName = code switch
        {
            "BM01" => "Báo cáo tình hình thay đổi lao động (Nghị định 145/2020/NĐ-CP)",
            "BM02" => "Báo cáo định kỳ tình hình tai nạn lao động (Nghị định 39/2016/NĐ-CP)",
            "BM03" => "Báo cáo công tác an toàn vệ sinh lao động (Thông tư 07/2016/TT-BLĐTBXH)",
            "BM04" => "Báo cáo tình hình sử dụng lao động nước ngoài",
            _ => "Báo cáo tổng hợp lao động"
        };

        return Ok(new
        {
            ReportCode = code,
            Title = reportName,
            GeneratedAt = DateTime.UtcNow,
            ExportUrl = $"/api/v1/p2/reports/government/{code}/download",
            Status = "Ready"
        });
    }
}