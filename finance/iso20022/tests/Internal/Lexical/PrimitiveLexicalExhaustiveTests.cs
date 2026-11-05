using System.Globalization;
using iso20022;

namespace iso20022.Tests;

public class PrimitiveLexicalExhaustiveTests
{
    [Test]
    public void SignedIntegerBoundariesRoundTripInvariantly()
    {
        int[] intValues = new int[] { int.MinValue, int.MinValue + 1, -1, 0, 1, int.MaxValue - 1, int.MaxValue };
        long[] longValues = new long[] { long.MinValue, long.MinValue + 1, -1L, 0L, 1L, long.MaxValue - 1, long.MaxValue };

        foreach (int value in intValues)
        {
            string text = IntegerLexical.FormatInt32(value);
            IntegerLexicalResult parsed = IntegerLexical.ParseInt32(text.AsSpan());
            Assert.That(parsed.Success, Is.True, text);
            Assert.That(parsed.Value, Is.EqualTo(value), text);
        }

        foreach (long value in longValues)
        {
            string text = IntegerLexical.FormatInt64(value);
            LongIntegerLexicalResult parsed = IntegerLexical.ParseInt64(text.AsSpan());
            Assert.That(parsed.Success, Is.True, text);
            Assert.That(parsed.Value, Is.EqualTo(value), text);
        }
    }

    [TestCase("-0", "-0", true)]
    [TestCase("+0.0", "0.0", false)]
    [TestCase("-.5", "-0.5", false)]
    [TestCase("00012.3400", "12.3400", false)]
    [TestCase("0.0000000000000000000000000001", "0.0000000000000000000000000001", false)]
    [TestCase("1.00000000000000000000000000000", "1.00000000000000000000000000000", false)]
    [TestCase("79228162514264337593543950335.0", "79228162514264337593543950335.0", false)]
    [TestCase("-79228162514264337593543950335", "-79228162514264337593543950335", false)]
    public void EveryRepresentableDecimalShapeRoundTripsWithDeclaredScale(string text, string expectedFormat, bool negativeZero)
    {
        IsoDecimalValue? parsed = DecimalLexical.ParseDecimal(text.AsSpan());

        Assert.That(parsed, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(parsed?.IsNegativeZero, Is.EqualTo(negativeZero));
            Assert.That(parsed?.LexicalValue, Is.EqualTo(text));
            Assert.That(DecimalLexical.FormatDecimal(parsed!), Is.EqualTo(expectedFormat));
        });
    }

    [Test]
    public void DecimalFormatterRejectsInconsistentOrUnsupportedState()
    {
        IsoDecimalValue negativeScale = new IsoDecimalValue { Value = 1m, Scale = -1 };
        IsoDecimalValue excessiveScale = new IsoDecimalValue { Value = 1m, Scale = 1000 };
        IsoDecimalValue falseNegativeZero = new IsoDecimalValue { Value = 1m, Scale = 0, IsNegativeZero = true };
        IsoDecimalValue precisionLoss = new IsoDecimalValue { Value = 1.25m, Scale = 1 };

        Assert.Multiple(() =>
        {
            Assert.That(DecimalLexical.FormatDecimal(negativeScale), Is.Null);
            Assert.That(DecimalLexical.FormatDecimal(excessiveScale), Is.Null);
            Assert.That(DecimalLexical.FormatDecimal(falseNegativeZero), Is.Null);
            Assert.That(DecimalLexical.FormatDecimal(precisionLoss), Is.Null);
            Assert.That(DecimalLexical.FormatDecimal(null!), Is.Null);
        });
    }

    [Test]
    public void BinaryEncodingsRoundTripEveryLengthAndByteValue()
    {
        int length = 0;
        while (length <= 256)
        {
            byte[] value = new byte[length];
            int index = 0;
            while (index < value.Length)
            {
                value[index] = (byte)((index * 73 + length * 29) & 255);
                index++;
            }

            string base64 = BinaryLexical.FormatBase64(value);
            string hexadecimal = BinaryLexical.FormatHexadecimal(value);
            IsoBinaryValue? parsedBase64 = BinaryLexical.ParseBase64(base64.AsSpan());
            IsoBinaryValue? parsedHexadecimal = BinaryLexical.ParseHexadecimal(hexadecimal.AsSpan());
            Assert.That(parsedBase64, Is.Not.Null, "Base64 length " + length.ToString(CultureInfo.InvariantCulture));
            Assert.That(parsedHexadecimal, Is.Not.Null, "Hex length " + length.ToString(CultureInfo.InvariantCulture));
            Assert.That(parsedBase64?.Value, Is.EqualTo(value), "Base64 length " + length.ToString(CultureInfo.InvariantCulture));
            Assert.That(parsedHexadecimal?.Value, Is.EqualTo(value), "Hex length " + length.ToString(CultureInfo.InvariantCulture));
            length++;
        }
    }

    [Test]
    public void Base64ParserAcceptsXmlWhitespaceAfterEveryLexicalCharacter()
    {
        byte[] value = new byte[] { 0, 1, 2, 3, 127, 128, 254, 255 };
        string canonical = BinaryLexical.FormatBase64(value);
        string spaced = string.Empty;
        int index = 0;
        while (index < canonical.Length)
        {
            spaced = spaced + canonical[index] + " \t\r\n";
            index++;
        }

        IsoBinaryValue? parsed = BinaryLexical.ParseBase64(spaced.AsSpan());

        Assert.That(parsed, Is.Not.Null);
        Assert.That(parsed?.Value, Is.EqualTo(value));
    }

    [Test]
    public void PrimitiveParsersDistinguishAbsentFromValidEmptyBinaryValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(IntegerLexical.ParseInt32(ReadOnlySpan<char>.Empty).Success, Is.False);
            Assert.That(IntegerLexical.ParseInt64(ReadOnlySpan<char>.Empty).Success, Is.False);
            Assert.That(DecimalLexical.ParseDecimal(ReadOnlySpan<char>.Empty), Is.Null);
            Assert.That(BooleanLexical.ParseBoolean(ReadOnlySpan<char>.Empty), Is.Null);
            Assert.That(DateTimeLexical.ParseDate(ReadOnlySpan<char>.Empty), Is.Null);
            Assert.That(DateTimeLexical.ParseTime(ReadOnlySpan<char>.Empty), Is.Null);
            Assert.That(DateTimeLexical.ParseDateTime(ReadOnlySpan<char>.Empty), Is.Null);
            Assert.That(DurationLexical.ParseDuration(ReadOnlySpan<char>.Empty), Is.Null);
            Assert.That(PartialDateLexical.ParsePartialDate(ReadOnlySpan<char>.Empty), Is.Null);
            Assert.That(BinaryLexical.ParseBase64(ReadOnlySpan<char>.Empty)?.Value, Is.Empty);
            Assert.That(BinaryLexical.ParseHexadecimal(ReadOnlySpan<char>.Empty)?.Value, Is.Empty);
        });
    }

    [Test]
    public void XmlNameStartAndNameCharacterRangesHonorEverySpecifiedBoundary()
    {
        int[] validStartCodePoints = new int[]
        {
            58, 65, 90, 95, 97, 122, 192, 214, 216, 246, 248, 767, 880, 893, 895, 8191,
            8204, 8205, 8304, 8591, 11264, 12271, 12289, 55295, 63744, 64975, 65008,
            65533, 65536, 983039
        };
        int[] invalidStartCodePoints = new int[]
        {
            0, 45, 48, 57, 64, 91, 96, 123, 183, 215, 247, 768, 879, 894, 8192,
            8203, 8206, 8303, 8592, 11263, 12272, 12288, 63743, 64976, 65007,
            65534, 983040
        };
        int[] additionalNameCharacters = new int[] { 45, 46, 48, 57, 183, 768, 879, 8255, 8256 };

        foreach (int codePoint in validStartCodePoints)
        {
            string text = char.ConvertFromUtf32(codePoint);
            Assert.That(XmlNameLexical.IsXmlName(text.AsSpan()), Is.True, "Valid start U+" + codePoint.ToString("X", CultureInfo.InvariantCulture));
        }

        foreach (int codePoint in invalidStartCodePoints)
        {
            string text = char.ConvertFromUtf32(codePoint);
            Assert.That(XmlNameLexical.IsXmlName(text.AsSpan()), Is.False, "Invalid start U+" + codePoint.ToString("X", CultureInfo.InvariantCulture));
        }

        foreach (int codePoint in additionalNameCharacters)
        {
            string text = "A" + char.ConvertFromUtf32(codePoint);
            Assert.That(XmlNameLexical.IsXmlName(text.AsSpan()), Is.True, "Valid continuation U+" + codePoint.ToString("X", CultureInfo.InvariantCulture));
        }
    }
}
