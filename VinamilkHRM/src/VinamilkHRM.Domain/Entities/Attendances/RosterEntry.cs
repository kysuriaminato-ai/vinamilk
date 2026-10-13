using System;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Entities.Shifts;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Attendances;

/// <summary>
/// Lịch phân ca chi tiết cho từng nhân viên theo từng ngày (RosterEntry).
/// Ràng buộc duy nhất: Unique (EmployeeId, WorkDate).
/// </summary>
public class RosterEntry : BaseAuditableEntity
{
    public int EmployeeId { get; private set; }
    public virtual Employee? Employee { get; private set; }

    public DateOnly WorkDate { get; private set; }

    public int? ShiftDefinitionId { get; private set; }
    public virtual ShiftDefinition? ShiftDefinition { get; private set; }

    public bool IsOff { get; private set; } = false;
    public RosterSource Source { get; private set; } = RosterSource.Pattern;
    public RosterStatus Status { get; private set; } = RosterStatus.Draft;
    public string? Note { get; private set; }

    protected RosterEntry()
    {
    }

    public RosterEntry(
        int employeeId,
        DateOnly workDate,
        int? shiftDefinitionId,
        bool isOff = false,
        RosterSource source = RosterSource.Pattern,
        RosterStatus status = RosterStatus.Draft,
        string? note = null)
    {
        if (employeeId <= 0)
            throw new DomainException("Mã nhân viên phân ca không hợp lệ.", "ERR_ROSTER_EMP_INVALID");

        EmployeeId = employeeId;
        WorkDate = workDate;
        ShiftDefinitionId = shiftDefinitionId;
        IsOff = isOff || (shiftDefinitionId == null);
        Source = source;
        Status = status;
        Note = note?.Trim();
    }

    public void Publish()
    {
        Status = RosterStatus.Published;
    }

    public void UpdateShift(int? newShiftDefinitionId, bool isOff, RosterSource source, string? note = null)
    {
        ShiftDefinitionId = newShiftDefinitionId;
        IsOff = isOff || (newShiftDefinitionId == null);
        Source = source;
        if (!string.IsNullOrWhiteSpace(note))
        {
            Note = note.Trim();
        }
    }

    public void ApplySwap(int? swappedShiftDefinitionId, bool isOff, string? note = null)
    {
        ShiftDefinitionId = swappedShiftDefinitionId;
        IsOff = isOff || (swappedShiftDefinitionId == null);
        Source = RosterSource.Swap;
        Note = note ?? "Đổi ca làm việc (ShiftSwap)";
    }
}
