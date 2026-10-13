using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Jobs;
using VinamilkHRM.Domain.Entities.OrganizationalUnits;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Kpis;

/// <summary>
/// Nguồn số liệu chỉ số tự động (Sản lượng, Chất lượng, Doanh số) tiếp nhận qua Excel hoặc POST /api/v1/kpi-metrics.
/// </summary>
public class KpiMetric : BaseAuditableEntity
{
    public string MetricCode { get; private set; } = string.Empty;
    public string Period { get; private set; } = string.Empty; // YYYY-MM
    public int? OrganizationalUnitId { get; private set; }
    public virtual OrganizationalUnit? OrganizationalUnit { get; private set; }

    public int? EmployeeId { get; private set; }
    public virtual Employee? Employee { get; private set; }
    public decimal Value { get; private set; }
    public string Unit { get; private set; } = string.Empty;
    public KpiDataSource Source { get; private set; } = KpiDataSource.Production;
    public string? BatchReference { get; private set; }
    public string? Notes { get; private set; }

    protected KpiMetric() { }

    public KpiMetric(
        string metricCode,
        string period,
        decimal value,
        string unit,
        KpiDataSource source,
        int? orgUnitId = null,
        int? employeeId = null,
        string? batchRef = null,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(metricCode))
            throw new DomainException("Mã chỉ số không được để trống.", "ERR_METRIC_CODE_EMPTY");

        if (string.IsNullOrWhiteSpace(period))
            throw new DomainException("Kỳ ghi nhận chỉ số không được để trống.", "ERR_METRIC_PERIOD_EMPTY");

        MetricCode = metricCode.Trim().ToUpperInvariant();
        Period = period.Trim();
        Value = value;
        Unit = unit.Trim();
        Source = source;
        OrganizationalUnitId = orgUnitId;
        EmployeeId = employeeId;
        BatchReference = batchRef?.Trim();
        Notes = notes?.Trim();
    }
}

/// <summary>
/// Mẫu chỉ tiêu KPI theo chức danh/khối ngành phục vụ giao KPI hàng loạt cho cả tổ/phòng ban.
/// </summary>
public class KpiTemplate : BaseAuditableEntity
{
    public string TemplateCode { get; private set; } = string.Empty;
    public string TemplateName { get; private set; } = string.Empty;
    public int? JobTitleId { get; private set; }
    public virtual JobTitle? JobTitle { get; private set; }

    public int? JobFamilyId { get; private set; }
    public virtual JobFamily? JobFamily { get; private set; }

    public LeadershipLevel LeadershipLevel { get; private set; } = LeadershipLevel.None;
    public bool IsActive { get; private set; } = true;

    private readonly List<KpiTemplateItem> _items = new();
    public virtual IReadOnlyCollection<KpiTemplateItem> Items => _items.AsReadOnly();

    protected KpiTemplate() { }

    public KpiTemplate(string code, string name, LeadershipLevel level, int? jobTitleId = null, int? jobFamilyId = null)
    {
        TemplateCode = code.Trim().ToUpperInvariant();
        TemplateName = name.Trim();
        LeadershipLevel = level;
        JobTitleId = jobTitleId;
        JobFamilyId = jobFamilyId;
        IsActive = true;
    }

    public void AddItem(int kpiDefinitionId, decimal defaultTarget, decimal weight, DateOnly? defaultDueDate = null)
    {
        _items.Add(new KpiTemplateItem(Id, kpiDefinitionId, defaultTarget, weight, defaultDueDate));
    }
}

public class KpiTemplateItem : BaseEntity
{
    public int KpiTemplateId { get; private set; }
    public virtual KpiTemplate? KpiTemplate { get; private set; }

    public int KpiDefinitionId { get; private set; }
    public virtual KpiDefinition? KpiDefinition { get; private set; }

    public decimal DefaultTarget { get; private set; }
    public decimal WeightPercentage { get; private set; }
    public DateOnly? DefaultDueDate { get; private set; }

    protected KpiTemplateItem() { }

    public KpiTemplateItem(int templateId, int defId, decimal target, decimal weight, DateOnly? dueDate = null)
    {
        KpiTemplateId = templateId;
        KpiDefinitionId = defId;
        DefaultTarget = target;
        WeightPercentage = weight;
        DefaultDueDate = dueDate;
    }
}

/// <summary>
/// Bảng tham số cấu hình quy chế KPI có ngày hiệu lực (Không hard-code).
/// </summary>
public class KpiConfiguration : BaseAuditableEntity
{
    public string Code { get; private set; } = string.Empty;
    public decimal Value { get; private set; }
    public string? StringValue { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public string? Description { get; private set; }

    protected KpiConfiguration() { }

    public KpiConfiguration(string code, decimal value, DateOnly effectiveFrom, DateOnly? effectiveTo = null, string? stringVal = null, string? desc = null)
    {
        Code = code.Trim().ToUpperInvariant();
        Value = value;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        StringValue = stringVal?.Trim();
        Description = desc?.Trim();
    }
}

