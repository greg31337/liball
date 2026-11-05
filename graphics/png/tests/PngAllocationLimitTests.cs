using System.IO;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class PngAllocationLimitTests
{
    [Test]
    public void ReadChunks_RejectsChunkPayloadsAboveConfiguredAllocationLimits()
    {
        byte[] metadataPng = Png.WriteChunks(new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkText, new byte[] { 65, 0, 66 }),
            CreateChunk(PngConstants.ChunkIEND, new byte[0])
        });
        byte[] idatPng = Png.WriteChunks(new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkIDAT, new byte[] { 1, 2, 3 }),
            CreateChunk(PngConstants.ChunkIEND, new byte[0])
        });
        PngSecurityLimits limits = new PngSecurityLimits();
        limits.MaxMetadataBytes = 2;
        limits.MaxIdatBytes = 2;

        Assert.Throws<OverflowException>(() => Png.ReadChunks(metadataPng, limits));
        Assert.Throws<OverflowException>(() => Png.ReadChunks(new MemoryStream(idatPng), limits));
    }

    [Test]
    public void WriteChunks_RejectsChunkPayloadsAboveConfiguredAllocationLimits()
    {
        PngChunk text = CreateChunk(PngConstants.ChunkText, new byte[] { 65, 0, 66 });
        PngChunk idat = CreateChunk(PngConstants.ChunkIDAT, new byte[] { 1, 2, 3 });
        PngChunk iend = CreateChunk(PngConstants.ChunkIEND, new byte[0]);
        PngSecurityLimits limits = new PngSecurityLimits();
        limits.MaxMetadataBytes = 2;
        limits.MaxIdatBytes = 2;

        Assert.Throws<OverflowException>(() => Png.WriteChunks(new PngChunk[] { text, iend }, limits));
        Assert.Throws<OverflowException>(() => Png.WriteChunks(new MemoryStream(), new PngChunk[] { idat, iend }, limits));
    }

    [Test]
    public void DecodeBytes_RejectsCumulativeIdatAboveConfiguredLimit()
    {
        byte[] compressed = PngZlibCompressor.Compress(new byte[] { 0, 1, 2, 3, 4 });
        int split = compressed.Length / 2;
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1)),
            CreateChunkBytes(PngConstants.ChunkIDAT, CopyBytes(compressed, 0, split)),
            CreateChunkBytes(PngConstants.ChunkIDAT, CopyBytes(compressed, split, compressed.Length - split)),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));
        PngDecoderOptions options = new PngDecoderOptions();
        options.Limits.MaxIdatBytes = compressed.Length - 1;

        Assert.Throws<OverflowException>(() => Png.DecodeBytes(png, options));
    }

    [Test]
    public void DecodeBytes_RejectsConfiguredDimensionLimitsBeforePixelAllocation()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(2, 1)),
            CreateChunkBytes(PngConstants.ChunkIDAT, PngZlibCompressor.Compress(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 })),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));
        PngDecoderOptions options = new PngDecoderOptions();
        options.Limits.MaxWidth = 1;

        Assert.Throws<InvalidDataException>(() => Png.DecodeBytes(png, options));
    }

    [Test]
    public void EncodeToBytes_RejectsScanlineAllocationOverflowBeforeReadingPixelData()
    {
        PngImage image = new PngImage();
        image.Pixels = CreateBuffer(PngPixelFormat.Rgba16, int.MaxValue, 1, int.MaxValue, new byte[0]);

        Assert.Throws<OverflowException>(() => Png.EncodeToBytes(image, CreateRgba16Options()));
    }

    static PngPixelBuffer CreateBuffer(PngPixelFormat format, int width, int height, int stride, byte[] data)
    {
        PngPixelBuffer buffer = new PngPixelBuffer();
        buffer.Format = format;
        buffer.Width = width;
        buffer.Height = height;
        buffer.Stride = stride;
        buffer.Data = data;
        return buffer;
    }

    static PngEncoderOptions CreateRgba16Options()
    {
        PngEncoderOptions options = new PngEncoderOptions();
        options.ColorType = PngColorType.TruecolorAlpha;
        options.BitDepth = PngBitDepth.Sixteen;
        options.InterlaceMethod = PngInterlaceMethod.None;
        options.UseAutomaticColorType = false;
        options.UseAutomaticBitDepth = false;
        options.UseFixedFilter = true;
        options.FixedFilterType = PngFilterType.None;
        return options;
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

    static byte[] CreateChunkBytes(string chunkType, byte[] chunkData)
    {
        PngChunk chunk = CreateChunk(chunkType, chunkData);
        return PngRawChunkWriter.WriteToBytes(chunk);
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

    static byte[] CopyBytes(byte[] data, int offset, int length)
    {
        byte[] result = new byte[length];
        int index = 0;
        while (index < length)
        {
            result[index] = data[offset + index];
            index += 1;
        }

        return result;
    }
}
