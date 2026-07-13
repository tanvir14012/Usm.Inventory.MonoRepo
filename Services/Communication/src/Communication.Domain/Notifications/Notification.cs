using Communication.Domain.Common;
using Usm.Shared.Data.DbContextExtensions;

namespace Communication.Domain.Notifications;

public enum NotificationChannel
{
    Email,
    InApp,
    Sms
}

public enum NotificationStatus
{
    Pending,
    Sent,
    Delivered,
    Failed,
    Read
}

public class Notification : AggregateRoot<Guid>, IAuditable
{
    public Guid RecipientId { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public NotificationChannel Channel { get; private set; }
    public NotificationStatus Status { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    private Notification() { }

    public static Notification Create(Guid recipientId, string subject, string body, NotificationChannel channel)
    {
        return new Notification
        {
            Id = Guid.NewGuid(),
            RecipientId = recipientId,
            Subject = subject,
            Body = body,
            Channel = channel,
            Status = NotificationStatus.Pending
        };
    }

    public void MarkSent()
    {
        Status = NotificationStatus.Sent;
        SentAt = DateTimeOffset.UtcNow;
    }

    public void MarkRead()
    {
        Status = NotificationStatus.Read;
        ReadAt = DateTimeOffset.UtcNow;
    }
}
