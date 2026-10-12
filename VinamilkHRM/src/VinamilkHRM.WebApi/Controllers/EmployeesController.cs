using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Application.Common.Security;
using VinamilkHRM.Application.Interfaces;

namespace VinamilkHRM.WebApi.Controllers;

[ApiController]
[Route("api/v1/employees")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly IDataScope _dataScope;
    private readonly ICurrentUser _currentUser;

    public EmployeesController(
        IEmployeeService employeeService,
        IDataScope dataScope,
        ICurrentUser currentUser)
    {
        _employeeService = employeeService;
        _dataScope = dataScope;
        _currentUser = currentUser;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.NhansuView)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEmployees(CancellationToken cancellationToken)
    {
        if (_dataScope.IsGlobalAccess)
        {
            var employees = await _employeeService.GetAllEmployeesAsync(cancellationToken);
            return Ok(employees);
        }

        // Vai trò Employee (ESS) chỉ xem được chính mình
        if (_currentUser.IsInRole(Roles.Employee) &&
            !_currentUser.IsInRole(Roles.UnitManager) &&
            !_currentUser.IsInRole(Roles.TeamLeader) &&
            !_currentUser.IsInRole(Roles.HRManager) &&
            !_currentUser.IsInRole(Roles.HRSpecialist))
        {
            if (!_dataScope.CurrentEmployeeId.HasValue)
            {
                return Ok(Array.Empty<object>());
            }

            var self = await _employeeService.GetEmployeeByIdAsync(_dataScope.CurrentEmployeeId.Value, cancellationToken);
            return Ok(self != null ? new[] { self } : Array.Empty<object>());
        }

        // Cấp quản lý: Lọc theo danh sách đơn vị được phép trong IDataScope
        var authorizedUnitIds = await _dataScope.GetAuthorizedUnitIdsAsync(cancellationToken);
        var allEmployees = await _employeeService.GetAllEmployeesAsync(cancellationToken);
        var scopedEmployees = allEmployees.Where(e => authorizedUnitIds.Contains(e.OrganizationalUnitId)).ToList();

        return Ok(scopedEmployees);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = Permissions.NhansuView)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEmployeeById(int id, CancellationToken cancellationToken)
    {
        bool canAccess = await _dataScope.CanAccessEmployeeAsync(id, cancellationToken);
        if (!canAccess)
        {
            return Forbid();
        }

        var employee = await _employeeService.GetEmployeeByIdAsync(id, cancellationToken);
        if (employee == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy nhân sự",
                Detail = $"Nhân sự với mã định danh ID {id} không tồn tại trong hệ thống."
            });
        }

        return Ok(employee);
    }
}
