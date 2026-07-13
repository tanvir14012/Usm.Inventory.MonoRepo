namespace Iam.Application.Roles.Dtos;

public sealed record RoleDto(
    Guid Id,
    string Name,
    string Description,
    bool IsActive,
    DateTimeOffset CreatedAt);
