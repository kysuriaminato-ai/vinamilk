using Vinamilk.HRM.Domain.Common;

namespace Vinamilk.HRM.Domain.Events;

public sealed record EmployeeCreatedEvent(int EmployeeId, string EmployeeCode, string FullName, DateTime OccurredOnUtc) : IDomainEvent;

public sealed record LeaveRequestSubmittedEvent(int LeaveRequestId, int EmployeeId, decimal TotalDays, DateTime OccurredOnUtc) : IDomainEvent;

public sealed record PayrollPeriodLockedEvent(int PayrollPeriodId, int Month, int Year, DateTime OccurredOnUtc) : IDomainEvent;

public sealed record PersonnelTransferApprovedEvent(int TransferId, int EmployeeId, int ToUnitId, int ToPositionId, DateTime OccurredOnUtc) : IDomainEvent;

