using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Contracts;

/// <summary>
/// Lớp cơ sở trừu tượng đại diện cho Hợp đồng lao động tại Vinamilk.
/// Áp dụng tính Đóng gói chặt chẽ: Kiểm soát việc kích hoạt, gia hạn, và chấm dứt hợp đồng lao động.
/// </summary>
public abstract class LaborContract : BaseAuditableEntity
{
    public string ContractNumber { get; protected set; } = string.Empty;
    public int EmployeeId { get; protected set; }
    public virtual Employee? Employee { get; protected set; }
    public ContractType ContractType { get; protected set; }
    public DateTime StartDate { get; protected set; }
    public DateTime? EndDate { get; protected set; }
    public decimal BasicSalary { get; protected set; }
    public decimal InsuranceSalary { get; protected set; }
    public int WorkingHoursPerWeek { get; protected set; } = 40;
    public ContractStatus Status { get; protected set; } = ContractStatus.Draft;
    public DateTime SignDate { get; protected set; }
    public DateTime? TerminationDate { get; protected set; }
    public string? TerminationReason { get; protected set; }

    protected LaborContract()
    {
    }

    protected LaborContract(
        string contractNumber,
        int employeeId,
        ContractType contractType,
        DateTime startDate,
        DateTime? endDate,
        decimal basicSalary,
        decimal insuranceSalary,
        DateTime signDate,
        int workingHoursPerWeek = 40)
    {
        if (string.IsNullOrWhiteSpace(contractNumber))
            throw new DomainException("Số hiệu hợp đồng lao động không được để trống.", "ERR_CONTRACT_NUM_EMPTY");

        if (employeeId <= 0)
            throw new DomainException("Mã nhân viên thụ hưởng hợp đồng không hợp lệ.", "ERR_CONTRACT_EMP_INVALID");

        if (basicSalary <= 0)
            throw new DomainException("Lương cơ bản trong hợp đồng phải lớn hơn 0.", "ERR_CONTRACT_SALARY_INVALID");

        if (insuranceSalary <= 0)
            throw new DomainException("Lương đóng bảo hiểm xã hội phải lớn hơn 0.", "ERR_CONTRACT_INSURANCE_INVALID");

        if (endDate.HasValue && endDate.Value < startDate)
            throw new DomainException("Ngày kết thúc hợp đồng không thể trước ngày bắt đầu.", "ERR_CONTRACT_DATES_INVALID");

        ContractNumber = contractNumber.Trim().ToUpperInvariant();
        EmployeeId = employeeId;
        ContractType = contractType;
        StartDate = startDate;
        EndDate = endDate;
        BasicSalary = basicSalary;
        InsuranceSalary = insuranceSalary;
        SignDate = signDate;
        WorkingHoursPerWeek = workingHoursPerWeek > 0 ? workingHoursPerWeek : 40;
        Status = ContractStatus.Active;
    }

    /// <summary>
    /// Kích hoạt trạng thái hiệu lực của hợp đồng.
    /// </summary>
    public void Activate()
    {
        if (Status == ContractStatus.Terminated)
            throw new DomainException("Không thể kích hoạt lại hợp đồng đã chấm dứt.", "ERR_CONTRACT_ALREADY_TERMINATED");

        Status = ContractStatus.Active;
    }

    /// <summary>
    /// Chấm dứt / Thanh lý hợp đồng lao động.
    /// </summary>
    public void Terminate(DateTime terminationDate, string reason)
    {
        if (Status == ContractStatus.Terminated)
            throw new DomainException("Hợp đồng này đã được chấm dứt trước đó.", "ERR_CONTRACT_ALREADY_TERMINATED");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Phải ghi rõ lý do hoặc căn cứ chấm dứt hợp đồng lao động.", "ERR_CONTRACT_TERMINATION_REASON_EMPTY");

        if (terminationDate < StartDate)
            throw new DomainException("Ngày chấm dứt không thể xảy ra trước ngày hợp đồng có hiệu lực.", "ERR_CONTRACT_TERMINATION_DATE_INVALID");

        Status = ContractStatus.Terminated;
        TerminationDate = terminationDate;
        TerminationReason = reason.Trim();
    }

    /// <summary>
    /// Gia hạn hợp đồng lao động và cập nhật chế độ lương mới.
    /// </summary>
    public virtual void Renew(DateTime newEndDate, decimal newBasicSalary)
    {
        if (Status == ContractStatus.Terminated)
            throw new DomainException("Không thể gia hạn hợp đồng đã chấm dứt.", "ERR_CONTRACT_CANNOT_RENEW_TERMINATED");

        if (newEndDate <= (EndDate ?? StartDate))
            throw new DomainException("Thời hạn gia hạn mới phải lớn hơn thời hạn hiện tại.", "ERR_CONTRACT_RENEW_DATE_INVALID");

        if (newBasicSalary <= 0)
            throw new DomainException("Mức lương mới sau gia hạn phải lớn hơn 0.", "ERR_CONTRACT_RENEW_SALARY_INVALID");

        EndDate = newEndDate;
        BasicSalary = newBasicSalary;
        Status = ContractStatus.Active;
    }

    public void UpdateSalaryTerms(decimal newBasicSalary, decimal newInsuranceSalary)
    {
        if (newBasicSalary <= 0 || newInsuranceSalary <= 0)
            throw new DomainException("Lương cơ bản và lương bảo hiểm phải lớn hơn 0.", "ERR_CONTRACT_SALARY_TERMS_INVALID");

        BasicSalary = newBasicSalary;
        InsuranceSalary = newInsuranceSalary;
    }
}
