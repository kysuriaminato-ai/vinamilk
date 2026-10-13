using VinamilkHRM.Domain.Common;

namespace VinamilkHRM.Domain.Entities.Employees;

/// <summary>
/// Người phụ thuộc giảm trừ gia cảnh thuế TNCN theo Luật Thuế Thu Nhập Cá Nhân.
/// </summary>
public class Dependent : BaseAuditableEntity
{
    public int EmployeeId { get; private set; }
    public virtual Employee? Employee { get; private set; }

    public string FullName { get; private set; } = string.Empty;
    public string Relationship { get; private set; } = string.Empty; // Con đẻ, Bố mẹ, Vợ/chồng...
    public string? TaxCode { get; private set; }                     // Mã số thuế người phụ thuộc
    public string? CitizenId { get; private set; }                   // Số CCCD hoặc giấy khai sinh
    public DateOnly? DateOfBirth { get; private set; }
    public DateOnly FromMonth { get; private set; }                  // Tháng bắt đầu tính giảm trừ
    public DateOnly? ToMonth { get; private set; }                    // Tháng kết thúc tính giảm trừ
    public bool IsActive { get; private set; } = true;
    public string? Note { get; private set; }

    protected Dependent()
    {
    }

    public Dependent(
        int employeeId,
        string fullName,
        string relationship,
        DateOnly fromMonth,
        string? taxCode = null,
        string? citizenId = null,
        DateOnly? dateOfBirth = null,
        DateOnly? toMonth = null,
        string? note = null)
    {
        if (employeeId <= 0)
            throw new DomainException("Mã nhân viên người phụ thuộc không hợp lệ.", "ERR_DEP_EMP_INVALID");

        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("Họ và tên người phụ thuộc không được để trống.", "ERR_DEP_NAME_EMPTY");

        if (string.IsNullOrWhiteSpace(relationship))
            throw new DomainException("Mối quan hệ với người nộp thuế không được để trống.", "ERR_DEP_RELATION_EMPTY");

        if (toMonth.HasValue && toMonth.Value < fromMonth)
            throw new DomainException("Tháng kết thúc giảm trừ không thể trước tháng bắt đầu.", "ERR_DEP_DATES_INVALID");

        EmployeeId = employeeId;
        FullName = fullName.Trim();
        Relationship = relationship.Trim();
        FromMonth = fromMonth;
        TaxCode = taxCode?.Trim();
        CitizenId = citizenId?.Trim();
        DateOfBirth = dateOfBirth;
        ToMonth = toMonth;
        IsActive = true;
        Note = note?.Trim();
    }

    public void Deactivate(DateOnly toMonth)
    {
        if (toMonth < FromMonth)
            throw new DomainException("Tháng kết thúc không thể trước tháng bắt đầu.", "ERR_DEP_DATES_INVALID");

        ToMonth = toMonth;
        IsActive = false;
    }
}
