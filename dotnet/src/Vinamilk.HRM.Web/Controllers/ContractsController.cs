using Microsoft.AspNetCore.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Application.CoreHR.ContractsAndTransfers;
using Vinamilk.HRM.Infrastructure.Services;

namespace Vinamilk.HRM.Web.Controllers;

[Route("Contracts")]
[Authorize]
public class ContractsController : Controller
{
    private readonly ISender _sender;
    private readonly IApplicationDbContext _context;
    private readonly ContractExpirationAlertJob _alertJob;

    public ContractsController(
        ISender sender,
        IApplicationDbContext context,
        ContractExpirationAlertJob alertJob)
    {
        _sender = sender;
        _context = context;
        _alertJob = alertJob;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(
        string? contractType = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Quản lý Hợp đồng Lao động, Phụ lục, Gia hạn & Cảnh báo Hết hạn 30 ngày";
        ViewData["BreadcrumbParent"] = "Hồ sơ & Tổ chức (Core HR)";

        var dto = await _sender.Send(new GetContractsOverviewQuery(contractType, status), cancellationToken);
        return View(dto);
    }

    [HttpPost("CreateAddendum")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAddendum(
        int laborContractId,
        string addendumNumber,
        string changeType,
        DateOnly effectiveDate,
        DateOnly? extendedEndDate,
        decimal? newBaseSalary,
        decimal? newAllowance,
        string? contentSummary,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new CreateContractAddendumCommand(
            laborContractId,
            addendumNumber,
            changeType,
            effectiveDate,
            extendedEndDate,
            newBaseSalary,
            newAllowance,
            contentSummary), cancellationToken);

        TempData["CoreHrSuccess"] = $"Đã ký Phụ lục Hợp đồng [{addendumNumber}], tự động gia hạn / đồng bộ mức lương mới vào Hồ sơ Nhân viên!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("RunExpiryAlertJob")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunExpiryAlertJob(CancellationToken cancellationToken)
    {
        await _alertJob.ExecuteDailyScanAsync(cancellationToken);
        TempData["CoreHrSuccess"] = "Đã kích hoạt Hangfire Job (ContractExpirationAlertJob): Quét toàn bộ hợp đồng hết hạn trong <= 30 ngày và gửi cảnh báo vào Trung tâm Thông báo!";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Print/{id:int}")]
    public async Task<IActionResult> Print(int id, CancellationToken cancellationToken)
    {
        var contract = await _context.LaborContracts
            .AsNoTracking()
            .Include(c => c.Employee)
                .ThenInclude(e => e.OrganizationUnit)
            .Include(c => c.Employee)
                .ThenInclude(e => e.Position)
            .Include(c => c.Addendums)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (contract is null)
            return RedirectToAction(nameof(Index));

        ViewData["Title"] = $"Hợp đồng Lao động số {contract.ContractNumber}";
        return View(contract);
    }
}

