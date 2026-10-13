#pragma warning disable CS8602
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Application.Common.Security;
using VinamilkHRM.Application.DTOs.Salaries;
using VinamilkHRM.Application.Interfaces;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Salaries;
using VinamilkHRM.Domain.Enums;
using VinamilkHRM.Infrastructure.Services.Printing;

namespace VinamilkHRM.WebApi.Controllers;

[ApiController]
[Route("api/v1/payroll")]
[Authorize]
public class PayrollController : ControllerBase
{
    private readonly IPayrollService _payrollService;
    private readonly IPayrollExportService _exportService;
    private readonly QuestPdfPayslipGenerator _questPdfGenerator;
    private readonly IApplicationDbContext _dbContext;
    private readonly IDataScope _dataScope;
    private readonly ICurrentUser _currentUser;

    public PayrollController(
        IPayrollService payrollService,
        IPayrollExportService exportService,
        QuestPdfPayslipGenerator questPdfGenerator,
        IApplicationDbContext dbContext,
        IDataScope dataScope,
        ICurrentUser currentUser)
    {
        _payrollService = payrollService;
        _exportService = exportService;
        _questPdfGenerator = questPdfGenerator;
        _dbContext = dbContext;
        _dataScope = dataScope;
        _currentUser = currentUser;
    }

    [HttpGet("payslips")]
    [Authorize(Policy = Permissions.LuongView)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaySlips([FromQuery] string? period, CancellationToken cancellationToken)
    {
        string p = string.IsNullOrWhiteSpace(period) ? DateTime.UtcNow.ToString("yyyy-MM") : period.Trim();

        // Vai trò Employee (ESS) chỉ xem được phiếu lương của chính mình
        if (_currentUser.IsInRole(Roles.Employee) &&
            !_currentUser.IsInRole(Roles.UnitManager) &&
            !_currentUser.IsInRole(Roles.ChiefAccountant) &&
            !_currentUser.IsInRole(Roles.PayrollAccountant))
        {
            if (!_dataScope.CurrentEmployeeId.HasValue)
            {
                return Ok(Array.Empty<object>());
            }

            var selfPayslip = await _payrollService.GetPaySlipAsync(_dataScope.CurrentEmployeeId.Value, p, cancellationToken);
            return Ok(selfPayslip != null ? new[] { selfPayslip } : Array.Empty<object>());
        }

        var allPayslips = await _payrollService.GetPaySlipsByPeriodAsync(p, cancellationToken);

        if (_dataScope.IsGlobalAccess)
        {
            return Ok(allPayslips);
        }

        // Lọc theo cây đơn vị được phép
        var authorizedUnitIds = await _dataScope.GetAuthorizedUnitIdsAsync(cancellationToken);
        var scopedEmployeeIds = await _dbContext.Employees
            .AsNoTracking()
            .Where(e => authorizedUnitIds.Contains(e.OrganizationalUnitId))
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);

        var filtered = allPayslips.Where(slip => scopedEmployeeIds.Contains(slip.EmployeeId)).ToList();
        return Ok(filtered);
    }

    [HttpGet("payslips/{id}")]
    [Authorize(Policy = Permissions.LuongView)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaySlipById(int id, CancellationToken cancellationToken)
    {
        var payslip = await _dbContext.PaySlips
            .AsNoTracking()
            .Include(p => p.Employee)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (payslip == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy phiếu lương",
                Detail = $"Phiếu lương với ID {id} không tồn tại."
            });
        }

        // Kiểm tra bảo mật
        if (_currentUser.IsInRole(Roles.Employee) &&
            !_currentUser.IsInRole(Roles.UnitManager) &&
            !_currentUser.IsInRole(Roles.ChiefAccountant) &&
            !_currentUser.IsInRole(Roles.PayrollAccountant) &&
            !_currentUser.IsInRole(Roles.SystemAdmin))
        {
            if (!_dataScope.CurrentEmployeeId.HasValue || _dataScope.CurrentEmployeeId.Value != payslip.EmployeeId)
            {
                return Forbid();
            }
        }
        else
        {
            bool canAccess = await _dataScope.CanAccessEmployeeAsync(payslip.EmployeeId, cancellationToken);
            if (!canAccess)
            {
                return Forbid();
            }
        }

        return Ok(payslip);
    }

    [HttpGet("payslips/{id}/pdf")]
    [Authorize(Policy = Permissions.LuongView)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaySlipPdf(int id, [FromQuery] string? password, CancellationToken cancellationToken)
    {
        var payslip = await _dbContext.PaySlips
            .Include(p => p.Employee)
                .ThenInclude(e => e.OrganizationalUnit)
            .Include(p => p.Employee)
                .ThenInclude(e => e.StandardJobTitle)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (payslip == null)
        {
            return NotFound(new ProblemDetails { Status = 404, Detail = $"Không tìm thấy phiếu lương Id={id}" });
        }

        if (payslip.Employee == null) return BadRequest(new ProblemDetails { Detail = "Phiếu lương chưa gán thông tin nhân sự" });
        var pdfBytes = _questPdfGenerator.GeneratePayslipPdf(payslip, payslip.Employee, payslip.Employee.OrganizationalUnit!, password);
        return File(pdfBytes, "application/pdf", $"Payslip_{payslip.Employee?.EmployeeCode ?? "EMP"}_{payslip.PayrollPeriod}.pdf");
    }

    [HttpPost("calculate-run")]
    [Authorize(Policy = Permissions.LuongEdit)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CalculatePayrollRun([FromQuery] string period, [FromQuery] int? unitId, CancellationToken cancellationToken)
    {
        var result = await _payrollService.CalculatePayrollRunAsync(period, unitId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("run")]
    [Authorize(Policy = Permissions.LuongView)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPayrollRun([FromQuery] string period, [FromQuery] int? unitId, CancellationToken cancellationToken)
    {
        var result = await _payrollService.GetPayrollRunAsync(period, unitId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("run/{id}/submit")]
    [Authorize(Policy = Permissions.LuongEdit)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SubmitPayrollRun(int id, CancellationToken cancellationToken)
    {
        int accountantId = _dataScope.CurrentEmployeeId ?? 1;
        await _payrollService.SubmitPayrollRunAsync(id, accountantId, cancellationToken);
        return Ok(new { succeeded = true, message = "Nộp duyệt bảng lương thành công." });
    }

    [HttpPost("run/{id}/approve")]
    [Authorize(Policy = Permissions.LuongApprove)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ApprovePayrollRun(int id, [FromQuery] int chiefAccountantId, [FromQuery] int hrDirectorId, CancellationToken cancellationToken)
    {
        await _payrollService.ApprovePayrollRunAsync(id, chiefAccountantId, hrDirectorId, cancellationToken);
        return Ok(new { succeeded = true, message = "Kế toán trưởng & Giám đốc Nhân sự đã phê duyệt bảng lương." });
    }

    [HttpPost("run/{id}/lock")]
    [Authorize(Policy = Permissions.LuongApprove)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> LockPayrollRun(int id, CancellationToken cancellationToken)
    {
        int hrId = _dataScope.CurrentEmployeeId ?? 1;
        await _payrollService.LockPayrollRunAsync(id, hrId, cancellationToken);
        return Ok(new { succeeded = true, message = "Khóa sổ kỳ lương thành công (Bất biến)." });
    }

    [HttpPost("run/{id}/pay")]
    [Authorize(Policy = Permissions.LuongApprove)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkPayrollRunPaid(int id, [FromBody] MarkPaidRequest request, CancellationToken cancellationToken)
    {
        int financeId = _dataScope.CurrentEmployeeId ?? 1;
        await _payrollService.MarkPayrollRunAsPaidAsync(id, financeId, request.PaymentReference, cancellationToken);
        return Ok(new { succeeded = true, message = "Đã cập nhật trạng thái chi trả ngân hàng thành công." });
    }

    [HttpGet("anomalies")]
    [Authorize(Policy = Permissions.LuongView)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPayrollAnomalies([FromQuery] string period, CancellationToken cancellationToken)
    {
        var list = await _payrollService.CheckPayrollAnomaliesAsync(period, cancellationToken);
        return Ok(list);
    }

    [HttpGet("runs/{id}/bank-file")]
    [Authorize(Policy = Permissions.LuongView)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportBankPaymentCsv(int id, [FromQuery] string format = "VCB", CancellationToken cancellationToken = default)
    {
        var csvBytes = await _exportService.ExportBankPaymentCsvAsync(id, format, cancellationToken);
        return File(csvBytes, "text/csv", $"BankPayment_{format}_{id}.csv");
    }

    [HttpGet("runs/{id}/cost-center-entries")]
    [Authorize(Policy = Permissions.LuongView)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCostCenterJournalEntries(int id, CancellationToken cancellationToken)
    {
        var entries = await _exportService.GenerateCostCenterJournalEntriesAsync(id, cancellationToken);
        return Ok(entries);
    }

    [HttpGet("salary-fund")]
    [Authorize(Policy = Permissions.LuongView)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnitSalaryFundReport([FromQuery] string period, CancellationToken cancellationToken)
    {
        var report = await _exportService.GenerateUnitSalaryFundReportAsync(period, cancellationToken);
        return Ok(report);
    }

    [HttpGet("d02-lt")]
    [Authorize(Policy = Permissions.LuongView)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSocialInsuranceD02Lt([FromQuery] string period, CancellationToken cancellationToken)
    {
        var report = await _exportService.GenerateSocialInsuranceD02LtAsync(period, cancellationToken);
        return Ok(report);
    }

    [HttpGet("05-qtt")]
    [Authorize(Policy = Permissions.LuongView)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAnnualPitFinalization([FromQuery] int year, CancellationToken cancellationToken)
    {
        var report = await _exportService.GenerateAnnualPitFinalizationAsync(year, cancellationToken);
        return Ok(report);
    }
}

public record MarkPaidRequest(string PaymentReference);



