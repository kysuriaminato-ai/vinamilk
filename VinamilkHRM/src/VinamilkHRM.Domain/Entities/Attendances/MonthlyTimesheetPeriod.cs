using System;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Entities.OrganizationalUnits;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Attendances;

/// <summary>
/// Kỳ chấm công hàng tháng của đơn vị (MonthlyTimesheetPeriod).
/// Chu trình: Dự thảo (Draft) -> TeamLeader xác nhận -> UnitManager duyệt -> HR khóa (Locked).
/// RÀNG BUỘC PHÁP LÝ: Kỳ công sau khi khóa là BẤT BIẾN, mọi sai sót xử lý bằng AttendanceAdjustment ở kỳ sau.
/// </summary>
public class MonthlyTimesheetPeriod : BaseAuditableEntity
{
    public string Period { get; private set; } = string.Empty; // Định dạng "YYYY-MM"
    public int OrganizationalUnitId { get; private set; }
    public virtual OrganizationalUnit? OrganizationalUnit { get; private set; }

    public TimesheetPeriodStatus Status { get; private set; } = TimesheetPeriodStatus.Draft;

    public int? ConfirmedBy { get; private set; }
    public virtual Employee? ConfirmedByEmployee { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }

    public int? ApprovedBy { get; private set; }
    public virtual Employee? ApprovedByEmployee { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }

    public int? LockedBy { get; private set; }
    public virtual Employee? LockedByEmployee { get; private set; }
    public DateTimeOffset? LockedAt { get; private set; }

    protected MonthlyTimesheetPeriod()
    {
    }

    public MonthlyTimesheetPeriod(string period, int organizationalUnitId)
    {
        if (string.IsNullOrWhiteSpace(period))
            throw new DomainException("Kỳ chấm công không được để trống.", "ERR_PERIOD_CODE_EMPTY");

        if (organizationalUnitId <= 0)
            throw new DomainException("Mã đơn vị không hợp lệ.", "ERR_PERIOD_UNIT_INVALID");

        Period = period.Trim();
        OrganizationalUnitId = organizationalUnitId;
        Status = TimesheetPeriodStatus.Draft;
    }

    public void ConfirmByLeader(int leaderId)
    {
        EnsureNotLocked();
        ConfirmedBy = leaderId;
        ConfirmedAt = DateTimeOffset.UtcNow;
        Status = TimesheetPeriodStatus.ConfirmedByLeader;
    }

    public void ApproveByManager(int managerId)
    {
        EnsureNotLocked();
        ApprovedBy = managerId;
        ApprovedAt = DateTimeOffset.UtcNow;
        Status = TimesheetPeriodStatus.ApprovedByManager;
    }

    public void Lock(int hrUserId)
    {
        LockedBy = hrUserId;
        LockedAt = DateTimeOffset.UtcNow;
        Status = TimesheetPeriodStatus.Locked;
    }

    public void EnsureNotLocked()
    {
        if (Status == TimesheetPeriodStatus.Locked)
        {
            throw new DomainException(
                $"Kỳ chấm công {Period} của đơn vị đã bị khóa sổ (Locked), không thể chỉnh sửa dữ liệu.",
                "ERR_ATT_PERIOD_LOCKED");
        }
    }
}
