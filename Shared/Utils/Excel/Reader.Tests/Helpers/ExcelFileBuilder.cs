using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Usm.Shared.Utils.Excel.Reader.Tests.Helpers;

/// <summary>
/// Builds minimal in-memory .xlsx files for use in tests.
/// </summary>
public static class ExcelFileBuilder
{
    /// <summary>
    /// Creates a workbook with a single sheet containing the given rows.
    /// Each row value is written as an inline string so no shared-string table is needed.
    /// Returns a rewound <see cref="MemoryStream"/>.
    /// </summary>
    public static MemoryStream Build(IEnumerable<IEnumerable<string?>> rows)
    {
        var ms = new MemoryStream();

        using (var doc = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart  = doc.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData     = new SheetData();

            uint rowIdx = 1;
            foreach (var rowValues in rows)
            {
                var row = new Row { RowIndex = rowIdx };
                uint colIdx = 1;
                foreach (var value in rowValues)
                {
                    row.AppendChild(new Cell
                    {
                        CellReference = CellRef(rowIdx, colIdx),
                        DataType = CellValues.InlineString,
                        InlineString = new InlineString
                        {
                            Text = new Text(value ?? string.Empty)
                        }
                    });
                    colIdx++;
                }
                sheetData.AppendChild(row);
                rowIdx++;
            }

            worksheetPart.Worksheet = new Worksheet(sheetData);
            worksheetPart.Worksheet.Save();

            var sheets = workbookPart.Workbook.AppendChild(new Sheets());
            sheets.AppendChild(new Sheet
            {
                Id      = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name    = "Sheet1"
            });

            workbookPart.Workbook.Save();
        }

        ms.Position = 0;
        return ms;
    }

    private static string CellRef(uint row, uint col)
    {
        var colName = string.Empty;
        uint c = col;
        while (c > 0)
        {
            colName = (char)('A' + (c - 1) % 26) + colName;
            c = (c - 1) / 26;
        }
        return $"{colName}{row}";
    }
}
