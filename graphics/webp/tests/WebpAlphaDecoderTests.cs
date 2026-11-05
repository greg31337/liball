using webp;

namespace webp.Tests;

[TestFixture]
public class WebpAlphaDecoderTests
{
    static byte[] ObservedLosslessStream = Array.Empty<byte>();
    static int ObservedLosslessWidth;
    static int ObservedLosslessHeight;
    static byte[] LosslessGreenValues = Array.Empty<byte>();

    [Test]
    public void ParseHeader_ExtractsCompressionFilterPreprocessingAndReservedBits()
    {
        WebpAlphaHeader header = WebpAlphaDecoder.ParseHeader(0xE5);

        Assert.That(header.RawHeader, Is.EqualTo(0xE5));
        Assert.That(header.CompressionMethod, Is.EqualTo(1));
        Assert.That(header.FilterMethod, Is.EqualTo(1));
        Assert.That(header.Preprocessing, Is.EqualTo(2));
        Assert.That(header.ReservedBits, Is.EqualTo(0xC0));
        Assert.That(header.HasReservedBits, Is.True);
    }

    [Test]
    public void Decode_UncompressedAlphaCopiesRawScanlineValues()
    {
        byte[] payload = new byte[] { 0, 11, 22, 33, 44, 55, 66 };

        WebpAlphaDecodeResult result = WebpAlphaDecoder.Decode(payload, 3, 2, WebpStrictness.Strict);
        payload[1] = 99;

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Header.CompressionMethod, Is.EqualTo(WebpConstants.AlphaCompressionNone));
        Assert.That(result.Header.FilterMethod, Is.EqualTo(0));
        Assert.That(result.Alpha, Is.EqualTo(new byte[] { 11, 22, 33, 44, 55, 66 }));
    }

    [Test]
    public void Decode_PermissiveReservedBitsWarnsAndStillDecodes()
    {
        byte[] payload = new byte[] { 0x80, 5 };

        WebpAlphaDecodeResult result = WebpAlphaDecoder.Decode(payload, 1, 1, WebpStrictness.Permissive);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Alpha, Is.EqualTo(new byte[] { 5 }));
        Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(WebpDiagnosticSeverity.Warning));
        Assert.That(result.Diagnostics[0].Message, Does.Contain("reserved"));
    }

    [Test]
    public void Decode_StrictReservedBitsRejectsPayload()
    {
        byte[] payload = new byte[] { 0x40, 5 };

        WebpAlphaDecodeResult result = WebpAlphaDecoder.Decode(payload, 1, 1, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Alpha, Is.Empty);
        Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(WebpDiagnosticSeverity.Error));
    }

    [Test]
    public void Decode_RejectsMissingHeader()
    {
        WebpAlphaDecodeResult result = WebpAlphaDecoder.Decode(Array.Empty<byte>(), 1, 1, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics[0].Message, Does.Contain("header"));
    }

    [Test]
    public void Decode_RejectsUncompressedPayloadLengthMismatch()
    {
        WebpAlphaDecodeResult tooShort = WebpAlphaDecoder.Decode(new byte[] { 0, 1, 2, 3 }, 2, 2, WebpStrictness.Strict);
        WebpAlphaDecodeResult tooLong = WebpAlphaDecoder.Decode(new byte[] { 0, 1, 2, 3, 4, 5 }, 2, 2, WebpStrictness.Strict);

        Assert.That(tooShort.IsValid, Is.False);
        Assert.That(tooLong.IsValid, Is.False);
        Assert.That(tooShort.Diagnostics[0].Message, Does.Contain("width * height + 1"));
        Assert.That(tooLong.Diagnostics[0].Message, Does.Contain("width * height + 1"));
    }

    [Test]
    public void Decode_RejectsInvalidCompressionMethod()
    {
        WebpAlphaDecodeResult result = WebpAlphaDecoder.Decode(new byte[] { 0x02, 1 }, 1, 1, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Header.CompressionMethod, Is.EqualTo(2));
        Assert.That(result.Diagnostics[0].Message, Does.Contain("compression"));
    }

    [Test]
    public void Decode_RejectsCompressedPayloadUntilLosslessAlphaDecodeExists()
    {
        WebpAlphaDecodeResult result = WebpAlphaDecoder.Decode(new byte[] { WebpConstants.AlphaCompressionLossless, 1 }, 1, 1, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Header.CompressionMethod, Is.EqualTo(WebpConstants.AlphaCompressionLossless));
        Assert.That(result.Diagnostics[0].Message, Does.Contain("compressed"));
    }

    [Test]
    public void Decode_CompressedAlphaUsesLosslessStreamDecoderWithImplicitDimensionsAndGreenChannel()
    {
        ObservedLosslessStream = Array.Empty<byte>();
        ObservedLosslessWidth = 0;
        ObservedLosslessHeight = 0;
        LosslessGreenValues = new byte[] { 10, 5, 10, 20 };
        byte header = (byte)(WebpConstants.AlphaCompressionLossless | (WebpConstants.AlphaFilterHorizontal << WebpConstants.AlphaFilterShift));
        byte[] payload = new byte[] { header, 0xAA, 0xBB, 0xCC };

        WebpAlphaDecodeResult result = WebpAlphaDecoder.Decode(payload, 2, 2, WebpStrictness.Strict, DecodeLosslessForTest);
        payload[1] = 0;

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Header.CompressionMethod, Is.EqualTo(WebpConstants.AlphaCompressionLossless));
        Assert.That(result.Header.FilterMethod, Is.EqualTo(WebpConstants.AlphaFilterHorizontal));
        Assert.That(ObservedLosslessStream, Is.EqualTo(new byte[] { 0xAA, 0xBB, 0xCC }));
        Assert.That(ObservedLosslessWidth, Is.EqualTo(2));
        Assert.That(ObservedLosslessHeight, Is.EqualTo(2));
        Assert.That(result.Alpha, Is.EqualTo(new byte[] { 10, 15, 20, 40 }));
    }

    [Test]
    public void Decode_CompressedAlphaRejectsMissingLosslessImageStream()
    {
        WebpAlphaDecodeResult result = WebpAlphaDecoder.Decode(new byte[] { WebpConstants.AlphaCompressionLossless }, 1, 1, WebpStrictness.Strict, DecodeLosslessForTest);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics[0].Message, Does.Contain("lossless image stream"));
    }

    [Test]
    public void Decode_CompressedAlphaRejectsMismatchedLosslessDimensions()
    {
        Assert.Throws<ArgumentException>(() => WebpAlphaDecoder.Decode(new byte[] { WebpConstants.AlphaCompressionLossless, 1 }, 2, 2, WebpStrictness.Strict, DecodeWrongSizeForTest));
    }

    [Test]
    public void Decode_AppliesAlphaFilter()
    {
        WebpAlphaDecodeResult result = WebpAlphaDecoder.Decode(new byte[] { 0x04, 10, 5, 5 }, 3, 1, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Header.FilterMethod, Is.EqualTo(WebpConstants.AlphaFilterHorizontal));
        Assert.That(result.Alpha, Is.EqualTo(new byte[] { 10, 15, 20 }));
    }

    [Test]
    public void Decode_UncompressedAlphaAppliesGradientFilterAcrossRows()
    {
        byte[] alpha = new byte[] { 10, 20, 35, 30, 50, 90, 80, 110, 160 };
        byte[] filtered = WebpAlphaFilter.Filter(alpha, 3, 3, WebpConstants.AlphaFilterGradient);
        byte[] payload = AddHeader(filtered, (byte)(WebpConstants.AlphaFilterGradient << WebpConstants.AlphaFilterShift));

        WebpAlphaDecodeResult result = WebpAlphaDecoder.Decode(payload, 3, 3, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Header.FilterMethod, Is.EqualTo(WebpConstants.AlphaFilterGradient));
        Assert.That(result.Alpha, Is.EqualTo(alpha));
    }

    [Test]
    public void Decode_RejectsInvalidDimensions()
    {
        byte[] payload = new byte[] { 0, 1 };

        Assert.Throws<ArgumentOutOfRangeException>(() => WebpAlphaDecoder.Decode(payload, 0, 1, WebpStrictness.Strict));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpAlphaDecoder.Decode(payload, 1, 0, WebpStrictness.Strict));
    }

    static byte[] AddHeader(byte[] alpha, byte header)
    {
        byte[] payload = new byte[alpha.Length + 1];
        payload[0] = header;
        int index = 0;
        while (index < alpha.Length)
        {
            payload[index + 1] = alpha[index];
            index = index + 1;
        }

        return payload;
    }

    static WebpPixelBuffer DecodeLosslessForTest(byte[] imageStream, int width, int height)
    {
        ObservedLosslessStream = WebpUnknownChunk.CopyBytes(imageStream);
        ObservedLosslessWidth = width;
        ObservedLosslessHeight = height;

        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(width, height);
        int index = 0;
        while (index < LosslessGreenValues.Length)
        {
            int pixel = index * 4;
            buffer.Rgba[pixel] = 1;
            buffer.Rgba[pixel + 1] = LosslessGreenValues[index];
            buffer.Rgba[pixel + 2] = 2;
            buffer.Rgba[pixel + 3] = 255;
            index = index + 1;
        }

        return buffer;
    }

    static WebpPixelBuffer DecodeWrongSizeForTest(byte[] imageStream, int width, int height)
    {
        ObservedLosslessStream = WebpUnknownChunk.CopyBytes(imageStream);
        ObservedLosslessWidth = width;
        ObservedLosslessHeight = height;
        return WebpPixelBufferHelper.AllocateRgba(1, 1);
    }
}
