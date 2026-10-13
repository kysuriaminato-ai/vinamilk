using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Application.DTOs.Kpis;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Entities.Kpis;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.Services;

public partial class KpiService
{
    public async Task<KpiAssignmentDto> CreateKpiAsync(CreateKpiCommand command, CancellationToken ct = default)
    {
        var employee = await _context.Employees.Include(e => e.OrganizationalUnit).FirstOrDefaultAsync(e => e.Id == command.EmployeeId, ct)
            ?? throw new DomainException($"Không tìm thấy nhân viên Id = {command.EmployeeId}.", "ERR_KPI_EMP_NOT_FOUND");

        var evaluator = await _context.Employees.FirstOrDefaultAsync(e => e.Id == command.EvaluatorId, ct)
            ?? throw new DomainException($"Không tìm thấy cấp quản lý đánh giá Id = {command.EvaluatorId}.", "ERR_KPI_EVAL_NOT_FOUND");

        var kpi = new KpiAssignment(
            command.EmployeeId,
            command.EvaluatorId,
            command.Period,
            command.Title,
            command.Description,
            command.TargetMetric,
            command.WeightPercentage,
            command.DueDate,
            command.BaseScore);

        await _context.KpiAssignments.AddAsync(kpi, ct);
        await _context.SaveChangesAsync(ct);

        return MapToLegacyDto(kpi, employee, evaluator);
    }

    public async Task StartKpiAsync(int kpiId, CancellationToken ct = default)
    {
        var kpi = await _context.KpiAssignments.FindAsync(new object[] { kpiId }, ct)
            ?? throw new DomainException($"Không tìm thấy chỉ tiêu KPI Id = {kpiId}.", "ERR_KPI_NOT_FOUND");

        kpi.StartProgress();
        await _context.SaveChangesAsync(ct);
    }

    public async Task<KpiAssignmentDto> SubmitKpiResultAsync(SubmitKpiCommand command, CancellationToken ct = default)
    {
        var kpi = await _context.KpiAssignments
            .Include(k => k.Employee)
                .ThenInclude(e => e!.OrganizationalUnit)
            .Include(k => k.Evaluator)
            .FirstOrDefaultAsync(k => k.Id == command.KpiId, ct)
            ?? throw new DomainException($"Không tìm thấy chỉ tiêu KPI Id = {command.KpiId}.", "ERR_KPI_NOT_FOUND");

        kpi.SubmitTask(command.ResultSummary, command.CompletedDate);
        await _context.SaveChangesAsync(ct);

        return MapToLegacyDto(kpi, kpi.Employee!, kpi.Evaluator!);
    }

    public async Task<KpiAssignmentDto> LeaderApproveKpiAsync(LeaderApproveKpiCommand command, CancellationToken ct = default)
    {
        var kpi = await _context.KpiAssignments
            .Include(k => k.Employee)
                .ThenInclude(e => e!.OrganizationalUnit)
            .Include(k => k.Evaluator)
            .FirstOrDefaultAsync(k => k.Id == command.KpiId, ct)
            ?? throw new DomainException($"Không tìm thấy chỉ tiêu KPI Id = {command.KpiId}.", "ERR_KPI_NOT_FOUND");

        kpi.LeaderApproveKpi(command.LeaderId, command.ScoreAdjustment, command.Feedback);
        await _context.SaveChangesAsync(ct);

        return MapToLegacyDto(kpi, kpi.Employee!, kpi.Evaluator!);
    }

    public async Task RejectKpiAsync(int kpiId, int leaderId, string reason, CancellationToken ct = default)
    {
        var kpi = await _context.KpiAssignments.FindAsync(new object[] { kpiId }, ct)
            ?? throw new DomainException($"Không tìm thấy chỉ tiêu KPI Id = {kpiId}.", "ERR_KPI_NOT_FOUND");

        kpi.RejectByLeader(leaderId, reason);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<KpiAssignmentDto?> GetKpiByIdAsync(int id, CancellationToken ct = default)
    {
        var kpi = await _context.KpiAssignments
            .AsNoTracking()
            .Include(k => k.Employee)
                .ThenInclude(e => e!.OrganizationalUnit)
            .Include(k => k.Evaluator)
            .FirstOrDefaultAsync(k => k.Id == id, ct);

        return kpi != null ? MapToLegacyDto(kpi, kpi.Employee!, kpi.Evaluator!) : null;
    }

    public async Task<KpiKanbanBoardDto> GetLegacyKanbanBoardAsync(string period, int? unitId = null, CancellationToken ct = default)
    {
        var query = _context.KpiAssignments
            .AsNoTracking()
            .Include(k => k.Employee)
                .ThenInclude(e => e!.OrganizationalUnit)
            .Include(k => k.Evaluator)
            .Where(k => k.Period == period);

        if (unitId.HasValue)
        {
            query = query.Where(k => k.Employee != null && k.Employee.OrganizationalUnitId == unitId.Value);
        }

        var list = await query.ToListAsync(ct);
        var dtos = list.Select(k => MapToLegacyDto(k, k.Employee!, k.Evaluator!)).ToList();

        var assigned = dtos.Where(k => k.Status == KpiStatus.Assigned || k.Status == KpiStatus.Draft).ToList();
        var inProgress = dtos.Where(k => k.Status == KpiStatus.InProgress).ToList();
        var pendingApproval = dtos.Where(k => k.Status == KpiStatus.Submitted).ToList();
        var approved = dtos.Where(k => k.Status == KpiStatus.Approved).ToList();

        decimal avgScore = approved.Any() ? Math.Round(approved.Average(k => k.FinalScore ?? 0m), 2) : 0m;

        return new KpiKanbanBoardDto(
            assigned,
            inProgress,
            pendingApproval,
            approved,
            dtos.Count,
            avgScore);
    }

    private static KpiAssignmentDto MapToLegacyDto(KpiAssignment k, Employee emp, Employee eval)
    {
        return new KpiAssignmentDto(
            k.Id,
            k.EmployeeId,
            emp?.EmployeeCode ?? string.Empty,
            emp?.FullName ?? string.Empty,
            emp?.OrganizationalUnit?.UnitName ?? "Toàn công ty",
            k.EvaluatorId,
            eval?.FullName ?? string.Empty,
            k.Period,
            k.Title,
            k.Description,
            k.TargetMetric,
            k.WeightPercentage,
            k.DueDate,
            k.BaseScore,
            k.SlaBonusPenaltyScore,
            k.ScoreAdjustment,
            k.SlaCalculationDetail,
            k.CompletedDate,
            k.ActualResult,
            k.SelfEvaluationScore,
            k.FinalScore,
            k.Status,
            k.Feedback,
            k.SubmissionDate,
            k.EvaluationDate);
    }
}
