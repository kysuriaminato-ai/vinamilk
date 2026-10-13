using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VinamilkHRM.Application.Privacy;

namespace VinamilkHRM.WebApi.Controllers;

[ApiController]
[Route("api/v1/privacy")]
[Authorize]
public class PrivacyController : ControllerBase
{
    private readonly IPersonalDataProtectionService _privacyService;

    public PrivacyController(IPersonalDataProtectionService privacyService)
    {
        _privacyService = privacyService;
    }

    /// <summary>
    /// Ghi nhận sự đồng ý xử lý dữ liệu cá nhân theo Nghị định 13/2023/NĐ-CP.
    /// </summary>
    [HttpPost("consent")]
    public async Task<IActionResult> RecordConsent([FromBody] ConsentRequestDto dto, CancellationToken ct)
    {
        string? ip = HttpContext?.Connection?.RemoteIpAddress?.ToString();
        await _privacyService.RecordConsentAsync(
            dto.SubjectId,
            dto.SubjectType,
            dto.SubjectEmail,
            dto.Purpose,
            dto.TermsVersion,
            dto.IsAgreed,
            ip,
            ct);

        return Ok(new { success = true, message = "Ghi nhận sự đồng ý dữ liệu cá nhân thành công." });
    }

    /// <summary>
    /// Xuất trích xuất dữ liệu cá nhân theo quyền của chủ thể dữ liệu (Điều 14 NĐ 13/2023).
    /// </summary>
    [HttpGet("export/{employeeId:int}")]
    public async Task<IActionResult> ExportPersonalData(int employeeId, CancellationToken ct)
    {
        int currentUserId = 1; // Default
        var exportPackage = await _privacyService.ExportEmployeePersonalDataAsync(employeeId, currentUserId, ct);
        return Ok(exportPackage);
    }

    /// <summary>
    /// Kích hoạt tác vụ ẩn danh hồ sơ ứng viên không trúng tuyển quá hạn lưu trữ.
    /// </summary>
    [HttpPost("anonymize-candidates")]
    [Authorize(Roles = "HR,HRAdmin,Admin")]
    public async Task<IActionResult> AnonymizeExpiredCandidates([FromQuery] int retentionMonths = 12, CancellationToken ct = default)
    {
        int count = await _privacyService.AnonymizeExpiredCandidatesAsync(retentionMonths, ct);
        return Ok(new
        {
            success = true,
            anonymizedCount = count,
            message = $"Đã ẩn danh {count} hồ sơ ứng viên quá hạn {retentionMonths} tháng."
        });
    }
}

public class ConsentRequestDto
{
    public int SubjectId { get; set; }
    public string SubjectType { get; set; } = "Employee";
    public string SubjectEmail { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string TermsVersion { get; set; } = "VNM-PDP-v2026.1";
    public bool IsAgreed { get; set; } = true;
}