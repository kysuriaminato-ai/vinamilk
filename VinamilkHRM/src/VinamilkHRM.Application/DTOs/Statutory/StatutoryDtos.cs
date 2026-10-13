using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.DTOs.Statutory;

public record StatutoryParameterDto(
    int Id,
    string Code,
    decimal Value,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string? LegalReference,
    string? Description);

public record MinimumWageDto(
    int Id,
    MinimumWageRegion Region,
    decimal MonthlyAmount,
    decimal HourlyAmount,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string? LegalReference);

public record TaxBracketDto(
    int Id,
    string ScheduleCode,
    int Order,
    decimal? UpperBoundMonthly,
    decimal Rate,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo);

public record InsuranceRateDto(
    int Id,
    InsuranceRateKind Kind,
    decimal EmployeeRate,
    decimal EmployerRate,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo);

public record OvertimeRateDto(
    int Id,
    OvertimeDayKind DayKind,
    bool IsNight,
    decimal Multiplier);

public record PublicHolidayDto(
    int Id,
    DateOnly Date,
    string Name,
    bool IsPaid);
