namespace Identity.Application.Users.Dtos;

public sealed record UserDto(
    Guid Id,
    string Username,
    string Email,
    bool IsActive,
    DateTimeOffset CreatedAt);
