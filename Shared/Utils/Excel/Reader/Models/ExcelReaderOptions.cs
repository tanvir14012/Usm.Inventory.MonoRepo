namespace Usm.Shared.Utils.Excel.Reader.Models;

/// <summary>
/// Configuration options for a single <see cref="Abstractions.IExcelReader.ReadAsync{TDto}"/> call.
/// </summary>
public sealed class ExcelReaderOptions
{
    /// <summary>Skip the first row (treated as a header). Default: true.</summary>
    public bool SkipHeaderRow { get; set; } = true;

    /// <summary>
    /// When true, import stops at the first conversion error rather than collecting all errors.
    /// Default: false.
    /// </summary>
    public bool FailFast { get; set; } = false;

    /// <summary>Number of rows between progress notifications. Default: 100.</summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>0-based index of the worksheet to read. Default: 0.</summary>
    public int WorksheetIndex { get; set; } = 0;

    /// <summary>
    /// Correlation ID included in every structured log message.
    /// Auto-generated when null.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>Optional progress reporter fired every <see cref="BatchSize"/> rows.</summary>
    public IProgress<ExcelReadProgress>? Progress { get; set; }
}
