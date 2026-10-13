using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VinamilkHRM.Application.Common.Security;
using VinamilkHRM.Application.DTOs.OrganizationalUnits;
using VinamilkHRM.Application.Interfaces;

namespace VinamilkHRM.WebApi.Controllers;

[ApiController]
[Route("api/v1/organizational-units")]
[Authorize]
public class OrganizationalUnitsController : ControllerBase
{
    private readonly IOrganizationalUnitService _unitService;

    public OrganizationalUnitsController(IOrganizationalUnitService unitService)
    {
        _unitService = unitService ?? throw new ArgumentNullException(nameof(unitService));
    }

    [HttpGet("tree")]
    [Authorize(Policy = Permissions.NhansuView)]
    [ProducesResponseType(typeof(List<OrganizationalUnitTreeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTree(CancellationToken cancellationToken)
    {
        var tree = await _unitService.GetTreeAsync(cancellationToken);
        return Ok(tree);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = Permissions.NhansuView)]
    [ProducesResponseType(typeof(OrganizationalUnitDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var unit = await _unitService.GetByIdAsync(id, cancellationToken);
        if (unit == null)
            return NotFound(new ProblemDetails { Title = "Không tìm thấy đơn vị", Detail = $"Đơn vị {id} không tồn tại." });

        return Ok(unit);
    }

    [HttpPost("{id}/move")]
    [Authorize(Policy = Permissions.NhansuManage)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MoveUnit(int id, [FromBody] MoveUnitRequest request, CancellationToken cancellationToken)
    {
        await _unitService.MoveUnitAsync(id, request.NewParentUnitId, cancellationToken);
        return Ok(new { Message = "Di chuyển đơn vị và cập nhật cây phân cấp HierarchyId thành công." });
    }

    [HttpPost("{id}/assign-manager")]
    [Authorize(Policy = Permissions.NhansuManage)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> AssignManager(int id, [FromBody] AssignManagerRequest request, CancellationToken cancellationToken)
    {
        await _unitService.AssignManagerAsync(id, request, cancellationToken);
        return Ok(new { Message = "Bổ nhiệm người đứng đầu đơn vị thành công." });
    }

    [HttpPost("{id}/minimum-wage-region")]
    [Authorize(Policy = Permissions.NhansuManage)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangeWageRegion(int id, [FromBody] ChangeWageRegionRequest request, CancellationToken cancellationToken)
    {
        await _unitService.ChangeWageRegionAsync(id, request, cancellationToken);
        return Ok(new { Message = "Cập nhật vùng lương tối thiểu theo địa giới hành chính thành công." });
    }

    [HttpGet("{id}/managers")]
    [Authorize(Policy = Permissions.NhansuView)]
    [ProducesResponseType(typeof(IReadOnlyList<UnitManagerHistoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetManagerHistories(int id, CancellationToken cancellationToken)
    {
        var histories = await _unitService.GetManagerHistoriesAsync(id, cancellationToken);
        return Ok(histories);
    }
}
