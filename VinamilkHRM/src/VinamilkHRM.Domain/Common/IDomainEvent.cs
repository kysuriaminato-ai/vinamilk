namespace VinamilkHRM.Domain.Common;

/// <summary>
/// Đánh dấu một sự kiện miền (Domain Event) phát sinh từ các hành vi nghiệp vụ.
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
