using System;

namespace VinamilkHRM.Domain.Enums;

[Flags]
public enum NotificationChannel
{
    None = 0,
    InApp = 1,
    Email = 2,
    Sms = 4
}

public enum NotificationCategory
{
    Workflow = 1,
    Contract = 2,
    Payroll = 3,
    Attendance = 4,
    System = 5
}

public enum OutboxMessageStatus
{
    Pending = 0,
    Processing = 1,
    Sent = 2,
    Failed = 3
}