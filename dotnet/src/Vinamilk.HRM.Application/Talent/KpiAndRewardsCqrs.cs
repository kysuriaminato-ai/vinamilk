using MediatR;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Application.Workflows;
using Vinamilk.HRM.Domain.Entities.Core;
using Vinamilk.HRM.Domain.Entities.Talent;

namespace Vinamilk.HRM.Application.Talent;

// ============================================================================
// 1. BỘ TÍNH ĐIỂM KPI/OKR, XẾP LOẠI A/B/C/D & HỆ SỐ THƯỞNG LIÊN KẾT LƯƠNG
// ============================================================================

public static class KpiGradingCalculator
{
    public static (string RatingGrade, decimal BonusMultiplier, string GradeLabelVi) EvaluateGrade(decimal finalScore)
    {
        if (finalScore >= 90m)
            return ("A_Excellent", 1.50m, "A - Xuất sắc (150% Quỹ thưởng KPI)");
        if (finalScore >= 75m)
            return ("B_Good", 1.20m, "B - Tốt (120% Quỹ thưởng KPI)");
        if (finalScore >= 60m)
            return ("C_Average", 1.00m, "C - Đạt yêu cầu (100% Quỹ thưởng KPI)");
        return ("D_Poor", 0.50m, "D - Cần cải thiện (50% Quỹ thưởng KPI)");
    }
}

public record EvaluationCycleDto(
    int Id,
    string CycleCode,
    string CycleName,
    string CycleType,
    int Year,
    int PeriodNumber,
    DateOnly StartDate,
    DateOnly EndDate,
    DateOnly SelfEvalDeadline,
    DateOnly ManagerEvalDeadline,
    string Status,
    int EvaluationsCount);

public record KpiItemDto(
    int Id,
    string GoalTitle,
    string Perspective,
    string MeasureUnit,
    decimal WeightPercent,
    decimal TargetValue,
    decimal ActualValue,
    decimal CompletionRatePercent,
    decimal SelfScore,
    decimal ManagerScore,
    string? EvidenceNote);

public record KpiEvaluationDto(
    int Id,
    int EvaluationCycleId,
    string CycleName,
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string UnitName,
    string PositionName,
    int? ReviewerEmployeeId,
    string ReviewerName,
    decimal TotalWeight,
    decimal SelfTotalScore,
    decimal ManagerTotalScore,
    decimal FinalScore,
    string RatingGrade,
    string GradeLabelVi,
    decimal BonusMultiplier,
    decimal LinkedPayrollBonusEstimate,
    string? EmployeeComment,
    string? ReviewerComment,
    string Status,
    DateTime? ApprovedAt,
    IReadOnlyList<KpiItemDto> Items);

public record KpiOverviewDto(
    IReadOnlyList<EvaluationCycleDto> Cycles,
    IReadOnlyList<KpiEvaluationDto> Evaluations,
    IReadOnlyList<SimpleLookupDto> ActiveEmployees,
    int SelectedCycleId,
    int GradeACount,
    int GradeBCount,
    int GradeCDCount,
    decimal AverageFinalScore);

// ============================================================================
// 2. DTOs CHO KHEN THƯỞNG / KỶ LUẬT & BÁO CÁO BM03
// ============================================================================

public record RewardDisciplineCategoryDto(
    int Id,
    string CategoryCode,
    string CategoryName,
    string CategoryType,
    decimal DefaultAmount);

public record RewardDisciplineItemDto(
    int Id,
    string DecisionNumber,
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string UnitName,
    string PositionName,
    int CategoryId,
    string CategoryName,
    string Type,
    DateOnly DecisionDate,
    DateOnly EffectiveDate,
    decimal Amount,
    string? Reason,
    string Status);

public record Bm03UnitSummaryDto(
    string UnitCode,
    string UnitName,
    int RewardCount,
    decimal TotalRewardAmount,
    int DisciplineCount,
    decimal TotalDisciplineAmount);

public record RewardsAndBm03OverviewDto(
    int Year,
    IReadOnlyList<RewardDisciplineCategoryDto> Categories,
    IReadOnlyList<RewardDisciplineItemDto> Items,
    IReadOnlyList<Bm03UnitSummaryDto> Bm03UnitSummaries,
    IReadOnlyList<SimpleLookupDto> ActiveEmployees,
    int TotalRewardCount,
    decimal TotalRewardAmount,
    int TotalDisciplineCount,
    decimal TotalDisciplineAmount);

// ============================================================================
// 3. AUTO-SEED CHU KỲ KPI/OKR & PHIẾU ĐÁNH GIÁ MẪU
// ============================================================================

public static class KpiSeeder
{
    public static async Task EnsureSeededAsync(IApplicationDbContext context, CancellationToken cancellationToken)
    {
        if (await context.EvaluationCycles.AnyAsync(cancellationToken))
            return;

        int year = DateTime.Today.Year;
        var cycle = new EvaluationCycle
        {
            CycleCode = $"KPI-{year}-Q3",
            CycleName = $"Đánh giá KPI & OKR Quý 3/{year}",
            CycleType = "Quarterly",
            Year = year,
            PeriodNumber = 3,
            StartDate = new DateOnly(year, 7, 1),
            EndDate = new DateOnly(year, 9, 30),
            SelfEvalDeadline = new DateOnly(year, 10, 5),
            ManagerEvalDeadline = new DateOnly(year, 10, 15),
            Status = "Open"
        };

        await context.EvaluationCycles.AddAsync(cycle, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var emps = await context.Employees.AsNoTracking().OrderBy(e => e.EmployeeCode).Take(4).ToListAsync(cancellationToken);
        if (emps.Count >= 3)
        {
            var eval1 = new EmployeeKpiEvaluation
            {
                EvaluationCycleId = cycle.Id,
                EmployeeId = emps[1].Id,
                ReviewerEmployeeId = emps[0].Id,
                TotalWeight = 100m,
                SelfTotalScore = 91.5m,
                ManagerTotalScore = 90.0m,
                FinalScore = 90.5m,
                RatingGrade = "A_Excellent",
                BonusMultiplier = 1.50m,
                EmployeeComment = "Hoàn thành vượt mức sản lượng đóng gói sữa tươi & giảm tỷ lệ hao hụt xuống 0.3%.",
                ReviewerComment = "Đánh giá 360° từ Quản lý & Đồng nghiệp: Tinh thần trách nhiệm xuất sắc, phối hợp nhịp nhàng giữa 3 ca.",
                Status = "Approved",
                ApprovedAt = DateTime.UtcNow.AddDays(-5),
                Items = new List<EmployeeKpiItem>
                {
                    new() { GoalTitle = "Đạt sản lượng dây chuyền UHT (triệu lít/quý)", Perspective = "InternalProcess", MeasureUnit = "Triệu lít", WeightPercent = 40m, TargetValue = 120m, ActualValue = 128m, CompletionRatePercent = 106.7m, SelfScore = 95m, ManagerScore = 94m },
                    new() { GoalTitle = "Tỷ lệ sản phẩm đạt chuẩn FSSC 22000 ngay lần đầu", Perspective = "Customer", MeasureUnit = "%", WeightPercent = 35m, TargetValue = 99.5m, ActualValue = 99.8m, CompletionRatePercent = 100.3m, SelfScore = 90m, ManagerScore = 88m },
                    new() { GoalTitle = "Hoàn thành khóa đào tạo Lean Six Sigma & Đào tạo kèm cặp nhân sự mới", Perspective = "LearningGrowth", MeasureUnit = "Giờ", WeightPercent = 25m, TargetValue = 24m, ActualValue = 24m, CompletionRatePercent = 100m, SelfScore = 88m, ManagerScore = 87m }
                }
            };

            var eval2 = new EmployeeKpiEvaluation
            {
                EvaluationCycleId = cycle.Id,
                EmployeeId = emps[2].Id,
                ReviewerEmployeeId = emps[0].Id,
                TotalWeight = 100m,
                SelfTotalScore = 82.0m,
                ManagerTotalScore = 0m,
                FinalScore = 82.0m,
                RatingGrade = "B_Good",
                BonusMultiplier = 1.20m,
                EmployeeComment = "Đã hoàn tất tự đánh giá KPI Quý 3, đang chờ Quản lý trực tiếp chấm điểm & hiệu chỉnh.",
                Status = "ManagerEval",
                Items = new List<EmployeeKpiItem>
                {
                    new() { GoalTitle = "Tối ưu chi phí vận hành & bảo trì thiết bị", Perspective = "Financial", MeasureUnit = "%", WeightPercent = 50m, TargetValue = 100m, ActualValue = 96m, CompletionRatePercent = 96m, SelfScore = 84m, ManagerScore = 0m },
                    new() { GoalTitle = "Đảm bảo không xảy ra sự cố dừng chuyền ngoài kế hoạch", Perspective = "InternalProcess", MeasureUnit = "Sự cố", WeightPercent = 50m, TargetValue = 0m, ActualValue = 0m, CompletionRatePercent = 100m, SelfScore = 80m, ManagerScore = 0m }
                }
            };

            await context.EmployeeKpiEvaluations.AddRangeAsync(new[] { eval1, eval2 }, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}

// ============================================================================
// 4. QUERIES & COMMANDS CHO KPI / OKR
// ============================================================================

public record GetKpiOverviewQuery(int? CycleId = null) : IRequest<KpiOverviewDto>;

public class GetKpiOverviewQueryHandler : IRequestHandler<GetKpiOverviewQuery, KpiOverviewDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService? _currentUser;

    public GetKpiOverviewQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService? currentUser = null)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<KpiOverviewDto> Handle(GetKpiOverviewQuery request, CancellationToken cancellationToken)
    {
        await KpiSeeder.EnsureSeededAsync(_context, cancellationToken);
        var scope = await Common.Security.DataScopeResolver.ResolveAsync(_context, _currentUser, cancellationToken);

        var activeEmployeesQuery = _context.Employees.AsNoTracking()
            .Where(e => e.WorkStatus == "Official" || e.WorkStatus == "Probation");

        if (!scope.IsUnrestricted)
        {
            var allowedUnitIds = scope.AllowedUnitIdList;
            var selfEmpId = scope.CurrentEmployeeId;
            activeEmployeesQuery = activeEmployeesQuery.Where(e =>
                allowedUnitIds.Contains(e.OrganizationUnitId) ||
                (selfEmpId.HasValue && e.Id == selfEmpId.Value));
        }

        var activeEmployees = await activeEmployeesQuery
            .OrderBy(e => e.EmployeeCode)
            .Select(e => new SimpleLookupDto(e.Id, e.EmployeeCode, $"{e.EmployeeCode} - {e.FullName}"))
            .ToListAsync(cancellationToken);

        var empNameMap = activeEmployees.ToDictionary(e => e.Id, e => e.Name);

        var cycles = await _context.EvaluationCycles.AsNoTracking()
            .Include(c => c.Evaluations)
            .OrderByDescending(c => c.Year).ThenByDescending(c => c.PeriodNumber)
            .Select(c => new EvaluationCycleDto(
                c.Id,
                c.CycleCode,
                c.CycleName,
                c.CycleType,
                c.Year,
                c.PeriodNumber,
                c.StartDate,
                c.EndDate,
                c.SelfEvalDeadline,
                c.ManagerEvalDeadline,
                c.Status,
                c.Evaluations.Count))
            .ToListAsync(cancellationToken);

        int selectedCycleId = request.CycleId ?? cycles.First().Id;

        var evalsQuery = _context.EmployeeKpiEvaluations.AsNoTracking()
            .Include(e => e.EvaluationCycle)
            .Include(e => e.Employee).ThenInclude(emp => emp.OrganizationUnit)
            .Include(e => e.Employee).ThenInclude(emp => emp.Position)
            .Include(e => e.Items)
            .Where(e => e.EvaluationCycleId == selectedCycleId);

        if (!scope.IsUnrestricted)
        {
            var allowedUnitIds = scope.AllowedUnitIdList;
            var selfEmpId = scope.CurrentEmployeeId;
            evalsQuery = evalsQuery.Where(e =>
                allowedUnitIds.Contains(e.Employee.OrganizationUnitId) ||
                (selfEmpId.HasValue && e.EmployeeId == selfEmpId.Value));
        }

        var evalsRaw = await evalsQuery
            .OrderByDescending(e => e.FinalScore)
            .ToListAsync(cancellationToken);

        var evaluations = evalsRaw.Select(e =>
        {
            var (_, _, gradeLabel) = KpiGradingCalculator.EvaluateGrade(e.FinalScore);
            var estimatedBonus = Math.Round(e.Employee.BaseSalary * 0.25m * e.BonusMultiplier, 0);

            return new KpiEvaluationDto(
                e.Id,
                e.EvaluationCycleId,
                e.EvaluationCycle.CycleName,
                e.EmployeeId,
                e.Employee.EmployeeCode,
                e.Employee.FullName,
                e.Employee.OrganizationUnit.UnitName,
                e.Employee.Position.PositionName,
                e.ReviewerEmployeeId,
                e.ReviewerEmployeeId.HasValue && empNameMap.TryGetValue(e.ReviewerEmployeeId.Value, out var rn) ? rn : "Quản lý trực tiếp",
                e.TotalWeight,
                e.SelfTotalScore,
                e.ManagerTotalScore,
                e.FinalScore,
                e.RatingGrade,
                gradeLabel,
                e.BonusMultiplier,
                estimatedBonus,
                e.EmployeeComment,
                e.ReviewerComment,
                e.Status,
                e.ApprovedAt,
                e.Items.Select(i => new KpiItemDto(
                    i.Id,
                    i.GoalTitle,
                    i.Perspective,
                    i.MeasureUnit,
                    i.WeightPercent,
                    i.TargetValue,
                    i.ActualValue,
                    i.CompletionRatePercent,
                    i.SelfScore,
                    i.ManagerScore,
                    i.EvidenceNote)).ToList());
        }).ToList();

        return new KpiOverviewDto(
            cycles,
            evaluations,
            activeEmployees,
            selectedCycleId,
            evaluations.Count(x => x.RatingGrade == "A_Excellent"),
            evaluations.Count(x => x.RatingGrade == "B_Good"),
            evaluations.Count(x => x.RatingGrade is "C_Average" or "D_Poor"),
            evaluations.Count > 0 ? Math.Round(evaluations.Average(x => x.FinalScore), 1) : 0m);
    }
}

public record CreateEvaluationCycleCommand(
    string CycleCode,
    string CycleName,
    string CycleType,
    int Year,
    int PeriodNumber,
    DateOnly StartDate,
    DateOnly EndDate) : IRequest<int>;

public class CreateEvaluationCycleCommandHandler : IRequestHandler<CreateEvaluationCycleCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateEvaluationCycleCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateEvaluationCycleCommand request, CancellationToken cancellationToken)
    {
        var cycle = new EvaluationCycle
        {
            CycleCode = request.CycleCode.Trim().ToUpperInvariant(),
            CycleName = request.CycleName.Trim(),
            CycleType = request.CycleType,
            Year = request.Year,
            PeriodNumber = request.PeriodNumber,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            SelfEvalDeadline = request.EndDate.AddDays(5),
            ManagerEvalDeadline = request.EndDate.AddDays(15),
            Status = "Open"
        };
        await _context.EvaluationCycles.AddAsync(cycle, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return cycle.Id;
    }
}

public record CreateEmployeeKpiCommand(
    int EvaluationCycleId,
    int EmployeeId,
    int? ReviewerEmployeeId,
    string GoalTitle,
    string Perspective,
    string MeasureUnit,
    decimal TargetValue,
    decimal ActualValue,
    decimal SelfScore,
    string? EmployeeComment) : IRequest<int>;

public class CreateEmployeeKpiCommandHandler : IRequestHandler<CreateEmployeeKpiCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateEmployeeKpiCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateEmployeeKpiCommand request, CancellationToken cancellationToken)
    {
        var compRate = request.TargetValue > 0 ? Math.Round(request.ActualValue / request.TargetValue * 100m, 1) : 100m;
        var (grade, mult, _) = KpiGradingCalculator.EvaluateGrade(request.SelfScore);

        var eval = new EmployeeKpiEvaluation
        {
            EvaluationCycleId = request.EvaluationCycleId,
            EmployeeId = request.EmployeeId,
            ReviewerEmployeeId = request.ReviewerEmployeeId,
            TotalWeight = 100m,
            SelfTotalScore = request.SelfScore,
            ManagerTotalScore = 0m,
            FinalScore = request.SelfScore,
            RatingGrade = grade,
            BonusMultiplier = mult,
            EmployeeComment = request.EmployeeComment?.Trim(),
            Status = "ManagerEval",
            Items = new List<EmployeeKpiItem>
            {
                new()
                {
                    GoalTitle = request.GoalTitle.Trim(),
                    Perspective = request.Perspective,
                    MeasureUnit = request.MeasureUnit,
                    WeightPercent = 100m,
                    TargetValue = request.TargetValue,
                    ActualValue = request.ActualValue,
                    CompletionRatePercent = compRate,
                    SelfScore = request.SelfScore,
                    ManagerScore = 0m
                }
            }
        };

        await _context.EmployeeKpiEvaluations.AddAsync(eval, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return eval.Id;
    }
}

public record CalibrateAndApproveKpiCommand(
    int EvaluationId,
    decimal ManagerScore,
    decimal? CalibratedFinalScore,
    string? ReviewerAnd360Comment) : IRequest<string>;

public class CalibrateAndApproveKpiCommandHandler : IRequestHandler<CalibrateAndApproveKpiCommand, string>
{
    private readonly IApplicationDbContext _context;

    public CalibrateAndApproveKpiCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string> Handle(CalibrateAndApproveKpiCommand request, CancellationToken cancellationToken)
    {
        var eval = await _context.EmployeeKpiEvaluations
            .Include(e => e.Items)
            .FirstOrDefaultAsync(e => e.Id == request.EvaluationId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy phiếu đánh giá KPI.");

        eval.ManagerTotalScore = Math.Clamp(request.ManagerScore, 0m, 100m);
        var finalScore = request.CalibratedFinalScore ?? Math.Round((eval.SelfTotalScore * 0.3m) + (eval.ManagerTotalScore * 0.7m), 1);
        eval.FinalScore = Math.Clamp(finalScore, 0m, 100m);

        var (grade, mult, label) = KpiGradingCalculator.EvaluateGrade(eval.FinalScore);
        eval.RatingGrade = grade;
        eval.BonusMultiplier = mult;
        eval.ReviewerComment = request.ReviewerAnd360Comment?.Trim();
        eval.Status = "Approved";
        eval.ApprovedAt = DateTime.UtcNow;

        foreach (var item in eval.Items)
        {
            if (item.ManagerScore <= 0m)
                item.ManagerScore = eval.ManagerTotalScore;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return $"Đã hiệu chỉnh & phê duyệt KPI: {eval.FinalScore:0.0} điểm — Xếp loại {label}.";
    }
}

// ============================================================================
// 5. QUERIES & COMMANDS CHO KHEN THƯỞNG / KỶ LUẬT & BÁO CÁO BM03
// ============================================================================

public record GetRewardsAndBm03OverviewQuery(int? Year = null) : IRequest<RewardsAndBm03OverviewDto>;

public class GetRewardsAndBm03OverviewQueryHandler : IRequestHandler<GetRewardsAndBm03OverviewQuery, RewardsAndBm03OverviewDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService? _currentUser;

    public GetRewardsAndBm03OverviewQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService? currentUser = null)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<RewardsAndBm03OverviewDto> Handle(GetRewardsAndBm03OverviewQuery request, CancellationToken cancellationToken)
    {
        var scope = await Common.Security.DataScopeResolver.ResolveAsync(_context, _currentUser, cancellationToken);
        int year = request.Year ?? DateTime.Today.Year;

        var activeEmployeesQuery = _context.Employees.AsNoTracking()
            .Where(e => e.WorkStatus == "Official" || e.WorkStatus == "Probation");

        var itemsQuery = _context.RewardDisciplines.AsNoTracking()
            .Include(r => r.Category)
            .Include(r => r.Employee).ThenInclude(e => e.OrganizationUnit)
            .Include(r => r.Employee).ThenInclude(e => e.Position)
            .AsQueryable();

        if (!scope.IsUnrestricted)
        {
            var allowedUnitIds = scope.AllowedUnitIdList;
            var selfEmpId = scope.CurrentEmployeeId;
            activeEmployeesQuery = activeEmployeesQuery.Where(e =>
                allowedUnitIds.Contains(e.OrganizationUnitId) ||
                (selfEmpId.HasValue && e.Id == selfEmpId.Value));
            itemsQuery = itemsQuery.Where(r =>
                allowedUnitIds.Contains(r.Employee.OrganizationUnitId) ||
                (selfEmpId.HasValue && r.EmployeeId == selfEmpId.Value));
        }

        var activeEmployees = await activeEmployeesQuery
            .OrderBy(e => e.EmployeeCode)
            .Select(e => new SimpleLookupDto(e.Id, e.EmployeeCode, $"{e.EmployeeCode} - {e.FullName}"))
            .ToListAsync(cancellationToken);

        var categories = await _context.RewardDisciplineCategories.AsNoTracking()
            .OrderBy(c => c.CategoryType).ThenBy(c => c.CategoryCode)
            .Select(c => new RewardDisciplineCategoryDto(c.Id, c.CategoryCode, c.CategoryName, c.CategoryType, c.DefaultAmount))
            .ToListAsync(cancellationToken);

        var itemsRaw = await itemsQuery
            .OrderByDescending(r => r.DecisionDate)
            .ToListAsync(cancellationToken);

        var items = itemsRaw.Select(r => new RewardDisciplineItemDto(
            r.Id,
            r.DecisionNumber,
            r.EmployeeId,
            r.Employee.EmployeeCode,
            r.Employee.FullName,
            r.Employee.OrganizationUnit.UnitName,
            r.Employee.Position.PositionName,
            r.CategoryId,
            r.Category.CategoryName,
            r.Type,
            r.DecisionDate,
            r.EffectiveDate,
            r.Amount,
            r.Reason,
            r.Status)).ToList();

        var bm03UnitSummaries = itemsRaw
            .GroupBy(r => new { r.Employee.OrganizationUnit.UnitCode, r.Employee.OrganizationUnit.UnitName })
            .Select(g => new Bm03UnitSummaryDto(
                g.Key.UnitCode,
                g.Key.UnitName,
                g.Count(x => x.Type == "Reward"),
                g.Where(x => x.Type == "Reward").Sum(x => x.Amount),
                g.Count(x => x.Type == "Discipline"),
                g.Where(x => x.Type == "Discipline").Sum(x => x.Amount)))
            .OrderBy(x => x.UnitCode)
            .ToList();

        return new RewardsAndBm03OverviewDto(
            year,
            categories,
            items,
            bm03UnitSummaries,
            activeEmployees,
            items.Count(x => x.Type == "Reward"),
            items.Where(x => x.Type == "Reward").Sum(x => x.Amount),
            items.Count(x => x.Type == "Discipline"),
            items.Where(x => x.Type == "Discipline").Sum(x => x.Amount));
    }
}

public record CreateRewardDisciplineCommand(
    int EmployeeId,
    int CategoryId,
    DateOnly DecisionDate,
    DateOnly EffectiveDate,
    decimal Amount,
    string Reason,
    bool ApproveImmediately = true) : IRequest<int>;

public class CreateRewardDisciplineCommandHandler : IRequestHandler<CreateRewardDisciplineCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateRewardDisciplineCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateRewardDisciplineCommand request, CancellationToken cancellationToken)
    {
        var category = await _context.RewardDisciplineCategories.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy danh mục khen thưởng/kỷ luật.");

        var count = await _context.RewardDisciplines.CountAsync(cancellationToken) + 1;
        var prefix = category.CategoryType == "Reward" ? "QĐ-KT" : "QĐ-KL";

        var entity = new RewardDiscipline
        {
            DecisionNumber = $"{prefix}-{request.DecisionDate.Year}/{count:D3}",
            EmployeeId = request.EmployeeId,
            CategoryId = request.CategoryId,
            Type = category.CategoryType,
            DecisionDate = request.DecisionDate,
            EffectiveDate = request.EffectiveDate,
            Amount = request.Amount > 0 ? request.Amount : category.DefaultAmount,
            Reason = request.Reason.Trim(),
            Status = request.ApproveImmediately ? "Approved" : "PendingApproval"
        };

        await _context.RewardDisciplines.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

public record GetRewardDisciplinePrintDetailQuery(int Id) : IRequest<RewardDisciplineItemDto?>;

public class GetRewardDisciplinePrintDetailQueryHandler : IRequestHandler<GetRewardDisciplinePrintDetailQuery, RewardDisciplineItemDto?>
{
    private readonly IApplicationDbContext _context;

    public GetRewardDisciplinePrintDetailQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RewardDisciplineItemDto?> Handle(GetRewardDisciplinePrintDetailQuery request, CancellationToken cancellationToken)
    {
        var r = await _context.RewardDisciplines.AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Employee).ThenInclude(e => e.OrganizationUnit)
            .Include(x => x.Employee).ThenInclude(e => e.Position)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (r == null) return null;

        return new RewardDisciplineItemDto(
            r.Id,
            r.DecisionNumber,
            r.EmployeeId,
            r.Employee.EmployeeCode,
            r.Employee.FullName,
            r.Employee.OrganizationUnit.UnitName,
            r.Employee.Position.PositionName,
            r.CategoryId,
            r.Category.CategoryName,
            r.Type,
            r.DecisionDate,
            r.EffectiveDate,
            r.Amount,
            r.Reason,
            r.Status);
    }
}

