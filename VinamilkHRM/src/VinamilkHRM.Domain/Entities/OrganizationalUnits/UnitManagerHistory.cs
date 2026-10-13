using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;

namespace VinamilkHRM.Domain.Entities.OrganizationalUnits;

/// <summary>
/// Lịch sử bổ nhiệm và giữ chức vụ Trưởng đơn vị tổ chức qua các thời kỳ.
/// </summary>
public class UnitManagerHistory : BaseAuditableEntity
{
    public int OrganizationalUnitId { get; private set; }
    public virtual OrganizationalUnit? OrganizationalUnit { get; private set; }

    public int EmployeeId { get; private set; }
    public virtual Employee? Employee { get; private set; }

    public DateOnly FromDate { get; private set; }
    public DateOnly? ToDate { get; private set; }
    public string? AppointmentDecisionNumber { get; private set; } // Số quyết định bổ nhiệm
    public string? Note { get; private set; }

    protected UnitManagerHistory()
    {
    }

    public UnitManagerHistory(
        int organizationalUnitId,
        int employeeId,
        DateOnly fromDate,
        DateOnly? toDate = null,
        string? appointmentDecisionNumber = null,
        string? note = null)
    {
        if (organizationalUnitId < 0)
            throw new DomainException("Mã đơn vị tổ chức không hợp lệ.", "ERR_UNIT_MGR_UNIT_INVALID");

        if (employeeId <= 0)
            throw new DomainException("Mã nhân viên bổ nhiệm không hợp lệ.", "ERR_UNIT_MGR_EMP_INVALID");

        if (toDate.HasValue && toDate.Value < fromDate)
            throw new DomainException("Ngày kết thúc nhiệm kỳ không thể trước ngày bắt đầu.", "ERR_UNIT_MGR_DATES_INVALID");

        OrganizationalUnitId = organizationalUnitId;
        EmployeeId = employeeId;
        FromDate = fromDate;
        ToDate = toDate;
        AppointmentDecisionNumber = appointmentDecisionNumber?.Trim();
        Note = note?.Trim();
    }

    public void EndTerm(DateOnly toDate)
    {
        if (toDate < FromDate)
            throw new DomainException("Ngày kết thúc nhiệm kỳ không thể trước ngày bắt đầu.", "ERR_UNIT_MGR_DATES_INVALID");

        ToDate = toDate;
    }
}
