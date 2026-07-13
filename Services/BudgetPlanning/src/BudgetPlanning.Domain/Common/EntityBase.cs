namespace BudgetPlanning.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}

public abstract class EntityBase<TId> where TId : struct
{
    public TId Id { get; protected set; }
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    protected void RaiseDomainEvent(IDomainEvent e) => _domainEvents.Add(e);
    public void ClearDomainEvents() => _domainEvents.Clear();
}

public abstract class AggregateRoot<TId> : EntityBase<TId> where TId : struct { }
