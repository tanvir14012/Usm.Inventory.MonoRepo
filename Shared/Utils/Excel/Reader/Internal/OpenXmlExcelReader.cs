using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Usm.Shared.Utils.Excel.Reader.Abstractions;
using Usm.Shared.Utils.Excel.Reader.Models;
using Usm.Shared.Utils.Excel.Writer.Abstractions;
using Usm.Shared.Utils.Excel.Writer.Internal;

namespace Usm.Shared.Utils.Excel.Reader.Internal;

/// <summary>
/// SAX-based (streaming) Excel reader built on DocumentFormat.OpenXml.
/// Loads one row at a time; the shared-string table is the only structure kept in memory
/// for the full duration of the read.
/// </summary>
internal sealed class OpenXmlExcelReader(
    IServiceProvider serviceProvider,
    ILogger<OpenXmlExcelReader> logger) : IExcelReader
{
    public async Task<ExcelImportResult<TDto>> ReadAsync<TDto>(
        Stream stream,
        ExcelReaderOptions? options = null,
        CancellationToken cancellationToken = default)
        where TDto : new()
    {
        options ??= new ExcelReaderOptions();
        var correlationId = options.CorrelationId ?? Guid.NewGuid().ToString("N")[..8];

        using var logScope = logger.BeginScope(
            new Dictionary<string, object> { ["CorrelationId"] = correlationId });

        logger.LogInformation(
            "Starting Excel import. DTO={DtoType} Sheet={SheetIndex} SkipHeader={Skip}",
            typeof(TDto).Name, options.WorksheetIndex, options.SkipHeaderRow);

        var records = new List<TDto>();
        var errors  = new List<ExcelValidationError>();

        // Resolve dependencies once before entering the sync OpenXml loop.
        var populator = ResolvePopulator<TDto>();
        var validators = ResolveValidators<TDto>();

        // DocumentFormat.OpenXml is synchronous; wrap in Task.Run to free the caller thread.
        await Task.Run(() =>
            ReadWorkbook(stream, options, correlationId, populator, validators,
                         records, errors, cancellationToken),
            cancellationToken);

        logger.LogInformation(
            "Excel import finished. Records={Count} Errors={Errors}",
            records.Count, errors.Count);

        return new ExcelImportResult<TDto> { Records = records, Errors = errors };
    }

    // ── Core reader ───────────────────────────────────────────────────────────

    private static void ReadWorkbook<TDto>(
        Stream stream,
        ExcelReaderOptions options,
        string correlationId,
        IDtoPopulator<TDto> populator,
        IValidator<TDto>[] validators,
        List<TDto> records,
        List<ExcelValidationError> errors,
        CancellationToken ct)
        where TDto : new()
    {
        using var document = SpreadsheetDocument.Open(stream, isEditable: false);

        var workbookPart = document.WorkbookPart
            ?? throw new InvalidOperationException("The workbook has no WorkbookPart.");

        // Load shared-string table once — typically small relative to the sheet data.
        var sharedStrings = BuildSharedStringIndex(workbookPart);

        // Resolve worksheet
        var sheets = workbookPart.Workbook.Sheets?.Elements<Sheet>().ToArray()
            ?? throw new InvalidOperationException("The workbook contains no sheets.");

        if (options.WorksheetIndex >= sheets.Length)
            throw new ArgumentOutOfRangeException(
                nameof(options.WorksheetIndex),
                $"Worksheet index {options.WorksheetIndex} is out of range (workbook has {sheets.Length} sheet(s)).");

        var sheetId  = sheets[options.WorksheetIndex].Id?.Value
            ?? throw new InvalidOperationException("Sheet has no Id attribute.");
        var wsPart = (WorksheetPart)workbookPart.GetPartById(sheetId);

        // SAX loop — one Row DOM element loaded at a time, all others streamed past.
        using var reader  = OpenXmlReader.Create(wsPart);
        int rowNumber     = 0;
        int processedRows = 0;

        while (reader.Read())
        {
            ct.ThrowIfCancellationRequested();

            if (reader.ElementType != typeof(Row) || !reader.IsStartElement)
                continue;

            // LoadCurrentElement() reads the entire <Row>…</Row> into a DOM object and
            // advances the SAX reader past </Row> — subsequent iterations continue streaming.
            var row = (Row)reader.LoadCurrentElement()!;
            rowNumber++;

            if (rowNumber == 1 && options.SkipHeaderRow)
                continue;

            var cellValues = ExtractRowValues(row, sharedStrings);
            var (dto, conversionErrors) = populator.Populate(cellValues);

            // Collect type-conversion errors
            foreach (var ce in conversionErrors)
            {
                errors.Add(new ExcelValidationError
                {
                    RowNumber     = rowNumber,
                    ColumnIndex   = ce.ColumnIndex,
                    PropertyName  = ce.PropertyName,
                    OriginalValue = ce.OriginalValue,
                    ExpectedType  = ce.ExpectedType,
                    Message       = ce.Message
                });
            }

            // Only run semantic validators when type-conversion succeeded
            if (conversionErrors.Count == 0)
            {
                foreach (var validator in validators)
                {
                    var vResult = validator.Validate(dto);
                    foreach (var failure in vResult.Errors)
                    {
                        errors.Add(new ExcelValidationError
                        {
                            RowNumber     = rowNumber,
                            ColumnIndex   = -1,
                            PropertyName  = failure.PropertyName,
                            OriginalValue = failure.AttemptedValue?.ToString(),
                            ExpectedType  = "Validation",
                            Message       = failure.ErrorMessage
                        });
                    }
                }

                records.Add(dto);
            }

            processedRows++;

            if (options.FailFast && errors.Count > 0)
                break;

            if (processedRows % options.BatchSize == 0)
            {
                options.Progress?.Report(new ExcelReadProgress
                {
                    ProcessedRows = processedRows,
                    ErrorCount    = errors.Count,
                    CorrelationId = correlationId
                });
            }
        }

        options.Progress?.Report(new ExcelReadProgress
        {
            ProcessedRows = processedRows,
            ErrorCount    = errors.Count,
            CorrelationId = correlationId
        });
    }

    // ── Row value extraction ──────────────────────────────────────────────────

    private static IReadOnlyList<string?> ExtractRowValues(Row row, string[]? sharedStrings)
    {
        // Build a sparse column-index → raw-value map so gaps (empty cells) produce
        // null entries rather than shifting subsequent columns.
        var sparse = new SortedDictionary<int, string?>();

        foreach (var cell in row.Elements<Cell>())
        {
            var colIndex = ExcelCellHelper.GetColumnIndex(cell.CellReference?.Value);
            if (colIndex < 0) continue;
            sparse[colIndex] = GetCellStringValue(cell, sharedStrings);
        }

        if (sparse.Count == 0)
            return Array.Empty<string?>();

        var maxCol = sparse.Keys.Max();
        var dense  = new string?[maxCol + 1];
        foreach (var (col, val) in sparse)
            dense[col] = val;

        return dense;
    }

    private static string? GetCellStringValue(Cell cell, string[]? sharedStrings)
    {
        var dataType = cell.DataType?.Value;

        // Inline strings are stored in <is> element, NOT in <v> (CellValue).
        // Must be checked first before attempting CellValue lookup.
        if (dataType == CellValues.InlineString)
            return cell.InlineString?.Text?.Text;

        var raw = cell.CellValue?.Text;
        if (raw is null) return null;

        if (dataType == CellValues.SharedString)
        {
            if (sharedStrings is not null
                && int.TryParse(raw, out var idx)
                && idx < sharedStrings.Length)
                return sharedStrings[idx];
            return raw;
        }

        if (dataType == CellValues.Boolean)
            return raw == "1" ? "true" : "false";

        if (dataType == CellValues.Error)
            return null;

        // Number, date serial, formula cached value, etc.
        return raw;
    }

    private static string[]? BuildSharedStringIndex(WorkbookPart workbookPart)
        => workbookPart.SharedStringTablePart?.SharedStringTable
               .Elements<SharedStringItem>()
               .Select(si => si.InnerText)
               .ToArray();

    // ── DI helpers ────────────────────────────────────────────────────────────

    private IDtoPopulator<TDto> ResolvePopulator<TDto>() where TDto : new()
        => serviceProvider.GetService(typeof(IDtoPopulator<TDto>)) as IDtoPopulator<TDto>
           ?? new DtoPopulator<TDto>();   // fallback if DI not configured

    private IValidator<TDto>[] ResolveValidators<TDto>()
        => (serviceProvider.GetService(typeof(IEnumerable<IValidator<TDto>>))
               as IEnumerable<IValidator<TDto>>)?.ToArray()
           ?? [];
}
