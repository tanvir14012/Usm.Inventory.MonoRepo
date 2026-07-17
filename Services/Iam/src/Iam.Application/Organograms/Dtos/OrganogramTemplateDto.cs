using Iam.Domain.Organograms;

namespace Iam.Application.Organograms.Dtos;

public sealed record OrganogramTemplateDto(
    Guid Id,
    string Name,
    string Version,
    OrganogramTemplateStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? FirstInstantiatedAt);
