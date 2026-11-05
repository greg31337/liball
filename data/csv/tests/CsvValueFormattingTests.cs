using System;
using System.Globalization;
using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvValueFormattingTests
{
    [Test]
    public void ToStringValue_FormatsEveryTypeKindUsingColumnHints()
    {
        ICsvConverter converter = new CsvConverterService();
        CsvConversionOptions options = new CsvConversionOptions();
        Guid guid = new Guid("00112233-4455-6677-8899-aabbccddeeff");

        Assert.That(converter.ToStringValue(123, CreateColumn(CsvTypeKind.Unknown, ""), options), Is.EqualTo("123"));
        Assert.That(converter.ToStringValue("ignored", CreateColumn(CsvTypeKind.Null, ""), options), Is.EqualTo(""));
        Assert.That(converter.ToStringValue(true, CreateColumn(CsvTypeKind.Boolean, "G"), options), Is.EqualTo("True"));
        Assert.That(converter.ToStringValue(42, CreateColumn(CsvTypeKind.Int32, "D4"), options), Is.EqualTo("0042"));
        Assert.That(converter.ToStringValue(9000000000L, CreateColumn(CsvTypeKind.Int64, "D12"), options), Is.EqualTo("009000000000"));
        Assert.That(converter.ToStringValue(12.5m, CreateColumn(CsvTypeKind.Decimal, "0.00"), options), Is.EqualTo("12.50"));
        Assert.That(converter.ToStringValue(12.345d, CreateColumn(CsvTypeKind.Double, "0.00"), options), Is.EqualTo("12.35"));
        Assert.That(converter.ToStringValue(new DateTime(2024, 2, 29, 13, 14, 15), CreateColumn(CsvTypeKind.DateTime, "yyyyMMddHHmmss"), options), Is.EqualTo("20240229131415"));
        Assert.That(converter.ToStringValue(new DateOnly(2024, 12, 31), CreateColumn(CsvTypeKind.DateOnly, "yyyyMMdd"), options), Is.EqualTo("20241231"));
        Assert.That(converter.ToStringValue(new TimeOnly(13, 14, 15), CreateColumn(CsvTypeKind.TimeOnly, "HHmmss"), options), Is.EqualTo("131415"));
        Assert.That(converter.ToStringValue(guid, CreateColumn(CsvTypeKind.Guid, "N"), options), Is.EqualTo("00112233445566778899aabbccddeeff"));
        Assert.That(converter.ToStringValue(new byte[] { 0, 10, 255, 241 }, CreateColumn(CsvTypeKind.Binary, "X2"), options), Is.EqualTo("000AFFF1"));
        Assert.That(converter.ToStringValue("plain text", CreateColumn(CsvTypeKind.String, "ignored"), options), Is.EqualTo("plain text"));
    }

    [Test]
    public void ToStringValue_UsesConfiguredCultureSeparatorsAndDateTimeFormats()
    {
        ICsvConverter converter = new CsvConverterService();
        CsvConversionOptions numericOptions = new CsvConversionOptions();
        numericOptions.Culture = new CultureInfo("fr-FR");
        numericOptions.DecimalSeparator = ",";
        numericOptions.ThousandsSeparator = ".";

        string number = converter.ToStringValue(1234.5m, CreateColumn(CsvTypeKind.Decimal, "#,##0.00"), numericOptions);

        CsvConversionOptions dateOptions = new CsvConversionOptions();
        dateOptions.Culture = new CultureInfo("fr-FR");
        dateOptions.DateTimeFormats = new string[] { "dd MMMM yyyy" };
        string date = converter.ToStringValue(new DateOnly(2024, 2, 29), CreateColumn(CsvTypeKind.DateOnly, ""), dateOptions);

        CsvConversionOptions timeOptions = new CsvConversionOptions();
        timeOptions.DateTimeFormats = new string[] { "HH'h'mm" };
        string time = converter.ToStringValue(new TimeOnly(13, 14), CreateColumn(CsvTypeKind.TimeOnly, ""), timeOptions);

        Assert.That(number, Is.EqualTo("1.234,50"));
        Assert.That(date, Is.EqualTo("29 février 2024"));
        Assert.That(time, Is.EqualTo("13h14"));
    }

    [Test]
    public void ToStringValue_AppliesNullTokenEmptyAndInvalidDefaults()
    {
        ICsvConverter converter = new CsvConverterService();
        CsvConversionOptions options = new CsvConversionOptions();
        options.HasNullString = true;
        options.NullString = "NULL";
        options.EmptyStringIsNull = true;
        options.UseDefaultForNull = true;
        options.UseDefaultForInvalid = true;
        options.DefaultValue = "fallback";
        CsvColumn stringColumn = CreateColumn(CsvTypeKind.String, "");

        Assert.That(converter.ToStringValue(null, stringColumn, options), Is.EqualTo("fallback"));
        Assert.That(converter.ToStringValue("NULL", stringColumn, options), Is.EqualTo("fallback"));
        Assert.That(converter.ToStringValue("", stringColumn, options), Is.EqualTo("fallback"));
        Assert.That(converter.ToStringValue(new DateTime(2024, 1, 1), CreateColumn(CsvTypeKind.DateOnly, "yyyy-MM-dd"), options), Is.EqualTo("fallback"));

        options.UseDefaultForInvalid = false;

        Assert.That(converter.ToStringValue(new DateTime(2024, 1, 1), CreateColumn(CsvTypeKind.DateOnly, "yyyy-MM-dd"), options), Is.EqualTo(""));
    }

    [Test]
    public void ToStringValue_UsesStableDefaultsWhenFormatHintsAreAbsent()
    {
        ICsvConverter converter = new CsvConverterService();
        CsvConversionOptions options = new CsvConversionOptions();

        Assert.That(converter.ToStringValue(new DateTime(2024, 2, 29, 1, 2, 3), CreateColumn(CsvTypeKind.DateTime, ""), options), Is.EqualTo("2024-02-29T01:02:03"));
        Assert.That(converter.ToStringValue(new DateOnly(2024, 2, 29), CreateColumn(CsvTypeKind.DateOnly, ""), options), Is.EqualTo("2024-02-29"));
        Assert.That(converter.ToStringValue(new TimeOnly(1, 2, 3), CreateColumn(CsvTypeKind.TimeOnly, ""), options), Is.EqualTo("01:02:03"));
        Assert.That(converter.ToStringValue(new Guid("00112233-4455-6677-8899-aabbccddeeff"), CreateColumn(CsvTypeKind.Guid, ""), options), Is.EqualTo("00112233-4455-6677-8899-aabbccddeeff"));
        Assert.That(converter.ToStringValue(new byte[] { 1, 2, 171 }, CreateColumn(CsvTypeKind.Binary, ""), options), Is.EqualTo("0102AB"));
        Assert.That(converter.ToStringValue(new byte[] { 1, 2, 171 }, CreateColumn(CsvTypeKind.Binary, "x2"), options), Is.EqualTo("0102ab"));
    }

    static CsvColumn CreateColumn(CsvTypeKind typeKind, string formatHint)
    {
        CsvColumn column = new CsvColumn();
        column.TypeKind = typeKind;
        column.FormatHint = formatHint;
        return column;
    }
}
