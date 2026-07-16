namespace Usm.Shared.Utils.Excel.Reader.Internal;

/// <summary>Utility methods for converting Excel cell references to column indices.</summary>
internal static class ExcelCellHelper
{
    /// <summary>
    /// Converts an Excel cell reference such as "A1", "AB12", or "AAA1" to a 0-based column index.
    /// Returns -1 when the reference is null or empty.
    /// </summary>
    internal static int GetColumnIndex(string? cellReference)
    {
        if (string.IsNullOrEmpty(cellReference))
            return -1;

        int index = 0;
        foreach (var ch in cellReference)
        {
            if (!char.IsLetter(ch)) break;
            index = index * 26 + (ch - 'A' + 1);
        }

        return index - 1; // convert 1-based to 0-based
    }
}
