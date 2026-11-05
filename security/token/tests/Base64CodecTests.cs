using token;

namespace token.Tests;

public class Base64CodecTests
{
    [TestCase("", "")]
    [TestCase("f", "Zg==")]
    [TestCase("fo", "Zm8=")]
    [TestCase("foo", "Zm9v")]
    [TestCase("foob", "Zm9vYg==")]
    [TestCase("fooba", "Zm9vYmE=")]
    [TestCase("foobar", "Zm9vYmFy")]
    public void EncodeMatchesRfc4648Vectors(string input, string expected)
    {
        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(input);

        TokenTextResult result = Base64Codec.Encode(bytes);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Text, Is.EqualTo(expected));
        });
    }

    [Test]
    public void EncodeUsesStandardAlphabetAndPadding()
    {
        TokenTextResult result = Base64Codec.Encode(new byte[] { 0xFB, 0xFF, 0xFF, 0xFA });

        Assert.That(result.Text, Is.EqualTo("+///+g=="));
    }

    [Test]
    public void EncodeSupportsByteSpanSlices()
    {
        byte[] bytes = new byte[] { 0xFF, 102, 111, 111, 0xFF };

        TokenTextResult result = Base64Codec.Encode(bytes.AsSpan(1, 3));

        Assert.That(result.Text, Is.EqualTo("Zm9v"));
    }

    [Test]
    public void EncodeArrayOverloadRejectsNull()
    {
        Assert.That(() => Base64Codec.Encode((byte[])null), Throws.ArgumentNullException);
    }

    [Test]
    public void EncodeAndDecodeMatchBuiltInBase64AcrossBinaryLengths()
    {
        for (int length = 0; length <= 128; length++)
        {
            byte[] bytes = new byte[length];
            for (int index = 0; index < bytes.Length; index++)
            {
                bytes[index] = (byte)((index * 73 + length * 29) & 255);
            }

            string expected = Convert.ToBase64String(bytes);
            TokenTextResult encoded = Base64Codec.Encode(bytes);
            TokenBytesResult decoded = Base64Codec.Decode(expected);

            Assert.That(encoded.Text, Is.EqualTo(expected), $"Encoding length {length}");
            Assert.That(decoded.Bytes, Is.EqualTo(bytes), $"Decoding length {length}");
        }
    }

    [TestCase("", "")]
    [TestCase("Zg==", "f")]
    [TestCase("Zm8=", "fo")]
    [TestCase("Zm9v", "foo")]
    [TestCase("Zm9vYg==", "foob")]
    [TestCase("Zm9vYmE=", "fooba")]
    [TestCase("Zm9vYmFy", "foobar")]
    public void DecodeMatchesRfc4648Vectors(string input, string expected)
    {
        TokenBytesResult result = Base64Codec.Decode(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(System.Text.Encoding.ASCII.GetString(result.Bytes), Is.EqualTo(expected));
        });
    }

    [Test]
    public void DecodeReadsStandardAlphabetAndEmbeddedZeroBytes()
    {
        TokenBytesResult alphabet = Base64Codec.Decode("+///+g==");
        TokenBytesResult zero = Base64Codec.Decode("ZgA=");

        Assert.Multiple(() =>
        {
            Assert.That(alphabet.Bytes, Is.EqualTo(new byte[] { 0xFB, 0xFF, 0xFF, 0xFA }));
            Assert.That(zero.Bytes, Is.EqualTo(new byte[] { 102, 0 }));
        });
    }

    [Test]
    public void PemModeIgnoresAsciiWhitespaceAndPreservesPositionMapping()
    {
        TokenBytesResult valid = Base64Codec.Decode(" Zm9v\r\nYmFy\t", true);
        TokenBytesResult invalid = Base64Codec.Decode(" Zm9v\r\nYm-y\t", true);

        Assert.Multiple(() =>
        {
            Assert.That(System.Text.Encoding.ASCII.GetString(valid.Bytes), Is.EqualTo("foobar"));
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Error.Code, Is.EqualTo("invalid_base64"));
            Assert.That(invalid.Error.Position, Is.EqualTo(9));
        });
    }

    [TestCase("Zg==\n", 4)]
    [TestCase("Z g==", 1)]
    public void StrictModeRejectsWhitespace(string input, int position)
    {
        AssertInvalidBase64(Base64Codec.Decode(input), position);
    }

    [TestCase("Zg", 2)]
    [TestCase("Zg=", 3)]
    [TestCase("Zg===", 2)]
    [TestCase("=m9v", 0)]
    [TestCase("Zm=v", 2)]
    [TestCase("Zm-v", 2)]
    [TestCase("Zm_v", 2)]
    [TestCase("Zm.v", 2)]
    public void DecodeRejectsMalformedLengthPaddingAndAlphabet(string input, int position)
    {
        AssertInvalidBase64(Base64Codec.Decode(input), position);
    }

    [TestCase("Zh==", 1)]
    [TestCase("Zm9=", 2)]
    public void DecodeRejectsNonCanonicalUnusedBits(string input, int position)
    {
        AssertInvalidBase64(Base64Codec.Decode(input), position);
    }

    [Test]
    public void DecodeSupportsCharacterSpanSlicesAndWhitespaceMode()
    {
        string strictText = "!Zm9v!";
        string pemText = "!Zg==\n!";

        TokenBytesResult strict = Base64Codec.Decode(strictText.AsSpan(1, 4));
        TokenBytesResult pem = Base64Codec.Decode(pemText.AsSpan(1, 5), true);

        Assert.Multiple(() =>
        {
            Assert.That(strict.Bytes, Is.EqualTo(new byte[] { 102, 111, 111 }));
            Assert.That(pem.Bytes, Is.EqualTo(new byte[] { 102 }));
        });
    }

    [Test]
    public void WhitespaceModeRejectsNonPemUnicodeWhitespace()
    {
        TokenBytesResult result = Base64Codec.Decode("Zg\u00A0=", true);

        AssertInvalidBase64(result, 2);
    }

    [Test]
    public void DecodeStringOverloadsRejectNull()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => Base64Codec.Decode((string)null), Throws.ArgumentNullException);
            Assert.That(() => Base64Codec.Decode((string)null, true), Throws.ArgumentNullException);
        });
    }

    static void AssertInvalidBase64(TokenBytesResult result, int position)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Bytes, Is.Null);
            Assert.That(result.Error.Code, Is.EqualTo("invalid_base64"));
            Assert.That(result.Error.Position, Is.EqualTo(position));
        });
    }
}
