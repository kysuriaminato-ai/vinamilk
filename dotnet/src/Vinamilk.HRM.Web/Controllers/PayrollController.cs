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

    public PayrollController(IMediator mediator)
    {
        _mediator = mediator;
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
    public async Task<IActionResult> Calculate(int payrollPeriodId, CancellationToken cancellationToken = default)
    {
        try
        {
            var count = await _mediator.Send(new CalculateMonthlyPayrollCommand(payrollPeriodId), cancellationToken);
            TempData["PayrollSuccess"] = $"Đã tính toán bảng lương theo tham số hiệu lực cuối kỳ cho {count} nhân sự.";
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
}

