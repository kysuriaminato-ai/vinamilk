using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Application.Common.Security;
using VinamilkHRM.Application.DTOs.Statutory;
using VinamilkHRM.Domain.Entities.Statutory;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.WebApi.Controllers;

[ApiController]
[Route("api/v1/statutory")]
[Authorize]
public class StatutoryController : ControllerBase
{
    private readonly IStatutoryParameterProvider _parameterProvider;
    private readonly IApplicationDbContext _context;

    public StatutoryController(
        IStatutoryParameterProvider parameterProvider,
        IApplicationDbContext context)
    {
        _parameterProvider = parameterProvider ?? throw new ArgumentNullException(nameof(parameterProvider));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [HttpGet("parameter")]
    [Authorize(Policy = Permissions.TienluongView)]
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetParameter(
        [FromQuery] string code,
        [FromQuery] DateOnly? onDate,
        CancellationToken cancellationToken)
    {
        var effectiveDate = onDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var val = await _parameterProvider.GetAsync(code, effectiveDate, cancellationToken);
        return Ok(new { Code = code, EffectiveDate = effectiveDate, Value = val });
    }

    [HttpGet("minimum-wages")]
    [Authorize(Policy = Permissions.TienluongView)]
    [ProducesResponseType(typeof(IReadOnlyList<MinimumWageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMinimumWages(
        [FromQuery] DateOnly? onDate,
        CancellationToken cancellationToken)
    {
        var effectiveDate = onDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var query = _context.MinimumWages
            .AsNoTracking()
            .Where(w => w.EffectiveFrom <= effectiveDate && (w.EffectiveTo == null || w.EffectiveTo >= effectiveDate))
            .OrderBy(w => w.Region);

        var list = await query.ToListAsync(cancellationToken);
        var dtos = list.Select(w => new MinimumWageDto(
            w.Id,
            w.Region,
            w.MonthlyAmount,
            w.HourlyAmount,
            w.EffectiveFrom,
            w.EffectiveTo,
            w.LegalReference
        )).ToList();

        return Ok(dtos);
    }

    [HttpGet("tax-brackets")]
    [Authorize(Policy = Permissions.TienluongView)]
    [ProducesResponseType(typeof(IReadOnlyList<TaxBracketDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTaxBrackets(
        [FromQuery] string scheduleCode = "PIT_2026",
        [FromQuery] DateOnly? onDate = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveDate = onDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var brackets = await _parameterProvider.GetTaxBracketsAsync(scheduleCode, effectiveDate, cancellationToken);

        var dtos = brackets.Select(b => new TaxBracketDto(
            b.Id,
            b.ScheduleCode,
            b.Order,
            b.UpperBoundMonthly,
            b.Rate,
            b.EffectiveFrom,
            b.EffectiveTo
        )).ToList();

        return Ok(dtos);
    }

    [HttpGet("insurance-rates")]
    [Authorize(Policy = Permissions.TienluongView)]
    [ProducesResponseType(typeof(IReadOnlyList<InsuranceRateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInsuranceRates(
        [FromQuery] DateOnly? onDate,
        CancellationToken cancellationToken)
    {
        var effectiveDate = onDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var rates = await _context.InsuranceRates
            .AsNoTracking()
            .Where(r => r.EffectiveFrom <= effectiveDate && (r.EffectiveTo == null || r.EffectiveTo >= effectiveDate))
            .ToListAsync(cancellationToken);

        var dtos = rates.Select(r => new InsuranceRateDto(
            r.Id,
            r.Kind,
            r.EmployeeRate,
            r.EmployerRate,
            r.EffectiveFrom,
            r.EffectiveTo
        )).ToList();

        return Ok(dtos);
    }

    [HttpGet("overtime-rates")]
    [Authorize(Policy = Permissions.TienluongView)]
    [ProducesResponseType(typeof(IReadOnlyList<OvertimeRateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOvertimeRates(CancellationToken cancellationToken)
    {
        var rates = await _context.OvertimeRates
            .AsNoTracking()
            .OrderBy(o => o.DayKind)
            .ThenBy(o => o.Night)
            .ToListAsync(cancellationToken);

        var dtos = rates.Select(o => new OvertimeRateDto(
            o.Id,
            o.DayKind,
            o.Night,
            o.Multiplier
        )).ToList();

        return Ok(dtos);
    }

    [HttpGet("public-holidays")]
    [Authorize(Policy = Permissions.TienluongView)]
    [ProducesResponseType(typeof(IReadOnlyList<PublicHolidayDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublicHolidays(
        [FromQuery] int? year,
        CancellationToken cancellationToken)
    {
        int targetYear = year ?? DateTime.UtcNow.Year;
        var start = new DateOnly(targetYear, 1, 1);
        var end = new DateOnly(targetYear, 12, 31);

        var holidays = await _context.PublicHolidays
            .AsNoTracking()
            .Where(h => h.Date >= start && h.Date <= end)
            .OrderBy(h => h.Date)
            .ToListAsync(cancellationToken);

        var dtos = holidays.Select(h => new PublicHolidayDto(
            h.Id,
            h.Date,
            h.Name,
            h.IsPaid
        )).ToList();

        return Ok(dtos);
    }
}
