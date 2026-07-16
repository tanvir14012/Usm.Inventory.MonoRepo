namespace Usm.Shared.Utils.Excel.Writer.Models;

/// <summary>
/// Describes a failed type-conversion for a single cell during DTO population.
/// </summary>
public sealed record PropertyConversionError(
    int ColumnIndex,
    string PropertyName,
    string? OriginalValue,
    string ExpectedType,
    string Message);
