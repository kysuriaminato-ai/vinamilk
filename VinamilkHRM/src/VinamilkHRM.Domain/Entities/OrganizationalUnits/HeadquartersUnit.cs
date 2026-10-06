using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.OrganizationalUnits;

/// <summary>
/// Đại diện cho Trụ sở chính Tập đoàn Vinamilk (Vinamilk Tower - Quận 7).
/// </summary>
public class HeadquartersUnit : OrganizationalUnit
{
    public int FloorCount { get; private set; }
    public int MeetingRoomCount { get; private set; }
    public bool HasExecutiveSuite { get; private set; }
    public string DivisionName { get; private set; } = string.Empty;

    // Parameterless constructor cho Entity Framework Core
    protected HeadquartersUnit() : base()
    {
    }

    public HeadquartersUnit(
        string unitCode,
        string unitName,
        string address,
        string phoneNumber,
        string email,
        string taxCode,
        int floorCount,
        int meetingRoomCount,
        bool hasExecutiveSuite,
        string divisionName,
        int? parentUnitId = null)
        : base(unitCode, unitName, UnitType.Headquarters, address, phoneNumber, email, taxCode, parentUnitId)
    {
        if (floorCount <= 0)
            throw new DomainException("Số tầng tại tòa nhà trụ sở phải lớn hơn 0.", "ERR_HQ_INVALID_FLOORS");

        FloorCount = floorCount;
        MeetingRoomCount = meetingRoomCount >= 0 ? meetingRoomCount : 0;
        HasExecutiveSuite = hasExecutiveSuite;
        DivisionName = divisionName?.Trim() ?? string.Empty;
    }

    public void UpdateFacilities(int floorCount, int meetingRoomCount, bool hasExecutiveSuite, string divisionName)
    {
        if (floorCount <= 0)
            throw new DomainException("Số tầng tại tòa nhà trụ sở phải lớn hơn 0.", "ERR_HQ_INVALID_FLOORS");

        FloorCount = floorCount;
        MeetingRoomCount = meetingRoomCount >= 0 ? meetingRoomCount : 0;
        HasExecutiveSuite = hasExecutiveSuite;
        DivisionName = divisionName?.Trim() ?? string.Empty;
    }
}
