using System.IO;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class PngRepairerTests
{
    [Test]
    public void RepairAncillaryOrdering_ReordersKnownMisplacedAncillaryChunks()
    {
        PngChunk[] chunks = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkIHDR),
            CreateChunk(PngConstants.ChunkPLTE),
            CreateChunk(PngConstants.ChunkGamma),
            CreateChunk(PngConstants.ChunkIDAT),
            CreateChunk(PngConstants.ChunkPhysicalPixels),
            CreateChunk(PngConstants.ChunkText),
            CreateChunk(PngConstants.ChunkIEND)
        };

        PngChunk[] repaired = PngRepairer.RepairAncillaryOrdering(chunks);

        Assert.That(GetTypes(repaired), Is.EqualTo(new string[]
        {
            PngConstants.ChunkIHDR,
            PngConstants.ChunkGamma,
            PngConstants.ChunkPLTE,
            PngConstants.ChunkPhysicalPixels,
            PngConstants.ChunkIDAT,
            PngConstants.ChunkText,
            PngConstants.ChunkIEND
        }));
    }

    [Test]
    public void RepairAncillaryOrdering_PreservesCriticalOrderAndUnconstrainedAncillaryChunks()
    {
        PngChunk[] chunks = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkIHDR),
            CreateChunk(PngConstants.ChunkIDAT),
            CreateChunk(PngConstants.ChunkPLTE),
            CreateChunk(PngConstants.ChunkText),
            CreateChunk(PngConstants.ChunkIEND)
        };

        PngChunk[] repaired = PngRepairer.RepairAncillaryOrdering(chunks);

        Assert.That(repaired, Is.EqualTo(chunks));
    }

    [Test]
    public void RepairAncillaryOrdering_RepairsPngBytes()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1)),
            CreateChunkBytes(PngConstants.ChunkPLTE, new byte[] { 1, 2, 3 }),
            CreateChunkBytes(PngConstants.ChunkGamma, new byte[] { 0, 0, 0, 1 }),
            CreateChunkBytes(PngConstants.ChunkIDAT, CreateValidIdatData()),
            CreateChunkBytes(PngConstants.ChunkPhysicalPixels, new byte[] { 0, 0, 0, 1, 0, 0, 0, 1, 1 }),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));

        byte[] repaired = PngRepairer.RepairAncillaryOrdering(png);
        PngChunkListResult chunks = Png.ReadChunks(repaired);
        PngValidationResult validation = PngValidator.ValidateStrict(repaired);

        Assert.That(GetTypes(chunks.Chunks), Is.EqualTo(new string[]
        {
            PngConstants.ChunkIHDR,
            PngConstants.ChunkGamma,
            PngConstants.ChunkPLTE,
            PngConstants.ChunkPhysicalPixels,
            PngConstants.ChunkIDAT,
            PngConstants.ChunkIEND
        }));
        Assert.That(validation.IsValid, Is.True);
    }

    [Test]
    public void RepairAncillaryOrdering_RepairsEmbeddedBytesAndStreams()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1)),
            CreateChunkBytes(PngConstants.ChunkIDAT, CreateValidIdatData()),
            CreateChunkBytes(PngConstants.ChunkPhysicalPixels, new byte[] { 0, 0, 0, 1, 0, 0, 0, 1, 1 }),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));
        byte[] wrapped = Join(new byte[] { 9, 8 }, png);

        byte[] byteResult = PngRepairer.RepairAncillaryOrdering(wrapped, 2);
        byte[] streamResult = PngRepairer.RepairAncillaryOrdering(new MemoryStream(png));

        Assert.That(PngValidator.ValidateStrict(byteResult).IsValid, Is.True);
        Assert.That(PngValidator.ValidateStrict(streamResult).IsValid, Is.True);
    }

    [Test]
    public void RepairAncillaryCrc_RepairsOnlyAncillaryChunkCrcs()
    {
        PngChunk text = CreateChunk(PngConstants.ChunkText);
        PngChunk idat = CreateChunk(PngConstants.ChunkIDAT);
        PngChunk frameData = CreateChunk(PngConstants.ChunkFrameData);
        text.Data = new byte[] { 65, 0, 66 };
        idat.Data = new byte[] { 1, 2, 3 };
        frameData.Data = new byte[] { 0, 0, 0, 0, 1 };
        text.Crc = 0;
        idat.Crc = 0;
        frameData.Crc = 0;

        PngChunk[] repaired = PngRepairer.RepairAncillaryCrc(new PngChunk[] { text, idat, frameData });

        Assert.That(repaired[0].Crc, Is.EqualTo(PngCrc32.CalculateChunkCrc(PngConstants.ChunkText, text.Data)));
        Assert.That(repaired[1].Crc, Is.EqualTo(0));
        Assert.That(repaired[2].Crc, Is.EqualTo(0));
    }

    [Test]
    public void RepairAncillaryCrc_RepairsPngBytesAndPreservesCriticalCrcMismatch()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1)),
            CreateChunkBytesWithCrc(PngConstants.ChunkText, new byte[] { 65, 0, 66 }, 0),
            CreateChunkBytesWithCrc(PngConstants.ChunkIDAT, CreateValidIdatData(), 0),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));

        byte[] repaired = PngRepairer.RepairAncillaryCrc(png);
        PngChunkListResult chunks = Png.ReadChunks(repaired);
        PngValidationResult validation = PngValidator.ValidateStrict(repaired);

        Assert.That(chunks.Chunks[1].Crc, Is.EqualTo(PngCrc32.CalculateChunkCrc(PngConstants.ChunkText, chunks.Chunks[1].Data)));
        Assert.That(chunks.Chunks[2].Crc, Is.EqualTo(0));
        Assert.That(validation.IsValid, Is.False);
        Assert.That(HasError(validation, "PNG_CHUNK_CRC_MISMATCH", PngConstants.ChunkIDAT), Is.True);
        Assert.That(HasError(validation, "PNG_CHUNK_CRC_MISMATCH", PngConstants.ChunkText), Is.False);
    }

    [Test]
    public void RepairAncillaryCrc_RepairsEmbeddedBytesAndStreams()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1)),
            CreateChunkBytesWithCrc(PngConstants.ChunkText, new byte[] { 65, 0, 66 }, 0),
            CreateChunkBytes(PngConstants.ChunkIDAT, CreateValidIdatData()),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));
        byte[] wrapped = Join(new byte[] { 9, 8 }, png);

        byte[] byteResult = PngRepairer.RepairAncillaryCrc(wrapped, 2);
        byte[] streamResult = PngRepairer.RepairAncillaryCrc(new MemoryStream(png));

        Assert.That(PngValidator.ValidateStrict(byteResult).IsValid, Is.True);
        Assert.That(PngValidator.ValidateStrict(streamResult).IsValid, Is.True);
    }

    [Test]
    public void RepairAncillaryOrdering_RejectsInvalidInputs()
    {
        PngChunk nullType = CreateChunk(PngConstants.ChunkText);
        nullType.Type = null!;
        PngChunk nullData = CreateChunk(PngConstants.ChunkText);
        nullData.Data = null!;

        Assert.Throws<ArgumentNullException>(() => PngRepairer.RepairAncillaryOrdering((byte[])null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngRepairer.RepairAncillaryOrdering(new byte[0], -1));
        Assert.Throws<InvalidDataException>(() => PngRepairer.RepairAncillaryOrdering(new byte[] { 1, 2, 3 }));
        Assert.Throws<ArgumentNullException>(() => PngRepairer.RepairAncillaryOrdering((Stream)null!));
        Assert.Throws<ArgumentNullException>(() => PngRepairer.RepairAncillaryOrdering((PngChunk[])null!));
        Assert.Throws<ArgumentNullException>(() => PngRepairer.RepairAncillaryOrdering(new PngChunk[] { null! }));
        Assert.Throws<ArgumentNullException>(() => PngRepairer.RepairAncillaryOrdering(new PngChunk[] { nullType }));
        Assert.Throws<ArgumentNullException>(() => PngRepairer.RepairAncillaryOrdering(new PngChunk[] { nullData }));
    }

    [Test]
    public void RepairAncillaryCrc_RejectsInvalidInputs()
    {
        PngChunk nullType = CreateChunk(PngConstants.ChunkText);
        nullType.Type = null!;
        PngChunk nullData = CreateChunk(PngConstants.ChunkText);
        nullData.Data = null!;

        Assert.Throws<ArgumentNullException>(() => PngRepairer.RepairAncillaryCrc((byte[])null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngRepairer.RepairAncillaryCrc(new byte[0], -1));
        Assert.Throws<InvalidDataException>(() => PngRepairer.RepairAncillaryCrc(new byte[] { 1, 2, 3 }));
        Assert.Throws<ArgumentNullException>(() => PngRepairer.RepairAncillaryCrc((Stream)null!));
        Assert.Throws<ArgumentNullException>(() => PngRepairer.RepairAncillaryCrc((PngChunk[])null!));
        Assert.Throws<ArgumentNullException>(() => PngRepairer.RepairAncillaryCrc(new PngChunk[] { null! }));
        Assert.Throws<ArgumentNullException>(() => PngRepairer.RepairAncillaryCrc(new PngChunk[] { nullType }));
        Assert.Throws<ArgumentNullException>(() => PngRepairer.RepairAncillaryCrc(new PngChunk[] { nullData }));
    }

    static PngChunk CreateChunk(string chunkType)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = chunkType;
        chunk.Data = new byte[0];
        return chunk;
    }

    static string[] GetTypes(PngChunk[] chunks)
    {
        string[] types = new string[chunks.Length];
        int index = 0;
        while (index < chunks.Length)
        {
            types[index] = chunks[index].Type;
            index += 1;
        }

        return types;
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

    static bool HasError(PngValidationResult result, string code, string chunkType)
    {
        int index = 0;
        while (index < result.Errors.Length)
        {
            if (result.Errors[index].Code == code && result.Errors[index].ChunkType == chunkType)
            {
                return true;
            }

            index += 1;
        }

        return false;
    }
}
