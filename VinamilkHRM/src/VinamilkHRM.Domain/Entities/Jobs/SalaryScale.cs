using VinamilkHRM.Domain.Common;

namespace VinamilkHRM.Domain.Entities.Jobs;

/// <summary>
/// Thang bảng lương 3P: P1 = Lương vị trí theo (JobGrade, Step) tại ngày hiệu lực.
/// Ràng buộc: P1 không được thấp hơn mức lương tối thiểu vùng của đơn vị tại ngày áp dụng.
/// </summary>
public class SalaryScale : BaseAuditableEntity
{
    public int JobGradeId { get; private set; }
    public virtual JobGrade? JobGrade { get; private set; }

    public int Step { get; private set; }           // Bậc lương trong ngạch (Bậc 1, 2, 3...)
    public decimal Amount { get; private set; }     // Mức lương vị trí P1 (VND)
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public string? LegalReference { get; private set; }

    protected SalaryScale()
    {
    }

    public SalaryScale(
        int jobGradeId,
        int step,
        decimal amount,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo = null,
        string? legalReference = null,
        decimal? minimumWage = null)
    {
        if (jobGradeId <= 0)
            throw new DomainException("Mã ngạch lương (JobGrade) không hợp lệ.", "ERR_SAL_GRADE_INVALID");

        if (step <= 0)
            throw new DomainException("Bậc lương (Step) phải lớn hơn 0.", "ERR_SAL_STEP_INVALID");

        if (amount <= 0)
            throw new DomainException("Mức lương vị trí (P1) phải lớn hơn 0.", "ERR_SAL_AMOUNT_INVALID");

        if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom)
            throw new DomainException("Ngày hết hiệu lực không thể trước ngày bắt đầu.", "ERR_SAL_DATES_INVALID");

        if (minimumWage.HasValue && amount < minimumWage.Value)
            throw new DomainException($"Mức lương vị trí P1 ({amount:N0} đ) không được thấp hơn mức lương tối thiểu vùng ({minimumWage.Value:N0} đ) tại ngày áp dụng.", "ERR_SAL_BELOW_MIN_WAGE");

        JobGradeId = jobGradeId;
        Step = step;
        Amount = amount;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        LegalReference = legalReference?.Trim();
    }

    /// <summary>
    /// Kiểm tra ràng buộc P1 >= Lương tối thiểu vùng của đơn vị tại ngày áp dụng.
    /// Vi phạm thì ném ra DomainException với mã ERR_SAL_BELOW_MIN_WAGE.
    /// </summary>
    public void ValidateAgainstMinimumWage(decimal unitMinimumWage)
    {
        if (Amount < unitMinimumWage)
        {
            throw new DomainException(
                $"Mức lương vị trí P1 ({Amount:N0} đ) thấp hơn lương tối thiểu vùng của đơn vị ({unitMinimumWage:N0} đ) tại ngày áp dụng {EffectiveFrom:yyyy-MM-dd}.",
                "ERR_SAL_BELOW_MIN_WAGE");
        }
    }
}
