using Vinamilk.HRM.Domain.Entities.Core;
using Vinamilk.HRM.Domain.Entities.Payroll;

namespace Vinamilk.HRM.Application.Payroll;

public sealed record PitBracketTaxSliceDto(
    int BracketLevel,
    decimal LowerBound,
    decimal? UpperBound,
    decimal TaxRate,
    decimal IncomeInBracket,
    decimal TaxAmount);

public sealed record PayslipPdfResult(
    byte[] PdfBytes,
    string FileName,
    string PasswordHint,
    bool IsPasswordProtected);

public sealed record BankTransferRowDto(
    int Stt,
    string EmployeeCode,
    string AccountNumber,
    string AccountHolderUppercaseNoAccent,
    string BankName,
    string BankBranch,
    decimal Amount,
    string TransferDescription);

public sealed record BankTransferExportResult(
    byte[] FileBytes,
    string ContentType,
    string FileName,
    string TemplateCode,
    int EmployeeCount,
    decimal TotalAmount);

public sealed record PayrollDepartmentSummaryRowDto(
    string UnitCode,
    string UnitName,
    int Headcount,
    decimal TotalActualAndAllowances,
    decimal TotalOtAndBonus,
    decimal TotalGross,
    decimal TotalEmployeeInsurance,
    decimal TotalCompanyInsurance,
    decimal TotalCompanyUnionFee,
    decimal TotalPit,
    decimal TotalNet);

public sealed record PayrollDepartmentSummaryReportDto(
    int PayrollPeriodId,
    string PeriodCode,
    string PeriodTitle,
    DateOnly EndDate,
    string Status,
    int TotalEmployees,
    IReadOnlyList<PayrollDepartmentSummaryRowDto> Rows,
    decimal GrandTotalGross,
    decimal GrandTotalEmployeeInsurance,
    decimal GrandTotalCompanyInsurance,
    decimal GrandTotalUnionFee,
    decimal GrandTotalPit,
    decimal GrandTotalNet);

public sealed record PayslipDispatchItemDto(
    long LogId,
    string EmployeeCode,
    string EmployeeName,
    string RecipientEmail,
    string Status,
    int AttemptCount,
    string? ErrorMessage,
    DateTime? SentAt);

public sealed record PayslipDispatchBatchReportDto(
    Guid BatchId,
    int TotalCount,
    int QueuedCount,
    int SentCount,
    int FailedCount,
    IReadOnlyList<PayslipDispatchItemDto> Items);

public interface IPayrollDocumentService
{
    PayslipPdfResult GeneratePayslipPdf(
        PayrollRecord record,
        Employee employee,
        PayrollPeriod period,
        IReadOnlyList<PitBracket> pitBrackets,
        bool applyPassword = true);

    PayslipPdfResult GenerateTerminationSettlementPdf(
        EmployeeTerminationSettlement settlement,
        Employee employee);

    byte[] ExportDepartmentSummaryExcel(PayrollDepartmentSummaryReportDto report);
}

public interface IBankTransferTemplateFormatter
{
    string TemplateCode { get; }
    string DisplayName { get; }
    byte[] ExportCsv(PayrollPeriod period, IReadOnlyList<BankTransferRowDto> rows);
    byte[] ExportExcel(PayrollPeriod period, IReadOnlyList<BankTransferRowDto> rows);
}

public interface IPayslipBatchDispatcher
{
    Task<Guid> EnqueueBatchAsync(
        int payrollPeriodId,
        IReadOnlyList<long> dispatchLogIds,
        CancellationToken cancellationToken = default);
}

public interface IPayslipEmailJob
{
    Task SendSinglePayslipAsync(long dispatchLogId, CancellationToken cancellationToken = default);
}

