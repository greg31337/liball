namespace uri.Tests;

public class Utf8CodecCoreTests
{
    [TestCase(0, "00")]
    [TestCase(127, "7F")]
    [TestCase(128, "C280")]
    [TestCase(2047, "DFBF")]
    [TestCase(2048, "E0A080")]
    [TestCase(55295, "ED9FBF")]
    [TestCase(57344, "EE8080")]
    [TestCase(65535, "EFBFBF")]
    [TestCase(65536, "F0908080")]
    [TestCase(1114111, "F48FBFBF")]
    public void EncodeScalarProducesCanonicalUtf8(int scalar, string expectedHex)
    {
        Utf8EncodeResult result = Utf8CodecCore.EncodeScalar(scalar);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(Convert.ToHexString(result.Bytes), Is.EqualTo(expectedHex));
            Assert.That(result.ErrorOffset, Is.Zero);
            Assert.That(result.ErrorLength, Is.Zero);
        });
    }

    [TestCase(-1)]
    [TestCase(55296)]
    [TestCase(57343)]
    [TestCase(1114112)]
    public void EncodeScalarRejectsInvalidUnicodeValues(int scalar)
    {
        Utf8EncodeResult result = Utf8CodecCore.EncodeScalar(scalar);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Bytes, Is.Empty);
            Assert.That(Utf8CodecCore.GetEncodedLength(scalar), Is.Zero);
        });
    }

    [Test]
    public void WriteScalarWritesAtRequestedOffset()
    {
        byte[] output = new byte[] { 1, 2, 0, 0, 0, 0, 3 };

        int written = Utf8CodecCore.WriteScalar(128512, output, 2);

        Assert.Multiple(() =>
        {
            Assert.That(written, Is.EqualTo(4));
            Assert.That(Convert.ToHexString(output), Is.EqualTo("0102F09F988003"));
        });
    }

    [Test]
    public void EncodeTextHandlesEveryUtf8LengthInOnePassResult()
    {
        string text = "\u0000A\u00a2\u20ac\ud83d\ude00";

        Utf8EncodeResult result = Utf8CodecCore.EncodeText(text.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(Convert.ToHexString(result.Bytes), Is.EqualTo("0041C2A2E282ACF09F9880"));
        });
    }

    [Test]
    public void EncodeTextRejectsMalformedUtf16()
    {
        string isolatedHigh = new string(new char[] { (char)55296 });
        string lowAfterAscii = new string(new char[] { 'A', (char)57343 });
        string highBeforeAscii = new string(new char[] { (char)55296, 'A' });

        AssertMalformedEncoding(isolatedHigh, 0);
        AssertMalformedEncoding(lowAfterAscii, 1);
        AssertMalformedEncoding(highBeforeAscii, 0);
    }

    [TestCase("00", 0, 0, 1)]
    [TestCase("7F", 0, 127, 1)]
    [TestCase("C280", 0, 128, 2)]
    [TestCase("DFBF", 0, 2047, 2)]
    [TestCase("E0A080", 0, 2048, 3)]
    [TestCase("ED9FBF", 0, 55295, 3)]
    [TestCase("EE8080", 0, 57344, 3)]
    [TestCase("EFBFBF", 0, 65535, 3)]
    [TestCase("F0908080", 0, 65536, 4)]
    [TestCase("F48FBFBF", 0, 1114111, 4)]
    [TestCase("FF41", 1, 65, 1)]
    public void DecodeScalarAcceptsCanonicalUtf8(
        string hex,
        int offset,
        int expectedScalar,
        int expectedLength)
    {
        byte[] bytes = Convert.FromHexString(hex);

        Utf8ScalarResult result = Utf8CodecCore.DecodeScalar(bytes, offset);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ScalarValue, Is.EqualTo(expectedScalar));
            Assert.That(result.ByteLength, Is.EqualTo(expectedLength));
            Assert.That(result.ErrorOffset, Is.Zero);
            Assert.That(result.ErrorLength, Is.Zero);
        });
    }

    [TestCase("80", 0, 1)]
    [TestCase("C080", 0, 1)]
    [TestCase("C180", 0, 1)]
    [TestCase("C2", 1, 0)]
    [TestCase("C220", 1, 1)]
    [TestCase("E08080", 1, 1)]
    [TestCase("EDA080", 1, 1)]
    [TestCase("E228A1", 1, 1)]
    [TestCase("E282", 2, 0)]
    [TestCase("F0808080", 1, 1)]
    [TestCase("F4908080", 1, 1)]
    [TestCase("F09080", 3, 0)]
    [TestCase("F0908041", 3, 1)]
    [TestCase("F5808080", 0, 1)]
    [TestCase("FF", 0, 1)]
    public void DecodeScalarRejectsMalformedAndNonShortestUtf8(
        string hex,
        int expectedErrorOffset,
        int expectedErrorLength)
    {
        byte[] bytes = Convert.FromHexString(hex);

        Utf8ScalarResult result = Utf8CodecCore.DecodeScalar(bytes, 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ByteLength, Is.Zero);
            Assert.That(result.ErrorOffset, Is.EqualTo(expectedErrorOffset));
            Assert.That(result.ErrorLength, Is.EqualTo(expectedErrorLength));
        });
    }

    [Test]
    public void DecodeScalarReportsEmptyAndInvalidOffsetsDeterministically()
    {
        byte[] bytes = new byte[] { 65 };

        Utf8ScalarResult before = Utf8CodecCore.DecodeScalar(bytes, -1);
        Utf8ScalarResult atEnd = Utf8CodecCore.DecodeScalar(bytes, 1);
        Utf8ScalarResult beyondEnd = Utf8CodecCore.DecodeScalar(bytes, 2);

        Assert.Multiple(() =>
        {
            Assert.That(before.ErrorOffset, Is.Zero);
            Assert.That(before.ErrorLength, Is.Zero);
            Assert.That(atEnd.ErrorOffset, Is.EqualTo(1));
            Assert.That(atEnd.ErrorLength, Is.Zero);
            Assert.That(beyondEnd.ErrorOffset, Is.EqualTo(1));
            Assert.That(beyondEnd.ErrorLength, Is.Zero);
        });
    }

    [Test]
    public void EveryIllegalStandaloneLeadByteIsRejectedAtItsOwnOffset()
    {
        for (int value = 128; value <= 193; value++)
        {
            AssertIllegalLead((byte)value);
        }

        for (int value = 245; value <= 255; value++)
        {
            AssertIllegalLead((byte)value);
        }
    }

    [Test]
    public void RepresentativeScalarsAcrossUnicodeRangeRoundTripStrictly()
    {
        int scalar = 0;
        while (scalar <= 1114111)
        {
            if (UnicodeScalar.IsValid(scalar))
            {
                Utf8EncodeResult encoded = Utf8CodecCore.EncodeScalar(scalar);
                Utf8ScalarResult decoded = Utf8CodecCore.DecodeScalar(encoded.Bytes, 0);

                Assert.That(encoded.Success, Is.True, $"encode U+{scalar:X}");
                Assert.That(decoded.Success, Is.True, $"decode U+{scalar:X}");
                Assert.That(decoded.ScalarValue, Is.EqualTo(scalar), $"round trip U+{scalar:X}");
                Assert.That(decoded.ByteLength, Is.EqualTo(encoded.Bytes.Length));
            }

            scalar = scalar + 997;
        }
    }

    [Test]
    public void EncodeAndDecodeTextRoundTripsAllScalarLengths()
    {
        string text = "\u0000ASCII \u00a2 \u20ac \ud83d\ude00 \udbff\udfff";

        Utf8EncodeResult encoded = Utf8CodecCore.EncodeText(text.AsSpan());
        Utf8DecodeResult decoded = Utf8CodecCore.DecodeBytes(encoded.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Text, Is.EqualTo(text));
            Assert.That(decoded.ErrorOffset, Is.Zero);
            Assert.That(decoded.ErrorLength, Is.Zero);
        });
    }

    [Test]
    public void DecodeBytesReturnsNoPartialTextOnFailure()
    {
        byte[] bytes = Convert.FromHexString("41E282");

        Utf8DecodeResult result = Utf8CodecCore.DecodeBytes(bytes);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Text, Is.Empty);
            Assert.That(result.ErrorOffset, Is.EqualTo(3));
            Assert.That(result.ErrorLength, Is.Zero);
        });
    }

    [Test]
    public void EmptyTextAndBytesRoundTripSuccessfully()
    {
        Utf8EncodeResult encoded = Utf8CodecCore.EncodeText("".AsSpan());
        Utf8DecodeResult decoded = Utf8CodecCore.DecodeBytes(new byte[0]);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Bytes, Is.Empty);
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Text, Is.Empty);
        });
    }

    private static void AssertMalformedEncoding(string text, int errorOffset)
    {
        Utf8EncodeResult result = Utf8CodecCore.EncodeText(text.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Bytes, Is.Empty);
            Assert.That(result.ErrorOffset, Is.EqualTo(errorOffset));
            Assert.That(result.ErrorLength, Is.EqualTo(1));
        });
    }

    private static void AssertIllegalLead(byte value)
    {
        byte[] bytes = new byte[] { 65, value };

        Utf8ScalarResult result = Utf8CodecCore.DecodeScalar(bytes, 1);

        Assert.That(result.Success, Is.False, $"lead byte 0x{value:X2}");
        Assert.That(result.ErrorOffset, Is.EqualTo(1));
        Assert.That(result.ErrorLength, Is.EqualTo(1));
    }
}
