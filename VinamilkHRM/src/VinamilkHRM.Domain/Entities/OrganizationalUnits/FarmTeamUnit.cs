using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.OrganizationalUnits;

/// <summary>
/// Tổ đội sản xuất trực thuộc Trang trại bò sữa (Tổ vắt sữa, Đội thú y, Tổ chăm sóc bê, Đội cơ giới & thức ăn...).
/// </summary>
public class FarmTeamUnit : OrganizationalUnit
{
    public string FarmSpecialty { get; private set; } = string.Empty;

    protected FarmTeamUnit()
    {
    }

    public FarmTeamUnit(
        string unitCode,
        string unitName,
        string address,
        string phoneNumber,
        string email,
        string taxCode,
        string farmSpecialty,
        int? parentUnitId = null,
        int regionId = 3,
        string regionName = "Miền Nam",
        string costCenterCode = "CC_FARM_TEAM",
        WorkPattern workPattern = WorkPattern.FarmSplitShift,
        MinimumWageRegion minimumWageRegion = MinimumWageRegion.RegionII,
        int plannedHeadcount = 25)
        : base(unitCode, unitName, UnitType.FarmTeam, address, phoneNumber, email, taxCode, parentUnitId, regionId, regionName, costCenterCode, workPattern, minimumWageRegion, plannedHeadcount)
    {
        FarmSpecialty = farmSpecialty?.Trim() ?? string.Empty;
    }
}
