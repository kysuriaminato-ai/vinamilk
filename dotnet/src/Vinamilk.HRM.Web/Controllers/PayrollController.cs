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
        if (model.SelectedPeriod != null)
        {
            ViewBag.AttendanceLockStatus = await _mediator.Send(
                new CheckPayrollAttendanceLockStatusQuery(model.SelectedPeriod.Year, model.SelectedPeriod.Month),
                cancellationToken);
        }
        return View(model);
    }

    [HttpPost("CreatePeriod")]
    [Authorize(Policy = "Payroll.Calculate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePeriod(
        int year,
        int month,
        decimal standardWorkdays = 26m,
        string periodType = "Monthly",
        bool createNewBatch = false,
        DateOnly? disbursementDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var id = await _mediator.Send(
                new CreatePayrollPeriodCommand(year, month, standardWorkdays, periodType, DisbursementDate: disbursementDate, CreateNewBatch: createNewBatch),
                cancellationToken);
            TempData["PayrollSuccess"] = $"Đã tạo / mở kỳ lương ({periodType}) tháng {month:D2}/{year}.";
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
        bool allowUnlockedAttendance = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var count = await _mediator.Send(
                new CalculateMonthlyPayrollCommand(payrollPeriodId, employeeId, notes, RequireLockedAttendance: !allowUnlockedAttendance),
                cancellationToken);
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
    [Authorize(Policy = "Payroll.Submit")]
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
        decimal? companyOccupationalAccidentRatePercent = null,
        decimal? employeeUnionFeeRatePercent = null,
        decimal? pitNonResidentRatePercent = null,
        decimal? pitShortTermRatePercent = null,
        decimal? pitShortTermThreshold = null,
        decimal? otMultiplier150 = null,
        decimal? otMultiplier200 = null,
        decimal? otMultiplier300 = null,
        decimal? otNightShiftBonusRatePercent = null,
        decimal? otNightOvertimeExtraRatePercent = null,
        bool isOtExcessTaxExempt = true,
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
                dependentDeduction,
                companyOccupationalAccidentRatePercent / 100m,
                employeeUnionFeeRatePercent / 100m,
                pitNonResidentRatePercent / 100m,
                pitShortTermRatePercent / 100m,
                pitShortTermThreshold,
                otMultiplier150,
                otMultiplier200,
                otMultiplier300,
                otNightShiftBonusRatePercent / 100m,
                otNightOvertimeExtraRatePercent / 100m,
                isOtExcessTaxExempt), cancellationToken);

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
        string taxResidencyMode = "ResidentStandard",
        bool hasPitCommitment08 = false,
        string? commitment08Number = null,
        bool isNetContract = false,
        decimal? targetNetSalary = null,
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
                fixedAllowances,
                taxResidencyMode,
                hasPitCommitment08,
                commitment08Number,
                isNetContract,
                targetNetSalary), cancellationToken);

            TempData["PayrollSuccess"] = $"Đã lưu hồ sơ lương hiệu lực từ {effectiveFrom:dd/MM/yyyy} cho nhân viên #{employeeId}.";
        }
        catch (Exception ex)
        {
            TempData["PayrollError"] = ex.Message;
        }

        return RedirectToAction(nameof(SalaryStructures), new { employeeId });
    }

    // =========================================================================
    // CL6: GIẢ LẬP CHẠY LƯƠNG, DRILL-DOWN TRUY VẾT, NET->GROSS & TẠM ỨNG/VAY/TRUY LĨNH
    // =========================================================================

    [HttpGet("Simulation")]
    [Authorize(Policy = "Payroll.Calculate")]
    public async Task<IActionResult> Simulation(int periodId, int? employeeId = null, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Giả lập chạy lương (Simulation) & So sánh kỳ trước";
        ViewData["BreadcrumbParent"] = "Chấm công & Tiền lương (C&B)";

        var model = await _mediator.Send(new SimulatePayrollPeriodQuery(periodId, employeeId), cancellationToken);
        return View(model);
    }

    [HttpGet("RecordTrace")]
    public async Task<IActionResult> RecordTrace(long recordId, CancellationToken cancellationToken = default)
    {
        var trace = await _mediator.Send(new GetPayrollRecordTraceQuery(recordId), cancellationToken);
        return Json(trace);
    }

    [HttpGet("DownloadTracePdf")]
    public async Task<IActionResult> DownloadTracePdf(long recordId, CancellationToken cancellationToken = default)
    {
        var trace = await _mediator.Send(new GetPayrollRecordTraceQuery(recordId), cancellationToken);
        var pdfBytes = _documentService.GeneratePayrollTracePdf(trace);
        return File(pdfBytes, "application/pdf", $"DienGiaiTinhLuong_{trace.EmployeeCode}_{trace.PeriodCode}.pdf");
    }

    [HttpGet("Calculator")]
    public async Task<IActionResult> Calculator(CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Máy tính lương chuẩn pháp lý (Gross ↔ Net & 15 bước)";
        ViewData["BreadcrumbParent"] = "Chấm công & Tiền lương (C&B)";

        var input = new AdhocPayrollCalculatorInput(EffectiveDate: DateOnly.FromDateTime(DateTime.Today));
        var result = await _mediator.Send(new CalculateAdhocPayrollQuery(input), cancellationToken);
        ViewBag.CalculatorInput = input;
        return View(result);
    }

    [HttpPost("Calculator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Calculator(AdhocPayrollCalculatorInput input, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Máy tính lương chuẩn pháp lý (Gross ↔ Net & 15 bước)";
        ViewData["BreadcrumbParent"] = "Chấm công & Tiền lương (C&B)";

        var result = await _mediator.Send(new CalculateAdhocPayrollQuery(input), cancellationToken);
        ViewBag.CalculatorInput = input;
        return View(result);
    }

    [HttpPost("DownloadCalculatorTracePdf")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DownloadCalculatorTracePdf(AdhocPayrollCalculatorInput input, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new CalculateAdhocPayrollQuery(input), cancellationToken);
        var pdfBytes = _documentService.GeneratePayrollTracePdf(
            result.Trace,
            result.EffectiveDate,
            $"Tính theo Máy tính lương ad-hoc (Tham số hiệu lực ngày {result.EffectiveDate:dd/MM/yyyy}: {result.StatutoryParameter.LegalReference})");
        return File(pdfBytes, "application/pdf", $"MayTinhLuong_DienGiai_{result.EffectiveDate:yyyyMMdd}.pdf");
    }

    [HttpGet("Operations")]
    [Authorize(Policy = "Payroll.Calculate")]
    public async Task<IActionResult> Operations(CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Tạm ứng, Khoản vay, Truy lĩnh/Truy thu & Quy đổi Net → Gross";
        ViewData["BreadcrumbParent"] = "Chấm công & Tiền lương (C&B)";

        var model = await _mediator.Send(new GetPayrollOperationsOverviewQuery(), cancellationToken);
        return View(model);
    }

    [HttpPost("SolveNetToGross")]
    [Authorize(Policy = "Payroll.Calculate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SolveNetToGross(
        [FromBody] NetToGrossInput input,
        CancellationToken cancellationToken = default)
    {
        var solution = await _mediator.Send(new SolveNetToGrossQuery(input), cancellationToken);
        return Json(solution);
    }

    [HttpPost("CreateAdvance")]
    [Authorize(Policy = "Payroll.Calculate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAdvance(
        int employeeId,
        DateOnly advanceDate,
        int targetYear,
        int targetMonth,
        decimal amount,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _mediator.Send(new CreateSalaryAdvanceCommand(employeeId, advanceDate, targetYear, targetMonth, amount, reason), cancellationToken);
            TempData["PayrollSuccess"] = $"Đã tạo phiếu tạm ứng lương {amount:N0} đ cho kỳ {targetMonth:D2}/{targetYear}.";
        }
        catch (Exception ex)
        {
            TempData["PayrollError"] = ex.Message;
        }
        return RedirectToAction(nameof(Operations));
    }

    [HttpPost("CreateLoan")]
    [Authorize(Policy = "Payroll.Calculate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateLoan(
        int employeeId,
        DateOnly disbursementDate,
        decimal principalAmount,
        int termMonths,
        int startYear,
        int startMonth,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _mediator.Send(new CreateEmployeeLoanCommand(employeeId, disbursementDate, principalAmount, termMonths, startYear, startMonth, reason), cancellationToken);
            TempData["PayrollSuccess"] = $"Đã tạo khoản vay {principalAmount:N0} đ trả góp {termMonths} tháng.";
        }
        catch (Exception ex)
        {
            TempData["PayrollError"] = ex.Message;
        }
        return RedirectToAction(nameof(Operations));
    }

    [HttpPost("CreateAdjustment")]
    [Authorize(Policy = "Payroll.Calculate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAdjustment(
        int employeeId,
        string adjustmentType,
        int targetYear,
        int targetMonth,
        decimal amount,
        string reason,
        int? sourceLockedPeriodId = null,
        bool isTaxable = true,
        bool? recalculateTaxInSourcePeriod = null,
        bool autoAssignNextOpenPeriod = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _mediator.Send(new CreatePayrollAdjustmentCommand(
                employeeId,
                adjustmentType,
                targetYear,
                targetMonth,
                amount,
                reason,
                sourceLockedPeriodId,
                isTaxable,
                recalculateTaxInSourcePeriod,
                autoAssignNextOpenPeriod), cancellationToken);
            TempData["PayrollSuccess"] = $"Đã tạo phiếu điều chỉnh ({adjustmentType}) {amount:N0} đ.";
        }
        catch (Exception ex)
        {
            TempData["PayrollError"] = ex.Message;
        }
        return RedirectToAction(nameof(Operations));
    }

    // =========================================================================
    // CL7: CHI TIẾT KHOẢN VAY, TẤT TOÁN SỚM & QUYẾT TOÁN NGHỈ VIỆC (FINAL SETTLEMENT)
    // =========================================================================

    [HttpGet("LoanDetail")]
    [Authorize(Policy = "Payroll.Calculate")]
    public async Task<IActionResult> LoanDetail(int id, CancellationToken cancellationToken = default)
    {
        var detail = await _mediator.Send(new GetEmployeeLoanDetailQuery(id), cancellationToken);
        return Json(detail);
    }

    [HttpPost("SettleLoanEarly")]
    [Authorize(Policy = "Payroll.Calculate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SettleLoanEarly(
        int employeeLoanId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var settled = await _mediator.Send(new SettleEmployeeLoanEarlyCommand(employeeLoanId, reason), cancellationToken);
            TempData["PayrollSuccess"] = $"Đã tất toán sớm khoản vay #{employeeLoanId} (số tiền còn lại {settled:N0} đ).";
        }
        catch (Exception ex)
        {
            TempData["PayrollError"] = ex.Message;
        }
        return RedirectToAction(nameof(Operations));
    }

    [HttpGet("TerminationSettlements")]
    [Authorize(Policy = "Payroll.Calculate")]
    public async Task<IActionResult> TerminationSettlements(CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Quyết toán Nghỉ việc (Final Settlement & Trợ cấp thôi việc)";
        ViewData["BreadcrumbParent"] = "Chấm công & Tiền lương (C&B)";

        var model = await _mediator.Send(new GetTerminationSettlementsOverviewQuery(), cancellationToken);
        return View(model);
    }

    [HttpPost("CalculateTerminationSettlement")]
    [Authorize(Policy = "Payroll.Calculate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CalculateTerminationSettlement(
        int employeeId,
        DateOnly lastWorkingDate,
        string terminationType = "Resignation",
        decimal standardWorkdays = 26m,
        decimal? unemploymentInsuranceCoveredMonthsOverride = null,
        bool deductEmployeeInsuranceOnFinalMonth = true,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var res = await _mediator.Send(new CalculateTerminationSettlementCommand(
                employeeId,
                lastWorkingDate,
                terminationType,
                standardWorkdays,
                unemploymentInsuranceCoveredMonthsOverride,
                deductEmployeeInsuranceOnFinalMonth,
                notes), cancellationToken);
            TempData["PayrollSuccess"] = $"Đã lập phiếu quyết toán nghỉ việc {res.SettlementCode} cho {res.EmployeeName} — Thực nhận: {res.NetSettlementAmount:N0} đ.";
        }
        catch (Exception ex)
        {
            TempData["PayrollError"] = ex.Message;
        }
        return RedirectToAction(nameof(TerminationSettlements));
    }

    [HttpPost("ApproveTerminationSettlement")]
    [Authorize(Policy = "Payroll.Approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveTerminationSettlement(
        int id,
        bool markAsPaid = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var code = await _mediator.Send(new ApproveAndSettleTerminationCommand(id, markAsPaid), cancellationToken);
            TempData["PayrollSuccess"] = $"Đã phê duyệt & chốt quyết toán nghỉ việc {code} (đã cấn trừ tạm ứng & tất toán khoản vay).";
        }
        catch (Exception ex)
        {
            TempData["PayrollError"] = ex.Message;
        }
        return RedirectToAction(nameof(TerminationSettlements));
    }

    [HttpGet("DownloadTerminationSettlementPdf")]
    [Authorize(Policy = "Payroll.Calculate")]
    public async Task<IActionResult> DownloadTerminationSettlementPdf(int id, CancellationToken cancellationToken = default)
    {
        var pdf = await _mediator.Send(new GetTerminationSettlementPdfQuery(id), cancellationToken);
        return File(pdf.PdfBytes, "application/pdf", pdf.FileName);
    }

    // =========================================================================
    // CL8: BÁO CÁO LƯƠNG, BHXH (D02-LT), THUẾ TNCN, HẠCH TOÁN KẾ TOÁN & DASHBOARD C&B
    // =========================================================================

    [HttpGet("Reports")]
    public async Task<IActionResult> Reports(
        int? periodId = null,
        int? year = null,
        int? month = null,
        int? quarter = null,
        int? comparePrevPeriodId = null,
        int? organizationUnitId = null,
        string tab = "unit",
        CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Báo cáo Lương, BHXH (D02-LT), Thuế TNCN & Hạch toán Kế toán";
        ViewData["BreadcrumbParent"] = "Chấm công & Tiền lương (C&B)";

        var overview = await _mediator.Send(new GetPayrollManagementOverviewQuery(periodId), cancellationToken);
        var selectedPeriod = overview.SelectedPeriod ?? overview.Periods.FirstOrDefault();

        int effectiveYear = year ?? selectedPeriod?.Year ?? DateTime.Today.Year;
        int effectiveMonth = month ?? selectedPeriod?.Month ?? DateTime.Today.Month;

        DetailedUnitPayrollReportDto? unitReport = null;
        CostCenterLaborCostReportDto? costCenterReport = null;
        PayrollAccountingJournalDto? accountingJournal = null;
        PayrollPeriodComparisonDto? comparison = null;
        SocialInsuranceMonthlyReportDto? siReport = null;

        if (selectedPeriod is not null)
        {
            unitReport = await _mediator.Send(new GetDetailedUnitPayrollReportQuery(selectedPeriod.Id, organizationUnitId), cancellationToken);
            costCenterReport = await _mediator.Send(new GetCostCenterLaborCostQuery(selectedPeriod.Id), cancellationToken);
            accountingJournal = await _mediator.Send(new GeneratePayrollAccountingEntriesQuery(selectedPeriod.Id), cancellationToken);

            var prevId = comparePrevPeriodId
                ?? overview.Periods.FirstOrDefault(p => p.Id != selectedPeriod.Id && (p.Year < selectedPeriod.Year || (p.Year == selectedPeriod.Year && p.Month < selectedPeriod.Month)))?.Id;
            if (prevId.HasValue)
            {
                comparison = await _mediator.Send(new ComparePayrollPeriodsQuery(selectedPeriod.Id, prevId.Value), cancellationToken);
            }

            try
            {
                siReport = await _mediator.Send(
                    new GetSocialInsuranceMonthlyReportQuery(effectiveYear, effectiveMonth, selectedPeriod.Id),
                    cancellationToken);
            }
            catch (KeyNotFoundException)
            {
                siReport = null;
            }
        }

        var fundSummary = await _mediator.Send(new GetPayrollFundSummaryQuery(effectiveYear), cancellationToken);
        var pitDeclaration = await _mediator.Send(new GetPitDeclarationSummaryQuery(effectiveYear, quarter.HasValue ? null : effectiveMonth, quarter), cancellationToken);
        var pitFinalization = await _mediator.Send(new GetAnnualPitFinalizationQuery(effectiveYear), cancellationToken);
        var accountConfigs = await _mediator.Send(new GetAccountingAccountConfigsQuery(), cancellationToken);
        var siTemplates = await _mediator.Send(new GetSocialInsuranceTemplatesQuery(), cancellationToken);

        var vm = new PayrollReportsWorkspaceViewModel(
            ActiveTab: tab.ToLowerInvariant(),
            EffectiveYear: effectiveYear,
            EffectiveMonth: effectiveMonth,
            EffectiveQuarter: quarter,
            OrganizationUnitId: organizationUnitId,
            ComparePreviousPeriodId: comparePrevPeriodId,
            Periods: overview.Periods,
            SelectedPeriod: selectedPeriod,
            UnitReport: unitReport,
            FundSummary: fundSummary,
            PeriodComparison: comparison,
            CostCenterReport: costCenterReport,
            SocialInsuranceReport: siReport,
            PitDeclaration: pitDeclaration,
            PitFinalization: pitFinalization,
            AccountingJournal: accountingJournal,
            AccountingConfigs: accountConfigs,
            SocialInsuranceTemplates: siTemplates);

        return View(vm);
    }

    [HttpGet("CompensationDashboard")]
    public async Task<IActionResult> CompensationDashboard(int? year = null, int? periodId = null, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Dashboard Quản trị Tiền lương & Chi phí Nhân sự (C&B)";
        ViewData["BreadcrumbParent"] = "Chấm công & Tiền lương (C&B)";

        var overview = await _mediator.Send(new GetPayrollManagementOverviewQuery(periodId), cancellationToken);
        int effectiveYear = year ?? overview.SelectedPeriod?.Year ?? overview.Periods.FirstOrDefault()?.Year ?? DateTime.Today.Year;
        int? focusId = periodId ?? overview.SelectedPeriod?.Id;

        var dashboard = await _mediator.Send(new GetCompensationDashboardQuery(effectiveYear, focusId), cancellationToken);
        ViewBag.Periods = overview.Periods;
        return View(dashboard);
    }

    [HttpGet("ExportUnitPayrollA3Pdf")]
    [Authorize(Policy = "Payroll.Export")]
    public async Task<IActionResult> ExportUnitPayrollA3Pdf(int periodId, int? organizationUnitId = null, CancellationToken cancellationToken = default)
    {
        var pdf = await _mediator.Send(new ExportDetailedUnitPayrollA3PdfQuery(periodId, organizationUnitId), cancellationToken);
        return File(pdf.PdfBytes, "application/pdf", pdf.FileName);
    }

    [HttpGet("ExportSocialInsuranceExcel")]
    [Authorize(Policy = "Payroll.Export")]
    public async Task<IActionResult> ExportSocialInsuranceExcel(
        int year,
        int month,
        int? periodId = null,
        string templateCode = "D02-LT",
        CancellationToken cancellationToken = default)
    {
        var file = await _mediator.Send(new ExportSocialInsuranceExcelQuery(year, month, templateCode, periodId), cancellationToken);
        return File(file.FileBytes, file.ContentType, file.FileName);
    }

    [HttpGet("DownloadPitCertificatePdf")]
    [Authorize(Policy = "Payroll.Export")]
    public async Task<IActionResult> DownloadPitCertificatePdf(
        int employeeId,
        int taxYear,
        bool persist = true,
        CancellationToken cancellationToken = default)
    {
        var pdf = await _mediator.Send(new IssueAndGetPitCertificatePdfQuery(employeeId, taxYear, persist, EnforceSelfOnly: false), cancellationToken);
        return File(pdf.PdfBytes, "application/pdf", pdf.FileName);
    }

    [HttpGet("ExportPitSettlementExcel")]
    [Authorize(Policy = "Payroll.Export")]
    public async Task<IActionResult> ExportPitSettlementExcel(int taxYear, CancellationToken cancellationToken = default)
    {
        var file = await _mediator.Send(new ExportPitSettlementExcelQuery(taxYear), cancellationToken);
        return File(file.FileBytes, file.ContentType, file.FileName);
    }

    [HttpGet("ExportAccountingJournal")]
    [Authorize(Policy = "Payroll.Export")]
    public async Task<IActionResult> ExportAccountingJournal(
        int periodId,
        string format = "excel",
        CancellationToken cancellationToken = default)
    {
        var file = await _mediator.Send(new ExportAccountingJournalQuery(periodId, format), cancellationToken);
        return File(file.FileBytes, file.ContentType, file.FileName);
    }

    [HttpPost("SaveAccountingAccountConfig")]
    [Authorize(Policy = "Payroll.Approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAccountingAccountConfig(
        int id,
        string accountCode,
        string costCenterPattern,
        string legalReference,
        bool needsHrVerification = false,
        int? periodId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _mediator.Send(
                new SaveAccountingAccountConfigCommand(id, accountCode, costCenterPattern, legalReference, needsHrVerification),
                cancellationToken);
            TempData["PayrollSuccess"] = $"Đã cập nhật cấu hình tài khoản kế toán #{id} ({accountCode}).";
        }
        catch (Exception ex)
        {
            TempData["PayrollError"] = ex.Message;
        }
        return RedirectToAction(nameof(Reports), new { periodId, tab = "accounting" });
    }

    [HttpPost("SaveSocialInsuranceTemplate")]
    [Authorize(Policy = "Payroll.Approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSocialInsuranceTemplate(
        int id,
        string columnMappingsJson,
        string softwareVendor,
        string legalReference,
        bool needsHrVerification = false,
        int? periodId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _mediator.Send(
                new SaveSocialInsuranceTemplateCommand(id, columnMappingsJson, softwareVendor, legalReference, needsHrVerification),
                cancellationToken);
            TempData["PayrollSuccess"] = $"Đã cập nhật cấu hình cột mẫu BHXH #{id} (không cần sửa code).";
        }
        catch (Exception ex)
        {
            TempData["PayrollError"] = ex.Message;
        }
        return RedirectToAction(nameof(Reports), new { periodId, tab = "bhxh" });
    }
}

public sealed record PayrollReportsWorkspaceViewModel(
    string ActiveTab,
    int EffectiveYear,
    int EffectiveMonth,
    int? EffectiveQuarter,
    int? OrganizationUnitId,
    int? ComparePreviousPeriodId,
    IReadOnlyList<PayrollPeriodRowDto> Periods,
    PayrollPeriodRowDto? SelectedPeriod,
    DetailedUnitPayrollReportDto? UnitReport,
    PayrollFundSummaryReportDto FundSummary,
    PayrollPeriodComparisonDto? PeriodComparison,
    CostCenterLaborCostReportDto? CostCenterReport,
    SocialInsuranceMonthlyReportDto? SocialInsuranceReport,
    PitDeclarationSummaryDto PitDeclaration,
    PitAnnualFinalizationReportDto PitFinalization,
    PayrollAccountingJournalDto? AccountingJournal,
    IReadOnlyList<Domain.Entities.Payroll.PayrollAccountingAccountConfig> AccountingConfigs,
    IReadOnlyList<Domain.Entities.Payroll.SocialInsuranceReportTemplate> SocialInsuranceTemplates);

