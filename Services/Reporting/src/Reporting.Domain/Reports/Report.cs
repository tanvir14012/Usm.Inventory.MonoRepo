using Reporting.Domain.Common;
using Usm.Shared.Contracts.Localization;
using Usm.Shared.Data.DbContextExtensions;

namespace Reporting.Domain.Reports;

public enum ReportStatus { Queued, Generating, Completed, Failed }

public sealed class Report : AggregateRoot<Guid>, IAuditable
{
    public LocalizedText Title { get; private set; } = LocalizedText.Empty;
    public string ReportType { get; private set; } = string.Empty;
    public Guid GeneratedById { get; private set; }
    public string Parameters { get; private set; } = string.Empty;
    public ReportStatus Status { get; private set; }
    public string? OutputPath { get; private set; }
    public DateTimeOffset? GeneratedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    private Report() { }

    public static Report Create(LocalizedText title, string reportType, Guid generatedById, string parameters)
    {
        return new Report
        {
            Id = Guid.NewGuid(),
            Title = title,
            ReportType = reportType,
            GeneratedById = generatedById,
            Parameters = parameters,
            Status = ReportStatus.Queued,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void MarkCompleted(string outputPath, DateTimeOffset generatedAt)
    {
        OutputPath = outputPath;
        GeneratedAt = generatedAt;
        Status = ReportStatus.Completed;
    }

    public void MarkFailed() => Status = ReportStatus.Failed;
}
