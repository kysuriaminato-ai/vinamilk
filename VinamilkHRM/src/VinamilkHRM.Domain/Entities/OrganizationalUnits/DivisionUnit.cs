using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.OrganizationalUnits;

/// <summary>
/// Khối chức năng cấp Tập đoàn (Khối Sản xuất, Khối Tài chính, Khối Nhân sự, Khối Chuỗi Cung ứng, Khối Kinh doanh nội địa/quốc tế...).
/// </summary>
public class DivisionUnit : OrganizationalUnit
{
    public string DivisionScope { get; private set; } = string.Empty;

    protected DivisionUnit()
    {
    }

    public DivisionUnit(
        string unitCode,
        string unitName,
        string address,
        string phoneNumber,
        string email,
        string taxCode,
        int? parentUnitId = null,
        int regionId = 3,
        string regionName = "Miền Nam",
        string costCenterCode = "CC_DIV",
        WorkPattern workPattern = WorkPattern.OfficeWeek,
        MinimumWageRegion minimumWageRegion = MinimumWageRegion.RegionI,
        int plannedHeadcount = 50,
        string divisionScope = "Toàn quốc")
        : base(unitCode, unitName, UnitType.Division, address, phoneNumber, email, taxCode, parentUnitId, regionId, regionName, costCenterCode, workPattern, minimumWageRegion, plannedHeadcount)
    {
        DivisionScope = divisionScope?.Trim() ?? "Toàn quốc";
    }
}
