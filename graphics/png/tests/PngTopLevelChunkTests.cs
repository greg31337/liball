using System.IO;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class PngTopLevelChunkTests
{
    [Test]
    public void ReadChunks_ReadsChunksFromCompletePngBytes()
    {
        PngChunk text = CreateChunk(PngConstants.ChunkText, new byte[] { 65, 66 });
        PngChunk iend = CreateChunk(PngConstants.ChunkIEND, new byte[0]);
        byte[] encoded = PngChunkDatastreamWriter.WriteToBytes(new PngChunk[] { text, iend });

        PngChunkListResult result = Png.ReadChunks(encoded);

        Assert.That(result.FoundIend, Is.True);
        Assert.That(result.Chunks, Has.Length.EqualTo(2));
        Assert.That(result.Chunks[0].Type, Is.EqualTo(PngConstants.ChunkText));
        Assert.That(result.Chunks[0].Offset, Is.EqualTo(PngConstants.SignatureLength));
        Assert.That(result.Chunks[1].Type, Is.EqualTo(PngConstants.ChunkIEND));
        Assert.That(result.TrailingBytes, Is.Empty);
        Assert.That(result.Warnings, Is.Empty);
    }

    [Test]
    public void ReadChunks_UsesByteOffsetForEmbeddedDatastream()
    {
        PngChunk text = CreateChunk(PngConstants.ChunkText, new byte[] { 1, 2, 3 });
        PngChunk iend = CreateChunk(PngConstants.ChunkIEND, new byte[0]);
        byte[] encoded = PngChunkDatastreamWriter.WriteToBytes(new PngChunk[] { text, iend });
        byte[] wrapped = Join(new byte[] { 0xAA, 0xBB }, encoded);

        PngChunkListResult result = Png.ReadChunks(wrapped, 2);

        Assert.That(result.FoundIend, Is.True);
        Assert.That(result.Chunks, Has.Length.EqualTo(2));
        Assert.That(result.Chunks[0].Offset, Is.EqualTo(2 + PngConstants.SignatureLength));
        Assert.That(result.Chunks[0].Data, Is.EqualTo(new byte[] { 1, 2, 3 }));
    }

    [Test]
    public void ReadChunks_StreamReadsAfterSignatureAndReportsTrailingBytes()
    {
        PngChunk text = CreateChunk(PngConstants.ChunkText, new byte[] { 65 });
        PngChunk iend = CreateChunk(PngConstants.ChunkIEND, new byte[0]);
        byte[] trailing = new byte[] { 9, 8 };
        byte[] encoded = PngChunkDatastreamWriter.WriteToBytes(new PngChunk[] { text, iend });
        MemoryStream stream = new MemoryStream(Join(encoded, trailing));

        PngChunkListResult result = Png.ReadChunks(stream);

        Assert.That(result.FoundIend, Is.True);
        Assert.That(result.Chunks, Has.Length.EqualTo(2));
        Assert.That(result.TrailingBytes, Is.EqualTo(trailing));
        Assert.That(stream.Position, Is.EqualTo(stream.Length));
    }

    [Test]
    public void WriteChunks_WritesCompleteDatastream()
    {
        PngChunk text = CreateChunk(PngConstants.ChunkText, new byte[] { 65, 66 });
        PngChunk iend = CreateChunk(PngConstants.ChunkIEND, new byte[0]);

        byte[] encoded = Png.WriteChunks(new PngChunk[] { text, iend });
        PngChunkListResult result = Png.ReadChunks(encoded);

        Assert.That(PngSignature.HasSignature(encoded), Is.True);
        Assert.That(result.FoundIend, Is.True);
        Assert.That(result.Chunks, Has.Length.EqualTo(2));
        Assert.That(result.Chunks[0].Type, Is.EqualTo(PngConstants.ChunkText));
        Assert.That(result.Chunks[1].Type, Is.EqualTo(PngConstants.ChunkIEND));
        Assert.That(text.Offset, Is.EqualTo(PngConstants.SignatureLength));
        Assert.That(iend.Offset, Is.EqualTo(PngConstants.SignatureLength + 14));
    }

    [Test]
    public void WriteChunks_StreamWritesAtCurrentPosition()
    {
        PngChunk iend = CreateChunk(PngConstants.ChunkIEND, new byte[0]);
        MemoryStream stream = new MemoryStream();
        stream.WriteByte(0xAA);

        Png.WriteChunks(stream, new PngChunk[] { iend });

        byte[] bytes = stream.ToArray();
        Assert.That(bytes[0], Is.EqualTo(0xAA));
        Assert.That(PngSignature.HasSignature(bytes, 1), Is.True);
        Assert.That(iend.Offset, Is.EqualTo(1 + PngConstants.SignatureLength));
        Assert.That(stream.Position, Is.EqualTo(bytes.Length));
    }

    [Test]
    public void RepairAncillaryOrdering_RewritesMisplacedAncillaryChunks()
    {
        byte[] png = Png.WriteChunks(new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkIHDR, CreateIhdrData(1, 1)),
            CreateChunk(PngConstants.ChunkIDAT, CreateValidIdatData()),
            CreateChunk(PngConstants.ChunkPhysicalPixels, new byte[] { 0, 0, 0, 1, 0, 0, 0, 1, 1 }),
            CreateChunk(PngConstants.ChunkIEND, new byte[0])
        });
        byte[] wrapped = Join(new byte[] { 1, 2 }, png);

        byte[] byteResult = Png.RepairAncillaryOrderingBytes(wrapped, 2);
        byte[] streamResult = Png.RepairAncillaryOrderingStream(new MemoryStream(png));
        PngChunk[] chunkResult = Png.RepairAncillaryOrderingChunks(Png.ReadChunks(png).Chunks);

        Assert.That(Png.ValidateBytes(byteResult).IsValid, Is.True);
        Assert.That(Png.ValidateBytes(streamResult).IsValid, Is.True);
        Assert.That(chunkResult[1].Type, Is.EqualTo(PngConstants.ChunkPhysicalPixels));
        Assert.That(chunkResult[2].Type, Is.EqualTo(PngConstants.ChunkIDAT));
    }

    [Test]
    public void RepairAncillaryCrc_RewritesOnlyAncillaryCrcs()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1)),
            CreateChunkBytesWithCrc(PngConstants.ChunkText, new byte[] { 65, 0, 66 }, 0),
            CreateChunkBytes(PngConstants.ChunkIDAT, CreateValidIdatData()),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));
        byte[] wrapped = Join(new byte[] { 1, 2 }, png);
        PngChunk text = CreateChunk(PngConstants.ChunkText, new byte[] { 65, 0, 66 });
        text.Crc = 0;

        byte[] byteResult = Png.RepairAncillaryCrcBytes(wrapped, 2);
        byte[] streamResult = Png.RepairAncillaryCrcStream(new MemoryStream(png));
        PngChunk[] chunkResult = Png.RepairAncillaryCrcChunks(new PngChunk[] { text });

        Assert.That(Png.ValidateBytes(byteResult).IsValid, Is.True);
        Assert.That(Png.ValidateBytes(streamResult).IsValid, Is.True);
        Assert.That(chunkResult[0].Crc, Is.EqualTo(PngCrc32.CalculateChunkCrc(PngConstants.ChunkText, text.Data)));
    }

    [Test]
    public void ReadChunks_EnforcesLimits()
    {
        PngChunk text = CreateChunk(PngConstants.ChunkText, new byte[] { 65 });
        PngChunk iend = CreateChunk(PngConstants.ChunkIEND, new byte[0]);
        byte[] encoded = PngChunkDatastreamWriter.WriteToBytes(new PngChunk[] { text, iend });
        PngSecurityLimits limits = new PngSecurityLimits();
        limits.MaxChunkCount = 1;

        Assert.Throws<OverflowException>(() => Png.ReadChunks(encoded, limits));
    }

    [Test]
    public void WriteChunks_EnforcesLimits()
    {
        PngChunk text = CreateChunk(PngConstants.ChunkText, new byte[] { 65 });
        PngChunk iend = CreateChunk(PngConstants.ChunkIEND, new byte[0]);
        PngSecurityLimits limits = new PngSecurityLimits();
        limits.MaxChunkCount = 1;

        Assert.Throws<OverflowException>(() => Png.WriteChunks(new PngChunk[] { text, iend }, limits));
    }

    [Test]
    public void ReadWriteChunks_RejectInvalidInputs()
    {
        PngChunk iend = CreateChunk(PngConstants.ChunkIEND, new byte[0]);
        byte[] encoded = PngChunkDatastreamWriter.WriteToBytes(new PngChunk[] { iend });
        byte[] invalidSignature = new byte[encoded.Length];
        Array.Copy(encoded, invalidSignature, encoded.Length);
        invalidSignature[0] = 0;

        Assert.Throws<ArgumentNullException>(() => Png.ReadChunks((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => Png.ReadChunks(encoded, null!));
        Assert.Throws<ArgumentNullException>(() => Png.ReadChunks((Stream)null!));
        Assert.Throws<ArgumentNullException>(() => Png.ReadChunks(new MemoryStream(encoded), null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => Png.ReadChunks(encoded, -1));
        Assert.Throws<InvalidDataException>(() => Png.ReadChunks(invalidSignature));
        Assert.Throws<InvalidDataException>(() => Png.ReadChunks(new MemoryStream(invalidSignature)));
        Assert.Throws<ArgumentNullException>(() => Png.WriteChunks(null!));
        Assert.Throws<ArgumentNullException>(() => Png.WriteChunks(new PngChunk[] { iend }, null!));
        Assert.Throws<ArgumentNullException>(() => Png.WriteChunks(null!, new PngChunk[] { iend }));
        Assert.Throws<ArgumentNullException>(() => Png.WriteChunks(new MemoryStream(), null!));
        Assert.Throws<ArgumentNullException>(() => Png.RepairAncillaryOrderingBytes(null!));
        Assert.Throws<ArgumentNullException>(() => Png.RepairAncillaryOrderingBytes(null!, 0));
        Assert.Throws<ArgumentNullException>(() => Png.RepairAncillaryOrderingStream(null!));
        Assert.Throws<ArgumentNullException>(() => Png.RepairAncillaryOrderingChunks(null!));
        Assert.Throws<ArgumentNullException>(() => Png.RepairAncillaryCrcBytes(null!));
        Assert.Throws<ArgumentNullException>(() => Png.RepairAncillaryCrcBytes(null!, 0));
        Assert.Throws<ArgumentNullException>(() => Png.RepairAncillaryCrcStream(null!));
        Assert.Throws<ArgumentNullException>(() => Png.RepairAncillaryCrcChunks(null!));
        Assert.Throws<InvalidDataException>(() => Png.WriteChunks(new PngChunk[0]));
    }

    static PngChunk CreateChunk(string chunkType, byte[] data)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = chunkType;
        chunk.Data = data;
        return chunk;
    }

    static byte[] CreateIhdrData(int width, int height)
    {
        byte[] data = new byte[13];
        PngBinaryWriter.WriteUInt32(data, 0, (uint)width);
        PngBinaryWriter.WriteUInt32(data, 4, (uint)height);
        data[8] = PngConstants.BitDepth8;
        data[9] = PngConstants.ColorTypeTruecolorAlpha;
        data[10] = PngConstants.CompressionMethodDeflate;
        data[11] = PngConstants.FilterMethodAdaptive;
        data[12] = PngConstants.InterlaceMethodNone;
        return data;
    }

    static byte[] CreateValidIdatData()
    {
        return PngZlibCompressor.Compress(new byte[] { 0, 1, 2, 3, 4 });
    }

    static byte[] CreateChunkBytes(string chunkType, byte[] chunkData)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = chunkType;
        chunk.Data = chunkData;
        return PngRawChunkWriter.WriteToBytes(chunk);
    }

    static byte[] CreateChunkBytesWithCrc(string chunkType, byte[] chunkData, uint crc)
    {
        byte[] bytes = new byte[chunkData.Length + 12];
        PngBinaryWriter.WriteUInt32(bytes, 0, (uint)chunkData.Length);
        bytes[4] = (byte)chunkType[0];
        bytes[5] = (byte)chunkType[1];
        bytes[6] = (byte)chunkType[2];
        bytes[7] = (byte)chunkType[3];
        int index = 0;
        while (index < chunkData.Length)
        {
            bytes[8 + index] = chunkData[index];
            index += 1;
        }

        PngBinaryWriter.WriteUInt32(bytes, 8 + chunkData.Length, crc);
        return bytes;
    }

    static byte[] CreatePng(params byte[][] chunks)
    {
        byte[] signature = new byte[]
        {
            PngConstants.SignatureByte0,
            PngConstants.SignatureByte1,
            PngConstants.SignatureByte2,
            PngConstants.SignatureByte3,
            PngConstants.SignatureByte4,
            PngConstants.SignatureByte5,
            PngConstants.SignatureByte6,
            PngConstants.SignatureByte7
        };

        byte[][] arrays = new byte[chunks.Length + 1][];
        arrays[0] = signature;
        int index = 0;
        while (index < chunks.Length)
        {
            arrays[index + 1] = chunks[index];
            index += 1;
        }

        return Join(arrays);
    }

    static byte[] Join(params byte[][] arrays)
    {
        int totalLength = 0;
        int index = 0;
        while (index < arrays.Length)
        {
            totalLength += arrays[index].Length;
            index += 1;
        }

        byte[] result = new byte[totalLength];
        int offset = 0;
        index = 0;
        while (index < arrays.Length)
        {
            Array.Copy(arrays[index], 0, result, offset, arrays[index].Length);
            offset += arrays[index].Length;
            index += 1;
        }

        return result;
    }
}
