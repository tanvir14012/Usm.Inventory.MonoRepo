using Usm.Shared.Utils.Excel.Writer.Models;

namespace Usm.Shared.Utils.Excel.Writer.Abstractions;

/// <summary>
/// Populates a DTO from a position-ordered list of raw cell string values.
/// Column index maps directly to DTO property index (declaration order).
/// Empty/missing cells leave the corresponding property at its CLR default; they
/// do NOT shift subsequent column→property mappings.
/// </summary>
public interface IDtoPopulator<TDto> where TDto : new()
{
    /// <summary>
    /// Creates a new <typeparamref name="TDto"/> and sets each property from the
    /// corresponding cell value. Conversion errors are collected rather than thrown.
    /// </summary>
    /// <param name="cellValues">
    /// Ordered raw string values; index 0 maps to property 0, index 1 to property 1, etc.
    /// Null entries represent empty cells.
    /// </param>
    /// <returns>
    /// The populated DTO (may be partially populated if errors occurred) and the
    /// list of conversion errors.
    /// </returns>
    (TDto Dto, IReadOnlyList<PropertyConversionError> Errors) Populate(
        IReadOnlyList<string?> cellValues);
}
