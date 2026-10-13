using VinamilkHRM.Application.DTOs.Jobs;

namespace VinamilkHRM.Application.Interfaces;

public interface IJobService
{
    Task<IReadOnlyList<JobTitleDto>> GetJobTitlesAsync(CancellationToken cancellationToken = default);
    Task<JobTitleDto> CreateJobTitleAsync(CreateJobTitleRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JobGradeDto>> GetJobGradesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JobFamilyDto>> GetJobFamiliesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalaryScaleDto>> GetSalaryScalesAsync(int? jobGradeId = null, CancellationToken cancellationToken = default);
    Task<SalaryScaleDto> CreateSalaryScaleAsync(CreateSalaryScaleRequest request, CancellationToken cancellationToken = default);
}
