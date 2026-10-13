#pragma warning disable CS8602
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Application.DTOs.Salaries;
using VinamilkHRM.Application.Interfaces;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Attendances;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Entities.Kpis;
using VinamilkHRM.Domain.Entities.Salaries;
using VinamilkHRM.Domain.Entities.Statutory;
using VinamilkHRM.Domain.Enums;
using VinamilkHRM.Domain.Interfaces.Printing;
using VinamilkHRM.Domain.Interfaces.Repositories;
using VinamilkHRM.Domain.Services.Payroll;

namespace VinamilkHRM.Application.Services;

/// <summary>
/// Động cơ tính lương và quản lý chu trình bảng lương Vinamilk (3P: Position, Person, Performance).
/// Quy trình chuẩn: Draft -> Submitted (Kế toán) -> Approved (Kế toán trưởng & GĐNS) -> Locked -> Paid.
/// RÀNG BUỘC PHÁP LÝ: Bất biến từ Approved trở đi. Bỏ hẳn mặc định 22 công, bắt buộc bảng công đã khóa.
/// </summary>
public class PayrollService : IPayrollService
{
    private readonly IPaySlipRepository _paySlipRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILaborContractRepository _contractRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IKpiAssignmentRepository _kpiRepository;
    private readonly IPayslipPrinter _payslipPrinter;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationDbContext _context;
    private readonly IStatutoryParameterProvider _statutoryProvider;

    public PayrollService(
        IPaySlipRepository paySlipRepository,
        IEmployeeRepository employeeRepository,
        ILaborContractRepository contractRepository,
        IAttendanceRepository attendanceRepository,
        IKpiAssignmentRepository kpiRepository,
        IPayslipPrinter payslipPrinter,
        IUnitOfWork unitOfWork,
        IApplicationDbContext context,
        IStatutoryParameterProvider statutoryProvider)
    {
        _paySlipRepository = paySlipRepository ?? throw new ArgumentNullException(nameof(paySlipRepository));
        _employeeRepository = employeeRepository ?? throw new ArgumentNullException(nameof(employeeRepository));
        _contractRepository = contractRepository ?? throw new ArgumentNullException(nameof(contractRepository));
        _attendanceRepository = attendanceRepository ?? throw new ArgumentNullException(nameof(attendanceRepository));
        _kpiRepository = kpiRepository ?? throw new ArgumentNullException(nameof(kpiRepository));
        _payslipPrinter = payslipPrinter ?? throw new ArgumentNullException(nameof(payslipPrinter));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _statutoryProvider = statutoryProvider ?? throw new ArgumentNullException(nameof(statutoryProvider));
    }

    public async Task<PaySlipDto?> GetPaySlipAsync(int employeeId, string period, CancellationToken cancellationToken = default)
    {
        var payslip = await _paySlipRepository.GetByEmployeeAndPeriodAsync(employeeId, period, cancellationToken);
        if (payslip == null) return null;

        var employee = await _employeeRepository.GetWithDetailsAsync(employeeId, cancellationToken);
        return MapToDto(payslip, employee);
    }

    public async Task<IReadOnlyList<PaySlipDto>> GetPaySlipsByPeriodAsync(string period, CancellationToken cancellationToken = default)
    {
        var list = await _paySlipRepository.GetByPeriodAsync(period, cancellationToken);
        return list.Select(p => MapToDto(p, null)).ToList();
    }

    public async Task<PaySlipDto> GenerateMonthlyPayslipAsync(int employeeId, string period, CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra kỳ lương có bị khóa/duyệt không
        var existingRun = await _context.PayrollRuns
            .FirstOrDefaultAsync(r => r.Period == period, cancellationToken);
        existingRun?.EnsureCanRecalculate();

        var employee = await _employeeRepository.GetWithDetailsAsync(employeeId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy nhân sự có Id={employeeId}.", "ERR_EMP_NOT_FOUND");

        var contract = await _contractRepository.GetActiveContractByEmployeeIdAsync(employeeId, cancellationToken);

        // 2. Kiểm tra Bảng công đã khóa (Không có bảng công đã khóa -> ném ERR_PAY_NO_LOCKED_TIMESHEET)
        var timesheetSummary = await _context.MonthlyTimesheetSummaries
            .FirstOrDefaultAsync(s => s.EmployeeId == employeeId && s.Period == period, cancellationToken);

        var timesheetPeriod = await _context.MonthlyTimesheetPeriods
            .FirstOrDefaultAsync(p => p.Period == period && p.OrganizationalUnitId == employee.OrganizationalUnitId, cancellationToken);

        bool isTimesheetLocked = timesheetPeriod?.Status == TimesheetPeriodStatus.Locked;
        if (timesheetSummary == null || !isTimesheetLocked)
        {
            throw new DomainException("Không có bảng công đã khóa trong kỳ tính lương.", "ERR_PAY_NO_LOCKED_TIMESHEET");
        }

        // 3. Kết quả KPI đã khóa từ Phiên 5
        var kpiResult = await _context.KpiResults
            .FirstOrDefaultAsync(k => k.EmployeeId == employeeId && k.Period == period, cancellationToken);

        // 4. Nạp tham số pháp lý động theo ngày cuối kỳ
        int year = int.Parse(period.Split('-')[0]);
        int month = int.Parse(period.Split('-')[1]);
        var periodEndDate = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

        decimal baseSalary = await _statutoryProvider.GetAsync("BASE_SALARY", periodEndDate, cancellationToken);
        decimal selfDeduction = await _statutoryProvider.GetAsync("PIT_SELF_DEDUCTION", periodEndDate, cancellationToken);
        decimal dependentDeduction = await _statutoryProvider.GetAsync("PIT_DEPENDENT_DEDUCTION", periodEndDate, cancellationToken);

        var region = employee.OrganizationalUnit?.MinimumWageRegion ?? MinimumWageRegion.RegionI;
        var minWage = await _statutoryProvider.GetMinimumWageAsync(region, periodEndDate, cancellationToken);
        var taxBrackets = await _statutoryProvider.GetTaxBracketsAsync("PIT_REGULAR", periodEndDate, cancellationToken);

        // 5. Chuẩn bị PayrollInput
        decimal p1Base = contract?.BasicSalary ?? (employee.BaseSalary > 0 ? employee.BaseSalary : minWage.MonthlyAmount);
        decimal fixedAllowances = employee.Allowance;
        decimal insurableAllowances = employee.Allowance;
        decimal taxExemptAllowances = employee is OfficeEmployee ? 730_000m : 0m;
        decimal attendanceAllowance = employee is FactoryWorker ? 500_000m : 0m;

        var input = new PayrollInput
        {
            EmployeeId = employee.Id,
            EmployeeCode = employee.EmployeeCode,
            FullName = employee.FullName,
            PayrollPeriod = period,
            PeriodEndDate = periodEndDate,
            LeadershipLevel = employee.StandardJobTitle?.LeadershipLevel ?? LeadershipLevel.None,
            Region = region,
            IsUnionMember = true,
            DependentCount = employee.DependentCount,
            TimesheetSummary = timesheetSummary,
            IsTimesheetLocked = true,
            P1BaseSalary = p1Base,
            FixedAllowances = fixedAllowances,
            InsurableAllowances = insurableAllowances,
            TaxExemptAllowances = taxExemptAllowances,
            KpiResult = kpiResult,
            AttendanceAllowance = attendanceAllowance,
            BaseSalary = baseSalary,
            RegionalMinimumWage = minWage.MonthlyAmount,
            SelfDeduction = selfDeduction,
            DependentDeductionPerPerson = dependentDeduction,
            TaxBrackets = taxBrackets
        };

        // 6. Tính toán thuần Domain
        var calcResult = PayrollCalculator.Calculate(input);

        // 7. Cập nhật phiếu lương (Không xóa phiếu cũ gây mất liên kết audit)
        var existingSlip = await _paySlipRepository.GetByEmployeeAndPeriodAsync(employeeId, period, cancellationToken);
        if (existingSlip != null)
        {
            existingSlip.UpdateFromCalculation(calcResult, existingRun?.Id);
            _paySlipRepository.Update(existingSlip);
        }
        else
        {
            existingSlip = new PaySlip(
                employeeId: employee.Id,
                payrollPeriod: period,
                baseSalary: p1Base,
                allowance: fixedAllowances,
                actualWorkDays: timesheetSummary.ActualWorkDays,
                overtimeHours: timesheetSummary.TotalOtHours,
                overtimePay: calcResult.Lines.Where(l => l.Code.StartsWith("OT")).Sum(l => l.Amount),
                kpiBonus: calcResult.Lines.FirstOrDefault(l => l.Code == "P3_HIEU_SUAT")?.Amount ?? 0m,
                otherBonuses: attendanceAllowance,
                insuranceSalary: calcResult.InsurableSalary,
                standardWorkDays: timesheetSummary.StandardWorkDays,
                dependentCount: employee.DependentCount,
                nightShiftHours: timesheetSummary.NightHours,
                nightShiftPay: calcResult.Lines.FirstOrDefault(l => l.Code == "PHU_CAP_CA_DEM")?.Amount ?? 0m,
                hazardousAllowance: 0m,
                salesCommission: 0m,
                lunchAllowance: taxExemptAllowances,
                personalDeduction: selfDeduction,
                dependentDeductionPerPerson: dependentDeduction,
                taxBrackets: taxBrackets);

            existingSlip.UpdateFromCalculation(calcResult, existingRun?.Id);
            await _paySlipRepository.AddAsync(existingSlip, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToDto(existingSlip, employee);
    }

    public async Task<PayrollRunDto> CalculatePayrollRunAsync(string period, int? unitId = null, CancellationToken cancellationToken = default)
    {
        var run = await _context.PayrollRuns
            .Include(r => r.PaySlips)
            .FirstOrDefaultAsync(r => r.Period == period && r.OrganizationalUnitId == unitId, cancellationToken);

        if (run != null)
        {
            run.EnsureCanRecalculate();
        }
        else
        {
            run = new PayrollRun(period, unitId);
            _context.PayrollRuns.Add(run);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Lấy danh sách nhân sự cần tính lương
        var empQuery = _context.Employees
            .Include(e => e.StandardJobTitle)
            .Include(e => e.OrganizationalUnit)
            .Where(e => e.IsActive);

        if (unitId.HasValue)
        {
            empQuery = empQuery.Where(e => e.OrganizationalUnitId == unitId.Value);
        }

        var employees = await empQuery.ToListAsync(cancellationToken);

        int year = int.Parse(period.Split('-')[0]);
        int month = int.Parse(period.Split('-')[1]);
        var periodEndDate = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

        decimal baseSalary = await _statutoryProvider.GetAsync("BASE_SALARY", periodEndDate, cancellationToken);
        decimal selfDeduction = await _statutoryProvider.GetAsync("PIT_SELF_DEDUCTION", periodEndDate, cancellationToken);
        decimal dependentDeduction = await _statutoryProvider.GetAsync("PIT_DEPENDENT_DEDUCTION", periodEndDate, cancellationToken);
        var taxBrackets = await _statutoryProvider.GetTaxBracketsAsync("PIT_REGULAR", periodEndDate, cancellationToken);

        var timesheets = await _context.MonthlyTimesheetSummaries
            .Where(s => s.Period == period)
            .ToDictionaryAsync(s => s.EmployeeId, cancellationToken);

        var lockedPeriods = await _context.MonthlyTimesheetPeriods
            .Where(p => p.Period == period && p.Status == TimesheetPeriodStatus.Locked)
            .Select(p => p.OrganizationalUnitId)
            .ToHashSetAsync(cancellationToken);

        var kpiResults = await _context.KpiResults
            .Where(k => k.Period == period)
            .ToDictionaryAsync(k => k.EmployeeId, cancellationToken);

        decimal totalGross = 0m;
        decimal totalNet = 0m;
        decimal totalEmployerCost = 0m;
        decimal totalTax = 0m;
        decimal totalEmpInsurance = 0m;
        decimal totalErInsurance = 0m;
        int processedCount = 0;

        foreach (var emp in employees)
        {
            if (!timesheets.TryGetValue(emp.Id, out var ts) || !lockedPeriods.Contains(emp.OrganizationalUnitId))
            {
                continue; // Bỏ qua nếu chưa có bảng công đã khóa
            }

            kpiResults.TryGetValue(emp.Id, out var kpi);
            var region = emp.OrganizationalUnit?.MinimumWageRegion ?? MinimumWageRegion.RegionI;
            var minWage = await _statutoryProvider.GetMinimumWageAsync(region, periodEndDate, cancellationToken);

            var input = new PayrollInput
            {
                EmployeeId = emp.Id,
                EmployeeCode = emp.EmployeeCode,
                FullName = emp.FullName,
                PayrollPeriod = period,
                PeriodEndDate = periodEndDate,
                LeadershipLevel = emp.StandardJobTitle?.LeadershipLevel ?? LeadershipLevel.None,
                Region = region,
                IsUnionMember = true,
                DependentCount = emp.DependentCount,
                TimesheetSummary = ts,
                IsTimesheetLocked = true,
                P1BaseSalary = emp.BaseSalary > 0 ? emp.BaseSalary : minWage.MonthlyAmount,
                FixedAllowances = emp.Allowance,
                InsurableAllowances = emp.Allowance,
                KpiResult = kpi,
                BaseSalary = baseSalary,
                RegionalMinimumWage = minWage.MonthlyAmount,
                SelfDeduction = selfDeduction,
                DependentDeductionPerPerson = dependentDeduction,
                TaxBrackets = taxBrackets
            };

            var calcResult = PayrollCalculator.Calculate(input);

            var slip = await _context.PaySlips.FirstOrDefaultAsync(p => p.EmployeeId == emp.Id && p.PayrollPeriod == period, cancellationToken);
            if (slip != null)
            {
                slip.UpdateFromCalculation(calcResult, run.Id);
            }
            else
            {
                slip = PaySlip.CreateFromCalculation(calcResult, run.Id);
                _context.PaySlips.Add(slip);
            }

            totalGross += calcResult.GrossSalary;
            totalNet += calcResult.NetSalary;
            totalEmployerCost += calcResult.TotalEmployerCost;
            totalTax += calcResult.PersonalIncomeTax;
            totalEmpInsurance += calcResult.TotalEmployeeInsurance;
            totalErInsurance += calcResult.TotalEmployerInsurance;
            processedCount++;
        }

        run.UpdateTotals(totalGross, totalNet, totalEmployerCost, totalTax, totalEmpInsurance, totalErInsurance, processedCount);
        await _context.SaveChangesAsync(cancellationToken);

        return MapRunToDto(run);
    }

    public async Task<PayrollRunDto?> GetPayrollRunAsync(string period, int? unitId = null, CancellationToken cancellationToken = default)
    {
        var run = await _context.PayrollRuns
            .Include(r => r.OrganizationalUnit)
            .FirstOrDefaultAsync(r => r.Period == period && r.OrganizationalUnitId == unitId, cancellationToken);

        return run == null ? null : MapRunToDto(run);
    }

    public async Task SubmitPayrollRunAsync(int runId, int accountantId, CancellationToken cancellationToken = default)
    {
        var run = await _context.PayrollRuns.FindAsync(new object[] { runId }, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy đợt tính lương Id={runId}.", "ERR_PAY_RUN_NOT_FOUND");

        run.Submit(accountantId);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ApprovePayrollRunAsync(int runId, int chiefAccountantId, int hrDirectorId, CancellationToken cancellationToken = default)
    {
        var run = await _context.PayrollRuns.FindAsync(new object[] { runId }, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy đợt tính lương Id={runId}.", "ERR_PAY_RUN_NOT_FOUND");

        run.Approve(chiefAccountantId, hrDirectorId);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task LockPayrollRunAsync(int runId, int hrId, CancellationToken cancellationToken = default)
    {
        var run = await _context.PayrollRuns.FindAsync(new object[] { runId }, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy đợt tính lương Id={runId}.", "ERR_PAY_RUN_NOT_FOUND");

        run.Lock(hrId);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkPayrollRunAsPaidAsync(int runId, int financeId, string paymentReference, CancellationToken cancellationToken = default)
    {
        var run = await _context.PayrollRuns
            .Include(r => r.PaySlips)
            .FirstOrDefaultAsync(r => r.Id == runId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy đợt tính lương Id={runId}.", "ERR_PAY_RUN_NOT_FOUND");

        run.MarkAsPaid(financeId, paymentReference);

        foreach (var slip in run.PaySlips)
        {
            if (slip.Status == PaySlipStatus.Approved)
            {
                slip.MarkAsPaid(paymentReference);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PayrollAnomalyDto>> CheckPayrollAnomaliesAsync(string period, CancellationToken cancellationToken = default)
    {
        var currentSlips = await _context.PaySlips
            .Include(p => p.Employee)
                .ThenInclude(e => e.OrganizationalUnit)
            .Where(p => p.PayrollPeriod == period)
            .ToListAsync(cancellationToken);

        int year = int.Parse(period.Split('-')[0]);
        int month = int.Parse(period.Split('-')[1]);
        var prevDate = new DateTime(year, month, 1).AddMonths(-1);
        string prevPeriod = $"{prevDate.Year:D4}-{prevDate.Month:D2}";

        var prevSlips = await _context.PaySlips
            .Where(p => p.PayrollPeriod == prevPeriod)
            .ToDictionaryAsync(p => p.EmployeeId, cancellationToken);

        var dateOnly = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
        var anomalies = new List<PayrollAnomalyDto>();

        foreach (var slip in currentSlips)
        {
            var emp = slip.Employee;
            if (emp == null) continue;

            var region = emp.OrganizationalUnit?.MinimumWageRegion ?? MinimumWageRegion.RegionI;
            var minWage = await _statutoryProvider.GetMinimumWageAsync(region, dateOnly, cancellationToken);

            // Cảnh báo 1: Gross < Lương tối thiểu vùng
            if (slip.GrossSalary < minWage.MonthlyAmount)
            {
                anomalies.Add(new PayrollAnomalyDto(
                    emp.Id,
                    emp.EmployeeCode,
                    emp.FullName,
                    "GROSS_BELOW_MIN_WAGE",
                    slip.GrossSalary,
                    minWage.MonthlyAmount,
                    Math.Round((slip.GrossSalary - minWage.MonthlyAmount) / minWage.MonthlyAmount * 100m, 1),
                    $"Thu nhập Gross ({slip.GrossSalary:N0} đ) thấp hơn Lương tối thiểu vùng ({minWage.MonthlyAmount:N0} đ)."));
            }

            // Cảnh báo 2: Net chênh lệch > 20% so với kỳ trước
            if (prevSlips.TryGetValue(emp.Id, out var prevSlip) && prevSlip.NetSalary > 0)
            {
                decimal diff = Math.Abs(slip.NetSalary - prevSlip.NetSalary);
                decimal diffPercent = (diff / prevSlip.NetSalary) * 100m;

                if (diffPercent > 20m)
                {
                    anomalies.Add(new PayrollAnomalyDto(
                        emp.Id,
                        emp.EmployeeCode,
                        emp.FullName,
                        "NET_VARIATION_OVER_20",
                        slip.NetSalary,
                        prevSlip.NetSalary,
                        Math.Round(diffPercent, 1),
                        $"Thực lĩnh Net ({slip.NetSalary:N0} đ) biến động {diffPercent:F1}% so với kỳ trước ({prevSlip.NetSalary:N0} đ)."));
                }
            }
        }

        return anomalies;
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

    public async Task<string> GetPayslipHtmlPreviewAsync(int payslipId, CancellationToken cancellationToken = default)
    {
        var payslip = await _paySlipRepository.GetByIdAsync(payslipId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy phiếu lương Id={payslipId}.", "ERR_PAY_NOT_FOUND");

        var employee = await _employeeRepository.GetWithDetailsAsync(payslip.EmployeeId, cancellationToken)
            ?? throw new DomainException("Không tìm thấy thông tin nhân sự của phiếu lương.", "ERR_EMP_NOT_FOUND");

        return _payslipPrinter.GenerateHtmlPreview(payslip, employee, employee.OrganizationalUnit!);
    }

    public async Task<byte[]> ExportPayrollPdfAsync(int payslipId, CancellationToken cancellationToken = default)
    {
        var payslip = await _paySlipRepository.GetByIdAsync(payslipId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy phiếu lương Id={payslipId}.", "ERR_PAY_NOT_FOUND");

        var employee = await _employeeRepository.GetWithDetailsAsync(payslip.EmployeeId, cancellationToken)
            ?? throw new DomainException("Không tìm thấy thông tin nhân viên.", "ERR_EMP_NOT_FOUND");

        return _payslipPrinter.PrintPayslipToPdf(payslip, employee, employee.OrganizationalUnit!);
    }

    private static PaySlipDto MapToDto(PaySlip p, Employee? emp) => new(
        p.Id,
        p.EmployeeId,
        emp?.EmployeeCode ?? string.Empty,
        emp?.FullName ?? string.Empty,
        emp?.OrganizationalUnit?.UnitName ?? string.Empty,
        emp?.EmployeeType.ToString() ?? "Office",
        p.PayrollPeriod,
        p.StandardWorkDays,
        p.ActualWorkDays,
        p.BaseSalary,
        p.Allowance,
        p.LunchAllowance,
        p.OvertimeHours,
        p.OvertimePay,
        p.NightShiftHours,
        p.NightShiftPay,
        p.HazardousAllowance,
        p.SalesCommission,
        p.KpiBonus,
        p.OtherBonuses,
        p.GrossSalary,
        p.SocialInsuranceDeduction,
        p.HealthInsuranceDeduction,
        p.UnemploymentInsuranceDeduction,
        p.DependentCount,
        p.PersonalDeduction,
        p.DependentDeduction,
        p.TaxableIncome,
        p.PersonalIncomeTax,
        p.TotalDeductions,
        p.NetSalary,
        p.Status,
        p.PaymentDate,
        p.TransactionReference);

    private static PayrollRunDto MapRunToDto(PayrollRun r) => new(
        r.Id,
        r.Period,
        r.OrganizationalUnitId,
        r.OrganizationalUnit?.UnitName,
        r.Status,
        r.TotalGross,
        r.TotalNet,
        r.TotalEmployerCost,
        r.TotalPersonalIncomeTax,
        r.TotalEmployeeInsurance,
        r.TotalEmployerInsurance,
        r.EmployeeCount,
        r.SubmittedAt,
        r.ApprovedAccountantAt,
        r.ApprovedDirectorAt,
        r.LockedAt,
        r.PaidAt,
        r.PaymentReference);
}

