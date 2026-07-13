namespace TrafficSecurity.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}

public abstract class EntityBase<TId> where TId : struct
{
    public TId Id { get; protected set; }
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
}

public abstract class AggregateRoot<TId> : EntityBase<TId> where TId : struct { }
