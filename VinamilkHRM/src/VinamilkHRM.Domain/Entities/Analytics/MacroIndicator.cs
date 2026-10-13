using VinamilkHRM.Domain.Common;

namespace VinamilkHRM.Domain.Entities.Analytics;

/// <summary>
/// Chỉ số kinh tế vĩ mô (MacroIndicator): CPI_YOY, MIN_WAGE_REGION_I..IV, BASE_SALARY, UNEMPLOYMENT_RATE.
/// </summary>
public class MacroIndicator : BaseAuditableEntity
{
    public string Code { get; private set; } = string.Empty; // Mã: CPI_YOY, BASE_SALARY...
    public string Period { get; private set; } = string.Empty; // YYYY hoặc YYYY-MM
    public decimal Value { get; private set; }
    public string? SourceUrl { get; private set; }
    public string? Notes { get; private set; }
    public bool IsSampleData { get; private set; } = false;

    protected MacroIndicator() { }

    public MacroIndicator(
        string code,
        string period,
        decimal value,
        string? sourceUrl = null,
        string? notes = null,
        bool isSampleData = false)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Mã chỉ số vĩ mô không được để trống.", "ERR_MACRO_CODE_EMPTY");

        if (string.IsNullOrWhiteSpace(period))
            throw new DomainException("Kỳ chỉ số không được để trống.", "ERR_MACRO_PERIOD_EMPTY");

        Code = code.Trim().ToUpperInvariant();
        Period = period.Trim();
        Value = value;
        SourceUrl = sourceUrl?.Trim();
        Notes = notes?.Trim();
        IsSampleData = isSampleData;
    }
}