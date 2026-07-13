using MediatR;

namespace Reporting.Application.Reports.Queries;

public sealed class GetReportsQueryHandler : IRequestHandler<GetReportsQuery, IReadOnlyList<ReportDto>>
{
    public Task<IReadOnlyList<ReportDto>> Handle(GetReportsQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<ReportDto>>(Array.Empty<ReportDto>());
    }
}
