using webp;

namespace webp.Tests;

[TestFixture]
public class WebpUnknownChunkMuxApiTests
{
    [Test]
    public void EnumerateUnknownChunks_AllInputFormsPreserveSourceOrderAndReturnCopies()
    {
        byte[] data = NewContainerWithUnknowns();
        IWebpMux mux = new WebpCodec();
        MemoryStream stream = new MemoryStream(data);
        string path = NewTempPath();
        File.WriteAllBytes(path, data);

        try
        {
            List<WebpUnknownChunk> arrayResult = mux.EnumerateUnknownChunks(data);
            List<WebpUnknownChunk> spanResult = mux.EnumerateUnknownChunksSpan(data);
            List<WebpUnknownChunk> streamResult = mux.EnumerateUnknownChunksStream(stream);
            List<WebpUnknownChunk> fileResult = mux.EnumerateUnknownChunksFile(path);
            data[20] = 99;

            AssertUnknownChunks(arrayResult);
            AssertUnknownChunks(spanResult);
            AssertUnknownChunks(streamResult);
            AssertUnknownChunks(fileResult);
            Assert.That(stream.Position, Is.EqualTo(stream.Length));
            Assert.That(stream.CanRead, Is.True);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void ReplaceUnknownChunks_AllInputFormsHonorPlacementAndReplacementOrder()
    {
        byte[] data = NewContainerWithUnknowns();
        List<WebpUnknownChunk> replacements = NewReplacements();
        IWebpMux mux = new WebpCodec();
        MemoryStream stream = new MemoryStream(data);
        string path = NewTempPath();
        File.WriteAllBytes(path, data);

        try
        {
            byte[] beforeArray = mux.ReplaceUnknownChunks(data, replacements, WebpUnknownChunkPlacement.BeforeRecognizedChunks);
            byte[] afterSpan = mux.ReplaceUnknownChunksSpan(data, replacements, WebpUnknownChunkPlacement.AfterRecognizedChunks);
            byte[] beforeStream = mux.ReplaceUnknownChunksStream(stream, replacements, WebpUnknownChunkPlacement.BeforeRecognizedChunks);
            byte[] afterFile = mux.ReplaceUnknownChunksFile(path, replacements, WebpUnknownChunkPlacement.AfterRecognizedChunks);

            AssertFourCcs(beforeArray, new string[] { "one1", "two2", "VP8 ", "EXIF" });
            AssertFourCcs(beforeStream, new string[] { "one1", "two2", "VP8 ", "EXIF" });
            AssertFourCcs(afterSpan, new string[] { "VP8 ", "EXIF", "one1", "two2" });
            AssertFourCcs(afterFile, new string[] { "VP8 ", "EXIF", "one1", "two2" });
            AssertUnknownReplacementPayloads(mux.EnumerateUnknownChunks(afterSpan));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void StripUnknownChunks_AllInputFormsRemoveOnlyTopLevelUnknownChunks()
    {
        byte[] data = NewContainerWithUnknowns();
        IWebpMux mux = new WebpCodec();
        MemoryStream stream = new MemoryStream(data);
        string path = NewTempPath();
        File.WriteAllBytes(path, data);

        try
        {
            byte[] arrayResult = mux.StripUnknownChunks(data);
            byte[] spanResult = mux.StripUnknownChunksSpan(data);
            byte[] streamResult = mux.StripUnknownChunksStream(stream);
            byte[] fileResult = mux.StripUnknownChunksFile(path);

            AssertStripped(arrayResult, mux);
            AssertStripped(spanResult, mux);
            AssertStripped(streamResult, mux);
            AssertStripped(fileResult, mux);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void ReplaceUnknownChunks_RejectsInvalidPlacementThroughPublicContract()
    {
        IWebpMux mux = new WebpCodec();

        Assert.Throws<ArgumentOutOfRangeException>(() => mux.ReplaceUnknownChunks(
            NewContainerWithUnknowns(),
            NewReplacements(),
            (WebpUnknownChunkPlacement)99));
    }

    static byte[] NewContainerWithUnknowns()
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("old1", new byte[] { 9 }));
        chunks.Add(new WebpRiffChunk("VP8 ", NewVp8Payload(2, 3)));
        chunks.Add(new WebpRiffChunk("old2", new byte[] { 8, 7 }));
        chunks.Add(new WebpRiffChunk("EXIF", new byte[] { 4 }));
        return WriteWithoutSorting(chunks);
    }

    static List<WebpUnknownChunk> NewReplacements()
    {
        List<WebpUnknownChunk> replacements = new List<WebpUnknownChunk>();
        replacements.Add(new WebpUnknownChunk("one1", new byte[] { 1 }));
        replacements.Add(new WebpUnknownChunk("two2", new byte[] { 2, 3 }));
        return replacements;
    }

    static void AssertUnknownChunks(List<WebpUnknownChunk> chunks)
    {
        Assert.That(chunks, Has.Count.EqualTo(2));
        Assert.That(chunks[0].FourCc, Is.EqualTo("old1"));
        Assert.That(chunks[0].Data, Is.EqualTo(new byte[] { 9 }));
        Assert.That(chunks[1].FourCc, Is.EqualTo("old2"));
        Assert.That(chunks[1].Data, Is.EqualTo(new byte[] { 8, 7 }));
    }

    static void AssertUnknownReplacementPayloads(List<WebpUnknownChunk> chunks)
    {
        Assert.That(chunks, Has.Count.EqualTo(2));
        Assert.That(chunks[0].FourCc, Is.EqualTo("one1"));
        Assert.That(chunks[0].Data, Is.EqualTo(new byte[] { 1 }));
        Assert.That(chunks[1].FourCc, Is.EqualTo("two2"));
        Assert.That(chunks[1].Data, Is.EqualTo(new byte[] { 2, 3 }));
    }

    static void AssertStripped(byte[] data, IWebpMux mux)
    {
        Assert.That(mux.EnumerateUnknownChunks(data), Is.Empty);
        AssertFourCcs(data, new string[] { "VP8 ", "EXIF" });
        WebpMetadata metadata = mux.GetMetadata(data);
        Assert.That(metadata.Exif, Is.EqualTo(new byte[] { 4 }));
    }

    static void AssertFourCcs(byte[] data, string[] expected)
    {
        WebpRiffParseResult riff = WebpRiffParser.Parse(data);
        Assert.That(riff.Chunks, Has.Count.EqualTo(expected.Length));
        int index = 0;
        while (index < expected.Length)
        {
            Assert.That(riff.Chunks[index].FourCc, Is.EqualTo(expected[index]));
            index = index + 1;
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

    static byte[] WriteWithoutSorting(List<WebpRiffChunk> chunks)
    {
        long riffSize = WebpRiffWriter.ComputeRiffSize(chunks);
        List<byte> data = new List<byte>();
        AppendFourCc(data, "RIFF");
        WebpEndianWriter.AppendUInt32(data, (uint)riffSize);
        AppendFourCc(data, "WEBP");

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
        int index = 0;
        while (index < fourCc.Length)
        {
            data.Add((byte)fourCc[index]);
            index = index + 1;
        }
    }

    static string NewTempPath()
    {
        return Path.Combine(TestContext.CurrentContext.WorkDirectory, "unknown-chunks-" + Guid.NewGuid().ToString("N") + ".webp");
    }
}
