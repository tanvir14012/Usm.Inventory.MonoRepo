using MediatR;

namespace Reporting.Application.Reports.Queries;

public sealed record GetReportsQuery : IRequest<IReadOnlyList<ReportDto>>;
