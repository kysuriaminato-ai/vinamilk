using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.DTOs.Salaries;

public record PaySlipDto(
    int Id,
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string UnitName,
    string PayrollPeriod,
    decimal StandardWorkDays,
    decimal ActualWorkDays,
    decimal BaseSalary,
    decimal Allowance,
    decimal OvertimeHours,
    decimal OvertimePay,
    decimal KpiBonus,
    decimal OtherBonuses,
    decimal GrossSalary,
    decimal SocialInsuranceDeduction,
    decimal HealthInsuranceDeduction,
    decimal UnemploymentInsuranceDeduction,
    decimal PersonalIncomeTax,
    decimal TotalDeductions,
    decimal NetSalary,
    PaySlipStatus Status,
    DateTime? PaymentDate,
    string? TransactionReference);
