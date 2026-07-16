using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Usm.Shared.Utils.Excel.Reader.Abstractions;
using Usm.Shared.Utils.Excel.Reader.Extensions;
using Usm.Shared.Utils.Excel.Reader.Internal;
using Usm.Shared.Utils.Excel.Reader.Models;
using Usm.Shared.Utils.Excel.Reader.Tests.Helpers;
using Xunit;

namespace Usm.Shared.Utils.Excel.Reader.Tests;

/// <summary>
/// Integration tests for <see cref="OpenXmlExcelReader"/>.
/// Each test builds a real in-memory .xlsx file and runs the full read pipeline.
/// </summary>
public sealed class OpenXmlExcelReaderIntegrationTests
{
    private sealed class PersonDto
    {
        public string Name       { get; set; } = string.Empty;
        public int    Age        { get; set; }
        public decimal Salary    { get; set; }
        public bool   IsActive   { get; set; }
    }

    private sealed class NullableDto
    {
        public int?      NullableInt     { get; set; }
        public decimal?  NullableDecimal { get; set; }
        public DateTime? NullableDate    { get; set; }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static IExcelReader BuildReader()
    {
        var services = new ServiceCollection();
        services.AddExcelReader();
        services.AddLogging();
        var sp = services.BuildServiceProvider();
        return sp.GetRequiredService<IExcelReader>();
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ReadAsync_MapsColumnsToPropertiesInDeclarationOrder()
    {
        using var stream = ExcelFileBuilder.Build(
        [
            ["Name",    "Age", "Salary", "IsActive"],   // header
            ["Alice",   "30",  "5000.50","true"],
            ["Bob",     "25",  "3200.00","false"]
        ]);

        var reader = BuildReader();
        var result = await reader.ReadAsync<PersonDto>(stream);

        Assert.False(result.HasErrors, string.Join(", ", result.Errors.Select(e => e.Message)));
        Assert.Equal(2, result.Records.Count);

        var alice = result.Records[0];
        Assert.Equal("Alice", alice.Name);
        Assert.Equal(30,      alice.Age);
        Assert.Equal(5000.50m, alice.Salary);
        Assert.True(alice.IsActive);
    }

    [Fact]
    public async Task ReadAsync_EmptyCellDoesNotShiftSubsequentColumns()
    {
        // Column B (Age) is empty — Salary and IsActive must still land in columns C and D.
        using var stream = ExcelFileBuilder.Build(
        [
            ["Name", "Age", "Salary", "IsActive"],
            ["Carol", "",  "9000.00", "true"]
        ]);

        var reader = BuildReader();
        var result = await reader.ReadAsync<PersonDto>(stream);

        // Age conversion of "" yields default(int) = 0; no error expected
        Assert.False(result.HasErrors, string.Join(", ", result.Errors.Select(e => e.Message)));
        var carol = result.Records[0];
        Assert.Equal("Carol",   carol.Name);
        Assert.Equal(0,         carol.Age);
        Assert.Equal(9000.00m,  carol.Salary);
        Assert.True(carol.IsActive);
    }

    [Fact]
    public async Task ReadAsync_CollectsErrorsWithoutStoppingImport()
    {
        using var stream = ExcelFileBuilder.Build(
        [
            ["Name", "Age",    "Salary",   "IsActive"],
            ["Dave", "NOT_INT","5000.00",  "true"],    // Age bad
            ["Eve",  "22",     "5500.00",  "false"]
        ]);

        var reader = BuildReader();
        var result = await reader.ReadAsync<PersonDto>(stream);

        // Eve's row should succeed
        Assert.Equal(1, result.Records.Count);
        Assert.Equal("Eve", result.Records[0].Name);

        // Dave's row should produce a conversion error on Age
        Assert.True(result.HasErrors);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PersonDto.Age) && e.RowNumber == 2);
    }

    [Fact]
    public async Task ReadAsync_NullableCellsResolveCorrectly()
    {
        using var stream = ExcelFileBuilder.Build(
        [
            ["NullableInt", "NullableDecimal", "NullableDate"],
            ["",            "",                ""],               // all empty → all null
            ["42",          "3.14",            "2024-01-15"]
        ]);

        var reader = BuildReader();
        var result = await reader.ReadAsync<NullableDto>(stream);

        Assert.False(result.HasErrors, string.Join(", ", result.Errors.Select(e => e.Message)));
        Assert.Equal(2, result.Records.Count);

        Assert.Null(result.Records[0].NullableInt);
        Assert.Null(result.Records[0].NullableDecimal);
        Assert.Null(result.Records[0].NullableDate);

        Assert.Equal(42,     result.Records[1].NullableInt);
        Assert.Equal(3.14m,  result.Records[1].NullableDecimal);
        Assert.Equal(new DateTime(2024, 1, 15), result.Records[1].NullableDate);
    }

    [Fact]
    public async Task ReadAsync_FailFast_StopsAfterFirstError()
    {
        using var stream = ExcelFileBuilder.Build(
        [
            ["Name", "Age"],
            ["A",    "BAD"],
            ["B",    "BAD"],
            ["C",    "BAD"]
        ]);

        var reader = BuildReader();
        var opts   = new ExcelReaderOptions { FailFast = true };
        var result = await reader.ReadAsync<PersonDto>(stream, opts);

        Assert.Single(result.Errors);
    }

    [Fact]
    public async Task ReadAsync_ProgressIsReported()
    {
        var rows = Enumerable.Range(0, 110).Select(_ => new[] { "X", "10", "1.0", "true" });
        var allRows = new[] { new[] { "Name", "Age", "Salary", "IsActive" } }.Concat(rows);
        using var stream = ExcelFileBuilder.Build(allRows);

        int progressCallCount = 0;
        var opts = new ExcelReaderOptions
        {
            BatchSize = 50,
            Progress  = new Progress<ExcelReadProgress>(_ => progressCallCount++)
        };

        var reader = BuildReader();
        await reader.ReadAsync<PersonDto>(stream, opts);

        // 110 rows / 50 batch = 2 batch reports + 1 final = 3
        Assert.True(progressCallCount >= 2);
    }
}
