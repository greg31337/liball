using System;
using System.Globalization;
using System.Text;
using fix;

namespace fix.Tests;

[TestFixture]
public class ScalarCodecExhaustiveTests
{
    [Test]
    public void PublicCodecCanonicallyRoundTripsEveryScalarDatatype()
    {
        FixFieldType[] types = ScalarTypes();
        string[] sources =
        [
            "-00023", "00023", "000", "00023", "35",
            "-00023.2300", "00023.2300", "-00023.2300", "00023.2300", "-00023.2300", "00023.2300",
            "é", "N", "Café", "2 A f", "AV AN A", "USD", "US", "XNAS"
        ];
        string[] canonical =
        [
            "-23", "23", "0", "23", "35",
            "-23.23", "23.23", "-23.23", "23.23", "-23.23", "23.23",
            "é", "N", "Café", "2 A f", "AV AN A", "USD", "US", "XNAS"
        ];
        FixFieldValueCodec codec = new FixFieldValueCodec();

        Assert.That(types, Has.Length.EqualTo(sources.Length));
        Assert.Multiple(() =>
        {
            for (int index = 0; index < types.Length; index++)
            {
                byte[] first = EncodeExact(codec, types[index], sources[index], new EncodeOptions());
                FieldParseResult decoded = codec.DecodeFieldValue(first, 100 + index, types[index], new DecodeOptions());
                Assert.That(Encoding.Latin1.GetString(first), Is.EqualTo(canonical[index]), types[index].ToString());
                Assert.That(decoded.Status, Is.EqualTo(DecodeStatus.Success), types[index] + ": " + decoded.ErrorCode);
                Assert.That(decoded.Consumed, Is.EqualTo(first.Length), types[index].ToString());
                Assert.That(decoded.Required, Is.EqualTo(first.Length), types[index].ToString());
                byte[] second = EncodeExact(codec, types[index], Encoding.Latin1.GetString(decoded.Field!.Value), new EncodeOptions());
                Assert.That(second, Is.EqualTo(first), types[index].ToString());
            }
        });
    }

    [Test]
    public void EveryScalarDatatypeHonorsExactLimitsAndNeverPartiallyWrites()
    {
        FixFieldType[] types = ScalarTypes();
        string[] values =
        [
            "123", "123", "123", "123", "123",
            "12.5", "12.5", "12.5", "12.5", "12.5", "12.5",
            "A", "Y", "ABC", "A B", "AA BB", "USD", "US", "XNAS"
        ];
        FixFieldValueCodec codec = new FixFieldValueCodec();

        Assert.Multiple(() =>
        {
            for (int index = 0; index < types.Length; index++)
            {
                byte[] source = Encoding.Latin1.GetBytes(values[index]);
                DecodeOptions exactDecode = new DecodeOptions { MaxFieldSize = source.Length };
                DecodeOptions limitedDecode = new DecodeOptions { MaxFieldSize = source.Length - 1 };
                EncodeOptions exactEncode = new EncodeOptions { MaxFieldSize = source.Length };
                EncodeOptions limitedEncode = new EncodeOptions { MaxFieldSize = source.Length - 1 };
                FieldParseResult decoded = codec.DecodeFieldValue(source, 200 + index, types[index], exactDecode);
                FieldParseResult decodeLimit = codec.DecodeFieldValue(source, 200 + index, types[index], limitedDecode);
                byte[] destination = new byte[source.Length + 2];
                Array.Fill(destination, (byte)88);
                byte[] original = destination.AsSpan().ToArray();
                EncodeResult shortResult = codec.EncodeFieldValue(new FixField { Tag = 200 + index, Value = source }, types[index], destination.AsSpan(0, source.Length - 1), exactEncode);
                EncodeResult encodeLimit = codec.EncodeFieldValue(new FixField { Tag = 200 + index, Value = source }, types[index], destination, limitedEncode);

                Assert.That(decoded.Status, Is.EqualTo(DecodeStatus.Success), types[index].ToString());
                Assert.That(decodeLimit.Status, Is.EqualTo(DecodeStatus.LimitExceeded), types[index].ToString());
                Assert.That(decodeLimit.Required, Is.EqualTo(source.Length), types[index].ToString());
                Assert.That(shortResult.Status, Is.EqualTo(EncodeStatus.DestinationTooSmall), types[index].ToString());
                Assert.That(shortResult.RequiredSize, Is.EqualTo(source.Length), types[index].ToString());
                Assert.That(encodeLimit.Status, Is.EqualTo(EncodeStatus.LimitExceeded), types[index].ToString());
                Assert.That(encodeLimit.RequiredSize, Is.EqualTo(source.Length), types[index].ToString());
                Assert.That(destination, Is.EqualTo(original), types[index].ToString());
            }
        });
    }

    [Test]
    public void IntegerAliasesCoverSignedBoundariesZerosAndMalformedValues()
    {
        FixFieldValueCodec codec = new FixFieldValueCodec();
        FixFieldType[] unsignedAliases = [FixFieldType.SeqNum, FixFieldType.NumInGroup, FixFieldType.Length, FixFieldType.TagNum];

        Assert.Multiple(() =>
        {
            Assert.That(Text(EncodeExact(codec, FixFieldType.Int, "-2147483648", new EncodeOptions())), Is.EqualTo("-2147483648"));
            Assert.That(Text(EncodeExact(codec, FixFieldType.Int, "2147483647", new EncodeOptions())), Is.EqualTo("2147483647"));
            Assert.That(Text(EncodeExact(codec, FixFieldType.Int, "-000", new EncodeOptions())), Is.EqualTo("0"));
            AssertEncodeError(codec, FixFieldType.Int, "2147483648", "overflow");
            AssertEncodeError(codec, FixFieldType.Int, "-2147483649", "overflow");
            AssertEncodeError(codec, FixFieldType.Int, "+1", "invalid-digit");
            AssertEncodeError(codec, FixFieldType.Int, "1 ", "invalid-digit");
            for (int index = 0; index < unsignedAliases.Length; index++)
            {
                FixFieldType type = unsignedAliases[index];
                Assert.That(Text(EncodeExact(codec, type, "2147483647", new EncodeOptions())), Is.EqualTo("2147483647"), type.ToString());
                AssertEncodeError(codec, type, "-1", "negative-value");
            }
            Assert.That(Text(EncodeExact(codec, FixFieldType.NumInGroup, "000", new EncodeOptions())), Is.EqualTo("0"));
            AssertEncodeError(codec, FixFieldType.SeqNum, "0", "zero-not-allowed");
            AssertEncodeError(codec, FixFieldType.Length, "0", "zero-not-allowed");
            AssertEncodeError(codec, FixFieldType.TagNum, "0", "zero-not-allowed");
            AssertEncodeError(codec, FixFieldType.TagNum, "035", "leading-zero");
        });
    }

    [Test]
    public void DecimalAliasesCoverMaximumPrecisionCanonicalZeroAndInvariantCulture()
    {
        FixFieldType[] aliases = DecimalTypes();
        FixFieldValueCodec codec = new FixFieldValueCodec();
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("ar-SA");
            Assert.Multiple(() =>
            {
                for (int index = 0; index < aliases.Length; index++)
                {
                    FixFieldType type = aliases[index];
                    Assert.That(Text(EncodeExact(codec, type, "79228162514264337593543950335", new EncodeOptions())), Is.EqualTo("79228162514264337593543950335"), type.ToString());
                    Assert.That(Text(EncodeExact(codec, type, "-79228162514264337593543950335", new EncodeOptions())), Is.EqualTo("-79228162514264337593543950335"), type.ToString());
                    Assert.That(Text(EncodeExact(codec, type, "0.1234567890123456789012345678", new EncodeOptions())), Is.EqualTo("0.1234567890123456789012345678"), type.ToString());
                    Assert.That(Text(EncodeExact(codec, type, "-000.0000", new EncodeOptions())), Is.EqualTo("0"), type.ToString());
                    Assert.That(Text(EncodeExact(codec, type, "23.", new EncodeOptions())), Is.EqualTo("23"), type.ToString());
                }
            });
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Test]
    public void DecimalAliasesRejectEveryMalformedShapeAndExcessPrecision()
    {
        FixFieldType[] aliases = DecimalTypes();
        string[] malformed = ["", "-", ".5", "-.5", "+1", "1,5", "1e2", "1.2.3", "1 2"];
        string[] errors = ["incomplete", "incomplete", "invalid-decimal-point", "invalid-decimal-point", "invalid-digit", "invalid-digit", "invalid-digit", "invalid-decimal-point", "invalid-digit"];
        FixFieldValueCodec codec = new FixFieldValueCodec();

        Assert.Multiple(() =>
        {
            for (int typeIndex = 0; typeIndex < aliases.Length; typeIndex++)
            {
                for (int valueIndex = 0; valueIndex < malformed.Length; valueIndex++)
                {
                    AssertEncodeError(codec, aliases[typeIndex], malformed[valueIndex], errors[valueIndex]);
                }
                AssertEncodeError(codec, aliases[typeIndex], "0.12345678901234567890123456789", "precision-exceeded");
                AssertEncodeError(codec, aliases[typeIndex], "79228162514264337593543950336", "overflow");
            }
        });
    }

    [Test]
    public void CharacterAndBooleanParsingIsExhaustiveAcrossAllByteValues()
    {
        FixFieldValueCodec codec = new FixFieldValueCodec();

        Assert.Multiple(() =>
        {
            for (int value = 0; value <= 255; value++)
            {
                byte[] source = [(byte)value];
                FieldParseResult character = codec.DecodeFieldValue(source, 300, FixFieldType.Char, new DecodeOptions());
                EncodeResult encodedCharacter = codec.EncodeFieldValue(new FixField { Tag = 300, Value = source }, FixFieldType.Char, new byte[1], new EncodeOptions());
                bool validCharacter = value != FixDefaults.Soh;
                Assert.That(character.Status == DecodeStatus.Success, Is.EqualTo(validCharacter), "Char " + value);
                Assert.That(encodedCharacter.Status == EncodeStatus.Success, Is.EqualTo(validCharacter), "Char " + value);

                FieldParseResult boolean = codec.DecodeFieldValue(source, 301, FixFieldType.Boolean, new DecodeOptions());
                EncodeResult encodedBoolean = codec.EncodeFieldValue(new FixField { Tag = 301, Value = source }, FixFieldType.Boolean, new byte[1], new EncodeOptions());
                bool validBoolean = value == 89 || value == 78;
                Assert.That(boolean.Status == DecodeStatus.Success, Is.EqualTo(validBoolean), "Boolean " + value);
                Assert.That(encodedBoolean.Status == EncodeStatus.Success, Is.EqualTo(validBoolean), "Boolean " + value);
            }
        });
    }

    [Test]
    public void Latin1TextRoundTripsEveryPermittedByteAndAsciiPolicyIsSeparate()
    {
        byte[] allLatin1 = new byte[255];
        int destinationIndex = 0;
        for (int value = 0; value <= 255; value++)
        {
            if (value != FixDefaults.Soh)
            {
                allLatin1[destinationIndex] = (byte)value;
                destinationIndex++;
            }
        }
        FixFieldValueCodec codec = new FixFieldValueCodec();
        FieldParseResult decoded = codec.DecodeFieldValue(allLatin1, 400, FixFieldType.String, new DecodeOptions());
        byte[] destination = new byte[allLatin1.Length];
        EncodeResult encoded = codec.EncodeFieldValue(new FixField { Tag = 400, Value = allLatin1 }, FixFieldType.String, destination, new EncodeOptions());
        DecodeOptions asciiDecode = new DecodeOptions { TextEncodingName = "ASCII" };
        EncodeOptions asciiEncode = new EncodeOptions { TextEncodingName = "US-ASCII" };
        FieldParseResult nonAsciiDecode = codec.DecodeFieldValue([233], 401, FixFieldType.String, asciiDecode);
        EncodeResult nonAsciiEncode = codec.EncodeFieldValue(new FixField { Tag = 401, Value = [233] }, FixFieldType.String, new byte[1], asciiEncode);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Status, Is.EqualTo(DecodeStatus.Success), decoded.ErrorCode);
            Assert.That(decoded.Field!.Value, Is.EqualTo(allLatin1));
            Assert.That(encoded.Status, Is.EqualTo(EncodeStatus.Success), encoded.ErrorCode);
            Assert.That(destination, Is.EqualTo(allLatin1));
            Assert.That(nonAsciiDecode.ErrorCode, Is.EqualTo("invalid-character"));
            Assert.That(nonAsciiDecode.ErrorOffset, Is.Zero);
            Assert.That(nonAsciiEncode.ErrorCode, Is.EqualTo("invalid-character"));
        });
    }

    [Test]
    public void FixedLengthAndMultipleValueTextAliasesCoverBoundariesAndMalformedTokens()
    {
        FixFieldValueCodec codec = new FixFieldValueCodec();

        Assert.Multiple(() =>
        {
            Assert.That(Text(EncodeExact(codec, FixFieldType.Country, "US", new EncodeOptions())), Is.EqualTo("US"));
            Assert.That(Text(EncodeExact(codec, FixFieldType.Currency, "USD", new EncodeOptions())), Is.EqualTo("USD"));
            Assert.That(Text(EncodeExact(codec, FixFieldType.Exchange, "X", new EncodeOptions())), Is.EqualTo("X"));
            Assert.That(Text(EncodeExact(codec, FixFieldType.MultipleCharValue, "0 9 A Z a z", new EncodeOptions())), Is.EqualTo("0 9 A Z a z"));
            Assert.That(Text(EncodeExact(codec, FixFieldType.MultipleStringValue, "A Café 123", new EncodeOptions())), Is.EqualTo("A Café 123"));
            AssertEncodeError(codec, FixFieldType.Country, "U", "invalid-length");
            AssertEncodeError(codec, FixFieldType.Country, "USA", "invalid-length");
            AssertEncodeError(codec, FixFieldType.Currency, "US", "invalid-length");
            AssertEncodeError(codec, FixFieldType.Currency, "USDX", "invalid-length");
            AssertEncodeError(codec, FixFieldType.MultipleCharValue, "A ?", "invalid-multiple-char-value");
            AssertEncodeError(codec, FixFieldType.MultipleCharValue, "AA", "invalid-multiple-char-value");
            AssertEncodeError(codec, FixFieldType.MultipleStringValue, " A", "empty-multivalue-token");
            AssertEncodeError(codec, FixFieldType.MultipleStringValue, "A ", "empty-multivalue-token");
            AssertEncodeError(codec, FixFieldType.MultipleStringValue, "A  B", "empty-multivalue-token");
        });
    }

    private static byte[] EncodeExact(FixFieldValueCodec codec, FixFieldType type, string value, EncodeOptions options)
    {
        FixField field = new FixField { Tag = 500, Value = Encoding.Latin1.GetBytes(value) };
        EncodeResult measured = codec.EncodeFieldValue(field, type, Span<byte>.Empty, options);
        Assert.That(measured.Status, Is.EqualTo(EncodeStatus.DestinationTooSmall), type + ": " + measured.ErrorCode);
        byte[] destination = new byte[measured.RequiredSize];
        EncodeResult written = codec.EncodeFieldValue(field, type, destination, options);
        Assert.That(written.Status, Is.EqualTo(EncodeStatus.Success), type + ": " + written.ErrorCode);
        Assert.That(written.BytesWritten, Is.EqualTo(destination.Length), type.ToString());
        Assert.That(written.RequiredSize, Is.EqualTo(destination.Length), type.ToString());
        return destination;
    }

    private static void AssertEncodeError(FixFieldValueCodec codec, FixFieldType type, string value, string errorCode)
    {
        byte[] destination = [88, 88, 88, 88];
        byte[] original = destination.AsSpan().ToArray();
        FixField field = new FixField { Tag = 600, Value = Encoding.Latin1.GetBytes(value) };
        EncodeResult result = codec.EncodeFieldValue(field, type, destination, new EncodeOptions());
        Assert.That(result.Status, Is.Not.EqualTo(EncodeStatus.Success), type + ": " + value);
        Assert.That(result.ErrorCode, Is.EqualTo(errorCode), type + ": " + value);
        Assert.That(result.BytesWritten, Is.Zero, type + ": " + value);
        Assert.That(destination, Is.EqualTo(original), type + ": " + value);
    }

    private static FixFieldType[] ScalarTypes()
    {
        return
        [
            FixFieldType.Int,
            FixFieldType.SeqNum,
            FixFieldType.NumInGroup,
            FixFieldType.Length,
            FixFieldType.TagNum,
            FixFieldType.Float,
            FixFieldType.Qty,
            FixFieldType.Price,
            FixFieldType.PriceOffset,
            FixFieldType.Amt,
            FixFieldType.Percentage,
            FixFieldType.Char,
            FixFieldType.Boolean,
            FixFieldType.String,
            FixFieldType.MultipleCharValue,
            FixFieldType.MultipleStringValue,
            FixFieldType.Currency,
            FixFieldType.Country,
            FixFieldType.Exchange
        ];
    }

    private static FixFieldType[] DecimalTypes()
    {
        return [FixFieldType.Float, FixFieldType.Qty, FixFieldType.Price, FixFieldType.PriceOffset, FixFieldType.Amt, FixFieldType.Percentage];
    }

    private static string Text(byte[] value)
    {
        return Encoding.Latin1.GetString(value);
    }
}
