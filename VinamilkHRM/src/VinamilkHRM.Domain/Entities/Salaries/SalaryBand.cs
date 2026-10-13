using System;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Jobs;

namespace VinamilkHRM.Domain.Entities.Salaries;

/// <summary>
/// Khung lương theo bậc ngạch chức danh (JobGrade): Min, Mid, Max.
/// Compa-ratio = P1 / Mid (Tỷ lệ so sánh với trung vị thị trường).
/// </summary>
public class SalaryBand : BaseAuditableEntity
{
    public int JobGradeId { get; private set; }
    public virtual JobGrade? JobGrade { get; private set; }

    public decimal Min { get; private set; }
    public decimal Mid { get; private set; }
    public decimal Max { get; private set; }

    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    protected SalaryBand() { }

    public SalaryBand(
        int jobGradeId,
        decimal min,
        decimal mid,
        decimal max,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo = null,
        string? description = null)
    {
        if (jobGradeId <= 0)
            throw new DomainException("Mã bậc ngạch chức danh (JobGrade) không hợp lệ.", "ERR_SAL_BAND_GRADE_INVALID");

        if (min <= 0)
            throw new DomainException("Mức lương tối thiểu của khung (Min) phải lớn hơn 0.", "ERR_SAL_BAND_MIN_INVALID");

        if (mid < min)
            throw new DomainException("Mức lương trung vị (Mid) không thể nhỏ hơn mức tối thiểu (Min).", "ERR_SAL_BAND_MID_INVALID");

        if (max < mid)
            throw new DomainException("Mức lương tối đa của khung (Max) không thể nhỏ hơn mức trung vị (Mid).", "ERR_SAL_BAND_MAX_INVALID");

        if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom)
            throw new DomainException("Ngày hết hiệu lực không thể trước ngày bắt đầu.", "ERR_SAL_BAND_DATES_INVALID");

        JobGradeId = jobGradeId;
        Min = min;
        Mid = mid;
        Max = max;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        Description = description?.Trim();
        IsActive = true;
    }

    /// <summary>
    /// Tính toán compa-ratio = P1 / Mid.
    /// </summary>
    public decimal CalculateCompaRatio(decimal p1)
    {
        if (Mid <= 0) return 0m;
        return Math.Round(p1 / Mid, 4, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Kiểm tra mức lương đề xuất có vượt quá trần Max của khung hay không.
    /// </summary>
    public bool ExceedsMax(decimal proposedP1) => proposedP1 > Max;
}