using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Jobs;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Analytics;

/// <summary>
/// Số liệu khảo sát lương thị trường (Market Salary Benchmark) nhập từ Excel theo kỳ.
/// </summary>
public class MarketSalaryBenchmark : BaseAuditableEntity
{
    public string SurveySource { get; private set; } = string.Empty; // Nguồn: Mercer-Talentnet, Navigos, VietnamWorks
    public int Year { get; private set; }

    public int JobFamilyId { get; private set; }
    public virtual JobFamily? JobFamily { get; private set; }

    public int JobGradeId { get; private set; }
    public virtual JobGrade? JobGrade { get; private set; }

    public MinimumWageRegion Region { get; private set; } = MinimumWageRegion.RegionI;

    public decimal P25 { get; private set; } // Bách phân vị 25
    public decimal P50 { get; private set; } // Bách phân vị 50 (Trung vị thị trường)
    public decimal P75 { get; private set; } // Bách phân vị 75

    public bool IsSampleData { get; private set; } = false; // Gắn nhãn DỮ LIỆU MẪU nếu dùng để demo

    protected MarketSalaryBenchmark() { }

    public MarketSalaryBenchmark(
        string surveySource,
        int year,
        int jobFamilyId,
        int jobGradeId,
        MinimumWageRegion region,
        decimal p25,
        decimal p50,
        decimal p75,
        bool isSampleData = false)
    {
        if (string.IsNullOrWhiteSpace(surveySource))
            throw new DomainException("Nguồn khảo sát không được để trống.", "ERR_MKT_SOURCE_EMPTY");

        if (p50 <= 0 || p25 <= 0 || p75 <= 0)
            throw new DomainException("Các bách phân vị lương thị trường phải lớn hơn 0.", "ERR_MKT_VALUES_INVALID");

        if (p25 > p50 || p50 > p75)
            throw new DomainException("Bách phân vị lương phải thỏa mãn P25 <= P50 <= P75.", "ERR_MKT_PERCENTILE_ORDER_INVALID");

        SurveySource = surveySource.Trim();
        Year = year;
        JobFamilyId = jobFamilyId;
        JobGradeId = jobGradeId;
        Region = region;
        P25 = p25;
        P50 = p50;
        P75 = p75;
        IsSampleData = isSampleData;
    }
}