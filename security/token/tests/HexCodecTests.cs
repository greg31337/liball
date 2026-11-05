using token;

namespace token.Tests;

public class HexCodecTests
{
    [Test]
    public void EncodeUsesLowercaseHexadecimal()
    {
        TokenTextResult result = HexCodec.Encode(new byte[] { 0x00, 0x01, 0x09, 0x0A, 0x10, 0x7F, 0x80, 0xAB, 0xFF });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Text, Is.EqualTo("0001090a107f80abff"));
        });
    }

    [Test]
    public void EncodeMatchesBuiltInHexAcrossAllByteValues()
    {
        byte[] bytes = Enumerable.Range(0, 256).Select(value => (byte)value).ToArray();

        TokenTextResult result = HexCodec.Encode(bytes);

        Assert.That(result.Text, Is.EqualTo(Convert.ToHexString(bytes).ToLowerInvariant()));
    }

    [Test]
    public void EncodeSupportsByteSpanSlices()
    {
        byte[] bytes = new byte[] { 0xFF, 0x12, 0xAB, 0x00, 0xFF };

        TokenTextResult result = HexCodec.Encode(bytes.AsSpan(1, 3));

        Assert.That(result.Text, Is.EqualTo("12ab00"));
    }

    [Test]
    public void DecodeAcceptsUppercaseAndLowercaseHexadecimal()
    {
        TokenBytesResult lowercase = HexCodec.Decode("0001090a107f80abff");
        TokenBytesResult uppercase = HexCodec.Decode("0001090A107F80ABFF");
        byte[] expected = new byte[] { 0x00, 0x01, 0x09, 0x0A, 0x10, 0x7F, 0x80, 0xAB, 0xFF };

        Assert.Multiple(() =>
        {
            Assert.That(lowercase.Success, Is.True);
            Assert.That(lowercase.Error, Is.Null);
            Assert.That(lowercase.Bytes, Is.EqualTo(expected));
            Assert.That(uppercase.Bytes, Is.EqualTo(expected));
        });
    }

    [Test]
    public void DecodeMatchesBuiltInHexAcrossAllByteValues()
    {
        byte[] expected = Enumerable.Range(0, 256).Select(value => (byte)value).ToArray();
        string text = Convert.ToHexString(expected);

        TokenBytesResult result = HexCodec.Decode(text);

        Assert.That(result.Bytes, Is.EqualTo(expected));
    }

    [TestCase("0", 1)]
    [TestCase("abc", 3)]
    [TestCase("0x", 1)]
    [TestCase("0g", 1)]
    [TestCase("-1", 0)]
    [TestCase("00 1", 2)]
    public void DecodeRejectsOddLengthsAndInvalidCharacters(string text, int position)
    {
        TokenBytesResult result = HexCodec.Decode(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Bytes, Is.Null);
            Assert.That(result.Error.Code, Is.EqualTo("invalid_hex"));
            Assert.That(result.Error.Position, Is.EqualTo(position));
        });
    }

    [Test]
    public void DecodeSupportsCharacterSpanSlices()
    {
        string text = "!12aB00!";

        TokenBytesResult result = HexCodec.Decode(text.AsSpan(1, 6));

        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 0x12, 0xAB, 0x00 }));
    }

    [Test]
    public void EmptyInputsSucceed()
    {
        TokenTextResult encoded = HexCodec.Encode(Array.Empty<byte>());
        TokenBytesResult decoded = HexCodec.Decode(string.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Text, Is.Empty);
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Bytes, Is.Empty);
        });
    }

    [Test]
    public void ArrayAndStringOverloadsRejectNull()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => HexCodec.Encode((byte[])null), Throws.ArgumentNullException);
            Assert.That(() => HexCodec.Decode((string)null), Throws.ArgumentNullException);
        });
    }
}
