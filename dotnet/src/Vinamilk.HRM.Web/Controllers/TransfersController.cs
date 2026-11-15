using Microsoft.AspNetCore.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Application.CoreHR.ContractsAndTransfers;

namespace Vinamilk.HRM.Web.Controllers;

[Route("Transfers")]
[Authorize(Policy = "Transfers.View")]
public class TransfersController : Controller
{
    private readonly ISender _sender;
    private readonly IApplicationDbContext _context;

    public TransfersController(ISender sender, IApplicationDbContext context)
    {
        _sender = sender;
        _context = context;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(int? empId = null, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Điều chuyển, Bổ nhiệm, Thử việc, Lộ trình Nghỉ hưu (BLLĐ 2019) & Offboarding";
        ViewData["BreadcrumbParent"] = "Hồ sơ & Tổ chức (Core HR)";

        var dto = await _sender.Send(new GetTransfersAndLifecycleQuery(), cancellationToken);
        ViewBag.PreselectedEmpId = empId;
        return View(dto);
    }

    [HttpPost("CreateTransfer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTransfer(
        string decisionNumber,
        int employeeId,
        string transferType,
        int toUnitId,
        int toPositionId,
        DateOnly effectiveDate,
        decimal? proposedNewSalary,
        string? reason,
        bool approveImmediately = false,
        CancellationToken cancellationToken = default)
    {
        await _sender.Send(new CreatePersonnelTransferCommand(
            decisionNumber,
            employeeId,
            transferType,
            toUnitId,
            toPositionId,
            effectiveDate,
            proposedNewSalary,
            reason,
            approveImmediately), cancellationToken);

        TempData["CoreHrSuccess"] = approveImmediately
            ? $"Đã ban hành & phê duyệt Quyết định [{decisionNumber}], tự động cập nhật Đơn vị, Chức danh, Mức lương và Quá trình công tác!"
            : $"Đã tạo Tờ trình Quyết định [{decisionNumber}] ở trạng thái Chờ phê duyệt (PendingApproval)!";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("ApproveTransfer")]
    [Authorize(Policy = "Transfers.Approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveTransfer(
        int transferId,
        bool isApproved,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new ApprovePersonnelTransferCommand(transferId, isApproved), cancellationToken);
        TempData["CoreHrSuccess"] = isApproved
            ? $"Đã phê duyệt Quyết định #{transferId} và đồng bộ hồ sơ nhân sự!"
            : $"Đã từ chối tờ trình điều chuyển #{transferId}.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("EvaluateProbation")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EvaluateProbation(
        int employeeId,
        bool passedProbation,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new EvaluateProbationCommand(employeeId, passedProbation), cancellationToken);
        TempData["CoreHrSuccess"] = passedProbation
            ? "Đã đánh giá ĐẠT THỬ VIỆC: Chuyển nhân sự sang Chính thức (100% lương) và ký HĐLĐ 12 tháng!"
            : "Đã ghi nhận kết quả Không đạt thử việc.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("ProcessOffboarding")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessOffboarding(
        int employeeId,
        string offboardingType,
        DateOnly terminationDate,
        string? reason,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new ProcessOffboardingCommand(employeeId, offboardingType, terminationDate, reason), cancellationToken);
        TempData["CoreHrSuccess"] = "Đã hoàn tất thủ tục Nghỉ việc / Nghỉ hưu (Offboarding), thanh lý hợp đồng và thu hồi quyền truy cập tài khoản!";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("PrintDecision/{id:int}")]
    public async Task<IActionResult> PrintDecision(int id, CancellationToken cancellationToken)
    {
        var transfer = await _context.PersonnelTransfers
            .AsNoTracking()
            .Include(t => t.Employee)
            .Include(t => t.FromUnit)
            .Include(t => t.ToUnit)
            .Include(t => t.FromPosition)
            .Include(t => t.ToPosition)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (transfer is null)
            return RedirectToAction(nameof(Index));

        ViewData["Title"] = $"Quyết định số {transfer.DecisionNumber}";
        return View(transfer);
    }
}

