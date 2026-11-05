namespace png.Tests;

public class PngMalformedFixtures
{
    public static byte[] CreateInvalidSignature()
    {
        byte[] png = CreateValidMinimalPng();
        png[0] = 0;
        return png;
    }

    public static byte[] CreateTruncatedSignature()
    {
        return new byte[]
        {
            PngConstants.SignatureByte0,
            PngConstants.SignatureByte1,
            PngConstants.SignatureByte2,
            PngConstants.SignatureByte3
        };
    }

    public static byte[] CreateMissingIhdr()
    {
        return CreatePng(
            CreateChunkBytes(PngConstants.ChunkIDAT, CreateValidIdatData()),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));
    }

    public static byte[] CreateMissingIend()
    {
        return CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1)),
            CreateChunkBytes(PngConstants.ChunkIDAT, CreateValidIdatData()));
    }

    public static byte[] CreateBadCriticalCrc()
    {
        return CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1)),
            CreateChunkBytesWithCrc(PngConstants.ChunkIDAT, CreateValidIdatData(), 0),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));
    }

    public static byte[] CreateBadAncillaryCrc()
    {
        return CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1)),
            CreateChunkBytesWithCrc(PngConstants.ChunkText, new byte[] { 65, 0, 66 }, 0),
            CreateChunkBytes(PngConstants.ChunkIDAT, CreateValidIdatData()),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));
    }

    public static byte[] CreateAncillaryAfterIdat()
    {
        return CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1)),
            CreateChunkBytes(PngConstants.ChunkIDAT, CreateValidIdatData()),
            CreateChunkBytes(PngConstants.ChunkPhysicalPixels, new byte[] { 0, 0, 0, 1, 0, 0, 0, 1, 1 }),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));
    }

    public static byte[] CreatePlteAfterIdat()
    {
        return CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1)),
            CreateChunkBytes(PngConstants.ChunkIDAT, CreateValidIdatData()),
            CreateChunkBytes(PngConstants.ChunkPLTE, new byte[] { 1, 2, 3 }),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));
    }

    public static byte[] CreateTruncatedChunkData()
    {
        byte[] signature = CreateSignature();
        byte[] chunkPrefix = new byte[]
        {
            0, 0, 0, 13,
            (byte)'I', (byte)'H', (byte)'D', (byte)'R',
            0, 0, 0
        };
        return Join(signature, chunkPrefix);
    }

    public static byte[] CreateValidMinimalPng()
    {
        return CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1)),
            CreateChunkBytes(PngConstants.ChunkIDAT, CreateValidIdatData()),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));
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
        return CreateChunkBytesWithCrc(chunkType, chunkData, PngCrc32.CalculateChunkCrc(chunkType, chunkData));
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
        byte[][] arrays = new byte[chunks.Length + 1][];
        arrays[0] = CreateSignature();
        int index = 0;
        while (index < chunks.Length)
        {
            arrays[index + 1] = chunks[index];
            index += 1;
        }

        return Join(arrays);
    }

    static byte[] CreateSignature()
    {
        return new byte[]
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
