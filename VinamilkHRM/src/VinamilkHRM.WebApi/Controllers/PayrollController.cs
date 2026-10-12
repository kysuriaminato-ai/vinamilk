using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Application.Common.Security;
using VinamilkHRM.Application.Interfaces;

namespace VinamilkHRM.WebApi.Controllers;

[ApiController]
[Route("api/v1/payroll")]
[Authorize]
public class PayrollController : ControllerBase
{
    private readonly IPayrollService _payrollService;
    private readonly IApplicationDbContext _dbContext;
    private readonly IDataScope _dataScope;
    private readonly ICurrentUser _currentUser;

    public PayrollController(
        IPayrollService payrollService,
        IApplicationDbContext dbContext,
        IDataScope dataScope,
        ICurrentUser currentUser)
    {
        _payrollService = payrollService;
        _dbContext = dbContext;
        _dataScope = dataScope;
        _currentUser = currentUser;
    }

    [HttpGet("payslips")]
    [Authorize(Policy = Permissions.LuongView)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaySlips([FromQuery] string period, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(period))
        {
            period = DateTime.UtcNow.ToString("yyyy-MM");
        }

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

            var selfPayslip = await _payrollService.GetPaySlipAsync(_dataScope.CurrentEmployeeId.Value, period, cancellationToken);
            return Ok(selfPayslip != null ? new[] { selfPayslip } : Array.Empty<object>());
        }

        var allPayslips = await _payrollService.GetPaySlipsByPeriodAsync(period, cancellationToken);

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

        var filtered = allPayslips.Where(p => scopedEmployeeIds.Contains(p.EmployeeId)).ToList();
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

        // Kiểm tra bảo mật: Nhân viên không được gọi lương của người khác
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
            // Kiểm tra phân quyền phạm vi đơn vị
            bool canAccess = await _dataScope.CanAccessEmployeeAsync(payslip.EmployeeId, cancellationToken);
            if (!canAccess)
            {
                return Forbid();
            }
        }

        return Ok(payslip);
    }

    [HttpPost("approve/{id}")]
    [Authorize(Policy = Permissions.LuongApprove)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ApprovePaySlip(int id, CancellationToken cancellationToken)
    {
        await _payrollService.ApprovePaySlipAsync(id, cancellationToken);
        return Ok(new { succeeded = true, message = $"Phê duyệt phiếu lương ID {id} thành công." });
    }
}
