using webp;

namespace webp.Tests;

[TestFixture]
public class BasicContainerParserTests
{
    [Test]
    public void Parse_BasicVp8ContainerReturnsLossyDimensions()
    {
        byte[] payload = NewVp8Payload(320, 240);
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("VP8 ", payload));

        WebpBasicContainerParseResult result = WebpBasicContainerParser.Parse(WebpRiffWriter.Write("WEBP", chunks), WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.ContainerKind, Is.EqualTo(WebpContainerKind.SimpleLossy));
        Assert.That(result.BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossy));
        Assert.That(result.Width, Is.EqualTo(320));
        Assert.That(result.Height, Is.EqualTo(240));
        Assert.That(result.HasAlpha, Is.False);
        Assert.That(result.ImagePayload, Is.EqualTo(payload));
        Assert.That(result.ImagePayload, Is.Not.SameAs(payload));
        Assert.That(result.ImageChunk.FourCc, Is.EqualTo("VP8 "));
    }

    [Test]
    public void Parse_BasicVp8ContainerAcceptsScaleAndFrameTagBits()
    {
        byte[] payload = NewVp8Payload(320, 240);
        payload[0] = 0x14;
        WebpEndianWriter.WriteUInt16(payload, 6, (ushort)(320 | (2 << 14)));
        WebpEndianWriter.WriteUInt16(payload, 8, (ushort)(240 | (3 << 14)));

        WebpBasicContainerParseResult result = ParseVp8Payload(payload);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Width, Is.EqualTo(320));
        Assert.That(result.Height, Is.EqualTo(240));
    }

    [Test]
    public void Parse_BasicVp8LContainerReturnsLosslessDimensionsAndAlphaHint()
    {
        byte[] payload = NewVp8LPayload(17, 33, true, 0);
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("VP8L", payload));

        WebpBasicContainerParseResult result = WebpBasicContainerParser.Parse(WebpRiffWriter.Write("WEBP", chunks), WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.ContainerKind, Is.EqualTo(WebpContainerKind.SimpleLossless));
        Assert.That(result.BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossless));
        Assert.That(result.Width, Is.EqualTo(17));
        Assert.That(result.Height, Is.EqualTo(33));
        Assert.That(result.HasAlpha, Is.True);
        Assert.That(result.ImagePayload, Is.EqualTo(payload));
        Assert.That(result.ImageChunk.FourCc, Is.EqualTo("VP8L"));
    }

    [Test]
    public void Parse_BasicContainerAllowsUnknownChunksAndKeepsChunkTable()
    {
        byte[] payload = NewVp8Payload(2, 3);
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("zzzz", new byte[] { 9 }));
        chunks.Add(new WebpRiffChunk("VP8 ", payload));

        WebpBasicContainerParseResult result = WebpBasicContainerParser.Parse(WebpRiffWriter.Write("WEBP", chunks), WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Chunks, Has.Count.EqualTo(2));
        Assert.That(result.Chunks[0].FourCc, Is.EqualTo("VP8 "));
        Assert.That(result.Chunks[0].IsKnown, Is.True);
        Assert.That(result.Chunks[1].FourCc, Is.EqualTo("zzzz"));
        Assert.That(result.Chunks[1].IsKnown, Is.False);
        Assert.That(result.ImagePayload, Is.EqualTo(payload));
    }

    [Test]
    public void Parse_RejectsVp8XExtendedContainer()
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("VP8X", new byte[WebpConstants.Vp8XChunkPayloadSize]));
        chunks.Add(new WebpRiffChunk("VP8 ", NewVp8Payload(1, 1)));

        WebpBasicContainerParseResult result = WebpBasicContainerParser.Parse(WebpRiffWriter.Write("WEBP", chunks), WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics[0].FourCc, Is.EqualTo("VP8X"));
        Assert.That(result.Diagnostics[0].Message, Does.Contain("not allowed"));
    }

    [Test]
    public void Parse_RejectsMissingAndMultipleImageChunks()
    {
        WebpBasicContainerParseResult missing = WebpBasicContainerParser.Parse(WebpRiffWriter.Write("WEBP", new List<WebpRiffChunk>()), WebpStrictness.Strict);
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("VP8 ", NewVp8Payload(1, 1)));
        chunks.Add(new WebpRiffChunk("VP8L", NewVp8LPayload(1, 1, false, 0)));

        WebpBasicContainerParseResult multiple = WebpBasicContainerParser.Parse(WebpRiffWriter.Write("WEBP", chunks), WebpStrictness.Strict);

        Assert.That(missing.IsValid, Is.False);
        Assert.That(missing.Diagnostics[0].Message, Does.Contain("missing"));
        Assert.That(multiple.IsValid, Is.False);
        Assert.That(multiple.Diagnostics[0].Message, Does.Contain("exactly one"));
    }

    [Test]
    public void Parse_RejectsMalformedVp8Headers()
    {
        byte[] badSignature = NewVp8Payload(1, 1);
        badSignature[3] = 0;
        byte[] zeroWidth = NewVp8Payload(1, 1);
        WebpEndianWriter.WriteUInt16(zeroWidth, 6, 0);

        WebpBasicContainerParseResult signatureResult = ParseVp8Payload(badSignature);
        WebpBasicContainerParseResult dimensionResult = ParseVp8Payload(zeroWidth);

        Assert.That(signatureResult.IsValid, Is.False);
        Assert.That(signatureResult.Diagnostics[0].Message, Does.Contain("signature"));
        Assert.That(dimensionResult.IsValid, Is.False);
        Assert.That(dimensionResult.Diagnostics[0].Message, Does.Contain("dimensions"));
    }

    [Test]
    public void Parse_RejectsMalformedVp8LHeaders()
    {
        byte[] badSignature = NewVp8LPayload(1, 1, false, 0);
        badSignature[0] = 0;
        byte[] badVersion = NewVp8LPayload(1, 1, false, 1);

        WebpBasicContainerParseResult signatureResult = ParseVp8LPayload(badSignature);
        WebpBasicContainerParseResult versionResult = ParseVp8LPayload(badVersion);

        Assert.That(signatureResult.IsValid, Is.False);
        Assert.That(signatureResult.Diagnostics[0].Message, Does.Contain("signature"));
        Assert.That(versionResult.IsValid, Is.False);
        Assert.That(versionResult.Diagnostics[0].Message, Does.Contain("version"));
    }

    [Test]
    public void Parse_RejectsExtendedMetadataChunksInBasicContainer()
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("VP8 ", NewVp8Payload(1, 1)));
        chunks.Add(new WebpRiffChunk("EXIF", new byte[] { 1, 2 }));

        WebpBasicContainerParseResult result = WebpBasicContainerParser.Parse(WebpRiffWriter.Write("WEBP", chunks), WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics[0].FourCc, Is.EqualTo("EXIF"));
        Assert.That(result.Diagnostics[0].Message, Does.Contain("extended"));
    }

    static WebpBasicContainerParseResult ParseVp8Payload(byte[] payload)
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("VP8 ", payload));
        return WebpBasicContainerParser.Parse(WebpRiffWriter.Write("WEBP", chunks), WebpStrictness.Strict);
    }

    static WebpBasicContainerParseResult ParseVp8LPayload(byte[] payload)
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("VP8L", payload));
        return WebpBasicContainerParser.Parse(WebpRiffWriter.Write("WEBP", chunks), WebpStrictness.Strict);
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
}
