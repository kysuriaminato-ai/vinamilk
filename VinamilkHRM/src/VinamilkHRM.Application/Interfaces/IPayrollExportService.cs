using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace VinamilkHRM.Application.Interfaces;

public record BankPaymentItem(
    int OrderNumber,
    string EmployeeCode,
    string FullName,
    string BankAccountNumber,
    string BankName,
    decimal Amount,
    string Description);

public record CostCenterJournalEntry(
    string CostCenterCode,
    string CostCenterName,
    string DebitAccount,
    string CreditAccount,
    decimal Amount,
    string Description);

public record UnitSalaryFundItem(
    int UnitId,
    string UnitCode,
    string UnitName,
    int Headcount,
    decimal TotalGross,
    decimal TotalNet,
    decimal TotalEmployerCost,
    decimal TotalPit);

public record SocialInsuranceD02LtItem(
    int OrderNumber,
    string EmployeeCode,
    string FullName,
    string SocialInsuranceNumber,
    string ChangeType, // TANG_MOI, DIEU_CHINH_LUONG, GIAM_HAN_HD
    decimal OldSalary,
    decimal NewSalary,
    string EffectiveDate,
    string Note);

public record AnnualPitFinalization05QttItem(
    int OrderNumber,
    string EmployeeCode,
    string FullName,
    string TaxCode,
    decimal TotalIncome,
    decimal TaxExemptIncome,
    decimal TotalInsuranceDeducted,
    decimal SelfRelief,
    decimal DependentRelief,
    decimal TaxableIncome,
    decimal TotalTaxDeducted,
    decimal TaxPayableOrRefund);

public interface IPayrollExportService
{
    Task<byte[]> ExportBankPaymentCsvAsync(int payrollRunId, string bankFormat, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CostCenterJournalEntry>> GenerateCostCenterJournalEntriesAsync(int payrollRunId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UnitSalaryFundItem>> GenerateUnitSalaryFundReportAsync(string period, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SocialInsuranceD02LtItem>> GenerateSocialInsuranceD02LtAsync(string period, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AnnualPitFinalization05QttItem>> GenerateAnnualPitFinalizationAsync(int year, CancellationToken cancellationToken = default);
}
