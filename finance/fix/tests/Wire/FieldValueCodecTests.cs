using System;
using System.Text;
using fix;

namespace fix.Tests;

[TestFixture]
public class FieldValueCodecTests
{
    [Test]
    public void DecodeValidatesAndCopiesScalarTemporalAndBinaryValues()
    {
        FixFieldValueCodec codec = new FixFieldValueCodec();
        DecodeOptions options = new DecodeOptions();
        byte[] integerSource = Encoding.ASCII.GetBytes("00023");
        byte[] timestampSource = Encoding.ASCII.GetBytes("20260830-12:34:56.123456789012");
        byte[] dataSource = [65, 1, 66, 0, 255];

        FieldParseResult integer = codec.DecodeFieldValue(integerSource, 38, FixFieldType.Int, options);
        FieldParseResult timestamp = codec.DecodeFieldValue(timestampSource, 60, FixFieldType.UtcTimestamp, options);
        FieldParseResult data = codec.DecodeFieldValue(dataSource, 96, FixFieldType.Data, options);
        integerSource[0] = 88;
        timestampSource[0] = 88;
        dataSource[0] = 88;

        Assert.Multiple(() =>
        {
            Assert.That(integer.Status, Is.EqualTo(DecodeStatus.Success));
            Assert.That(integer.Field, Is.Not.Null);
            Assert.That(integer.Field!.Tag, Is.EqualTo(38));
            Assert.That(integer.Field.Value, Is.EqualTo(Encoding.ASCII.GetBytes("00023")));
            Assert.That(integer.Consumed, Is.EqualTo(5));
            Assert.That(integer.Required, Is.EqualTo(5));
            Assert.That(timestamp.Status, Is.EqualTo(DecodeStatus.Success));
            Assert.That(timestamp.Field!.Value, Is.EqualTo(Encoding.ASCII.GetBytes("20260830-12:34:56.123456789012")));
            Assert.That(data.Status, Is.EqualTo(DecodeStatus.Success));
            Assert.That(data.Field!.Value, Is.EqualTo(new byte[] { 65, 1, 66, 0, 255 }));
            Assert.That(data.Field.IsEncodedData, Is.True);
        });
    }

    [Test]
    public void DecodeMapsExpectedFailuresToExplicitStatuses()
    {
        FixFieldValueCodec codec = new FixFieldValueCodec();
        DecodeOptions options = new DecodeOptions();
        DecodeOptions limitedOptions = new DecodeOptions { MaxFieldSize = 2 };
        DecodeOptions invalidLimitOptions = new DecodeOptions { MaxFieldSize = -1 };
        DecodeOptions encodingOptions = new DecodeOptions { TextEncodingName = "utf-8" };

        FieldParseResult incomplete = codec.DecodeFieldValue(Array.Empty<byte>(), 38, FixFieldType.Int, options);
        FieldParseResult invalid = codec.DecodeFieldValue("X"u8, 54, FixFieldType.Boolean, options);
        FieldParseResult calendar = codec.DecodeFieldValue("20230229"u8, 75, FixFieldType.UtcDateOnly, options);
        FieldParseResult limited = codec.DecodeFieldValue("ABC"u8, 58, FixFieldType.String, limitedOptions);
        FieldParseResult invalidLimit = codec.DecodeFieldValue("1"u8, 38, FixFieldType.Int, invalidLimitOptions);
        FieldParseResult invalidTag = codec.DecodeFieldValue("1"u8, 0, FixFieldType.Int, options);
        FieldParseResult unsupportedType = codec.DecodeFieldValue("1"u8, 38, FixFieldType.Unknown, options);
        FieldParseResult unsupportedEncoding = codec.DecodeFieldValue("ABC"u8, 58, FixFieldType.String, encodingOptions);

        Assert.Multiple(() =>
        {
            Assert.That(incomplete.Status, Is.EqualTo(DecodeStatus.Incomplete));
            Assert.That(incomplete.Required, Is.EqualTo(1));
            Assert.That(incomplete.ErrorCode, Is.EqualTo("incomplete"));
            Assert.That(invalid.Status, Is.EqualTo(DecodeStatus.Invalid));
            Assert.That(invalid.ErrorOffset, Is.Zero);
            Assert.That(invalid.ErrorCode, Is.EqualTo("invalid-boolean"));
            Assert.That(invalid.ErrorMessage, Is.Not.Empty);
            Assert.That(calendar.Status, Is.EqualTo(DecodeStatus.Invalid));
            Assert.That(calendar.ErrorCode, Is.EqualTo("invalid-calendar-date"));
            Assert.That(limited.Status, Is.EqualTo(DecodeStatus.LimitExceeded));
            Assert.That(limited.ErrorCode, Is.EqualTo("limit-exceeded"));
            Assert.That(invalidLimit.ErrorCode, Is.EqualTo("invalid-limit"));
            Assert.That(invalidTag.ErrorCode, Is.EqualTo("invalid-tag"));
            Assert.That(unsupportedType.ErrorCode, Is.EqualTo("unsupported-field-type"));
            Assert.That(unsupportedEncoding.ErrorCode, Is.EqualTo("unsupported-text-encoding"));
        });
    }

    [Test]
    public void DecodeCanExplicitlyPreserveEmptyValues()
    {
        FixFieldValueCodec codec = new FixFieldValueCodec();
        DecodeOptions options = new DecodeOptions { AllowEmptyValues = true };

        FieldParseResult text = codec.DecodeFieldValue(Array.Empty<byte>(), 58, FixFieldType.String, options);
        FieldParseResult data = codec.DecodeFieldValue(Array.Empty<byte>(), 96, FixFieldType.Data, options);
        FieldParseResult unsupported = codec.DecodeFieldValue(Array.Empty<byte>(), 1, FixFieldType.Unknown, options);

        Assert.Multiple(() =>
        {
            Assert.That(text.Status, Is.EqualTo(DecodeStatus.Success));
            Assert.That(text.Field!.Value, Is.Empty);
            Assert.That(data.Status, Is.EqualTo(DecodeStatus.Success));
            Assert.That(data.Field!.IsEncodedData, Is.True);
            Assert.That(unsupported.Status, Is.EqualTo(DecodeStatus.Invalid));
        });
    }

    [Test]
    public void ConfiguredAsciiEncodingRejectsNonAsciiTextButNotBinaryData()
    {
        FixFieldValueCodec codec = new FixFieldValueCodec();
        DecodeOptions options = new DecodeOptions { TextEncodingName = "US-ASCII" };
        EncodeOptions encodeOptions = new EncodeOptions { TextEncodingName = "ascii" };
        byte[] highByte = [233];

        FieldParseResult text = codec.DecodeFieldValue(highByte, 58, FixFieldType.String, options);
        FieldParseResult data = codec.DecodeFieldValue(highByte, 96, FixFieldType.Data, options);
        EncodeResult encodedText = codec.EncodeFieldValue(new FixField { Tag = 58, Value = highByte }, FixFieldType.String, new byte[1], encodeOptions);
        EncodeResult encodedData = codec.EncodeFieldValue(new FixField { Tag = 96, Value = highByte }, FixFieldType.Data, new byte[1], encodeOptions);

        Assert.Multiple(() =>
        {
            Assert.That(text.ErrorCode, Is.EqualTo("invalid-character"));
            Assert.That(text.ErrorOffset, Is.Zero);
            Assert.That(data.Status, Is.EqualTo(DecodeStatus.Success));
            Assert.That(encodedText.ErrorCode, Is.EqualTo("invalid-character"));
            Assert.That(encodedData.Status, Is.EqualTo(EncodeStatus.Success));
        });
    }

    [Test]
    public void EncodeCanonicalizesScalarAndTemporalLexicalForms()
    {
        FixFieldValueCodec codec = new FixFieldValueCodec();
        EncodeOptions options = new EncodeOptions();
        byte[] destination = new byte[64];

        Assert.Multiple(() =>
        {
            AssertEncodedText(codec, options, destination, FixFieldType.Int, "00023", "23");
            AssertEncodedText(codec, options, destination, FixFieldType.Price, "-00023.2300", "-23.23");
            AssertEncodedText(codec, options, destination, FixFieldType.Boolean, "Y", "Y");
            AssertEncodedText(codec, options, destination, FixFieldType.MultipleStringValue, "AV AN A", "AV AN A");
            AssertEncodedText(codec, options, destination, FixFieldType.MonthYear, "202608W5", "202608w5");
            AssertEncodedText(codec, options, destination, FixFieldType.DayOfMonth, "01", "1");
            AssertEncodedText(codec, options, destination, FixFieldType.UtcTimestamp, "20260830-12:34:56.123456789012", "20260830-12:34:56.123456789012");
            AssertEncodedText(codec, options, destination, FixFieldType.TzTimestamp, "20260830-12:34:56.123456789+05:30", "20260830-12:34:56.123456789+05:30");
        });
    }

    [Test]
    public void EncodePreservesArbitraryDataBytes()
    {
        FixFieldValueCodec codec = new FixFieldValueCodec();
        EncodeOptions options = new EncodeOptions();
        byte[] value = [65, 1, 66, 0, 255];
        byte[] destination = new byte[value.Length];
        FixField field = new FixField { Tag = 96, Value = value };

        EncodeResult result = codec.EncodeFieldValue(field, FixFieldType.Data, destination, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(EncodeStatus.Success));
            Assert.That(result.BytesWritten, Is.EqualTo(value.Length));
            Assert.That(result.RequiredSize, Is.EqualTo(value.Length));
            Assert.That(destination, Is.EqualTo(value));
        });
    }

    [Test]
    public void EncodeReturnsFailuresWithoutPartiallyWritingTheDestination()
    {
        FixFieldValueCodec codec = new FixFieldValueCodec();
        EncodeOptions options = new EncodeOptions();
        byte[] shortDestination = [88, 88, 88];
        byte[] invalidDestination = [88, 88, 88];

        EncodeResult shortResult = codec.EncodeFieldValue(new FixField { Tag = 44, Value = Encoding.ASCII.GetBytes("12.5") }, FixFieldType.Price, shortDestination, options);
        EncodeResult invalid = codec.EncodeFieldValue(new FixField { Tag = 54, Value = [88] }, FixFieldType.Boolean, invalidDestination, options);
        EncodeResult unsupported = codec.EncodeFieldValue(new FixField { Tag = 1, Value = [49] }, FixFieldType.Unknown, new byte[8], options);
        EncodeResult limited = codec.EncodeFieldValue(new FixField { Tag = 58, Value = [65, 66, 67] }, FixFieldType.String, new byte[8], new EncodeOptions { MaxFieldSize = 2 });
        EncodeResult invalidTag = codec.EncodeFieldValue(new FixField { Tag = 0, Value = [49] }, FixFieldType.Int, new byte[8], options);

        Assert.Multiple(() =>
        {
            Assert.That(shortResult.Status, Is.EqualTo(EncodeStatus.DestinationTooSmall));
            Assert.That(shortResult.RequiredSize, Is.EqualTo(4));
            Assert.That(shortDestination, Is.EqualTo(new byte[] { 88, 88, 88 }));
            Assert.That(invalid.Status, Is.EqualTo(EncodeStatus.Invalid));
            Assert.That(invalid.ErrorCode, Is.EqualTo("invalid-boolean"));
            Assert.That(invalid.ErrorMessage, Is.Not.Empty);
            Assert.That(invalidDestination, Is.EqualTo(new byte[] { 88, 88, 88 }));
            Assert.That(unsupported.ErrorCode, Is.EqualTo("unsupported-field-type"));
            Assert.That(limited.Status, Is.EqualTo(EncodeStatus.LimitExceeded));
            Assert.That(invalidTag.ErrorCode, Is.EqualTo("invalid-tag"));
        });
    }

    [Test]
    public void BinaryAndTemporalEdgeFailuresHaveSpecificStatuses()
    {
        FixFieldValueCodec codec = new FixFieldValueCodec();
        FieldParseResult emptyData = codec.DecodeFieldValue(ReadOnlySpan<byte>.Empty, 96, FixFieldType.Data, new DecodeOptions());
        EncodeResult invalidLimit = codec.EncodeFieldValue(new FixField { Tag = 1, Value = [49] }, FixFieldType.Int, new byte[8], new EncodeOptions { MaxFieldSize = -1 });
        EncodeResult invalidEncoding = codec.EncodeFieldValue(new FixField { Tag = 58, Value = [65] }, FixFieldType.String, new byte[8], new EncodeOptions { TextEncodingName = "utf-8" });
        EncodeResult missingValue = codec.EncodeFieldValue(new FixField { Tag = 58, Value = null! }, FixFieldType.String, new byte[8], new EncodeOptions());
        EncodeResult emptyDataEncoding = codec.EncodeFieldValue(new FixField { Tag = 96, Value = [] }, FixFieldType.Data, new byte[8], new EncodeOptions());
        EncodeResult invalidTimestamp = codec.EncodeFieldValue(new FixField { Tag = 60, Value = "not-a-time"u8.ToArray() }, FixFieldType.UtcTimestamp, new byte[32], new EncodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(emptyData.Status, Is.EqualTo(DecodeStatus.Incomplete));
            Assert.That(emptyData.ErrorCode, Is.EqualTo("incomplete"));
            Assert.That(invalidLimit.ErrorCode, Is.EqualTo("invalid-limit"));
            Assert.That(invalidEncoding.ErrorCode, Is.EqualTo("unsupported-text-encoding"));
            Assert.That(missingValue.ErrorCode, Is.EqualTo("missing-field-value"));
            Assert.That(emptyDataEncoding.ErrorCode, Is.EqualTo("empty-data"));
            Assert.That(invalidTimestamp.Status, Is.EqualTo(EncodeStatus.Invalid));
            Assert.That(invalidTimestamp.ErrorCode, Is.Not.Empty);
        });
    }

    private static void AssertEncodedText(FixFieldValueCodec codec, EncodeOptions options, byte[] destination, FixFieldType fieldType, string source, string expected)
    {
        Array.Fill(destination, (byte)88);
        FixField field = new FixField { Tag = 1, Value = Encoding.ASCII.GetBytes(source) };
        EncodeResult result = codec.EncodeFieldValue(field, fieldType, destination, options);
        Assert.That(result.Status, Is.EqualTo(EncodeStatus.Success), result.ErrorCode);
        Assert.That(Encoding.ASCII.GetString(destination, 0, result.BytesWritten), Is.EqualTo(expected));
    }
}
