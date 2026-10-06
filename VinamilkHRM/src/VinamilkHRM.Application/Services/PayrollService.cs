using VinamilkHRM.Application.DTOs.Salaries;
using VinamilkHRM.Application.Interfaces;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Entities.Salaries;
using VinamilkHRM.Domain.Interfaces.Printing;
using VinamilkHRM.Domain.Interfaces.Repositories;

namespace VinamilkHRM.Application.Services;

public class PayrollService : IPayrollService
{
    private readonly IPaySlipRepository _paySlipRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILaborContractRepository _contractRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IKpiAssignmentRepository _kpiRepository;
    private readonly IPayslipPrinter _payslipPrinter;
    private readonly IUnitOfWork _unitOfWork;

    public PayrollService(
        IPaySlipRepository paySlipRepository,
        IEmployeeRepository employeeRepository,
        ILaborContractRepository contractRepository,
        IAttendanceRepository attendanceRepository,
        IKpiAssignmentRepository kpiRepository,
        IPayslipPrinter payslipPrinter,
        IUnitOfWork unitOfWork)
    {
        _paySlipRepository = paySlipRepository ?? throw new ArgumentNullException(nameof(paySlipRepository));
        _employeeRepository = employeeRepository ?? throw new ArgumentNullException(nameof(employeeRepository));
        _contractRepository = contractRepository ?? throw new ArgumentNullException(nameof(contractRepository));
        _attendanceRepository = attendanceRepository ?? throw new ArgumentNullException(nameof(attendanceRepository));
        _kpiRepository = kpiRepository ?? throw new ArgumentNullException(nameof(kpiRepository));
        _payslipPrinter = payslipPrinter ?? throw new ArgumentNullException(nameof(payslipPrinter));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<PaySlipDto?> GetPaySlipAsync(int employeeId, string period, CancellationToken cancellationToken = default)
    {
        var payslip = await _paySlipRepository.GetByEmployeeAndPeriodAsync(employeeId, period, cancellationToken);
        return payslip == null ? null : MapToDto(payslip);
    }

    public async Task<IReadOnlyList<PaySlipDto>> GetPaySlipsByPeriodAsync(string period, CancellationToken cancellationToken = default)
    {
        var list = await _paySlipRepository.GetByPeriodAsync(period, cancellationToken);
        return list.Select(p => MapToDto(p, null)).ToList();
    }

    public async Task<PaySlipDto> GenerateMonthlyPayslipAsync(int employeeId, string period, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy nhân sự có Id={employeeId}.", "ERR_EMP_NOT_FOUND");

        var contract = await _contractRepository.GetActiveContractByEmployeeIdAsync(employeeId, cancellationToken);
        decimal insuranceSalary = contract?.InsuranceSalary ?? (employee.BaseSalary * 0.85m);

        // Giả lập chu kỳ tính công trong tháng
        int year = int.Parse(period.Split('-')[0]);
        int month = int.Parse(period.Split('-')[1]);
        DateTime startDate = new DateTime(year, month, 1);
        DateTime endDate = startDate.AddMonths(1).AddDays(-1);

        decimal totalWorkHours = await _attendanceRepository.GetTotalWorkHoursAsync(employeeId, startDate, endDate, cancellationToken);
        decimal totalOvertimeHours = await _attendanceRepository.GetTotalOvertimeHoursAsync(employeeId, startDate, endDate, cancellationToken);
        decimal actualWorkDays = Math.Round(totalWorkHours / 8m, 1);
        if (actualWorkDays <= 0) actualWorkDays = 22m; // Mặc định đủ công chuẩn nếu chưa có quẹt thẻ chi tiết

        // Sử dụng Đa hình: employee.CalculateOvertimePay
        decimal overtimePay = employee.CalculateOvertimePay(totalOvertimeHours, isHoliday: false);

        // Tính điểm KPI và tiền thưởng hiệu suất
        decimal avgKpiScore = await _kpiRepository.CalculateAverageKpiScoreAsync(employeeId, period, cancellationToken);
        decimal kpiBonus = avgKpiScore >= 80m ? (employee.BaseSalary * (avgKpiScore / 100m) * 0.15m) : 0m;

        var existing = await _paySlipRepository.GetByEmployeeAndPeriodAsync(employeeId, period, cancellationToken);
        if (existing != null)
        {
            _paySlipRepository.Remove(existing);
        }

        var payslip = new PaySlip(
            employeeId: employee.Id,
            payrollPeriod: period,
            baseSalary: employee.BaseSalary,
            allowance: employee.Allowance,
            actualWorkDays: actualWorkDays,
            overtimeHours: totalOvertimeHours,
            overtimePay: overtimePay,
            kpiBonus: kpiBonus,
            otherBonuses: 500_000m, // Hỗ trợ phúc lợi Vinamilk (sữa chua, sữa tươi)
            insuranceSalary: insuranceSalary,
            standardWorkDays: 22m);

        await _paySlipRepository.AddAsync(payslip, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(payslip, employee);
    }

    public async Task ApprovePaySlipAsync(int payslipId, CancellationToken cancellationToken = default)
    {
        var payslip = await _paySlipRepository.GetByIdAsync(payslipId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy phiếu lương Id={payslipId}.", "ERR_PAY_NOT_FOUND");

        payslip.Approve();
        _paySlipRepository.Update(payslip);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkPaySlipAsPaidAsync(int payslipId, string transactionReference, CancellationToken cancellationToken = default)
    {
        var payslip = await _paySlipRepository.GetByIdAsync(payslipId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy phiếu lương Id={payslipId}.", "ERR_PAY_NOT_FOUND");

        payslip.MarkAsPaid(transactionReference);
        _paySlipRepository.Update(payslip);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<byte[]> ExportPayrollPdfAsync(int payslipId, CancellationToken cancellationToken = default)
    {
        var payslip = await _paySlipRepository.GetByIdAsync(payslipId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy phiếu lương Id={payslipId}.", "ERR_PAY_NOT_FOUND");

        var employee = await _employeeRepository.GetWithDetailsAsync(payslip.EmployeeId, cancellationToken)
            ?? throw new DomainException("Không tìm thấy thông tin nhân viên.", "ERR_EMP_NOT_FOUND");

        return _payslipPrinter.PrintPayslipToPdf(payslip, employee, employee.OrganizationalUnit!);
    }

    private static PaySlipDto MapToDto(PaySlip p, Employee? emp = null)
    {
        var employee = emp ?? p.Employee;
        return new PaySlipDto(
            p.Id,
            p.EmployeeId,
            employee?.EmployeeCode ?? "N/A",
            employee?.FullName ?? "N/A",
            employee?.OrganizationalUnit?.UnitName ?? "Vinamilk",
            p.PayrollPeriod,
            p.StandardWorkDays,
            p.ActualWorkDays,
            p.BaseSalary,
            p.Allowance,
            p.OvertimeHours,
            p.OvertimePay,
            p.KpiBonus,
            p.OtherBonuses,
            p.GrossSalary,
            p.SocialInsuranceDeduction,
            p.HealthInsuranceDeduction,
            p.UnemploymentInsuranceDeduction,
            p.PersonalIncomeTax,
            p.TotalDeductions,
            p.NetSalary,
            p.Status,
            p.PaymentDate,
            p.TransactionReference);
    }
}
