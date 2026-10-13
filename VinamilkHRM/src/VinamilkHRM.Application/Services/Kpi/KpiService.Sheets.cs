using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Application.DTOs.Kpis;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Entities.Kpis;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.Services;

public partial class KpiService
{
    public async Task<KpiSheetDetailDto> GetSheetByIdAsync(int sheetId, CancellationToken ct = default)
    {
        var sheet = await _context.KpiSheets
            .AsNoTracking()
            .Include(s => s.Employee)
            .Include(s => s.Evaluator)
            .Include(s => s.OrganizationalUnit)
            .Include(s => s.Items)
                .ThenInclude(i => i.KpiDefinition)
            .Include(s => s.Items)
                .ThenInclude(i => i.ExtensionRequests)
            .FirstOrDefaultAsync(s => s.Id == sheetId, ct)
            ?? throw new DomainException($"Không tìm thấy phiếu KPI Id = {sheetId}.", "ERR_KPI_SHEET_NOT_FOUND");

        return MapToSheetDetailDto(sheet);
    }

    public async Task<KpiSheetDetailDto?> GetEmployeeSheetInPeriodAsync(int employeeId, string period, CancellationToken ct = default)
    {
        var sheet = await _context.KpiSheets
            .AsNoTracking()
            .Include(s => s.Employee)
            .Include(s => s.Evaluator)
            .Include(s => s.OrganizationalUnit)
            .Include(s => s.Items)
                .ThenInclude(i => i.KpiDefinition)
            .Include(s => s.Items)
                .ThenInclude(i => i.ExtensionRequests)
            .FirstOrDefaultAsync(s => s.EmployeeId == employeeId && s.Period == period, ct);

        return sheet != null ? MapToSheetDetailDto(sheet) : null;
    }

    public async Task<List<KpiSheetDetailDto>> GetSheetsByUnitAsync(int unitId, string period, CancellationToken ct = default)
    {
        var sheets = await _context.KpiSheets
            .AsNoTracking()
            .Include(s => s.Employee)
            .Include(s => s.Evaluator)
            .Include(s => s.OrganizationalUnit)
            .Include(s => s.Items)
                .ThenInclude(i => i.KpiDefinition)
            .Where(s => s.OrganizationalUnitId == unitId && s.Period == period)
            .ToListAsync(ct);

        return sheets.Select(MapToSheetDetailDto).ToList();
    }

    public async Task<KpiSheetDetailDto> CreateSheetAsync(CreateKpiSheetRequest request, CancellationToken ct = default)
    {
        var emp = await _context.Employees
            .Include(e => e.StandardJobTitle)
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, ct)
            ?? throw new DomainException($"Không tìm thấy nhân viên Id = {request.EmployeeId}.", "ERR_EMP_NOT_FOUND");

        var period = await _context.KpiPeriods
            .FirstOrDefaultAsync(p => p.Period == request.Period, ct)
            ?? throw new DomainException($"Không tìm thấy kỳ đánh giá {request.Period}.", "ERR_PERIOD_NOT_FOUND");

        if (await _context.KpiSheets.AnyAsync(s => s.EmployeeId == request.EmployeeId && s.Period == request.Period, ct))
            throw new DomainException($"Nhân viên {emp.FullName} đã có phiếu KPI trong kỳ {request.Period}.", "ERR_KPI_SHEET_EXISTS");

        var level = emp.StandardJobTitle?.LeadershipLevel ?? LeadershipLevel.None;
        var sheet = new KpiSheet(
            emp.Id,
            period.Id,
            request.Period,
            level,
            emp.OrganizationalUnitId,
            request.EvaluatorId,
            emp.StandardJobTitle?.JobFamilyId,
            emp.StandardJobTitleId);

        foreach (var itemReq in request.Items)
        {
            var item = new KpiItem(
                sheet.Id,
                itemReq.Group,
                itemReq.Title,
                itemReq.Unit,
                itemReq.ScoringMethod,
                itemReq.TargetValue,
                itemReq.WeightPercentage,
                itemReq.KpiDefinitionId,
                itemReq.Description,
                itemReq.DueDate);

            sheet.AddItem(item);
        }

        await _context.KpiSheets.AddAsync(sheet, ct);
        await _context.SaveChangesAsync(ct);

        return await GetSheetByIdAsync(sheet.Id, ct);
    }

    public async Task PublishSheetAsync(int sheetId, CancellationToken ct = default)
    {
        var sheet = await _context.KpiSheets
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == sheetId, ct)
            ?? throw new DomainException($"Không tìm thấy phiếu KPI Id = {sheetId}.", "ERR_KPI_SHEET_NOT_FOUND");

        sheet.Publish();
        await _context.SaveChangesAsync(ct);
    }

    public async Task<int> BulkAssignKpiAsync(BulkAssignKpiRequest request, CancellationToken ct = default)
    {
        var period = await _context.KpiPeriods.FirstOrDefaultAsync(p => p.Period == request.Period, ct)
            ?? throw new DomainException($"Không tìm thấy kỳ {request.Period}.", "ERR_PERIOD_NOT_FOUND");

        var empQuery = _context.Employees
            .Include(e => e.StandardJobTitle)
            .Where(e => e.OrganizationalUnitId == request.OrganizationalUnitId && e.IsActive);

        if (request.JobTitleId.HasValue)
            empQuery = empQuery.Where(e => e.StandardJobTitleId == request.JobTitleId.Value);

        if (request.JobFamilyId.HasValue)
            empQuery = empQuery.Where(e => e.StandardJobTitle != null && e.StandardJobTitle.JobFamilyId == request.JobFamilyId.Value);

        var employees = await empQuery.ToListAsync(ct);
        if (!employees.Any())
            return 0;

        KpiTemplate? template = null;
        if (request.TemplateId.HasValue)
        {
            template = await _context.KpiTemplates
                .Include(t => t.Items)
                    .ThenInclude(ti => ti.KpiDefinition)
                .FirstOrDefaultAsync(t => t.Id == request.TemplateId.Value, ct);
        }

        int count = 0;
        foreach (var emp in employees)
        {
            if (await _context.KpiSheets.AnyAsync(s => s.EmployeeId == emp.Id && s.Period == request.Period, ct))
                continue;

            int evaluatorId = emp.ManagerId ?? emp.Id;
            var level = emp.StandardJobTitle?.LeadershipLevel ?? LeadershipLevel.None;
            var sheet = new KpiSheet(
                emp.Id,
                period.Id,
                request.Period,
                level,
                emp.OrganizationalUnitId,
                evaluatorId,
                emp.StandardJobTitle?.JobFamilyId,
                emp.StandardJobTitleId);

            if (template != null)
            {
                foreach (var ti in template.Items)
                {
                    var item = new KpiItem(
                        sheet.Id,
                        ti.KpiDefinition?.Group ?? KpiGroup.IndividualWork,
                        ti.KpiDefinition?.Name ?? "Chỉ tiêu nhiệm vụ",
                        ti.KpiDefinition?.Unit ?? "%",
                        ti.KpiDefinition?.ScoringMethod ?? KpiScoringMethod.HigherIsBetter,
                        ti.DefaultTarget,
                        ti.WeightPercentage,
                        ti.KpiDefinitionId,
                        ti.KpiDefinition?.Description,
                        ti.DefaultDueDate);
                    sheet.AddItem(item);
                }
            }
            else if (request.CustomItems != null)
            {
                foreach (var ci in request.CustomItems)
                {
                    var item = new KpiItem(
                        sheet.Id,
                        ci.Group,
                        ci.Title,
                        ci.Unit,
                        ci.ScoringMethod,
                        ci.TargetValue,
                        ci.WeightPercentage,
                        ci.KpiDefinitionId,
                        ci.Description,
                        ci.DueDate);
                    sheet.AddItem(item);
                }
            }

            // Giao KPI và công bố nếu đủ 100% trọng số
            decimal sumWeight = sheet.Items.Sum(i => i.WeightPercentage);
            if (sumWeight == 100m)
            {
                sheet.Publish();
            }

            await _context.KpiSheets.AddAsync(sheet, ct);
            count++;
        }

        await _context.SaveChangesAsync(ct);
        return count;
    }

    private static KpiSheetDetailDto MapToSheetDetailDto(KpiSheet s)
    {
        return new KpiSheetDetailDto
        {
            Id = s.Id,
            EmployeeId = s.EmployeeId,
            EmployeeCode = s.Employee?.EmployeeCode ?? string.Empty,
            EmployeeName = s.Employee?.FullName ?? string.Empty,
            Period = s.Period,
            LeadershipLevel = s.LeadershipLevel,
            OrganizationalUnitId = s.OrganizationalUnitId,
            OrganizationalUnitName = s.OrganizationalUnit?.UnitName ?? string.Empty,
            EvaluatorId = s.EvaluatorId,
            EvaluatorName = s.Evaluator?.FullName ?? string.Empty,
            Status = s.Status,
            StandardWorkDays = s.StandardWorkDays,
            ActualWorkDays = s.ActualWorkDays,
            IsStandardWorkingDaysMet = s.IsStandardWorkingDaysMet,
            SelfReviewLatePenalty = s.SelfReviewLatePenalty,
            ManagerReviewLatePenalty = s.ManagerReviewLatePenalty,
            HasSevereViolation = s.HasSevereViolation,
            UnitResultScore = s.UnitResultScore,
            IndividualWorkScore = s.IndividualWorkScore,
            PeopleManagementScore = s.PeopleManagementScore,
            ComplianceScore = s.ComplianceScore,
            TotalScore = s.TotalScore,
            FinalGrade = s.FinalGrade,
            BonusCoefficient = s.BonusCoefficient,
            ManagerFeedback = s.ManagerFeedback,
            CalibrationNotes = s.CalibrationNotes,
            Items = s.Items.Select(i => new KpiItemDto
            {
                Id = i.Id,
                KpiSheetId = i.KpiSheetId,
                KpiDefinitionId = i.KpiDefinitionId,
                KpiDefinitionCode = i.KpiDefinition?.Code,
                Group = i.Group,
                Title = i.Title,
                Description = i.Description,
                Unit = i.Unit,
                ScoringMethod = i.ScoringMethod,
                TargetValue = i.TargetValue,
                ActualValue = i.ActualValue,
                WeightPercentage = i.WeightPercentage,
                DueDate = i.DueDate,
                ExtendedDueDate = i.ExtendedDueDate,
                CompletedDate = i.CompletedDate,
                EvidenceUrl = i.EvidenceUrl,
                EvidenceDescription = i.EvidenceDescription,
                SelfScore = i.SelfScore,
                ManagerScore = i.ManagerScore,
                Adjustment = i.Adjustment,
                AdjustmentReason = i.AdjustmentReason,
                FinalScore = i.FinalScore,
                HasPendingExtension = i.ExtensionRequests.Any(r => r.Status == KpiExtensionStatus.Pending),
                ExtensionRequests = i.ExtensionRequests.Select(r => new KpiExtensionRequestDto
                {
                    Id = r.Id,
                    KpiItemId = r.KpiItemId,
                    OriginalDueDate = r.OriginalDueDate,
                    RequestedDueDate = r.RequestedDueDate,
                    Reason = r.Reason,
                    ObjectiveReason = r.ObjectiveReason,
                    Status = r.Status,
                    ApprovedBy = r.ApprovedBy,
                    ApprovedAt = r.ApprovedAt,
                    Comment = r.Comment
                }).ToList()
            }).ToList()
        };
    }
}
