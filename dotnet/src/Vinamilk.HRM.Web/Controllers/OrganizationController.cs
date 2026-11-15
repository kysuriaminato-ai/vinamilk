using Microsoft.AspNetCore.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vinamilk.HRM.Application.CoreHR.Organization;

namespace Vinamilk.HRM.Web.Controllers;

[Route("Organization")]
[Authorize(Policy = "Departments.View")]
public class OrganizationController : Controller
{
    private readonly ISender _sender;

    public OrganizationController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Cơ cấu Tổ chức Đa cấp, Sơ đồ Tổ chức, Chức danh & Ngạch bậc lương";
        ViewData["BreadcrumbParent"] = "Hồ sơ & Tổ chức (Core HR)";

        var dto = await _sender.Send(new GetOrganizationOverviewQuery(), cancellationToken);
        return View(dto);
    }

    [HttpPost("SaveUnit")]
    [Authorize(Policy = "Departments.Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveUnit(
        int? id,
        string unitCode,
        string unitName,
        string unitType,
        int? parentUnitId,
        int? managerEmployeeId,
        string? costCenterCode,
        int plannedHeadcount,
        int sortOrder,
        string? address,
        int wageRegion = 1,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _sender.Send(new SaveOrganizationUnitCommand(
                id,
                unitCode,
                unitName,
                unitType,
                parentUnitId,
                managerEmployeeId,
                costCenterCode,
                plannedHeadcount,
                sortOrder,
                address,
                wageRegion), cancellationToken);

            TempData["CoreHrSuccess"] = $"Đã lưu đơn vị tổ chức [{unitCode.ToUpperInvariant()} - {unitName}] và cập nhật cây phân cấp TreePath!";
        }
        catch (Exception ex)
        {
            TempData["CoreHrError"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("DeleteUnit")]
    [Authorize(Policy = "Departments.Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUnit(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var msg = await _sender.Send(new DeleteOrganizationUnitCommand(id), cancellationToken);
            TempData["CoreHrSuccess"] = msg;
        }
        catch (Exception ex)
        {
            TempData["CoreHrError"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("ReorderUnits")]
    [Authorize(Policy = "Departments.Edit")]
    public async Task<IActionResult> ReorderUnits([FromBody] List<int> orderedUnitIds, CancellationToken cancellationToken)
    {
        await _sender.Send(new ReorderOrganizationUnitsCommand(orderedUnitIds), cancellationToken);
        return Ok(new { success = true, message = "Đã lưu thứ tự sắp xếp cây đơn vị." });
    }

    [HttpPost("SavePosition")]
    [Authorize(Policy = "Departments.Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePosition(
        int? id,
        string positionCode,
        string positionName,
        string positionGroup,
        int positionLevel,
        decimal defaultPositionAllowance,
        string? jobDescription,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new SavePositionCommand(
            id,
            positionCode,
            positionName,
            positionGroup,
            positionLevel,
            defaultPositionAllowance,
            jobDescription), cancellationToken);

        TempData["CoreHrSuccess"] = $"Đã lưu chức danh [{positionCode.ToUpperInvariant()} - {positionName}]!";
        return RedirectToAction(nameof(Index));
    }
}

