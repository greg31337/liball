using token;

namespace token.Tests;

public class Base64UrlTests
{
    [TestCase("", "")]
    [TestCase("f", "Zg")]
    [TestCase("fo", "Zm8")]
    [TestCase("foo", "Zm9v")]
    [TestCase("foob", "Zm9vYg")]
    [TestCase("fooba", "Zm9vYmE")]
    [TestCase("foobar", "Zm9vYmFy")]
    public void EncodeMatchesRfc4648Vectors(string input, string expected)
    {
        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(input);

        TokenTextResult result = Base64Url.Encode(bytes);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Text, Is.EqualTo(expected));
        });
    }

    [Test]
    public void EncodeUsesUrlSafeAlphabetWithoutPadding()
    {
        TokenTextResult result = Base64Url.Encode(new byte[] { 0xFB, 0xFF, 0xFF, 0xFA });

        Assert.Multiple(() =>
        {
            Assert.That(result.Text, Is.EqualTo("-___-g"));
            Assert.That(result.Text, Does.Not.Contain("+"));
            Assert.That(result.Text, Does.Not.Contain("/"));
            Assert.That(result.Text, Does.Not.Contain("="));
        });
    }

    [Test]
    public void EncodeSupportsByteSpanSlices()
    {
        byte[] bytes = new byte[] { 0xFF, 102, 111, 111, 0xFF };

        TokenTextResult result = Base64Url.Encode(bytes.AsSpan(1, 3));

        Assert.That(result.Text, Is.EqualTo("Zm9v"));
    }

    [Test]
    public void EncodeDoesNotDependOnCallerArrayAfterReturning()
    {
        byte[] bytes = new byte[] { 1, 2, 3 };

        TokenTextResult result = Base64Url.Encode(bytes);
        bytes[0] = 9;

        Assert.That(result.Text, Is.EqualTo("AQID"));
    }

    [Test]
    public void EncodeArrayOverloadRejectsNull()
    {
        Assert.That(() => Base64Url.Encode((byte[])null), Throws.ArgumentNullException);
    }

    [TestCase("", "")]
    [TestCase("Zg", "f")]
    [TestCase("Zm8", "fo")]
    [TestCase("Zm9v", "foo")]
    [TestCase("Zm9vYg", "foob")]
    [TestCase("Zm9vYmE", "fooba")]
    [TestCase("Zm9vYmFy", "foobar")]
    public void DecodeMatchesUnpaddedRfc4648Vectors(string input, string expected)
    {
        TokenBytesResult result = Base64Url.Decode(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(System.Text.Encoding.ASCII.GetString(result.Bytes), Is.EqualTo(expected));
        });
    }

    [Test]
    public void DecodeReadsUrlSafeAlphabet()
    {
        TokenBytesResult result = Base64Url.Decode("-___-g");

        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 0xFB, 0xFF, 0xFF, 0xFA }));
    }

    [TestCase("Zg==", new byte[] { 102 })]
    [TestCase("Zm8=", new byte[] { 102, 111 })]
    [TestCase("ZgA=", new byte[] { 102, 0 })]
    [TestCase("Zm9v", new byte[] { 102, 111, 111 })]
    public void CompatibilityModeAcceptsOptionalValidPadding(string input, byte[] expected)
    {
        TokenBytesResult result = Base64Url.Decode(input, true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Bytes, Is.EqualTo(expected));
        });
    }

    [TestCase("Zg==", 2)]
    [TestCase("Zm8=", 3)]
    public void StrictModeRejectsPadding(string input, int position)
    {
        TokenBytesResult result = Base64Url.Decode(input);

        AssertInvalidBase64Url(result, position);
    }

    [TestCase("A", 1)]
    [TestCase("Zm+v", 2)]
    [TestCase("Zm/v", 2)]
    [TestCase("Zm v", 2)]
    [TestCase("Zm.v", 2)]
    [TestCase("Zm=v", 2)]
    [TestCase("Zg=", 3)]
    [TestCase("Zg===", 2)]
    [TestCase("ZgA==", 5)]
    public void DecodeRejectsInvalidLengthsCharactersAndPadding(string input, int position)
    {
        TokenBytesResult result = Base64Url.Decode(input, true);

        AssertInvalidBase64Url(result, position);
    }

    [TestCase("Zh", 1)]
    [TestCase("Zm9", 2)]
    [TestCase("Zh==", 1)]
    [TestCase("Zm9=", 2)]
    public void DecodeRejectsNonCanonicalUnusedBits(string input, int position)
    {
        TokenBytesResult result = Base64Url.Decode(input, true);

        AssertInvalidBase64Url(result, position);
    }

    [Test]
    public void DecodeSupportsCharacterSpanSlicesAndExplicitModes()
    {
        string unpadded = "!Zm9v!";
        string padded = "!Zg==!";

        TokenBytesResult strict = Base64Url.Decode(unpadded.AsSpan(1, 4));
        TokenBytesResult compatible = Base64Url.Decode(padded.AsSpan(1, 4), true);

        Assert.Multiple(() =>
        {
            Assert.That(strict.Bytes, Is.EqualTo(new byte[] { 102, 111, 111 }));
            Assert.That(compatible.Bytes, Is.EqualTo(new byte[] { 102 }));
        });
    }

    [Test]
    public void DecodeStringOverloadsRejectNull()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => Base64Url.Decode((string)null), Throws.ArgumentNullException);
            Assert.That(() => Base64Url.Decode((string)null, true), Throws.ArgumentNullException);
        });
    }

    static void AssertInvalidBase64Url(TokenBytesResult result, int position)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Bytes, Is.Null);
            Assert.That(result.Error.Code, Is.EqualTo("invalid_base64url"));
            Assert.That(result.Error.Position, Is.EqualTo(position));
        });
    }
}
