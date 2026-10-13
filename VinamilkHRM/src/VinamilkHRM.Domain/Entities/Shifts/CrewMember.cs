using System;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;

namespace VinamilkHRM.Domain.Entities.Shifts;

/// <summary>
/// Nhân viên thuộc kíp sản xuất (CrewMember).
/// </summary>
public class CrewMember : BaseEntity
{
    public int CrewId { get; private set; }
    public virtual Crew? Crew { get; private set; }

    public int EmployeeId { get; private set; }
    public virtual Employee? Employee { get; private set; }

    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }

    protected CrewMember()
    {
    }

    public CrewMember(int crewId, int employeeId, DateOnly effectiveFrom, DateOnly? effectiveTo = null)
    {
        if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom)
            throw new DomainException("Ngày kết thúc không thể trước ngày bắt đầu.", "ERR_CREW_MEMBER_DATES_INVALID");

        CrewId = crewId;
        EmployeeId = employeeId;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
    }

    public void CloseEffectivity(DateOnly toDate)
    {
        EffectiveTo = toDate;
    }
}
