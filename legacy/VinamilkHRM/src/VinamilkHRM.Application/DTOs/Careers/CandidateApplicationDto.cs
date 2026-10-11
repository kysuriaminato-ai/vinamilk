using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.DTOs.Careers;

public record CandidateApplicationDto(
    int Id,
    int JobPostingId,
    string JobTitle,
    string JobCode,
    string FullName,
    string Email,
    string PhoneNumber,
    string CitizenId,
    DateTime DateOfBirth,
    Gender Gender,
    string Address,
    string ResumeFileName,
    string CoverLetter,
    string PortfolioUrl,
    JobApplicationStatus Status,
    string StatusName,
    DateTime AppliedAt,
    DateTime? InterviewDate,
    decimal? InterviewScore,
    string? EvaluationNotes,
    int? HiredEmployeeId,
    string? HiredEmployeeCode,
    DateTime? HiredDate
);
