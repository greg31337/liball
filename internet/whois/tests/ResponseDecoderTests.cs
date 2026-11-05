using whois;

namespace whois.Tests;

public class ResponseDecoderTests
{
    [Test]
    public void ExplicitEncodingTakesPrecedenceOverByteOrderMarkDetection()
    {
        WhoisRawResponse raw = CreateRaw(new byte[] { 239, 187, 191, 233 });
        WhoisEncodingOptions options = new WhoisEncodingOptions();
        options.ResponseEncoding = System.Text.Encoding.Latin1;
        IWhoisResponseDecoder decoder = new WhoisResponseDecoder();

        WhoisTextResponse response = decoder.DecodeResponse(raw, options);

        Assert.That(response.RawResponse, Is.SameAs(raw));
        Assert.That(response.Text, Is.EqualTo("ï»¿é"));
        Assert.That(response.EncodingName, Is.EqualTo("iso-8859-1"));
        Assert.That(response.ByteOrderMarkDetected, Is.False);
        Assert.That(response.FallbackUsed, Is.False);
        Assert.That(response.ReplacementUsed, Is.False);
        Assert.That(response.IsDecoded, Is.True);
        Assert.That(response.Error, Is.Null);
    }

    [Test]
    public void DetectsAndRemovesUtf8ByteOrderMark()
    {
        WhoisRawResponse raw = CreateRaw(new byte[] { 239, 187, 191, 226, 130, 172 });
        IWhoisResponseDecoder decoder = new WhoisResponseDecoder();

        WhoisTextResponse response = decoder.DecodeResponse(raw, new WhoisEncodingOptions());

        Assert.That(response.Text, Is.EqualTo("€"));
        Assert.That(response.EncodingName, Is.EqualTo("utf-8"));
        Assert.That(response.ByteOrderMarkDetected, Is.True);
        Assert.That(response.IsDecoded, Is.True);
        Assert.That(response.Error, Is.Null);
    }

    [TestCase(false, "utf-16")]
    [TestCase(true, "utf-16BE")]
    public void DetectsBothUtf16ByteOrders(bool bigEndian, string expectedEncodingName)
    {
        System.Text.Encoding encoding = new System.Text.UnicodeEncoding(bigEndian, true, true);
        WhoisRawResponse raw = CreateRaw(EncodeWithPreamble(encoding, "domain: café\r\n"));
        IWhoisResponseDecoder decoder = new WhoisResponseDecoder();

        WhoisTextResponse response = decoder.DecodeResponse(raw, new WhoisEncodingOptions());

        Assert.That(response.Text, Is.EqualTo("domain: café\r\n"));
        Assert.That(response.EncodingName, Is.EqualTo(expectedEncodingName));
        Assert.That(response.ByteOrderMarkDetected, Is.True);
        Assert.That(response.FallbackUsed, Is.False);
        Assert.That(response.ReplacementUsed, Is.False);
        Assert.That(response.IsDecoded, Is.True);
        Assert.That(response.Error, Is.Null);
    }

    [Test]
    public void ExplicitBigEndianUnicodeDecodesWithoutAByteOrderMark()
    {
        System.Text.Encoding encoding = new System.Text.UnicodeEncoding(true, false, true);
        WhoisRawResponse raw = CreateRaw(encoding.GetBytes("registrar: Example"));
        WhoisEncodingOptions options = new WhoisEncodingOptions();
        options.ResponseEncoding = encoding;
        IWhoisResponseDecoder decoder = new WhoisResponseDecoder();

        WhoisTextResponse response = decoder.DecodeResponse(raw, options);

        Assert.That(response.Text, Is.EqualTo("registrar: Example"));
        Assert.That(response.EncodingName, Is.EqualTo("utf-16BE"));
        Assert.That(response.ByteOrderMarkDetected, Is.False);
        Assert.That(response.IsDecoded, Is.True);
        Assert.That(response.Error, Is.Null);
    }

    [Test]
    public void AsciiBytesDecodeAsStrictUtf8()
    {
        WhoisRawResponse raw = CreateRaw(System.Text.Encoding.ASCII.GetBytes("status: active\r\n"));
        IWhoisResponseDecoder decoder = new WhoisResponseDecoder();

        WhoisTextResponse response = decoder.DecodeResponse(raw, new WhoisEncodingOptions());

        Assert.That(response.Text, Is.EqualTo("status: active\r\n"));
        Assert.That(response.EncodingName, Is.EqualTo("utf-8"));
        Assert.That(response.FallbackUsed, Is.False);
        Assert.That(response.ReplacementUsed, Is.False);
        Assert.That(response.IsDecoded, Is.True);
    }

    [Test]
    public void DisabledByteOrderMarkDetectionPreservesUtf8BomCharacter()
    {
        WhoisRawResponse raw = CreateRaw(new byte[] { 239, 187, 191, 120 });
        WhoisEncodingOptions options = new WhoisEncodingOptions();
        options.DetectByteOrderMark = false;
        IWhoisResponseDecoder decoder = new WhoisResponseDecoder();

        WhoisTextResponse response = decoder.DecodeResponse(raw, options);

        Assert.That(response.Text, Is.EqualTo("\uFEFFx"));
        Assert.That(response.EncodingName, Is.EqualTo("utf-8"));
        Assert.That(response.ByteOrderMarkDetected, Is.False);
        Assert.That(response.IsDecoded, Is.True);
    }

    [Test]
    public void ValidUtf8UsesStrictAutomaticDecodingWithoutFallbackMetadata()
    {
        WhoisRawResponse raw = CreateRaw(System.Text.Encoding.UTF8.GetBytes("café\r\n"));
        IWhoisResponseDecoder decoder = new WhoisResponseDecoder();

        WhoisTextResponse response = decoder.DecodeResponse(raw, new WhoisEncodingOptions());

        Assert.That(response.Text, Is.EqualTo("café\r\n"));
        Assert.That(response.EncodingName, Is.EqualTo("utf-8"));
        Assert.That(response.ByteOrderMarkDetected, Is.False);
        Assert.That(response.FallbackUsed, Is.False);
        Assert.That(response.ReplacementUsed, Is.False);
        Assert.That(response.IsDecoded, Is.True);
        Assert.That(response.Error, Is.Null);
    }

    [Test]
    public void StrictModeReturnsDecoderFailureWithRawContext()
    {
        WhoisRawResponse raw = CreateRaw(new byte[] { 195, 40 });
        IWhoisResponseDecoder decoder = new WhoisResponseDecoder();

        WhoisTextResponse response = decoder.DecodeResponse(raw, new WhoisEncodingOptions());

        Assert.That(response.RawResponse, Is.SameAs(raw));
        Assert.That(response.Text, Is.Empty);
        Assert.That(response.EncodingName, Is.EqualTo("utf-8"));
        Assert.That(response.IsDecoded, Is.False);
        Assert.That(response.FallbackUsed, Is.False);
        Assert.That(response.ReplacementUsed, Is.False);
        Assert.That(response.Error, Is.TypeOf<System.Text.DecoderFallbackException>());
    }

    [Test]
    public void StrictModeRejectsRepresentativeMalformedUtf8Forms()
    {
        byte[][] malformedBodies = new byte[][]
        {
            new byte[] { 128 },
            new byte[] { 192, 175 },
            new byte[] { 237, 160, 128 },
            new byte[] { 240, 159, 146 }
        };
        IWhoisResponseDecoder decoder = new WhoisResponseDecoder();
        int index = 0;
        while (index < malformedBodies.Length)
        {
            WhoisRawResponse raw = CreateRaw(malformedBodies[index]);

            WhoisTextResponse response = decoder.DecodeResponse(raw, new WhoisEncodingOptions());

            Assert.That(response.RawResponse, Is.SameAs(raw));
            Assert.That(response.IsDecoded, Is.False);
            Assert.That(response.Error, Is.TypeOf<System.Text.DecoderFallbackException>());
            index = index + 1;
        }
    }

    [Test]
    public void ReplacementModeMarksInvalidUtf8Replacement()
    {
        WhoisRawResponse raw = CreateRaw(new byte[] { 195, 40 });
        WhoisEncodingOptions options = new WhoisEncodingOptions();
        options.DecodingMode = WhoisDecodingMode.ReplaceInvalidUtf8;
        IWhoisResponseDecoder decoder = new WhoisResponseDecoder();

        WhoisTextResponse response = decoder.DecodeResponse(raw, options);

        Assert.That(response.Text, Is.EqualTo("�("));
        Assert.That(response.EncodingName, Is.EqualTo("utf-8"));
        Assert.That(response.ReplacementUsed, Is.True);
        Assert.That(response.FallbackUsed, Is.False);
        Assert.That(response.IsDecoded, Is.True);
        Assert.That(response.Error, Is.Null);
    }

    [Test]
    public void Latin1ModePreservesEveryInvalidUtf8ByteAsACharacter()
    {
        WhoisRawResponse raw = CreateRaw(new byte[] { 255, 0, 128 });
        WhoisEncodingOptions options = new WhoisEncodingOptions();
        options.DecodingMode = WhoisDecodingMode.Latin1Fallback;
        IWhoisResponseDecoder decoder = new WhoisResponseDecoder();

        WhoisTextResponse response = decoder.DecodeResponse(raw, options);

        Assert.That(response.Text, Is.EqualTo("ÿ\0"));
        Assert.That(response.EncodingName, Is.EqualTo("iso-8859-1"));
        Assert.That(response.FallbackUsed, Is.True);
        Assert.That(response.ReplacementUsed, Is.False);
        Assert.That(response.IsDecoded, Is.True);
        Assert.That(response.Error, Is.Null);
    }

    [Test]
    public void EmptyBodyProducesSuccessfulEmptyUtf8View()
    {
        WhoisRawResponse raw = CreateRaw(Array.Empty<byte>());
        IWhoisResponseDecoder decoder = new WhoisResponseDecoder();

        WhoisTextResponse response = decoder.DecodeResponse(raw, new WhoisEncodingOptions());

        Assert.That(response.RawResponse, Is.SameAs(raw));
        Assert.That(response.Text, Is.Empty);
        Assert.That(response.EncodingName, Is.EqualTo("utf-8"));
        Assert.That(response.ByteOrderMarkDetected, Is.False);
        Assert.That(response.IsDecoded, Is.True);
        Assert.That(response.Error, Is.Null);
    }

    [Test]
    public void EmbeddedNulIsPreservedInSuccessfulUtf8Text()
    {
        WhoisRawResponse raw = CreateRaw(new byte[] { 97, 0, 98 });
        IWhoisResponseDecoder decoder = new WhoisResponseDecoder();

        WhoisTextResponse response = decoder.DecodeResponse(raw, new WhoisEncodingOptions());

        Assert.That(response.Text, Is.EqualTo("a\0b"));
        Assert.That(response.Text.Length, Is.EqualTo(3));
        Assert.That(response.IsDecoded, Is.True);
        Assert.That(response.Error, Is.Null);
    }

    private static WhoisRawResponse CreateRaw(byte[] body)
    {
        WhoisRawResponse raw = new WhoisRawResponse();
        raw.Body = body;
        return raw;
    }

    private static byte[] EncodeWithPreamble(System.Text.Encoding encoding, string text)
    {
        byte[] preamble = encoding.GetPreamble();
        byte[] content = encoding.GetBytes(text);
        byte[] bytes = new byte[preamble.Length + content.Length];
        Buffer.BlockCopy(preamble, 0, bytes, 0, preamble.Length);
        Buffer.BlockCopy(content, 0, bytes, preamble.Length, content.Length);
        return bytes;
    }
}
