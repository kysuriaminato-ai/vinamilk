using VinamilkHRM.Application.DTOs.Employees;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.Interfaces;

public interface IEmployeeService
{
    Task<EmployeeDto?> GetEmployeeByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<EmployeeDto?> GetEmployeeByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmployeeDto>> GetAllEmployeesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmployeeDto>> GetEmployeesByUnitAsync(int unitId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VinamilkHRM.Application.DTOs.OrganizationalUnits.OrganizationalUnitDto>> GetAllUnitsAsync(CancellationToken cancellationToken = default);
    Task<decimal> CalculateOvertimePayAsync(int employeeId, decimal hours, bool isHoliday, CancellationToken cancellationToken = default);
    Task<EmployeeDetailDto?> GetEmployeeDetailByIdAsync(int id, bool canViewSensitive = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmployeeDetailDto>> GetAllEmployeeDetailsAsync(bool canViewSensitive = false, CancellationToken cancellationToken = default);
    Task<DependentDto> AddDependentAsync(int employeeId, CreateDependentRequest request, CancellationToken cancellationToken = default);
    Task<EmployeeWorkHistoryDto> AddWorkHistoryAsync(int employeeId, CreateWorkHistoryRequest request, CancellationToken cancellationToken = default);
    Task<EmployeeEducationDto> AddEducationAsync(int employeeId, CreateEducationRequest request, CancellationToken cancellationToken = default);
    Task<EmployeeCertificateDto> AddCertificateAsync(int employeeId, CreateCertificateRequest request, CancellationToken cancellationToken = default);
    Task<EmployeeAttachmentDto> AddAttachmentAsync(int employeeId, string documentType, string fileName, string filePath, long fileSizeBytes, string checksumSha256, CancellationToken cancellationToken = default);
}
