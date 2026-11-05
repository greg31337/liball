using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeByteEncodingTests
{
    [Test]
    public void EncodeUtf8_ReturnsExpectedBytesAndRoundTrips()
    {
        string text = "A" + char.ConvertFromUtf32(0x1F600) + "\u20AC";
        byte[] expected = new byte[] { 0x41, 0xF0, 0x9F, 0x98, 0x80, 0xE2, 0x82, 0xAC };

        byte[] bytes = new UnicodeByteEncoding().EncodeUtf8(text);

        Assert.That(bytes, Is.EqualTo(expected));
        Assert.That(new UnicodeByteEncoding().DecodeUtf8(bytes), Is.EqualTo(text));
    }

    [Test]
    public void EncodeUtf16Le_ReturnsExpectedBytesAndRoundTrips()
    {
        string text = "A" + char.ConvertFromUtf32(0x1F600) + "\u20AC";
        byte[] expected = new byte[] { 0x41, 0x00, 0x3D, 0xD8, 0x00, 0xDE, 0xAC, 0x20 };

        byte[] bytes = new UnicodeByteEncoding().EncodeUtf16Le(text);

        Assert.That(bytes, Is.EqualTo(expected));
        Assert.That(new UnicodeByteEncoding().DecodeUtf16Le(bytes), Is.EqualTo(text));
    }

    [Test]
    public void EncodeUtf16Be_ReturnsExpectedBytesAndRoundTrips()
    {
        string text = "A" + char.ConvertFromUtf32(0x1F600) + "\u20AC";
        byte[] expected = new byte[] { 0x00, 0x41, 0xD8, 0x3D, 0xDE, 0x00, 0x20, 0xAC };

        byte[] bytes = new UnicodeByteEncoding().EncodeUtf16Be(text);

        Assert.That(bytes, Is.EqualTo(expected));
        Assert.That(new UnicodeByteEncoding().DecodeUtf16Be(bytes), Is.EqualTo(text));
    }

    [Test]
    public void EncodeUtf32LeAndDefaultUtf32_ReturnExpectedBytesAndRoundTrip()
    {
        string text = "A" + char.ConvertFromUtf32(0x1F600) + "\u20AC";
        byte[] expected = new byte[] { 0x41, 0x00, 0x00, 0x00, 0x00, 0xF6, 0x01, 0x00, 0xAC, 0x20, 0x00, 0x00 };

        byte[] bytes = new UnicodeByteEncoding().EncodeUtf32Le(text);

        Assert.That(bytes, Is.EqualTo(expected));
        Assert.That(new UnicodeByteEncoding().EncodeUtf32(text), Is.EqualTo(expected));
        Assert.That(new UnicodeByteEncoding().DecodeUtf32Le(bytes), Is.EqualTo(text));
        Assert.That(new UnicodeByteEncoding().DecodeUtf32(bytes), Is.EqualTo(text));
    }

    [Test]
    public void EncodeUtf32Be_ReturnsExpectedBytesAndRoundTrips()
    {
        string text = "A" + char.ConvertFromUtf32(0x1F600) + "\u20AC";
        byte[] expected = new byte[] { 0x00, 0x00, 0x00, 0x41, 0x00, 0x01, 0xF6, 0x00, 0x00, 0x00, 0x20, 0xAC };

        byte[] bytes = new UnicodeByteEncoding().EncodeUtf32Be(text);

        Assert.That(bytes, Is.EqualTo(expected));
        Assert.That(new UnicodeByteEncoding().DecodeUtf32Be(bytes), Is.EqualTo(text));
    }

    [Test]
    public void EncodeMethods_ReplaceInvalidUtf16BeforeEncoding()
    {
        string text = "\uD800A";

        Assert.That(new UnicodeByteEncoding().EncodeUtf8(text), Is.EqualTo(new byte[] { 0xEF, 0xBF, 0xBD, 0x41 }));
        Assert.That(new UnicodeByteEncoding().EncodeUtf16Le(text), Is.EqualTo(new byte[] { 0xFD, 0xFF, 0x41, 0x00 }));
        Assert.That(new UnicodeByteEncoding().EncodeUtf16Be(text), Is.EqualTo(new byte[] { 0xFF, 0xFD, 0x00, 0x41 }));
        Assert.That(new UnicodeByteEncoding().EncodeUtf32Le(text), Is.EqualTo(new byte[] { 0xFD, 0xFF, 0x00, 0x00, 0x41, 0x00, 0x00, 0x00 }));
    }

    [Test]
    public void DecodeUtf8_ReplacesInvalidByteSequences()
    {
        byte[] bytes = new byte[] { 0x41, 0xC3, 0x28 };

        Assert.That(new UnicodeByteEncoding().DecodeUtf8(bytes), Is.EqualTo("A\uFFFD("));
    }

    [Test]
    public void PolicyMethods_RoundTripEveryExplicitEncoding()
    {
        UnicodeByteEncoding encoder = new UnicodeByteEncoding();
        string text = "A" + char.ConvertFromUtf32(0x1F600) + "\u20AC";
        UnicodeByteEncodingKind[] encodings = new UnicodeByteEncodingKind[]
        {
            UnicodeByteEncodingKind.Utf8,
            UnicodeByteEncodingKind.Utf16Le,
            UnicodeByteEncodingKind.Utf16Be,
            UnicodeByteEncodingKind.Utf32Le,
            UnicodeByteEncodingKind.Utf32Be
        };

        foreach (UnicodeByteEncodingKind encoding in encodings)
        {
            byte[] bytes = encoder.EncodeWithPolicy(text, encoding, UnicodeMalformedInputPolicy.Strict);
            string decoded = encoder.DecodeWithPolicy(bytes, encoding, UnicodeMalformedInputPolicy.Strict);

            Assert.That(decoded, Is.EqualTo(text), encoding.ToString());
        }
    }

    [Test]
    public void BufferMethods_ReportRequiredCapacityAndDoNotPartiallyWrite()
    {
        UnicodeByteEncoding encoder = new UnicodeByteEncoding();
        byte[] tooSmallBytes = new byte[] { 0xCC, 0xCC };

        UnicodeBufferResult encodeResult = encoder.EncodeBuffer(
            "\u20AC",
            tooSmallBytes,
            UnicodeByteEncodingKind.Utf8,
            UnicodeMalformedInputPolicy.Strict);

        Assert.That(encodeResult.Success, Is.False);
        Assert.That(encodeResult.RequiredLength, Is.EqualTo(3));
        Assert.That(encodeResult.WrittenLength, Is.EqualTo(0));
        Assert.That(tooSmallBytes, Is.EqualTo(new byte[] { 0xCC, 0xCC }));

        byte[] bytes = new byte[3];
        UnicodeBufferResult completeEncode = encoder.EncodeBuffer(
            "\u20AC",
            bytes,
            UnicodeByteEncodingKind.Utf8,
            UnicodeMalformedInputPolicy.Strict);

        Assert.That(completeEncode.Success, Is.True);
        Assert.That(completeEncode.RequiredLength, Is.EqualTo(3));
        Assert.That(completeEncode.WrittenLength, Is.EqualTo(3));
        Assert.That(bytes, Is.EqualTo(new byte[] { 0xE2, 0x82, 0xAC }));

        char[] tooSmallCharacters = new char[] { 'X' };
        UnicodeBufferResult decodeResult = encoder.DecodeBuffer(
            new byte[] { 0x41, 0x42 },
            tooSmallCharacters,
            UnicodeByteEncodingKind.Utf8,
            UnicodeMalformedInputPolicy.Strict);

        Assert.That(decodeResult.Success, Is.False);
        Assert.That(decodeResult.RequiredLength, Is.EqualTo(2));
        Assert.That(decodeResult.WrittenLength, Is.EqualTo(0));
        Assert.That(tooSmallCharacters, Is.EqualTo(new char[] { 'X' }));

        char[] characters = new char[2];
        UnicodeBufferResult completeDecode = encoder.DecodeBuffer(
            new byte[] { 0x41, 0x42 },
            characters,
            UnicodeByteEncodingKind.Utf8,
            UnicodeMalformedInputPolicy.Strict);

        Assert.That(completeDecode.Success, Is.True);
        Assert.That(completeDecode.RequiredLength, Is.EqualTo(2));
        Assert.That(completeDecode.WrittenLength, Is.EqualTo(2));
        Assert.That(new string(characters), Is.EqualTo("AB"));
    }

    [Test]
    public void StrictPolicy_ThrowsAndReplacementPolicySubstitutesMalformedInput()
    {
        UnicodeByteEncoding encoder = new UnicodeByteEncoding();

        Assert.Throws<System.Text.EncoderFallbackException>(() => encoder.EncodeWithPolicy(
            "\uD800",
            UnicodeByteEncodingKind.Utf8,
            UnicodeMalformedInputPolicy.Strict));
        Assert.That(
            encoder.EncodeWithPolicy(
                "\uD800",
                UnicodeByteEncodingKind.Utf8,
                UnicodeMalformedInputPolicy.Replace),
            Is.EqualTo(new byte[] { 0xEF, 0xBF, 0xBD }));

        byte[] invalidUtf8 = new byte[] { 0xC3, 0x28 };
        Assert.Throws<System.Text.DecoderFallbackException>(() => encoder.DecodeWithPolicy(
            invalidUtf8,
            UnicodeByteEncodingKind.Utf8,
            UnicodeMalformedInputPolicy.Strict));
        Assert.That(
            encoder.DecodeWithPolicy(
                invalidUtf8,
                UnicodeByteEncodingKind.Utf8,
                UnicodeMalformedInputPolicy.Replace),
            Is.EqualTo("\uFFFD("));
    }

    [Test]
    public void StrictPolicy_RejectsMalformedInputForEveryEncodingFamily()
    {
        UnicodeByteEncoding encoder = new UnicodeByteEncoding();
        UnicodeByteEncodingKind[] encodings = new UnicodeByteEncodingKind[]
        {
            UnicodeByteEncodingKind.Utf8,
            UnicodeByteEncodingKind.Utf16Le,
            UnicodeByteEncodingKind.Utf16Be,
            UnicodeByteEncodingKind.Utf32Le,
            UnicodeByteEncodingKind.Utf32Be
        };
        byte[][] malformed = new byte[][]
        {
            new byte[] { 0xC3, 0x28 },
            new byte[] { 0x41 },
            new byte[] { 0x00 },
            new byte[] { 0x00, 0x00, 0x11, 0x00 },
            new byte[] { 0x00, 0x11, 0x00, 0x00 }
        };

        for (int index = 0; index < encodings.Length; index++)
        {
            UnicodeByteEncodingKind encoding = encodings[index];
            byte[] bytes = malformed[index];

            Assert.Throws<System.Text.DecoderFallbackException>(() => encoder.DecodeWithPolicy(
                bytes,
                encoding,
                UnicodeMalformedInputPolicy.Strict), encoding.ToString());
        }
    }

    [Test]
    public void PolicyMethods_DoNotAddPreamblesAndPreserveDecodedBomCharacters()
    {
        UnicodeByteEncoding encoder = new UnicodeByteEncoding();

        Assert.That(
            encoder.EncodeWithPolicy("", UnicodeByteEncodingKind.Utf8, UnicodeMalformedInputPolicy.Strict),
            Is.Empty);
        Assert.That(
            encoder.EncodeWithPolicy("\uFEFFA", UnicodeByteEncodingKind.Utf8, UnicodeMalformedInputPolicy.Strict),
            Is.EqualTo(new byte[] { 0xEF, 0xBB, 0xBF, 0x41 }));
        Assert.That(
            encoder.DecodeWithPolicy(
                new byte[] { 0xEF, 0xBB, 0xBF, 0x41 },
                UnicodeByteEncodingKind.Utf8,
                UnicodeMalformedInputPolicy.Strict),
            Is.EqualTo("\uFEFFA"));
    }

    [Test]
    public void PolicyMethods_RejectUnknownEncodingAndPolicy()
    {
        UnicodeByteEncoding encoder = new UnicodeByteEncoding();

        Assert.Throws<ArgumentOutOfRangeException>(() => encoder.EncodeWithPolicy(
            "A",
            (UnicodeByteEncodingKind)99,
            UnicodeMalformedInputPolicy.Replace));
        Assert.Throws<ArgumentOutOfRangeException>(() => encoder.DecodeWithPolicy(
            new byte[] { 0x41 },
            UnicodeByteEncodingKind.Utf8,
            (UnicodeMalformedInputPolicy)99));
    }

    [Test]
    public void EncodingHelpers_ThrowForNullInputs()
    {
        Assert.Throws<ArgumentNullException>(() => new UnicodeByteEncoding().EncodeUtf8(null!));
        Assert.Throws<ArgumentNullException>(() => new UnicodeByteEncoding().DecodeUtf8(null!));
        Assert.Throws<ArgumentNullException>(() => new UnicodeByteEncoding().EncodeUtf16Le(null!));
        Assert.Throws<ArgumentNullException>(() => new UnicodeByteEncoding().DecodeUtf16Le(null!));
        Assert.Throws<ArgumentNullException>(() => new UnicodeByteEncoding().EncodeUtf16Be(null!));
        Assert.Throws<ArgumentNullException>(() => new UnicodeByteEncoding().DecodeUtf16Be(null!));
        Assert.Throws<ArgumentNullException>(() => new UnicodeByteEncoding().EncodeUtf32(null!));
        Assert.Throws<ArgumentNullException>(() => new UnicodeByteEncoding().DecodeUtf32(null!));
        Assert.Throws<ArgumentNullException>(() => new UnicodeByteEncoding().EncodeUtf32Le(null!));
        Assert.Throws<ArgumentNullException>(() => new UnicodeByteEncoding().DecodeUtf32Le(null!));
        Assert.Throws<ArgumentNullException>(() => new UnicodeByteEncoding().EncodeUtf32Be(null!));
        Assert.Throws<ArgumentNullException>(() => new UnicodeByteEncoding().DecodeUtf32Be(null!));
        Assert.Throws<ArgumentNullException>(() => new UnicodeByteEncoding().EncodeWithPolicy(
            null!,
            UnicodeByteEncodingKind.Utf8,
            UnicodeMalformedInputPolicy.Replace));
        Assert.Throws<ArgumentNullException>(() => new UnicodeByteEncoding().DecodeWithPolicy(
            null!,
            UnicodeByteEncodingKind.Utf8,
            UnicodeMalformedInputPolicy.Replace));
    }
}
