using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Kpis;

/// <summary>
/// Khung cơ cấu tỷ trọng 4 nhóm chỉ tiêu KPI theo cấp bậc lãnh đạo (LeadershipLevel).
/// Áp dụng theo ngày hiệu lực, không hard-code.
/// </summary>
public class KpiScheme : BaseAuditableEntity
{
    public LeadershipLevel LeadershipLevel { get; private set; }

    /// <summary>
    /// Tỷ trọng nhóm Kết quả đơn vị (%)
    /// </summary>
    public decimal UnitResultWeight { get; private set; }

    /// <summary>
    /// Tỷ trọng nhóm Công việc cá nhân / Chuyên môn (%)
    /// </summary>
    public decimal IndividualWorkWeight { get; private set; }

    /// <summary>
    /// Tỷ trọng nhóm Quản lý con người / Phát triển đội ngũ (%)
    /// </summary>
    public decimal PeopleManagementWeight { get; private set; }

    /// <summary>
    /// Tỷ trọng nhóm Tuân thủ (%)
    /// </summary>
    public decimal ComplianceWeight { get; private set; }

    /// <summary>
    /// Cờ xác định nhóm Tuân thủ là "Cổng" (Gatekeeper).
    /// Áp dụng cho Director: vi phạm an toàn/kỷ luật nghiêm trọng trong kỳ -> hệ số thưởng = 0.
    /// </summary>
    public bool IsComplianceGating { get; private set; }

    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public string? Description { get; private set; }

    protected KpiScheme() { }

    public KpiScheme(
        LeadershipLevel leadershipLevel,
        decimal unitResultWeight,
        decimal individualWorkWeight,
        decimal peopleManagementWeight,
        decimal complianceWeight,
        bool isComplianceGating,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo = null,
        string? description = null)
    {
        // Kiểm tra tổng trọng số
        decimal total = unitResultWeight + individualWorkWeight + peopleManagementWeight + complianceWeight;
        if (Math.Round(total, 2) != 100m)
            throw new DomainException($"Tổng tỷ trọng 4 nhóm chỉ tiêu phải bằng 100% (Hiện tại: {total}%).", "ERR_KPI_SCHEME_TOTAL_NOT_100");

        if (effectiveTo.HasValue && effectiveTo < effectiveFrom)
            throw new DomainException("Ngày hết hiệu lực không thể trước ngày bắt đầu hiệu lực.", "ERR_KPI_SCHEME_DATES_INVALID");

        LeadershipLevel = leadershipLevel;
        UnitResultWeight = unitResultWeight;
        IndividualWorkWeight = individualWorkWeight;
        PeopleManagementWeight = peopleManagementWeight;
        ComplianceWeight = complianceWeight;
        IsComplianceGating = isComplianceGating;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        Description = description?.Trim();
    }
}
