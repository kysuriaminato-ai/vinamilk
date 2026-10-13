using System;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;

namespace VinamilkHRM.Domain.Entities.Attendances;

/// <summary>
/// Khoản điều chỉnh công (AttendanceAdjustment).
/// Ghi nhận sai sót của kỳ công đã khóa và cộng/trừ vào kỳ tính lương kế tiếp.
/// </summary>
public class AttendanceAdjustment : BaseAuditableEntity
{
    public string OriginalPeriod { get; private set; } = string.Empty; // Kỳ gốc bị sai sót ("YYYY-MM")
    public string TargetPeriod { get; private set; } = string.Empty;   // Kỳ áp dụng điều chỉnh ("YYYY-MM")

    public int EmployeeId { get; private set; }
    public virtual Employee? Employee { get; private set; }

    public decimal AdjustedWorkDays { get; private set; } = 0m;
    public decimal AdjustedOtHours { get; private set; } = 0m;
    public decimal AdjustedNightHours { get; private set; } = 0m;

    public string Reason { get; private set; } = string.Empty;

    protected AttendanceAdjustment()
    {
    }

    public AttendanceAdjustment(
        string originalPeriod,
        string targetPeriod,
        int employeeId,
        decimal adjustedWorkDays,
        decimal adjustedOtHours,
        decimal adjustedNightHours,
        string reason)
    {
        if (string.IsNullOrWhiteSpace(originalPeriod) || string.IsNullOrWhiteSpace(targetPeriod))
            throw new DomainException("Kỳ công gốc và kỳ điều chỉnh không được để trống.", "ERR_ADJ_PERIOD_EMPTY");

        if (employeeId <= 0)
            throw new DomainException("Mã nhân viên không hợp lệ.", "ERR_ADJ_EMP_INVALID");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Lý do điều chỉnh không được để trống.", "ERR_ADJ_REASON_EMPTY");

        OriginalPeriod = originalPeriod.Trim();
        TargetPeriod = targetPeriod.Trim();
        EmployeeId = employeeId;
        AdjustedWorkDays = adjustedWorkDays;
        AdjustedOtHours = adjustedOtHours;
        AdjustedNightHours = adjustedNightHours;
        Reason = reason.Trim();
    }
}
