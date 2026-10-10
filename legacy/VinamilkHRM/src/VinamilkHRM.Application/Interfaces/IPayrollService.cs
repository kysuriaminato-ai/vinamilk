using VinamilkHRM.Application.DTOs.Salaries;

namespace VinamilkHRM.Application.Interfaces;

public interface IPayrollService
{
    Task<PaySlipDto?> GetPaySlipAsync(int employeeId, string period, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaySlipDto>> GetPaySlipsByPeriodAsync(string period, CancellationToken cancellationToken = default);
    Task<PaySlipDto> GenerateMonthlyPayslipAsync(int employeeId, string period, CancellationToken cancellationToken = default);
    Task ApprovePaySlipAsync(int payslipId, CancellationToken cancellationToken = default);
    Task MarkPaySlipAsPaidAsync(int payslipId, string transactionReference, CancellationToken cancellationToken = default);
    Task<byte[]> ExportPayrollPdfAsync(int payslipId, CancellationToken cancellationToken = default);
}
