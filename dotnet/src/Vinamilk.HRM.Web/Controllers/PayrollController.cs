using System.Globalization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vinamilk.HRM.Application.Payroll;

namespace Vinamilk.HRM.Web.Controllers;

[Route("Payroll")]
[Authorize(Policy = "Payroll.View")]
public class PayrollController : Controller
{
    private readonly IMediator _mediator;
    private readonly IPayrollDocumentService _documentService;

    public PayrollController(IMediator mediator, IPayrollDocumentService documentService)
    {
        _mediator = mediator;
        _documentService = documentService;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(int? periodId = null, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Bảng tính lương 3P, BHXH & Thuế TNCN";
        ViewData["BreadcrumbParent"] = "Chấm công & Tiền lương (C&B)";

        var model = await _mediator.Send(new GetPayrollManagementOverviewQuery(periodId), cancellationToken);
        return View(model);
    }

    [HttpPost("CreatePeriod")]
    [Authorize(Policy = "Payroll.Calculate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePeriod(int year, int month, decimal standardWorkdays = 26m, CancellationToken cancellationToken = default)
    {
        try
        {
            var id = await _mediator.Send(new CreatePayrollPeriodCommand(year, month, standardWorkdays), cancellationToken);
            TempData["PayrollSuccess"] = $"Đã tạo / mở kỳ lương tháng {month:D2}/{year}.";
            return RedirectToAction(nameof(Index), new { periodId = id });
        }
        catch (Exception ex)
        {
            TempData["PayrollError"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost("Calculate")]
    [Authorize(Policy = "Payroll.Calculate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Calculate(
        int payrollPeriodId,
        int? employeeId = null,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var count = await _mediator.Send(new CalculateMonthlyPayrollCommand(payrollPeriodId, employeeId, notes), cancellationToken);
            TempData["PayrollSuccess"] = employeeId.HasValue
                ? $"Đã tính lại lương cho nhân sự #{employeeId.Value} và lưu lịch sử phiên bản."
                : $"Đã tính toán bảng lương theo tham số hiệu lực cuối kỳ cho {count} nhân sự.";
        }
        catch (Exception ex)
        {
            TempData["PayrollError"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { periodId = payrollPeriodId });
    }

    [HttpPost("Submit")]
    [Authorize(Policy = "Payroll.Calculate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int payrollPeriodId, string? comment = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var wfId = await _mediator.Send(new SubmitPayrollPeriodCommand(payrollPeriodId, comment), cancellationToken);
            TempData["PayrollSuccess"] = $"Đã trình duyệt kỳ lương lên luồng phê duyệt (Workflow #{wfId}). Người tính không thể tự duyệt kỳ này.";
        }
        catch (Exception ex)
        {
            TempData["PayrollError"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { periodId = payrollPeriodId });
    }

    [HttpPost("Approve")]
    [Authorize(Policy = "Payroll.Approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(
        int payrollPeriodId,
        bool approve = true,
        string? comment = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var status = await _mediator.Send(new ApprovePayrollPeriodCommand(payrollPeriodId, approve, comment), cancellationToken);
            TempData["PayrollSuccess"] = approve
                ? $"Đã phê duyệt kỳ lương (trạng thái hiện tại: {status})."
                : $"Đã từ chối / trả lại kỳ lương để hiệu chỉnh (trạng thái: {status}).";
        }
        catch (Exception ex)
        {
            TempData["PayrollError"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { periodId = payrollPeriodId });
    }

    [HttpPost("Lock")]
    [Authorize(Policy = "Payroll.Lock")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Lock(int payrollPeriodId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _mediator.Send(new LockPayrollPeriodCommand(payrollPeriodId), cancellationToken);
            TempData["PayrollSuccess"] = "Đã khóa kỳ lương thành công. Không thể chỉnh sửa tham số hoặc tính lại cho kỳ đã khóa.";
        }
        catch (Exception ex)
        {
            TempData["PayrollError"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { periodId = payrollPeriodId });
    }

    [HttpPost("Unlock")]
    [Authorize(Roles = "SUPER_ADMIN,HR_DIRECTOR", Policy = "Payroll.Unlock")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(int payrollPeriodId, string reason, CancellationToken cancellationToken = default)
    {
        try
        {
            await _mediator.Send(new UnlockPayrollPeriodCommand(payrollPeriodId, reason), cancellationToken);
            TempData["PayrollSuccess"] = "Đã mở khóa kỳ lương về trạng thái Calculated và ghi nhận AuditLog.";
        }
        catch (Exception ex)
        {
            TempData["PayrollError"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { periodId = payrollPeriodId });
    }

    [HttpPost("MarkPaid")]
    [Authorize(Policy = "Payroll.Lock")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkPaid(int payrollPeriodId, string? paymentNote = null, CancellationToken cancellationToken = default)
    {
        try
        {
            await _mediator.Send(new MarkPayrollPeriodPaidCommand(payrollPeriodId, paymentNote), cancellationToken);
            TempData["PayrollSuccess"] = "Đã xác nhận chi trả kỳ lương thành công (Paid).";
        }
        catch (Exception ex)
        {
            TempData["PayrollError"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { periodId = payrollPeriodId });
    }

    [HttpPost("SendPayslips")]
    [Authorize(Policy = "Payroll.Approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendPayslips(int payrollPeriodId, CancellationToken cancellationToken = default)
    {
        try
        {
            var report = await _mediator.Send(new SendPayslipsBatchCommand(payrollPeriodId), cancellationToken);
            TempData["PayrollSuccess"] = $"Đã xử lý gửi {report.TotalCount} phiếu lương (Thành công: {report.SentCount}, Đang chờ: {report.QueuedCount}, Thất bại: {report.FailedCount}).";
        }
        catch (Exception ex)
        {
            TempData["PayrollError"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { periodId = payrollPeriodId });
    }

    [HttpGet("DownloadPayslip/{payrollRecordId:long}")]
    [Authorize(Policy = "Payroll.View")]
    public async Task<IActionResult> DownloadPayslip(long payrollRecordId, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _mediator.Send(new GetPayrollRecordPdfQuery(payrollRecordId, EnforceSelfOnly: false), cancellationToken);
            return File(result.PdfBytes, "application/pdf", result.FileName);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet("ExportBankTransfer")]
    [Authorize(Policy = "Payroll.Export")]
    public async Task<IActionResult> ExportBankTransfer(
        int payrollPeriodId,
        string format = "excel",
        string templateCode = "COMMON",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _mediator.Send(new ExportBankTransferFileQuery(payrollPeriodId, format, templateCode), cancellationToken);
            return File(result.FileBytes, result.ContentType, result.FileName);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet("ExportDepartmentSummaryExcel")]
    [Authorize(Policy = "Payroll.Export")]
    public async Task<IActionResult> ExportDepartmentSummaryExcel(int payrollPeriodId, CancellationToken cancellationToken = default)
    {
        try
        {
            var report = await _mediator.Send(new GetPayrollDepartmentSummaryReportQuery(payrollPeriodId), cancellationToken);
            var bytes = _documentService.ExportDepartmentSummaryExcel(report);
            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"TongHopLuongTheoPhongBan_{report.PeriodCode}.xlsx");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet("Parameters")]
    public async Task<IActionResult> Parameters(CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Quản trị Tham số Lương, LTTV 4 Vùng & Biểu thuế TNCN theo Hiệu lực";
        ViewData["BreadcrumbParent"] = "Chấm công & Tiền lương (C&B)";

        var model = await _mediator.Send(new GetPayrollManagementOverviewQuery(), cancellationToken);
        return View(model);
    }

    [HttpPost("SaveParameter")]
    [Authorize(Policy = "Payroll.Approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveParameter(
        int? id,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo,
        string? legalReference,
        string? notes,
        decimal statutoryBaseSalary,
        decimal minWageRegion1,
        decimal minWageRegion2,
        decimal minWageRegion3,
        decimal minWageRegion4,
        decimal socialInsuranceCapMultiplier,
        decimal unemploymentInsuranceCapMultiplier,
        decimal employeeSiRatePercent,
        decimal employeeHiRatePercent,
        decimal employeeUiRatePercent,
        decimal companySiRatePercent,
        decimal companyHiRatePercent,
        decimal companyUiRatePercent,
        decimal companyUnionFeeRatePercent,
        decimal selfDeduction,
        decimal dependentDeduction,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _mediator.Send(new SavePayrollStatutoryParameterCommand(
                id,
                effectiveFrom,
                effectiveTo,
                legalReference,
                notes,
                statutoryBaseSalary,
                minWageRegion1,
                minWageRegion2,
                minWageRegion3,
                minWageRegion4,
                socialInsuranceCapMultiplier,
                unemploymentInsuranceCapMultiplier,
                employeeSiRatePercent / 100m,
                employeeHiRatePercent / 100m,
                employeeUiRatePercent / 100m,
                companySiRatePercent / 100m,
                companyHiRatePercent / 100m,
                companyUiRatePercent / 100m,
                companyUnionFeeRatePercent / 100m,
                selfDeduction,
                dependentDeduction), cancellationToken);

            TempData["PayrollSuccess"] = $"Đã lưu bộ tham số lương hiệu lực từ {effectiveFrom:dd/MM/yyyy} và ghi AuditLog.";
        }
        catch (Exception ex)
        {
            TempData["PayrollError"] = ex.Message;
        }

        return RedirectToAction(nameof(Parameters));
    }

    [HttpPost("SavePitBrackets")]
    [Authorize(Policy = "Payroll.Approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePitBrackets(
        DateOnly? originalEffectiveFrom,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo,
        string? legalReference,
        List<int> bracketLevel,
        List<decimal> lowerBound,
        List<string?> upperBound,
        List<decimal> taxRatePercent,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var items = new List<PitBracketInputItem>();
            for (int i = 0; i < bracketLevel.Count; i++)
            {
                decimal? parsedUpper = null;
                if (i < upperBound.Count && !string.IsNullOrWhiteSpace(upperBound[i]) &&
                    decimal.TryParse(upperBound[i], NumberStyles.Number, CultureInfo.InvariantCulture, out var val))
                {
                    parsedUpper = val;
                }

                items.Add(new PitBracketInputItem(
                    bracketLevel[i],
                    lowerBound[i],
                    parsedUpper,
                    taxRatePercent[i]));
            }

            await _mediator.Send(new SavePitBracketGroupCommand(
                originalEffectiveFrom,
                effectiveFrom,
                effectiveTo,
                legalReference,
                items), cancellationToken);

            TempData["PayrollSuccess"] = $"Đã lưu biểu thuế TNCN hiệu lực từ {effectiveFrom:dd/MM/yyyy} và ghi AuditLog.";
        }
        catch (Exception ex)
        {
            TempData["PayrollError"] = ex.Message;
        }

        return RedirectToAction(nameof(Parameters));
    }

    // =========================================================================
    // CL5: CẤU TRÚC LƯƠNG, HỒ SƠ LƯƠNG HIỆU LỰC, MẪU LƯƠNG & CÔNG THỨC NCalc
    // =========================================================================

    [HttpGet("SalaryStructures")]
    public async Task<IActionResult> SalaryStructures(int? employeeId = null, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Cấu trúc lương, Công thức NCalc & Hồ sơ lương hiệu lực";
        ViewData["BreadcrumbParent"] = "Chấm công & Tiền lương (C&B)";

        var model = await _mediator.Send(new GetSalaryStructuresOverviewQuery(employeeId), cancellationToken);
        return View(model);
    }

    public sealed record TestFormulaAjaxRequest(
        string FormulaExpression,
        string? TargetComponentCode,
        bool IsTaxable,
        decimal TaxExemptLimit,
        string? TaxExemptLimitParameterCode,
        Dictionary<string, decimal>? SampleVariables);

    [HttpPost("TestFormula")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TestFormula(
        [FromBody] TestFormulaAjaxRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new ValidateAndTestSalaryFormulaQuery(
            request.FormulaExpression,
            request.TargetComponentCode,
            request.SampleVariables,
            request.IsTaxable,
            request.TaxExemptLimit,
            request.TaxExemptLimitParameterCode), cancellationToken);

        return Json(result);
    }

    [HttpPost("SaveComponent")]
    [Authorize(Policy = "Payroll.Approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveComponent(
        int? id,
        string componentCode,
        string componentName,
        string componentType,
        string calculationType,
        decimal defaultAmount,
        bool isTaxable,
        decimal taxExemptLimit,
        string? taxExemptLimitParameterCode,
        bool isSocialInsuranceBase,
        bool showOnPayslip,
        string? formulaExpression,
        int sortOrder,
        bool isActive = true,
        string? legalReference = null,
        bool needsHrVerification = false,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _mediator.Send(new SaveSalaryComponentCommand(
                id,
                componentCode,
                componentName,
                componentType,
                calculationType,
                defaultAmount,
                isTaxable,
                taxExemptLimit,
                taxExemptLimitParameterCode,
                isSocialInsuranceBase,
                showOnPayslip,
                formulaExpression,
                sortOrder,
                isActive,
                legalReference,
                needsHrVerification,
                description), cancellationToken);

            TempData["PayrollSuccess"] = $"Đã lưu thành phần lương '{componentCode?.Trim().ToUpperInvariant()}' (đã kiểm tra cú pháp & vòng lặp NCalc).";
        }
        catch (Exception ex)
        {
            TempData["PayrollError"] = ex.Message;
        }

        return RedirectToAction(nameof(SalaryStructures));
    }

    [HttpPost("SaveTemplate")]
    [Authorize(Policy = "Payroll.Approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveTemplate(
        int? id,
        string templateCode,
        string templateName,
        string staffGroup,
        string defaultPayFrequency,
        decimal defaultProbationSalaryRatePercent,
        string roundingRule,
        bool isDefaultForCompany,
        bool isActive,
        string? legalReference,
        bool needsHrVerification,
        string? description,
        int[]? selectedComponentIds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var items = selectedComponentIds?
                .Select((compId, idx) => new SaveSalaryTemplateItemInput(compId, idx + 1))
                .ToList();

            await _mediator.Send(new SaveSalaryTemplateCommand(
                id,
                templateCode,
                templateName,
                staffGroup,
                defaultPayFrequency,
                defaultProbationSalaryRatePercent,
                roundingRule,
                isDefaultForCompany,
                isActive,
                legalReference,
                needsHrVerification,
                description,
                items), cancellationToken);

            TempData["PayrollSuccess"] = $"Đã lưu mẫu cấu trúc lương '{templateCode?.Trim().ToUpperInvariant()}' và ghi AuditLog.";
        }
        catch (Exception ex)
        {
            TempData["PayrollError"] = ex.Message;
        }

        return RedirectToAction(nameof(SalaryStructures));
    }

    [HttpPost("SaveSalaryProfile")]
    [Authorize(Policy = "Payroll.Approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSalaryProfile(
        int employeeId,
        DateOnly effectiveFrom,
        decimal baseSalary,
        decimal insuranceSalary,
        int? salaryStepId,
        int? salaryTemplateId,
        string payMode,
        decimal unitRate,
        bool isProbation,
        decimal probationRatePercent,
        decimal positionAllowance,
        decimal otherAllowance,
        bool isUnionMember,
        string? decisionNumber,
        string? reason,
        int[]? allowanceComponentIds,
        decimal[]? allowanceAmounts,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fixedAllowances = new List<EmployeeSalaryAllowanceInput>();
            if (allowanceComponentIds != null && allowanceAmounts != null)
            {
                for (int i = 0; i < Math.Min(allowanceComponentIds.Length, allowanceAmounts.Length); i++)
                {
                    if (allowanceComponentIds[i] > 0 && allowanceAmounts[i] > 0m)
                        fixedAllowances.Add(new EmployeeSalaryAllowanceInput(allowanceComponentIds[i], allowanceAmounts[i]));
                }
            }

            await _mediator.Send(new SaveEmployeeSalaryProfileCommand(
                employeeId,
                effectiveFrom,
                baseSalary,
                insuranceSalary,
                salaryStepId,
                salaryTemplateId,
                payMode,
                unitRate,
                isProbation,
                probationRatePercent,
                positionAllowance,
                otherAllowance,
                isUnionMember,
                decisionNumber,
                reason,
                fixedAllowances), cancellationToken);

            TempData["PayrollSuccess"] = $"Đã lưu hồ sơ lương hiệu lực từ {effectiveFrom:dd/MM/yyyy} cho nhân viên #{employeeId}.";
        }
        catch (Exception ex)
        {
            TempData["PayrollError"] = ex.Message;
        }

        return RedirectToAction(nameof(SalaryStructures), new { employeeId });
    }
}

