using token;

namespace token.Tests;

public class TokenEncodingServiceTests
{
    [TestCase("", "", "")]
    [TestCase("f", "Zg", "Zg==")]
    [TestCase("fo", "Zm8", "Zm8=")]
    [TestCase("foo", "Zm9v", "Zm9v")]
    [TestCase("foob", "Zm9vYg", "Zm9vYg==")]
    [TestCase("fooba", "Zm9vYmE", "Zm9vYmE=")]
    [TestCase("foobar", "Zm9vYmFy", "Zm9vYmFy")]
    public void PublicBase64CodecsMatchRfc4648BoundaryVectors(
        string plainText,
        string expectedBase64Url,
        string expectedBase64)
    {
        ITokenEncodingService service = new TokenEncodingService();
        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(plainText);

        TokenTextResult base64Url = service.EncodeBase64Url(bytes);
        TokenBytesResult decodedBase64Url = service.DecodeBase64Url(expectedBase64Url);
        TokenTextResult base64 = service.EncodeBase64(bytes);
        TokenBytesResult decodedBase64 = service.DecodeBase64(expectedBase64);

        Assert.Multiple(() =>
        {
            Assert.That(base64Url.Text, Is.EqualTo(expectedBase64Url));
            Assert.That(decodedBase64Url.Bytes, Is.EqualTo(bytes));
            Assert.That(base64.Text, Is.EqualTo(expectedBase64));
            Assert.That(decodedBase64.Bytes, Is.EqualTo(bytes));
        });
    }

    [Test]
    public void StrictUtf8RoundTripsSpanSlicesAndRejectsMalformedInput()
    {
        ITokenEncodingService service = new TokenEncodingService();
        string source = "xA\u00A2\u20AC\U00010348y";

        TokenBytesResult encoded = service.EncodeUtf8Span(
            source.AsSpan(1, source.Length - 2));
        TokenTextResult decoded = service.DecodeUtf8Span(encoded.Bytes);
        TokenTextResult malformed = service.DecodeUtf8(
            new byte[] { 0xE0, 0x80, 0x80 });

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Text, Is.EqualTo("A\u00A2\u20AC\U00010348"));
            Assert.That(malformed.Success, Is.False);
            Assert.That(malformed.Error.Code, Is.EqualTo("invalid_utf8"));
            Assert.That(malformed.Error.Position, Is.EqualTo(1));
        });
    }

    [Test]
    public void Base64UrlSeparatesStrictAndCompatibilityModes()
    {
        ITokenEncodingService service = new TokenEncodingService();
        byte[] source = new byte[] { 0, 102, 111, 111, 0 };

        TokenTextResult encoded = service.EncodeBase64UrlSpan(
            source.AsSpan(1, 3));
        TokenBytesResult decoded = service.DecodeBase64UrlSpan(
            "!Zm9v!".AsSpan(1, 4));
        TokenBytesResult strictPadding = service.DecodeBase64Url("Zg==");
        TokenBytesResult compatiblePadding =
            service.DecodeCompatibleBase64UrlSpan("!Zg==!".AsSpan(1, 4));

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Text, Is.EqualTo("Zm9v"));
            Assert.That(decoded.Bytes, Is.EqualTo(new byte[] { 102, 111, 111 }));
            Assert.That(strictPadding.Success, Is.False);
            Assert.That(strictPadding.Error.Code,
                Is.EqualTo("invalid_base64url"));
            Assert.That(compatiblePadding.Success, Is.True);
            Assert.That(compatiblePadding.Bytes, Is.EqualTo(new byte[] { 102 }));
        });
    }

    [Test]
    public void StandardBase64AndHexRoundTripThroughSpanPaths()
    {
        ITokenEncodingService service = new TokenEncodingService();
        byte[] source = new byte[] { 0, 0xFB, 0xFF, 0x00, 0 };

        TokenTextResult base64 = service.EncodeBase64Span(source.AsSpan(1, 3));
        TokenBytesResult strictWhitespace = service.DecodeBase64("+/8A\n");
        TokenBytesResult compatibleWhitespace =
            service.DecodeWhitespaceBase64("+ /8A\r\n");
        TokenTextResult hex = service.EncodeHexSpan(source.AsSpan(1, 3));
        TokenBytesResult decodedHex = service.DecodeHexSpan("!FBff00!".AsSpan(1, 6));

        Assert.Multiple(() =>
        {
            Assert.That(base64.Text, Is.EqualTo("+/8A"));
            Assert.That(strictWhitespace.Success, Is.False);
            Assert.That(strictWhitespace.Error.Code, Is.EqualTo("invalid_base64"));
            Assert.That(compatibleWhitespace.Bytes,
                Is.EqualTo(new byte[] { 0xFB, 0xFF, 0x00 }));
            Assert.That(hex.Text, Is.EqualTo("fbff00"));
            Assert.That(decodedHex.Bytes,
                Is.EqualTo(new byte[] { 0xFB, 0xFF, 0x00 }));
        });
    }

    [Test]
    public void PemBlockRoundTripsCanonicalSpanOutput()
    {
        ITokenEncodingService service = new TokenEncodingService();
        byte[] source = Enumerable.Range(0, 66).Select(value => (byte)value).ToArray();

        TokenTextResult written = service.WritePemBlockSpan(
            "TEST DATA",
            source.AsSpan(1, 64));
        PemReadResult read = service.ReadPemBlock(
            " \r\n" + written.Text + "\t");

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True);
            Assert.That(written.Text, Does.StartWith("-----BEGIN TEST DATA-----\n"));
            Assert.That(written.Text, Does.EndWith("-----END TEST DATA-----\n"));
            Assert.That(read.Success, Is.True);
            Assert.That(read.Block.Label, Is.EqualTo("TEST DATA"));
            Assert.That(read.Block.Bytes, Is.EqualTo(source.AsSpan(1, 64).ToArray()));
        });
    }

    [Test]
    public void CompactScannerExposesBoundariesAndEnforcesMaximumLength()
    {
        ITokenEncodingService service = new TokenEncodingService();
        string compact = "YQ.Yg.";

        JoseCompactScanResult exact = service.ScanCompactToken(
            compact,
            compact.Length);
        JoseCompactScanResult limited = service.ScanCompactToken(
            compact,
            compact.Length - 1);
        JoseCompactScanResult malformed = service.ScanCompactToken(
            "YQ.*.",
            5);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Success, Is.True);
            Assert.That(exact.Segments.Text, Is.EqualTo(compact));
            Assert.That(exact.Segments.SegmentCount, Is.EqualTo(3));
            Assert.That(exact.Segments.SegmentStarts.Take(3),
                Is.EqualTo(new int[] { 0, 3, 6 }));
            Assert.That(exact.Segments.SegmentLengths.Take(3),
                Is.EqualTo(new int[] { 2, 2, 0 }));
            Assert.That(limited.Success, Is.False);
            Assert.That(limited.Error.Code, Is.EqualTo("buffer_limit_exceeded"));
            Assert.That(limited.Error.Position, Is.EqualTo(5));
            Assert.That(malformed.Success, Is.False);
            Assert.That(malformed.Error.Code, Is.EqualTo("invalid_compact_token"));
            Assert.That(malformed.Error.Position, Is.EqualTo(3));
        });
    }

    [Test]
    public void PublicBuffersSupportSpanIoAndAtomicLimitFailures()
    {
        ITokenBufferReader reader = new TokenBufferReader(
            new byte[] { 10, 20, 30 },
            3);
        byte[] destination = new byte[2];
        TokenResult read = reader.ReadSpan(destination.AsSpan());
        byte[] unchanged = new byte[] { 9, 9 };
        TokenResult failedRead = reader.ReadSpan(unchanged.AsSpan());

        ITokenBufferWriter writer = new TokenBufferWriter(3);
        byte[] source = new byte[] { 0, 1, 2, 3, 4 };
        TokenResult written = writer.WriteSpan(source.AsSpan(1, 2));
        TokenResult failedWrite = writer.WriteSpan(source.AsSpan(3, 2));
        TokenResult recovered = writer.Write((byte)3);

        Assert.Multiple(() =>
        {
            Assert.That(read.Success, Is.True);
            Assert.That(destination, Is.EqualTo(new byte[] { 10, 20 }));
            Assert.That(failedRead.Success, Is.False);
            Assert.That(failedRead.Error.Code,
                Is.EqualTo("unexpected_end_of_buffer"));
            Assert.That(unchanged, Is.EqualTo(new byte[] { 9, 9 }));
            Assert.That(reader.GetPosition(), Is.EqualTo(2));
            Assert.That(written.Success, Is.True);
            Assert.That(failedWrite.Success, Is.False);
            Assert.That(recovered.Success, Is.True);
            Assert.That(writer.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void ReferenceInputsAndNegativeLimitsAreRejected()
    {
        ITokenEncodingService service = new TokenEncodingService();

        Assert.Multiple(() =>
        {
            Assert.That(() => service.EncodeUtf8(null), Throws.ArgumentNullException);
            Assert.That(() => service.DecodeUtf8(null), Throws.ArgumentNullException);
            Assert.That(() => service.EncodeBase64Url(null), Throws.ArgumentNullException);
            Assert.That(() => service.DecodeBase64Url(null), Throws.ArgumentNullException);
            Assert.That(() => service.EncodeBase64(null), Throws.ArgumentNullException);
            Assert.That(() => service.DecodeBase64(null), Throws.ArgumentNullException);
            Assert.That(() => service.EncodeHex(null), Throws.ArgumentNullException);
            Assert.That(() => service.DecodeHex(null), Throws.ArgumentNullException);
            Assert.That(() => service.WritePemBlock(null, Array.Empty<byte>()),
                Throws.ArgumentNullException);
            Assert.That(() => service.ReadPemBlock(null), Throws.ArgumentNullException);
            Assert.That(() => service.ScanCompactToken(null, 0),
                Throws.ArgumentNullException);
            Assert.That(() => service.ScanCompactToken(string.Empty, -1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    [Test]
    public void RemainingCompatibilityAndSpanDecodersArePublic()
    {
        ITokenEncodingService service =
            new TokenEncodingService();

        TokenBytesResult base64 = service.DecodeBase64Span(
            "AQI=".AsSpan());
        TokenBytesResult compatible =
            service.DecodeCompatibleBase64Url("AQI=");
        TokenBytesResult whitespace =
            service.DecodeWhitespaceBase64Span(
                " A Q I = \n".AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(base64.Success, Is.True);
            Assert.That(base64.Bytes,
                Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(compatible.Success, Is.True);
            Assert.That(compatible.Bytes,
                Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(whitespace.Success, Is.True);
            Assert.That(whitespace.Bytes,
                Is.EqualTo(new byte[] { 1, 2 }));
        });
    }

    [Test]
    public void PublicDecodersRejectNonCanonicalAndMalformedRepresentations()
    {
        ITokenEncodingService service = new TokenEncodingService();

        TokenBytesResult base64UrlLength = service.DecodeBase64Url("A");
        TokenBytesResult base64UrlBits = service.DecodeCompatibleBase64Url("Zh==");
        TokenBytesResult base64Bits = service.DecodeBase64("Zm9=");
        TokenBytesResult hexLength = service.DecodeHex("abc");
        TokenBytesResult hexCharacter = service.DecodeHex("0g");
        PemReadResult pemLabels = service.ReadPemBlock(
            "-----BEGIN DATA-----\nZg==\n-----END OTHER-----\n");
        TokenBytesResult invalidUtf16 = service.EncodeUtf8("\uD800");

        Assert.Multiple(() =>
        {
            Assert.That(base64UrlLength.Error!.Code, Is.EqualTo("invalid_base64url"));
            Assert.That(base64UrlLength.Error.Position, Is.EqualTo(1));
            Assert.That(base64UrlBits.Error!.Code, Is.EqualTo("invalid_base64url"));
            Assert.That(base64UrlBits.Error.Position, Is.EqualTo(1));
            Assert.That(base64Bits.Error!.Code, Is.EqualTo("invalid_base64"));
            Assert.That(base64Bits.Error.Position, Is.EqualTo(2));
            Assert.That(hexLength.Error!.Code, Is.EqualTo("invalid_hex"));
            Assert.That(hexLength.Error.Position, Is.EqualTo(3));
            Assert.That(hexCharacter.Error!.Code, Is.EqualTo("invalid_hex"));
            Assert.That(hexCharacter.Error.Position, Is.EqualTo(1));
            Assert.That(pemLabels.Error!.Code, Is.EqualTo("invalid_pem"));
            Assert.That(invalidUtf16.Error!.Code, Is.EqualTo("invalid_utf16"));
        });
    }
}
