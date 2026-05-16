namespace InvoiceFlow.Domain.Common;

/// <summary>
/// Base class for all domain entities.
/// Provides identity, timestamps, and domain event support.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }

    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>Called by Domain Services (same assembly) to raise a domain event.</summary>
    internal void RaiseDomainEvent(IDomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>Called by Domain Services (same assembly) to stamp the updated timestamp.</summary>
    internal void MarkUpdated() => UpdatedAt = DateTime.UtcNow;
}
