using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Statutory;

/// <summary>
/// Mức lương tối thiểu vùng theo tháng và theo giờ quy định tại Nghị định của Chính phủ.
/// Dùng làm căn cứ sàn lương vị trí P1 và mức trần bảo hiểm thất nghiệp.
/// </summary>
public class MinimumWage : BaseAuditableEntity
{
    public MinimumWageRegion Region { get; private set; }
    public decimal MonthlyAmount { get; private set; }
    public decimal HourlyAmount { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public string? LegalReference { get; private set; }

    protected MinimumWage()
    {
    }

    public MinimumWage(
        MinimumWageRegion region,
        decimal monthlyAmount,
        decimal hourlyAmount,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo = null,
        string? legalReference = null)
    {
        if (monthlyAmount <= 0 || hourlyAmount <= 0)
            throw new DomainException("Mức lương tối thiểu vùng theo tháng và theo giờ phải lớn hơn 0.", "ERR_MIN_WAGE_INVALID");

        if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom)
            throw new DomainException("Ngày hết hiệu lực không thể trước ngày bắt đầu.", "ERR_MIN_WAGE_DATES_INVALID");

        Region = region;
        MonthlyAmount = monthlyAmount;
        HourlyAmount = hourlyAmount;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        LegalReference = legalReference?.Trim();
    }

    public void CloseEffectivity(DateOnly effectiveTo)
    {
        if (effectiveTo < EffectiveFrom)
            throw new DomainException("Ngày đóng hiệu lực không thể trước ngày bắt đầu.", "ERR_MIN_WAGE_DATES_INVALID");

        EffectiveTo = effectiveTo;
    }
}
