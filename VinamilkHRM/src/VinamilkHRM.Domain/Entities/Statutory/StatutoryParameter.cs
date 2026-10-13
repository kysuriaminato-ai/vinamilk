using VinamilkHRM.Domain.Common;

namespace VinamilkHRM.Domain.Entities.Statutory;

/// <summary>
/// Bảng tham số pháp lý có hiệu lực theo thời gian (giảm trừ gia cảnh bản thân, người phụ thuộc, lương cơ sở, trần bảo hiểm...).
/// Tuyệt đối không hard-code các giá trị này trong mã nguồn.
/// </summary>
public class StatutoryParameter : BaseAuditableEntity
{
    public string Code { get; private set; } = string.Empty;
    public decimal Value { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public string? LegalReference { get; private set; }
    public string? Description { get; private set; }

    protected StatutoryParameter()
    {
    }

    public StatutoryParameter(
        string code,
        decimal value,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo = null,
        string? legalReference = null,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Mã tham số pháp lý không được để trống.", "ERR_STAT_PARAM_CODE_EMPTY");

        if (value < 0)
            throw new DomainException("Giá trị tham số pháp lý không thể là số âm.", "ERR_STAT_PARAM_VALUE_NEGATIVE");

        if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom)
            throw new DomainException("Ngày hết hiệu lực không thể trước ngày bắt đầu hiệu lực.", "ERR_STAT_PARAM_DATES_INVALID");

        Code = code.Trim().ToUpperInvariant();
        Value = value;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        LegalReference = legalReference?.Trim();
        Description = description?.Trim();
    }

    public void UpdateValue(decimal newValue, string? legalReference, string? description)
    {
        if (newValue < 0)
            throw new DomainException("Giá trị tham số pháp lý không thể là số âm.", "ERR_STAT_PARAM_VALUE_NEGATIVE");

        Value = newValue;
        LegalReference = legalReference?.Trim();
        Description = description?.Trim();
    }

    public void CloseEffectivity(DateOnly effectiveTo)
    {
        if (effectiveTo < EffectiveFrom)
            throw new DomainException("Ngày đóng hiệu lực không thể trước ngày bắt đầu.", "ERR_STAT_PARAM_DATES_INVALID");

        EffectiveTo = effectiveTo;
    }
}
