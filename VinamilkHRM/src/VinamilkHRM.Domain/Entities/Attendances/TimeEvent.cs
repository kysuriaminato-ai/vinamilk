using System;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Attendances;

/// <summary>
/// Lần quẹt thẻ thô từ máy chấm công, nhận diện khuôn mặt hoặc di động (TimeEvent).
/// Ràng buộc duy nhất: Unique (DeviceId, EmployeeId, Timestamp).
/// </summary>
public class TimeEvent : BaseEntity
{
    public int EmployeeId { get; private set; }
    public virtual Employee? Employee { get; private set; }

    public DateTimeOffset Timestamp { get; private set; }
    public string DeviceId { get; private set; } = string.Empty;
    public PunchMethod Method { get; private set; } = PunchMethod.Fingerprint;

    public double? LocationLat { get; private set; }
    public double? LocationLong { get; private set; }
    public string? IpAddress { get; private set; }
    public bool IsProcessed { get; private set; } = false;

    protected TimeEvent()
    {
    }

    public TimeEvent(
        int employeeId,
        DateTimeOffset timestamp,
        string deviceId,
        PunchMethod method = PunchMethod.Fingerprint,
        double? locationLat = null,
        double? locationLong = null,
        string? ipAddress = null)
    {
        if (employeeId <= 0)
            throw new DomainException("Mã nhân viên chấm công không hợp lệ.", "ERR_TIMEEVENT_EMP_INVALID");

        if (string.IsNullOrWhiteSpace(deviceId))
            throw new DomainException("Mã thiết bị chấm công không được để trống.", "ERR_TIMEEVENT_DEVICE_EMPTY");

        EmployeeId = employeeId;
        Timestamp = timestamp;
        DeviceId = deviceId.Trim();
        Method = method;
        LocationLat = locationLat;
        LocationLong = locationLong;
        IpAddress = ipAddress?.Trim();
        IsProcessed = false;
    }

    public void MarkProcessed()
    {
        IsProcessed = true;
    }
}
