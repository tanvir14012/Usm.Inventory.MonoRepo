using Usm.Shared.Utils.Excel.Writer.Internal;
using Xunit;

namespace Usm.Shared.Utils.Excel.Writer.Tests;

public sealed class DtoPopulatorTests
{
    private sealed class AllTypesDto
    {
        public string    StringProp  { get; set; } = string.Empty;
        public int       IntProp     { get; set; }
        public long      LongProp    { get; set; }
        public decimal   DecimalProp { get; set; }
        public float     FloatProp   { get; set; }
        public double    DoubleProp  { get; set; }
        public bool      BoolProp    { get; set; }
        public DateTime  DateProp    { get; set; }
        public Guid      GuidProp    { get; set; }
        public TimeSpan  TimeProp    { get; set; }
    }

    private sealed class NullableDto
    {
        public int?      NullableInt  { get; set; }
        public decimal?  NullableDec  { get; set; }
        public bool?     NullableBool { get; set; }
        public DateTime? NullableDate { get; set; }
    }

    public enum Status { Active, Inactive }

    private sealed class EnumDto
    {
        public Status StatusProp { get; set; }
    }

    private static DtoPopulator<T> Populator<T>() where T : new() => new();

    // ── String ────────────────────────────────────────────────────────────────
    [Fact]
    public void Populate_String_MapsCorrectly()
    {
        var (dto, errors) = Populator<AllTypesDto>().Populate(["Hello"]);
        Assert.Empty(errors);
        Assert.Equal("Hello", dto.StringProp);
    }

    // ── Int / numeric ─────────────────────────────────────────────────────────
    [Fact]
    public void Populate_Int_MapsCorrectly()
    {
        var (dto, errors) = Populator<AllTypesDto>().Populate(["", "42"]);
        Assert.Empty(errors);
        Assert.Equal(42, dto.IntProp);
    }

    [Fact]
    public void Populate_Decimal_MapsCorrectly()
    {
        var (dto, errors) = Populator<AllTypesDto>().Populate(["", "", "", "3.14"]);
        Assert.Empty(errors);
        Assert.Equal(3.14m, dto.DecimalProp);
    }

    // ── Bool ──────────────────────────────────────────────────────────────────
    [Theory]
    [InlineData("true",  true)]
    [InlineData("false", false)]
    [InlineData("1",     true)]
    [InlineData("0",     false)]
    [InlineData("Yes",   true)]
    [InlineData("No",    false)]
    public void Populate_Bool_VariousForms(string raw, bool expected)
    {
        var (dto, errors) = Populator<AllTypesDto>().Populate(["", "", "", "", "", "", raw]);
        Assert.Empty(errors);
        Assert.Equal(expected, dto.BoolProp);
    }

    // ── DateTime ──────────────────────────────────────────────────────────────
    [Fact]
    public void Populate_DateTime_IsoFormat()
    {
        var (dto, errors) = Populator<AllTypesDto>().Populate(
            ["", "", "", "", "", "", "", "2024-06-15"]);
        Assert.Empty(errors);
        Assert.Equal(new DateTime(2024, 6, 15), dto.DateProp);
    }

    [Fact]
    public void Populate_DateTime_OADate()
    {
        var oaDate = new DateTime(2024, 1, 1).ToOADate().ToString("F6");
        var (dto, errors) = Populator<AllTypesDto>().Populate(
            ["", "", "", "", "", "", "", oaDate]);
        Assert.Empty(errors);
        Assert.Equal(new DateTime(2024, 1, 1), dto.DateProp.Date);
    }

    // ── Guid ──────────────────────────────────────────────────────────────────
    [Fact]
    public void Populate_Guid_MapsCorrectly()
    {
        var g = Guid.NewGuid();
        var (dto, errors) = Populator<AllTypesDto>().Populate(
            ["", "", "", "", "", "", "", "", g.ToString()]);
        Assert.Empty(errors);
        Assert.Equal(g, dto.GuidProp);
    }

    // ── Nullable ──────────────────────────────────────────────────────────────
    [Fact]
    public void Populate_NullableProperties_EmptyCellProducesNull()
    {
        var (dto, errors) = Populator<NullableDto>().Populate(["", "", "", ""]);
        Assert.Empty(errors);
        Assert.Null(dto.NullableInt);
        Assert.Null(dto.NullableDec);
        Assert.Null(dto.NullableBool);
        Assert.Null(dto.NullableDate);
    }

    [Fact]
    public void Populate_NullableInt_WithValue()
    {
        var (dto, errors) = Populator<NullableDto>().Populate(["99"]);
        Assert.Empty(errors);
        Assert.Equal(99, dto.NullableInt);
    }

    // ── Enum ──────────────────────────────────────────────────────────────────
    [Theory]
    [InlineData("Active",   Status.Active)]
    [InlineData("inactive", Status.Inactive)]
    [InlineData("ACTIVE",   Status.Active)]
    public void Populate_Enum_CaseInsensitive(string raw, Status expected)
    {
        var (dto, errors) = Populator<EnumDto>().Populate([raw]);
        Assert.Empty(errors);
        Assert.Equal(expected, dto.StatusProp);
    }

    // ── Error collection ──────────────────────────────────────────────────────
    [Fact]
    public void Populate_InvalidInt_CollectsErrorAndContinues()
    {
        var (dto, errors) = Populator<AllTypesDto>().Populate(["name", "NOT_A_NUMBER"]);
        Assert.Single(errors);
        Assert.Equal(nameof(AllTypesDto.IntProp), errors[0].PropertyName);
        Assert.Equal("NOT_A_NUMBER", errors[0].OriginalValue);
        // String property should still be mapped
        Assert.Equal("name", dto.StringProp);
    }

    [Fact]
    public void Populate_FewerCellsThanProperties_DoesNotThrow()
    {
        var (dto, errors) = Populator<AllTypesDto>().Populate(["only one cell"]);
        Assert.Empty(errors);
        Assert.Equal("only one cell", dto.StringProp);
        Assert.Equal(0, dto.IntProp); // default
    }

    // ── Column mapping stays stable with empty intermediate cells ─────────────
    [Fact]
    public void Populate_EmptyMiddleCell_SubsequentPropertiesUnaffected()
    {
        // cellValues[1] is empty (age), but cellValues[2] should map to LongProp
        var (dto, errors) = Populator<AllTypesDto>().Populate(["text", "", "99"]);
        Assert.Empty(errors);
        Assert.Equal("text", dto.StringProp);
        Assert.Equal(0,      dto.IntProp);    // empty → default
        Assert.Equal(99L,    dto.LongProp);
    }
}
