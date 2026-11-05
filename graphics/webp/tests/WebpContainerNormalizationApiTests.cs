using webp;

namespace webp.Tests;

[TestFixture]
public class WebpContainerNormalizationApiTests
{
    [Test]
    public void NormalizeContainer_RepairsOrderingPaddingFlagsAndExtendedLayout()
    {
        byte[] imagePayload = NewVp8Payload(7, 9);
        byte[] vp8x = new byte[WebpConstants.Vp8XChunkPayloadSize];
        vp8x[0] = 0xFF;
        vp8x[1] = 7;
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk(WebpConstants.FourCcXmpString, new byte[] { 4 }));
        chunks.Add(new WebpRiffChunk(WebpConstants.FourCcVp8String, imagePayload));
        chunks.Add(new WebpRiffChunk(WebpConstants.FourCcExifString, new byte[] { 3 }));
        chunks.Add(new WebpRiffChunk(WebpConstants.FourCcIccProfileString, new byte[] { 1, 2, 3 }));
        chunks.Add(new WebpRiffChunk("uNk1", new byte[] { 5 }));
        chunks.Add(new WebpRiffChunk(WebpConstants.FourCcVp8XString, vp8x));
        byte[] source = WebpUnknownChunkMux.WritePreservingOrder(WebpConstants.FourCcWebpString, chunks);
        DirtyPadding(source, WebpConstants.FourCcXmpString);
        IWebpMux mux = new WebpCodec();

        WebpNormalizationResult result = mux.NormalizeContainer(source);

        Assert.That(result.IsNormalized, Is.True);
        Assert.That(result.WasChanged, Is.True);
        Assert.That(result.Diagnostics, Is.Empty);
        AssertStrictlyValid(result.Data);
        WebpExtendedContainerParseResult parsed = WebpExtendedContainerParser.Parse(result.Data, WebpStrictness.Strict);
        Assert.That(parsed.Vp8X.CanvasWidth, Is.EqualTo(7));
        Assert.That(parsed.Vp8X.CanvasHeight, Is.EqualTo(9));
        Assert.That(parsed.Vp8X.HasIccProfile, Is.True);
        Assert.That(parsed.Vp8X.HasExif, Is.True);
        Assert.That(parsed.Vp8X.HasXmp, Is.True);
        Assert.That(parsed.Vp8X.HasAlpha, Is.False);
        Assert.That(parsed.Vp8X.HasAnimation, Is.False);
        Assert.That(parsed.Vp8X.HasReservedFeatureBits, Is.False);
        Assert.That(parsed.Vp8X.HasReservedBytes, Is.False);
        Assert.That(GetFourCcs(result.Data), Is.EqualTo(new[] { "VP8X", "ICCP", "VP8 ", "EXIF", "XMP ", "uNk1" }));
        Assert.That(GetPayload(result.Data, WebpConstants.FourCcVp8String), Is.EqualTo(imagePayload));
        AssertAllPaddingIsCanonical(result.Data);
    }

    [Test]
    public void NormalizeContainer_AllInputFormsRemoveUnneededExtendedLayout()
    {
        byte[] imagePayload = NewVp8Payload(3, 4);
        WebpMetadata metadata = new WebpMetadata();
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk(WebpConstants.FourCcVp8XString, WebpContainerWriter.CreateVp8XPayload(3, 4, metadata, false, false)));
        chunks.Add(new WebpRiffChunk(WebpConstants.FourCcVp8String, imagePayload));
        byte[] source = WebpUnknownChunkMux.WritePreservingOrder(WebpConstants.FourCcWebpString, chunks);
        IWebpMux mux = new WebpCodec();
        MemoryStream stream = new MemoryStream(source);
        string path = NewTempPath();
        File.WriteAllBytes(path, source);

        try
        {
            WebpNormalizationResult arrayResult = mux.NormalizeContainer(source);
            WebpNormalizationResult spanResult = mux.NormalizeContainerSpan(source);
            WebpNormalizationResult streamResult = mux.NormalizeContainerStream(stream);
            WebpNormalizationResult fileResult = mux.NormalizeContainerFile(path);

            Assert.That(spanResult.Data, Is.EqualTo(arrayResult.Data));
            Assert.That(streamResult.Data, Is.EqualTo(arrayResult.Data));
            Assert.That(fileResult.Data, Is.EqualTo(arrayResult.Data));
            Assert.That(arrayResult.IsNormalized, Is.True);
            Assert.That(arrayResult.WasChanged, Is.True);
            Assert.That(GetFourCcs(arrayResult.Data), Is.EqualTo(new[] { "VP8 " }));
            Assert.That(GetPayload(arrayResult.Data, WebpConstants.FourCcVp8String), Is.EqualTo(imagePayload));
            AssertStrictlyValid(arrayResult.Data);
            Assert.That(stream.Position, Is.EqualTo(stream.Length));
            Assert.That(stream.CanRead, Is.True);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void NormalizeContainer_AddsExtendedLayoutForMetadataWithoutChangingImagePayload()
    {
        byte[] imagePayload = NewVp8Payload(5, 6);
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk(WebpConstants.FourCcExifString, new byte[] { 9, 8, 7 }));
        chunks.Add(new WebpRiffChunk(WebpConstants.FourCcVp8String, imagePayload));
        byte[] source = WebpUnknownChunkMux.WritePreservingOrder(WebpConstants.FourCcWebpString, chunks);

        WebpNormalizationResult result = new WebpCodec().NormalizeContainer(source);

        Assert.That(result.IsNormalized, Is.True);
        Assert.That(GetFourCcs(result.Data), Is.EqualTo(new[] { "VP8X", "VP8 ", "EXIF" }));
        Assert.That(GetPayload(result.Data, WebpConstants.FourCcVp8String), Is.EqualTo(imagePayload));
        WebpExtendedContainerParseResult parsed = WebpExtendedContainerParser.Parse(result.Data, WebpStrictness.Strict);
        Assert.That(parsed.Vp8X.HasExif, Is.True);
        Assert.That(parsed.Vp8X.CanvasWidth, Is.EqualTo(5));
        Assert.That(parsed.Vp8X.CanvasHeight, Is.EqualTo(6));
        AssertStrictlyValid(result.Data);
    }

    [Test]
    public void NormalizeContainer_RepairsMissingFinalPaddingAndRiffLength()
    {
        byte[] imagePayload = NewVp8Payload(2, 3);
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk(WebpConstants.FourCcVp8String, imagePayload));
        chunks.Add(new WebpRiffChunk("odd1", new byte[] { 7 }));
        byte[] complete = WebpUnknownChunkMux.WritePreservingOrder(WebpConstants.FourCcWebpString, chunks);
        byte[] truncated = new byte[complete.Length - 1];
        Array.Copy(complete, truncated, truncated.Length);

        WebpNormalizationResult result = new WebpCodec().NormalizeContainer(truncated);

        Assert.That(result.IsNormalized, Is.True);
        Assert.That(result.WasChanged, Is.True);
        Assert.That(GetPayload(result.Data, "odd1"), Is.EqualTo(new byte[] { 7 }));
        Assert.That(GetPayload(result.Data, WebpConstants.FourCcVp8String), Is.EqualTo(imagePayload));
        AssertStrictlyValid(result.Data);
        AssertAllPaddingIsCanonical(result.Data);
    }

    [Test]
    public void NormalizeContainer_RepairsAnimationOrderFlagsAndFrameSubchunkPadding()
    {
        WebpFrame frame = new WebpFrame();
        frame.Width = 2;
        frame.Height = 2;
        frame.DurationMilliseconds = 12;
        frame.BitstreamKind = WebpBitstreamKind.Lossy;
        frame.EncodedBitstream = NewVp8Payload(2, 2);
        frame.AlphaData = new byte[] { 0, 1, 2, 3, 4 };
        frame.UnknownChunks.Add(new WebpUnknownChunk("fr01", new byte[] { 6 }));
        List<WebpFrame> frames = new List<WebpFrame>();
        frames.Add(frame);
        byte[] canonical = WebpContainerWriter.WriteAnimated(frames, 2, 2, 3, 0xFF112233U);
        List<WebpRiffChunk> chunks = CopyRiffChunks(canonical);
        WebpRiffChunk anmf = FindRiffChunk(chunks, WebpConstants.FourCcAnimationFrameString);
        DirtyFramePadding(anmf.Payload, "fr01");
        WebpRiffChunk vp8x = FindRiffChunk(chunks, WebpConstants.FourCcVp8XString);
        vp8x.Payload[0] = 0;
        vp8x.Payload[1] = 9;
        List<WebpRiffChunk> disordered = new List<WebpRiffChunk>();
        disordered.Add(anmf);
        disordered.Add(FindRiffChunk(chunks, WebpConstants.FourCcAnimString));
        disordered.Add(vp8x);
        byte[] source = WebpUnknownChunkMux.WritePreservingOrder(WebpConstants.FourCcWebpString, disordered);

        WebpNormalizationResult result = new WebpCodec().NormalizeContainer(source);

        Assert.That(result.IsNormalized, Is.True);
        Assert.That(result.WasChanged, Is.True);
        AssertStrictlyValid(result.Data);
        Assert.That(GetFourCcs(result.Data), Is.EqualTo(new[] { "VP8X", "ANIM", "ANMF" }));
        WebpExtendedContainerParseResult parsed = WebpExtendedContainerParser.Parse(result.Data, WebpStrictness.Strict);
        Assert.That(parsed.Vp8X.HasAnimation, Is.True);
        Assert.That(parsed.Vp8X.HasAlpha, Is.True);
        Assert.That(parsed.Vp8X.HasReservedBytes, Is.False);
        Assert.That(parsed.Frames[0].EncodedBitstream, Is.EqualTo(frame.EncodedBitstream));
        Assert.That(parsed.Frames[0].AlphaData, Is.EqualTo(frame.AlphaData));
        Assert.That(parsed.Frames[0].UnknownChunks[0].Data, Is.EqualTo(new byte[] { 6 }));
        AssertFramePaddingIsCanonical(GetPayload(result.Data, WebpConstants.FourCcAnimationFrameString));
    }

    [Test]
    public void NormalizeContainer_ReturnsOriginalDataAndDiagnosticsWhenRepairIsAmbiguous()
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk(WebpConstants.FourCcVp8String, NewVp8Payload(1, 1)));
        chunks.Add(new WebpRiffChunk(WebpConstants.FourCcVp8String, NewVp8Payload(2, 2)));
        byte[] source = WebpUnknownChunkMux.WritePreservingOrder(WebpConstants.FourCcWebpString, chunks);

        WebpNormalizationResult result = new WebpCodec().NormalizeContainerSpan(source);

        Assert.That(result.IsNormalized, Is.False);
        Assert.That(result.WasChanged, Is.False);
        Assert.That(result.Data, Is.EqualTo(source));
        Assert.That(result.Diagnostics, Is.Not.Empty);
        Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(WebpDiagnosticSeverity.Error));
        Assert.That(result.Diagnostics[0].Message, Does.Contain("exactly one"));
    }

    static void AssertStrictlyValid(byte[] data)
    {
        WebpDecodeOptions options = new WebpDecodeOptions();
        options.Strictness = WebpStrictness.Strict;
        WebpValidationResult validation = new WebpCodec().Validate(data, options);
        Assert.That(validation.IsValid, Is.True, JoinDiagnostics(validation.Diagnostics));
    }

    static string JoinDiagnostics(List<WebpDiagnostic> diagnostics)
    {
        List<string> messages = new List<string>();
        int index = 0;
        while (index < diagnostics.Count)
        {
            messages.Add(diagnostics[index].Message);
            index = index + 1;
        }

        return string.Join("; ", messages);
    }

    static List<string> GetFourCcs(byte[] data)
    {
        WebpRiffParseResult riff = WebpRiffParser.Parse(data);
        List<string> names = new List<string>();
        int index = 0;
        while (index < riff.Chunks.Count)
        {
            names.Add(riff.Chunks[index].FourCc);
            index = index + 1;
        }

        return names;
    }

    static byte[] GetPayload(byte[] data, string fourCc)
    {
        WebpRiffParseResult riff = WebpRiffParser.Parse(data);
        int index = 0;
        while (index < riff.Chunks.Count)
        {
            WebpChunkInfo chunk = riff.Chunks[index];
            if (chunk.FourCc == fourCc)
            {
                return WebpEndianReader.ReadSlice(data, (int)chunk.PayloadOffset, (int)chunk.PayloadLength).ToArray();
            }

            index = index + 1;
        }

        throw new AssertionException("chunk not found: " + fourCc);
    }

    static void DirtyPadding(byte[] data, string fourCc)
    {
        WebpRiffParseResult riff = WebpRiffParser.Parse(data);
        int index = 0;
        while (index < riff.Chunks.Count)
        {
            WebpChunkInfo chunk = riff.Chunks[index];
            if (chunk.FourCc == fourCc && chunk.HasPadding)
            {
                data[(int)chunk.PaddingOffset] = 0x7F;
                return;
            }

            index = index + 1;
        }

        throw new AssertionException("padded chunk not found: " + fourCc);
    }

    static void AssertAllPaddingIsCanonical(byte[] data)
    {
        WebpRiffParseResult riff = WebpRiffParser.Parse(data);
        int index = 0;
        while (index < riff.Chunks.Count)
        {
            WebpChunkInfo chunk = riff.Chunks[index];
            if (chunk.HasPadding)
            {
                Assert.That(data[(int)chunk.PaddingOffset], Is.Zero);
            }

            index = index + 1;
        }
    }

    static List<WebpRiffChunk> CopyRiffChunks(byte[] data)
    {
        WebpRiffParseResult riff = WebpRiffParser.Parse(data);
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        int index = 0;
        while (index < riff.Chunks.Count)
        {
            WebpChunkInfo chunk = riff.Chunks[index];
            chunks.Add(new WebpRiffChunk(chunk.FourCc, WebpEndianReader.ReadSlice(data, (int)chunk.PayloadOffset, (int)chunk.PayloadLength).ToArray()));
            index = index + 1;
        }

        return chunks;
    }

    static WebpRiffChunk FindRiffChunk(List<WebpRiffChunk> chunks, string fourCc)
    {
        int index = 0;
        while (index < chunks.Count)
        {
            if (chunks[index].FourCc == fourCc)
            {
                return chunks[index];
            }

            index = index + 1;
        }

        throw new AssertionException("chunk not found: " + fourCc);
    }

    static void DirtyFramePadding(byte[] payload, string fourCc)
    {
        int offset = WebpConstants.AnimFrameHeaderSize;
        while (offset < payload.Length)
        {
            string current = WebpRiffParser.ReadFourCc(payload, offset);
            uint length = WebpEndianReader.ReadUInt32(payload, offset + WebpConstants.FourCcByteCount);
            int payloadEnd = offset + WebpConstants.ChunkHeaderSize + (int)length;
            if (current == fourCc && (length & 1U) != 0)
            {
                payload[payloadEnd] = 0x6D;
                return;
            }

            offset = payloadEnd + (int)(length & 1U);
        }

        throw new AssertionException("padded frame chunk not found: " + fourCc);
    }

    static void AssertFramePaddingIsCanonical(byte[] payload)
    {
        int offset = WebpConstants.AnimFrameHeaderSize;
        while (offset < payload.Length)
        {
            uint length = WebpEndianReader.ReadUInt32(payload, offset + WebpConstants.FourCcByteCount);
            int payloadEnd = offset + WebpConstants.ChunkHeaderSize + (int)length;
            if ((length & 1U) != 0)
            {
                Assert.That(payload[payloadEnd], Is.Zero);
            }

            offset = payloadEnd + (int)(length & 1U);
        }
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

    static string NewTempPath()
    {
        return Path.Combine(TestContext.CurrentContext.WorkDirectory, "normalize-" + Guid.NewGuid().ToString("N") + ".webp");
    }
}
