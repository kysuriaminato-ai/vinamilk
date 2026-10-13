using System.Collections.Generic;

namespace VinamilkHRM.Domain.Services.Payroll;

/// <summary>
/// Kết quả tính lương chi tiết cho một nhân viên trong kỳ từ Động cơ tính lương thuần.
/// </summary>
public class PayrollResult
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PayrollPeriod { get; set; } = string.Empty;

    public decimal StandardWorkDays { get; set; }
    public decimal ActualWorkDays { get; set; }
    public decimal CompanyPaidDays { get; set; }
    public decimal HourlyWage { get; set; }

    public decimal GrossSalary { get; set; }
    public decimal InsurableSalary { get; set; }
    public bool IsExemptFromInsurance { get; set; }

    // Các khoản người lao động trích nộp
    public decimal EmployeeSocialInsurance { get; set; }
    public decimal EmployeeHealthInsurance { get; set; }
    public decimal EmployeeUnemploymentInsurance { get; set; }
    public decimal TotalEmployeeInsurance { get; set; }
    public decimal EmployeeUnionFee { get; set; }

    // Chi phí người sử dụng lao động (Vinamilk) chịu trách nhiệm
    public decimal EmployerSocialInsurance { get; set; }
    public decimal EmployerHealthInsurance { get; set; }
    public decimal EmployerUnemploymentInsurance { get; set; }
    public decimal EmployerUnionFee { get; set; }
    public decimal TotalEmployerInsurance { get; set; }
    public decimal TotalEmployerCost { get; set; }

    // Thuế thu nhập cá nhân
    public decimal TaxExemptAmount { get; set; }
    public decimal TaxableIncome { get; set; }
    public decimal SelfRelief { get; set; }
    public decimal DependentRelief { get; set; }
    public decimal TaxAssessedIncome { get; set; } // TNTT
    public decimal PersonalIncomeTax { get; set; }

    // Khấu trừ khác
    public decimal DamageCompensationDeduction { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetSalary { get; set; }

    // Danh sách dòng diễn giải chi tiết
    public List<PayrollLine> Lines { get; set; } = new();
    public string CompactFormulaTrace { get; set; } = string.Empty;
    public List<string> Warnings { get; set; } = new();
}
