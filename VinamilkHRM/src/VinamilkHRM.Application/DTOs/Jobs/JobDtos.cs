using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.DTOs.Jobs;

public record JobFamilyDto(
    int Id,
    string FamilyCode,
    string FamilyName,
    string? Description,
    bool IsActive);

public record JobGradeDto(
    int Id,
    int GradeNumber,
    string GradeCode,
    string GradeName,
    string? Description,
    bool IsActive);

public record JobTitleDto(
    int Id,
    string Code,
    string Name,
    int JobFamilyId,
    string? JobFamilyName,
    LeadershipLevel LeadershipLevel,
    int? StandardJobGradeId,
    string? StandardJobGradeName,
    string? Description,
    bool IsActive);

public record CreateJobTitleRequest(
    string Code,
    string Name,
    int JobFamilyId,
    LeadershipLevel LeadershipLevel,
    int? StandardJobGradeId = null,
    string? Description = null);

public record SalaryScaleDto(
    int Id,
    int JobGradeId,
    int GradeNumber,
    string? GradeName,
    int Step,
    decimal Amount,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    bool IsActive);

public record CreateSalaryScaleRequest(
    int JobGradeId,
    int Step,
    decimal Amount,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo = null,
    int? UnitId = null);
