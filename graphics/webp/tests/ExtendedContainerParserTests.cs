using webp;

namespace webp.Tests;

[TestFixture]
public class ExtendedContainerParserTests
{
    [Test]
    public void Parse_ExtendedStillContainerExtractsVp8XMetadataAlphaImageAndUnknownChunks()
    {
        byte flags = (byte)(
            WebpConstants.Vp8XFeatureIccProfile
            | WebpConstants.Vp8XFeatureAlpha
            | WebpConstants.Vp8XFeatureExif
            | WebpConstants.Vp8XFeatureXmp);
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("zzzz", new byte[] { 9, 8 }));
        chunks.Add(new WebpRiffChunk("VP8 ", new byte[] { 1, 2, 3 }));
        chunks.Add(new WebpRiffChunk("ALPH", new byte[] { 4, 5 }));
        chunks.Add(new WebpRiffChunk("EXIF", new byte[] { 6 }));
        chunks.Add(new WebpRiffChunk("XMP ", new byte[] { 7 }));
        chunks.Add(new WebpRiffChunk("ICCP", new byte[] { 8 }));
        chunks.Add(new WebpRiffChunk("VP8X", NewVp8XPayload(flags, 640, 480)));

        WebpExtendedContainerParseResult result = WebpExtendedContainerParser.Parse(WebpRiffWriter.Write("WEBP", chunks), WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.HasVp8X, Is.True);
        Assert.That(result.Vp8X.CanvasWidth, Is.EqualTo(640));
        Assert.That(result.Vp8X.CanvasHeight, Is.EqualTo(480));
        Assert.That(result.Vp8X.HasIccProfile, Is.True);
        Assert.That(result.Vp8X.HasAlpha, Is.True);
        Assert.That(result.Vp8X.HasExif, Is.True);
        Assert.That(result.Vp8X.HasXmp, Is.True);
        Assert.That(result.Metadata.IccProfile, Is.EqualTo(new byte[] { 8 }));
        Assert.That(result.Metadata.Exif, Is.EqualTo(new byte[] { 6 }));
        Assert.That(result.Metadata.Xmp, Is.EqualTo(new byte[] { 7 }));
        Assert.That(result.AlphaData, Is.EqualTo(new byte[] { 4, 5 }));
        Assert.That(result.ImagePayload, Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(result.BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossy));
        Assert.That(result.UnknownChunks, Has.Count.EqualTo(1));
        Assert.That(result.UnknownChunks[0].FourCc, Is.EqualTo("zzzz"));
    }

    [Test]
    public void Parse_ExtendedLosslessStillExtractsVp8LImageWithoutAlphaSideChunk()
    {
        byte[] payload = new byte[] { WebpConstants.Vp8LSignature, 0, 0, 0, 0 };
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("VP8X", NewVp8XPayload(0, 1, 1)));
        chunks.Add(new WebpRiffChunk("VP8L", payload));

        WebpExtendedContainerParseResult result = WebpExtendedContainerParser.Parse(WebpRiffWriter.Write("WEBP", chunks), WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.ImagePayload, Is.EqualTo(payload));
        Assert.That(result.BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossless));
        Assert.That(result.AlphaData, Is.Null);
        Assert.That(result.Frames, Is.Empty);
        Assert.That(result.UnknownChunks, Is.Empty);
    }

    [Test]
    public void Parse_AnimatedContainerExtractsAnimationAndFrameSubchunks()
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("VP8X", NewVp8XPayload(WebpConstants.Vp8XFeatureAnimation, 100, 90)));
        chunks.Add(new WebpRiffChunk("ANIM", NewAnimPayload(0x11223344U, 3)));
        chunks.Add(new WebpRiffChunk("ANMF", NewAnmfPayload(2, 3, 11, 12, 75, 0x03, true, "VP8 ", new byte[] { 1, 2 })));

        WebpExtendedContainerParseResult result = WebpExtendedContainerParser.Parse(WebpRiffWriter.Write("WEBP", chunks), WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Vp8X.HasAnimation, Is.True);
        Assert.That(result.AnimationBackgroundColorBgra, Is.EqualTo(0x11223344U));
        Assert.That(result.AnimationLoopCount, Is.EqualTo(3));
        Assert.That(result.Frames, Has.Count.EqualTo(1));
        Assert.That(result.Frames[0].X, Is.EqualTo(4));
        Assert.That(result.Frames[0].Y, Is.EqualTo(6));
        Assert.That(result.Frames[0].Width, Is.EqualTo(11));
        Assert.That(result.Frames[0].Height, Is.EqualTo(12));
        Assert.That(result.Frames[0].DurationMilliseconds, Is.EqualTo(75));
        Assert.That(result.Frames[0].DisposeMode, Is.EqualTo(WebpFrameDisposeMode.DisposeToBackground));
        Assert.That(result.Frames[0].BlendMode, Is.EqualTo(WebpFrameBlendMode.Replace));
        Assert.That(result.Frames[0].AlphaData, Is.EqualTo(new byte[] { 0xAA }));
        Assert.That(result.Frames[0].EncodedBitstream, Is.EqualTo(new byte[] { 1, 2 }));
        Assert.That(result.Frames[0].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossy));
        Assert.That(result.Frames[0].UnknownChunks, Has.Count.EqualTo(1));
        Assert.That(result.Frames[0].UnknownChunks[0].FourCc, Is.EqualTo("frmx"));
    }

    [Test]
    public void Parse_RejectsExtendedContainerWithoutVp8X()
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("VP8 ", new byte[] { 1 }));

        WebpExtendedContainerParseResult result = WebpExtendedContainerParser.Parse(WebpRiffWriter.Write("WEBP", chunks), WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics[0].Message, Does.Contain("missing VP8X"));
    }

    [Test]
    public void Parse_RejectsDuplicateVp8XAndTopLevelImageChunks()
    {
        List<WebpRiffChunk> duplicateVp8X = new List<WebpRiffChunk>();
        duplicateVp8X.Add(new WebpRiffChunk("VP8X", NewVp8XPayload(0, 1, 1)));
        duplicateVp8X.Add(new WebpRiffChunk("VP8X", NewVp8XPayload(0, 1, 1)));
        duplicateVp8X.Add(new WebpRiffChunk("VP8 ", new byte[] { 1 }));
        List<WebpRiffChunk> duplicateImages = new List<WebpRiffChunk>();
        duplicateImages.Add(new WebpRiffChunk("VP8X", NewVp8XPayload(0, 1, 1)));
        duplicateImages.Add(new WebpRiffChunk("VP8 ", new byte[] { 1 }));
        duplicateImages.Add(new WebpRiffChunk("VP8L", new byte[] { WebpConstants.Vp8LSignature, 0, 0, 0, 0 }));

        WebpExtendedContainerParseResult duplicateVp8XResult = WebpExtendedContainerParser.Parse(WebpRiffWriter.Write("WEBP", duplicateVp8X), WebpStrictness.Strict);
        WebpExtendedContainerParseResult duplicateImageResult = WebpExtendedContainerParser.Parse(WebpRiffWriter.Write("WEBP", duplicateImages), WebpStrictness.Strict);

        Assert.That(duplicateVp8XResult.IsValid, Is.False);
        Assert.That(duplicateVp8XResult.Diagnostics, Has.Some.Matches<WebpDiagnostic>(diagnostic => diagnostic.Message.Contains("multiple VP8X")));
        Assert.That(duplicateImageResult.IsValid, Is.False);
        Assert.That(duplicateImageResult.Diagnostics, Has.Some.Matches<WebpDiagnostic>(diagnostic => diagnostic.Message.Contains("multiple top-level image")));
    }

    [Test]
    public void Parse_PropagatesVp8XReservedDiagnosticsAtAbsoluteOffsets()
    {
        byte[] vp8x = NewVp8XPayload(0x40, 1, 1);
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("VP8X", vp8x));

        WebpExtendedContainerParseResult result = WebpExtendedContainerParser.Parse(WebpRiffWriter.Write("WEBP", chunks), WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(WebpDiagnosticSeverity.Error));
        Assert.That(result.Diagnostics[0].FourCc, Is.EqualTo("VP8X"));
        Assert.That(result.Diagnostics[0].Offset, Is.EqualTo(20));
    }

    [Test]
    public void Parse_ReservedAnmfFlagsWarnInPermissiveMode()
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("VP8X", NewVp8XPayload(WebpConstants.Vp8XFeatureAnimation, 10, 10)));
        chunks.Add(new WebpRiffChunk("ANMF", NewAnmfPayload(0, 0, 1, 1, 1, 0x80, false, "VP8 ", new byte[] { 1 })));

        WebpExtendedContainerParseResult result = WebpExtendedContainerParser.Parse(WebpRiffWriter.Write("WEBP", chunks), WebpStrictness.Permissive);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(WebpDiagnosticSeverity.Warning));
        Assert.That(result.Diagnostics[0].FourCc, Is.EqualTo("ANMF"));
    }

    [Test]
    public void Parse_RejectsAnmfWithoutImageSubchunk()
    {
        List<byte> payload = NewAnmfHeader(0, 0, 1, 1, 1, 0);
        AppendSubchunk(payload, "frmx", new byte[] { 1 });
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("VP8X", NewVp8XPayload(WebpConstants.Vp8XFeatureAnimation, 10, 10)));
        chunks.Add(new WebpRiffChunk("ANMF", payload.ToArray()));

        WebpExtendedContainerParseResult result = WebpExtendedContainerParser.Parse(WebpRiffWriter.Write("WEBP", chunks), WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics[0].Message, Does.Contain("missing"));
    }

    [Test]
    public void Parse_RejectsMalformedAnmfSubchunks()
    {
        List<byte> truncatedPayload = NewAnmfHeader(0, 0, 1, 1, 1, 0);
        truncatedPayload.Add((byte)'f');
        truncatedPayload.Add((byte)'r');
        List<byte> badPaddingPayload = NewAnmfHeader(0, 0, 1, 1, 1, 0);
        AppendSubchunk(badPaddingPayload, "VP8 ", new byte[] { 1 });
        badPaddingPayload[badPaddingPayload.Count - 1] = 0x7F;
        List<WebpRiffChunk> truncatedChunks = new List<WebpRiffChunk>();
        truncatedChunks.Add(new WebpRiffChunk("VP8X", NewVp8XPayload(WebpConstants.Vp8XFeatureAnimation, 10, 10)));
        truncatedChunks.Add(new WebpRiffChunk("ANMF", truncatedPayload.ToArray()));
        List<WebpRiffChunk> badPaddingChunks = new List<WebpRiffChunk>();
        badPaddingChunks.Add(new WebpRiffChunk("VP8X", NewVp8XPayload(WebpConstants.Vp8XFeatureAnimation, 10, 10)));
        badPaddingChunks.Add(new WebpRiffChunk("ANMF", badPaddingPayload.ToArray()));

        WebpExtendedContainerParseResult truncated = WebpExtendedContainerParser.Parse(WebpRiffWriter.Write("WEBP", truncatedChunks), WebpStrictness.Strict);
        WebpExtendedContainerParseResult badPadding = WebpExtendedContainerParser.Parse(WebpRiffWriter.Write("WEBP", badPaddingChunks), WebpStrictness.Strict);

        Assert.That(truncated.IsValid, Is.False);
        Assert.That(truncated.Diagnostics[0].Message, Does.Contain("subchunk header"));
        Assert.That(badPadding.IsValid, Is.False);
        Assert.That(badPadding.Diagnostics, Has.Some.Matches<WebpDiagnostic>(diagnostic => diagnostic.Message.Contains("padding")));
    }

    [Test]
    public void Parse_RejectsAlphWithVp8LLosslessImage()
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("VP8X", NewVp8XPayload(WebpConstants.Vp8XFeatureAlpha, 10, 10)));
        chunks.Add(new WebpRiffChunk("ALPH", new byte[] { 1 }));
        chunks.Add(new WebpRiffChunk("VP8L", new byte[] { WebpConstants.Vp8LSignature, 0, 0, 0, 0 }));

        WebpExtendedContainerParseResult result = WebpExtendedContainerParser.Parse(WebpRiffWriter.Write("WEBP", chunks), WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics[0].Message, Does.Contain("ALPH"));
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
