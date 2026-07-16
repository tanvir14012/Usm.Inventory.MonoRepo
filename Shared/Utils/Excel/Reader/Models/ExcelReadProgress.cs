namespace Usm.Shared.Utils.Excel.Reader.Models;

/// <summary>Progress snapshot reported at each batch boundary.</summary>
public sealed class ExcelReadProgress
{
    public int ProcessedRows { get; init; }
    public int ErrorCount    { get; init; }
    public string? CorrelationId { get; init; }
}
