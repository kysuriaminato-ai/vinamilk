using VinamilkHRM.Domain.Common;

namespace VinamilkHRM.Domain.Entities.Employees;

/// <summary>
/// Quá trình công tác trước đây và lịch sử làm việc của nhân sự (tương ứng qt_congtac).
/// </summary>
public class EmployeeWorkHistory : BaseAuditableEntity
{
    public int EmployeeId { get; private set; }
    public virtual Employee? Employee { get; private set; }

    public DateOnly FromDate { get; private set; }
    public DateOnly? ToDate { get; private set; }
    public string CompanyName { get; private set; } = string.Empty;
    public string? JobTitle { get; private set; }
    public string? MainResponsibilities { get; private set; }
    public string? ReferenceContact { get; private set; }
    public string? Note { get; private set; }
    public int? LegacyId { get; private set; }

    protected EmployeeWorkHistory()
    {
    }

    public EmployeeWorkHistory(
        int employeeId,
        DateOnly fromDate,
        string companyName,
        DateOnly? toDate = null,
        string? jobTitle = null,
        string? mainResponsibilities = null,
        string? referenceContact = null,
        string? note = null,
        int? legacyId = null)
    {
        if (employeeId <= 0)
            throw new DomainException("Mã nhân viên quá trình công tác không hợp lệ.", "ERR_WORK_HIST_EMP_INVALID");

        if (string.IsNullOrWhiteSpace(companyName))
            throw new DomainException("Tên cơ quan/đơn vị công tác không được để trống.", "ERR_WORK_HIST_COMPANY_EMPTY");

        if (toDate.HasValue && toDate.Value < fromDate)
            throw new DomainException("Ngày kết thúc công tác không thể trước ngày bắt đầu.", "ERR_WORK_HIST_DATES_INVALID");

        EmployeeId = employeeId;
        FromDate = fromDate;
        CompanyName = companyName.Trim();
        ToDate = toDate;
        JobTitle = jobTitle?.Trim();
        MainResponsibilities = mainResponsibilities?.Trim();
        ReferenceContact = referenceContact?.Trim();
        Note = note?.Trim();
        LegacyId = legacyId;
    }
}
