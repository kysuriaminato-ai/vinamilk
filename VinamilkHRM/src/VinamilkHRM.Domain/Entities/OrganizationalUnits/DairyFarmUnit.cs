using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.OrganizationalUnits;

/// <summary>
/// Đại diện cho Trang trại Bò sữa Vinamilk (vd: Trang trại Bò sữa sinh thái Tây Ninh).
/// </summary>
public class DairyFarmUnit : OrganizationalUnit
{
    public int HerdSize { get; private set; }
    public decimal TotalAreaHectares { get; private set; }
    public int BarnCount { get; private set; }
    public string GlobalGapCertificationNumber { get; private set; } = string.Empty;

    protected DairyFarmUnit() : base()
    {
    }

    public DairyFarmUnit(
        string unitCode,
        string unitName,
        string address,
        string phoneNumber,
        string email,
        string taxCode,
        int herdSize,
        decimal totalAreaHectares,
        int barnCount,
        string globalGapCertificationNumber,
        int? parentUnitId = null)
        : base(unitCode, unitName, UnitType.DairyFarm, address, phoneNumber, email, taxCode, parentUnitId)
    {
        if (herdSize < 0)
            throw new DomainException("Quy mô đàn bò không thể là số âm.", "ERR_FARM_INVALID_HERD_SIZE");

        if (totalAreaHectares <= 0)
            throw new DomainException("Diện tích trang trại (ha) phải lớn hơn 0.", "ERR_FARM_INVALID_AREA");

        HerdSize = herdSize;
        TotalAreaHectares = totalAreaHectares;
        BarnCount = barnCount >= 0 ? barnCount : 0;
        GlobalGapCertificationNumber = globalGapCertificationNumber?.Trim() ?? string.Empty;
    }

    public void UpdateFarmMetrics(int herdSize, decimal totalAreaHectares, int barnCount)
    {
        if (herdSize < 0)
            throw new DomainException("Quy mô đàn bò không thể là số âm.", "ERR_FARM_INVALID_HERD_SIZE");

        if (totalAreaHectares <= 0)
            throw new DomainException("Diện tích trang trại (ha) phải lớn hơn 0.", "ERR_FARM_INVALID_AREA");

        HerdSize = herdSize;
        TotalAreaHectares = totalAreaHectares;
        BarnCount = barnCount >= 0 ? barnCount : 0;
    }

    public void UpdateGlobalGapCertification(string certificationNumber)
    {
        GlobalGapCertificationNumber = certificationNumber?.Trim() ?? string.Empty;
    }
}
