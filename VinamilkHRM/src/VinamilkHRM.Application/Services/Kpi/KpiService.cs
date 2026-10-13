using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Application.DTOs.Kpis;
using VinamilkHRM.Application.Interfaces;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Kpis;
using VinamilkHRM.Domain.Enums;
using VinamilkHRM.Domain.Services.Kpi;

namespace VinamilkHRM.Application.Services;

public partial class KpiService : IKpiService
{
    private readonly IApplicationDbContext _context;
    private readonly IWorkCalendar _workCalendar;

    public KpiService(IApplicationDbContext context, IWorkCalendar? workCalendar = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _workCalendar = workCalendar ?? new DefaultWorkCalendar();
    }

    public async Task<List<KpiDefinitionDto>> GetDefinitionsAsync(int? jobFamilyId = null, CancellationToken ct = default)
    {
        var query = _context.KpiDefinitions
            .AsNoTracking()
            .Include(d => d.JobFamily)
            .Where(d => d.IsActive);

        if (jobFamilyId.HasValue)
        {
            query = query.Where(d => d.JobFamilyId == null || d.JobFamilyId == jobFamilyId.Value);
        }

        var list = await query.OrderBy(d => d.Code).ToListAsync(ct);
        return list.Select(MapToDefinitionDto).ToList();
    }

    public async Task<KpiDefinitionDto> GetDefinitionByIdAsync(int id, CancellationToken ct = default)
    {
        var def = await _context.KpiDefinitions
            .AsNoTracking()
            .Include(d => d.JobFamily)
            .Include(d => d.Thresholds)
            .FirstOrDefaultAsync(d => d.Id == id, ct)
            ?? throw new DomainException($"Không tìm thấy chỉ tiêu KPI với mã Id = {id}.", "ERR_KPI_DEF_NOT_FOUND");

        return MapToDefinitionDto(def);
    }

    public async Task<KpiDefinitionDto> CreateDefinitionAsync(CreateKpiDefinitionRequest request, CancellationToken ct = default)
    {
        if (await _context.KpiDefinitions.AnyAsync(d => d.Code == request.Code.Trim().ToUpperInvariant(), ct))
            throw new DomainException($"Mã chỉ tiêu {request.Code} đã tồn tại trong thư viện.", "ERR_KPI_DEF_CODE_EXISTS");

        var def = new KpiDefinition(
            request.Code,
            request.Name,
            request.Unit,
            request.Group,
            request.ScoringMethod,
            request.DataSource,
            request.JobFamilyId,
            request.ApplicableLeadershipLevel,
            request.Description,
            request.MeasurementGuide);

        if (request.ScoringMethod == KpiScoringMethod.ThresholdTable && request.Thresholds != null)
        {
            foreach (var t in request.Thresholds)
            {
                def.AddThreshold(t.MinThreshold, t.MaxThreshold, t.Score);
            }
        }

        await _context.KpiDefinitions.AddAsync(def, ct);
        await _context.SaveChangesAsync(ct);

        return MapToDefinitionDto(def);
    }

    public async Task<List<KpiSchemeDto>> GetSchemesAsync(CancellationToken ct = default)
    {
        var schemes = await _context.KpiSchemes
            .AsNoTracking()
            .OrderBy(s => s.LeadershipLevel)
            .ThenByDescending(s => s.EffectiveFrom)
            .ToListAsync(ct);

        return schemes.Select(s => new KpiSchemeDto
        {
            Id = s.Id,
            LeadershipLevel = s.LeadershipLevel,
            UnitResultWeight = s.UnitResultWeight,
            IndividualWorkWeight = s.IndividualWorkWeight,
            PeopleManagementWeight = s.PeopleManagementWeight,
            ComplianceWeight = s.ComplianceWeight,
            IsComplianceGating = s.IsComplianceGating,
            EffectiveFrom = s.EffectiveFrom,
            EffectiveTo = s.EffectiveTo,
            Description = s.Description
        }).ToList();
    }

    private static KpiDefinitionDto MapToDefinitionDto(KpiDefinition d)
    {
        return new KpiDefinitionDto
        {
            Id = d.Id,
            Code = d.Code,
            Name = d.Name,
            Unit = d.Unit,
            Group = d.Group,
            ScoringMethod = d.ScoringMethod,
            DataSource = d.DataSource,
            JobFamilyId = d.JobFamilyId,
            JobFamilyName = d.JobFamily?.FamilyName,
            ApplicableLeadershipLevel = d.ApplicableLeadershipLevel,
            Description = d.Description,
            MeasurementGuide = d.MeasurementGuide,
            IsActive = d.IsActive
        };
    }
}
