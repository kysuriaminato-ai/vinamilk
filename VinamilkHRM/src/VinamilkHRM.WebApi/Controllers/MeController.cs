using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VinamilkHRM.Application.Common.Interfaces;

namespace VinamilkHRM.WebApi.Controllers;

[ApiController]
[Route("api/v1/me")]
[Authorize]
public class MeController : ControllerBase
{
    private readonly ICurrentUser _currentUser;

    public MeController(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetCurrentUser()
    {
        return Ok(new
        {
            userId = _currentUser.UserId,
            userName = _currentUser.UserName,
            email = _currentUser.Email,
            employeeId = _currentUser.EmployeeId,
            roles = _currentUser.Roles,
            permissions = _currentUser.Permissions
        });
    }
}
