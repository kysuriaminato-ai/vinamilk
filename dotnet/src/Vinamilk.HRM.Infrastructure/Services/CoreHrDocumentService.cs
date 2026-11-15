using ClosedXML.Excel;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Application.CoreHR.ContractsAndTransfers;
using Vinamilk.HRM.Domain.Entities.Core;

namespace Vinamilk.HRM.Infrastructure.Services;

public sealed record ExcelImportRowReport(
    int RowNumber,
    string EmployeeCode,
    string FullName,
    bool IsSuccess,
    string Message);

public sealed record ExcelImportSummaryReport(
    int TotalRows,
    int SuccessCount,
    int ErrorCount,
    IReadOnlyList<ExcelImportRowReport> RowReports);

/// <summary>
/// Dịch vụ xử lý Tài liệu Nhân sự (ClosedXML Excel Import/Export có báo lỗi từng dòng & QuestPDF xuất Mẫu 2C).
/// </summary>
public sealed class CoreHrDocumentService
{
    private readonly IApplicationDbContext _context;
    private readonly IPiiEncryptionService _encryptionService;

    public CoreHrDocumentService(
        IApplicationDbContext context,
        IPiiEncryptionService encryptionService)
    {
        _context = context;
        _encryptionService = encryptionService;
    }

    /* ------------------------------------------------------------------------
     * 1. TẠO FILE EXCEL MẪU (.XLSX) ĐỂ IMPORT NHÂN VIÊN (CLOSEDXML)
     * ------------------------------------------------------------------------ */
    public byte[] GenerateEmployeeImportTemplateExcel()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Mau_Import_NhanVien");

        var headers = new[]
        {
            "Mã NV (*)", "Họ và Tên (*)", "Giới tính (Nam/Nữ)", "Ngày sinh (yyyy-MM-dd)",
            "Số CCCD (*)", "Số điện thoại", "Email công việc (*)", "Mã Đơn vị (*)",
            "Mã Chức danh (*)", "Lương cơ bản VNĐ (*)", "Ngày vào làm (yyyy-MM-dd)"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0213B0");
        }

        // Dòng dữ liệu mẫu 1 (Hợp lệ)
        ws.Cell(2, 1).Value = "VNM-0091";
        ws.Cell(2, 2).Value = "Nguyễn Hoàng Bảo Ngọc";
        ws.Cell(2, 3).Value = "Nữ";
        ws.Cell(2, 4).Value = "1996-08-15";
        ws.Cell(2, 5).Value = "079196008812";
        ws.Cell(2, 6).Value = "0908112233";
        ws.Cell(2, 7).Value = "ngoc.nhb@vinamilk.com.vn";
        ws.Cell(2, 8).Value = "P_IT";
        ws.Cell(2, 9).Value = "CV_CC";
        ws.Cell(2, 10).Value = 28500000;
        ws.Cell(2, 11).Value = "2026-11-01";

        // Dòng dữ liệu mẫu 2 (Cố tình chứa lỗi để kiểm thử báo cáo lỗi từng dòng)
        ws.Cell(3, 1).Value = "VNM-0001"; // Trùng Mã NV đã tồn tại
        ws.Cell(3, 2).Value = "Trần Văn Lỗi Mẫu";
        ws.Cell(3, 3).Value = "Nam";
        ws.Cell(3, 4).Value = "1992-04-20";
        ws.Cell(3, 5).Value = "123"; // CCCD không đủ 12 số
        ws.Cell(3, 6).Value = "0912334455";
        ws.Cell(3, 7).Value = "loi.tv@vinamilk.com.vn";
        ws.Cell(3, 8).Value = "MA_PHONG_SAI";
        ws.Cell(3, 9).Value = "CV_CC";
        ws.Cell(3, 10).Value = -5000000;
        ws.Cell(3, 11).Value = "2026-11-01";

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    /* ------------------------------------------------------------------------
     * 2. IMPORT NHÂN VIÊN TỪ EXCEL (.XLSX) KÈM BÁO LỖI TỪNG DÒNG CHI TIẾT
     * ------------------------------------------------------------------------ */
    public async Task<ExcelImportSummaryReport> ImportEmployeesFromExcelAsync(
        Stream excelStream,
        CancellationToken cancellationToken = default)
    {
        var rowReports = new List<ExcelImportRowReport>();
        using var workbook = new XLWorkbook(excelStream);
        var ws = workbook.Worksheets.First();

        var existingCodes = (await _context.Employees
                .AsNoTracking()
                .Select(e => e.EmployeeCode)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var unitsByCode = await _context.OrganizationUnits
            .AsNoTracking()
            .ToDictionaryAsync(u => u.UnitCode, u => u.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var defaultUnitId = unitsByCode.Values.FirstOrDefault();

        var positionsByCode = await _context.Positions
            .AsNoTracking()
            .ToDictionaryAsync(p => p.PositionCode, p => p.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var defaultPositionId = positionsByCode.Values.FirstOrDefault();

        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
        var successCount = 0;
        var errorCount = 0;

        for (int rowNum = 2; rowNum <= lastRow; rowNum++)
        {
            var code = ws.Cell(rowNum, 1).GetString().Trim().ToUpperInvariant();
            var fullName = ws.Cell(rowNum, 2).GetString().Trim();
            var gender = ws.Cell(rowNum, 3).GetString().Trim();
            var dobStr = ws.Cell(rowNum, 4).GetString().Trim();
            var cccd = ws.Cell(rowNum, 5).GetString().Trim();
            var phone = ws.Cell(rowNum, 6).GetString().Trim();
            var email = ws.Cell(rowNum, 7).GetString().Trim();
            var unitCode = ws.Cell(rowNum, 8).GetString().Trim();
            var posCode = ws.Cell(rowNum, 9).GetString().Trim();
            var salaryStr = ws.Cell(rowNum, 10).GetString().Trim();
            var hireDateStr = ws.Cell(rowNum, 11).GetString().Trim();

            if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(fullName))
                continue;

            var rowErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(code))
                rowErrors.Add("Thiếu Mã nhân viên.");
            else if (existingCodes.Contains(code))
                rowErrors.Add($"Mã nhân viên '{code}' đã tồn tại trong CSDL.");

            if (string.IsNullOrWhiteSpace(fullName))
                rowErrors.Add("Thiếu Họ và tên.");

            if (string.IsNullOrWhiteSpace(cccd) || cccd.Length < 9)
                rowErrors.Add("Số CCCD phải từ 9–12 chữ số.");

            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
                rowErrors.Add("Email công việc không hợp lệ.");

            if (!decimal.TryParse(salaryStr, out var baseSalary) || baseSalary <= 0)
                rowErrors.Add("Mức lương cơ bản phải lớn hơn 0 VNĐ.");

            int unitId = defaultUnitId;
            if (!string.IsNullOrWhiteSpace(unitCode) && !unitsByCode.TryGetValue(unitCode, out unitId))
            {
                rowErrors.Add($"Mã đơn vị '{unitCode}' không tồn tại trong cây tổ chức.");
            }

            int posId = defaultPositionId;
            if (!string.IsNullOrWhiteSpace(posCode) && !positionsByCode.TryGetValue(posCode, out posId))
            {
                rowErrors.Add($"Mã chức danh '{posCode}' không tồn tại.");
            }

            if (rowErrors.Count > 0)
            {
                errorCount++;
                rowReports.Add(new ExcelImportRowReport(
                    rowNum,
                    code,
                    fullName,
                    false,
                    string.Join(" | ", rowErrors)));
                continue;
            }

            var dob = DateOnly.TryParse(dobStr, out var parsedDob) ? parsedDob : new DateOnly(1995, 1, 1);
            var hireDate = DateOnly.TryParse(hireDateStr, out var parsedHire) ? parsedHire : DateOnly.FromDateTime(DateTime.Today);

            var newEmp = new Employee
            {
                EmployeeCode = code,
                FullName = fullName,
                Gender = string.IsNullOrWhiteSpace(gender) ? "Nam" : gender,
                DateOfBirth = dob,
                CitizenIdEncrypted = _encryptionService.Encrypt(cccd),
                CitizenIdHash = _encryptionService.ComputeDeterministicHash(cccd),
                PhoneNumber = phone,
                WorkEmail = email,
                OrganizationUnitId = unitId,
                PositionId = posId,
                BaseSalary = baseSalary,
                PositionAllowance = 2000000m,
                OtherAllowance = 1500000m,
                HireDate = hireDate,
                WorkStatus = "Official",
                CurrentContractType = "Fixed12M"
            };

            await _context.Employees.AddAsync(newEmp, cancellationToken);
            existingCodes.Add(code);
            successCount++;

            rowReports.Add(new ExcelImportRowReport(
                rowNum,
                code,
                fullName,
                true,
                $"Import thành công ({baseSalary:N0} VNĐ)"));
        }

        if (successCount > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new ExcelImportSummaryReport(
            rowReports.Count,
            successCount,
            errorCount,
            rowReports);
    }

    /* ------------------------------------------------------------------------
     * 3. EXPORT DANH SÁCH NHÂN VIÊN RA EXCEL (.XLSX CLOSEDXML)
     * ------------------------------------------------------------------------ */
    public byte[] ExportEmployeesToExcel(IReadOnlyList<Employee> employees)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("DanhSachNhanSu_Vinamilk");

        ws.Cell(1, 1).Value = "CÔNG TY CỔ PHẦN SỮA VIỆT NAM (VINAMILK) — DANH SÁCH HỒ SƠ NHÂN SỰ";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#0213B0");

        var headers = new[]
        {
            "STT", "Mã NV", "Họ và Tên", "Giới tính", "Ngày sinh", "Đơn vị công tác",
            "Chức danh", "Email công việc", "Ngày vào làm", "Loại HĐLĐ", "Lương cơ bản (VNĐ)", "Phụ cấp (VNĐ)", "Trạng thái"
        };

        for (int c = 0; c < headers.Length; c++)
        {
            var cell = ws.Cell(3, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0213B0");
        }

        var rowIdx = 4;
        var stt = 1;
        foreach (var e in employees)
        {
            ws.Cell(rowIdx, 1).Value = stt++;
            ws.Cell(rowIdx, 2).Value = e.EmployeeCode;
            ws.Cell(rowIdx, 3).Value = e.FullName;
            ws.Cell(rowIdx, 4).Value = e.Gender;
            ws.Cell(rowIdx, 5).Value = e.DateOfBirth.ToString("dd/MM/yyyy");
            ws.Cell(rowIdx, 6).Value = e.OrganizationUnit?.UnitName ?? "";
            ws.Cell(rowIdx, 7).Value = e.Position?.PositionName ?? "";
            ws.Cell(rowIdx, 8).Value = e.WorkEmail ?? "";
            ws.Cell(rowIdx, 9).Value = e.HireDate.ToString("dd/MM/yyyy");
            ws.Cell(rowIdx, 10).Value = e.CurrentContractType;
            ws.Cell(rowIdx, 11).Value = e.BaseSalary;
            ws.Cell(rowIdx, 11).Style.NumberFormat.Format = "#,##0";
            ws.Cell(rowIdx, 12).Value = e.PositionAllowance + e.OtherAllowance;
            ws.Cell(rowIdx, 12).Style.NumberFormat.Format = "#,##0";
            ws.Cell(rowIdx, 13).Value = e.WorkStatus;
            rowIdx++;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    /* ------------------------------------------------------------------------
     * 4. XUẤT PDF SƠ YẾU LÝ LỊCH MẪU 2C-BNV/2008 BẰNG QUESTPDF
     * ------------------------------------------------------------------------ */
    public byte[] GenerateEmployee2CPdf(Employee emp)
    {
        var cccd = _encryptionService.Decrypt(emp.CitizenIdEncrypted);

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.8f, QuestPDF.Infrastructure.Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10.5f));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text("CÔNG TY CỔ PHẦN SỮA VIỆT NAM").Bold();
                            left.Item().Text("(VINAMILK)").Bold();
                            left.Item().Text($"Mã hồ sơ: {emp.EmployeeCode}");
                        });
                        row.RelativeItem().AlignRight().Column(right =>
                        {
                            right.Item().Text("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM").Bold();
                            right.Item().Text("Độc lập - Tự do - Hạnh phúc").Italic();
                            right.Item().Text("Mẫu 2C-BNV/2008").FontSize(9);
                        });
                    });

                    col.Item().PaddingTop(12).AlignCenter().Text("SƠ YẾU LÝ LỊCH NHÂN SỰ (MẪU 2C)").FontSize(15).Bold();
                    col.Item().PaddingBottom(10).AlignCenter().Text($"Họ và tên: {emp.FullName.ToUpperInvariant()} — Mã NV: {emp.EmployeeCode}").Bold();
                });

                page.Content().Column(col =>
                {
                    col.Spacing(6);
                    col.Item().Text($"1. Họ và tên khai sinh: {emp.FullName.ToUpperInvariant()}     |     2. Giới tính: {emp.Gender}").Bold();
                    col.Item().Text($"3. Ngày tháng năm sinh: {emp.DateOfBirth:dd/MM/yyyy}     |     4. Dân tộc: {emp.Ethnicity}     |     5. Tôn giáo: {emp.Religion}");
                    col.Item().Text($"6. Số CCCD (Đã giải mã bảo mật): {cccd}     |     7. Số sổ BHXH: {emp.SocialInsuranceNumber ?? "Đang cập nhật"}");
                    col.Item().Text($"8. Nơi ở hiện nay: {emp.CurrentAddress ?? emp.PermanentAddress ?? "TP. Hồ Chí Minh"}");
                    col.Item().Text($"9. Đơn vị công tác hiện tại: {emp.OrganizationUnit?.UnitName ?? "Vinamilk"}");
                    col.Item().Text($"10. Chức danh / Chức vụ: {emp.Position?.PositionName ?? "Chuyên viên"}");
                    col.Item().Text($"11. Ngày tuyển dụng vào Công ty: {emp.HireDate:dd/MM/yyyy}     |     Loại HĐLĐ: {emp.CurrentContractType}");
                    col.Item().Text($"12. Trình độ chuyên môn: {emp.EducationLevel?.LevelName ?? "Đại học"} — Chuyên ngành: {emp.Major ?? "Quản trị / Kỹ thuật"}");
                    col.Item().Text($"13. Mức lương cơ bản hiện hưởng: {emp.BaseSalary:N0} VNĐ/tháng     |     Phụ cấp chức vụ: {emp.PositionAllowance:N0} VNĐ/tháng");

                    col.Item().PaddingTop(10).Text("14. TÓM TẮT QUÁ TRÌNH CÔNG TÁC:").Bold();
                    if (emp.EmploymentHistories.Any())
                    {
                        foreach (var h in emp.EmploymentHistories.OrderBy(x => x.FromDate))
                        {
                            col.Item().Text($"• Từ {h.FromDate:MM/yyyy} đến {(h.ToDate.HasValue ? h.ToDate.Value.ToString("MM/yyyy") : "Nay")}: {h.PositionName} tại {h.DepartmentName} ({h.Notes})");
                        }
                    }
                    else
                    {
                        col.Item().Text($"• Từ {emp.HireDate:MM/yyyy} đến Nay: {emp.Position?.PositionName} tại {emp.OrganizationUnit?.UnitName}");
                    }

                    col.Item().PaddingTop(10).Text("15. QUAN HỆ GIA ĐÌNH & NGƯỜI PHỤ THUỘC GIẢM TRỪ GIA CẢNH:").Bold();
                    if (emp.Dependents.Any())
                    {
                        foreach (var d in emp.Dependents)
                        {
                            col.Item().Text($"• {d.FullName} — Quan hệ: {d.Relationship} — Năm sinh: {d.DateOfBirth?.Year} — Giảm trừ thuế TNCN: {(d.IsTaxDeductible ? "Có" : "Không")}");
                        }
                    }
                    else
                    {
                        col.Item().Text("• Không có người phụ thuộc đăng ký giảm trừ gia cảnh.");
                    }
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Trích xuất từ Hệ thống Vinamilk Enterprise HRM (.NET 8) — Trang ");
                    x.CurrentPageNumber();
                });
            });
        });

        return doc.GeneratePdf();
    }
}

/// <summary>
/// Hangfire Background Job chạy định kỳ hằng ngày để quét hợp đồng lao động sắp hết hạn trước 30 ngày.
/// </summary>
public sealed class ContractExpirationAlertJob
{
    private readonly ISender _sender;
    private readonly ILogger<ContractExpirationAlertJob> _logger;

    public ContractExpirationAlertJob(ISender sender, ILogger<ContractExpirationAlertJob> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task ExecuteDailyScanAsync(CancellationToken cancellationToken = default)
    {
        var count = await _sender.Send(new ScanExpiringContractsCommand(), cancellationToken);
        _logger.LogInformation("[Hangfire ContractExpirationAlertJob] Đã quét và tạo cảnh báo cho {Count} hợp đồng sắp hết hạn trong 30 ngày.", count);
    }
}
