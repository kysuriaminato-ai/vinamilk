using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.OrganizationalUnits;

/// <summary>
/// Đại diện cho Nhà máy chế biến sữa Vinamilk (vd: Mega Factory Bình Dương).
/// </summary>
public class FactoryUnit : OrganizationalUnit
{
    public decimal FactoryCapacityLitersPerDay { get; private set; }
    public int ProductionLineCount { get; private set; }
    public string IsoStandard { get; private set; } = string.Empty;
    public string EnvironmentalPermitNumber { get; private set; } = string.Empty;

    protected FactoryUnit() : base()
    {
    }

    public FactoryUnit(
        string unitCode,
        string unitName,
        string address,
        string phoneNumber,
        string email,
        string taxCode,
        decimal factoryCapacityLitersPerDay,
        int productionLineCount,
        string isoStandard,
        string environmentalPermitNumber,
        int? parentUnitId = null)
        : base(unitCode, unitName, UnitType.Factory, address, phoneNumber, email, taxCode, parentUnitId)
    {
        if (factoryCapacityLitersPerDay < 0)
            throw new DomainException("Công suất nhà máy không thể là số âm.", "ERR_FACTORY_INVALID_CAPACITY");

        if (productionLineCount < 0)
            throw new DomainException("Số lượng dây chuyền sản xuất không thể âm.", "ERR_FACTORY_INVALID_LINES");

        FactoryCapacityLitersPerDay = factoryCapacityLitersPerDay;
        ProductionLineCount = productionLineCount;
        IsoStandard = isoStandard?.Trim() ?? string.Empty;
        EnvironmentalPermitNumber = environmentalPermitNumber?.Trim() ?? string.Empty;
    }

    public void UpdateFactoryCapacity(decimal newCapacity, int newLineCount)
    {
        if (newCapacity < 0)
            throw new DomainException("Công suất nhà máy không thể là số âm.", "ERR_FACTORY_INVALID_CAPACITY");

        if (newLineCount < 0)
            throw new DomainException("Số lượng dây chuyền sản xuất không thể âm.", "ERR_FACTORY_INVALID_LINES");

        FactoryCapacityLitersPerDay = newCapacity;
        ProductionLineCount = newLineCount;
    }

    public void UpdateCertifications(string isoStandard, string environmentalPermitNumber)
    {
        IsoStandard = isoStandard?.Trim() ?? string.Empty;
        EnvironmentalPermitNumber = environmentalPermitNumber?.Trim() ?? string.Empty;
    }
}
