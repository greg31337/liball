using token;

namespace token.Tests;

public class Utf8CodecTests
{
    [Test]
    public void EncodeProducesUtf8ForAllSequenceLengths()
    {
        string text = "A\u00A2\u20AC\U00010348";

        TokenBytesResult result = Utf8Codec.Encode(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Bytes, Is.EqualTo(new byte[]
            {
                0x41,
                0xC2, 0xA2,
                0xE2, 0x82, 0xAC,
                0xF0, 0x90, 0x8D, 0x88
            }));
        });
    }

    [Test]
    public void EncodeRejectsUnpairedSurrogates()
    {
        TokenBytesResult highAtEnd = Utf8Codec.Encode(new string(new char[] { 'x', '\uD800' }));
        TokenBytesResult lowWithoutHigh = Utf8Codec.Encode(new string(new char[] { 'x', '\uDC00' }));
        TokenBytesResult consecutiveHigh = Utf8Codec.Encode(new string(new char[] { '\uD800', '\uD800' }));

        Assert.Multiple(() =>
        {
            Assert.That(highAtEnd.Success, Is.False);
            Assert.That(highAtEnd.Bytes, Is.Null);
            Assert.That(highAtEnd.Error.Code, Is.EqualTo("invalid_utf16"));
            Assert.That(highAtEnd.Error.Position, Is.EqualTo(1));
            Assert.That(lowWithoutHigh.Success, Is.False);
            Assert.That(lowWithoutHigh.Error.Position, Is.EqualTo(1));
            Assert.That(consecutiveHigh.Success, Is.False);
            Assert.That(consecutiveHigh.Error.Position, Is.Zero);
        });
    }

    [Test]
    public void EncodeSupportsCharacterSpanSlices()
    {
        string text = "xTokeny";

        TokenBytesResult result = Utf8Codec.Encode(text.AsSpan(1, 5));

        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 84, 111, 107, 101, 110 }));
    }

    [Test]
    public void DecodeReadsUtf8ForAllSequenceLengths()
    {
        byte[] bytes = new byte[]
        {
            0x41,
            0xC2, 0xA2,
            0xE2, 0x82, 0xAC,
            0xF0, 0x90, 0x8D, 0x88
        };

        TokenTextResult result = Utf8Codec.Decode(bytes);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Text, Is.EqualTo("A\u00A2\u20AC\U00010348"));
        });
    }

    [TestCase(new byte[] { 0x80 }, 0)]
    [TestCase(new byte[] { 0xC0, 0x80 }, 0)]
    [TestCase(new byte[] { 0xC2 }, 0)]
    [TestCase(new byte[] { 0xC2, 0x20 }, 1)]
    [TestCase(new byte[] { 0xE0, 0x80, 0x80 }, 1)]
    [TestCase(new byte[] { 0xED, 0xA0, 0x80 }, 1)]
    [TestCase(new byte[] { 0xE2, 0x82 }, 0)]
    [TestCase(new byte[] { 0xE2, 0x82, 0x20 }, 2)]
    [TestCase(new byte[] { 0xF0, 0x80, 0x80, 0x80 }, 1)]
    [TestCase(new byte[] { 0xF4, 0x90, 0x80, 0x80 }, 1)]
    [TestCase(new byte[] { 0xF0, 0x90, 0x80 }, 0)]
    [TestCase(new byte[] { 0xF5, 0x80, 0x80, 0x80 }, 0)]
    public void DecodeRejectsMalformedUtf8(byte[] bytes, int position)
    {
        TokenTextResult result = Utf8Codec.Decode(bytes);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Text, Is.Null);
            Assert.That(result.Error.Code, Is.EqualTo("invalid_utf8"));
            Assert.That(result.Error.Position, Is.EqualTo(position));
        });
    }

    [Test]
    public void DecodeSupportsByteSpanSlices()
    {
        byte[] bytes = new byte[] { 0xFF, 0x54, 0x6F, 0x6B, 0x65, 0x6E, 0xFF };

        TokenTextResult result = Utf8Codec.Decode(bytes.AsSpan(1, 5));

        Assert.That(result.Text, Is.EqualTo("Token"));
    }

    [Test]
    public void EmptyInputsSucceed()
    {
        TokenBytesResult encoded = Utf8Codec.Encode(string.Empty);
        TokenTextResult decoded = Utf8Codec.Decode(Array.Empty<byte>());

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Bytes, Is.Empty);
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Text, Is.Empty);
        });
    }

    [Test]
    public void ArrayAndStringOverloadsRejectNull()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => Utf8Codec.Encode((string)null), Throws.ArgumentNullException);
            Assert.That(() => Utf8Codec.Decode((byte[])null), Throws.ArgumentNullException);
        });
    }
}
