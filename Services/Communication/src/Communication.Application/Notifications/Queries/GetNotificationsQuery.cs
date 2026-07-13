using MediatR;
using Communication.Domain.Notifications;

namespace Communication.Application.Notifications.Queries;

public record NotificationDto(Guid Id, Guid RecipientId, string Subject, NotificationStatus Status, DateTimeOffset CreatedAt);

public record GetNotificationsQuery : IRequest<IReadOnlyList<NotificationDto>>;

public class GetNotificationsQueryHandler
    : IRequestHandler<GetNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    public Task<IReadOnlyList<NotificationDto>> Handle(
        GetNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<NotificationDto>>(Array.Empty<NotificationDto>());
    }
}
