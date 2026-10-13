using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Jobs;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Kpis;

/// <summary>
/// Thư viện chỉ tiêu KPI chuẩn hóa cấp Tập đoàn Vinamilk.
/// </summary>
public class KpiDefinition : BaseAuditableEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Unit { get; private set; } = string.Empty; // %, Lit, VND, Cong, Task...
    public KpiGroup Group { get; private set; } = KpiGroup.IndividualWork;
    public KpiScoringMethod ScoringMethod { get; private set; } = KpiScoringMethod.HigherIsBetter;
    public KpiDataSource DataSource { get; private set; } = KpiDataSource.Manual;
    public string? Description { get; private set; }
    public string? MeasurementGuide { get; private set; }

    public int? JobFamilyId { get; private set; }
    public virtual JobFamily? JobFamily { get; private set; }

    public LeadershipLevel? ApplicableLeadershipLevel { get; private set; }
    public bool IsActive { get; private set; } = true;

    private readonly List<KpiThreshold> _thresholds = new();
    public virtual IReadOnlyCollection<KpiThreshold> Thresholds => _thresholds.AsReadOnly();

    protected KpiDefinition() { }

    public KpiDefinition(
        string code,
        string name,
        string unit,
        KpiGroup group,
        KpiScoringMethod scoringMethod,
        KpiDataSource dataSource,
        int? jobFamilyId = null,
        LeadershipLevel? leadershipLevel = null,
        string? description = null,
        string? measurementGuide = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Mã chỉ tiêu KPI không được để trống.", "ERR_KPI_DEF_CODE_EMPTY");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Tên chỉ tiêu KPI không được để trống.", "ERR_KPI_DEF_NAME_EMPTY");

        if (string.IsNullOrWhiteSpace(unit))
            throw new DomainException("Đơn vị đo lường không được để trống.", "ERR_KPI_DEF_UNIT_EMPTY");

        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Unit = unit.Trim();
        Group = group;
        ScoringMethod = scoringMethod;
        DataSource = dataSource;
        JobFamilyId = jobFamilyId;
        ApplicableLeadershipLevel = leadershipLevel;
        Description = description?.Trim();
        MeasurementGuide = measurementGuide?.Trim();
        IsActive = true;
    }

    public void AddThreshold(decimal minThreshold, decimal maxThreshold, decimal score)
    {
        if (ScoringMethod != KpiScoringMethod.ThresholdTable)
            throw new DomainException("Chỉ có thể thiết lập bảng ngưỡng cho phương pháp ThresholdTable.", "ERR_KPI_THRESHOLD_NOT_ALLOWED");

        _thresholds.Add(new KpiThreshold(Id, minThreshold, maxThreshold, score));
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}

/// <summary>
/// Bảng tra ngưỡng điểm cho phương pháp ThresholdTable.
/// </summary>
public class KpiThreshold : BaseEntity
{
    public int KpiDefinitionId { get; private set; }
    public virtual KpiDefinition? KpiDefinition { get; private set; }

    public decimal MinThreshold { get; private set; }
    public decimal MaxThreshold { get; private set; }
    public decimal Score { get; private set; }

    protected KpiThreshold() { }

    public KpiThreshold(int kpiDefinitionId, decimal minThreshold, decimal maxThreshold, decimal score)
    {
        if (minThreshold > maxThreshold)
            throw new DomainException("Ngưỡng cận dưới không thể lớn hơn ngưỡng cận trên.", "ERR_KPI_THRESHOLD_RANGE_INVALID");

        KpiDefinitionId = kpiDefinitionId;
        MinThreshold = minThreshold;
        MaxThreshold = maxThreshold;
        Score = Math.Clamp(score, 0m, 120m);
    }
}
