using System;
using System.Collections.Generic;
using System.Linq;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.OrganizationalUnits;

namespace VinamilkHRM.Domain.Entities.Shifts;

/// <summary>
/// Định nghĩa Ca làm việc (ShiftDefinition).
/// Quản lý danh sách ShiftSegment (ca liên tục hoặc ca gãy 2 đoạn), cờ CrossesMidnight và đơn vị áp dụng.
/// </summary>
public class ShiftDefinition : BaseAuditableEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool CrossesMidnight { get; private set; } = false;
    public decimal TotalPlannedWorkHours { get; private set; } = 8.0m;
    public bool IsActive { get; private set; } = true;

    public int? OrganizationalUnitId { get; private set; }
    public virtual OrganizationalUnit? OrganizationalUnit { get; private set; }

    private readonly List<ShiftSegment> _segments = new();
    public virtual IReadOnlyCollection<ShiftSegment> Segments => _segments.AsReadOnly();

    protected ShiftDefinition()
    {
    }

    public ShiftDefinition(
        string code,
        string name,
        bool crossesMidnight = false,
        decimal totalPlannedWorkHours = 8.0m,
        int? organizationalUnitId = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Mã ca làm việc không được để trống.", "ERR_SHIFT_DEF_CODE_EMPTY");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Tên ca làm việc không được để trống.", "ERR_SHIFT_DEF_NAME_EMPTY");

        if (totalPlannedWorkHours <= 0)
            throw new DomainException("Tổng số giờ làm việc kế hoạch phải lớn hơn 0.", "ERR_SHIFT_HOURS_INVALID");

        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        CrossesMidnight = crossesMidnight;
        TotalPlannedWorkHours = totalPlannedWorkHours;
        OrganizationalUnitId = organizationalUnitId;
        IsActive = true;
    }

    public ShiftSegment AddSegment(
        int segmentIndex,
        TimeOnly startTime,
        TimeOnly endTime,
        int breakMinutes = 0,
        bool isPaidBreak = false)
    {
        var segment = new ShiftSegment(Id, segmentIndex, startTime, endTime, breakMinutes, isPaidBreak);
        _segments.Add(segment);

        // Tự động cập nhật tổng giờ kế hoạch và kiểm tra vắt qua đêm
        RecalculatePlannedHours();
        return segment;
    }

    public void RecalculatePlannedHours()
    {
        if (_segments.Count > 0)
        {
            TotalPlannedWorkHours = _segments.Sum(s => s.CalculateWorkHours());
            // Nếu có đoạn endTime < startTime hoặc đoạn sau bắt đầu qua ngày hôm sau thì ca là CrossesMidnight
            CrossesMidnight = CrossesMidnight || _segments.Any(s => s.EndTime < s.StartTime);
        }
    }

    public TimeOnly GetFirstStartTime()
    {
        if (_segments.Count == 0) return new TimeOnly(8, 0);
        return _segments.OrderBy(s => s.SegmentIndex).First().StartTime;
    }

    public TimeOnly GetLastEndTime()
    {
        if (_segments.Count == 0) return new TimeOnly(17, 0);
        return _segments.OrderBy(s => s.SegmentIndex).Last().EndTime;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}
