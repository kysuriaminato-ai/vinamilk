using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Application.Interfaces;

namespace VinamilkHRM.Application.Jobs;

/// <summary>
/// Các tác vụ chạy nền định kỳ và tính toán song song hàng loạt cho 10.000 nhân viên Vinamilk (Hangfire).
/// Phân mảnh (Partition) theo Đơn vị tổ chức (Nhà máy, Trang trại, Chi nhánh, Cửa hàng) để chạy song song.
/// </summary>
public class PayrollBackgroundJobs
{
    private readonly IApplicationDbContext _context;
    private readonly IPayrollService _payrollService;
    private readonly ILogger<PayrollBackgroundJobs> _logger;

    public PayrollBackgroundJobs(
        IApplicationDbContext context,
        IPayrollService payrollService,
        ILogger<PayrollBackgroundJobs> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _payrollService = payrollService ?? throw new ArgumentNullException(nameof(payrollService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Job tính toán lương định kỳ hàng loạt cho toàn bộ 10.000 cán bộ nhân viên Vinamilk.
    /// Chia theo đơn vị và thực thi song song, theo dõi tiến độ chi tiết.
    /// </summary>
    public async Task RunMonthlyPayrollBatchCalculationAsync(string period, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[PAYROLL-JOB] Bắt đầu tính lương hàng loạt kỳ {Period} cho toàn bộ nhân sự Vinamilk.", period);

        var units = await _context.OrganizationalUnits
            .AsNoTracking()
            .Select(u => new { u.Id, u.UnitName, u.UnitCode })
            .ToListAsync(cancellationToken);

        int totalUnits = units.Count;
        int completedUnits = 0;
        int totalEmployeesCalculated = 0;

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(2, Environment.ProcessorCount),
            CancellationToken = cancellationToken
        };

        var progressBag = new ConcurrentBag<(string UnitCode, int Count)>();

        await Parallel.ForEachAsync(units, parallelOptions, async (unit, ct) =>
        {
            try
            {
                var runDto = await _payrollService.CalculatePayrollRunAsync(period, unit.Id, ct);
                progressBag.Add((unit.UnitCode, runDto.EmployeeCount));
                Interlocked.Increment(ref completedUnits);
                Interlocked.Add(ref totalEmployeesCalculated, runDto.EmployeeCount);

                int percent = (int)((completedUnits / (double)totalUnits) * 100);
                _logger.LogInformation(
                    "[TIẾN ĐỘ TÍNH LƯƠNG {Percent}%] Đơn vị {UnitCode} ({UnitName}) đã tính xong {Count} nhân sự. ({Done}/{Total} đơn vị)",
                    percent, unit.UnitCode, unit.UnitName, runDto.EmployeeCount, completedUnits, totalUnits);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PAYROLL-JOB-ERROR] Lỗi khi tính lương đơn vị {UnitCode} ({UnitName})", unit.UnitCode, unit.UnitName);
            }
        });

        _logger.LogInformation(
            "[PAYROLL-JOB-SUCCESS] Hoàn tất tính lương kỳ {Period}: Tổng cộng {EmpCount} nhân sự trên {UnitCount} đơn vị.",
            period, totalEmployeesCalculated, completedUnits);
    }
}
