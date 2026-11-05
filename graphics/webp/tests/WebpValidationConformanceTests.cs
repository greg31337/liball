using webp;

namespace webp.Tests;

[TestFixture]
public class WebpValidationConformanceTests
{
    [Test]
    public void RiffSizeAndMissingPaddingRemainErrorsAtBothStrictnessLevels()
    {
        byte[] valid = WriteChunks(new WebpRiffChunk("VP8 ", NewVp8Payload(2, 2)));
        byte[] oversized = CopyBytes(valid);
        WebpEndianWriter.WriteUInt32(oversized, 4, (uint)(valid.Length + 20));
        byte[] oddChunk = WriteChunks(
            new WebpRiffChunk("VP8 ", NewVp8Payload(2, 2)),
            new WebpRiffChunk("odd1", new byte[] { 1 }));
        byte[] missingPadding = new byte[oddChunk.Length - 1];
        Array.Copy(oddChunk, missingPadding, missingPadding.Length);

        AssertErrorAtBothStrictnessLevels(oversized, "extends beyond");
        AssertErrorAtBothStrictnessLevels(missingPadding, "padding byte is missing");
    }

    [Test]
    public void NonzeroPaddingAndTrailingDataWarnPermissivelyButFailStrictly()
    {
        byte[] padded = WriteChunks(
            new WebpRiffChunk("VP8 ", NewVp8Payload(2, 2)),
            new WebpRiffChunk("odd1", new byte[] { 1 }));
        padded[padded.Length - 1] = 0x7F;
        byte[] canonical = WriteChunks(new WebpRiffChunk("VP8 ", NewVp8Payload(2, 2)));
        byte[] trailing = new byte[canonical.Length + 4];
        Array.Copy(canonical, trailing, canonical.Length);
        trailing[canonical.Length] = (byte)'R';
        trailing[canonical.Length + 1] = (byte)'I';
        trailing[canonical.Length + 2] = (byte)'F';
        trailing[canonical.Length + 3] = (byte)'F';

        AssertWarningPermissiveErrorStrict(padded, "padding byte must be zero");
        AssertWarningPermissiveErrorStrict(trailing, "trailing data");
    }

    [Test]
    public void ChunkOrderIsStrictnessSensitiveButSingletonDuplicatesAlwaysFail()
    {
        byte flags = WebpConstants.Vp8XFeatureAlpha;
        byte[] outOfOrder = WriteWithoutSorting(
            new WebpRiffChunk("VP8X", NewVp8XPayload(flags, 2, 2)),
            new WebpRiffChunk("VP8 ", NewVp8Payload(2, 2)),
            new WebpRiffChunk("ALPH", NewAlphaPayload(4)));
        byte metadataFlags = WebpConstants.Vp8XFeatureExif;
        byte[] duplicate = WriteWithoutSorting(
            new WebpRiffChunk("VP8X", NewVp8XPayload(metadataFlags, 2, 2)),
            new WebpRiffChunk("VP8 ", NewVp8Payload(2, 2)),
            new WebpRiffChunk("EXIF", new byte[] { 1 }),
            new WebpRiffChunk("EXIF", new byte[] { 2 }));

        AssertWarningPermissiveErrorStrict(outOfOrder, "appears after later reconstruction chunk");
        AssertErrorAtBothStrictnessLevels(duplicate, "multiple EXIF chunks");
    }

    [Test]
    public void ReservedVp8XAnmfAndAlphaBitsFollowRequestedStrictness()
    {
        byte[] reservedVp8X = WriteChunks(
            new WebpRiffChunk("VP8X", NewVp8XPayload(0x40, 2, 2)),
            new WebpRiffChunk("VP8 ", NewVp8Payload(2, 2)));
        byte animationFlags = WebpConstants.Vp8XFeatureAnimation;
        byte[] reservedAnmf = WriteChunks(
            new WebpRiffChunk("VP8X", NewVp8XPayload(animationFlags, 2, 2)),
            new WebpRiffChunk("ANIM", NewAnimPayload()),
            new WebpRiffChunk("ANMF", NewAnmfPayload(0, 0, 2, 2, 0x80, null, "VP8 ", NewVp8Payload(2, 2))));
        byte alphaFlags = WebpConstants.Vp8XFeatureAlpha;
        byte[] reservedAlpha = WriteChunks(
            new WebpRiffChunk("VP8X", NewVp8XPayload(alphaFlags, 2, 2)),
            new WebpRiffChunk("ALPH", new byte[] { 0xC0, 1, 2, 3, 4 }),
            new WebpRiffChunk("VP8 ", NewVp8Payload(2, 2)));

        AssertWarningPermissiveErrorStrict(reservedVp8X, "reserved feature bits");
        AssertWarningPermissiveErrorStrict(reservedAnmf, "ANMF reserved flag bits");
        AssertWarningPermissiveErrorStrict(reservedAlpha, "ALPH reserved bits");
    }

    [Test]
    public void FeatureMismatchesWarnPermissivelyAndFailStrictlyUnlessDataIsMissing()
    {
        byte[] undeclaredXmp = WriteChunks(
            new WebpRiffChunk("VP8X", NewVp8XPayload(0, 2, 2)),
            new WebpRiffChunk("VP8 ", NewVp8Payload(2, 2)),
            new WebpRiffChunk("XMP ", new byte[] { 1 }));
        byte[] declaredAlphaWithoutData = WriteChunks(
            new WebpRiffChunk("VP8X", NewVp8XPayload(WebpConstants.Vp8XFeatureAlpha, 2, 2)),
            new WebpRiffChunk("VP8 ", NewVp8Payload(2, 2)));
        byte[] declaredIccWithoutChunk = WriteChunks(
            new WebpRiffChunk("VP8X", NewVp8XPayload(WebpConstants.Vp8XFeatureIccProfile, 2, 2)),
            new WebpRiffChunk("VP8 ", NewVp8Payload(2, 2)));

        AssertWarningPermissiveErrorStrict(undeclaredXmp, "does not declare XMP");
        AssertWarningPermissiveErrorStrict(declaredAlphaWithoutData, "declares alpha but no alpha data");
        AssertErrorAtBothStrictnessLevels(declaredIccWithoutChunk, "declares ICC profile");
    }

    [Test]
    public void MalformedExtendedVp8AndVp8LHeadersAlwaysFailValidation()
    {
        byte[] malformedVp8 = NewVp8Payload(2, 2);
        malformedVp8[3] = 0;
        byte[] malformedVp8L = NewVp8LPayload(2, 2, false);
        malformedVp8L[4] = (byte)(malformedVp8L[4] | 0x20);
        byte[] lossy = WriteChunks(
            new WebpRiffChunk("VP8X", NewVp8XPayload(0, 2, 2)),
            new WebpRiffChunk("VP8 ", malformedVp8));
        byte[] lossless = WriteChunks(
            new WebpRiffChunk("VP8X", NewVp8XPayload(0, 2, 2)),
            new WebpRiffChunk("VP8L", malformedVp8L));

        AssertErrorAtBothStrictnessLevels(lossy, "VP8 key-frame signature");
        AssertErrorAtBothStrictnessLevels(lossless, "VP8L version");
    }

    [Test]
    public void InvalidAlphaCombinationsAndDuplicateFrameSubchunksAlwaysFail()
    {
        byte alphaFlags = WebpConstants.Vp8XFeatureAlpha;
        byte[] alphaWithVp8L = WriteChunks(
            new WebpRiffChunk("VP8X", NewVp8XPayload(alphaFlags, 2, 2)),
            new WebpRiffChunk("ALPH", NewAlphaPayload(4)),
            new WebpRiffChunk("VP8L", NewVp8LPayload(2, 2, true)));
        byte[] emptyAlpha = WriteChunks(
            new WebpRiffChunk("VP8X", NewVp8XPayload(alphaFlags, 2, 2)),
            new WebpRiffChunk("ALPH", Array.Empty<byte>()),
            new WebpRiffChunk("VP8 ", NewVp8Payload(2, 2)));
        List<byte> duplicatedFramePayload = NewAnmfHeader(0, 0, 2, 2, 0);
        AppendSubchunk(duplicatedFramePayload, "VP8 ", NewVp8Payload(2, 2));
        AppendSubchunk(duplicatedFramePayload, "VP8L", NewVp8LPayload(2, 2, false));
        byte[] duplicateFrameImages = WriteChunks(
            new WebpRiffChunk("VP8X", NewVp8XPayload(WebpConstants.Vp8XFeatureAnimation, 2, 2)),
            new WebpRiffChunk("ANIM", NewAnimPayload()),
            new WebpRiffChunk("ANMF", duplicatedFramePayload.ToArray()));

        AssertErrorAtBothStrictnessLevels(alphaWithVp8L, "ALPH chunk is not allowed with a VP8L");
        AssertErrorAtBothStrictnessLevels(emptyAlpha, "ALPH payload must contain a header");
        AssertErrorAtBothStrictnessLevels(duplicateFrameImages, "multiple image subchunks");
    }

    [Test]
    public void InvalidAnimationRectanglesMismatchedImagesAndZeroDimensionsAlwaysFail()
    {
        byte animationFlags = WebpConstants.Vp8XFeatureAnimation;
        byte[] outsideCanvas = WriteChunks(
            new WebpRiffChunk("VP8X", NewVp8XPayload(animationFlags, 4, 4)),
            new WebpRiffChunk("ANIM", NewAnimPayload()),
            new WebpRiffChunk("ANMF", NewAnmfPayload(1, 1, 3, 3, 0, null, "VP8 ", NewVp8Payload(3, 3))));
        byte[] mismatchedImage = WriteChunks(
            new WebpRiffChunk("VP8X", NewVp8XPayload(animationFlags, 4, 4)),
            new WebpRiffChunk("ANIM", NewAnimPayload()),
            new WebpRiffChunk("ANMF", NewAnmfPayload(0, 0, 2, 2, 0, null, "VP8L", NewVp8LPayload(1, 2, false))));
        byte[] zeroWidthPayload = NewVp8Payload(1, 2);
        WebpEndianWriter.WriteUInt16(zeroWidthPayload, 6, 0);
        byte[] zeroSizedImage = WriteChunks(
            new WebpRiffChunk("VP8X", NewVp8XPayload(animationFlags, 2, 2)),
            new WebpRiffChunk("ANIM", NewAnimPayload()),
            new WebpRiffChunk("ANMF", NewAnmfPayload(0, 0, 1, 2, 0, null, "VP8 ", zeroWidthPayload)));

        AssertErrorAtBothStrictnessLevels(outsideCanvas, "exceeds the VP8X canvas");
        AssertErrorAtBothStrictnessLevels(mismatchedImage, "dimensions do not match");
        AssertErrorAtBothStrictnessLevels(zeroSizedImage, "dimensions must be non-zero");
    }

    static void AssertWarningPermissiveErrorStrict(byte[] data, string message)
    {
        WebpValidationResult permissive = Validate(data, WebpStrictness.Permissive);
        WebpValidationResult strict = Validate(data, WebpStrictness.Strict);
        Assert.That(permissive.IsValid, Is.True, JoinDiagnostics(permissive));
        Assert.That(HasDiagnostic(permissive, WebpDiagnosticSeverity.Warning, message), Is.True, JoinDiagnostics(permissive));
        Assert.That(strict.IsValid, Is.False);
        Assert.That(HasDiagnostic(strict, WebpDiagnosticSeverity.Error, message), Is.True, JoinDiagnostics(strict));
    }

    static void AssertErrorAtBothStrictnessLevels(byte[] data, string message)
    {
        WebpValidationResult permissive = Validate(data, WebpStrictness.Permissive);
        WebpValidationResult strict = Validate(data, WebpStrictness.Strict);
        Assert.That(permissive.IsValid, Is.False);
        Assert.That(strict.IsValid, Is.False);
        Assert.That(HasDiagnostic(permissive, WebpDiagnosticSeverity.Error, message), Is.True, JoinDiagnostics(permissive));
        Assert.That(HasDiagnostic(strict, WebpDiagnosticSeverity.Error, message), Is.True, JoinDiagnostics(strict));
    }

    static WebpValidationResult Validate(byte[] data, WebpStrictness strictness)
    {
        WebpDecodeOptions options = new WebpDecodeOptions();
        options.Strictness = strictness;
        IWebpValidator validator = new WebpCodec();
        return validator.Validate(data, options);
    }

    static bool HasDiagnostic(WebpValidationResult result, WebpDiagnosticSeverity severity, string message)
    {
        int index = 0;
        while (index < result.Diagnostics.Count)
        {
            WebpDiagnostic diagnostic = result.Diagnostics[index];
            if (diagnostic.Severity == severity && diagnostic.Message.Contains(message))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static string JoinDiagnostics(WebpValidationResult result)
    {
        List<string> messages = new List<string>();
        int index = 0;
        while (index < result.Diagnostics.Count)
        {
            messages.Add(result.Diagnostics[index].Severity + ": " + result.Diagnostics[index].Message);
            index = index + 1;
        }

        return string.Join("; ", messages);
    }

    static byte[] WriteChunks(params WebpRiffChunk[] chunks)
    {
        return WebpRiffWriter.Write("WEBP", new List<WebpRiffChunk>(chunks));
    }

    static byte[] WriteWithoutSorting(params WebpRiffChunk[] chunks)
    {
        List<WebpRiffChunk> chunkList = new List<WebpRiffChunk>(chunks);
        long riffSize = WebpRiffWriter.ComputeRiffSize(chunkList);
        List<byte> data = new List<byte>();
        AppendFourCc(data, "RIFF");
        WebpEndianWriter.AppendUInt32(data, (uint)riffSize);
        AppendFourCc(data, "WEBP");
        int index = 0;
        while (index < chunkList.Count)
        {
            WebpRiffChunk chunk = chunkList[index];
            AppendFourCc(data, chunk.FourCc);
            WebpEndianWriter.AppendUInt32(data, (uint)chunk.Payload.Length);
            WebpEndianWriter.AppendChunkPayload(data, chunk.Payload);
            index = index + 1;
        }

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

    static byte[] NewVp8LPayload(int width, int height, bool alpha)
    {
        byte[] payload = new byte[WebpConstants.Vp8LHeaderSize];
        uint bits = (uint)(width - 1);
        bits = bits | ((uint)(height - 1) << 14);
        if (alpha)
        {
            bits = bits | (1U << 28);
        }

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

    static byte[] NewAlphaPayload(int pixels)
    {
        byte[] payload = new byte[pixels + 1];
        payload[0] = WebpConstants.AlphaCompressionNone;
        int index = 1;
        while (index < payload.Length)
        {
            payload[index] = 255;
            index = index + 1;
        }

        return payload;
    }

    static byte[] NewAnimPayload()
    {
        return new byte[WebpConstants.AnimChunkPayloadSize];
    }

    static byte[] NewAnmfPayload(int xHalf, int yHalf, int width, int height, byte flags, byte[]? alpha, string imageFourCc, byte[] imagePayload)
    {
        List<byte> payload = NewAnmfHeader(xHalf, yHalf, width, height, flags);
        if (alpha != null)
        {
            AppendSubchunk(payload, "ALPH", alpha);
        }

        AppendSubchunk(payload, imageFourCc, imagePayload);
        return payload.ToArray();
    }

    static List<byte> NewAnmfHeader(int xHalf, int yHalf, int width, int height, byte flags)
    {
        List<byte> payload = new List<byte>();
        WebpEndianWriter.AppendUInt24(payload, (uint)xHalf);
        WebpEndianWriter.AppendUInt24(payload, (uint)yHalf);
        WebpEndianWriter.AppendUInt24(payload, (uint)(width - 1));
        WebpEndianWriter.AppendUInt24(payload, (uint)(height - 1));
        WebpEndianWriter.AppendUInt24(payload, 1);
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

    static byte[] CopyBytes(byte[] source)
    {
        byte[] copy = new byte[source.Length];
        Array.Copy(source, copy, source.Length);
        return copy;
    }
}
