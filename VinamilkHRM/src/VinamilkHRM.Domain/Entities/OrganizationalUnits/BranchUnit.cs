using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.OrganizationalUnits;

/// <summary>
/// Chi nhánh khu vực phụ trách kinh doanh và phân phối (Chi nhánh Hà Nội, Cần Thơ, Đà Nẵng, Bình Dương...).
/// </summary>
public class BranchUnit : OrganizationalUnit
{
    public string BranchTerritory { get; private set; } = string.Empty;

    protected BranchUnit()
    {
    }

    public BranchUnit(
        string unitCode,
        string unitName,
        string address,
        string phoneNumber,
        string email,
        string taxCode,
        string branchTerritory,
        int? parentUnitId = null,
        int regionId = 1,
        string regionName = "Miền Bắc",
        string costCenterCode = "CC_BRANCH",
        WorkPattern workPattern = WorkPattern.OfficeWeek,
        MinimumWageRegion minimumWageRegion = MinimumWageRegion.RegionI,
        int plannedHeadcount = 100)
        : base(unitCode, unitName, UnitType.Branch, address, phoneNumber, email, taxCode, parentUnitId, regionId, regionName, costCenterCode, workPattern, minimumWageRegion, plannedHeadcount)
    {
        BranchTerritory = branchTerritory?.Trim() ?? string.Empty;
    }
}
