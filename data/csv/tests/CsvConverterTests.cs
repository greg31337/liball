using System;
using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvConverterTests
{
    [Test]
    public void ToStringValue_HandlesNullAndObjects()
    {
        CsvConversionOptions options = CsvConversionOptions.CreateDefault();
        CsvColumn column = CsvColumn.Create("Value", 0);

        Assert.That(CsvConverter.ToStringValue(123, column, options), Is.EqualTo("123"));
        Assert.That(CsvConverter.ToStringValue(null, column, options), Is.EqualTo(""));

        options.UseDefaultForNull = true;
        options.DefaultValue = "fallback";

        Assert.That(CsvConverter.ToStringValue(null, column, options), Is.EqualTo("fallback"));
    }

    [Test]
    public void ToBoolean_ParsesCommonBooleanValuesAndDefaults()
    {
        CsvConversionOptions options = CsvConversionOptions.CreateDefault();

        Assert.That(CsvConverter.ToBoolean("true", options), Is.True);
        Assert.That(CsvConverter.ToBoolean("Y", options), Is.True);
        Assert.That(CsvConverter.ToBoolean("1", options), Is.True);
        Assert.That(CsvConverter.ToBoolean("false", options), Is.False);
        Assert.That(CsvConverter.ToBoolean("N", options), Is.False);

        options.UseDefaultForInvalid = true;
        options.DefaultValue = "yes";

        Assert.That(CsvConverter.ToBoolean("bad", options), Is.True);
    }

    [Test]
    public void ToInt32AndToInt64_ParseIntegersWithNullDefaults()
    {
        CsvConversionOptions options = CsvConversionOptions.CreateDefault();
        options.HasNullString = true;
        options.NullString = "NULL";
        options.UseDefaultForNull = true;
        options.DefaultValue = "7";

        Assert.That(CsvConverter.ToInt32("42", options), Is.EqualTo(42));
        Assert.That(CsvConverter.ToInt64("-9000000000", options), Is.EqualTo(-9000000000L));
        Assert.That(CsvConverter.ToInt32("NULL", options), Is.EqualTo(7));

        options.UseDefaultForInvalid = true;
        options.DefaultValue = "11";

        Assert.That(CsvConverter.ToInt32("not integer", options), Is.EqualTo(11));
    }

    [Test]
    public void ToInt64AndToInt32_UseInvalidDefaultForOverflow()
    {
        CsvConversionOptions options = CsvConversionOptions.CreateDefault();
        options.UseDefaultForInvalid = true;
        options.DefaultValue = "9";

        Assert.That(CsvConverter.ToInt64("9223372036854775808", options), Is.EqualTo(9L));
        Assert.That(CsvConverter.ToInt64("-9223372036854775809", options), Is.EqualTo(9L));
        Assert.That(CsvConverter.ToInt32("2147483648", options), Is.EqualTo(9));
    }

    [Test]
    public void ResolveInput_TreatsEmptyStringAsNullWhenConfigured()
    {
        CsvConversionOptions options = CsvConversionOptions.CreateDefault();
        options.EmptyStringIsNull = true;
        options.UseDefaultForNull = true;
        options.DefaultValue = "fallback";
        object? nullValue = null;

        Assert.That(CsvConverter.ResolveInput("", options), Is.EqualTo("fallback"));
        Assert.That(CsvConverter.ToStringValue(nullValue, CsvColumn.Create("Value", 0), options), Is.EqualTo("fallback"));
    }

    [Test]
    public void ToDecimalAndToDouble_ParseConfiguredSeparators()
    {
        CsvConversionOptions options = CsvConversionOptions.CreateDefault();
        options.DecimalSeparator = ",";
        options.ThousandsSeparator = ".";

        Assert.That(CsvConverter.ToDecimal("1.234,56", options), Is.EqualTo(1234.56m));
        Assert.That(CsvConverter.ToDouble("-1.234,5", options), Is.EqualTo(-1234.5d));
        Assert.That(CsvConverter.ToDouble("1,25e3", options), Is.EqualTo(1250d));
    }

    [Test]
    public void ToDateTime_ParsesIsoAndConfiguredFormats()
    {
        CsvConversionOptions options = CsvConversionOptions.CreateDefault();

        Assert.That(CsvConverter.ToDateTime("2024-02-29", options), Is.EqualTo(new DateTime(2024, 2, 29)));
        Assert.That(CsvConverter.ToDateTime("2024-02-29T13:14:15", options), Is.EqualTo(new DateTime(2024, 2, 29, 13, 14, 15)));

        options.DateTimeFormats = new string[] { "dd/MM/yyyy" };

        Assert.That(CsvConverter.ToDateTime("31/12/2024", options), Is.EqualTo(new DateTime(2024, 12, 31)));

        options.UseDefaultForInvalid = true;
        options.DefaultValue = "2024-01-02";

        Assert.That(CsvConverter.ToDateTime("not date", options), Is.EqualTo(new DateTime(2024, 1, 2)));
    }

    [Test]
    public void ToDateOnly_ParsesSupportedDateText()
    {
        CsvConversionOptions options = CsvConversionOptions.CreateDefault();

        Assert.That(CsvConverter.ToDateOnly("2024-02-29", options), Is.EqualTo(new DateOnly(2024, 2, 29)));

        options.DateTimeFormats = new string[] { "dd/MM/yyyy" };

        Assert.That(CsvConverter.ToDateOnly("31/12/2024", options), Is.EqualTo(new DateOnly(2024, 12, 31)));

        options.UseDefaultForInvalid = true;
        options.DefaultValue = "2024-01-02";

        Assert.That(CsvConverter.ToDateOnly("not date", options), Is.EqualTo(new DateOnly(2024, 1, 2)));
    }

    [Test]
    public void ToTimeOnly_ParsesSupportedTimeText()
    {
        CsvConversionOptions options = CsvConversionOptions.CreateDefault();

        Assert.That(CsvConverter.ToTimeOnly("13:14", options), Is.EqualTo(new TimeOnly(13, 14)));
        Assert.That(CsvConverter.ToTimeOnly("13:14:15", options), Is.EqualTo(new TimeOnly(13, 14, 15)));

        options.DateTimeFormats = new string[] { "HH:mm:ss" };

        Assert.That(CsvConverter.ToTimeOnly("01:02:03", options), Is.EqualTo(new TimeOnly(1, 2, 3)));

        options.UseDefaultForInvalid = true;
        options.DefaultValue = "04:05";

        Assert.That(CsvConverter.ToTimeOnly("not time", options), Is.EqualTo(new TimeOnly(4, 5)));
    }

    [Test]
    public void ToGuid_ParsesValidatedGuidText()
    {
        CsvConversionOptions options = CsvConversionOptions.CreateDefault();
        Guid expected = new Guid("00112233-4455-6677-8899-aabbccddeeff");

        Assert.That(CsvConverter.ToGuid("00112233-4455-6677-8899-aabbccddeeff", options), Is.EqualTo(expected));
        Assert.That(CsvConverter.ToGuid("not-guid", options), Is.EqualTo(Guid.Empty));
    }

    [Test]
    public void ToByteArray_ParsesHexText()
    {
        CsvConversionOptions options = CsvConversionOptions.CreateDefault();

        Assert.That(CsvConverter.ToByteArray("0A ff-10", options), Is.EqualTo(new byte[] { 10, 255, 16 }));
        Assert.That(CsvConverter.ToByteArray("bad hex z", options), Is.EqualTo(new byte[0]));

        options.UseDefaultForInvalid = true;
        options.DefaultValue = "01:02";

        Assert.That(CsvConverter.ToByteArray("z", options), Is.EqualTo(new byte[] { 1, 2 }));
    }

    [Test]
    public void ToByteArray_UsesInvalidDefaultForOddLengthHex()
    {
        CsvConversionOptions options = CsvConversionOptions.CreateDefault();
        options.UseDefaultForInvalid = true;
        options.DefaultValue = "0A";

        Assert.That(CsvConverter.ToByteArray("ABC", options), Is.EqualTo(new byte[] { 10 }));
    }
}
