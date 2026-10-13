using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Statutory;
using VinamilkHRM.Domain.Enums;
using VinamilkHRM.Infrastructure.Data;

namespace VinamilkHRM.Infrastructure.Services;

/// <summary>
/// Triển khai IStatutoryParameterProvider nạp tham số pháp lý từ CSDL SQL Server có bộ đệm IMemoryCache.
/// Ném ra DomainException("ERR_STATUTORY_PARAM_NOT_FOUND") nếu không tìm thấy tham số có hiệu lực.
/// </summary>
public class StatutoryParameterProvider : IStatutoryParameterProvider
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    public StatutoryParameterProvider(IServiceScopeFactory scopeFactory, IMemoryCache cache)
    {
        _scopeFactory = scopeFactory;
        _cache = cache;
    }

    public decimal Get(string code, DateOnly onDate)
    {
        return GetAsync(code, onDate).GetAwaiter().GetResult();
    }

    public async Task<decimal> GetAsync(string code, DateOnly onDate, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Mã tham số pháp lý không được để trống.", "ERR_STATUTORY_PARAM_CODE_EMPTY");

        string normalizedCode = code.Trim().ToUpperInvariant();
        string cacheKey = $"StatParam_{normalizedCode}_{onDate:yyyyMMdd}";

        if (_cache.TryGetValue(cacheKey, out decimal cachedValue))
        {
            return cachedValue;
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VinamilkHRMDbContext>();

        var param = await db.StatutoryParameters
            .AsNoTracking()
            .Where(p => p.Code == normalizedCode && p.EffectiveFrom <= onDate && (p.EffectiveTo == null || p.EffectiveTo >= onDate))
            .OrderByDescending(p => p.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

        if (param == null)
        {
            throw new DomainException(
                $"Không tìm thấy tham số pháp lý [{normalizedCode}] có hiệu lực tại ngày {onDate:yyyy-MM-dd}.",
                "ERR_STATUTORY_PARAM_NOT_FOUND");
        }

        _cache.Set(cacheKey, param.Value, CacheDuration);
        return param.Value;
    }

    public MinimumWage GetMinimumWage(MinimumWageRegion region, DateOnly onDate)
    {
        return GetMinimumWageAsync(region, onDate).GetAwaiter().GetResult();
    }

    public async Task<MinimumWage> GetMinimumWageAsync(MinimumWageRegion region, DateOnly onDate, CancellationToken cancellationToken = default)
    {
        string cacheKey = $"MinWage_{region}_{onDate:yyyyMMdd}";

        if (_cache.TryGetValue(cacheKey, out MinimumWage? cachedWage) && cachedWage != null)
        {
            return cachedWage;
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VinamilkHRMDbContext>();

        var wage = await db.MinimumWages
            .AsNoTracking()
            .Where(m => m.Region == region && m.EffectiveFrom <= onDate && (m.EffectiveTo == null || m.EffectiveTo >= onDate))
            .OrderByDescending(m => m.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

        if (wage == null)
        {
            throw new DomainException(
                $"Không tìm thấy mức lương tối thiểu cho {region} tại ngày {onDate:yyyy-MM-dd}.",
                "ERR_STATUTORY_PARAM_NOT_FOUND");
        }

        _cache.Set(cacheKey, wage, CacheDuration);
        return wage;
    }

    public IReadOnlyList<TaxBracket> GetTaxBrackets(string scheduleCode, DateOnly onDate)
    {
        return GetTaxBracketsAsync(scheduleCode, onDate).GetAwaiter().GetResult();
    }

    public async Task<IReadOnlyList<TaxBracket>> GetTaxBracketsAsync(string scheduleCode, DateOnly onDate, CancellationToken cancellationToken = default)
    {
        string normalizedSchedule = scheduleCode.Trim().ToUpperInvariant();
        string cacheKey = $"TaxBrackets_{normalizedSchedule}_{onDate:yyyyMMdd}";

        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<TaxBracket>? cachedBrackets) && cachedBrackets != null)
        {
            return cachedBrackets;
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VinamilkHRMDbContext>();

        var brackets = await db.TaxBrackets
            .AsNoTracking()
            .Where(t => t.ScheduleCode == normalizedSchedule && t.EffectiveFrom <= onDate && (t.EffectiveTo == null || t.EffectiveTo >= onDate))
            .OrderBy(t => t.Order)
            .ToListAsync(cancellationToken);

        if (brackets.Count == 0)
        {
            throw new DomainException(
                $"Không tìm thấy biểu thuế [{normalizedSchedule}] có hiệu lực tại ngày {onDate:yyyy-MM-dd}.",
                "ERR_STATUTORY_PARAM_NOT_FOUND");
        }

        _cache.Set(cacheKey, brackets, CacheDuration);
        return brackets;
    }

    public IReadOnlyList<InsuranceRate> GetInsuranceRates(DateOnly onDate)
    {
        return GetInsuranceRatesAsync(onDate).GetAwaiter().GetResult();
    }

    public async Task<IReadOnlyList<InsuranceRate>> GetInsuranceRatesAsync(DateOnly onDate, CancellationToken cancellationToken = default)
    {
        string cacheKey = $"InsRates_{onDate:yyyyMMdd}";

        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<InsuranceRate>? cachedRates) && cachedRates != null)
        {
            return cachedRates;
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VinamilkHRMDbContext>();

        var rates = await db.InsuranceRates
            .AsNoTracking()
            .Where(r => r.EffectiveFrom <= onDate && (r.EffectiveTo == null || r.EffectiveTo >= onDate))
            .ToListAsync(cancellationToken);

        if (rates.Count == 0)
        {
            throw new DomainException(
                $"Không tìm thấy tỷ lệ trích nộp bảo hiểm có hiệu lực tại ngày {onDate:yyyy-MM-dd}.",
                "ERR_STATUTORY_PARAM_NOT_FOUND");
        }

        _cache.Set(cacheKey, rates, CacheDuration);
        return rates;
    }

    public decimal GetOvertimeMultiplier(OvertimeDayKind dayKind, bool isNight, DateOnly onDate)
    {
        return GetOvertimeMultiplierAsync(dayKind, isNight, onDate).GetAwaiter().GetResult();
    }

    public async Task<decimal> GetOvertimeMultiplierAsync(OvertimeDayKind dayKind, bool isNight, DateOnly onDate, CancellationToken cancellationToken = default)
    {
        string cacheKey = $"OTMultiplier_{dayKind}_{isNight}_{onDate:yyyyMMdd}";

        if (_cache.TryGetValue(cacheKey, out decimal cachedMultiplier))
        {
            return cachedMultiplier;
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VinamilkHRMDbContext>();

        var rate = await db.OvertimeRates
            .AsNoTracking()
            .Where(o => o.DayKind == dayKind && o.Night == isNight && o.EffectiveFrom <= onDate && (o.EffectiveTo == null || o.EffectiveTo >= onDate))
            .OrderByDescending(o => o.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

        if (rate == null)
        {
            throw new DomainException(
                $"Không tìm thấy hệ số làm thêm giờ cho {dayKind} (Đêm: {isNight}) tại ngày {onDate:yyyy-MM-dd}.",
                "ERR_STATUTORY_PARAM_NOT_FOUND");
        }

        _cache.Set(cacheKey, rate.Multiplier, CacheDuration);
        return rate.Multiplier;
    }

    public bool IsPublicHoliday(DateOnly date)
    {
        return IsPublicHolidayAsync(date).GetAwaiter().GetResult();
    }

    public async Task<bool> IsPublicHolidayAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        string cacheKey = $"Holiday_{date:yyyyMMdd}";

        if (_cache.TryGetValue(cacheKey, out bool isHoliday))
        {
            return isHoliday;
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VinamilkHRMDbContext>();

        bool exists = await db.PublicHolidays
            .AsNoTracking()
            .AnyAsync(h => h.Date == date && h.IsPaid, cancellationToken);

        _cache.Set(cacheKey, exists, CacheDuration);
        return exists;
    }
}
