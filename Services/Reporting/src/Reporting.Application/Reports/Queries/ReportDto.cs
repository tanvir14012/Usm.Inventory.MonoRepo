using Reporting.Domain.Reports;

namespace Reporting.Application.Reports.Queries;

public sealed record ReportDto(
    Guid Id,
    string ReportType,
    ReportStatus Status,
    DateTimeOffset? GeneratedAt,
    DateTimeOffset CreatedAt);
