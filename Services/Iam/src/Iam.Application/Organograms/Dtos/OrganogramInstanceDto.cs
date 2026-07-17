namespace Iam.Application.Organograms.Dtos;

public sealed record OrganogramInstanceDto(
    Guid Id,
    Guid TemplateId,
    string Name,
    string InstanceCode,
    int UnitsCount,
    DateTimeOffset CreatedAt);
