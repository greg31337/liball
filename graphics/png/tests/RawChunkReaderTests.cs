using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class RawChunkReaderTests
{
    [Test]
    public void Read_ArrayParsesChunkFieldsAtOffset()
    {
        byte[] chunkBytes = CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]);
        byte[] data = new byte[chunkBytes.Length + 2];
        data[0] = 0xAA;
        data[1] = 0xBB;
        Array.Copy(chunkBytes, 0, data, 2, chunkBytes.Length);

        PngChunk chunk = PngRawChunkReader.Read(data, 2);

        Assert.That(chunk.Type, Is.EqualTo(PngConstants.ChunkIEND));
        Assert.That(chunk.Length, Is.EqualTo(0));
        Assert.That(chunk.Data, Is.Empty);
        Assert.That(chunk.Crc, Is.EqualTo(0xAE426082u));
        Assert.That(chunk.Offset, Is.EqualTo(2));
        Assert.That(chunk.IsKnown, Is.True);
        Assert.That(chunk.IsCritical, Is.True);
        Assert.That(chunk.IsPublic, Is.True);
        Assert.That(chunk.IsReservedBitValid, Is.True);
        Assert.That(chunk.IsSafeToCopy, Is.False);
    }

    [Test]
    public void Read_ArrayParsesAncillaryChunkDataAndFlags()
    {
        byte[] chunkData = new byte[] { 65, 66, 67 };
        byte[] data = CreateChunkBytes(PngConstants.ChunkText, chunkData);

        PngChunk chunk = PngRawChunkReader.Read(data, 0);

        Assert.That(chunk.Type, Is.EqualTo(PngConstants.ChunkText));
        Assert.That(chunk.Length, Is.EqualTo(3));
        Assert.That(chunk.Data, Is.EqualTo(chunkData));
        Assert.That(chunk.Crc, Is.EqualTo(PngCrc32.CalculateChunkCrc(PngConstants.ChunkText, chunkData)));
        Assert.That(chunk.IsKnown, Is.True);
        Assert.That(chunk.IsCritical, Is.False);
        Assert.That(chunk.IsPublic, Is.True);
        Assert.That(chunk.IsReservedBitValid, Is.True);
        Assert.That(chunk.IsSafeToCopy, Is.True);
    }

    [Test]
    public void Read_StreamParsesChunkAndAdvancesPosition()
    {
        byte[] chunkData = new byte[] { 1, 2, 3, 4 };
        byte[] chunkBytes = CreateChunkBytes("vpAg", chunkData);
        byte[] streamBytes = new byte[chunkBytes.Length + 2];
        streamBytes[0] = 0xAA;
        streamBytes[1] = 0xBB;
        Array.Copy(chunkBytes, 0, streamBytes, 2, chunkBytes.Length);
        MemoryStream stream = new MemoryStream(streamBytes);
        stream.Position = 2;

        PngChunk chunk = PngRawChunkReader.Read(stream);

        Assert.That(chunk.Type, Is.EqualTo("vpAg"));
        Assert.That(chunk.Length, Is.EqualTo(4));
        Assert.That(chunk.Data, Is.EqualTo(chunkData));
        Assert.That(chunk.Offset, Is.EqualTo(2));
        Assert.That(chunk.IsKnown, Is.False);
        Assert.That(chunk.IsCritical, Is.False);
        Assert.That(chunk.IsPublic, Is.False);
        Assert.That(chunk.IsSafeToCopy, Is.True);
        Assert.That(stream.Position, Is.EqualTo(18));
    }

    [Test]
    public void Read_RejectsTruncatedChunks()
    {
        byte[] headerOnly = new byte[] { 0, 0, 0, 1, 73, 69, 78, 68 };
        byte[] missingCrc = new byte[] { 0, 0, 0, 1, 73, 69, 78, 68, 0xFF };
        byte[] complete = CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]);

        Assert.Throws<EndOfStreamException>(() => PngRawChunkReader.Read(headerOnly, 0));
        Assert.Throws<EndOfStreamException>(() => PngRawChunkReader.Read(missingCrc, 0));
        Assert.Throws<EndOfStreamException>(() => PngRawChunkReader.Read(new MemoryStream(missingCrc)));
        Assert.Throws<EndOfStreamException>(() => PngRawChunkReader.Read(complete, 1));
        Assert.Throws<EndOfStreamException>(() => PngRawChunkReader.Read(complete, complete.Length));
    }

    [Test]
    public void Read_RejectsInvalidChunkType()
    {
        byte[] data = CreateChunkBytes("abca", new byte[0]);

        Assert.Throws<InvalidDataException>(() => PngRawChunkReader.Read(data, 0));
    }

    [Test]
    public void Read_RejectsChunkLengthBeyondArrayAllocationLimit()
    {
        byte[] data = new byte[12];
        PngBinaryWriter.WriteUInt32(data, 0, (uint)int.MaxValue + 1u);
        data[4] = 73;
        data[5] = 68;
        data[6] = 65;
        data[7] = 84;

        Assert.Throws<OverflowException>(() => PngRawChunkReader.Read(data, 0));
    }

    [Test]
    public void Read_EnforcesIdatAndMetadataLimits()
    {
        PngSecurityLimits limits = new PngSecurityLimits();
        limits.MaxIdatBytes = 2;
        limits.MaxMetadataBytes = 2;
        byte[] idat = CreateChunkBytes(PngConstants.ChunkIDAT, new byte[] { 1, 2, 3 });
        byte[] text = CreateChunkBytes(PngConstants.ChunkText, new byte[] { 1, 2, 3 });

        Assert.Throws<OverflowException>(() => PngRawChunkReader.Read(idat, 0, limits));
        Assert.Throws<OverflowException>(() => PngRawChunkReader.Read(text, 0, limits));
    }

    [Test]
    public void Read_RejectsInvalidInputs()
    {
        byte[] data = CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]);

        Assert.Throws<ArgumentNullException>(() => PngRawChunkReader.Read((byte[])null!, 0));
        Assert.Throws<ArgumentNullException>(() => PngRawChunkReader.Read(data, 0, null!));
        Assert.Throws<ArgumentNullException>(() => PngRawChunkReader.Read((Stream)null!));
        Assert.Throws<ArgumentNullException>(() => PngRawChunkReader.Read(new MemoryStream(data), null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngRawChunkReader.Read(data, -1));
    }

    [Test]
    public void Read_ArrayRoundTripsChunksWrittenByRawWriterAtOffset()
    {
        PngChunk source = new PngChunk();
        source.Type = "vpAg";
        source.Data = new byte[] { 9, 8, 7, 6, 5 };
        byte[] rawChunk = PngRawChunkWriter.WriteToBytes(source);
        byte[] data = new byte[rawChunk.Length + 4];
        Array.Copy(rawChunk, 0, data, 4, rawChunk.Length);

        PngChunk chunk = PngRawChunkReader.Read(data, 4);

        Assert.That(chunk.Type, Is.EqualTo(source.Type));
        Assert.That(chunk.Data, Is.EqualTo(source.Data));
        Assert.That(chunk.Crc, Is.EqualTo(source.Crc));
        Assert.That(chunk.Offset, Is.EqualTo(4));
        Assert.That(chunk.IsKnown, Is.False);
        Assert.That(chunk.IsSafeToCopy, Is.True);
    }

    static byte[] CreateChunkBytes(string chunkType, byte[] chunkData)
    {
        byte[] bytes = new byte[chunkData.Length + 12];
        PngBinaryWriter.WriteUInt32(bytes, 0, (uint)chunkData.Length);
        bytes[4] = (byte)chunkType[0];
        bytes[5] = (byte)chunkType[1];
        bytes[6] = (byte)chunkType[2];
        bytes[7] = (byte)chunkType[3];
        Array.Copy(chunkData, 0, bytes, 8, chunkData.Length);
        PngBinaryWriter.WriteUInt32(bytes, 8 + chunkData.Length, PngCrc32.CalculateChunkCrc(chunkType, chunkData));
        return bytes;
    }
}
