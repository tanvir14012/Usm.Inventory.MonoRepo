namespace Usm.Shared.Utils.Excel.Reader.Models;

/// <summary>
/// The result of an Excel import operation.
/// </summary>
public sealed class ExcelImportResult<TDto>
{
    /// <summary>Successfully parsed and validated records.</summary>
    public IReadOnlyList<TDto> Records { get; init; } = [];

    /// <summary>
    /// All conversion and validation errors collected across every row.
    /// Rows with conversion errors are excluded from <see cref="Records"/>.
    /// </summary>
    public IReadOnlyList<ExcelValidationError> Errors { get; init; } = [];

    public bool HasErrors => Errors.Count > 0;
    public bool IsSuccess => !HasErrors;
}
