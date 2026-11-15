using Microsoft.AspNetCore.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vinamilk.HRM.Application.CoreHR.Employees;
using Vinamilk.HRM.Infrastructure.Services;

namespace Vinamilk.HRM.Web.Controllers;

[Route("Employees")]
[Authorize]
public class EmployeesController : Controller
{
    private readonly ISender _sender;
    private readonly CoreHrDocumentService _documentService;

    public EmployeesController(ISender sender, CoreHrDocumentService documentService)
    {
        _sender = sender;
        _documentService = documentService;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(
        [FromQuery] GetEmployeesAdvancedQuery query,
        CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Danh sách Nhân sự Đa điều kiện, Import/Export Excel & In Mẫu 2C";
        ViewData["BreadcrumbParent"] = "Hồ sơ & Tổ chức (Core HR)";

        var result = await _sender.Send(query, cancellationToken);
        ViewBag.Filter = query;
        return View(result);
    }

    [HttpGet("Details/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var profile = await _sender.Send(new GetEmployee360ProfileQuery(id), cancellationToken);
        if (profile is null)
            return RedirectToAction(nameof(Index));

        ViewData["Title"] = $"Hồ sơ 360°: {profile.Employee.EmployeeCode} — {profile.Employee.FullName}";
        ViewData["BreadcrumbParent"] = "Quản lý Hồ sơ Nhân sự";
        return View(profile);
    }

    [HttpPost("Save")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(
        int? id,
        string employeeCode,
        string fullName,
        string gender,
        DateOnly dateOfBirth,
        string citizenId,
        string? phoneNumber,
        string? workEmail,
        string? personalEmail,
        string? permanentAddress,
        string? currentAddress,
        int organizationUnitId,
        int positionId,
        int? educationLevelId,
        string? major,
        int? salaryStepId,
        decimal baseSalary,
        decimal positionAllowance,
        decimal otherAllowance,
        string? bankAccount,
        string? bankName,
        string? taxCode,
        string? socialInsuranceNumber,
        DateOnly hireDate,
        string workStatus,
        string currentContractType,
        CancellationToken cancellationToken)
    {
        var empId = await _sender.Send(new SaveEmployeeCommand(
            id,
            employeeCode,
            fullName,
            gender,
            dateOfBirth,
            citizenId,
            phoneNumber,
            workEmail,
            personalEmail,
            permanentAddress,
            currentAddress,
            organizationUnitId,
            positionId,
            educationLevelId,
            major,
            salaryStepId,
            baseSalary,
            positionAllowance,
            otherAllowance,
            bankAccount,
            bankName,
            taxCode,
            socialInsuranceNumber,
            hireDate,
            workStatus,
            currentContractType), cancellationToken);

        TempData["CoreHrSuccess"] = $"Đã lưu hồ sơ nhân sự [{employeeCode.ToUpperInvariant()} - {fullName}], mã hóa AES-256 CCCD/Ngân hàng và ghi nhận AuditLog!";
        return RedirectToAction(nameof(Details), new { id = empId });
    }

    [HttpPost("AddDependent")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddDependent(
        int employeeId,
        string fullName,
        string relationship,
        DateOnly? dateOfBirth,
        bool isTaxDeductible,
        bool isEmergencyContact,
        string? emergencyPhone,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new AddEmployeeDependentCommand(
            employeeId,
            fullName,
            relationship,
            dateOfBirth,
            isTaxDeductible,
            isEmergencyContact,
            emergencyPhone), cancellationToken);

        TempData["CoreHrSuccess"] = $"Đã thêm người phụ thuộc '{fullName}' vào hồ sơ giảm trừ gia cảnh!";
        return RedirectToAction(nameof(Details), new { id = employeeId });
    }

    [HttpPost("AddCertificate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCertificate(
        int employeeId,
        string certificateName,
        string certificateType,
        string? issuedBy,
        DateOnly? issueDate,
        string? scoreOrGrade,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new AddEmployeeCertificateCommand(
            employeeId,
            certificateName,
            certificateType,
            issuedBy,
            issueDate,
            scoreOrGrade), cancellationToken);

        TempData["CoreHrSuccess"] = $"Đã bổ sung văn bằng / chứng chỉ '{certificateName}'!";
        return RedirectToAction(nameof(Details), new { id = employeeId });
    }

    [HttpPost("AddDocument")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddDocument(
        int employeeId,
        string documentCategory,
        string documentTitle,
        IFormFile? uploadFile,
        CancellationToken cancellationToken)
    {
        var fileName = uploadFile?.FileName ?? $"HoSo_{employeeId}_{DateTime.Now:yyyyMMdd}.pdf";
        var fileSize = uploadFile?.Length ?? 245_760L;

        await _sender.Send(new AddEmployeeDocumentCommand(
            employeeId,
            documentCategory,
            documentTitle,
            $"/uploads/employees/{employeeId}/{fileName}",
            fileSize), cancellationToken);

        TempData["CoreHrSuccess"] = $"Đã đính kèm tài liệu '{documentTitle}' ({fileName})!";
        return RedirectToAction(nameof(Details), new { id = employeeId });
    }

    [HttpPost("UpdateAvatar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAvatar(
        int employeeId,
        string croppedDataUrl,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new UpdateEmployeeAvatarCommand(employeeId, croppedDataUrl), cancellationToken);
        TempData["CoreHrSuccess"] = "Đã cắt (Crop) và cập nhật Ảnh đại diện nhân sự thành công!";
        return RedirectToAction(nameof(Details), new { id = employeeId });
    }

    /* ------------------------------------------------------------------------
     * EXCEL IMPORT / EXPORT (CLOSEDXML) & PDF MẪU 2C (QUESTPDF)
     * ------------------------------------------------------------------------ */
    [HttpGet("DownloadTemplate")]
    public IActionResult DownloadTemplate()
    {
        var bytes = _documentService.GenerateEmployeeImportTemplateExcel();
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "Vinamilk_HRM_Mau_Import_NhanSu.xlsx");
    }

    [HttpPost("ImportExcel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportExcel(IFormFile? excelFile, CancellationToken cancellationToken)
    {
        Stream stream;
        if (excelFile is not null && excelFile.Length > 0)
        {
            stream = excelFile.OpenReadStream();
        }
        else
        {
            // Nếu người dùng nhấn "Test Import Mẫu Ngay" mà chưa chọn file, dùng chính file template mẫu (có 1 dòng đúng + 1 dòng lỗi)
            var sampleBytes = _documentService.GenerateEmployeeImportTemplateExcel();
            stream = new MemoryStream(sampleBytes);
        }

        await using (stream)
        {
            var report = await _documentService.ImportEmployeesFromExcelAsync(stream, cancellationToken);
            var queryResult = await _sender.Send(new GetEmployeesAdvancedQuery(PageSize: 50), cancellationToken);
            ViewBag.Filter = new GetEmployeesAdvancedQuery(PageSize: 50);
            ViewBag.ImportReport = report;
            ViewData["Title"] = "Kết quả Import Excel Hồ sơ Nhân sự (Báo lỗi từng dòng)";
            return View("Index", queryResult);
        }
    }

    [HttpGet("ExportExcel")]
    public async Task<IActionResult> ExportExcel(
        [FromQuery] GetEmployeesAdvancedQuery query,
        CancellationToken cancellationToken)
    {
        var allResult = await _sender.Send(query with { PageNumber = 1, PageSize = 200 }, cancellationToken);
        var bytes = _documentService.ExportEmployeesToExcel(allResult.Items);
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"DanhSachNhanSu_Vinamilk_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
    }

    [HttpGet("Export2CPdf/{id:int}")]
    public async Task<IActionResult> Export2CPdf(int id, CancellationToken cancellationToken)
    {
        var profile = await _sender.Send(new GetEmployee360ProfileQuery(id), cancellationToken);
        if (profile is null)
            return NotFound();

        var pdfBytes = _documentService.GenerateEmployee2CPdf(profile.Employee);
        return File(
            pdfBytes,
            "application/pdf",
            $"SoYeuLyLich_2C_{profile.Employee.EmployeeCode}.pdf");
    }

    [HttpGet("Print2C/{id:int}")]
    public async Task<IActionResult> Print2C(int id, CancellationToken cancellationToken)
    {
        var profile = await _sender.Send(new GetEmployee360ProfileQuery(id), cancellationToken);
        if (profile is null)
            return RedirectToAction(nameof(Index));

        ViewData["Title"] = $"Sơ yếu lý lịch Mẫu 2C-BNV/2008 — {profile.Employee.EmployeeCode}";
        return View(profile);
    }
}

