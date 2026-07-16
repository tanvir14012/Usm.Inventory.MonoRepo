namespace Usm.Shared.Utils.Excel.Reader.Models;

/// <summary>
/// A single validation or type-conversion error encountered while importing an Excel row.
/// </summary>
public sealed class ExcelValidationError
{
    /// <summary>1-based row number in the worksheet (counting the header row).</summary>
    public int RowNumber { get; init; }

    /// <summary>0-based column index. -1 when the error is not column-specific (e.g. FluentValidation).</summary>
    public int ColumnIndex { get; init; }

    /// <summary>Name of the DTO property that could not be populated.</summary>
    public string PropertyName { get; init; } = string.Empty;

    /// <summary>The raw cell text that failed conversion.</summary>
    public string? OriginalValue { get; init; }

    /// <summary>Friendly CLR type name that was expected (e.g. "Int32", "DateTime?").</summary>
    public string ExpectedType { get; init; } = string.Empty;

    /// <summary>Human-readable description of why the value is invalid.</summary>
    public string Message { get; init; } = string.Empty;
}
