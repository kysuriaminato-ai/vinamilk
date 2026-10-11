using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.DTOs.Salaries;

public record SalaryProposalDto(
    int Id,
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string UnitName,
    int ProposedById,
    string ProposedByName,
    decimal CurrentSalary,
    decimal ProposedSalary,
    decimal CurrentAllowance,
    decimal ProposedAllowance,
    decimal SalaryIncreaseAmount,
    decimal IncreasePercentage,
    string Reason,
    DateTime EffectiveDate,
    SalaryProposalStatus Status,
    int? DepartmentHeadApprovedById,
    string? DepartmentHeadName,
    string? DepartmentHeadNotes,
    int? HRDirectorApprovedById,
    string? HRDirectorName,
    string? HRDirectorNotes,
    DateTime? FinalDecisionDate,
    DateTime CreatedAt);
