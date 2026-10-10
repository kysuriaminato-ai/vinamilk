using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Contracts;

/// <summary>
/// Đại diện cho Hợp đồng lao động xác định thời hạn (thường từ 12 đến 36 tháng).
/// Tuân thủ quy định Pháp luật Lao động: Tối đa được ký lại 1-2 lần trước khi chuyển sang không xác định thời hạn.
/// </summary>
public class DefiniteContract : LaborContract
{
    public int DurationMonths { get; private set; }
    public int MaximumRenewalsAllowed { get; private set; } = 2;
    public int CurrentRenewalCount { get; private set; } = 0;

    protected DefiniteContract() : base()
    {
    }

    public DefiniteContract(
        string contractNumber,
        int employeeId,
        DateTime startDate,
        DateTime endDate,
        decimal basicSalary,
        decimal insuranceSalary,
        DateTime signDate,
        int durationMonths,
        int maximumRenewalsAllowed = 2,
        int workingHoursPerWeek = 40)
        : base(
            contractNumber,
            employeeId,
            ContractType.Definite,
            startDate,
            endDate,
            basicSalary,
            insuranceSalary,
            signDate,
            workingHoursPerWeek)
    {
        if (durationMonths <= 0)
            throw new DomainException("Thời hạn hợp đồng xác định phải lớn hơn 0 tháng.", "ERR_DEFINITE_DURATION_INVALID");

        DurationMonths = durationMonths;
        MaximumRenewalsAllowed = maximumRenewalsAllowed;
        CurrentRenewalCount = 0;
    }

    /// <summary>
    /// Gia hạn hợp đồng xác định thời hạn kèm kiểm soát số lần gia hạn tối đa.
    /// </summary>
    public void ExtendContract(int additionalMonths, decimal newBasicSalary)
    {
        if (CurrentRenewalCount >= MaximumRenewalsAllowed)
            throw new DomainException(
                $"Đã vượt quá số lần tái ký hợp đồng xác định thời hạn ({MaximumRenewalsAllowed} lần). " +
                "Theo quy định Bộ luật Lao động và chính sách Vinamilk, nhân sự phải được chuyển sang Hợp đồng không xác định thời hạn.",
                "ERR_DEFINITE_MAX_RENEWALS_EXCEEDED");

        if (additionalMonths <= 0)
            throw new DomainException("Số tháng gia hạn phải lớn hơn 0.", "ERR_DEFINITE_ADDITIONAL_MONTHS_INVALID");

        if (!EndDate.HasValue)
            throw new DomainException("Hợp đồng xác định thời hạn phải có ngày kết thúc hiện tại.", "ERR_DEFINITE_END_DATE_NULL");

        DateTime newEndDate = EndDate.Value.AddMonths(additionalMonths);
        Renew(newEndDate, newBasicSalary);

        DurationMonths += additionalMonths;
        CurrentRenewalCount++;
    }
}
