using VinamilkHRM.Application.DTOs.Employees;
using VinamilkHRM.Application.Interfaces;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Interfaces.Repositories;

namespace VinamilkHRM.Application.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IOrganizationalUnitRepository _unitRepository;
    private readonly IUnitOfWork _unitOfWork;

    public EmployeeService(
        IEmployeeRepository employeeRepository,
        IOrganizationalUnitRepository unitRepository,
        IUnitOfWork unitOfWork)
    {
        _employeeRepository = employeeRepository ?? throw new ArgumentNullException(nameof(employeeRepository));
        _unitRepository = unitRepository ?? throw new ArgumentNullException(nameof(unitRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<EmployeeDto?> GetEmployeeByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetWithDetailsAsync(id, cancellationToken);
        return employee == null ? null : MapToDto(employee);
    }

    public async Task<EmployeeDto?> GetEmployeeByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByCodeAsync(code, cancellationToken);
        return employee == null ? null : MapToDto(employee);
    }

    public async Task<IReadOnlyList<EmployeeDto>> GetAllEmployeesAsync(CancellationToken cancellationToken = default)
    {
        var employees = await _employeeRepository.GetAllAsync(cancellationToken);
        return employees.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<EmployeeDto>> GetEmployeesByUnitAsync(int unitId, CancellationToken cancellationToken = default)
    {
        var employees = await _employeeRepository.GetByUnitIdAsync(unitId, cancellationToken);
        return employees.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<VinamilkHRM.Application.DTOs.OrganizationalUnits.OrganizationalUnitDto>> GetAllUnitsAsync(CancellationToken cancellationToken = default)
    {
        var units = await _unitRepository.GetAllAsync(cancellationToken);
        return units.Select(u => new VinamilkHRM.Application.DTOs.OrganizationalUnits.OrganizationalUnitDto(
            u.Id,
            u.UnitCode,
            u.UnitName,
            u.UnitType,
            u.Address,
            u.PhoneNumber,
            u.Email,
            u.TaxCode,
            u.ParentUnitId,
            u.ParentUnit?.UnitName,
            u.ManagerEmployeeId,
            u.ManagerEmployee?.FullName,
            u.Employees.Count
        )).ToList();
    }

    /// <summary>
    /// Minh chứng tính Đa hình (Polymorphism):
    /// Phương thức CalculateOvertimePay được gọi động dựa trên kiểu thực tế (Runtime Type) của Employee
    /// (OfficeEmployee, FactoryWorker, FarmSpecialist, StoreAssociate).
    /// </summary>
    public async Task<decimal> CalculateOvertimePayAsync(int employeeId, decimal hours, bool isHoliday, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy nhân viên với Id={employeeId}.", "ERR_EMP_NOT_FOUND");

        return employee.CalculateOvertimePay(hours, isHoliday);
    }

    public async Task PromoteSalaryAsync(int employeeId, decimal newBaseSalary, decimal newAllowance, string reason, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy nhân viên với Id={employeeId}.", "ERR_EMP_NOT_FOUND");

        employee.PromoteSalary(newBaseSalary, newAllowance, reason);
        _employeeRepository.Update(employee);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static EmployeeDto MapToDto(Employee e)
    {
        string roleInfo = e switch
        {
            OfficeEmployee off => $"Chức danh: {off.JobTitle} | Bàn: {off.CubicleNumber}",
            FactoryWorker fac => $"Tổ máy: {fac.ProductionShiftGroup} | Chuẩn AT: {fac.SafetyLevel}",
            FarmSpecialist farm => $"Chuyên môn: {farm.SpecializationArea} | Mã AT sinh học: {farm.BioSecurityClearanceCode}",
            StoreAssociate store => $"POS: {store.CashRegisterId} | Điểm DV: {store.CustomerServiceRating}/5★",
            _ => "Nhân viên tiêu chuẩn"
        };

        return new EmployeeDto(
            e.Id,
            e.EmployeeCode,
            e.FirstName,
            e.LastName,
            e.FullName,
            e.CitizenId,
            e.DateOfBirth,
            e.Gender,
            e.Email,
            e.PhoneNumber,
            e.Address,
            e.HireDate,
            e.EmployeeType,
            e.OrganizationalUnitId,
            e.OrganizationalUnit?.UnitName,
            e.BaseSalary,
            e.Allowance,
            e.IsActive,
            e.ManagerId,
            e.Manager?.FullName,
            roleInfo);
    }
}
