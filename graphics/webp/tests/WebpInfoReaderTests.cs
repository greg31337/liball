using webp;

namespace webp.Tests;

[TestFixture]
public class WebpInfoReaderTests
{
    [Test]
    public void Read_BasicVp8ReturnsDimensionsFeaturesAndChunkTable()
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("VP8 ", NewVp8Payload(320, 240)));

        WebpInfo info = WebpInfoReader.Read(WebpRiffWriter.Write("WEBP", chunks));

        Assert.That(info.Width, Is.EqualTo(320));
        Assert.That(info.Height, Is.EqualTo(240));
        Assert.That(info.ContainerKind, Is.EqualTo(WebpContainerKind.SimpleLossy));
        Assert.That(info.BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossy));
        Assert.That(info.HasAlpha, Is.False);
        Assert.That(info.HasAnimation, Is.False);
        Assert.That(info.HasIccProfile, Is.False);
        Assert.That(info.HasExif, Is.False);
        Assert.That(info.HasXmp, Is.False);
        Assert.That(info.LoopCount, Is.EqualTo(1));
        Assert.That(info.BackgroundColorBgra, Is.EqualTo(0));
        Assert.That(info.DeclaredFeatureFlags, Is.EqualTo(WebpVp8XFeatureFlags.None));
        Assert.That(info.ReservedFeatureBits, Is.EqualTo(0));
        Assert.That(info.HasReservedFeatureBits, Is.False);
        Assert.That(info.HasReservedBytes, Is.False);
        Assert.That(info.Chunks, Has.Count.EqualTo(1));
        Assert.That(info.Chunks[0].FourCc, Is.EqualTo("VP8 "));
        Assert.That(info.Chunks[0].PayloadLength, Is.EqualTo(WebpConstants.Vp8LossyKeyFrameHeaderSize));
        Assert.That(info.Frames, Is.Empty);
        Assert.That(info.Diagnostics, Is.Empty);
    }

    [Test]
    public void Read_BasicVp8LReturnsLosslessAlphaFeature()
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("VP8L", NewVp8LPayload(17, 33, true, 0)));

        WebpInfo info = WebpInfoReader.Read(WebpRiffWriter.Write("WEBP", chunks));

        Assert.That(info.Width, Is.EqualTo(17));
        Assert.That(info.Height, Is.EqualTo(33));
        Assert.That(info.ContainerKind, Is.EqualTo(WebpContainerKind.SimpleLossless));
        Assert.That(info.BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossless));
        Assert.That(info.HasAlpha, Is.True);
        Assert.That(info.Chunks, Has.Count.EqualTo(1));
        Assert.That(info.Diagnostics, Is.Empty);
    }

    [Test]
    public void Read_ExtendedStillReturnsVp8XFeaturesMetadataPresenceAndChunkTable()
    {
        byte flags = (byte)(
            WebpConstants.Vp8XFeatureIccProfile
            | WebpConstants.Vp8XFeatureAlpha
            | WebpConstants.Vp8XFeatureExif
            | WebpConstants.Vp8XFeatureXmp);
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("VP8X", NewVp8XPayload(flags, 640, 480)));
        chunks.Add(new WebpRiffChunk("ICCP", new byte[] { 1 }));
        chunks.Add(new WebpRiffChunk("ALPH", new byte[] { 2, 3 }));
        chunks.Add(new WebpRiffChunk("VP8 ", new byte[] { 4, 5, 6 }));
        chunks.Add(new WebpRiffChunk("EXIF", new byte[] { 7 }));
        chunks.Add(new WebpRiffChunk("XMP ", new byte[] { 8 }));
        chunks.Add(new WebpRiffChunk("zzzz", new byte[] { 9 }));

        WebpInfo info = WebpInfoReader.Read(WebpRiffWriter.Write("WEBP", chunks));

        Assert.That(info.Width, Is.EqualTo(640));
        Assert.That(info.Height, Is.EqualTo(480));
        Assert.That(info.ContainerKind, Is.EqualTo(WebpContainerKind.ExtendedStill));
        Assert.That(info.BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossy));
        Assert.That(info.HasAlpha, Is.True);
        Assert.That(info.HasAnimation, Is.False);
        Assert.That(info.HasIccProfile, Is.True);
        Assert.That(info.HasExif, Is.True);
        Assert.That(info.HasXmp, Is.True);
        Assert.That(info.DeclaredFeatureFlags, Is.EqualTo(
            WebpVp8XFeatureFlags.IccProfile
            | WebpVp8XFeatureFlags.Alpha
            | WebpVp8XFeatureFlags.Exif
            | WebpVp8XFeatureFlags.Xmp));
        Assert.That(info.HasReservedFeatureBits, Is.False);
        Assert.That(info.HasReservedBytes, Is.False);
        Assert.That(info.Chunks, Has.Count.EqualTo(7));
        Assert.That(info.Chunks[6].FourCc, Is.EqualTo("zzzz"));
        Assert.That(info.Chunks[6].IsKnown, Is.False);
        Assert.That(info.Frames, Is.Empty);
        Assert.That(info.Diagnostics, Is.Empty);
    }

    [Test]
    public void Read_AnimatedContainerReturnsFrameTableWithoutEncodedPayloads()
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("VP8X", NewVp8XPayload(WebpConstants.Vp8XFeatureAnimation, 100, 90)));
        chunks.Add(new WebpRiffChunk("ANIM", NewAnimPayload(0x11223344U, 3)));
        chunks.Add(new WebpRiffChunk("ANMF", NewAnmfPayload(2, 3, 11, 12, 75, 0x03, true, "VP8 ", new byte[] { 1, 2 })));

        WebpInfo info = WebpInfoReader.Read(WebpRiffWriter.Write("WEBP", chunks));

        Assert.That(info.Width, Is.EqualTo(100));
        Assert.That(info.Height, Is.EqualTo(90));
        Assert.That(info.ContainerKind, Is.EqualTo(WebpContainerKind.ExtendedAnimation));
        Assert.That(info.BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossy));
        Assert.That(info.HasAlpha, Is.True);
        Assert.That(info.HasAnimation, Is.True);
        Assert.That(info.LoopCount, Is.EqualTo(3));
        Assert.That(info.BackgroundColorBgra, Is.EqualTo(0x11223344U));
        Assert.That(info.DeclaredFeatureFlags, Is.EqualTo(WebpVp8XFeatureFlags.Animation));
        Assert.That(info.Chunks, Has.Count.EqualTo(3));
        Assert.That(info.Frames, Has.Count.EqualTo(1));
        Assert.That(info.Frames[0].X, Is.EqualTo(4));
        Assert.That(info.Frames[0].Y, Is.EqualTo(6));
        Assert.That(info.Frames[0].Width, Is.EqualTo(11));
        Assert.That(info.Frames[0].Height, Is.EqualTo(12));
        Assert.That(info.Frames[0].DurationMilliseconds, Is.EqualTo(75));
        Assert.That(info.Frames[0].DisposeMode, Is.EqualTo(WebpFrameDisposeMode.DisposeToBackground));
        Assert.That(info.Frames[0].BlendMode, Is.EqualTo(WebpFrameBlendMode.Replace));
        Assert.That(info.Frames[0].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossy));
        Assert.That(info.Frames[0].EncodedBitstream, Is.Null);
        Assert.That(info.Frames[0].AlphaData, Is.Null);
        Assert.That(info.Frames[0].Pixels, Is.Null);
        Assert.That(info.Frames[0].UnknownChunks, Is.Empty);
        Assert.That(info.Diagnostics, Is.Empty);
    }

    [Test]
    public void Read_InvalidInputReturnsDiagnostics()
    {
        byte[] data = new byte[] { 1, 2, 3 };

        WebpInfo info = WebpInfoReader.Read(data);

        Assert.That(info.ContainerKind, Is.EqualTo(WebpContainerKind.Unknown));
        Assert.That(info.BitstreamKind, Is.EqualTo(WebpBitstreamKind.Unknown));
        Assert.That(info.Chunks, Is.Empty);
        Assert.That(info.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(info.Diagnostics[0].Severity, Is.EqualTo(WebpDiagnosticSeverity.Error));
        Assert.That(info.Diagnostics[0].Message, Does.Contain("truncated"));
    }

    [Test]
    public void Read_UsesStrictnessFromOptions()
    {
        byte[] vp8x = NewVp8XPayload(0x40, 1, 1);
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("VP8X", vp8x));
        WebpDecodeOptions options = new WebpDecodeOptions();
        options.Strictness = WebpStrictness.Permissive;

        WebpInfo info = WebpInfoReader.Read(WebpRiffWriter.Write("WEBP", chunks), options);

        Assert.That(info.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(info.Diagnostics[0].Severity, Is.EqualTo(WebpDiagnosticSeverity.Warning));
        Assert.That(info.Diagnostics[0].FourCc, Is.EqualTo("VP8X"));
        Assert.That(info.DeclaredFeatureFlags, Is.EqualTo(WebpVp8XFeatureFlags.None));
        Assert.That(info.ReservedFeatureBits, Is.EqualTo(0x40));
        Assert.That(info.HasReservedFeatureBits, Is.True);
        Assert.That(info.HasReservedBytes, Is.False);
    }

    [Test]
    public void Read_ExposesReservedVp8XBytesThroughPublicReader()
    {
        byte[] vp8x = NewVp8XPayload(WebpConstants.Vp8XFeatureAlpha, 2, 3);
        vp8x[2] = 7;
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("VP8X", vp8x));
        chunks.Add(new WebpRiffChunk("ALPH", new byte[] { 0 }));
        chunks.Add(new WebpRiffChunk("VP8 ", new byte[] { 1 }));
        IWebpInfoReader reader = new WebpCodec();
        WebpDecodeOptions options = new WebpDecodeOptions();
        options.Strictness = WebpStrictness.Permissive;

        WebpInfo info = reader.Read(WebpRiffWriter.Write("WEBP", chunks), options);

        Assert.That(info.DeclaredFeatureFlags, Is.EqualTo(WebpVp8XFeatureFlags.Alpha));
        Assert.That(info.ReservedFeatureBits, Is.EqualTo(0));
        Assert.That(info.HasReservedFeatureBits, Is.False);
        Assert.That(info.HasReservedBytes, Is.True);
        Assert.That(info.Diagnostics, Has.Some.Matches<WebpDiagnostic>(diagnostic => diagnostic.Message.Contains("reserved bytes")));
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

    static byte[] NewVp8LPayload(int width, int height, bool alpha, int version)
    {
        byte[] payload = new byte[WebpConstants.Vp8LHeaderSize];
        uint bits = (uint)(width - 1);
        bits = bits | ((uint)(height - 1) << 14);
        if (alpha)
        {
            bits = bits | (1U << 28);
        }

        bits = bits | ((uint)version << 29);
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

        AppendSubchunk(payload, "frmx", new byte[] { 0x77 });
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
        data.Add((byte)fourCc[0]);
        data.Add((byte)fourCc[1]);
        data.Add((byte)fourCc[2]);
        data.Add((byte)fourCc[3]);
        WebpEndianWriter.AppendUInt32(data, (uint)payload.Length);
        WebpEndianWriter.AppendChunkPayload(data, payload);
    }
}
