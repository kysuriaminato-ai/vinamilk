namespace Vinamilk.HRM.Domain.Common;

public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}

public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
}

public interface IAuditableEntity : ISoftDeletable
{
    DateTime CreatedAt { get; set; }
    string CreatedBy { get; set; }
    DateTime? UpdatedAt { get; set; }
    string? UpdatedBy { get; set; }
    byte[] RowVersion { get; set; }
}

public abstract class BaseAuditableEntity<TKey> : IAuditableEntity
{
    public TKey Id { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; } = false;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void RemoveDomainEvent(IDomainEvent domainEvent) => _domainEvents.Remove(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
}

public abstract class BaseAuditableEntity : BaseAuditableEntity<int>
{
}

public abstract class BaseLongAuditableEntity : BaseAuditableEntity<long>
{
}

