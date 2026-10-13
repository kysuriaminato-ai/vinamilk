using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Domain.Entities.Salaries;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.Jobs;

/// <summary>
/// Các Background Jobs chạy tự động hàng tháng / sự kiện cho phân hệ Total Rewards (Phiên 7):
/// 1. StepIncrease: Lập danh sách nâng bậc lương định kỳ theo thâm niên.
/// 2. MinimumWage: Quét và tạo đề xuất điều chỉnh khi có mức lương tối thiểu vùng mới.
/// 3. MarketAdjustment: Tạo đề xuất điều chỉnh theo JobFamily khi có khảo sát lương thị trường.
/// </summary>
public class SalaryReviewBackgroundJobs
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<SalaryReviewBackgroundJobs> _logger;

    public SalaryReviewBackgroundJobs(
        IApplicationDbContext dbContext,
        ILogger<SalaryReviewBackgroundJobs> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Job hàng tháng: Quét danh sách nhân viên đủ thâm niên đến hạn nâng bậc (StepIncrease).
    /// </summary>
    public async Task ScanStepIncreasesMonthlyAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Bắt đầu job định kỳ quét nâng bậc lương theo thâm niên (StepIncrease)...");

        var employees = await _dbContext.Employees
            .Include(e => e.JobGrade)
            .Where(e => e.IsActive && e.JobGradeId.HasValue)
            .ToListAsync(cancellationToken);

        var salaryScales = await _dbContext.SalaryScales.AsNoTracking().ToListAsync(cancellationToken);

        int count = 0;
        foreach (var emp in employees)
        {
            // Kiểm tra thâm niên >= 24 tháng hoặc theo quy chế
            var monthsWorked = (DateTime.UtcNow.Year - emp.HireDate.Year) * 12 + DateTime.UtcNow.Month - emp.HireDate.Month;
            if (monthsWorked >= 24)
            {
                int nextStep = emp.SalaryStep + 1;
                var nextScale = salaryScales.FirstOrDefault(s => s.JobGradeId == emp.JobGradeId && s.Step == nextStep);

                if (nextScale != null && nextScale.Amount > emp.BaseSalary)
                {
                    var proposal = new SalaryProposal(
                        employeeId: emp.Id,
                        proposedById: emp.ManagerId ?? 1,
                        currentSalary: emp.BaseSalary,
                        proposedSalary: nextScale.Amount,
                        currentAllowance: emp.Allowance,
                        proposedAllowance: emp.Allowance,
                        reason: $"Nâng bậc lương định kỳ theo thâm niên (Bậc {emp.SalaryStep} -> Bậc {nextStep})",
                        effectiveDate: DateTime.UtcNow,
                        adjustmentType: SalaryAdjustmentType.StepIncrease,
                        fundSource: SalaryFundSource.CycleBudget,
                        organizationalUnitId: emp.OrganizationalUnitId);

                    proposal.ConfigureRoute(requiresBoardOfDirectors: false);
                    await _dbContext.SalaryProposals.AddAsync(proposal, cancellationToken);
                    count++;
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Hoàn thành quét nâng bậc thâm niên: Đã tạo {Count} đề xuất mới.", count);
    }

    /// <summary>
    /// Quét toàn bộ nhân viên có lương vị trí P1 thấp hơn lương tối thiểu vùng mới và tạo đề xuất hàng loạt.
    /// </summary>
    public async Task ScanMinimumWageAdjustmentsAsync(int regionId, decimal newMinWage, DateOnly effectiveDate, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Bắt đầu quét điều chỉnh lương sàn tối thiểu vùng {Region}: Mức mới {MinWage:N0} đ...", regionId, newMinWage);

        var employees = await _dbContext.Employees
            .Include(e => e.OrganizationalUnit)
            .Where(e => e.IsActive && e.BaseSalary < newMinWage)
            .ToListAsync(cancellationToken);

        int count = 0;
        foreach (var emp in employees)
        {
            var proposal = new SalaryProposal(
                employeeId: emp.Id,
                proposedById: 1, // Hệ thống tự động
                currentSalary: emp.BaseSalary,
                proposedSalary: newMinWage,
                currentAllowance: emp.Allowance,
                proposedAllowance: emp.Allowance,
                reason: $"Điều chỉnh theo Nghị định Lương tối thiểu vùng mới (Mức sàn {newMinWage:N0} đ)",
                effectiveDate: effectiveDate.ToDateTime(TimeOnly.MinValue),
                adjustmentType: SalaryAdjustmentType.MinimumWage,
                fundSource: SalaryFundSource.CycleBudget,
                organizationalUnitId: emp.OrganizationalUnitId);

            proposal.ConfigureRoute(requiresBoardOfDirectors: false);
            await _dbContext.SalaryProposals.AddAsync(proposal, cancellationToken);
            count++;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Hoàn thành điều chỉnh lương tối thiểu vùng: Đã tạo {Count} đề xuất hàng loạt.", count);
    }

    /// <summary>
    /// Tạo đề xuất điều chỉnh lương thị trường theo JobFamily.
    /// </summary>
    public async Task ScanMarketAdjustmentsAsync(int jobFamilyId, decimal percentage, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Bắt đầu điều chỉnh lương thị trường theo JobFamily {FamilyId}: Tỷ lệ {Pct}%...", jobFamilyId, percentage * 100);

        var employees = await _dbContext.Employees
            .Where(e => e.IsActive)
            .ToListAsync(cancellationToken);

        int count = 0;
        foreach (var emp in employees)
        {
            decimal newSalary = Math.Round(emp.BaseSalary * (1m + percentage), 0, MidpointRounding.AwayFromZero);
            var proposal = new SalaryProposal(
                employeeId: emp.Id,
                proposedById: 1,
                currentSalary: emp.BaseSalary,
                proposedSalary: newSalary,
                currentAllowance: emp.Allowance,
                proposedAllowance: emp.Allowance,
                reason: $"Điều chỉnh lương trượt giá thị trường JobFamily {jobFamilyId} (+{percentage * 100}%)",
                effectiveDate: DateTime.UtcNow,
                adjustmentType: SalaryAdjustmentType.MarketAdjustment,
                fundSource: SalaryFundSource.CycleBudget,
                organizationalUnitId: emp.OrganizationalUnitId);

            proposal.ConfigureRoute(requiresBoardOfDirectors: false);
            await _dbContext.SalaryProposals.AddAsync(proposal, cancellationToken);
            count++;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Hoàn thành điều chỉnh lương thị trường: Đã tạo {Count} đề xuất.", count);
    }
}