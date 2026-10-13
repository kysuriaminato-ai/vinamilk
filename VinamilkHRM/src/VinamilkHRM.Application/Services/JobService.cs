using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Application.DTOs.Jobs;
using VinamilkHRM.Application.Interfaces;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Jobs;
using VinamilkHRM.Domain.Entities.Statutory;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.Services;

public class JobService : IJobService
{
    private readonly IApplicationDbContext _context;
    private readonly IStatutoryParameterProvider _statutoryProvider;

    public JobService(
        IApplicationDbContext context,
        IStatutoryParameterProvider statutoryProvider)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _statutoryProvider = statutoryProvider ?? throw new ArgumentNullException(nameof(statutoryProvider));
    }

    public async Task<IReadOnlyList<JobTitleDto>> GetJobTitlesAsync(CancellationToken cancellationToken = default)
    {
        var titles = await _context.JobTitles
            .AsNoTracking()
            .Include(j => j.JobFamily)
            .Include(j => j.StandardJobGrade)
            .OrderBy(j => j.JobTitleCode)
            .ToListAsync(cancellationToken);

        return titles.Select(j => new JobTitleDto(
            j.Id,
            j.JobTitleCode,
            j.JobTitleName,
            j.JobFamilyId,
            j.JobFamily?.FamilyName,
            j.LeadershipLevel,
            j.StandardJobGradeId,
            j.StandardJobGrade?.GradeName,
            j.Description,
            j.IsActive
        )).ToList();
    }

    public async Task<JobTitleDto> CreateJobTitleAsync(CreateJobTitleRequest request, CancellationToken cancellationToken = default)
    {
        var codeUpper = request.Code.Trim().ToUpperInvariant();
        var exists = await _context.JobTitles.AnyAsync(j => j.JobTitleCode == codeUpper, cancellationToken);
        if (exists)
            throw new DomainException($"Mã chức danh '{codeUpper}' đã tồn tại.", "ERR_JOB_TITLE_CODE_EXISTS");

        var familyExists = await _context.JobFamilies.AnyAsync(f => f.Id == request.JobFamilyId, cancellationToken);
        if (!familyExists)
            throw new DomainException($"Không tìm thấy khối ngành với ID {request.JobFamilyId}.", "ERR_JOB_FAMILY_NOT_FOUND");

        var title = new JobTitle(
            codeUpper,
            request.Name,
            request.JobFamilyId,
            request.LeadershipLevel,
            request.StandardJobGradeId,
            request.Description
        );

        _context.JobTitles.Add(title);
        await _context.SaveChangesAsync(cancellationToken);

        return new JobTitleDto(
            title.Id,
            title.JobTitleCode,
            title.JobTitleName,
            title.JobFamilyId,
            null,
            title.LeadershipLevel,
            title.StandardJobGradeId,
            null,
            title.Description,
            title.IsActive
        );
    }

    public async Task<IReadOnlyList<JobGradeDto>> GetJobGradesAsync(CancellationToken cancellationToken = default)
    {
        var grades = await _context.JobGrades
            .AsNoTracking()
            .OrderBy(g => g.GradeNumber)
            .ToListAsync(cancellationToken);

        return grades.Select(g => new JobGradeDto(
            g.Id,
            g.GradeNumber,
            g.GradeCode,
            g.GradeName,
            g.Description,
            g.IsActive
        )).ToList();
    }

    public async Task<IReadOnlyList<JobFamilyDto>> GetJobFamiliesAsync(CancellationToken cancellationToken = default)
    {
        var families = await _context.JobFamilies
            .AsNoTracking()
            .OrderBy(f => f.FamilyCode)
            .ToListAsync(cancellationToken);

        return families.Select(f => new JobFamilyDto(
            f.Id,
            f.FamilyCode,
            f.FamilyName,
            f.Description,
            f.IsActive
        )).ToList();
    }

    public async Task<IReadOnlyList<SalaryScaleDto>> GetSalaryScalesAsync(int? jobGradeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.SalaryScales
            .AsNoTracking()
            .Include(s => s.JobGrade)
            .AsQueryable();

        if (jobGradeId.HasValue)
        {
            query = query.Where(s => s.JobGradeId == jobGradeId.Value);
        }

        var scales = await query
            .OrderBy(s => s.JobGrade!.GradeNumber)
            .ThenBy(s => s.Step)
            .ToListAsync(cancellationToken);

        return scales.Select(s => new SalaryScaleDto(
            s.Id,
            s.JobGradeId,
            s.JobGrade?.GradeNumber ?? 1,
            s.JobGrade?.GradeName,
            s.Step,
            s.Amount,
            s.EffectiveFrom,
            s.EffectiveTo,
            s.EffectiveTo == null || s.EffectiveTo >= DateOnly.FromDateTime(DateTime.UtcNow)
        )).ToList();
    }

    public async Task<SalaryScaleDto> CreateSalaryScaleAsync(CreateSalaryScaleRequest request, CancellationToken cancellationToken = default)
    {
        decimal? minWage = null;

        // Nếu có đơn vị, lấy lương tối thiểu vùng tương ứng của đơn vị tại ngày áp dụng
        if (request.UnitId.HasValue)
        {
            var unit = await _context.OrganizationalUnits
                .Include(u => u.UnitMinimumWageRegionHistories)
                .FirstOrDefaultAsync(u => u.Id == request.UnitId.Value, cancellationToken);

            if (unit != null)
            {
                var region = unit.GetMinimumWageRegionOn(request.EffectiveFrom);
                var minWageEntity = await _context.MinimumWages
                    .Where(w => w.Region == region && w.EffectiveFrom <= request.EffectiveFrom && (w.EffectiveTo == null || w.EffectiveTo >= request.EffectiveFrom))
                    .OrderByDescending(w => w.EffectiveFrom)
                    .FirstOrDefaultAsync(cancellationToken);

                if (minWageEntity != null)
                {
                    minWage = minWageEntity.MonthlyAmount;
                }
            }
        }
        else
        {
            // Mặc định kiểm tra với mức lương tối thiểu Vùng IV (mức thấp nhất toàn quốc)
            var defaultMinWage = await _context.MinimumWages
                .Where(w => w.Region == MinimumWageRegion.RegionIV && w.EffectiveFrom <= request.EffectiveFrom && (w.EffectiveTo == null || w.EffectiveTo >= request.EffectiveFrom))
                .OrderByDescending(w => w.EffectiveFrom)
                .FirstOrDefaultAsync(cancellationToken);

            if (defaultMinWage != null)
            {
                minWage = defaultMinWage.MonthlyAmount;
            }
        }

        // Domain constructor sẽ kiểm tra P1 >= minWage, ném DomainException("ERR_SAL_BELOW_MIN_WAGE") nếu vi phạm
        var scale = new SalaryScale(
            jobGradeId: request.JobGradeId,
            step: request.Step,
            amount: request.Amount,
            effectiveFrom: request.EffectiveFrom,
            effectiveTo: request.EffectiveTo,
            minimumWage: minWage
        );

        _context.SalaryScales.Add(scale);
        await _context.SaveChangesAsync(cancellationToken);

        var grade = await _context.JobGrades.FindAsync(new object[] { scale.JobGradeId }, cancellationToken);

        return new SalaryScaleDto(
            scale.Id,
            scale.JobGradeId,
            grade?.GradeNumber ?? 1,
            grade?.GradeName,
            scale.Step,
            scale.Amount,
            scale.EffectiveFrom,
            scale.EffectiveTo,
            true
        );
    }
}
