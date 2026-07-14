using Identity.Domain.Common;
using System.Net;
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
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    private readonly List<UserCredential> _credentials = [];
    public IReadOnlyCollection<UserCredential> Credentials => _credentials;


    private User() { }

    public void AddCredential(UserCredential credential)
    {
        _credentials.Add(credential);
    }

    public static User Create(string username, string email, string passwordHash)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        user.RaiseDomainEvent(new UserCreatedDomainEvent(user.Id, username, email));
        return user;
    }
}
