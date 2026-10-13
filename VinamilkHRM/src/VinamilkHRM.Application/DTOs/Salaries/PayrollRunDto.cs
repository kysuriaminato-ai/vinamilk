using System;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.DTOs.Salaries;

public record PayrollRunDto(
    int Id,
    string Period,
    int? OrganizationalUnitId,
    string? UnitName,
    PayrollRunStatus Status,
    decimal TotalGross,
    decimal TotalNet,
    decimal TotalEmployerCost,
    decimal TotalPersonalIncomeTax,
    decimal TotalEmployeeInsurance,
    decimal TotalEmployerInsurance,
    int EmployeeCount,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? ApprovedAccountantAt,
    DateTimeOffset? ApprovedDirectorAt,
    DateTimeOffset? LockedAt,
    DateTimeOffset? PaidAt,
    string? PaymentReference);

public record PayrollAnomalyDto(
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    string AnomalyType,
    decimal CurrentValue,
    decimal BaselineValue,
    decimal DiffPercentage,
    string Message);
