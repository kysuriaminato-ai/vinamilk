using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.DTOs.OrganizationalUnits;

public record OrganizationalUnitDto(
    int Id,
    string UnitCode,
    string UnitName,
    UnitType UnitType,
    string Address,
    string PhoneNumber,
    string Email,
    string TaxCode,
    int? ParentUnitId,
    string? ParentUnitName,
    int? ManagerEmployeeId,
    string? ManagerEmployeeName,
    int EmployeeCount);
