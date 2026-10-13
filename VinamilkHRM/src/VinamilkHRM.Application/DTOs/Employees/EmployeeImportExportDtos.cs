namespace VinamilkHRM.Application.DTOs.Employees;

public record ExcelEmployeeRowDto(
    int RowNumber,
    string EmployeeCode,
    string FullName,
    string CitizenId,
    string Gender,
    string DateOfBirth,
    string Email,
    string PhoneNumber,
    string UnitCode,
    string? JobTitleCode,
    decimal BaseSalary,
    decimal Allowance,
    string? BankAccountNumber,
    string? BankName,
    string? TaxCode);

public record ExcelImportRowErrorDto(
    int RowNumber,
    string Column,
    string Value,
    string ErrorMessage);

public record ExcelImportPreviewResultDto(
    int TotalRows,
    int ValidRowsCount,
    int ErrorRowsCount,
    List<ExcelEmployeeRowDto> ValidRows,
    List<ExcelImportRowErrorDto> Errors);

public record ExcelImportCommitRequest(
    List<ExcelEmployeeRowDto> Rows);
