using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Application.DTOs.Employees;
using VinamilkHRM.Application.Interfaces;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.Services;

public class EmployeeExcelService : IEmployeeExcelService
{
    private readonly IApplicationDbContext _context;
    private readonly IFieldEncryptionService _encryptionService;

    public EmployeeExcelService(
        IApplicationDbContext context,
        IFieldEncryptionService encryptionService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
    }

    public Task<byte[]> ExportToExcelAsync(IReadOnlyList<EmployeeDetailDto> employees, CancellationToken cancellationToken = default)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("DanhSachNhanSu");

        // Tiêu đề cột
        string[] headers =
        [
            "STT", "Mã Nhân Viên", "Họ Tên", "Số CCCD", "Ngày Sinh", "Giới Tính",
            "Đơn Vị Công Tác", "Vùng Lương", "Lương Cơ Bản (P1)", "Phụ Cấp",
            "Số TK Ngân Hàng", "Ngân Hàng", "Mã Số Thuế", "Điện Thoại", "Email"
        ];

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#003399"); // Vinamilk Corporate Blue
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        int row = 2;
        foreach (var emp in employees)
        {
            worksheet.Cell(row, 1).Value = row - 1;
            worksheet.Cell(row, 2).Value = emp.EmployeeCode;
            worksheet.Cell(row, 3).Value = emp.FullName;
            worksheet.Cell(row, 4).Value = emp.RawCitizenId ?? emp.MaskedCitizenId;
            worksheet.Cell(row, 5).Value = emp.DateOfBirth.ToString("dd/MM/yyyy");
            worksheet.Cell(row, 6).Value = emp.Gender == Gender.Female ? "Nữ" : "Nam";
            worksheet.Cell(row, 7).Value = emp.UnitName ?? "";
            worksheet.Cell(row, 8).Value = emp.MinimumWageRegion.ToString();
            worksheet.Cell(row, 9).Value = (double)emp.BaseSalary;
            worksheet.Cell(row, 9).Style.NumberFormat.Format = "#,##0";
            worksheet.Cell(row, 10).Value = (double)emp.Allowance;
            worksheet.Cell(row, 10).Style.NumberFormat.Format = "#,##0";
            worksheet.Cell(row, 11).Value = emp.RawBankAccountNumber ?? emp.MaskedBankAccountNumber ?? "";
            worksheet.Cell(row, 12).Value = emp.BankName ?? "";
            worksheet.Cell(row, 13).Value = emp.TaxCode ?? "";
            worksheet.Cell(row, 14).Value = emp.PhoneNumber;
            worksheet.Cell(row, 15).Value = emp.Email;

            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);
        return Task.FromResult(memoryStream.ToArray());
    }

    public async Task<ExcelImportPreviewResultDto> PreviewImportAsync(Stream excelStream, CancellationToken cancellationToken = default)
    {
        var validRows = new List<ExcelEmployeeRowDto>();
        var errors = new List<ExcelImportRowErrorDto>();

        var existingUnitCodes = await _context.OrganizationalUnits
            .AsNoTracking()
            .Select(u => u.UnitCode)
            .ToListAsync(cancellationToken);
        var unitCodeSet = new HashSet<string>(existingUnitCodes, StringComparer.OrdinalIgnoreCase);

        var existingEmpCodes = await _context.Employees
            .AsNoTracking()
            .Select(e => e.EmployeeCode)
            .ToListAsync(cancellationToken);
        var empCodeSet = new HashSet<string>(existingEmpCodes, StringComparer.OrdinalIgnoreCase);

        using var workbook = new XLWorkbook(excelStream);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet == null)
        {
            return new ExcelImportPreviewResultDto(0, 0, 0, validRows, errors);
        }

        var rows = worksheet.RangeUsed()?.RowsUsed().Skip(1); // Bỏ qua header
        if (rows == null)
        {
            return new ExcelImportPreviewResultDto(0, 0, 0, validRows, errors);
        }

        int rowIndex = 1;
        int totalRows = 0;

        foreach (var r in rows)
        {
            rowIndex++;
            totalRows++;

            string empCode = r.Cell(2).GetString().Trim().ToUpperInvariant();
            string fullName = r.Cell(3).GetString().Trim();
            string cccd = r.Cell(4).GetString().Trim();
            string dobStr = r.Cell(5).GetString().Trim();
            string gender = r.Cell(6).GetString().Trim();
            string unitCode = r.Cell(7).GetString().Trim().ToUpperInvariant();
            string baseSalaryStr = r.Cell(9).GetString().Trim();
            string allowanceStr = r.Cell(10).GetString().Trim();
            string bankAccount = r.Cell(11).GetString().Trim();
            string bankName = r.Cell(12).GetString().Trim();
            string taxCode = r.Cell(13).GetString().Trim();
            string phone = r.Cell(14).GetString().Trim();
            string email = r.Cell(15).GetString().Trim();

            bool rowHasError = false;

            // 1. Kiểm tra Mã NV
            if (string.IsNullOrWhiteSpace(empCode))
            {
                errors.Add(new ExcelImportRowErrorDto(rowIndex, "Mã Nhân Viên", empCode, "Mã nhân viên không được để trống."));
                rowHasError = true;
            }
            else if (empCodeSet.Contains(empCode))
            {
                errors.Add(new ExcelImportRowErrorDto(rowIndex, "Mã Nhân Viên", empCode, $"Mã nhân viên '{empCode}' đã tồn tại trong hệ thống."));
                rowHasError = true;
            }

            // 2. Họ tên
            if (string.IsNullOrWhiteSpace(fullName) || fullName.Split(' ').Length < 2)
            {
                errors.Add(new ExcelImportRowErrorDto(rowIndex, "Họ Tên", fullName, "Họ tên phải có ít nhất 2 từ (Họ và Tên)."));
                rowHasError = true;
            }

            // 3. Số CCCD
            if (string.IsNullOrWhiteSpace(cccd) || cccd.Length < 9 || !Regex.IsMatch(cccd, @"^\d{9,12}$"))
            {
                errors.Add(new ExcelImportRowErrorDto(rowIndex, "Số CCCD", cccd, "Số CCCD phải gồm 9 hoặc 12 chữ số."));
                rowHasError = true;
            }

            // 4. Ngày sinh (đủ 18 tuổi)
            if (!DateTime.TryParse(dobStr, out var dob) && !DateTime.TryParseExact(dobStr, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out dob))
            {
                errors.Add(new ExcelImportRowErrorDto(rowIndex, "Ngày Sinh", dobStr, "Ngày sinh không đúng định dạng (dd/MM/yyyy hoặc yyyy-MM-dd)."));
                rowHasError = true;
            }
            else if (dob >= DateTime.UtcNow.AddYears(-18))
            {
                errors.Add(new ExcelImportRowErrorDto(rowIndex, "Ngày Sinh", dobStr, "Người lao động phải từ đủ 18 tuổi trở lên."));
                rowHasError = true;
            }

            // 5. Đơn vị công tác
            if (string.IsNullOrWhiteSpace(unitCode) || !unitCodeSet.Contains(unitCode))
            {
                errors.Add(new ExcelImportRowErrorDto(rowIndex, "Đơn Vị Công Tác", unitCode, $"Mã đơn vị '{unitCode}' không tồn tại trong hệ thống."));
                rowHasError = true;
            }

            // 6. Lương cơ bản
            decimal baseSalary = 0m;
            if (!decimal.TryParse(baseSalaryStr, out baseSalary) || baseSalary <= 0)
            {
                errors.Add(new ExcelImportRowErrorDto(rowIndex, "Lương Cơ Bản", baseSalaryStr, "Lương cơ bản phải là số dương lớn hơn 0."));
                rowHasError = true;
            }

            decimal allowance = 0m;
            decimal.TryParse(allowanceStr, out allowance);

            if (!rowHasError)
            {
                validRows.Add(new ExcelEmployeeRowDto(
                    RowNumber: rowIndex,
                    EmployeeCode: empCode,
                    FullName: fullName,
                    CitizenId: cccd,
                    Gender: gender,
                    DateOfBirth: dob.ToString("yyyy-MM-dd"),
                    Email: !string.IsNullOrWhiteSpace(email) ? email : $"{empCode.ToLower()}@vinamilk.com.vn",
                    PhoneNumber: !string.IsNullOrWhiteSpace(phone) ? phone : "0900000000",
                    UnitCode: unitCode,
                    JobTitleCode: null,
                    BaseSalary: baseSalary,
                    Allowance: allowance,
                    BankAccountNumber: bankAccount,
                    BankName: bankName,
                    TaxCode: taxCode
                ));
            }
        }

        return new ExcelImportPreviewResultDto(
            TotalRows: totalRows,
            ValidRowsCount: validRows.Count,
            ErrorRowsCount: errors.Select(e => e.RowNumber).Distinct().Count(),
            ValidRows: validRows,
            Errors: errors
        );
    }

    public async Task<int> CommitImportAsync(ExcelImportCommitRequest request, CancellationToken cancellationToken = default)
    {
        var unitLookup = await _context.OrganizationalUnits
            .AsNoTracking()
            .ToDictionaryAsync(u => u.UnitCode, u => u.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);

        int insertedCount = 0;

        foreach (var row in request.Rows)
        {
            if (!unitLookup.TryGetValue(row.UnitCode, out int unitId))
                continue;

            string rawCccd = row.CitizenId.Trim();
            if (rawCccd.Length < 9) rawCccd = rawCccd.PadRight(12, '0');

            string encCccd = _encryptionService.Encrypt(rawCccd);
            string? encBank = !string.IsNullOrEmpty(row.BankAccountNumber) 
                ? _encryptionService.Encrypt(row.BankAccountNumber.Trim()) 
                : null;

            string fullName = row.FullName.Trim();
            string firstName = "An";
            string lastName = "Nguyễn";
            int lastSpace = fullName.LastIndexOf(' ');
            if (lastSpace > 0)
            {
                lastName = fullName.Substring(0, lastSpace);
                firstName = fullName.Substring(lastSpace + 1);
            }
            else
            {
                firstName = fullName;
                lastName = "Vinamilk";
            }

            DateTime dob = DateTime.TryParse(row.DateOfBirth, out var parsedDob) ? parsedDob : new DateTime(1990, 1, 1);
            if (dob >= DateTime.UtcNow.AddYears(-18))
            {
                dob = DateTime.UtcNow.AddYears(-25);
            }

            var emp = new OfficeEmployee(
                employeeCode: row.EmployeeCode,
                firstName: firstName,
                lastName: lastName,
                citizenId: encCccd,
                dateOfBirth: dob,
                gender: row.Gender.Equals("Nữ", StringComparison.OrdinalIgnoreCase) ? Gender.Female : Gender.Male,
                email: row.Email,
                phoneNumber: row.PhoneNumber,
                address: "TP. Hồ Chí Minh",
                hireDate: DateTime.UtcNow.Date,
                organizationalUnitId: unitId,
                baseSalary: row.BaseSalary,
                allowance: row.Allowance,
                jobTitle: "Chuyên viên",
                deskLocation: "Văn phòng",
                extensionNumber: "0",
                cubicleNumber: "A"
            );

            if (!string.IsNullOrEmpty(encBank))
            {
                emp.UpdateBankInfo(encBank, row.BankName, "Hồ Chí Minh");
            }
            if (!string.IsNullOrEmpty(row.TaxCode))
            {
                emp.UpdateCitizenDetails(null, null, row.TaxCode);
            }

            _context.Employees.Add(emp);
            insertedCount++;
        }

        if (insertedCount > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return insertedCount;
    }
}
