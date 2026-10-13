using VinamilkHRM.Domain.Common;

namespace VinamilkHRM.Domain.Entities.Statutory;

/// <summary>
/// Bậc thuế trong biểu thuế lũy tiến từng phần (Luật Thuế TNCN).
/// Hỗ trợ cả biểu 7 bậc (trước 2026) và biểu 5 bậc (từ 01/01/2026).
/// </summary>
public class TaxBracket : BaseAuditableEntity
{
    public string ScheduleCode { get; private set; } = "PIT_REGULAR"; // Biểu tính thuế
    public int Order { get; private set; }                            // Thứ tự bậc (1, 2, 3...)
    public decimal? UpperBoundMonthly { get; private set; }           // Ngưỡng thu nhập tính thuế tháng (null: không giới hạn)
    public decimal Rate { get; private set; }                          // Thuế suất (0.05, 0.10, 0.15, 0.20, 0.25, 0.30, 0.35)
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public string? Description { get; private set; }

    protected TaxBracket()
    {
    }

    public TaxBracket(
        string scheduleCode,
        int order,
        decimal? upperBoundMonthly,
        decimal rate,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo = null,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(scheduleCode))
            throw new DomainException("Mã biểu thuế không được để trống.", "ERR_TAX_BRACKET_CODE_EMPTY");

        if (order <= 0)
            throw new DomainException("Thứ tự bậc thuế phải lớn hơn 0.", "ERR_TAX_BRACKET_ORDER_INVALID");

        if (rate < 0 || rate > 1)
            throw new DomainException("Thuế suất phải nằm trong khoảng từ 0% đến 100% (0.0 đến 1.0).", "ERR_TAX_BRACKET_RATE_INVALID");

        if (upperBoundMonthly.HasValue && upperBoundMonthly.Value <= 0)
            throw new DomainException("Ngưỡng thu nhập tính thuế phải lớn hơn 0.", "ERR_TAX_BRACKET_UPPER_BOUND_INVALID");

        if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom)
            throw new DomainException("Ngày hết hiệu lực không thể trước ngày bắt đầu.", "ERR_TAX_BRACKET_DATES_INVALID");

        ScheduleCode = scheduleCode.Trim().ToUpperInvariant();
        Order = order;
        UpperBoundMonthly = upperBoundMonthly;
        Rate = rate;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        Description = description?.Trim();
    }
}
