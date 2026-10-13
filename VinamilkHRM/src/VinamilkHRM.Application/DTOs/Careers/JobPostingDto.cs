using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.DTOs.Careers;

public record JobPostingDto(
    int Id,
    string JobCode,
    string Title,
    string Slug,
    UnitType UnitType,
    string UnitTypeName,
    string Department,
    string WorkLocation,
    string City,
    int RegionId,
    string RegionName,
    string EmploymentType,
    string ExperienceLevel,
    string EducationRequirement,
    decimal? SalaryMin,
    decimal? SalaryMax,
    string SalaryDisplay,
    DateTime DatePosted,
    DateTime ValidThrough,
    string Description,
    string Requirements,
    string Benefits,
    bool IsActive,
    bool IsFeatured,
    int ViewCount,
    int ApplicationCount
);
