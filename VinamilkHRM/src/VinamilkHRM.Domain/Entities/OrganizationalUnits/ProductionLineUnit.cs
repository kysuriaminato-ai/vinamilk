using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.OrganizationalUnits;

/// <summary>
/// Phân xưởng / Dây chuyền sản xuất / Tổ sản xuất trực thuộc Nhà máy (Mega Factory, Thống Nhất...).
/// </summary>
public class ProductionLineUnit : OrganizationalUnit
{
    public int LineNumber { get; private set; }
    public decimal DesignedCapacityPerHour { get; private set; } // Công suất thiết kế hộp/chai mỗi giờ

    protected ProductionLineUnit()
    {
    }

    public ProductionLineUnit(
        string unitCode,
        string unitName,
        string address,
        string phoneNumber,
        string email,
        string taxCode,
        int lineNumber,
        decimal designedCapacityPerHour,
        int? parentUnitId = null,
        int regionId = 3,
        string regionName = "Miền Nam",
        string costCenterCode = "CC_LINE",
        WorkPattern workPattern = WorkPattern.ThreeShiftRotation,
        MinimumWageRegion minimumWageRegion = MinimumWageRegion.RegionI,
        int plannedHeadcount = 30)
        : base(unitCode, unitName, UnitType.ProductionLine, address, phoneNumber, email, taxCode, parentUnitId, regionId, regionName, costCenterCode, workPattern, minimumWageRegion, plannedHeadcount)
    {
        LineNumber = lineNumber;
        DesignedCapacityPerHour = designedCapacityPerHour;
    }
}
