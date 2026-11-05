using webp;

namespace webp.Tests;

[TestFixture]
public class WebpValidatorTests
{
    [Test]
    public void Validate_AcceptsBasicLossyContainer()
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("VP8 ", NewVp8Payload(12, 10)));

        WebpValidationResult result = WebpValidator.Validate(WebpRiffWriter.Write("WEBP", chunks));

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Diagnostics, Is.Empty);
    }

    [Test]
    public void Validate_ReturnsMalformedRiffDiagnostics()
    {
        WebpValidationResult result = WebpValidator.Validate(new byte[] { 1, 2, 3 });

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(result.Diagnostics[0].Message, Does.Contain("truncated"));
    }

    [Test]
    public void Validate_ReturnsInvalidChunkSizeDiagnostics()
    {
        byte[] data = NewChunkPayloadExceedsRiff();

        WebpValidationResult result = WebpValidator.Validate(data);

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasDiagnosticContaining(result, "payload exceeds"), Is.True);
    }

    [Test]
    public void Validate_RejectsBasicContainerWithMissingImageChunk()
    {
        WebpValidationResult result = WebpValidator.Validate(WebpRiffWriter.Write("WEBP", new List<WebpRiffChunk>()));

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasDiagnosticContaining(result, "missing a VP8 or VP8L"), Is.True);
    }

    [Test]
    public void Validate_RejectsInvalidBasicDimensions()
    {
        byte[] payload = NewVp8Payload(1, 1);
        WebpEndianWriter.WriteUInt16(payload, 6, 0);
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("VP8 ", payload));

        WebpValidationResult result = WebpValidator.Validate(WebpRiffWriter.Write("WEBP", chunks));

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasDiagnosticContaining(result, "dimensions"), Is.True);
    }

    [Test]
    public void Validate_RejectsBadVp8XFeatureFlagsInStrictMode()
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("VP8X", NewVp8XPayload(0x40, 1, 1)));
        WebpDecodeOptions options = new WebpDecodeOptions();
        options.Strictness = WebpStrictness.Strict;

        WebpValidationResult result = WebpValidator.Validate(WebpRiffWriter.Write("WEBP", chunks), options);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(WebpDiagnosticSeverity.Error));
        Assert.That(result.Diagnostics[0].FourCc, Is.EqualTo("VP8X"));
        Assert.That(result.Diagnostics[0].Message, Does.Contain("reserved feature bits"));
    }

    [Test]
    public void Validate_RejectsExtendedStillContainerWithMissingImageChunk()
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("VP8X", NewVp8XPayload(0, 5, 6)));

        WebpValidationResult result = WebpValidator.Validate(WebpRiffWriter.Write("WEBP", chunks));

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasDiagnosticContaining(result, "extended still-image container is missing"), Is.True);
    }

    [Test]
    public void Validate_RejectsDeclaredMetadataFeatureWithoutChunk()
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("VP8X", NewVp8XPayload(WebpConstants.Vp8XFeatureExif, 5, 6)));
        chunks.Add(new WebpRiffChunk("VP8 ", NewVp8Payload(5, 6)));

        WebpValidationResult result = WebpValidator.Validate(WebpRiffWriter.Write("WEBP", chunks));

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasDiagnosticContaining(result, "declares Exif metadata"), Is.True);
    }

    [Test]
    public void Validate_WarnsForUndeclaredMetadataChunkInPermissiveMode()
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("VP8X", NewVp8XPayload(0, 5, 6)));
        chunks.Add(new WebpRiffChunk("VP8 ", NewVp8Payload(5, 6)));
        chunks.Add(new WebpRiffChunk("XMP ", new byte[] { 2 }));
        WebpDecodeOptions options = new WebpDecodeOptions();
        options.Strictness = WebpStrictness.Permissive;

        WebpValidationResult result = WebpValidator.Validate(WebpRiffWriter.Write("WEBP", chunks), options);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(WebpDiagnosticSeverity.Warning));
        Assert.That(result.Diagnostics[0].Message, Does.Contain("does not declare XMP metadata"));
    }

    [Test]
    public void Validate_RejectsAnimatedContainerWithMissingAnimChunk()
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("VP8X", NewVp8XPayload(WebpConstants.Vp8XFeatureAnimation, 10, 10)));
        chunks.Add(new WebpRiffChunk("ANMF", NewAnmfPayload(0, 0, 1, 1, 1, 0, false, "VP8 ", new byte[] { 1 })));

        WebpValidationResult result = WebpValidator.Validate(WebpRiffWriter.Write("WEBP", chunks));

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasDiagnosticContaining(result, "missing ANIM"), Is.True);
    }

    [Test]
    public void Validate_RejectsAnimationFlagWithoutFrameChunks()
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("VP8X", NewVp8XPayload(WebpConstants.Vp8XFeatureAnimation, 10, 10)));
        chunks.Add(new WebpRiffChunk("ANIM", NewAnimPayload(0, 1)));

        WebpValidationResult result = WebpValidator.Validate(WebpRiffWriter.Write("WEBP", chunks));

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasDiagnosticContaining(result, "missing ANMF"), Is.True);
    }

    [Test]
    public void Validate_RejectsFrameRectangleOutsideCanvas()
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("VP8X", NewVp8XPayload(WebpConstants.Vp8XFeatureAnimation, 10, 10)));
        chunks.Add(new WebpRiffChunk("ANIM", NewAnimPayload(0, 1)));
        chunks.Add(new WebpRiffChunk("ANMF", NewAnmfPayload(4, 4, 5, 5, 1, 0, false, "VP8 ", new byte[] { 1 })));

        WebpValidationResult result = WebpValidator.Validate(WebpRiffWriter.Write("WEBP", chunks));

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasDiagnosticContaining(result, "exceeds the VP8X canvas"), Is.True);
    }

    [Test]
    public void Validate_ReportsMultipleDiagnosticsForMalformedExtendedAnimation()
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("VP8X", NewVp8XPayload(WebpConstants.Vp8XFeatureAnimation, 4, 4)));
        chunks.Add(new WebpRiffChunk("XMP ", new byte[] { 1, 2 }));
        chunks.Add(new WebpRiffChunk("ANMF", NewAnmfPayload(2, 2, 3, 3, 1, 0, true, "VP8L", NewVp8LPayload(3, 3))));

        WebpValidationResult result = WebpValidator.Validate(WebpRiffWriter.Write("WEBP", chunks));

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasDiagnosticContaining(result, "ALPH subchunk is not allowed with a VP8L frame image"), Is.True);
        Assert.That(HasDiagnosticContaining(result, "does not declare XMP metadata"), Is.True);
        Assert.That(HasDiagnosticContaining(result, "alpha feature flag is not set"), Is.True);
        Assert.That(HasDiagnosticContaining(result, "missing ANIM"), Is.True);
        Assert.That(HasDiagnosticContaining(result, "exceeds the VP8X canvas"), Is.True);
    }

    [Test]
    public void Validate_RejectsVp8XCanvasPixelCountAboveLimit()
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("VP8X", NewVp8XPayload(0, WebpConstants.Vp8XMaxCanvasDimension, WebpConstants.Vp8XMaxCanvasDimension)));
        chunks.Add(new WebpRiffChunk("VP8 ", new byte[] { 1 }));

        WebpValidationResult result = WebpValidator.Validate(WebpRiffWriter.Write("WEBP", chunks));

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasDiagnosticContaining(result, "maximum pixel count"), Is.True);
    }

    static bool HasDiagnosticContaining(WebpValidationResult result, string text)
    {
        int index = 0;
        while (index < result.Diagnostics.Count)
        {
            if (result.Diagnostics[index].Message.Contains(text))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static byte[] NewChunkPayloadExceedsRiff()
    {
        List<byte> data = new List<byte>();
        AppendFourCc(data, "RIFF");
        WebpEndianWriter.AppendUInt32(data, 12);
        AppendFourCc(data, "WEBP");
        AppendFourCc(data, "VP8 ");
        WebpEndianWriter.AppendUInt32(data, 1);
        return data.ToArray();
    }

    static byte[] NewVp8Payload(int width, int height)
    {
        byte[] payload = new byte[WebpConstants.Vp8LossyKeyFrameHeaderSize];
        payload[3] = WebpConstants.Vp8LossyKeyFrameSignature0;
        payload[4] = WebpConstants.Vp8LossyKeyFrameSignature1;
        payload[5] = WebpConstants.Vp8LossyKeyFrameSignature2;
        WebpEndianWriter.WriteUInt16(payload, 6, (ushort)width);
        WebpEndianWriter.WriteUInt16(payload, 8, (ushort)height);
        return payload;
    }

    static byte[] NewVp8LPayload(int width, int height)
    {
        byte[] payload = new byte[WebpConstants.Vp8LHeaderSize];
        uint bits = (uint)(width - 1);
        bits = bits | ((uint)(height - 1) << 14);
        payload[0] = WebpConstants.Vp8LSignature;
        WebpEndianWriter.WriteUInt32(payload, 1, bits);
        return payload;
    }

    static byte[] NewVp8XPayload(byte flags, int width, int height)
    {
        byte[] payload = new byte[WebpConstants.Vp8XChunkPayloadSize];
        payload[0] = flags;
        WebpEndianWriter.WriteUInt24(payload, 4, (uint)(width - 1));
        WebpEndianWriter.WriteUInt24(payload, 7, (uint)(height - 1));
        return payload;
    }

    static byte[] NewAnimPayload(uint backgroundColorBgra, ushort loopCount)
    {
        byte[] payload = new byte[WebpConstants.AnimChunkPayloadSize];
        WebpEndianWriter.WriteUInt32(payload, 0, backgroundColorBgra);
        WebpEndianWriter.WriteUInt16(payload, 4, loopCount);
        return payload;
    }

    static byte[] NewAnmfPayload(int xHalf, int yHalf, int width, int height, int duration, byte flags, bool includeAlpha, string imageFourCc, byte[] imagePayload)
    {
        List<byte> payload = NewAnmfHeader(xHalf, yHalf, width, height, duration, flags);
        if (includeAlpha)
        {
            AppendSubchunk(payload, "ALPH", new byte[] { 0xAA });
        }

        AppendSubchunk(payload, imageFourCc, imagePayload);
        return payload.ToArray();
    }

    static List<byte> NewAnmfHeader(int xHalf, int yHalf, int width, int height, int duration, byte flags)
    {
        List<byte> payload = new List<byte>();
        WebpEndianWriter.AppendUInt24(payload, (uint)xHalf);
        WebpEndianWriter.AppendUInt24(payload, (uint)yHalf);
        WebpEndianWriter.AppendUInt24(payload, (uint)(width - 1));
        WebpEndianWriter.AppendUInt24(payload, (uint)(height - 1));
        WebpEndianWriter.AppendUInt24(payload, (uint)duration);
        payload.Add(flags);
        return payload;
    }

    static void AppendSubchunk(List<byte> data, string fourCc, byte[] payload)
    {
        AppendFourCc(data, fourCc);
        WebpEndianWriter.AppendUInt32(data, (uint)payload.Length);
        WebpEndianWriter.AppendChunkPayload(data, payload);
    }

    static void AppendFourCc(List<byte> data, string fourCc)
    {
        data.Add((byte)fourCc[0]);
        data.Add((byte)fourCc[1]);
        data.Add((byte)fourCc[2]);
        data.Add((byte)fourCc[3]);
    }
}
