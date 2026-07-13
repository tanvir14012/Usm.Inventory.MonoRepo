using MediatR;
using Inspectorate.Domain.Inspections;

namespace Inspectorate.Application.Inspections.Queries;

public record InspectionDto(Guid Id, string InspectionNumber, InspectionStatus Status, DateTimeOffset ScheduledDate);

public record GetInspectionsQuery : IRequest<IReadOnlyList<InspectionDto>>;

public class GetInspectionsQueryHandler
    : IRequestHandler<GetInspectionsQuery, IReadOnlyList<InspectionDto>>
{
    public Task<IReadOnlyList<InspectionDto>> Handle(
        GetInspectionsQuery request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<InspectionDto>>(Array.Empty<InspectionDto>());
    }
}
