using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VinamilkHRM.Application.DTOs.Salaries;

namespace VinamilkHRM.Application.Interfaces;

public interface IPayrollService
{
    Task<PaySlipDto?> GetPaySlipAsync(int employeeId, string period, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaySlipDto>> GetPaySlipsByPeriodAsync(string period, CancellationToken cancellationToken = default);
    Task<PaySlipDto> GenerateMonthlyPayslipAsync(int employeeId, string period, CancellationToken cancellationToken = default);
    Task ApprovePaySlipAsync(int payslipId, CancellationToken cancellationToken = default);
    Task MarkPaySlipAsPaidAsync(int payslipId, string transactionReference, CancellationToken cancellationToken = default);
    Task<string> GetPayslipHtmlPreviewAsync(int payslipId, CancellationToken cancellationToken = default);
    Task<byte[]> ExportPayrollPdfAsync(int payslipId, CancellationToken cancellationToken = default);

    // Chu trình kỳ lương (PayrollRun)
    Task<PayrollRunDto> CalculatePayrollRunAsync(string period, int? unitId = null, CancellationToken cancellationToken = default);
    Task<PayrollRunDto?> GetPayrollRunAsync(string period, int? unitId = null, CancellationToken cancellationToken = default);
    Task SubmitPayrollRunAsync(int runId, int accountantId, CancellationToken cancellationToken = default);
    Task ApprovePayrollRunAsync(int runId, int chiefAccountantId, int hrDirectorId, CancellationToken cancellationToken = default);
    Task LockPayrollRunAsync(int runId, int hrId, CancellationToken cancellationToken = default);
    Task MarkPayrollRunAsPaidAsync(int runId, int financeId, string paymentReference, CancellationToken cancellationToken = default);

    // Cảnh báo bất thường lương (So sánh kỳ trước & Sàn tối thiểu vùng)
    Task<IReadOnlyList<PayrollAnomalyDto>> CheckPayrollAnomaliesAsync(string period, CancellationToken cancellationToken = default);
}
