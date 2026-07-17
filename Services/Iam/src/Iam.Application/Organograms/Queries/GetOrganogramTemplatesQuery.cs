using Iam.Application.Abstractions;
using Iam.Application.Organograms.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Iam.Application.Organograms.Queries;

public sealed record GetOrganogramTemplatesQuery : IRequest<IReadOnlyList<OrganogramTemplateDto>>;

public sealed class GetOrganogramTemplatesQueryHandler(IIamDbContext dbContext)
    : IRequestHandler<GetOrganogramTemplatesQuery, IReadOnlyList<OrganogramTemplateDto>>
{
    public async Task<IReadOnlyList<OrganogramTemplateDto>> Handle(GetOrganogramTemplatesQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.OrganogramTemplates
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new OrganogramTemplateDto(
                x.Id,
                x.Name,
                x.Version,
                x.Status,
                x.CreatedAt,
                x.FirstInstantiatedAt))
            .ToListAsync(cancellationToken);
    }
}
