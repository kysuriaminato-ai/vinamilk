using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.OrganizationalUnits;

/// <summary>
/// Lớp cơ sở trừu tượng đại diện cho một đơn vị tổ chức trong tập đoàn Vinamilk.
/// Áp dụng tính Đóng gói (Encapsulation) qua private setters và phương thức nghiệp vụ.
/// </summary>
public abstract class OrganizationalUnit : BaseAuditableEntity
{
    public string UnitCode { get; protected set; } = string.Empty;
    public string UnitName { get; protected set; } = string.Empty;
    public UnitType UnitType { get; protected set; }
    public string Address { get; protected set; } = string.Empty;
    public string PhoneNumber { get; protected set; } = string.Empty;
    public string Email { get; protected set; } = string.Empty;
    public string TaxCode { get; protected set; } = string.Empty;
    public int RegionId { get; protected set; } = 3; // 1: Miền Bắc, 2: Miền Trung, 3: Miền Nam
    public string RegionName { get; protected set; } = "Miền Nam";

    public int? ParentUnitId { get; protected set; }
    public virtual OrganizationalUnit? ParentUnit { get; protected set; }

    /// <summary>
    /// Đường dẫn cây phân cấp kiểu HierarchyId phục vụ truy vấn đơn vị và mọi đơn vị con trực thuộc.
    /// </summary>
    public Microsoft.EntityFrameworkCore.HierarchyId? Path { get; protected set; }

    public void SetPath(Microsoft.EntityFrameworkCore.HierarchyId? path)
    {
        Path = path;
    }

    public int? ManagerEmployeeId { get; protected set; }
    public virtual Employee? ManagerEmployee { get; protected set; }

    private readonly List<OrganizationalUnit> _childUnits = new();
    public virtual IReadOnlyCollection<OrganizationalUnit> ChildUnits => _childUnits.AsReadOnly();

    private readonly List<Employee> _employees = new();
    public virtual IReadOnlyCollection<Employee> Employees => _employees.AsReadOnly();

    protected OrganizationalUnit()
    {
    }

    protected OrganizationalUnit(
        string unitCode,
        string unitName,
        UnitType unitType,
        string address,
        string phoneNumber,
        string email,
        string taxCode,
        int? parentUnitId = null,
        int regionId = 3,
        string regionName = "Miền Nam")
    {
        if (string.IsNullOrWhiteSpace(unitCode))
            throw new DomainException("Mã đơn vị tổ chức không được để trống.", "ERR_UNIT_CODE_EMPTY");

        if (string.IsNullOrWhiteSpace(unitName))
            throw new DomainException("Tên đơn vị tổ chức không được để trống.", "ERR_UNIT_NAME_EMPTY");

        UnitCode = unitCode.Trim().ToUpperInvariant();
        UnitName = unitName.Trim();
        UnitType = unitType;
        Address = address?.Trim() ?? string.Empty;
        PhoneNumber = phoneNumber?.Trim() ?? string.Empty;
        Email = email?.Trim() ?? string.Empty;
        TaxCode = taxCode?.Trim() ?? string.Empty;
        ParentUnitId = parentUnitId;
        RegionId = regionId > 0 ? regionId : 3;
        RegionName = string.IsNullOrWhiteSpace(regionName) ? "Miền Nam" : regionName.Trim();
    }

    public void SetRegion(int regionId, string regionName)
    {
        if (regionId <= 0)
            throw new DomainException("Mã phân vùng địa lý không hợp lệ.", "ERR_REGION_INVALID");

        RegionId = regionId;
        RegionName = string.IsNullOrWhiteSpace(regionName) ? "Miền Nam" : regionName.Trim();
    }

    public void UpdateGeneralInfo(string unitName, string address, string phoneNumber, string email, string taxCode)
    {
        if (string.IsNullOrWhiteSpace(unitName))
            throw new DomainException("Tên đơn vị tổ chức không được để trống.", "ERR_UNIT_NAME_EMPTY");

        UnitName = unitName.Trim();
        Address = address?.Trim() ?? string.Empty;
        PhoneNumber = phoneNumber?.Trim() ?? string.Empty;
        Email = email?.Trim() ?? string.Empty;
        TaxCode = taxCode?.Trim() ?? string.Empty;
    }

    public void AssignManager(int managerEmployeeId)
    {
        if (managerEmployeeId <= 0)
            throw new DomainException("Mã định danh người quản lý không hợp lệ.", "ERR_INVALID_MANAGER_ID");

        ManagerEmployeeId = managerEmployeeId;
    }

    public void RemoveManager()
    {
        ManagerEmployeeId = null;
    }

    public void SetParentUnit(int? parentUnitId)
    {
        if (parentUnitId.HasValue && parentUnitId.Value == Id && Id > 0)
            throw new DomainException("Đơn vị không thể trực thuộc chính nó.", "ERR_CIRCULAR_UNIT_REFERENCE");

        ParentUnitId = parentUnitId;
    }
}
