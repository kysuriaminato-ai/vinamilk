using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.OrganizationalUnits;

/// <summary>
/// Phòng ban chức năng trực thuộc Khối hoặc Nhà máy (Phòng Kế toán, Phòng Nhân sự, Phòng Kỹ thuật, Ban Pháp chế...).
/// </summary>
public class DepartmentUnit : OrganizationalUnit
{
    public string FunctionDescription { get; private set; } = string.Empty;

    protected DepartmentUnit()
    {
    }

    public DepartmentUnit(
        string unitCode,
        string unitName,
        string address,
        string phoneNumber,
        string email,
        string taxCode,
        int? parentUnitId = null,
        int regionId = 3,
        string regionName = "Miền Nam",
        string costCenterCode = "CC_DEPT",
        WorkPattern workPattern = WorkPattern.OfficeWeek,
        MinimumWageRegion minimumWageRegion = MinimumWageRegion.RegionI,
        int plannedHeadcount = 20,
        string functionDescription = "")
        : base(unitCode, unitName, UnitType.Department, address, phoneNumber, email, taxCode, parentUnitId, regionId, regionName, costCenterCode, workPattern, minimumWageRegion, plannedHeadcount)
    {
        FunctionDescription = functionDescription?.Trim() ?? string.Empty;
    }
}
