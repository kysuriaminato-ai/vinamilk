using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Application.DTOs.Kpis;
using VinamilkHRM.Application.Interfaces;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Entities.Kpis;
using VinamilkHRM.Domain.Enums;
using VinamilkHRM.Domain.Interfaces.Repositories;

namespace VinamilkHRM.Application.Services;

/// <summary>
/// Động cơ Đánh giá KPI Giao việc theo SLA và Phê duyệt Lãnh đạo.
/// Cài đặt thuật toán SLA tự động (Thưởng +2đ/ngày tối đa +10đ; Phạt -5đ/ngày sàn 0đ)
/// và quy trình thẩm định, thông qua điểm chính thức chuyển tiếp sang Phân hệ Tính lương.
/// </summary>
public class KpiService : IKpiService
{
    private readonly IApplicationDbContext _context;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public KpiService(
        IApplicationDbContext context,
        IEmployeeRepository employeeRepository,
        IUnitOfWork unitOfWork)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _employeeRepository = employeeRepository ?? throw new ArgumentNullException(nameof(employeeRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<KpiAssignmentDto> CreateKpiAsync(CreateKpiCommand command, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(command.EmployeeId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy nhân viên Id={command.EmployeeId}.", "ERR_KPI_EMP_NOT_FOUND");

        var evaluator = await _employeeRepository.GetByIdAsync(command.EvaluatorId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy cấp quản lý đánh giá Id={command.EvaluatorId}.", "ERR_KPI_EVAL_NOT_FOUND");

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

        await _context.KpiAssignments.AddAsync(kpi, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(kpi, employee, evaluator);
    }

    public async Task StartKpiAsync(int kpiId, CancellationToken cancellationToken = default)
    {
        var kpi = await _context.KpiAssignments.FindAsync(new object[] { kpiId }, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy chỉ tiêu KPI Id={kpiId}.", "ERR_KPI_NOT_FOUND");

        kpi.StartProgress();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<KpiAssignmentDto> SubmitKpiResultAsync(SubmitKpiCommand command, CancellationToken cancellationToken = default)
    {
        var kpi = await _context.KpiAssignments
            .Include(k => k.Employee)
            .Include(k => k.Evaluator)
            .FirstOrDefaultAsync(k => k.Id == command.KpiId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy chỉ tiêu KPI Id={command.KpiId}.", "ERR_KPI_NOT_FOUND");

        // Gọi Domain Method: Tự động chạy thuật toán SLA và chuyển sang PendingApproval
        kpi.SubmitTask(command.ResultSummary, command.CompletedDate);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(kpi, kpi.Employee!, kpi.Evaluator!);
    }

    public async Task<KpiAssignmentDto> LeaderApproveKpiAsync(LeaderApproveKpiCommand command, CancellationToken cancellationToken = default)
    {
        var kpi = await _context.KpiAssignments
            .Include(k => k.Employee)
            .Include(k => k.Evaluator)
            .FirstOrDefaultAsync(k => k.Id == command.KpiId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy chỉ tiêu KPI Id={command.KpiId}.", "ERR_KPI_NOT_FOUND");

        // Gọi Domain Method: Lãnh đạo thẩm định, áp dụng điều chỉnh điểm và chốt FinalScore
        kpi.LeaderApproveKpi(command.LeaderId, command.ScoreAdjustment, command.Feedback);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(kpi, kpi.Employee!, kpi.Evaluator!);
    }

    public async Task RejectKpiAsync(int kpiId, int leaderId, string rejectionReason, CancellationToken cancellationToken = default)
    {
        var kpi = await _context.KpiAssignments.FindAsync(new object[] { kpiId }, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy chỉ tiêu KPI Id={kpiId}.", "ERR_KPI_NOT_FOUND");

        kpi.RejectByLeader(leaderId, rejectionReason);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<KpiKanbanBoardDto> GetKanbanBoardAsync(string period, int? unitId = null, int? employeeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.KpiAssignments
            .Include(k => k.Employee)
            .ThenInclude(e => e!.OrganizationalUnit)
            .Include(k => k.Evaluator)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(period))
        {
            query = query.Where(k => k.Period == period);
        }

        if (unitId.HasValue)
        {
            query = query.Where(k => k.Employee != null && k.Employee.OrganizationalUnitId == unitId.Value);
        }

        if (employeeId.HasValue)
        {
            query = query.Where(k => k.EmployeeId == employeeId.Value);
        }

        var allTasks = await query.ToListAsync(cancellationToken);
        var dtos = allTasks.Select(k => MapToDto(k, k.Employee!, k.Evaluator!)).ToList();

        var assigned = dtos.Where(t => t.Status == KpiStatus.Assigned || t.Status == KpiStatus.Draft).ToList();
        var inProgress = dtos.Where(t => t.Status == KpiStatus.InProgress).ToList();
        var pendingApproval = dtos.Where(t => t.Status == KpiStatus.Submitted).ToList();
        var approved = dtos.Where(t => t.Status == KpiStatus.Approved).ToList();

        decimal avgScore = approved.Count > 0 ? Math.Round(approved.Average(a => a.FinalScore ?? 0m), 1) : 0m;

        return new KpiKanbanBoardDto(
            assigned,
            inProgress,
            pendingApproval,
            approved,
            dtos.Count,
            avgScore
        );
    }

    public async Task<KpiAssignmentDto?> GetKpiByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var kpi = await _context.KpiAssignments
            .Include(k => k.Employee)
            .Include(k => k.Evaluator)
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.Id == id, cancellationToken);

        return kpi == null ? null : MapToDto(kpi, kpi.Employee!, kpi.Evaluator!);
    }

    private static KpiAssignmentDto MapToDto(KpiAssignment k, Employee emp, Employee eval)
    {
        return new KpiAssignmentDto(
            k.Id,
            k.EmployeeId,
            emp?.EmployeeCode ?? "N/A",
            emp?.FullName ?? "N/A",
            emp?.OrganizationalUnit?.UnitName ?? "Vinamilk",
            k.EvaluatorId,
            eval?.FullName ?? "Lãnh đạo phê duyệt",
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
            k.EvaluationDate
        );
    }
}
