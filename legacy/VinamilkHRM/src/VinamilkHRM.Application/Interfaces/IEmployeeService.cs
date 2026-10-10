using VinamilkHRM.Application.DTOs.Employees;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.Interfaces;

public interface IEmployeeService
{
    Task<EmployeeDto?> GetEmployeeByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<EmployeeDto?> GetEmployeeByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmployeeDto>> GetAllEmployeesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmployeeDto>> GetEmployeesByUnitAsync(int unitId, CancellationToken cancellationToken = default);
    Task<decimal> CalculateOvertimePayAsync(int employeeId, decimal hours, bool isHoliday, CancellationToken cancellationToken = default);
    Task PromoteSalaryAsync(int employeeId, decimal newBaseSalary, decimal newAllowance, string reason, CancellationToken cancellationToken = default);
}
