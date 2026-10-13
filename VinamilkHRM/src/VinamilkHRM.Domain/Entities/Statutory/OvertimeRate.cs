using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Statutory;

/// <summary>
/// Hệ số lương làm thêm giờ (Overtime) theo Bộ luật Lao động Việt Nam.
/// </summary>
public class OvertimeRate : BaseAuditableEntity
{
    public OvertimeDayKind DayKind { get; private set; }
    public bool Night { get; private set; }
    public decimal Multiplier { get; private set; } // 1.50, 2.00, 2.70, 3.00, 3.90
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public string? LegalReference { get; private set; }

    protected OvertimeRate()
    {
    }

    public OvertimeRate(
        OvertimeDayKind dayKind,
        bool night,
        decimal multiplier,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo = null,
        string? legalReference = null)
    {
        if (multiplier < 1.0m)
            throw new DomainException("Hệ số tăng ca không thể nhỏ hơn 1.0 (100%).", "ERR_OT_MULTIPLIER_INVALID");

        if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom)
            throw new DomainException("Ngày hết hiệu lực không thể trước ngày bắt đầu.", "ERR_OT_DATES_INVALID");

        DayKind = dayKind;
        Night = night;
        Multiplier = multiplier;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        LegalReference = legalReference?.Trim();
    }
}
