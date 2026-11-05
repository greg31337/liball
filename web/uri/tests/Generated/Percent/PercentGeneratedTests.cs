using System.Globalization;

namespace uri.Tests;

public class PercentGeneratedTests
{
    [Test]
    public void EveryEncodeSetAndByteRoundTripsThroughThePublicByteApi()
    {
        IPercentCodec codec = new PercentCodec();
        foreach (PercentEncodeSet encodeSet in Enum.GetValues<PercentEncodeSet>())
        {
            PercentCodecOptions options = new PercentCodecOptions();
            options.EncodeSet = encodeSet;
            for (int value = 0; value <= 255; value++)
            {
                byte[] input = new byte[] { (byte)value };

                string encoded = codec.EncodeBytes(input, options);
                PercentDecodeResult decoded = codec.DecodeBytes(encoded, options);

                string expected = GetExpectedEncoding(value, encodeSet);
                Assert.That(encoded, Is.EqualTo(expected), $"set {encodeSet}, byte {value}");
                Assert.That(decoded.Success, Is.True, $"set {encodeSet}, byte {value}");
                Assert.That(decoded.Bytes, Is.EqualTo(input), $"set {encodeSet}, byte {value}");
            }
        }
    }

    [Test]
    public void EveryEncodeSetAndAsciiCharacterMatchesAcrossTextAndSpanPaths()
    {
        IPercentCodec codec = new PercentCodec();
        foreach (PercentEncodeSet encodeSet in Enum.GetValues<PercentEncodeSet>())
        {
            PercentCodecOptions options = new PercentCodecOptions();
            options.EncodeSet = encodeSet;
            for (int value = 0; value <= 127; value++)
            {
                string input = new string((char)value, 1);

                string fromText = codec.EncodeText(input, options);
                string fromSpan = codec.EncodeSpan(input.AsSpan(), options);
                PercentDecodeResult decoded = codec.DecodeText(fromText, options);

                Assert.That(
                    fromText,
                    Is.EqualTo(GetExpectedEncoding(value, encodeSet)),
                    $"set {encodeSet}, ASCII {value}");
                Assert.That(fromSpan, Is.EqualTo(fromText), $"set {encodeSet}, ASCII {value}");
                Assert.That(decoded.Success, Is.True, $"set {encodeSet}, ASCII {value}");
                Assert.That(decoded.Text, Is.EqualTo(input), $"set {encodeSet}, ASCII {value}");
            }
        }
    }

    [Test]
    public void EveryByteAndHexLetterCaseCombinationValidatesDecodesAndNormalizes()
    {
        IPercentCodec codec = new PercentCodec();
        PercentCodecOptions options = new PercentCodecOptions();
        for (int value = 0; value <= 255; value++)
        {
            for (int variant = 0; variant < 4; variant++)
            {
                bool uppercaseHigh = (variant & 2) != 0;
                bool uppercaseLow = (variant & 1) != 0;
                string input = CreateCaseVariant((byte)value, uppercaseHigh, uppercaseLow);

                UriDiagnostic? validation = codec.ValidatePercentEncoding(input, options);
                PercentDecodeResult decoded = codec.DecodeBytes(input, options);
                string normalized = codec.NormalizePercentEncoding(input, options);

                string expectedNormalized;
                if (value <= 127 && Ascii.IsUnreserved((char)value))
                {
                    expectedNormalized = new string((char)value, 1);
                }
                else
                {
                    expectedNormalized = "%" + value.ToString("X2", CultureInfo.InvariantCulture);
                }

                Assert.That(validation, Is.Null, $"byte {value}, variant {variant}");
                Assert.That(decoded.Success, Is.True, $"byte {value}, variant {variant}");
                Assert.That(decoded.Bytes, Is.EqualTo(new byte[] { (byte)value }), $"byte {value}, variant {variant}");
                Assert.That(normalized, Is.EqualTo(expectedNormalized), $"byte {value}, variant {variant}");
            }
        }
    }

    [Test]
    public void EveryMalformedTripletPositionIsStableAcrossPublicValidationAndDecoding()
    {
        IPercentCodec codec = new PercentCodec();
        PercentCodecOptions options = new PercentCodecOptions();
        int position = 0;
        while (position <= 64)
        {
            string prefix = new string('a', position);
            AssertMalformedAt(codec, options, prefix + "%", position, 1);
            AssertMalformedAt(codec, options, prefix + "%0", position, 1);
            AssertMalformedAt(codec, options, prefix + "%G0", position + 1, 1);
            AssertMalformedAt(codec, options, prefix + "%0G", position + 2, 1);
            position = position + 1;
        }
    }

    [Test]
    public void EveryReservedCharacterRemainsEncodedDuringPublicNormalization()
    {
        IPercentCodec codec = new PercentCodec();
        PercentCodecOptions options = new PercentCodecOptions();
        int offset = 0;
        while (offset < UriCharacterSets.Reserved.Length)
        {
            char reserved = UriCharacterSets.Reserved[offset];
            string lowerTriplet = "%" + ((int)reserved).ToString("x2", CultureInfo.InvariantCulture);
            string upperTriplet = "%" + ((int)reserved).ToString("X2", CultureInfo.InvariantCulture);

            string encodedResult = codec.NormalizePercentEncoding(lowerTriplet, options);
            string literalResult = codec.NormalizePercentEncoding(new string(reserved, 1), options);

            Assert.That(encodedResult, Is.EqualTo(upperTriplet), $"reserved {reserved}");
            Assert.That(literalResult, Is.EqualTo(new string(reserved, 1)), $"literal {reserved}");
            offset = offset + 1;
        }
    }

    [Test]
    public void EveryDoubleEncodedByteStopsAfterExactlyOnePercentLayer()
    {
        IPercentCodec codec = new PercentCodec();
        PercentCodecOptions options = new PercentCodecOptions();
        for (int value = 0; value <= 255; value++)
        {
            string inner = value.ToString("x2", CultureInfo.InvariantCulture);
            string doubleEncoded = "%25" + inner;

            string normalized = codec.NormalizePercentEncoding(doubleEncoded, options);
            PercentDecodeResult decoded = codec.DecodeText(doubleEncoded, options);
            string encoded = codec.EncodeText("%" + inner, options);

            Assert.That(normalized, Is.EqualTo(doubleEncoded), $"normalize byte {value}");
            Assert.That(decoded.Success, Is.True, $"decode byte {value}");
            Assert.That(decoded.Text, Is.EqualTo("%" + inner), $"decode byte {value}");
            Assert.That(encoded, Is.EqualTo(doubleEncoded), $"encode byte {value}");
        }
    }

    private static string GetExpectedEncoding(int value, PercentEncodeSet encodeSet)
    {
        if (value <= 127 && PercentAllowlist.IsAllowed((char)value, encodeSet))
        {
            return new string((char)value, 1);
        }

        return "%" + value.ToString("X2", CultureInfo.InvariantCulture);
    }

    private static string CreateCaseVariant(byte value, bool uppercaseHigh, bool uppercaseLow)
    {
        char high = Ascii.GetUpperHexCharacter(value >> 4);
        char low = Ascii.GetUpperHexCharacter(value & 15);
        if (!uppercaseHigh)
        {
            high = char.ToLowerInvariant(high);
        }

        if (!uppercaseLow)
        {
            low = char.ToLowerInvariant(low);
        }

        return new string(new char[] { '%', high, low });
    }

    private static void AssertMalformedAt(
        IPercentCodec codec,
        PercentCodecOptions options,
        string input,
        int expectedOffset,
        int expectedLength)
    {
        UriDiagnostic? validation = codec.ValidatePercentEncoding(input, options);
        PercentDecodeResult bytes = codec.DecodeBytes(input, options);
        PercentDecodeResult text = codec.DecodeText(input, options);
        PercentEncodingException? normalization = Assert.Throws<PercentEncodingException>(
            () => codec.NormalizePercentEncoding(input, options));

        Assert.That(validation, Is.Not.Null, input);
        Assert.That(validation!.Code, Is.EqualTo(UriDiagnosticCode.InvalidPercentEncoding), input);
        Assert.That(validation.Range.Offset, Is.EqualTo(expectedOffset), input);
        Assert.That(validation.Range.Length, Is.EqualTo(expectedLength), input);
        Assert.That(bytes.Success, Is.False, input);
        Assert.That(bytes.Diagnostic!.Range.Offset, Is.EqualTo(expectedOffset), input);
        Assert.That(bytes.Diagnostic.Range.Length, Is.EqualTo(expectedLength), input);
        Assert.That(text.Success, Is.False, input);
        Assert.That(text.Diagnostic!.Range.Offset, Is.EqualTo(expectedOffset), input);
        Assert.That(text.Diagnostic.Range.Length, Is.EqualTo(expectedLength), input);
        Assert.That(normalization, Is.Not.Null, input);
        Assert.That(normalization!.Diagnostic.Range.Offset, Is.EqualTo(expectedOffset), input);
        Assert.That(normalization.Diagnostic.Range.Length, Is.EqualTo(expectedLength), input);
    }
}
