using System.Collections.Generic;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.OrganizationalUnits;

namespace VinamilkHRM.Domain.Entities.Shifts;

/// <summary>
/// Đại diện cho Kíp sản xuất (Crew).
/// Phục vụ mô hình 3 ca 4 kíp (Kíp 1, 2, 3, 4) tại các Nhà máy Mega và Trang trại sinh thái.
/// </summary>
public class Crew : BaseAuditableEntity
{
    public string CrewCode { get; private set; } = string.Empty;
    public string CrewName { get; private set; } = string.Empty;
    public int OrganizationalUnitId { get; private set; }
    public virtual OrganizationalUnit? OrganizationalUnit { get; private set; }
    public bool IsActive { get; private set; } = true;

    private readonly List<CrewMember> _members = new();
    public virtual IReadOnlyCollection<CrewMember> Members => _members.AsReadOnly();

    protected Crew()
    {
    }

    public Crew(string crewCode, string crewName, int organizationalUnitId)
    {
        if (string.IsNullOrWhiteSpace(crewCode))
            throw new DomainException("Mã kíp không được để trống.", "ERR_CREW_CODE_EMPTY");

        if (string.IsNullOrWhiteSpace(crewName))
            throw new DomainException("Tên kíp không được để trống.", "ERR_CREW_NAME_EMPTY");

        if (organizationalUnitId <= 0)
            throw new DomainException("Mã đơn vị tổ chức không hợp lệ.", "ERR_CREW_UNIT_INVALID");

        CrewCode = crewCode.Trim().ToUpperInvariant();
        CrewName = crewName.Trim();
        OrganizationalUnitId = organizationalUnitId;
        IsActive = true;
    }

    public void AddMember(int employeeId, DateOnly effectiveFrom, DateOnly? effectiveTo = null)
    {
        if (employeeId <= 0)
            throw new DomainException("Mã nhân viên không hợp lệ.", "ERR_CREW_MEMBER_EMP_INVALID");

        var member = new CrewMember(Id, employeeId, effectiveFrom, effectiveTo);
        _members.Add(member);
    }
}
