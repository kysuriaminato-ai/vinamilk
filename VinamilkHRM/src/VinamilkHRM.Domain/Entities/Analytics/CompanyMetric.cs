using VinamilkHRM.Domain.Common;

namespace VinamilkHRM.Domain.Entities.Analytics;

/// <summary>
/// Số liệu tài chính - vận hành tập đoàn công bố theo quý (Doanh thu, Lợi nhuận sau thuế, Sản lượng).
/// </summary>
public class CompanyMetric : BaseAuditableEntity
{
    public string Quarter { get; private set; } = string.Empty; // YYYY-Q1, YYYY-Q2...
    public decimal Revenue { get; private set; }                 // Doanh thu (VND)
    public decimal NetProfitAfterTax { get; private set; }       // Lợi nhuận sau thuế (VND)
    public decimal ProductionVolumeLiters { get; private set; }  // Sản lượng sữa (Lít)
    public bool IsSampleData { get; private set; } = false;

    protected CompanyMetric() { }

    public CompanyMetric(
        string quarter,
        decimal revenue,
        decimal netProfitAfterTax,
        decimal productionVolumeLiters,
        bool isSampleData = false)
    {
        if (string.IsNullOrWhiteSpace(quarter))
            throw new DomainException("Kỳ quý tài chính không được để trống.", "ERR_METRIC_QUARTER_EMPTY");

        Quarter = quarter.Trim().ToUpperInvariant();
        Revenue = revenue;
        NetProfitAfterTax = netProfitAfterTax;
        ProductionVolumeLiters = productionVolumeLiters;
        IsSampleData = isSampleData;
    }
}