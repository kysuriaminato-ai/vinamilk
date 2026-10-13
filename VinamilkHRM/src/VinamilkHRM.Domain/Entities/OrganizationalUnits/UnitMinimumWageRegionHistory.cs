using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.OrganizationalUnits;

/// <summary>
/// Lịch sử phân vùng lương tối thiểu của đơn vị tổ chức (Vùng I - IV) có ngày hiệu lực.
/// Địa giới hành chính có thể thay đổi sau sắp xếp, sáp nhập đơn vị hành chính.
/// </summary>
public class UnitMinimumWageRegionHistory : BaseAuditableEntity
{
    public int OrganizationalUnitId { get; private set; }
    public virtual OrganizationalUnit? OrganizationalUnit { get; private set; }

    public MinimumWageRegion MinimumWageRegion { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public string? Reason { get; private set; }
    public string? LegalReference { get; private set; }

    protected UnitMinimumWageRegionHistory()
    {
    }

    public UnitMinimumWageRegionHistory(
        int organizationalUnitId,
        MinimumWageRegion minimumWageRegion,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo = null,
        string? reason = null,
        string? legalReference = null)
    {
        if (organizationalUnitId < 0)
            throw new DomainException("Mã đơn vị tổ chức không hợp lệ.", "ERR_UNIT_REGION_UNIT_INVALID");

        if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom)
            throw new DomainException("Ngày hết hiệu lực không thể trước ngày bắt đầu.", "ERR_UNIT_REGION_DATES_INVALID");

        OrganizationalUnitId = organizationalUnitId;
        MinimumWageRegion = minimumWageRegion;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        Reason = reason?.Trim();
        LegalReference = legalReference?.Trim();
    }

    public void CloseEffectivity(DateOnly effectiveTo)
    {
        if (effectiveTo < EffectiveFrom)
            throw new DomainException("Ngày đóng hiệu lực không thể trước ngày bắt đầu.", "ERR_UNIT_REGION_DATES_INVALID");

        EffectiveTo = effectiveTo;
    }
}
