using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Statutory;

/// <summary>
/// Tỷ lệ trích nộp các quỹ bảo hiểm bắt buộc phía Người lao động và Người sử dụng lao động.
/// </summary>
public class InsuranceRate : BaseAuditableEntity
{
    public InsuranceRateKind Kind { get; private set; }
    public decimal EmployeeRate { get; private set; }  // Tỷ lệ trích từ lương NLĐ (vd: 0.08 cho BHXH hưu trí)
    public decimal EmployerRate { get; private set; }  // Tỷ lệ NSDLĐ đóng (vd: 0.14)
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public string? LegalReference { get; private set; }

    protected InsuranceRate()
    {
    }

    public InsuranceRate(
        InsuranceRateKind kind,
        decimal employeeRate,
        decimal employerRate,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo = null,
        string? legalReference = null)
    {
        if (employeeRate < 0 || employeeRate > 1 || employerRate < 0 || employerRate > 1)
            throw new DomainException("Tỷ lệ đóng bảo hiểm phải nằm trong khoảng từ 0% đến 100% (0.0 đến 1.0).", "ERR_INS_RATE_INVALID");

        if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom)
            throw new DomainException("Ngày hết hiệu lực không thể trước ngày bắt đầu.", "ERR_INS_RATE_DATES_INVALID");

        Kind = kind;
        EmployeeRate = employeeRate;
        EmployerRate = employerRate;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        LegalReference = legalReference?.Trim();
    }
}
