using Usm.Shared.Utils.Excel.Reader.Models;

namespace Usm.Shared.Utils.Excel.Reader.Abstractions;

/// <summary>
/// Streams an .xlsx file and maps data rows to <typeparamref name="TDto"/> instances
/// using position-based column mapping:
/// <list type="bullet">
///   <item>Column 1 → DTO property 1 (first declared writable property)</item>
///   <item>Column 2 → DTO property 2, …</item>
///   <item>Empty cells never shift subsequent column→property mappings.</item>
/// </list>
/// </summary>
public interface IExcelReader
{
    /// <summary>
    /// Reads the Excel stream and returns all successfully mapped DTOs together with
    /// any type-conversion or validation errors encountered.
    /// Never throws for individual invalid rows (unless <see cref="ExcelReaderOptions.FailFast"/> is set).
    /// </summary>
    Task<ExcelImportResult<TDto>> ReadAsync<TDto>(
        Stream stream,
        ExcelReaderOptions? options = null,
        CancellationToken cancellationToken = default)
        where TDto : new();
}
