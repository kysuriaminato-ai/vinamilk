using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Analytics;
using VinamilkHRM.Domain.Entities.Assets;
using VinamilkHRM.Domain.Entities.OnboardingOffboarding;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.OnboardingOffboarding;

public interface IOnboardingOffboardingService
{
    Task<List<EmployeeChecklistTask>> InitiateOnboardingAsync(int employeeId, DateTime startDate, CancellationToken ct = default);
    Task<List<EmployeeChecklistTask>> InitiateOffboardingAsync(
        int employeeId,
        DateOnly lastWorkingDate,
        DateOnly officialTerminationDate,
        ExitReasonCategory exitReason,
        string? detailedReason = null,
        CancellationToken ct = default);

    Task ReturnAssetAsync(int employeeId, int assetId, string returnConditionNotes, CancellationToken ct = default);
    Task<FinalSettlement> CalculateFinalSettlementAsync(
        int employeeId,
        DateOnly lastWorkingDate,
        DateOnly officialTerminationDate,
        decimal daysWorkedInMonth,
        decimal unusedLeaveDays,
        decimal deductions = 0m,
        decimal severance = 0m,
        CancellationToken ct = default);

    Task CompleteTaskAsync(int taskId, int completedByUserId, string? notes = null, CancellationToken ct = default);
}

public class OnboardingOffboardingService : IOnboardingOffboardingService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<OnboardingOffboardingService> _logger;

    public OnboardingOffboardingService(
        IApplicationDbContext context,
        ILogger<OnboardingOffboardingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<EmployeeChecklistTask>> InitiateOnboardingAsync(int employeeId, DateTime startDate, CancellationToken ct = default)
    {
        var employee = await _context.Employees.FindAsync(new object[] { employeeId }, ct);
        if (employee == null)
            throw new DomainException($"Không tìm thấy nhân viên ID {employeeId}.", "ERR_ONBOARD_EMP_NOT_FOUND");

        var tasks = new List<EmployeeChecklistTask>
        {
            new EmployeeChecklistTask(employeeId, ChecklistType.Onboarding, "Ký kết Hợp đồng Thử việc / HĐLĐ", "HR chuẩn bị và ký kết hợp đồng", "HR", startDate.AddDays(0)),
            new EmployeeChecklistTask(employeeId, ChecklistType.Onboarding, "Cấp phát máy tính và tài khoản Vinamilk", "IT cấu hình laptop, email @vinamilk.com.vn và tài khoản ERP", "IT", startDate.AddDays(0)),
            new EmployeeChecklistTask(employeeId, ChecklistType.Onboarding, "Bàn giao thẻ ra vào và đồng phục", "Hành chính cấp thẻ từ và trang thiết bị", "Admin", startDate.AddDays(1)),
            new EmployeeChecklistTask(employeeId, ChecklistType.Onboarding, "Đào tạo Hội nhập văn hóa Vinamilk", "Tham gia khóa học văn hóa doanh nghiệp và quy tắc ứng xử", "HR", startDate.AddDays(3)),
            new EmployeeChecklistTask(employeeId, ChecklistType.Onboarding, "Đăng ký tăng mới BHXH (Mẫu D02-LT)", "HR khai trình tăng lao động lên cơ quan BHXH", "HR", startDate.AddDays(7))
        };

        _context.EmployeeChecklistTasks.AddRange(tasks);

        // Tự động sinh bản ghi Báo tăng BHXH (Mẫu D02-LT)
        string declNumber = $"D02-IN-{employeeId}-{startDate:yyyyMM}";
        var siDecl = new SocialInsuranceDeclaration(declNumber, employeeId, SocialInsuranceDeclarationType.IncreaseNew, startDate.ToString("yyyy-MM"), employee.BaseSalary);
        _context.SocialInsuranceDeclarations.Add(siDecl);

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Đã khởi tạo quy trình Onboarding và báo tăng BHXH cho nhân viên {EmpId}.", employeeId);

        return tasks;
    }

    public async Task<List<EmployeeChecklistTask>> InitiateOffboardingAsync(
        int employeeId,
        DateOnly lastWorkingDate,
        DateOnly officialTerminationDate,
        ExitReasonCategory exitReason,
        string? detailedReason = null,
        CancellationToken ct = default)
    {
        var employee = await _context.Employees.FindAsync(new object[] { employeeId }, ct);
        if (employee == null)
            throw new DomainException($"Không tìm thấy nhân viên ID {employeeId}.", "ERR_OFFBOARD_EMP_NOT_FOUND");

        DateTime lastDate = lastWorkingDate.ToDateTime(TimeOnly.MinValue);

        var tasks = new List<EmployeeChecklistTask>
        {
            new EmployeeChecklistTask(employeeId, ChecklistType.Offboarding, "Bàn giao công việc và tài liệu chuyên môn", "Quản lý trực tiếp nghiệm thu bàn giao", "DirectManager", lastDate.AddDays(-2)),
            new EmployeeChecklistTask(employeeId, ChecklistType.Offboarding, "Thu hồi tài sản và thiết bị (CompanyAsset)", "IT và Hành chính thu hồi laptop, thẻ, đồng phục", "IT", lastDate.AddDays(-1)),
            new EmployeeChecklistTask(employeeId, ChecklistType.Offboarding, "Khảo sát thôi việc (Exit Survey)", "Nhân viên điền biểu mẫu đánh giá lý do nghỉ", "Employee", lastDate.AddDays(0)),
            new EmployeeChecklistTask(employeeId, ChecklistType.Offboarding, "Báo giảm BHXH và chốt sổ BHXH", "HR thực hiện báo giảm theo Mẫu D02-LT", "HR", lastDate.AddDays(3)),
            new EmployeeChecklistTask(employeeId, ChecklistType.Offboarding, "Quyết toán cuối cùng (Final Settlement)", "Kế toán tính lương ngày làm, phép tồn và chi trả", "Finance", lastDate.AddDays(5))
        };

        _context.EmployeeChecklistTasks.AddRange(tasks);

        // Tự động khởi tạo Exit Survey (cung cấp dữ liệu cho People Analytics Phiên 8)
        var exitSurvey = new ExitSurvey(
            employeeId,
            lastWorkingDate,
            exitReason,
            detailedReason,
            compensationSatisfaction: 3,
            managementSatisfaction: 3,
            workEnvironmentSatisfaction: 4,
            careerGrowthSatisfaction: 3,
            wouldRecommendCompany: true,
            improvementSuggestions: "Đề xuất tối ưu hóa quy trình luân chuyển.");
        _context.ExitSurveys.Add(exitSurvey);

        // Tự động sinh bản ghi Báo giảm BHXH (Mẫu D02-LT)
        string declNumber = $"D02-OUT-{employeeId}-{lastWorkingDate:yyyyMM}";
        var siDecl = new SocialInsuranceDeclaration(declNumber, employeeId, SocialInsuranceDeclarationType.DecreaseExit, lastWorkingDate.ToString("yyyy-MM"), employee.BaseSalary);
        _context.SocialInsuranceDeclarations.Add(siDecl);

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Đã khởi tạo quy trình Offboarding, Exit Survey và báo giảm BHXH cho nhân viên {EmpId}.", employeeId);

        return tasks;
    }

    public async Task ReturnAssetAsync(int employeeId, int assetId, string returnConditionNotes, CancellationToken ct = default)
    {
        var asset = await _context.CompanyAssets
            .FirstOrDefaultAsync(a => a.Id == assetId && a.AssignedToEmployeeId == employeeId, ct);

        if (asset == null)
            throw new DomainException($"Không tìm thấy tài sản ID {assetId} đang giao cho nhân viên {employeeId}.", "ERR_ASSET_NOT_FOUND_FOR_EMP");

        asset.ReturnAsset(returnConditionNotes);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Đã thu hồi tài sản {AssetCode} từ nhân viên {EmpId}.", asset.AssetCode, employeeId);
    }

    public async Task<FinalSettlement> CalculateFinalSettlementAsync(
        int employeeId,
        DateOnly lastWorkingDate,
        DateOnly officialTerminationDate,
        decimal daysWorkedInMonth,
        decimal unusedLeaveDays,
        decimal deductions = 0m,
        decimal severance = 0m,
        CancellationToken ct = default)
    {
        var employee = await _context.Employees.FindAsync(new object[] { employeeId }, ct);
        if (employee == null)
            throw new DomainException($"Không tìm thấy nhân viên ID {employeeId}.", "ERR_SETTLEMENT_EMP_NOT_FOUND");

        // Lương 1 ngày công chuẩn theo quy chế (26 ngày công chuẩn)
        decimal dailySalaryRate = Math.Round(employee.BaseSalary / 26m, 0, MidpointRounding.AwayFromZero);

        var settlement = new FinalSettlement(
            employeeId,
            lastWorkingDate,
            officialTerminationDate,
            daysWorkedInMonth,
            dailySalaryRate,
            unusedLeaveDays,
            severanceAllowance: severance,
            otherAllowances: 0m,
            deductions: deductions);

        _context.FinalSettlements.Add(settlement);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Đã tính quyết toán cuối cho nhân viên {EmpId}: Thực nhận = {Amount:N0} VNĐ.", 
            employeeId, settlement.NetSettlementAmount);

        return settlement;
    }

    public async Task CompleteTaskAsync(int taskId, int completedByUserId, string? notes = null, CancellationToken ct = default)
    {
        var task = await _context.EmployeeChecklistTasks.FindAsync(new object[] { taskId }, ct);
        if (task == null)
            throw new DomainException($"Không tìm thấy nhiệm vụ ID {taskId}.", "ERR_TASK_NOT_FOUND");

        task.Complete(completedByUserId, notes, DateTimeOffset.UtcNow);
        await _context.SaveChangesAsync(ct);
    }
}