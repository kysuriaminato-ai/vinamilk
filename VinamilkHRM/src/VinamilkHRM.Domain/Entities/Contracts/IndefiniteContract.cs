using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Contracts;

/// <summary>
/// Đại diện cho Hợp đồng lao động không xác định thời hạn (chính thức lâu dài) tại Vinamilk.
/// </summary>
public class IndefiniteContract : LaborContract
{
    public string SeverancePackageTerms { get; private set; } = "Theo chế độ trợ cấp thôi việc hiện hành của Tập đoàn Vinamilk";
    public decimal AnnualTenureBonusRate { get; private set; } = 5.0m; // Tỷ lệ thưởng thâm niên hàng năm (%)

    protected IndefiniteContract() : base()
    {
    }

    public IndefiniteContract(
        string contractNumber,
        int employeeId,
        DateTime startDate,
        decimal basicSalary,
        decimal insuranceSalary,
        DateTime signDate,
        string severancePackageTerms,
        decimal annualTenureBonusRate = 5.0m,
        int workingHoursPerWeek = 40)
        : base(
            contractNumber,
            employeeId,
            ContractType.Indefinite,
            startDate,
            null, // Không xác định thời hạn => Không có ngày kết thúc
            basicSalary,
            insuranceSalary,
            signDate,
            workingHoursPerWeek)
    {
        if (annualTenureBonusRate < 0)
            throw new DomainException("Tỷ lệ thưởng thâm niên không thể âm.", "ERR_INDEFINITE_BONUS_NEGATIVE");

        SeverancePackageTerms = string.IsNullOrWhiteSpace(severancePackageTerms)
            ? "Theo chế độ trợ cấp thôi việc hiện hành của Tập đoàn Vinamilk"
            : severancePackageTerms.Trim();
        AnnualTenureBonusRate = annualTenureBonusRate;
    }

    public void UpdateTenureTerms(decimal annualBonusRate, string severanceTerms)
    {
        if (annualBonusRate < 0)
            throw new DomainException("Tỷ lệ thưởng thâm niên không thể âm.", "ERR_INDEFINITE_BONUS_NEGATIVE");

        AnnualTenureBonusRate = annualBonusRate;
        if (!string.IsNullOrWhiteSpace(severanceTerms))
            SeverancePackageTerms = severanceTerms.Trim();
    }
}
