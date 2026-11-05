using System;
using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvConversionResultTests
{
    [Test]
    public void ResultApis_ConvertEverySupportedTargetType()
    {
        ICsvConverter converter = new CsvConverterService();
        CsvConversionOptions options = new CsvConversionOptions();
        CsvRow row = CreateRow(new string?[]
        {
            "plain text",
            "yes",
            "42",
            "9000000000",
            "12.50",
            "1.25e2",
            "2024-02-29T13:14:15",
            "2024-02-29",
            "13:14:15",
            "00112233-4455-6677-8899-aabbccddeeff",
            "0A-ff:10"
        });

        CsvConversionResult text = converter.ConvertString(row, 0, options);
        CsvConversionResult boolean = converter.ConvertBoolean(row, 1, options);
        CsvConversionResult int32 = converter.ConvertInt32(row, 2, options);
        CsvConversionResult int64 = converter.ConvertInt64(row, 3, options);
        CsvConversionResult decimalResult = converter.ConvertDecimal(row, 4, options);
        CsvConversionResult doubleResult = converter.ConvertDouble(row, 5, options);
        CsvConversionResult dateTime = converter.ConvertDateTime(row, 6, options);
        CsvConversionResult dateOnly = converter.ConvertDateOnly(row, 7, options);
        CsvConversionResult timeOnly = converter.ConvertTimeOnly(row, 8, options);
        CsvConversionResult guid = converter.ConvertGuid(row, 9, options);
        CsvConversionResult bytes = converter.ConvertByteArray(row, 10, options);

        Assert.That(text.TextValue, Is.EqualTo("plain text"));
        Assert.That(boolean.BooleanValue, Is.True);
        Assert.That(int32.Int32Value, Is.EqualTo(42));
        Assert.That(int64.Int64Value, Is.EqualTo(9000000000L));
        Assert.That(decimalResult.DecimalValue, Is.EqualTo(12.50m));
        Assert.That(doubleResult.DoubleValue, Is.EqualTo(125d));
        Assert.That(dateTime.DateTimeValue, Is.EqualTo(new DateTime(2024, 2, 29, 13, 14, 15)));
        Assert.That(dateOnly.DateOnlyValue, Is.EqualTo(new DateOnly(2024, 2, 29)));
        Assert.That(timeOnly.TimeOnlyValue, Is.EqualTo(new TimeOnly(13, 14, 15)));
        Assert.That(guid.GuidValue, Is.EqualTo(new Guid("00112233-4455-6677-8899-aabbccddeeff")));
        Assert.That(bytes.ByteArrayValue, Is.EqualTo(new byte[] { 10, 255, 16 }));

        CsvConversionResult[] results = new CsvConversionResult[]
        {
            text, boolean, int32, int64, decimalResult, doubleResult, dateTime, dateOnly, timeOnly, guid, bytes
        };
        foreach (CsvConversionResult result in results)
        {
            Assert.That(result.Status, Is.EqualTo(CsvConversionStatus.Success));
            Assert.That(result.SourceStatus, Is.EqualTo(CsvConversionStatus.Success));
            Assert.That(result.HasValue, Is.True);
            Assert.That(result.Diagnostics.Items, Is.Empty);
        }
    }

    [Test]
    public void ResultApis_DistinguishNullMissingEmptyInvalidAndOutOfRange()
    {
        ICsvConverter converter = new CsvConverterService();
        CsvConversionOptions options = new CsvConversionOptions();
        options.HasNullString = true;
        options.NullString = "NULL";
        CsvRow row = CreateRow(new string?[] { null, "", "NULL", "not-a-number", "2147483648" });

        CsvConversionResult nullResult = converter.ConvertInt32(row, 0, options);
        CsvConversionResult emptyResult = converter.ConvertInt32(row, 1, options);
        CsvConversionResult tokenResult = converter.ConvertInt32(row, 2, options);
        CsvConversionResult invalidResult = converter.ConvertInt32(row, 3, options);
        CsvConversionResult rangeResult = converter.ConvertInt32(row, 4, options);
        CsvConversionResult missingResult = converter.ConvertInt32(row, 5, options);

        Assert.That(nullResult.Status, Is.EqualTo(CsvConversionStatus.Null));
        Assert.That(emptyResult.Status, Is.EqualTo(CsvConversionStatus.Empty));
        Assert.That(tokenResult.Status, Is.EqualTo(CsvConversionStatus.Null));
        Assert.That(invalidResult.Status, Is.EqualTo(CsvConversionStatus.Invalid));
        Assert.That(rangeResult.Status, Is.EqualTo(CsvConversionStatus.OutOfRange));
        Assert.That(missingResult.Status, Is.EqualTo(CsvConversionStatus.Missing));
        Assert.That(nullResult.HasValue, Is.False);
        Assert.That(invalidResult.HasValue, Is.False);
        Assert.That(rangeResult.Diagnostics.Items[0].Code, Is.EqualTo("ConversionOutOfRange"));
    }

    [Test]
    public void ResultApis_ReportConverterDiagnosticsWithRowAndFieldContext()
    {
        ICsvConverter converter = new CsvConverterService();
        CsvConversionOptions options = new CsvConversionOptions();
        CsvRow row = CreateRow(new string?[] { "bad-guid" });
        row.RowNumber = 17;
        row.Line = 21;
        row.CharacterOffset = 345;
        row.RawText = "bad-guid,tail";

        CsvConversionResult result = converter.ConvertGuid(row, 0, options);
        CsvDiagnostic diagnostic = result.Diagnostics.Items[0];

        Assert.That(diagnostic.Category, Is.EqualTo(CsvDiagnostic.ConverterCategory));
        Assert.That(diagnostic.Code, Is.EqualTo("ConversionInvalid"));
        Assert.That(diagnostic.Row, Is.EqualTo(17));
        Assert.That(diagnostic.Column, Is.EqualTo(1));
        Assert.That(diagnostic.Line, Is.EqualTo(21));
        Assert.That(diagnostic.CharacterOffset, Is.EqualTo(345));
        Assert.That(diagnostic.RawContext, Is.EqualTo("bad-guid,tail"));
        Assert.That(diagnostic.IsError, Is.True);
    }

    [Test]
    public void ResultApis_ReportSuccessfulDefaultsAndOriginalInputStatus()
    {
        ICsvConverter converter = new CsvConverterService();
        CsvConversionOptions nullOptions = new CsvConversionOptions();
        nullOptions.EmptyStringIsNull = true;
        nullOptions.UseDefaultForNull = true;
        nullOptions.DefaultValue = "7";
        CsvRow nullRow = CreateRow(new string?[] { null, "" });

        CsvConversionResult nullResult = converter.ConvertInt32(nullRow, 0, nullOptions);
        CsvConversionResult emptyResult = converter.ConvertInt32(nullRow, 1, nullOptions);
        CsvConversionResult missingResult = converter.ConvertInt32(nullRow, 2, nullOptions);

        CsvConversionOptions invalidOptions = new CsvConversionOptions();
        invalidOptions.UseDefaultForInvalid = true;
        invalidOptions.DefaultValue = "true";
        CsvConversionResult invalidResult = converter.ConvertBoolean(CreateRow(new string?[] { "bad" }), 0, invalidOptions);

        AssertDefaulted(nullResult, CsvConversionStatus.Null, 7);
        AssertDefaulted(emptyResult, CsvConversionStatus.Empty, 7);
        AssertDefaulted(missingResult, CsvConversionStatus.Missing, 7);
        Assert.That(invalidResult.Status, Is.EqualTo(CsvConversionStatus.Defaulted));
        Assert.That(invalidResult.SourceStatus, Is.EqualTo(CsvConversionStatus.Invalid));
        Assert.That(invalidResult.BooleanValue, Is.True);
        Assert.That(invalidResult.Diagnostics.Items[0].Code, Is.EqualTo("ConversionDefaulted"));
        Assert.That(invalidResult.Diagnostics.Items[0].IsError, Is.False);
    }

    [Test]
    public void ResultApis_ReturnOutOfRangeWithoutThrowingForOversizedValues()
    {
        ICsvConverter converter = new CsvConverterService();
        CsvConversionOptions options = new CsvConversionOptions();
        CsvRow row = CreateRow(new string?[]
        {
            "999999999999999999999999999999999999999999999999999999",
            "1e999999999999999999999999",
            "2024-13-01",
            "24:00"
        });

        CsvConversionResult decimalResult = new CsvConversionResult();
        CsvConversionResult doubleResult = new CsvConversionResult();
        CsvConversionResult dateResult = new CsvConversionResult();
        CsvConversionResult timeResult = new CsvConversionResult();

        Assert.DoesNotThrow(() => decimalResult = converter.ConvertDecimal(row, 0, options));
        Assert.DoesNotThrow(() => doubleResult = converter.ConvertDouble(row, 1, options));
        Assert.DoesNotThrow(() => dateResult = converter.ConvertDateOnly(row, 2, options));
        Assert.DoesNotThrow(() => timeResult = converter.ConvertTimeOnly(row, 3, options));

        Assert.That(decimalResult.Status, Is.EqualTo(CsvConversionStatus.OutOfRange));
        Assert.That(doubleResult.Status, Is.EqualTo(CsvConversionStatus.OutOfRange));
        Assert.That(dateResult.Status, Is.EqualTo(CsvConversionStatus.OutOfRange));
        Assert.That(timeResult.Status, Is.EqualTo(CsvConversionStatus.OutOfRange));
    }

    static CsvRow CreateRow(string?[] fields)
    {
        CsvRow row = new CsvRow();
        row.Fields = fields;
        return row;
    }

    static void AssertDefaulted(CsvConversionResult result, CsvConversionStatus sourceStatus, int value)
    {
        Assert.That(result.Status, Is.EqualTo(CsvConversionStatus.Defaulted));
        Assert.That(result.SourceStatus, Is.EqualTo(sourceStatus));
        Assert.That(result.HasValue, Is.True);
        Assert.That(result.Int32Value, Is.EqualTo(value));
        Assert.That(result.Diagnostics.Items[0].Code, Is.EqualTo("ConversionDefaulted"));
    }
}
