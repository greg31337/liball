using webp;

namespace webp.Tests;

[TestFixture]
public class WebpDetectionIdentificationTests
{
    [Test]
    public void DetectionHelpersInspectOnlyTheDocumentedSignatureMimeTypeAndExtension()
    {
        IWebpFormatDetector detector = new WebpCodec();
        byte[] webp = WebpRiffWriter.Write("WEBP", new List<WebpRiffChunk>());
        byte[] wrapped = Wrap(webp);
        byte[] impossibleDeclaredSize = CopyBytes(webp);
        WebpEndianWriter.WriteUInt32(impossibleDeclaredSize, 4, uint.MaxValue);
        byte[] wrongRiff = CopyBytes(webp);
        wrongRiff[3] = (byte)'X';
        byte[] wrongForm = WebpRiffWriter.Write("AVI ", new List<WebpRiffChunk>());

        Assert.That(detector.HasRiffWebpSignature(webp), Is.True);
        Assert.That(detector.HasRiffWebpSignature(impossibleDeclaredSize), Is.True);
        Assert.That(detector.HasRiffWebpSignatureSpan(wrapped.AsSpan(3, webp.Length)), Is.True);
        Assert.That(detector.HasRiffWebpSignature(new byte[] { 1, 2, 3 }), Is.False);
        Assert.That(detector.HasRiffWebpSignature(wrongRiff), Is.False);
        Assert.That(detector.HasRiffWebpSignature(wrongForm), Is.False);
        Assert.That(detector.IsWebpMimeType("image/webp"), Is.True);
        Assert.That(detector.IsWebpMimeType("IMAGE/WEBP"), Is.True);
        Assert.That(detector.IsWebpMimeType(" image/webp"), Is.False);
        Assert.That(detector.IsWebpMimeType("image/webp; charset=binary"), Is.False);
        Assert.That(detector.IsWebpFileExtension(".webp"), Is.True);
        Assert.That(detector.IsWebpFileExtension("WEBP"), Is.True);
        Assert.That(detector.IsWebpFileExtension("image.webp"), Is.False);
        Assert.That(detector.IsWebpFileExtension(".webp "), Is.False);
    }

    [Test]
    public void IdentifyReadsSimpleLossyAndLosslessHeaders()
    {
        IWebpInfoReader reader = new WebpCodec();
        byte[] lossy = WriteChunks(new WebpRiffChunk("VP8 ", NewVp8Payload(321, 123)));
        byte[] lossless = WriteChunks(new WebpRiffChunk("VP8L", NewVp8LPayload(17, 33, true)));

        WebpInfo lossyInfo = reader.Read(lossy, new WebpDecodeOptions());
        WebpInfo losslessInfo = reader.Read(lossless, new WebpDecodeOptions());

        Assert.That(lossyInfo.Width, Is.EqualTo(321));
        Assert.That(lossyInfo.Height, Is.EqualTo(123));
        Assert.That(lossyInfo.ContainerKind, Is.EqualTo(WebpContainerKind.SimpleLossy));
        Assert.That(lossyInfo.BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossy));
        Assert.That(lossyInfo.HasAlpha, Is.False);
        Assert.That(losslessInfo.Width, Is.EqualTo(17));
        Assert.That(losslessInfo.Height, Is.EqualTo(33));
        Assert.That(losslessInfo.ContainerKind, Is.EqualTo(WebpContainerKind.SimpleLossless));
        Assert.That(losslessInfo.BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossless));
        Assert.That(losslessInfo.HasAlpha, Is.True);
    }

    [TestCase(0x00, WebpVp8XFeatureFlags.None)]
    [TestCase(0x02, WebpVp8XFeatureFlags.Animation)]
    [TestCase(0x04, WebpVp8XFeatureFlags.Xmp)]
    [TestCase(0x08, WebpVp8XFeatureFlags.Exif)]
    [TestCase(0x10, WebpVp8XFeatureFlags.Alpha)]
    [TestCase(0x20, WebpVp8XFeatureFlags.IccProfile)]
    public void IdentifyExposesEachDeclaredVp8XFeatureFlag(int flag, WebpVp8XFeatureFlags expected)
    {
        IWebpInfoReader reader = new WebpCodec();
        byte[] data = WriteChunks(new WebpRiffChunk("VP8X", NewVp8XPayload((byte)flag, 5, 7)));

        WebpInfo info = reader.Read(data, new WebpDecodeOptions());

        Assert.That(info.DeclaredFeatureFlags, Is.EqualTo(expected));
        Assert.That(info.HasAnimation, Is.EqualTo(expected == WebpVp8XFeatureFlags.Animation));
        Assert.That(info.HasAlpha, Is.EqualTo(expected == WebpVp8XFeatureFlags.Alpha));
        Assert.That(info.HasIccProfile, Is.False);
        Assert.That(info.HasExif, Is.False);
        Assert.That(info.HasXmp, Is.False);
    }

    [Test]
    public void IdentifySeparatesDeclaredFeaturesFromMetadataAndUnknownChunkPresence()
    {
        byte flags = (byte)(WebpConstants.Vp8XFeatureIccProfile
            | WebpConstants.Vp8XFeatureAlpha
            | WebpConstants.Vp8XFeatureExif
            | WebpConstants.Vp8XFeatureXmp);
        byte[] data = WriteChunks(
            new WebpRiffChunk("VP8X", NewVp8XPayload(flags, 9, 11)),
            new WebpRiffChunk("ICCP", new byte[] { 1 }),
            new WebpRiffChunk("ALPH", new byte[] { 0 }),
            new WebpRiffChunk("VP8 ", new byte[] { 1, 2 }),
            new WebpRiffChunk("EXIF", new byte[] { 2 }),
            new WebpRiffChunk("XMP ", new byte[] { 3 }),
            new WebpRiffChunk("cust", new byte[] { 4, 5, 6 }));

        WebpInfo info = new WebpCodec().Read(data, new WebpDecodeOptions());

        Assert.That(info.DeclaredFeatureFlags, Is.EqualTo(
            WebpVp8XFeatureFlags.IccProfile
            | WebpVp8XFeatureFlags.Alpha
            | WebpVp8XFeatureFlags.Exif
            | WebpVp8XFeatureFlags.Xmp));
        Assert.That(info.HasIccProfile, Is.True);
        Assert.That(info.HasExif, Is.True);
        Assert.That(info.HasXmp, Is.True);
        Assert.That(info.HasAlpha, Is.True);
        Assert.That(info.Chunks, Has.Count.EqualTo(7));
        Assert.That(info.Chunks[6].FourCc, Is.EqualTo("cust"));
        Assert.That(info.Chunks[6].IsKnown, Is.False);
        Assert.That(info.Chunks[6].HasPadding, Is.True);
        Assert.That(info.Chunks[6].IsPaddingValid, Is.True);
    }

    [Test]
    public void IdentifyReturnsAnimationGlobalsMixedFrameTableAndVp8LFrameAlpha()
    {
        byte[] losslessAlpha = NewVp8LPayload(3, 4, true);
        byte[] data = WriteChunks(
            new WebpRiffChunk("VP8X", NewVp8XPayload(WebpConstants.Vp8XFeatureAnimation, 20, 18)),
            new WebpRiffChunk("ANIM", NewAnimPayload(0xA1B2C3D4U, 7)),
            new WebpRiffChunk("ANMF", NewAnmfPayload(1, 2, 3, 4, 25, 0x03, "VP8L", losslessAlpha)),
            new WebpRiffChunk("ANMF", NewAnmfPayload(4, 1, 5, 6, 40, 0x00, "VP8 ", new byte[] { 1, 2 })));

        WebpInfo info = new WebpCodec().Read(data, new WebpDecodeOptions());

        Assert.That(info.ContainerKind, Is.EqualTo(WebpContainerKind.ExtendedAnimation));
        Assert.That(info.HasAnimation, Is.True);
        Assert.That(info.HasAlpha, Is.True);
        Assert.That(info.LoopCount, Is.EqualTo(7));
        Assert.That(info.BackgroundColorBgra, Is.EqualTo(0xA1B2C3D4U));
        Assert.That(info.BitstreamKind, Is.EqualTo(WebpBitstreamKind.Unknown));
        Assert.That(info.Frames, Has.Count.EqualTo(2));
        AssertFrame(info.Frames[0], 2, 4, 3, 4, 25, WebpFrameDisposeMode.DisposeToBackground, WebpFrameBlendMode.Replace, WebpBitstreamKind.Lossless);
        AssertFrame(info.Frames[1], 8, 2, 5, 6, 40, WebpFrameDisposeMode.DoNotDispose, WebpFrameBlendMode.AlphaBlend, WebpBitstreamKind.Lossy);
    }

    [Test]
    public void IdentifyFindsAlphaInsideExtendedVp8LStillWithoutRelyingOnFeatureFlag()
    {
        byte[] data = WriteChunks(
            new WebpRiffChunk("VP8X", NewVp8XPayload(0, 2, 3)),
            new WebpRiffChunk("VP8L", NewVp8LPayload(2, 3, true)));

        WebpInfo info = new WebpCodec().Read(data, new WebpDecodeOptions());

        Assert.That(info.DeclaredFeatureFlags, Is.EqualTo(WebpVp8XFeatureFlags.None));
        Assert.That(info.BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossless));
        Assert.That(info.HasAlpha, Is.True);
    }

    [Test]
    public void IdentifyExposesReservedBitsAndBytesWithRequestedStrictness()
    {
        byte[] vp8x = NewVp8XPayload(0x41, 2, 3);
        vp8x[2] = 9;
        byte[] data = WriteChunks(new WebpRiffChunk("VP8X", vp8x));
        WebpDecodeOptions permissive = new WebpDecodeOptions();
        permissive.Strictness = WebpStrictness.Permissive;
        WebpDecodeOptions strict = new WebpDecodeOptions();
        strict.Strictness = WebpStrictness.Strict;

        WebpInfo permissiveInfo = new WebpCodec().Read(data, permissive);
        WebpInfo strictInfo = new WebpCodec().Read(data, strict);

        Assert.That(permissiveInfo.ReservedFeatureBits, Is.EqualTo(0x41));
        Assert.That(permissiveInfo.HasReservedFeatureBits, Is.True);
        Assert.That(permissiveInfo.HasReservedBytes, Is.True);
        Assert.That(permissiveInfo.Diagnostics, Has.Some.Matches<WebpDiagnostic>(diagnostic => diagnostic.Severity == WebpDiagnosticSeverity.Warning));
        Assert.That(strictInfo.ReservedFeatureBits, Is.EqualTo(0x41));
        Assert.That(strictInfo.HasReservedFeatureBits, Is.True);
        Assert.That(strictInfo.HasReservedBytes, Is.True);
        Assert.That(strictInfo.Diagnostics, Has.Some.Matches<WebpDiagnostic>(diagnostic => diagnostic.Severity == WebpDiagnosticSeverity.Error));
    }

    [Test]
    public void IdentifyPreservesPaddingStateAndReportsTrailingData()
    {
        byte[] data = WriteChunks(
            new WebpRiffChunk("odd1", new byte[] { 7 }),
            new WebpRiffChunk("VP8L", NewVp8LPayload(1, 1, false)));
        WebpRiffParseResult parsed = WebpRiffParser.Parse(data);
        byte[] badPadding = CopyBytes(data);
        badPadding[(int)parsed.Chunks[1].PaddingOffset] = 0x7F;
        byte[] trailing = new byte[data.Length + 2];
        Array.Copy(data, trailing, data.Length);
        trailing[trailing.Length - 2] = 0xAA;
        trailing[trailing.Length - 1] = 0xBB;

        WebpInfo validInfo = new WebpCodec().Read(data, new WebpDecodeOptions());
        WebpInfo badPaddingInfo = new WebpCodec().Read(badPadding, new WebpDecodeOptions());
        WebpInfo trailingInfo = new WebpCodec().Read(trailing, new WebpDecodeOptions());

        Assert.That(validInfo.Chunks[1].FourCc, Is.EqualTo("odd1"));
        Assert.That(validInfo.Chunks[1].HasPadding, Is.True);
        Assert.That(validInfo.Chunks[1].PaddingOffset, Is.EqualTo(validInfo.Chunks[1].PayloadOffset + 1));
        Assert.That(validInfo.Chunks[1].PaddedEndOffset, Is.EqualTo(validInfo.Chunks[1].PaddingOffset + 1));
        Assert.That(validInfo.Chunks[1].IsPaddingValid, Is.True);
        Assert.That(badPaddingInfo.Chunks, Has.Count.EqualTo(2));
        Assert.That(badPaddingInfo.Chunks[1].IsPaddingValid, Is.False);
        Assert.That(badPaddingInfo.Diagnostics, Has.Some.Matches<WebpDiagnostic>(diagnostic => diagnostic.Message.Contains("padding")));
        Assert.That(trailingInfo.Chunks, Has.Count.EqualTo(2));
        Assert.That(trailingInfo.Diagnostics, Has.Some.Matches<WebpDiagnostic>(diagnostic => diagnostic.Severity == WebpDiagnosticSeverity.Warning && diagnostic.Message.Contains("trailing")));
    }

    static void AssertFrame(WebpFrame frame, int x, int y, int width, int height, int duration, WebpFrameDisposeMode dispose, WebpFrameBlendMode blend, WebpBitstreamKind bitstream)
    {
        Assert.That(frame.X, Is.EqualTo(x));
        Assert.That(frame.Y, Is.EqualTo(y));
        Assert.That(frame.Width, Is.EqualTo(width));
        Assert.That(frame.Height, Is.EqualTo(height));
        Assert.That(frame.DurationMilliseconds, Is.EqualTo(duration));
        Assert.That(frame.DisposeMode, Is.EqualTo(dispose));
        Assert.That(frame.BlendMode, Is.EqualTo(blend));
        Assert.That(frame.BitstreamKind, Is.EqualTo(bitstream));
        Assert.That(frame.Pixels, Is.Null);
        Assert.That(frame.EncodedBitstream, Is.Null);
        Assert.That(frame.AlphaData, Is.Null);
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
        uint bits = (uint)(width - 1) | ((uint)(height - 1) << 14);
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

    static byte[] NewAnimPayload(uint backgroundColorBgra, ushort loopCount)
    {
        byte[] payload = new byte[WebpConstants.AnimChunkPayloadSize];
        WebpEndianWriter.WriteUInt32(payload, 0, backgroundColorBgra);
        WebpEndianWriter.WriteUInt16(payload, 4, loopCount);
        return payload;
    }

    static byte[] NewAnmfPayload(int xHalf, int yHalf, int width, int height, int duration, byte flags, string imageFourCc, byte[] imagePayload)
    {
        List<byte> payload = new List<byte>();
        WebpEndianWriter.AppendUInt24(payload, (uint)xHalf);
        WebpEndianWriter.AppendUInt24(payload, (uint)yHalf);
        WebpEndianWriter.AppendUInt24(payload, (uint)(width - 1));
        WebpEndianWriter.AppendUInt24(payload, (uint)(height - 1));
        WebpEndianWriter.AppendUInt24(payload, (uint)duration);
        payload.Add(flags);
        AppendSubchunk(payload, imageFourCc, imagePayload);
        return payload.ToArray();
    }

    static void AppendSubchunk(List<byte> data, string fourCc, byte[] payload)
    {
        data.Add((byte)fourCc[0]);
        data.Add((byte)fourCc[1]);
        data.Add((byte)fourCc[2]);
        data.Add((byte)fourCc[3]);
        WebpEndianWriter.AppendUInt32(data, (uint)payload.Length);
        WebpEndianWriter.AppendChunkPayload(data, payload);
    }

    static byte[] WriteChunks(params WebpRiffChunk[] source)
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        int index = 0;
        while (index < source.Length)
        {
            chunks.Add(source[index]);
            index = index + 1;
        }

        return WebpRiffWriter.Write("WEBP", chunks);
    }

    static byte[] Wrap(byte[] data)
    {
        byte[] wrapped = new byte[data.Length + 6];
        wrapped[0] = 90;
        wrapped[1] = 91;
        wrapped[2] = 92;
        Array.Copy(data, 0, wrapped, 3, data.Length);
        return wrapped;
    }

    static byte[] CopyBytes(byte[] data)
    {
        byte[] copy = new byte[data.Length];
        Array.Copy(data, copy, data.Length);
        return copy;
    }
}
