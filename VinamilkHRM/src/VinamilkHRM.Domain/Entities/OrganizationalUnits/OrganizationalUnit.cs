using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.OrganizationalUnits;

/// <summary>
/// Lớp cơ sở đại diện cho một đơn vị tổ chức trong tập đoàn Vinamilk.
/// Hỗ trợ cây phân cấp đa điểm qua Microsoft.EntityFrameworkCore.HierarchyId.
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

    public string CostCenterCode { get; protected set; } = "CC_DEFAULT";
    public WorkPattern WorkPattern { get; protected set; } = WorkPattern.OfficeWeek;
    public MinimumWageRegion MinimumWageRegion { get; protected set; } = MinimumWageRegion.RegionI;
    public int PlannedHeadcount { get; protected set; } = 0; // Định biên nhân sự
    public string? LegacyId { get; protected set; }

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

    private readonly List<UnitManagerHistory> _unitManagerHistories = new();
    public virtual IReadOnlyCollection<UnitManagerHistory> UnitManagerHistories => _unitManagerHistories.AsReadOnly();

    private readonly List<UnitMinimumWageRegionHistory> _unitMinimumWageRegionHistories = new();
    public virtual IReadOnlyCollection<UnitMinimumWageRegionHistory> UnitMinimumWageRegionHistories => _unitMinimumWageRegionHistories.AsReadOnly();

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
        string regionName = "Miền Nam",
        string costCenterCode = "CC_DEFAULT",
        WorkPattern workPattern = WorkPattern.OfficeWeek,
        MinimumWageRegion minimumWageRegion = MinimumWageRegion.RegionI,
        int plannedHeadcount = 0,
        string? legacyId = null)
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
        CostCenterCode = string.IsNullOrWhiteSpace(costCenterCode) ? "CC_DEFAULT" : costCenterCode.Trim().ToUpperInvariant();
        WorkPattern = workPattern;
        MinimumWageRegion = minimumWageRegion;
        PlannedHeadcount = plannedHeadcount >= 0 ? plannedHeadcount : 0;
        LegacyId = legacyId?.Trim();
    }

    public void SetRegion(int regionId, string regionName)
    {
        if (regionId <= 0)
            throw new DomainException("Mã phân vùng địa lý không hợp lệ.", "ERR_REGION_INVALID");

        RegionId = regionId;
        RegionName = string.IsNullOrWhiteSpace(regionName) ? "Miền Nam" : regionName.Trim();
    }

    public void UpdateGeneralInfo(string unitName, string address, string phoneNumber, string email, string taxCode, string? costCenterCode = null, WorkPattern? workPattern = null, int? plannedHeadcount = null)
    {
        if (string.IsNullOrWhiteSpace(unitName))
            throw new DomainException("Tên đơn vị tổ chức không được để trống.", "ERR_UNIT_NAME_EMPTY");

        UnitName = unitName.Trim();
        Address = address?.Trim() ?? string.Empty;
        PhoneNumber = phoneNumber?.Trim() ?? string.Empty;
        Email = email?.Trim() ?? string.Empty;
        TaxCode = taxCode?.Trim() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(costCenterCode))
            CostCenterCode = costCenterCode.Trim().ToUpperInvariant();

        if (workPattern.HasValue)
            WorkPattern = workPattern.Value;

        if (plannedHeadcount.HasValue && plannedHeadcount.Value >= 0)
            PlannedHeadcount = plannedHeadcount.Value;
    }

    public void AssignManager(int managerEmployeeId, string? appointmentDecisionNumber = null, string? note = null, DateOnly? fromDate = null)
    {
        if (managerEmployeeId <= 0)
            throw new DomainException("Mã định danh người quản lý không hợp lệ.", "ERR_INVALID_MANAGER_ID");

        var effectiveFrom = fromDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        // Đóng nhiệm kỳ trước nếu có
        var activeHistory = _unitManagerHistories.FirstOrDefault(h => h.ToDate == null);
        activeHistory?.EndTerm(effectiveFrom);

        ManagerEmployeeId = managerEmployeeId;
        _unitManagerHistories.Add(new UnitManagerHistory(Id, managerEmployeeId, effectiveFrom, null, appointmentDecisionNumber, note));
    }

    public void RemoveManager(DateOnly? toDate = null)
    {
        var effectiveTo = toDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var activeHistory = _unitManagerHistories.FirstOrDefault(h => h.ToDate == null);
        activeHistory?.EndTerm(effectiveTo);

        ManagerEmployeeId = null;
    }

    public void SetParentUnit(int? parentUnitId)
    {
        if (parentUnitId.HasValue && parentUnitId.Value == Id && Id > 0)
            throw new DomainException("Đơn vị không thể trực thuộc chính nó.", "ERR_CIRCULAR_UNIT_REFERENCE");

        ParentUnitId = parentUnitId;
    }

    public void MoveTo(int? newParentUnitId, Microsoft.EntityFrameworkCore.HierarchyId? newPath)
    {
        if (newParentUnitId.HasValue && newParentUnitId.Value == Id && Id > 0)
            throw new DomainException("Đơn vị không thể chuyển trực thuộc chính nó.", "ERR_CIRCULAR_UNIT_REFERENCE");

        ParentUnitId = newParentUnitId;
        Path = newPath;
    }

    public void ChangeMinimumWageRegion(MinimumWageRegion newRegion, DateOnly effectiveFrom, string? reason = null, string? legalReference = null)
    {
        var activeRegion = _unitMinimumWageRegionHistories.FirstOrDefault(r => r.EffectiveTo == null);
        activeRegion?.CloseEffectivity(effectiveFrom.AddDays(-1));

        MinimumWageRegion = newRegion;
        _unitMinimumWageRegionHistories.Add(new UnitMinimumWageRegionHistory(Id, newRegion, effectiveFrom, null, reason, legalReference));
    }

    public MinimumWageRegion GetMinimumWageRegionOn(DateOnly onDate)
    {
        var matched = _unitMinimumWageRegionHistories
            .Where(r => r.EffectiveFrom <= onDate && (r.EffectiveTo == null || r.EffectiveTo >= onDate))
            .OrderByDescending(r => r.EffectiveFrom)
            .FirstOrDefault();

        return matched?.MinimumWageRegion ?? MinimumWageRegion;
    }

    public void SetLegacyId(string legacyId)
    {
        LegacyId = legacyId?.Trim();
    }
}
