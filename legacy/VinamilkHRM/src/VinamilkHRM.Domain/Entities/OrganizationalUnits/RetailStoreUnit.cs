using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.OrganizationalUnits;

/// <summary>
/// Đại diện cho Cửa hàng Giấc Mơ Sữa Việt (Hệ thống chuỗi bán lẻ Vinamilk).
/// </summary>
public class RetailStoreUnit : OrganizationalUnit
{
    public string StoreCode { get; private set; } = string.Empty;
    public decimal ShelfAreaSquareMeters { get; private set; }
    public decimal ColdStorageCapacityLiters { get; private set; }
    public string BusinessHours { get; private set; } = "07:30 - 21:30";

    protected RetailStoreUnit() : base()
    {
    }

    public RetailStoreUnit(
        string unitCode,
        string unitName,
        string address,
        string phoneNumber,
        string email,
        string taxCode,
        string storeCode,
        decimal shelfAreaSquareMeters,
        decimal coldStorageCapacityLiters,
        string businessHours,
        int? parentUnitId = null)
        : base(unitCode, unitName, UnitType.RetailStore, address, phoneNumber, email, taxCode, parentUnitId)
    {
        if (string.IsNullOrWhiteSpace(storeCode))
            throw new DomainException("Mã cửa hàng bán lẻ không được để trống.", "ERR_STORE_CODE_EMPTY");

        if (shelfAreaSquareMeters <= 0)
            throw new DomainException("Diện tích trưng bày phải lớn hơn 0.", "ERR_STORE_INVALID_AREA");

        StoreCode = storeCode.Trim().ToUpperInvariant();
        ShelfAreaSquareMeters = shelfAreaSquareMeters;
        ColdStorageCapacityLiters = coldStorageCapacityLiters >= 0 ? coldStorageCapacityLiters : 0;
        BusinessHours = businessHours?.Trim() ?? "07:30 - 21:30";
    }

    public void UpdateStoreDetails(decimal shelfArea, decimal coldStorageCapacity, string businessHours)
    {
        if (shelfArea <= 0)
            throw new DomainException("Diện tích trưng bày phải lớn hơn 0.", "ERR_STORE_INVALID_AREA");

        ShelfAreaSquareMeters = shelfArea;
        ColdStorageCapacityLiters = coldStorageCapacity >= 0 ? coldStorageCapacity : 0;
        BusinessHours = businessHours?.Trim() ?? "07:30 - 21:30";
    }
}
