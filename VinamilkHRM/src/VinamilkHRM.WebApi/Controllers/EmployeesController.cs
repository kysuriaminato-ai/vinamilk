using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Application.Common.Security;
using VinamilkHRM.Application.DTOs.Employees;
using VinamilkHRM.Application.Interfaces;
using VinamilkHRM.Application.Privacy;

namespace VinamilkHRM.WebApi.Controllers;

[ApiController]
[Route("api/v1/employees")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly IEmployeeExcelService _excelService;
    private readonly IDataScope _dataScope;
    private readonly ICurrentUser _currentUser;
    private readonly IPersonalDataProtectionService? _privacyService;

    public EmployeesController(
        IEmployeeService employeeService,
        IEmployeeExcelService excelService,
        IDataScope dataScope,
        ICurrentUser currentUser,
        IPersonalDataProtectionService? privacyService = null)
    {
        _employeeService = employeeService ?? throw new ArgumentNullException(nameof(employeeService));
        _excelService = excelService ?? throw new ArgumentNullException(nameof(excelService));
        _dataScope = dataScope ?? throw new ArgumentNullException(nameof(dataScope));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _privacyService = privacyService;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.NhansuView)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEmployees(
        [FromQuery] string? query,
        [FromQuery] int? unitId,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<EmployeeDto> employees;

        if (_dataScope.IsGlobalAccess)
        {
            employees = await _employeeService.GetAllEmployeesAsync(cancellationToken);
        }
        else if (_currentUser.IsInRole(Roles.Employee) &&
            !_currentUser.IsInRole(Roles.UnitManager) &&
            !_currentUser.IsInRole(Roles.TeamLeader) &&
            !_currentUser.IsInRole(Roles.HRManager) &&
            !_currentUser.IsInRole(Roles.HRSpecialist))
        {
            if (!_dataScope.CurrentEmployeeId.HasValue)
            {
                return Ok(new { Total = 0, PageIndex = pageIndex, PageSize = pageSize, Items = Array.Empty<object>() });
            }

            var self = await _employeeService.GetEmployeeByIdAsync(_dataScope.CurrentEmployeeId.Value, cancellationToken);
            var selfList = self != null ? new[] { self } : Array.Empty<EmployeeDto>();
            return Ok(new { Total = selfList.Length, PageIndex = 1, PageSize = pageSize, Items = selfList });
        }
        else
        {
            var authorizedUnitIds = await _dataScope.GetAuthorizedUnitIdsAsync(cancellationToken);
            var allEmployees = await _employeeService.GetAllEmployeesAsync(cancellationToken);
            employees = allEmployees.Where(e => authorizedUnitIds.Contains(e.OrganizationalUnitId)).ToList();
        }

        // Lọc theo đơn vị
        if (unitId.HasValue)
        {
            employees = employees.Where(e => e.OrganizationalUnitId == unitId.Value).ToList();
        }

        // Lọc theo từ khóa tìm kiếm
        if (!string.IsNullOrWhiteSpace(query))
        {
            string q = query.Trim().ToLower();
            employees = employees.Where(e => 
                e.FullName.ToLower().Contains(q) || 
                e.EmployeeCode.ToLower().Contains(q) ||
                e.Email.ToLower().Contains(q)
            ).ToList();
        }

        int total = employees.Count;
        var pagedItems = employees
            .Skip((Math.Max(1, pageIndex) - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Ok(new
        {
            Total = total,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            Items = pagedItems
        });
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

    [HttpGet("{id}/details")]
    [Authorize(Policy = Permissions.NhansuView)]
    [ProducesResponseType(typeof(EmployeeDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEmployeeDetails(int id, CancellationToken cancellationToken)
    {
        bool canAccess = await _dataScope.CanAccessEmployeeAsync(id, cancellationToken);
        if (!canAccess)
        {
            return Forbid();
        }

        bool canViewSensitive = User.HasClaim(c => c.Value == Permissions.NhansuViewSensitive) ||
                                User.IsInRole(Roles.SystemAdmin) ||
                                User.IsInRole(Roles.HRDirector) ||
                                User.IsInRole(Roles.HRManager) ||
                                User.IsInRole(Roles.PayrollAccountant);

        var details = await _employeeService.GetEmployeeDetailByIdAsync(id, canViewSensitive, cancellationToken);
        if (details == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy hồ sơ nhân sự",
                Detail = $"Hồ sơ nhân sự với ID {id} không tồn tại."
            });
        }

        if (_privacyService != null && _dataScope.CurrentEmployeeId != id)
        {
            int viewerId = _dataScope.CurrentEmployeeId ?? (int.TryParse(_currentUser.UserId, out var uid) ? uid : 1);
            if (viewerId <= 0) viewerId = 1;
            await _privacyService.LogSensitiveDataAccessAsync(
                viewerId,
                id,
                "CitizenId",
                "Xem chi tiết CCCD trong hồ sơ nhân sự (Nghị định 13/2023)",
                HttpContext?.Connection?.RemoteIpAddress?.ToString(),
                cancellationToken);
        }

        return Ok(details);
    }

    [HttpPost("{id}/dependents")]
    [Authorize(Policy = Permissions.NhansuManage)]
    [ProducesResponseType(typeof(DependentDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddDependent(int id, [FromBody] CreateDependentRequest request, CancellationToken cancellationToken)
    {
        bool canAccess = await _dataScope.CanAccessEmployeeAsync(id, cancellationToken);
        if (!canAccess) return Forbid();

        var dep = await _employeeService.AddDependentAsync(id, request, cancellationToken);
        return CreatedAtAction(nameof(GetEmployeeDetails), new { id }, dep);
    }

    [HttpPost("{id}/work-histories")]
    [Authorize(Policy = Permissions.NhansuManage)]
    [ProducesResponseType(typeof(EmployeeWorkHistoryDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddWorkHistory(int id, [FromBody] CreateWorkHistoryRequest request, CancellationToken cancellationToken)
    {
        bool canAccess = await _dataScope.CanAccessEmployeeAsync(id, cancellationToken);
        if (!canAccess) return Forbid();

        var wh = await _employeeService.AddWorkHistoryAsync(id, request, cancellationToken);
        return CreatedAtAction(nameof(GetEmployeeDetails), new { id }, wh);
    }

    [HttpPost("{id}/educations")]
    [Authorize(Policy = Permissions.NhansuManage)]
    [ProducesResponseType(typeof(EmployeeEducationDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddEducation(int id, [FromBody] CreateEducationRequest request, CancellationToken cancellationToken)
    {
        bool canAccess = await _dataScope.CanAccessEmployeeAsync(id, cancellationToken);
        if (!canAccess) return Forbid();

        var edu = await _employeeService.AddEducationAsync(id, request, cancellationToken);
        return CreatedAtAction(nameof(GetEmployeeDetails), new { id }, edu);
    }

    [HttpPost("{id}/certificates")]
    [Authorize(Policy = Permissions.NhansuManage)]
    [ProducesResponseType(typeof(EmployeeCertificateDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddCertificate(int id, [FromBody] CreateCertificateRequest request, CancellationToken cancellationToken)
    {
        bool canAccess = await _dataScope.CanAccessEmployeeAsync(id, cancellationToken);
        if (!canAccess) return Forbid();

        var cert = await _employeeService.AddCertificateAsync(id, request, cancellationToken);
        return CreatedAtAction(nameof(GetEmployeeDetails), new { id }, cert);
    }

    [HttpPost("{id}/attachments")]
    [Authorize(Policy = Permissions.NhansuManage)]
    [ProducesResponseType(typeof(EmployeeAttachmentDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> UploadAttachment(int id, [FromForm] IFormFile file, [FromForm] string documentType, [FromForm] string? description, CancellationToken cancellationToken)
    {
        bool canAccess = await _dataScope.CanAccessEmployeeAsync(id, cancellationToken);
        if (!canAccess) return Forbid();

        if (file == null || file.Length == 0)
            return BadRequest(new ProblemDetails { Title = "Tệp không hợp lệ", Detail = "Vui lòng chọn một tệp đính kèm." });

        // Tạo thư mục lưu file ngoài DB: Storage/Attachments/{id}
        string storageDir = Path.Combine(Directory.GetCurrentDirectory(), "Storage", "Attachments", id.ToString());
        Directory.CreateDirectory(storageDir);

        string safeFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        string filePath = Path.Combine(storageDir, safeFileName);

        string sha256Hash;
        using (var stream = file.OpenReadStream())
        {
            using var sha256 = SHA256.Create();
            byte[] hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
            sha256Hash = Convert.ToHexString(hashBytes).ToLowerInvariant();

            stream.Position = 0;
            using var fileStream = new FileStream(filePath, FileMode.Create);
            await stream.CopyToAsync(fileStream, cancellationToken);
        }

        var att = await _employeeService.AddAttachmentAsync(
            employeeId: id,
            documentType: documentType ?? "HOSONHANSU",
            fileName: file.FileName,
            filePath: filePath,
            fileSizeBytes: file.Length,
            checksumSha256: sha256Hash,
            cancellationToken: cancellationToken
        );

        return CreatedAtAction(nameof(GetEmployeeDetails), new { id }, att);
    }

    [HttpGet("export-excel")]
    [Authorize(Policy = Permissions.NhansuView)]
    public async Task<IActionResult> ExportExcel(CancellationToken cancellationToken)
    {
        bool canViewSensitive = User.HasClaim(c => c.Value == Permissions.NhansuViewSensitive) ||
                                User.IsInRole(Roles.SystemAdmin) ||
                                User.IsInRole(Roles.HRDirector) ||
                                User.IsInRole(Roles.HRManager);

        var details = await _employeeService.GetAllEmployeeDetailsAsync(canViewSensitive, cancellationToken);
        var bytes = await _excelService.ExportToExcelAsync(details, cancellationToken);

        string fileName = $"Vinamilk_Employees_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpPost("import-excel/preview")]
    [Authorize(Policy = Permissions.NhansuManage)]
    [ProducesResponseType(typeof(ExcelImportPreviewResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> PreviewImportExcel(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new ProblemDetails { Title = "Tệp trống", Detail = "Vui lòng chọn một tệp Excel để xem trước." });

        using var stream = file.OpenReadStream();
        var preview = await _excelService.PreviewImportAsync(stream, cancellationToken);
        return Ok(preview);
    }

    [HttpPost("import-excel/commit")]
    [Authorize(Policy = Permissions.NhansuManage)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CommitImportExcel([FromBody] ExcelImportCommitRequest request, CancellationToken cancellationToken)
    {
        if (request.Rows == null || request.Rows.Count == 0)
            return BadRequest(new ProblemDetails { Title = "Dữ liệu trống", Detail = "Không có dòng nào để ghi vào hệ thống." });

        int inserted = await _excelService.CommitImportAsync(request, cancellationToken);
        return Ok(new { InsertedCount = inserted, Message = $"Đã nhập thành công {inserted} hồ sơ nhân sự vào hệ thống." });
    }
}
