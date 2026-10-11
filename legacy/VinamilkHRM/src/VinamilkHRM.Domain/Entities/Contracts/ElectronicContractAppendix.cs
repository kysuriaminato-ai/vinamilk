using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;

namespace VinamilkHRM.Domain.Entities.Contracts;

/// <summary>
/// Đại diện cho Phụ lục Hợp đồng Lao động Điện tử (Electronic Contract Appendix) của Vinamilk.
/// Tự động sinh ra khi có quyết định nâng bậc lương thường lệ hoặc Giám đốc đặc cách (CEO Fast-track Override).
/// Tích hợp chữ ký số và mã băm bảo mật chống giả mạo.
/// </summary>
public class ElectronicContractAppendix : BaseAuditableEntity
{
    public string AppendixNumber { get; private set; } = string.Empty;
    public int LaborContractId { get; private set; }
    public virtual LaborContract? LaborContract { get; private set; }

    public int EmployeeId { get; private set; }
    public virtual Employee? Employee { get; private set; }

    public DateTime EffectiveDate { get; private set; }
    public DateTime SignDate { get; private set; }

    public decimal PreviousSalary { get; private set; }
    public decimal NewSalary { get; private set; }
    public decimal PreviousAllowance { get; private set; }
    public decimal NewAllowance { get; private set; }
    public string AdjustmentReason { get; private set; } = string.Empty;

    public bool IsCeoOverride { get; private set; }
    public string? CeoOverrideCode { get; private set; }
    public string DigitalSignatureHash { get; private set; } = string.Empty;
    public string SignerName { get; private set; } = "Mai Kiều Liên";
    public string SignerTitle { get; private set; } = "Tổng Giám Đốc (CEO) - Vinamilk";
    public bool IsSigned { get; private set; } = true;

    protected ElectronicContractAppendix()
    {
    }

    public ElectronicContractAppendix(
        string appendixNumber,
        int laborContractId,
        int employeeId,
        DateTime effectiveDate,
        DateTime signDate,
        decimal previousSalary,
        decimal newSalary,
        decimal previousAllowance,
        decimal newAllowance,
        string adjustmentReason,
        bool isCeoOverride,
        string? ceoOverrideCode,
        string digitalSignatureHash,
        string signerName = "Mai Kiều Liên",
        string signerTitle = "Tổng Giám Đốc (CEO) - Vinamilk")
    {
        if (string.IsNullOrWhiteSpace(appendixNumber))
            throw new DomainException("Số phụ lục hợp đồng điện tử không được để trống.", "ERR_APPENDIX_NUM_EMPTY");

        if (employeeId <= 0)
            throw new DomainException("Mã nhân viên thụ hưởng phụ lục không hợp lệ.", "ERR_APPENDIX_EMP_INVALID");

        if (newSalary <= 0)
            throw new DomainException("Mức lương mới trên phụ lục hợp đồng phải lớn hơn 0.", "ERR_APPENDIX_SALARY_INVALID");

        if (string.IsNullOrWhiteSpace(digitalSignatureHash))
            throw new DomainException("Chữ ký điện tử / mã băm xác thực không được để trống.", "ERR_APPENDIX_SIGNATURE_EMPTY");

        AppendixNumber = appendixNumber.Trim().ToUpperInvariant();
        LaborContractId = laborContractId;
        EmployeeId = employeeId;
        EffectiveDate = effectiveDate;
        SignDate = signDate;
        PreviousSalary = previousSalary;
        NewSalary = newSalary;
        PreviousAllowance = previousAllowance;
        NewAllowance = newAllowance;
        AdjustmentReason = adjustmentReason?.Trim() ?? "Điều chỉnh thang bảng lương nhân sự Vinamilk";
        IsCeoOverride = isCeoOverride;
        CeoOverrideCode = ceoOverrideCode?.Trim();
        DigitalSignatureHash = digitalSignatureHash.Trim();
        SignerName = string.IsNullOrWhiteSpace(signerName) ? "Mai Kiều Liên" : signerName.Trim();
        SignerTitle = string.IsNullOrWhiteSpace(signerTitle) ? "Tổng Giám Đốc (CEO) - Vinamilk" : signerTitle.Trim();
        IsSigned = true;
    }
}
