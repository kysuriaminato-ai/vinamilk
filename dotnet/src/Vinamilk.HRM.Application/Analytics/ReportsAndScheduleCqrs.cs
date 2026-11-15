using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Vinamilk.HRM.Application.Common.Interfaces;

namespace Vinamilk.HRM.Application.Analytics;

#region BM01 - BM04 DTOs & Queries

public sealed record Bm01EmployeeRowDto(
    string EmployeeCode,
    string FullName,
    string Gender,
    DateOnly DateOfBirth,
    string UnitName,
    string PositionName,
    string EducationLevel,
    DateOnly HireDate,
    string WorkStatus,
    string ContractType,
    decimal BaseSalary);

public sealed record GetBm01EmployeeMasterReportQuery(int? UnitId = null, string? WorkStatus = null)
    : IRequest<List<Bm01EmployeeRowDto>>;

public class GetBm01EmployeeMasterReportQueryHandler : IRequestHandler<GetBm01EmployeeMasterReportQuery, List<Bm01EmployeeRowDto>>
{
    private readonly IApplicationDbContext _context;
    public GetBm01EmployeeMasterReportQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<List<Bm01EmployeeRowDto>> Handle(GetBm01EmployeeMasterReportQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Employees
            .AsNoTracking()
            .Include(e => e.OrganizationUnit)
            .Include(e => e.Position)
            .Include(e => e.EducationLevel)
            .AsQueryable();

        if (request.UnitId.HasValue && request.UnitId > 0)
            query = query.Where(e => e.OrganizationUnitId == request.UnitId.Value);

        if (!string.IsNullOrWhiteSpace(request.WorkStatus))
            query = query.Where(e => e.WorkStatus == request.WorkStatus);

        return await query
            .OrderBy(e => e.OrganizationUnit.UnitName)
            .ThenBy(e => e.EmployeeCode)
            .Select(e => new Bm01EmployeeRowDto(
                e.EmployeeCode,
                e.FullName,
                e.Gender,
                e.DateOfBirth,
                e.OrganizationUnit.UnitName,
                e.Position.PositionName,
                e.EducationLevel != null ? e.EducationLevel.LevelName : "Chưa cập nhật",
                e.HireDate,
                e.WorkStatus,
                e.CurrentContractType,
                e.BaseSalary))
            .ToListAsync(cancellationToken);
    }
}

public sealed record Bm02MovementRowDto(
    string DecisionNumber,
    string EmployeeCode,
    string FullName,
    string TransferType,
    string FromUnitName,
    string ToUnitName,
    string FromPositionName,
    string ToPositionName,
    DateOnly EffectiveDate,
    string ApprovalStatus);

public sealed record GetBm02MovementReportQuery(int? Year = null, int? Month = null, int? UnitId = null)
    : IRequest<List<Bm02MovementRowDto>>;

public class GetBm02MovementReportQueryHandler : IRequestHandler<GetBm02MovementReportQuery, List<Bm02MovementRowDto>>
{
    private readonly IApplicationDbContext _context;
    public GetBm02MovementReportQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<List<Bm02MovementRowDto>> Handle(GetBm02MovementReportQuery request, CancellationToken cancellationToken)
    {
        var query = _context.PersonnelTransfers
            .AsNoTracking()
            .Include(t => t.Employee)
            .Include(t => t.FromUnit)
            .Include(t => t.ToUnit)
            .Include(t => t.FromPosition)
            .Include(t => t.ToPosition)
            .AsQueryable();

        if (request.Year.HasValue && request.Year > 0)
            query = query.Where(t => t.EffectiveDate.Year == request.Year.Value);

        if (request.Month.HasValue && request.Month > 0)
            query = query.Where(t => t.EffectiveDate.Month == request.Month.Value);

        if (request.UnitId.HasValue && request.UnitId > 0)
            query = query.Where(t => t.FromUnitId == request.UnitId.Value || t.ToUnitId == request.UnitId.Value);

        return await query
            .OrderByDescending(t => t.EffectiveDate)
            .Select(t => new Bm02MovementRowDto(
                t.DecisionNumber,
                t.Employee.EmployeeCode,
                t.Employee.FullName,
                t.TransferType,
                t.FromUnit.UnitName,
                t.ToUnit.UnitName,
                t.FromPosition.PositionName,
                t.ToPosition.PositionName,
                t.EffectiveDate,
                t.Status))
            .ToListAsync(cancellationToken);
    }
}

public sealed record Bm03RewardDisciplineRowDto(
    string DecisionNumber,
    string EmployeeCode,
    string FullName,
    string UnitName,
    string RecordType,
    string CategoryName,
    decimal Amount,
    DateOnly EffectiveDate,
    string Reason,
    string ApprovalStatus);

public sealed record GetBm03RewardDisciplineReportQuery(int? Year = null, string? RecordType = null)
    : IRequest<List<Bm03RewardDisciplineRowDto>>;

public class GetBm03RewardDisciplineReportQueryHandler : IRequestHandler<GetBm03RewardDisciplineReportQuery, List<Bm03RewardDisciplineRowDto>>
{
    private readonly IApplicationDbContext _context;
    public GetBm03RewardDisciplineReportQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<List<Bm03RewardDisciplineRowDto>> Handle(GetBm03RewardDisciplineReportQuery request, CancellationToken cancellationToken)
    {
        var query = _context.RewardDisciplines
            .AsNoTracking()
            .Include(r => r.Employee).ThenInclude(e => e.OrganizationUnit)
            .Include(r => r.Category)
            .AsQueryable();

        if (request.Year.HasValue && request.Year > 0)
            query = query.Where(r => r.EffectiveDate.Year == request.Year.Value);

        if (!string.IsNullOrWhiteSpace(request.RecordType))
            query = query.Where(r => r.Type == request.RecordType);

        return await query
            .OrderByDescending(r => r.EffectiveDate)
            .Select(r => new Bm03RewardDisciplineRowDto(
                r.DecisionNumber,
                r.Employee.EmployeeCode,
                r.Employee.FullName,
                r.Employee.OrganizationUnit.UnitName,
                r.Type,
                r.Category.CategoryName,
                r.Amount,
                r.EffectiveDate,
                r.Reason ?? string.Empty,
                r.Status))
            .ToListAsync(cancellationToken);
    }
}

public sealed record Bm04PayrollSummaryRowDto(
    string UnitCode,
    string UnitName,
    int Headcount,
    decimal TotalGross,
    decimal TotalEmployeeSi,
    decimal TotalCompanySi,
    decimal TotalPit,
    decimal TotalNet);

public sealed record GetBm04PayrollSummaryReportQuery(int? Year = null, int? Month = null)
    : IRequest<List<Bm04PayrollSummaryRowDto>>;

public class GetBm04PayrollSummaryReportQueryHandler : IRequestHandler<GetBm04PayrollSummaryReportQuery, List<Bm04PayrollSummaryRowDto>>
{
    private readonly IApplicationDbContext _context;
    public GetBm04PayrollSummaryReportQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<List<Bm04PayrollSummaryRowDto>> Handle(GetBm04PayrollSummaryReportQuery request, CancellationToken cancellationToken)
    {
        var year = request.Year ?? DateTime.Today.Year;
        var month = request.Month ?? DateTime.Today.Month;

        var period = await _context.PayrollPeriods
            .AsNoTracking()
            .Where(p => p.Year == year && p.Month == month)
            .FirstOrDefaultAsync(cancellationToken);

        period ??= await _context.PayrollPeriods
            .AsNoTracking()
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Month)
            .FirstOrDefaultAsync(cancellationToken);

        if (period is null)
            return new List<Bm04PayrollSummaryRowDto>();

        var rows = await _context.PayrollRecords
            .AsNoTracking()
            .Where(r => r.PayrollPeriodId == period.Id)
            .Include(r => r.OrganizationUnit)
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => new { r.OrganizationUnit.UnitCode, r.OrganizationUnit.UnitName })
            .Select(g => new Bm04PayrollSummaryRowDto(
                g.Key.UnitCode,
                g.Key.UnitName,
                g.Count(),
                g.Sum(x => x.GrossIncome),
                g.Sum(x => x.TotalEmployeeInsurance),
                g.Sum(x => x.CompanySocialInsurance + x.CompanyHealthInsurance + x.CompanyUnemploymentInsurance),
                g.Sum(x => x.PersonalIncomeTax),
                g.Sum(x => x.NetSalary)))
            .OrderByDescending(x => x.TotalGross)
            .ToList();
    }
}

#endregion

#region Dynamic Report Builder

public sealed record DynamicReportColumnOption(string Key, string Label);

public sealed record DynamicReportGroupSummaryDto(string GroupKey, int Count, decimal TotalBaseSalary);

public sealed record DynamicReportResultDto(
    List<DynamicReportColumnOption> Columns,
    List<Dictionary<string, string>> Rows,
    List<DynamicReportGroupSummaryDto> Groups,
    string GroupBy);

public sealed record RunDynamicReportQuery(
    List<string>? SelectedColumns = null,
    int? UnitId = null,
    string? WorkStatus = null,
    string GroupBy = "None") : IRequest<DynamicReportResultDto>;

public static class DynamicReportCatalog
{
    public static readonly List<DynamicReportColumnOption> AllColumns =
    [
        new("EmployeeCode", "Mã NV"),
        new("FullName", "Họ và tên"),
        new("Gender", "Giới tính"),
        new("DateOfBirth", "Ngày sinh"),
        new("UnitName", "Đơn vị"),
        new("PositionName", "Chức danh"),
        new("EducationLevel", "Trình độ"),
        new("WorkStatus", "Trạng thái"),
        new("ContractType", "Loại HĐ"),
        new("HireDate", "Ngày vào làm"),
        new("BaseSalary", "Lương cơ bản"),
        new("TotalAllowance", "Tổng phụ cấp"),
        new("WorkEmail", "Email công việc"),
        new("PhoneNumber", "Số điện thoại")
    ];
}

public class RunDynamicReportQueryHandler : IRequestHandler<RunDynamicReportQuery, DynamicReportResultDto>
{
    private readonly IApplicationDbContext _context;
    public RunDynamicReportQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<DynamicReportResultDto> Handle(RunDynamicReportQuery request, CancellationToken cancellationToken)
    {
        var selectedKeys = request.SelectedColumns is { Count: > 0 }
            ? request.SelectedColumns.Distinct().ToList()
            : ["EmployeeCode", "FullName", "UnitName", "PositionName", "WorkStatus", "BaseSalary"];

        var activeColumns = DynamicReportCatalog.AllColumns
            .Where(c => selectedKeys.Contains(c.Key, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (activeColumns.Count == 0)
            activeColumns = DynamicReportCatalog.AllColumns.Take(6).ToList();

        var query = _context.Employees
            .AsNoTracking()
            .Include(e => e.OrganizationUnit)
            .Include(e => e.Position)
            .Include(e => e.EducationLevel)
            .AsQueryable();

        if (request.UnitId.HasValue && request.UnitId > 0)
            query = query.Where(e => e.OrganizationUnitId == request.UnitId.Value);

        if (!string.IsNullOrWhiteSpace(request.WorkStatus))
            query = query.Where(e => e.WorkStatus == request.WorkStatus);

        var employees = await query
            .OrderBy(e => e.OrganizationUnit.UnitName)
            .ThenBy(e => e.EmployeeCode)
            .ToListAsync(cancellationToken);

        var rows = new List<Dictionary<string, string>>(employees.Count);
        foreach (var e in employees)
        {
            var dict = new Dictionary<string, string>();
            foreach (var col in activeColumns)
            {
                dict[col.Key] = col.Key switch
                {
                    "EmployeeCode" => e.EmployeeCode,
                    "FullName" => e.FullName,
                    "Gender" => e.Gender,
                    "DateOfBirth" => e.DateOfBirth.ToString("dd/MM/yyyy"),
                    "UnitName" => e.OrganizationUnit.UnitName,
                    "PositionName" => e.Position.PositionName,
                    "EducationLevel" => e.EducationLevel?.LevelName ?? "Chưa cập nhật",
                    "WorkStatus" => e.WorkStatus,
                    "ContractType" => e.CurrentContractType,
                    "HireDate" => e.HireDate.ToString("dd/MM/yyyy"),
                    "BaseSalary" => e.BaseSalary.ToString("N0"),
                    "TotalAllowance" => (e.PositionAllowance + e.OtherAllowance).ToString("N0"),
                    "WorkEmail" => e.WorkEmail ?? string.Empty,
                    "PhoneNumber" => e.PhoneNumber ?? string.Empty,
                    _ => string.Empty
                };
            }
            rows.Add(dict);
        }

        var groupBy = string.IsNullOrWhiteSpace(request.GroupBy) ? "None" : request.GroupBy;
        var groups = groupBy == "None"
            ? new List<DynamicReportGroupSummaryDto>()
            : employees
                .GroupBy(e => groupBy switch
                {
                    "UnitName" => e.OrganizationUnit.UnitName,
                    "PositionName" => e.Position.PositionName,
                    "WorkStatus" => e.WorkStatus,
                    "Gender" => e.Gender,
                    "EducationLevel" => e.EducationLevel?.LevelName ?? "Chưa cập nhật",
                    _ => "Tất cả"
                })
                .Select(g => new DynamicReportGroupSummaryDto(g.Key, g.Count(), g.Sum(x => x.BaseSalary)))
                .OrderByDescending(g => g.Count)
                .ToList();

        return new DynamicReportResultDto(activeColumns, rows, groups, groupBy);
    }
}

#endregion

#region Scheduled Report Email Delivery

public sealed record ReportScheduleConfigDto(
    string Id,
    string ReportCode,
    string ReportName,
    string Frequency,
    string Recipients,
    bool IsActive,
    DateTime? LastSentAt,
    string? LastStatus);

public sealed record GetReportSchedulesQuery : IRequest<List<ReportScheduleConfigDto>>;

public sealed record SaveReportScheduleCommand(
    string ReportCode,
    string Frequency,
    string Recipients,
    bool IsActive = true) : IRequest<ReportScheduleConfigDto>;

public sealed record SendScheduledReportCommand(string ScheduleId, string BaseUrl = "") : IRequest<string>;

internal static class ReportScheduleStore
{
    private const string CacheKey = "vinamilk_report_schedules_v1";

    public static List<ReportScheduleConfigDto> GetAll(IMemoryCache cache)
    {
        return cache.GetOrCreate(CacheKey, entry =>
        {
            entry.Priority = CacheItemPriority.NeverRemove;
            return new List<ReportScheduleConfigDto>
            {
                new("SCH-BM01", "BM01", "BM01 - Danh sách nhân sự toàn công ty", "Monthly", "hr-director@vinamilk.com.vn; ceo@vinamilk.com.vn", true, null, "Chưa gửi"),
                new("SCH-BM04", "BM04", "BM04 - Tổng hợp Quỹ lương & BHXH", "Monthly", "cfo@vinamilk.com.vn; payroll@vinamilk.com.vn", true, null, "Chưa gửi")
            };
        })!;
    }
}

public class GetReportSchedulesQueryHandler : IRequestHandler<GetReportSchedulesQuery, List<ReportScheduleConfigDto>>
{
    private readonly IMemoryCache _cache;
    public GetReportSchedulesQueryHandler(IMemoryCache cache) => _cache = cache;

    public Task<List<ReportScheduleConfigDto>> Handle(GetReportSchedulesQuery request, CancellationToken cancellationToken)
        => Task.FromResult(ReportScheduleStore.GetAll(_cache).ToList());
}

public class SaveReportScheduleCommandHandler : IRequestHandler<SaveReportScheduleCommand, ReportScheduleConfigDto>
{
    private readonly IMemoryCache _cache;
    public SaveReportScheduleCommandHandler(IMemoryCache cache) => _cache = cache;

    public Task<ReportScheduleConfigDto> Handle(SaveReportScheduleCommand request, CancellationToken cancellationToken)
    {
        var list = ReportScheduleStore.GetAll(_cache);
        var id = $"SCH-{request.ReportCode.ToUpperInvariant()}";
        var name = request.ReportCode.ToUpperInvariant() switch
        {
            "BM01" => "BM01 - Danh sách nhân sự toàn công ty",
            "BM02" => "BM02 - Biến động nhân sự & Điều chuyển",
            "BM03" => "BM03 - Khen thưởng & Kỷ luật",
            "BM04" => "BM04 - Tổng hợp Quỹ lương & BHXH",
            _ => $"{request.ReportCode} - Báo cáo tùy biến"
        };

        var existingIndex = list.FindIndex(x => x.Id == id);
        var item = new ReportScheduleConfigDto(
            id,
            request.ReportCode.ToUpperInvariant(),
            name,
            string.IsNullOrWhiteSpace(request.Frequency) ? "Monthly" : request.Frequency,
            request.Recipients.Trim(),
            request.IsActive,
            existingIndex >= 0 ? list[existingIndex].LastSentAt : null,
            existingIndex >= 0 ? list[existingIndex].LastStatus : "Đã cấu hình");

        if (existingIndex >= 0) list[existingIndex] = item;
        else list.Add(item);

        return Task.FromResult(item);
    }
}

public class SendScheduledReportCommandHandler : IRequestHandler<SendScheduledReportCommand, string>
{
    private readonly IMemoryCache _cache;
    private readonly IEmailNotificationService _emailService;
    private readonly IApplicationDbContext _context;

    public SendScheduledReportCommandHandler(
        IMemoryCache cache,
        IEmailNotificationService emailService,
        IApplicationDbContext context)
    {
        _cache = cache;
        _emailService = emailService;
        _context = context;
    }

    public async Task<string> Handle(SendScheduledReportCommand request, CancellationToken cancellationToken)
    {
        var list = ReportScheduleStore.GetAll(_cache);
        var idx = list.FindIndex(x => x.Id == request.ScheduleId);
        if (idx < 0)
            return "Không tìm thấy lịch gửi báo cáo.";

        var schedule = list[idx];
        var recipients = schedule.Recipients
            .Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var totalEmployees = await _context.Employees.CountAsync(e => e.WorkStatus != "Terminated", cancellationToken);
        var summaryComment = $"Báo cáo định kỳ ({schedule.Frequency}) cho {totalEmployees} nhân sự đang làm việc.";

        foreach (var email in recipients)
        {
            await _emailService.SendWorkflowEmailAsync(
                toEmail: email,
                recipientName: email,
                subject: $"[Vinamilk HRM] Gửi tự động {schedule.ReportName}",
                documentTitle: schedule.ReportName,
                initiatorName: "Hệ thống Báo cáo Tự động (Hangfire/Scheduler)",
                currentStepName: $"Chu kỳ: {schedule.Frequency}",
                statusBadge: "Đã phát hành",
                actionUrl: string.IsNullOrWhiteSpace(request.BaseUrl) ? "/Reports" : $"{request.BaseUrl}/Reports",
                comment: summaryComment,
                cancellationToken: cancellationToken);
        }

        list[idx] = schedule with
        {
            LastSentAt = DateTime.Now,
            LastStatus = $"Đã gửi ({recipients.Length} người nhận)"
        };

        return $"Đã gửi báo cáo {schedule.ReportCode} tới {recipients.Length} địa chỉ email.";
    }
}

#endregion
