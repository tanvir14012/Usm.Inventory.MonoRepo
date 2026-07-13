namespace Usm.Shared.Contracts.Events;

public interface IIntegrationEvent
{
    Guid Id { get; }
    DateTimeOffset OccurredOn { get; }
}