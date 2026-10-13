using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VinamilkHRM.Application.Common.Security;
using VinamilkHRM.Application.DTOs.Jobs;
using VinamilkHRM.Application.Interfaces;

namespace VinamilkHRM.WebApi.Controllers;

[ApiController]
[Route("api/v1/jobs")]
[Authorize]
public class JobsController : ControllerBase
{
    private readonly IJobService _jobService;

    public JobsController(IJobService jobService)
    {
        _jobService = jobService ?? throw new ArgumentNullException(nameof(jobService));
    }

    [HttpGet("titles")]
    [Authorize(Policy = Permissions.NhansuView)]
    [ProducesResponseType(typeof(IReadOnlyList<JobTitleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetJobTitles(CancellationToken cancellationToken)
    {
        var titles = await _jobService.GetJobTitlesAsync(cancellationToken);
        return Ok(titles);
    }

    [HttpPost("titles")]
    [Authorize(Policy = Permissions.NhansuManage)]
    [ProducesResponseType(typeof(JobTitleDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateJobTitle([FromBody] CreateJobTitleRequest request, CancellationToken cancellationToken)
    {
        var title = await _jobService.CreateJobTitleAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetJobTitles), new { id = title.Id }, title);
    }

    [HttpGet("grades")]
    [Authorize(Policy = Permissions.NhansuView)]
    [ProducesResponseType(typeof(IReadOnlyList<JobGradeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetJobGrades(CancellationToken cancellationToken)
    {
        var grades = await _jobService.GetJobGradesAsync(cancellationToken);
        return Ok(grades);
    }

    [HttpGet("families")]
    [Authorize(Policy = Permissions.NhansuView)]
    [ProducesResponseType(typeof(IReadOnlyList<JobFamilyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetJobFamilies(CancellationToken cancellationToken)
    {
        var families = await _jobService.GetJobFamiliesAsync(cancellationToken);
        return Ok(families);
    }

    [HttpGet("salary-scales")]
    [Authorize(Policy = Permissions.TienluongView)]
    [ProducesResponseType(typeof(IReadOnlyList<SalaryScaleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSalaryScales([FromQuery] int? jobGradeId, CancellationToken cancellationToken)
    {
        var scales = await _jobService.GetSalaryScalesAsync(jobGradeId, cancellationToken);
        return Ok(scales);
    }

    [HttpPost("salary-scales")]
    [Authorize(Policy = Permissions.TienluongCalculate)]
    [ProducesResponseType(typeof(SalaryScaleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSalaryScale([FromBody] CreateSalaryScaleRequest request, CancellationToken cancellationToken)
    {
        var scale = await _jobService.CreateSalaryScaleAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetSalaryScales), new { jobGradeId = scale.JobGradeId }, scale);
    }
}
