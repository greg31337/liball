using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class ChunkDatastreamWriterTests
{
    [Test]
    public void WriteToBytes_EmitsSignatureAndChunks()
    {
        PngChunk text = CreateChunk(PngConstants.ChunkText, new byte[] { 65, 66 });
        PngChunk iend = CreateChunk(PngConstants.ChunkIEND, new byte[0]);

        byte[] datastream = PngChunkDatastreamWriter.WriteToBytes(new PngChunk[] { text, iend });
        PngChunkListResult chunkList = PngChunkListReader.Read(datastream, PngConstants.SignatureLength);

        Assert.That(PngSignature.HasSignature(datastream), Is.True);
        Assert.That(chunkList.FoundIend, Is.True);
        Assert.That(chunkList.Chunks, Has.Length.EqualTo(2));
        Assert.That(chunkList.Chunks[0].Type, Is.EqualTo(PngConstants.ChunkText));
        Assert.That(chunkList.Chunks[1].Type, Is.EqualTo(PngConstants.ChunkIEND));
        Assert.That(chunkList.TrailingBytes, Is.Empty);
    }

    [Test]
    public void WriteToBytes_UpdatesChunkOffsetsAndGeneratedCrcs()
    {
        PngChunk text = CreateChunk(PngConstants.ChunkText, new byte[] { 65, 66 });
        PngChunk iend = CreateChunk(PngConstants.ChunkIEND, new byte[0]);
        text.Crc = 0;
        iend.Crc = 0;

        PngChunkDatastreamWriter.WriteToBytes(new PngChunk[] { text, iend });

        Assert.That(text.Offset, Is.EqualTo(8));
        Assert.That(text.Length, Is.EqualTo(2));
        Assert.That(text.Crc, Is.EqualTo(PngCrc32.CalculateChunkCrc(PngConstants.ChunkText, text.Data)));
        Assert.That(iend.Offset, Is.EqualTo(22));
        Assert.That(iend.Crc, Is.EqualTo(0xAE426082u));
    }

    [Test]
    public void Write_StreamWritesAtCurrentPosition()
    {
        PngChunk iend = CreateChunk(PngConstants.ChunkIEND, new byte[0]);
        MemoryStream stream = new MemoryStream();
        stream.WriteByte(0xAA);

        PngChunkDatastreamWriter.Write(stream, new PngChunk[] { iend });

        byte[] bytes = stream.ToArray();
        Assert.That(PngSignature.HasSignature(bytes, 1), Is.True);
        Assert.That(iend.Offset, Is.EqualTo(9));
        Assert.That(stream.Position, Is.EqualTo(21));
    }

    [Test]
    public void WriteToBytes_RoundTripsMultipleChunksThroughChunkListReader()
    {
        PngChunk text = CreateChunk(PngConstants.ChunkText, new byte[] { 65, 66 });
        PngChunk physical = CreateChunk(PngConstants.ChunkPhysicalPixels, new byte[] { 0, 0, 0, 1, 0, 0, 0, 1, 1 });
        PngChunk iend = CreateChunk(PngConstants.ChunkIEND, new byte[0]);

        byte[] datastream = PngChunkDatastreamWriter.WriteToBytes(new PngChunk[] { text, physical, iend });
        PngChunkListResult result = PngChunkListReader.Read(datastream, PngConstants.SignatureLength);

        Assert.That(result.FoundIend, Is.True);
        Assert.That(result.Chunks, Has.Length.EqualTo(3));
        Assert.That(result.Chunks[0].Type, Is.EqualTo(PngConstants.ChunkText));
        Assert.That(result.Chunks[1].Type, Is.EqualTo(PngConstants.ChunkPhysicalPixels));
        Assert.That(result.Chunks[2].Type, Is.EqualTo(PngConstants.ChunkIEND));
        Assert.That(result.Chunks[0].Data, Is.EqualTo(new byte[] { 65, 66 }));
        Assert.That(result.Chunks[1].Data, Is.EqualTo(physical.Data));
        Assert.That(result.TrailingBytes, Is.Empty);
    }

    [Test]
    public void Write_RejectsIncompleteDatastreamChunkLists()
    {
        PngChunk text = CreateChunk(PngConstants.ChunkText, new byte[] { 65 });
        PngChunk iend = CreateChunk(PngConstants.ChunkIEND, new byte[0]);

        Assert.Throws<InvalidDataException>(() => PngChunkDatastreamWriter.WriteToBytes(new PngChunk[0]));
        Assert.Throws<InvalidDataException>(() => PngChunkDatastreamWriter.WriteToBytes(new PngChunk[] { text }));
        Assert.Throws<InvalidDataException>(() => PngChunkDatastreamWriter.WriteToBytes(new PngChunk[] { iend, text }));
    }

    [Test]
    public void Write_EnforcesChunkCountLimit()
    {
        PngSecurityLimits limits = new PngSecurityLimits();
        limits.MaxChunkCount = 1;
        PngChunk text = CreateChunk(PngConstants.ChunkText, new byte[] { 65 });
        PngChunk iend = CreateChunk(PngConstants.ChunkIEND, new byte[0]);

        Assert.Throws<OverflowException>(() => PngChunkDatastreamWriter.WriteToBytes(new PngChunk[] { text, iend }, limits));
    }

    [Test]
    public void Write_RejectsInvalidInputs()
    {
        PngChunk iend = CreateChunk(PngConstants.ChunkIEND, new byte[0]);
        PngChunk invalidType = CreateChunk("abca", new byte[0]);
        PngChunk nullData = CreateChunk(PngConstants.ChunkText, new byte[] { 1 });
        nullData.Data = null!;

        Assert.Throws<ArgumentNullException>(() => PngChunkDatastreamWriter.WriteToBytes(null!));
        Assert.Throws<ArgumentNullException>(() => PngChunkDatastreamWriter.WriteToBytes(new PngChunk[] { iend }, null!));
        Assert.Throws<ArgumentNullException>(() => PngChunkDatastreamWriter.Write(null!, new PngChunk[] { iend }));
        Assert.Throws<ArgumentNullException>(() => PngChunkDatastreamWriter.Write(new MemoryStream(), null!));
        Assert.Throws<ArgumentNullException>(() => PngChunkDatastreamWriter.Write(new MemoryStream(), new PngChunk[] { null! }));
        Assert.Throws<InvalidDataException>(() => PngChunkDatastreamWriter.WriteToBytes(new PngChunk[] { invalidType, iend }));
        Assert.Throws<ArgumentNullException>(() => PngChunkDatastreamWriter.WriteToBytes(new PngChunk[] { nullData, iend }));
    }

    static PngChunk CreateChunk(string chunkType, byte[] data)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = chunkType;
        chunk.Data = data;
        return chunk;
    }
}
