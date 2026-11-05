using webp;

namespace webp.Tests;

[TestFixture]
public class UnknownChunkMuxTests
{
    [Test]
    public void Enumerate_ReturnsTopLevelUnknownChunksInFileOrderAsCopies()
    {
        byte[] data = NewContainerWithUnknowns();

        List<WebpUnknownChunk> unknownChunks = WebpUnknownChunkMux.Enumerate(data);
        data[data.Length - 2] = 0xFF;

        Assert.That(unknownChunks, Has.Count.EqualTo(2));
        Assert.That(unknownChunks[0].FourCc, Is.EqualTo("zzzz"));
        Assert.That(unknownChunks[0].Data, Is.EqualTo(new byte[] { 9 }));
        Assert.That(unknownChunks[1].FourCc, Is.EqualTo("aaaa"));
        Assert.That(unknownChunks[1].Data, Is.EqualTo(new byte[] { 8, 7 }));
    }

    [Test]
    public void Preserve_RewritesContainerWithoutDroppingUnknownChunksOrChangingOrder()
    {
        byte[] data = NewContainerWithUnknowns();

        byte[] preserved = WebpUnknownChunkMux.Preserve(data);
        WebpRiffParseResult parsed = WebpRiffParser.Parse(preserved);

        Assert.That(parsed.IsValid, Is.True);
        Assert.That(parsed.Chunks, Has.Count.EqualTo(4));
        Assert.That(parsed.Chunks[0].FourCc, Is.EqualTo("zzzz"));
        Assert.That(parsed.Chunks[1].FourCc, Is.EqualTo("VP8 "));
        Assert.That(parsed.Chunks[2].FourCc, Is.EqualTo("aaaa"));
        Assert.That(parsed.Chunks[3].FourCc, Is.EqualTo("EXIF"));
    }

    [Test]
    public void Strip_RemovesUnknownChunksAndPreservesKnownChunks()
    {
        byte[] data = NewContainerWithUnknowns();

        byte[] stripped = WebpUnknownChunkMux.Strip(data);
        WebpRiffParseResult parsed = WebpRiffParser.Parse(stripped);
        WebpMetadata metadata = WebpMetadataMux.Extract(stripped);

        Assert.That(WebpUnknownChunkMux.Enumerate(stripped), Is.Empty);
        Assert.That(parsed.Chunks, Has.Count.EqualTo(2));
        Assert.That(parsed.Chunks[0].FourCc, Is.EqualTo("VP8 "));
        Assert.That(parsed.Chunks[1].FourCc, Is.EqualTo("EXIF"));
        Assert.That(metadata.Exif, Is.EqualTo(new byte[] { 4 }));
    }

    [Test]
    public void Preserve_AnimatedContainerKeepsTopLevelAndFrameUnknownChunks()
    {
        byte[] data = NewAnimatedContainerWithUnknowns();
        byte[] beforeFrame = GetChunkPayload(data, "ANMF", 0);

        byte[] preserved = WebpUnknownChunkMux.Preserve(data);
        WebpExtendedContainerParseResult parsed = WebpExtendedContainerParser.Parse(preserved, WebpStrictness.Strict);
        List<WebpUnknownChunk> unknownChunks = WebpUnknownChunkMux.Enumerate(preserved);

        Assert.That(parsed.IsValid, Is.True);
        Assert.That(parsed.Vp8X.HasAnimation, Is.True);
        Assert.That(unknownChunks, Has.Count.EqualTo(1));
        Assert.That(unknownChunks[0].FourCc, Is.EqualTo("top1"));
        Assert.That(unknownChunks[0].Data, Is.EqualTo(new byte[] { 8, 9 }));
        Assert.That(parsed.UnknownChunks, Has.Count.EqualTo(1));
        Assert.That(parsed.UnknownChunks[0].FourCc, Is.EqualTo("top1"));
        Assert.That(parsed.Frames, Has.Count.EqualTo(1));
        Assert.That(parsed.Frames[0].UnknownChunks, Has.Count.EqualTo(1));
        Assert.That(parsed.Frames[0].UnknownChunks[0].FourCc, Is.EqualTo("frmx"));
        Assert.That(parsed.Frames[0].UnknownChunks[0].Data, Is.EqualTo(new byte[] { 5, 6, 7 }));
        Assert.That(GetChunkPayload(preserved, "ANMF", 0), Is.EqualTo(beforeFrame));
    }

    [Test]
    public void Strip_AnimatedContainerRemovesTopLevelUnknownsButPreservesFrameUnknownChunks()
    {
        byte[] data = NewAnimatedContainerWithUnknowns();
        byte[] beforeFrame = GetChunkPayload(data, "ANMF", 0);

        byte[] stripped = WebpUnknownChunkMux.Strip(data);
        WebpExtendedContainerParseResult parsed = WebpExtendedContainerParser.Parse(stripped, WebpStrictness.Strict);

        Assert.That(parsed.IsValid, Is.True);
        Assert.That(WebpUnknownChunkMux.Enumerate(stripped), Is.Empty);
        Assert.That(parsed.UnknownChunks, Is.Empty);
        Assert.That(parsed.Metadata.Exif, Is.EqualTo(new byte[] { 4 }));
        Assert.That(parsed.Frames, Has.Count.EqualTo(1));
        Assert.That(parsed.Frames[0].UnknownChunks, Has.Count.EqualTo(1));
        Assert.That(parsed.Frames[0].UnknownChunks[0].FourCc, Is.EqualTo("frmx"));
        Assert.That(parsed.Frames[0].UnknownChunks[0].Data, Is.EqualTo(new byte[] { 5, 6, 7 }));
        Assert.That(GetChunkPayload(stripped, "ANMF", 0), Is.EqualTo(beforeFrame));
    }

    [Test]
    public void Replace_RemovesOldUnknownChunksAndAppendsReplacementChunks()
    {
        byte[] data = NewContainerWithUnknowns();
        List<WebpUnknownChunk> replacements = new List<WebpUnknownChunk>();
        replacements.Add(new WebpUnknownChunk("one1", new byte[] { 1 }));
        replacements.Add(new WebpUnknownChunk("two2", new byte[] { 2, 3 }));

        byte[] replaced = WebpUnknownChunkMux.Replace(data, replacements);
        List<WebpUnknownChunk> unknownChunks = WebpUnknownChunkMux.Enumerate(replaced);
        WebpRiffParseResult parsed = WebpRiffParser.Parse(replaced);

        Assert.That(unknownChunks, Has.Count.EqualTo(2));
        Assert.That(unknownChunks[0].FourCc, Is.EqualTo("one1"));
        Assert.That(unknownChunks[0].Data, Is.EqualTo(new byte[] { 1 }));
        Assert.That(unknownChunks[1].FourCc, Is.EqualTo("two2"));
        Assert.That(unknownChunks[1].Data, Is.EqualTo(new byte[] { 2, 3 }));
        Assert.That(parsed.Chunks[0].FourCc, Is.EqualTo("VP8 "));
        Assert.That(parsed.Chunks[1].FourCc, Is.EqualTo("EXIF"));
        Assert.That(parsed.Chunks[2].FourCc, Is.EqualTo("one1"));
        Assert.That(parsed.Chunks[3].FourCc, Is.EqualTo("two2"));
    }

    [Test]
    public void Replace_CopiesReplacementUnknownPayloads()
    {
        byte[] data = NewContainerWithUnknowns();
        byte[] payload = new byte[] { 1, 2, 3 };
        List<WebpUnknownChunk> replacements = new List<WebpUnknownChunk>();
        replacements.Add(new WebpUnknownChunk("copy", payload));

        byte[] replaced = WebpUnknownChunkMux.Replace(data, replacements);
        payload[0] = 99;

        List<WebpUnknownChunk> unknownChunks = WebpUnknownChunkMux.Enumerate(replaced);

        Assert.That(unknownChunks[0].Data, Is.EqualTo(new byte[] { 1, 2, 3 }));
    }

    [Test]
    public void Replace_RejectsKnownFourCcAndInvalidInputs()
    {
        byte[] data = NewContainerWithUnknowns();
        List<WebpUnknownChunk> known = new List<WebpUnknownChunk>();
        WebpUnknownChunk knownChunk = new WebpUnknownChunk("test", new byte[] { 1 });
        knownChunk.FourCc = "VP8 ";
        known.Add(knownChunk);
        List<WebpUnknownChunk> invalid = new List<WebpUnknownChunk>();
        WebpUnknownChunk invalidChunk = new WebpUnknownChunk("test", new byte[] { 1 });
        invalidChunk.FourCc = "bad";
        invalid.Add(invalidChunk);

        Assert.Throws<ArgumentException>(() => WebpUnknownChunkMux.Replace(data, known));
        Assert.Throws<ArgumentException>(() => WebpUnknownChunkMux.Replace(data, invalid));
        Assert.Throws<ArgumentException>(() => WebpUnknownChunkMux.Enumerate(new byte[] { 1, 2, 3 }));
    }

    static byte[] NewContainerWithUnknowns()
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("zzzz", new byte[] { 9 }));
        chunks.Add(new WebpRiffChunk("VP8 ", NewVp8Payload(2, 3)));
        chunks.Add(new WebpRiffChunk("aaaa", new byte[] { 8, 7 }));
        chunks.Add(new WebpRiffChunk("EXIF", new byte[] { 4 }));
        return WriteWithoutSorting("WEBP", chunks);
    }

    static byte[] NewAnimatedContainerWithUnknowns()
    {
        WebpImage image = new WebpImage();
        image.CanvasWidth = 4;
        image.CanvasHeight = 2;
        image.HasAnimation = true;
        image.Metadata.Exif = new byte[] { 4 };
        image.UnknownChunks.Add(new WebpUnknownChunk("top1", new byte[] { 8, 9 }));

        WebpFrame frame = NewFrame(NewVp8Payload(2, 2), WebpBitstreamKind.Lossy);
        frame.DurationMilliseconds = 25;
        frame.UnknownChunks.Add(new WebpUnknownChunk("frmx", new byte[] { 5, 6, 7 }));
        image.Frames.Add(frame);

        return WebpContainerWriter.WriteAnimated(image);
    }

    static WebpFrame NewFrame(byte[] payload, WebpBitstreamKind bitstreamKind)
    {
        WebpBasicContainerParseResult parsed = ParsePayload(payload, bitstreamKind);
        WebpFrame frame = new WebpFrame();
        frame.Width = parsed.Width;
        frame.Height = parsed.Height;
        frame.BitstreamKind = bitstreamKind;
        frame.EncodedBitstream = payload;
        return frame;
    }

    static WebpBasicContainerParseResult ParsePayload(byte[] payload, WebpBitstreamKind bitstreamKind)
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        if (bitstreamKind == WebpBitstreamKind.Lossy)
        {
            chunks.Add(new WebpRiffChunk("VP8 ", payload));
        }
        else
        {
            chunks.Add(new WebpRiffChunk("VP8L", payload));
        }

        return WebpBasicContainerParser.Parse(WebpRiffWriter.Write("WEBP", chunks), WebpStrictness.Strict);
    }

    static byte[] GetChunkPayload(byte[] data, string fourCc, int occurrence)
    {
        WebpRiffParseResult riff = WebpRiffParser.Parse(data);
        int seen = 0;
        int index = 0;
        while (index < riff.Chunks.Count)
        {
            WebpChunkInfo chunk = riff.Chunks[index];
            if (chunk.FourCc == fourCc)
            {
                if (seen == occurrence)
                {
                    return WebpEndianReader.ReadSlice(data, (int)chunk.PayloadOffset, (int)chunk.PayloadLength).ToArray();
                }

                seen = seen + 1;
            }

            index = index + 1;
        }

        throw new InvalidOperationException("chunk was not found");
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

    static byte[] WriteWithoutSorting(string formatFourCc, List<WebpRiffChunk> chunks)
    {
        long riffSize = WebpRiffWriter.ComputeRiffSize(chunks);
        List<byte> data = new List<byte>();
        AppendFourCc(data, "RIFF");
        WebpEndianWriter.AppendUInt32(data, (uint)riffSize);
        AppendFourCc(data, formatFourCc);

        int index = 0;
        while (index < chunks.Count)
        {
            WebpRiffChunk chunk = chunks[index];
            AppendFourCc(data, chunk.FourCc);
            WebpEndianWriter.AppendUInt32(data, (uint)chunk.Payload.Length);
            WebpEndianWriter.AppendChunkPayload(data, chunk.Payload);
            index = index + 1;
        }

        return data.ToArray();
    }

    static void AppendFourCc(List<byte> data, string fourCc)
    {
        data.Add((byte)fourCc[0]);
        data.Add((byte)fourCc[1]);
        data.Add((byte)fourCc[2]);
        data.Add((byte)fourCc[3]);
    }
}
