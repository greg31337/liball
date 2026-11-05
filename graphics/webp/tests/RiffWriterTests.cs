using webp;

namespace webp.Tests;

[TestFixture]
public class RiffWriterTests
{
    [Test]
    public void Write_EmitsRiffHeaderWithPrecomputedSizeAndPadding()
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("VP8 ", new byte[] { 1, 2, 3 }));

        byte[] bytes = WebpRiffWriter.Write("WEBP", chunks);

        Assert.That(bytes, Is.EqualTo(new byte[]
        {
            (byte)'R',
            (byte)'I',
            (byte)'F',
            (byte)'F',
            16,
            0,
            0,
            0,
            (byte)'W',
            (byte)'E',
            (byte)'B',
            (byte)'P',
            (byte)'V',
            (byte)'P',
            (byte)'8',
            (byte)' ',
            3,
            0,
            0,
            0,
            1,
            2,
            3,
            0
        }));
    }

    [Test]
    public void Write_EmitsEvenPayloadWithoutPadding()
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("EXIF", new byte[] { 1, 2 }));

        byte[] bytes = WebpRiffWriter.Write("WEBP", chunks);
        WebpRiffParseResult parsed = WebpRiffParser.Parse(bytes);

        Assert.That(bytes.Length, Is.EqualTo(22));
        Assert.That(parsed.IsValid, Is.True);
        Assert.That(parsed.DeclaredSize, Is.EqualTo(14));
        Assert.That(parsed.Chunks[0].HasPadding, Is.False);
        Assert.That(parsed.Chunks[0].PaddedEndOffset, Is.EqualTo(22));
    }

    [Test]
    public void Write_OrdersKnownChunksDeterministically()
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("zzzz", new byte[] { 9 }));
        chunks.Add(new WebpRiffChunk("EXIF", new byte[] { 8 }));
        chunks.Add(new WebpRiffChunk("VP8 ", new byte[] { 7 }));
        chunks.Add(new WebpRiffChunk("ICCP", new byte[] { 6 }));
        chunks.Add(new WebpRiffChunk("VP8X", new byte[] { 5 }));
        chunks.Add(new WebpRiffChunk("aaaa", new byte[] { 4 }));
        chunks.Add(new WebpRiffChunk("XMP ", new byte[] { 3 }));

        byte[] bytes = WebpRiffWriter.Write("WEBP", chunks);
        WebpRiffParseResult parsed = WebpRiffParser.Parse(bytes);

        Assert.That(parsed.IsValid, Is.True);
        Assert.That(parsed.Chunks[0].FourCc, Is.EqualTo("VP8X"));
        Assert.That(parsed.Chunks[1].FourCc, Is.EqualTo("ICCP"));
        Assert.That(parsed.Chunks[2].FourCc, Is.EqualTo("VP8 "));
        Assert.That(parsed.Chunks[3].FourCc, Is.EqualTo("EXIF"));
        Assert.That(parsed.Chunks[4].FourCc, Is.EqualTo("XMP "));
        Assert.That(parsed.Chunks[5].FourCc, Is.EqualTo("aaaa"));
        Assert.That(parsed.Chunks[6].FourCc, Is.EqualTo("zzzz"));
    }

    [Test]
    public void OrderChunks_ReturnsCopiesWithoutMutatingInputList()
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("zzzz", new byte[] { 9 }));
        chunks.Add(new WebpRiffChunk("VP8X", new byte[] { 1 }));

        List<WebpRiffChunk> ordered = WebpRiffWriter.OrderChunks(chunks);
        ordered[0].Payload[0] = 99;

        Assert.That(chunks[0].FourCc, Is.EqualTo("zzzz"));
        Assert.That(chunks[1].FourCc, Is.EqualTo("VP8X"));
        Assert.That(ordered[0].FourCc, Is.EqualTo("VP8X"));
        Assert.That(chunks[1].Payload, Is.EqualTo(new byte[] { 1 }));
    }

    [Test]
    public void ComputeRiffSize_IncludesFormTypeChunkHeadersPayloadsAndPadding()
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("VP8 ", new byte[] { 1, 2, 3 }));
        chunks.Add(new WebpRiffChunk("EXIF", new byte[] { 4, 5 }));

        long riffSize = WebpRiffWriter.ComputeRiffSize(chunks);

        Assert.That(riffSize, Is.EqualTo(26));
    }

    [Test]
    public void Write_OutputParsesWithChunkOffsetsAndPadding()
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("VP8 ", new byte[] { 1, 2, 3 }));
        chunks.Add(new WebpRiffChunk("EXIF", new byte[] { 4, 5 }));

        WebpRiffParseResult parsed = WebpRiffParser.Parse(WebpRiffWriter.Write("WEBP", chunks));

        Assert.That(parsed.IsValid, Is.True);
        Assert.That(parsed.DeclaredSize, Is.EqualTo(26));
        Assert.That(parsed.Chunks, Has.Count.EqualTo(2));
        Assert.That(parsed.Chunks[0].HasPadding, Is.True);
        Assert.That(parsed.Chunks[0].IsPaddingValid, Is.True);
        Assert.That(parsed.Chunks[0].PaddedEndOffset, Is.EqualTo(24));
        Assert.That(parsed.Chunks[1].HasPadding, Is.False);
        Assert.That(parsed.Chunks[1].PaddedEndOffset, Is.EqualTo(34));
    }

    [Test]
    public void RiffChunk_CopiesPayloadBytes()
    {
        byte[] payload = new byte[] { 1, 2, 3 };
        WebpRiffChunk chunk = new WebpRiffChunk("VP8 ", payload);

        payload[0] = 99;

        Assert.That(chunk.Payload, Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(chunk.Payload, Is.Not.SameAs(payload));
    }

    [Test]
    public void Write_RejectsInvalidFourCcValues()
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("VP8 ", new byte[] { 1 }));

        Assert.Throws<ArgumentException>(() => WebpRiffWriter.Write("WEB", chunks));
        Assert.Throws<ArgumentException>(() => WebpRiffWriter.Write("WEBPP", chunks));
        Assert.Throws<ArgumentException>(() => WebpRiffWriter.Write("WE" + '\u0100' + "P", chunks));
        Assert.Throws<ArgumentException>(() => WebpRiffWriter.Write("WEBP", new List<WebpRiffChunk>
        {
            new WebpRiffChunk("BAD", new byte[] { 1 })
        }));
    }

    [Test]
    public void Write_RejectsNullChunksAndNullPayloads()
    {
        List<WebpRiffChunk> chunksWithNull = new List<WebpRiffChunk>();
        chunksWithNull.Add(null!);

        WebpRiffChunk chunkWithNullPayload = new WebpRiffChunk("VP8 ", Array.Empty<byte>());
        chunkWithNullPayload.Payload = null!;
        List<WebpRiffChunk> chunksWithNullPayload = new List<WebpRiffChunk>();
        chunksWithNullPayload.Add(chunkWithNullPayload);

        Assert.Throws<ArgumentException>(() => WebpRiffWriter.Write("WEBP", chunksWithNull));
        Assert.Throws<ArgumentNullException>(() => WebpRiffWriter.Write("WEBP", chunksWithNullPayload));
    }

    [Test]
    public void Write_RejectsNullFormatAndChunkList()
    {
        Assert.Throws<ArgumentNullException>(() => WebpRiffWriter.Write(null!, new List<WebpRiffChunk>()));
        Assert.Throws<ArgumentNullException>(() => WebpRiffWriter.Write("WEBP", null!));
        Assert.Throws<ArgumentNullException>(() => WebpRiffWriter.ComputeRiffSize(null!));
        Assert.Throws<ArgumentNullException>(() => WebpRiffWriter.OrderChunks(null!));
    }
}
