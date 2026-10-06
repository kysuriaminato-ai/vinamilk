using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.DTOs.Employees;

public record EmployeeDto(
    int Id,
    string EmployeeCode,
    string FirstName,
    string LastName,
    string FullName,
    string CitizenId,
    DateTime DateOfBirth,
    Gender Gender,
    string Email,
    string PhoneNumber,
    string Address,
    DateTime HireDate,
    EmployeeType EmployeeType,
    int OrganizationalUnitId,
    string? UnitName,
    decimal BaseSalary,
    decimal Allowance,
    bool IsActive,
    int? ManagerId,
    string? ManagerName,
    string? SpecializationOrRoleInfo);
