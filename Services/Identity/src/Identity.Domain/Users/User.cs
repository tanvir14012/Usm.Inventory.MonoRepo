using Identity.Domain.Common;
using Usm.Shared.Data.DbContextExtensions;

namespace Identity.Domain.Users;

public sealed record UserCreatedDomainEvent(Guid UserId, string Username, string Email) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}

public sealed class User : AggregateRoot<Guid>, IAuditable
{
    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    private User() { }

    public static User Create(string username, string email, string passwordHash)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            PasswordHash = passwordHash,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        user.RaiseDomainEvent(new UserCreatedDomainEvent(user.Id, username, email));
        return user;
    }
}
