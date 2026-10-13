#pragma warning disable CS8602
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Application.DTOs.Salaries;
using VinamilkHRM.Application.Interfaces;
using VinamilkHRM.Domain.Common;

namespace VinamilkHRM.WebApi.Controllers;

[ApiController]
[Route("api/v1/salary-reviews")]
[Authorize]
public class SalaryReviewController : ControllerBase
{
    private readonly ISalaryReviewService _salaryReviewService;
    private readonly ICurrentUser _currentUser;
    private readonly IDataScope _dataScope;

    public SalaryReviewController(
        ISalaryReviewService salaryReviewService,
        ICurrentUser currentUser,
        IDataScope dataScope)
    {
        _salaryReviewService = salaryReviewService ?? throw new ArgumentNullException(nameof(salaryReviewService));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _dataScope = dataScope ?? throw new ArgumentNullException(nameof(dataScope));
    }

    private int CurrentEmployeeId => _currentUser.EmployeeId ?? 1;

    /// <summary>
    /// Lấy danh sách khung lương theo ngạch chức danh (SalaryBands).
    /// </summary>
    [HttpGet("bands")]
    [ProducesResponseType(typeof(IReadOnlyList<SalaryBandDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSalaryBands(CancellationToken cancellationToken)
    {
        var result = await _salaryReviewService.GetSalaryBandsAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Tạo khung lương mới cho ngạch chức danh.
    /// </summary>
    [HttpPost("bands")]
    [ProducesResponseType(typeof(SalaryBandDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSalaryBand([FromBody] CreateSalaryBandRequest request, CancellationToken cancellationToken)
    {
        var result = await _salaryReviewService.CreateSalaryBandAsync(request, cancellationToken);
        return Created($"/api/v1/salary-reviews/bands/{result.Id}", result);
    }

    /// <summary>
    /// Lấy danh sách các chu kỳ xét tăng lương định kỳ.
    /// </summary>
    [HttpGet("cycles")]
    [ProducesResponseType(typeof(IReadOnlyList<SalaryReviewCycleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCycles(CancellationToken cancellationToken)
    {
        var result = await _salaryReviewService.GetCyclesAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Tạo chu kỳ xét tăng lương mới (Draft).
    /// </summary>
    [HttpPost("cycles")]
    [ProducesResponseType(typeof(SalaryReviewCycleDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCycle([FromBody] CreateSalaryReviewCycleRequest request, CancellationToken cancellationToken)
    {
        var result = await _salaryReviewService.CreateCycleAsync(request, cancellationToken);
        return Created($"/api/v1/salary-reviews/cycles/{result.Id}", result);
    }

    /// <summary>
    /// Mở chu kỳ xét tăng lương và tự động phân bổ ngân sách cho các đơn vị.
    /// </summary>
    [HttpPost("cycles/{id}/open")]
    [ProducesResponseType(typeof(SalaryReviewCycleDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> OpenCycle(int id, [FromQuery] decimal defaultBudgetRate = 0.08m, CancellationToken cancellationToken = default)
    {
        var result = await _salaryReviewService.OpenCycleAsync(id, defaultBudgetRate, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Tổng Giám Đốc điều chỉnh ngân sách đơn vị.
    /// </summary>
    [HttpPut("budgets")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateBudget([FromBody] UpdateSalaryBudgetRequest request, CancellationToken cancellationToken)
    {
        await _salaryReviewService.UpdateBudgetAsync(request, CurrentEmployeeId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Lấy danh sách gợi ý % tăng lương cho Trưởng đơn vị kèm co giãn ngân sách (Merit Suggestions).
    /// </summary>
    [HttpGet("cycles/{cycleId}/units/{unitId}/suggestions")]
    [ProducesResponseType(typeof(IReadOnlyList<UnitMeritSuggestionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnitSuggestions(int cycleId, int unitId, CancellationToken cancellationToken)
    {
        var result = await _salaryReviewService.GetUnitSuggestionsAsync(cycleId, unitId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Trưởng đơn vị nộp danh sách đề xuất tăng lương của phòng ban.
    /// Kiểm tra nghiêm ngặt hạn mức ngân sách: Không cho nộp nếu vượt (422 ERR_SAL_BUDGET_EXCEEDED).
    /// </summary>
    [HttpPost("units/submit")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SubmitUnitReview([FromBody] UnitMeritSubmissionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _salaryReviewService.SubmitUnitReviewAsync(request, CurrentEmployeeId, cancellationToken);
            return Ok(new { success = true, message = "Nộp danh sách xét tăng lương đơn vị thành công." });
        }
        catch (DomainException ex) when (ex.ErrorCode == "ERR_SAL_BUDGET_EXCEEDED")
        {
            return UnprocessableEntity(new { errorCode = ex.ErrorCode, message = ex.Message });
        }
    }

    /// <summary>
    /// Khởi tạo đề xuất tăng lương Đặc cách (Special Adjustment).
    /// Tự động bổ sung bước Hội đồng quản trị nếu tăng > 30% hoặc vượt Max khung lương.
    /// </summary>
    [HttpPost("adjustments/special")]
    [ProducesResponseType(typeof(SalaryAdjustmentDetailDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSpecialAdjustment([FromBody] SpecialSalaryAdjustmentRequest request, CancellationToken cancellationToken)
    {
        var result = await _salaryReviewService.CreateSpecialAdjustmentAsync(request, CurrentEmployeeId, cancellationToken);
        return Created($"/api/v1/salary-reviews/adjustments/{result.Id}", result);
    }

    /// <summary>
    /// Phê duyệt từng bước trong quy trình tăng lương.
    /// Chống xung đột lợi ích: Chặn người được tăng tự duyệt hoặc người lập tự duyệt (403).
    /// </summary>
    [HttpPost("adjustments/approve-step")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ApproveStep([FromBody] ApproveAdjustmentStepRequest request, CancellationToken cancellationToken)
    {
        try
        {
            int approverId = request.ApproverId > 0 ? request.ApproverId : CurrentEmployeeId;
            var req = request with { ApproverId = approverId };
            await _salaryReviewService.ApproveStepAsync(req, cancellationToken);
            return Ok(new { success = true, message = "Phê duyệt bước thành công." });
        }
        catch (DomainException ex) when (ex.ErrorCode == "ERR_SAL_APPROVAL_CONFLICT_SELF" ||
                                         ex.ErrorCode == "ERR_SAL_PROPOSER_CANNOT_APPROVE" ||
                                         ex.ErrorCode == "ERR_SAL_APPROVAL_CONFLICT_RELATIVE")
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { errorCode = ex.ErrorCode, message = ex.Message });
        }
    }

    /// <summary>
    /// Từ chối tờ trình tăng lương.
    /// </summary>
    [HttpPost("adjustments/{id}/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RejectProposal(int id, [FromBody] string reason, CancellationToken cancellationToken)
    {
        await _salaryReviewService.RejectProposalAsync(id, CurrentEmployeeId, reason, cancellationToken);
        return Ok(new { success = true, message = "Từ chối tờ trình thành công." });
    }

    /// <summary>
    /// Tổng Giám Đốc phê duyệt toàn bộ đợt xét tăng lương.
    /// Tự động cập nhật mức lương mới vào hồ sơ nhân sự, sinh phụ lục hợp đồng số,
    /// và sinh các khoản truy lĩnh PayrollAdjustment nếu ngày duyệt sau ngày hiệu lực.
    /// </summary>
    [HttpPost("cycles/{id}/executive-approve")]
    [ProducesResponseType(typeof(SalaryReviewCycleDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApproveExecutiveCycle(int id, CancellationToken cancellationToken)
    {
        var result = await _salaryReviewService.ApproveExecutiveCycleAsync(id, CurrentEmployeeId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Báo cáo Total Rewards toàn diện: Sử dụng ngân sách, phân bố theo xếp loại, bình đẳng giới Nam/Nữ, chi phí gồm BHXH công ty.
    /// </summary>
    [HttpGet("cycles/{id}/reports/total-rewards")]
    [ProducesResponseType(typeof(TotalRewardsReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTotalRewardsReport(int id, CancellationToken cancellationToken)
    {
        var result = await _salaryReviewService.GetTotalRewardsReportAsync(id, cancellationToken);
        return Ok(result);
    }
}