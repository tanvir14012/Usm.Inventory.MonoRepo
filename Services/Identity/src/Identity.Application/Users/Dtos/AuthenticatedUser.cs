namespace Identity.Application.Users.Dtos
{
    public sealed record AuthenticatedUser(Guid Id, string Username, string Email, string Name);
}
