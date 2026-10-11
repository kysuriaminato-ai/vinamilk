using VinamilkHRM.Application.DTOs.Careers;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.Interfaces;

/// <summary>
/// Dịch vụ quản lý Cổng thông tin Tuyển dụng Vinamilk (Careers Portal) và luồng tự động Onboard ứng viên.
/// </summary>
public interface ICareersService
{
    Task<IReadOnlyList<JobPostingDto>> GetActiveJobPostingsAsync(
        string? keyword = null,
        UnitType? unitType = null,
        string? city = null,
        CancellationToken cancellationToken = default);

    Task<JobPostingDto?> GetJobPostingBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<JobPostingDto?> GetJobPostingByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<CandidateApplicationDto> SubmitApplicationAsync(SubmitApplicationRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CandidateApplicationDto>> GetApplicationsAsync(
        int? jobPostingId = null,
        JobApplicationStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<CandidateApplicationDto?> GetApplicationByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<CandidateApplicationDto> UpdateApplicationStatusAsync(UpdateApplicationStatusRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kích hoạt tự động luồng chuyển dữ liệu ứng viên trúng tuyển sang Nhân viên thử việc (Probation Employee),
    /// tự sinh mã nhân viên chuẩn quy ước, lập hợp đồng thử việc và tạo thông tin tài khoản Cổng ESS.
    /// </summary>
    Task<OnboardingResultDto> OnboardHiredCandidateAsync(OnboardCandidateRequest request, CancellationToken cancellationToken = default);
}
