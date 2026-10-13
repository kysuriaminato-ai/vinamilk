using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Application.DTOs.Salaries;
using VinamilkHRM.Application.Interfaces;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Contracts;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Entities.Salaries;
using VinamilkHRM.Domain.Enums;
using VinamilkHRM.Domain.Interfaces.Repositories;
using VinamilkHRM.Domain.Services.TotalRewards;

namespace VinamilkHRM.Application.Services;

public class SalaryReviewService : ISalaryReviewService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILaborContractRepository _contractRepository;

    public SalaryReviewService(
        IApplicationDbContext dbContext,
        IEmployeeRepository employeeRepository,
        ILaborContractRepository contractRepository)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _employeeRepository = employeeRepository ?? throw new ArgumentNullException(nameof(employeeRepository));
        _contractRepository = contractRepository ?? throw new ArgumentNullException(nameof(contractRepository));
    }

    // 1. Quản lý Khung lương (Salary Bands)
    public async Task<IReadOnlyList<SalaryBandDto>> GetSalaryBandsAsync(CancellationToken cancellationToken = default)
    {
        var list = await _dbContext.SalaryBands
            .Include(b => b.JobGrade)
            .AsNoTracking()
            .OrderBy(b => b.JobGrade != null ? b.JobGrade.GradeNumber : 0)
            .ToListAsync(cancellationToken);

        return list.Select(b => new SalaryBandDto(
            b.Id,
            b.JobGradeId,
            b.JobGrade?.GradeCode ?? "N/A",
            b.JobGrade?.GradeName ?? "N/A",
            b.Min,
            b.Mid,
            b.Max,
            b.EffectiveFrom,
            b.EffectiveTo,
            b.Description,
            b.Min > 0 ? Math.Round((b.Max - b.Min) / b.Min, 2) : 0m)).ToList();
    }

    public async Task<SalaryBandDto> CreateSalaryBandAsync(CreateSalaryBandRequest request, CancellationToken cancellationToken = default)
    {
        var band = new SalaryBand(
            request.JobGradeId,
            request.Min,
            request.Mid,
            request.Max,
            request.EffectiveFrom,
            request.EffectiveTo,
            request.Description);

        await _dbContext.SalaryBands.AddAsync(band, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var grade = await _dbContext.JobGrades.FindAsync(new object[] { band.JobGradeId }, cancellationToken);

        return new SalaryBandDto(
            band.Id,
            band.JobGradeId,
            grade?.GradeCode ?? "N/A",
            grade?.GradeName ?? "N/A",
            band.Min,
            band.Mid,
            band.Max,
            band.EffectiveFrom,
            band.EffectiveTo,
            band.Description,
            band.Min > 0 ? Math.Round((band.Max - band.Min) / band.Min, 2) : 0m);
    }

    // 2. Chu kỳ xét tăng lương (Salary Review Cycle)
    public async Task<SalaryReviewCycleDto> CreateCycleAsync(CreateSalaryReviewCycleRequest request, CancellationToken cancellationToken = default)
    {
        var cycle = new SalaryReviewCycle(
            request.Year,
            request.Name,
            request.EffectiveDate,
            request.MinTenureMonths,
            request.ExcludeDisciplined,
            request.ExcludeRecentPromotionMonths);

        await _dbContext.SalaryReviewCycles.AddAsync(cycle, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToCycleDto(cycle);
    }

    public async Task<IReadOnlyList<SalaryReviewCycleDto>> GetCyclesAsync(CancellationToken cancellationToken = default)
    {
        var cycles = await _dbContext.SalaryReviewCycles
            .Include(c => c.Budgets)
            .AsNoTracking()
            .OrderByDescending(c => c.Year)
            .ToListAsync(cancellationToken);

        return cycles.Select(MapToCycleDto).ToList();
    }

    public async Task<SalaryReviewCycleDto> OpenCycleAsync(int cycleId, decimal defaultBudgetRate = 0.08m, CancellationToken cancellationToken = default)
    {
        var cycle = await _dbContext.SalaryReviewCycles
            .Include(c => c.Budgets)
            .FirstOrDefaultAsync(c => c.Id == cycleId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy chu kỳ Id={cycleId}.", "ERR_SAL_CYCLE_NOT_FOUND");

        cycle.Open();

        // Tự động phân bổ ngân sách cho từng đơn vị (mặc định = 8% quỹ lương hiện tại của đơn vị)
        var units = await _dbContext.OrganizationalUnits
            .Include(u => u.Employees)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        foreach (var unit in units)
        {
            decimal unitCurrentSalaryPool = unit.Employees.Where(e => e.IsActive).Sum(e => e.BaseSalary);
            decimal allocatedBudget = Math.Round(unitCurrentSalaryPool * defaultBudgetRate, 0, MidpointRounding.AwayFromZero);

            var existingBudget = cycle.Budgets.FirstOrDefault(b => b.OrganizationalUnitId == unit.Id);
            if (existingBudget == null)
            {
                var newBudget = new SalaryBudget(cycle.Id, unit.Id, allocatedBudget, $"Phân bổ ngân sách {defaultBudgetRate * 100}% quỹ lương");
                await _dbContext.SalaryBudgets.AddAsync(newBudget, cancellationToken);
            }
        }

        cycle.MoveToUnitReview();
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToCycleDto(cycle);
    }

    public async Task UpdateBudgetAsync(UpdateSalaryBudgetRequest request, int ceoId, CancellationToken cancellationToken = default)
    {
        var budget = await _dbContext.SalaryBudgets.FindAsync(new object[] { request.BudgetId }, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy ngân sách Id={request.BudgetId}.", "ERR_SAL_BUDGET_NOT_FOUND");

        budget.AdjustAmount(request.NewAmount, ceoId, request.Reason);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    // 3. Rà soát & Đề xuất tại Đơn vị (Unit Merit Review)
    public async Task<IReadOnlyList<UnitMeritSuggestionDto>> GetUnitSuggestionsAsync(int cycleId, int unitId, CancellationToken cancellationToken = default)
    {
        var cycle = await _dbContext.SalaryReviewCycles.FindAsync(new object[] { cycleId }, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy chu kỳ Id={cycleId}.", "ERR_SAL_CYCLE_NOT_FOUND");

        var budget = await _dbContext.SalaryBudgets
            .FirstOrDefaultAsync(b => b.SalaryReviewCycleId == cycleId && b.OrganizationalUnitId == unitId, cancellationToken);

        decimal unitBudgetAmount = budget?.Amount ?? 0m;

        // Lấy danh sách nhân viên trong đơn vị
        var employees = await _dbContext.Employees
            .Include(e => e.JobGrade)
            .Where(e => e.OrganizationalUnitId == unitId && e.IsActive)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Lấy khung lương theo ngạch
        var bands = await _dbContext.SalaryBands.AsNoTracking().ToListAsync(cancellationToken);

        // Lấy kết quả KPI 12 tháng gần nhất
        var allKpis = await _dbContext.KpiResults
            .Where(k => employees.Select(e => e.Id).Contains(k.EmployeeId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var suggestions = new List<UnitMeritSuggestionDto>();
        decimal totalSuggestedAmount = 0m;

        // Bước 1: Tính toán gợi ý tiêu chuẩn cho từng người
        var prelimList = new List<(Employee Emp, decimal CompaRatio, decimal Mid, decimal AnnualScore, KpiGrade Grade, decimal StdPct)>();

        foreach (var emp in employees)
        {
            // Điều kiện thâm niên
            if (cycle.MinTenureMonths > 0)
            {
                var tenureDays = (DateTime.UtcNow - emp.HireDate).TotalDays;
                if (tenureDays < cycle.MinTenureMonths * 30)
                    continue;
            }

            var empKpis = allKpis.Where(k => k.EmployeeId == emp.Id).ToList();
            var (annualScore, grade) = TotalRewardsEngine.CalculateAnnualKpi(empKpis);

            var band = bands.FirstOrDefault(b => b.JobGradeId == emp.JobGradeId);
            decimal mid = band?.Mid ?? emp.BaseSalary;
            decimal compaRatio = band != null ? band.CalculateCompaRatio(emp.BaseSalary) : 1.0m;

            decimal stdPct = TotalRewardsEngine.GetMeritIncreasePercentage(grade, compaRatio);
            decimal stdIncrease = Math.Round(emp.BaseSalary * stdPct, 0, MidpointRounding.AwayFromZero);
            totalSuggestedAmount += stdIncrease;

            prelimList.Add((emp, compaRatio, mid, annualScore, grade, stdPct));
        }

        // Bước 2: Co giãn ngân sách (Proration Factor)
        decimal prorationFactor = TotalRewardsEngine.CalculateProrationFactor(unitBudgetAmount, totalSuggestedAmount);

        // Bước 3: Tổng hợp danh sách gợi ý
        foreach (var item in prelimList)
        {
            decimal proratedPct = Math.Round(item.StdPct * prorationFactor, 4, MidpointRounding.AwayFromZero);
            decimal proratedIncrease = Math.Round(item.Emp.BaseSalary * proratedPct, 0, MidpointRounding.AwayFromZero);
            decimal proposedP1 = item.Emp.BaseSalary + proratedIncrease;

            // Biên độ cho phép Trưởng đơn vị điều chỉnh: +/- 3 điểm %
            decimal minPct = Math.Max(0m, proratedPct - 0.03m);
            decimal maxPct = proratedPct + 0.03m;

            suggestions.Add(new UnitMeritSuggestionDto(
                item.Emp.Id,
                item.Emp.EmployeeCode,
                item.Emp.FullName,
                item.Emp.JobGradeId ?? 0,
                item.Emp.JobGrade?.GradeCode ?? "N/A",
                item.Emp.BaseSalary,
                item.Mid,
                item.CompaRatio,
                item.AnnualScore,
                item.Grade,
                item.StdPct,
                proratedPct,
                proratedIncrease,
                proposedP1,
                minPct,
                maxPct));
        }

        return suggestions;
    }

    public async Task SubmitUnitReviewAsync(UnitMeritSubmissionRequest request, int managerId, CancellationToken cancellationToken = default)
    {
        var budget = await _dbContext.SalaryBudgets
            .FirstOrDefaultAsync(b => b.SalaryReviewCycleId == request.SalaryReviewCycleId && b.OrganizationalUnitId == request.OrganizationalUnitId, cancellationToken)
            ?? throw new DomainException("Chưa phân bổ ngân sách xét lương cho đơn vị này.", "ERR_SAL_BUDGET_NOT_FOUND");

        var empIds = request.Items.Select(i => i.EmployeeId).ToList();
        var employees = await _dbContext.Employees
            .Where(e => empIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, cancellationToken);

        // Tính tổng mức đề xuất của toàn đơn vị
        decimal totalProposed = 0m;
        foreach (var item in request.Items)
        {
            if (employees.TryGetValue(item.EmployeeId, out var emp))
            {
                decimal increase = item.ProposedP1 - emp.BaseSalary;
                if (increase > 0)
                {
                    totalProposed += increase;
                }
            }
        }

        // RÀNG BUỘC PHÁP LÝ & NGÂN SÁCH: Không cho phép nộp nếu vượt ngân sách đơn vị
        TotalRewardsEngine.ValidateUnitSubmission(budget.Amount, totalProposed);

        var cycle = await _dbContext.SalaryReviewCycles.FindAsync(new object[] { request.SalaryReviewCycleId }, cancellationToken);
        var bands = await _dbContext.SalaryBands.AsNoTracking().ToListAsync(cancellationToken);

        // Tạo hoặc cập nhật các SalaryProposal trong chu kỳ
        foreach (var item in request.Items)
        {
            if (!employees.TryGetValue(item.EmployeeId, out var emp))
                continue;

            decimal increase = item.ProposedP1 - emp.BaseSalary;
            if (increase <= 0) continue;

            var band = bands.FirstOrDefault(b => b.JobGradeId == emp.JobGradeId);
            decimal crBefore = band != null ? band.CalculateCompaRatio(emp.BaseSalary) : 0m;
            decimal crAfter = band != null ? band.CalculateCompaRatio(item.ProposedP1) : 0m;

            var proposal = new SalaryProposal(
                employeeId: emp.Id,
                proposedById: managerId,
                currentSalary: emp.BaseSalary,
                proposedSalary: item.ProposedP1,
                currentAllowance: emp.Allowance,
                proposedAllowance: emp.Allowance,
                reason: item.Reason ?? $"Xét tăng lương định kỳ năm {cycle?.Year}",
                effectiveDate: cycle?.EffectiveDate.ToDateTime(TimeOnly.MinValue) ?? DateTime.UtcNow,
                adjustmentType: SalaryAdjustmentType.Merit,
                compaRatioBefore: crBefore,
                compaRatioAfter: crAfter,
                fundSource: SalaryFundSource.CycleBudget,
                salaryReviewCycleId: request.SalaryReviewCycleId,
                organizationalUnitId: request.OrganizationalUnitId);

            proposal.ConfigureRoute(requiresBoardOfDirectors: false, unitManagerId: managerId);
            await _dbContext.SalaryProposals.AddAsync(proposal, cancellationToken);
        }

        budget.UpdateUsedAmount(totalProposed);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    // 4. Đặc cách (Special Adjustment)
    public async Task<SalaryAdjustmentDetailDto> CreateSpecialAdjustmentAsync(SpecialSalaryAdjustmentRequest request, int proposerId, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy nhân viên Id={request.EmployeeId}.", "ERR_EMP_NOT_FOUND");

        var band = await _dbContext.SalaryBands
            .FirstOrDefaultAsync(b => b.JobGradeId == employee.JobGradeId && b.IsActive, cancellationToken);

        // Đánh giá trần đặc cách 30% và Max của khung lương
        var (requiresBoard, pct, reason) = TotalRewardsEngine.EvaluateSpecialAdjustment(employee.BaseSalary, request.ProposedP1, band);

        decimal crBefore = band != null ? band.CalculateCompaRatio(employee.BaseSalary) : 0m;
        decimal crAfter = band != null ? band.CalculateCompaRatio(request.ProposedP1) : 0m;

        var proposal = new SalaryProposal(
            employeeId: employee.Id,
            proposedById: proposerId,
            currentSalary: employee.BaseSalary,
            proposedSalary: request.ProposedP1,
            currentAllowance: employee.Allowance,
            proposedAllowance: request.ProposedAllowance,
            reason: request.Reason,
            effectiveDate: request.EffectiveDate,
            adjustmentType: SalaryAdjustmentType.Special,
            compaRatioBefore: crBefore,
            compaRatioAfter: crAfter,
            evidence: request.Evidence,
            fundSource: SalaryFundSource.SpecialFund,
            organizationalUnitId: employee.OrganizationalUnitId);

        proposal.ConfigureRoute(requiresBoardOfDirectors: requiresBoard, ceoId: 999);
        await _dbContext.SalaryProposals.AddAsync(proposal, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDetailDto(proposal, employee);
    }

    // 5. Quy trình duyệt (Approval Workflow)
    public async Task ApproveStepAsync(ApproveAdjustmentStepRequest request, CancellationToken cancellationToken = default)
    {
        var proposal = await _dbContext.SalaryProposals
            .Include(p => p.Employee)
            .Include(p => p.ApprovalSteps)
            .FirstOrDefaultAsync(p => p.Id == request.ProposalId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy tờ trình Id={request.ProposalId}.", "ERR_SAL_PROPOSAL_NOT_FOUND");

        var approver = await _employeeRepository.GetByIdAsync(request.ApproverId, cancellationToken);

        // Xử lý duyệt từng bước kèm kiểm tra nghiêm ngặt Xung đột lợi ích
        proposal.ProcessStepApproval(request.ApproverId, request.Comment, reviewerCitizenId: approver?.CitizenId);

        // Nếu là đề xuất đặc cách đơn lẻ và đã được duyệt toàn bộ -> cập nhật hồ sơ & sinh phụ lục
        if (proposal.Status == SalaryProposalStatus.Approved && proposal.AdjustmentType == SalaryAdjustmentType.Special)
        {
            var emp = proposal.Employee ?? await _employeeRepository.GetByIdAsync(proposal.EmployeeId, cancellationToken);
            if (emp != null)
            {
                emp.PromoteSalary(proposal.ProposedSalary, proposal.ProposedAllowance, $"Phê duyệt đặc cách: {proposal.Reason}");

                var contract = await _contractRepository.GetActiveContractByEmployeeIdAsync(emp.Id, cancellationToken);
                string appendixNumber = $"PLHD-SPECIAL-{DateTime.UtcNow:yyyyMMdd}-{emp.EmployeeCode}";
                string digitalHash = ComputeSha256($"SPECIAL_SALARY_{proposal.Id}_{emp.EmployeeCode}_{DateTime.UtcNow:O}");

                var appendix = new ElectronicContractAppendix(
                    appendixNumber: appendixNumber,
                    laborContractId: contract?.Id ?? 1,
                    employeeId: emp.Id,
                    effectiveDate: proposal.EffectiveDate,
                    signDate: DateTime.UtcNow,
                    previousSalary: proposal.CurrentSalary,
                    newSalary: proposal.ProposedSalary,
                    previousAllowance: proposal.CurrentAllowance,
                    newAllowance: proposal.ProposedAllowance,
                    adjustmentReason: $"[ĐẶC CÁCH TĂNG LƯƠNG] {proposal.Reason}",
                    isCeoOverride: true,
                    ceoOverrideCode: $"SPECIAL-{proposal.Id}",
                    digitalSignatureHash: digitalHash,
                    signerName: "Mai Kiều Liên",
                    signerTitle: "Tổng Giám Đốc (CEO) - Vinamilk");

                await _dbContext.ElectronicContractAppendices.AddAsync(appendix, cancellationToken);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectProposalAsync(int proposalId, int reviewerId, string reason, CancellationToken cancellationToken = default)
    {
        var proposal = await _dbContext.SalaryProposals
            .Include(p => p.ApprovalSteps)
            .FirstOrDefaultAsync(p => p.Id == proposalId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy tờ trình Id={proposalId}.", "ERR_SAL_PROPOSAL_NOT_FOUND");

        proposal.Reject(reviewerId, reason);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    // 6. Phê duyệt Đợt tăng lương của Tổng Giám Đốc (Executive Approval)
    public async Task<SalaryReviewCycleDto> ApproveExecutiveCycleAsync(int cycleId, int ceoId, CancellationToken cancellationToken = default)
    {
        var cycle = await _dbContext.SalaryReviewCycles
            .Include(c => c.Budgets)
            .Include(c => c.Adjustments)
            .FirstOrDefaultAsync(c => c.Id == cycleId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy chu kỳ Id={cycleId}.", "ERR_SAL_CYCLE_NOT_FOUND");

        cycle.Approve(ceoId);

        var proposals = await _dbContext.SalaryProposals
            .Include(p => p.Employee)
            .Include(p => p.ApprovalSteps)
            .Where(p => p.SalaryReviewCycleId == cycleId)
            .ToListAsync(cancellationToken);

        var lockedRuns = await _dbContext.PayrollRuns
            .Where(r => r.Status >= PayrollRunStatus.Approved)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var approvalDate = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var proposal in proposals)
        {
            if (proposal.Status != SalaryProposalStatus.Approved)
            {
                proposal.ApproveByHRDirector(ceoId, "Tổng Giám Đốc phê duyệt đợt xét tăng lương toàn tập đoàn.");
            }

            var emp = proposal.Employee ?? await _employeeRepository.GetByIdAsync(proposal.EmployeeId, cancellationToken);
            if (emp != null)
            {
                // Cập nhật mức lương mới vào hồ sơ nhân sự
                emp.PromoteSalary(proposal.ProposedSalary, proposal.ProposedAllowance, $"Quyết định tăng lương định kỳ năm {cycle.Year}");

                // Sinh Phụ lục hợp đồng lao động điện tử
                var contract = await _contractRepository.GetActiveContractByEmployeeIdAsync(emp.Id, cancellationToken);
                string appendixNumber = $"PLHD-MERIT-{cycle.Year}-{emp.EmployeeCode}";
                string digitalHash = ComputeSha256($"MERIT_{cycle.Year}_{emp.EmployeeCode}_{proposal.ProposedSalary}_{DateTime.UtcNow:O}");

                var appendix = new ElectronicContractAppendix(
                    appendixNumber: appendixNumber,
                    laborContractId: contract?.Id ?? 1,
                    employeeId: emp.Id,
                    effectiveDate: cycle.EffectiveDate.ToDateTime(TimeOnly.MinValue),
                    signDate: DateTime.UtcNow,
                    previousSalary: proposal.CurrentSalary,
                    newSalary: proposal.ProposedSalary,
                    previousAllowance: proposal.CurrentAllowance,
                    newAllowance: proposal.ProposedAllowance,
                    adjustmentReason: $"[XÉT TĂNG LƯƠNG ĐỊNH KỲ NĂM {cycle.Year}] {proposal.Reason}",
                    isCeoOverride: false,
                    ceoOverrideCode: null,
                    digitalSignatureHash: digitalHash,
                    signerName: "Mai Kiều Liên",
                    signerTitle: "Tổng Giám Đốc (CEO) - Vinamilk");

                await _dbContext.ElectronicContractAppendices.AddAsync(appendix, cancellationToken);

                // KIỂM TRA HỒI TỐ: Duyệt sau ngày hiệu lực -> sinh truy lĩnh tự động
                if (approvalDate > cycle.EffectiveDate)
                {
                    var arrears = TotalRewardsEngine.GenerateRetroactiveAdjustments(
                        employeeId: emp.Id,
                        currentP1: proposal.CurrentSalary,
                        proposedP1: proposal.ProposedSalary,
                        effectiveDate: cycle.EffectiveDate,
                        approvalDate: approvalDate,
                        lockedRuns: lockedRuns,
                        approverId: ceoId);

                    foreach (var adj in arrears)
                    {
                        await _dbContext.PayrollAdjustments.AddAsync(adj, cancellationToken);
                    }
                }
            }
        }

        cycle.Apply(ceoId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToCycleDto(cycle);
    }

    // 7. Báo cáo Total Rewards
    public async Task<TotalRewardsReportDto> GetTotalRewardsReportAsync(int cycleId, CancellationToken cancellationToken = default)
    {
        var cycle = await _dbContext.SalaryReviewCycles
            .Include(c => c.Budgets)
                .ThenInclude(b => b.OrganizationalUnit)
            .Include(c => c.Adjustments)
                .ThenInclude(a => a.Employee)
                    .ThenInclude(e => e!.JobGrade)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == cycleId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy chu kỳ Id={cycleId}.", "ERR_SAL_CYCLE_NOT_FOUND");

        // 1. Sử dụng ngân sách theo đơn vị
        var unitReports = cycle.Budgets.Select(b => new UnitBudgetUsageReportItem(
            b.OrganizationalUnitId,
            b.OrganizationalUnit?.UnitName ?? "N/A",
            b.Amount,
            b.Used,
            b.Remaining,
            b.Amount > 0 ? Math.Round((b.Used / b.Amount) * 100m, 1) : 0m,
            b.OrganizationalUnit?.Employees.Count ?? 0,
            cycle.Adjustments.Count(a => a.OrganizationalUnitId == b.OrganizationalUnitId && a.AmountIncrease > 0))).ToList();

        // 2. Phân bố theo xếp loại KPI
        var gradeDistributions = cycle.Adjustments
            .Where(a => a.AnnualKpiGrade.HasValue)
            .GroupBy(a => a.AnnualKpiGrade!.Value)
            .Select(g => new GradeIncreaseDistributionItem(
                g.Key,
                g.Key.ToString(),
                g.Count(),
                g.Count() > 0 ? Math.Round(g.Average(a => a.IncreasePercentage), 2) : 0m,
                g.Sum(a => a.AmountIncrease))).ToList();

        // 3. Chênh lệch lương Nam / Nữ theo bậc ngạch (Gender Pay Equity)
        var allEmployees = await _dbContext.Employees
            .Include(e => e.JobGrade)
            .Where(e => e.IsActive && e.JobGrade != null)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var genderEquities = allEmployees
            .GroupBy(e => e.JobGrade!.GradeNumber)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var males = g.Where(e => e.Gender == Gender.Male).ToList();
                var females = g.Where(e => e.Gender == Gender.Female).ToList();

                decimal maleAvg = males.Count > 0 ? Math.Round(males.Average(m => m.BaseSalary), 0) : 0m;
                decimal femaleAvg = females.Count > 0 ? Math.Round(females.Average(f => f.BaseSalary), 0) : 0m;
                decimal gap = maleAvg > 0 ? Math.Round(((maleAvg - femaleAvg) / maleAvg) * 100m, 1) : 0m;

                return new GenderPayEquityItem(
                    g.Key,
                    g.First().JobGrade?.GradeCode ?? $"Bậc {g.Key}",
                    maleAvg,
                    males.Count,
                    femaleAvg,
                    females.Count,
                    gap);
            }).ToList();

        // 4. Chi phí tăng thêm/năm gồm BHXH doanh nghiệp (23.5% = 21.5% BH + 2% KPCĐ)
        decimal totalMonthlyIncrease = cycle.Adjustments.Sum(a => a.AmountIncrease);
        decimal annualGross = totalMonthlyIncrease * 12m;
        decimal annualEmployerCost = Math.Round(annualGross * 1.235m, 0, MidpointRounding.AwayFromZero);

        return new TotalRewardsReportDto(
            cycle.Id,
            cycle.Name,
            unitReports,
            gradeDistributions,
            genderEquities,
            annualGross,
            annualEmployerCost);
    }

    private static string ComputeSha256(string rawData)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
        return Convert.ToHexString(bytes);
    }

    private static SalaryReviewCycleDto MapToCycleDto(SalaryReviewCycle c)
    {
        decimal totalBudget = c.Budgets.Sum(b => b.Amount);
        decimal totalUsed = c.Budgets.Sum(b => b.Used);
        return new SalaryReviewCycleDto(
            c.Id,
            c.Year,
            c.Name,
            c.EffectiveDate,
            c.Status,
            c.MinTenureMonths,
            c.ExcludeDisciplined,
            c.ExcludeRecentPromotionMonths,
            totalBudget,
            totalUsed,
            totalBudget - totalUsed);
    }

    private static SalaryAdjustmentDetailDto MapToDetailDto(SalaryProposal p, Employee? emp)
    {
        var e = emp ?? p.Employee;
        return new SalaryAdjustmentDetailDto(
            p.Id,
            p.EmployeeId,
            e?.EmployeeCode ?? "N/A",
            e?.FullName ?? "N/A",
            e?.OrganizationalUnit?.UnitName ?? "Vinamilk",
            p.AdjustmentType,
            p.CurrentSalary,
            p.ProposedSalary,
            p.AmountIncrease,
            p.IncreasePercentage,
            p.CompaRatioBefore,
            p.CompaRatioAfter,
            p.AnnualKpiScore,
            p.AnnualKpiGrade,
            p.Reason,
            p.Evidence,
            p.FundSource,
            p.Status,
            p.EffectiveDate,
            p.ApprovalSteps.Select(s => new SalaryAdjustmentApprovalStepDto(
                s.StepNumber,
                s.RoleLevel,
                s.RoleName,
                s.ApproverId,
                s.Approver?.FullName,
                s.Status,
                s.Comment,
                s.ApprovedAt,
                s.IsConflictOfInterestFlagged)).ToList());
    }
}
