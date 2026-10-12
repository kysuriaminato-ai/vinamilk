using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.DTOs.Careers;

public record SubmitApplicationRequest(
    int JobPostingId,
    string FullName,
    string Email,
    string PhoneNumber,
    string CitizenId,
    DateTime DateOfBirth,
    Gender Gender,
    string Address,
    string ResumeFileName,
    string CoverLetter = "",
    string PortfolioUrl = ""
);

public record UpdateApplicationStatusRequest(
    int ApplicationId,
    JobApplicationStatus NewStatus,
    DateTime? InterviewDate = null,
    decimal? InterviewScore = null,
    string? Notes = null
);

public record OnboardCandidateRequest(
    int ApplicationId,
    int OrganizationalUnitId,
    decimal OfficialBasicSalary,
    decimal Allowance = 2000000,
    int? AssignedManagerId = null
);

public record OnboardingResultDto(
    bool Success,
    int ApplicationId,
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    string PositionTitle,
    string UnitName,
    decimal ProbationSalary,
    decimal OfficialSalary,
    string GeneratedEssUsername,
    string TempEssPassword,
    string Message
);
