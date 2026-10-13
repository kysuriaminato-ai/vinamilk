using VinamilkHRM.Application.DTOs.Employees;

namespace VinamilkHRM.Application.Interfaces;

public interface IEmployeeExcelService
{
    Task<byte[]> ExportToExcelAsync(IReadOnlyList<EmployeeDetailDto> employees, CancellationToken cancellationToken = default);
    Task<ExcelImportPreviewResultDto> PreviewImportAsync(Stream excelStream, CancellationToken cancellationToken = default);
    Task<int> CommitImportAsync(ExcelImportCommitRequest request, CancellationToken cancellationToken = default);
}
