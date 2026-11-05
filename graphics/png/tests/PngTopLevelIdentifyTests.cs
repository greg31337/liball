using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class PngTopLevelIdentifyTests
{
    [Test]
    public void IdentifyBytes_IdentifiesPngData()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(2, 3)),
            CreateChunkBytes(PngConstants.ChunkIDAT, new byte[] { 1, 2, 3 }),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));

        PngIdentifyResult result = Png.IdentifyBytes(png);

        Assert.That(result.IsPng, Is.True);
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Info.Width, Is.EqualTo(2));
        Assert.That(result.Info.Height, Is.EqualTo(3));
        Assert.That(result.IdatBytes, Is.EqualTo(3));
    }

    [Test]
    public void IdentifyBytes_UsesOffsetAndOptions()
    {
        byte[] actl = new byte[] { 0, 0, 0, 2, 0, 0, 0, 0 };
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1)),
            CreateChunkBytes(PngConstants.ChunkAnimationControl, actl),
            CreateChunkBytes(PngConstants.ChunkIDAT, new byte[] { 1 }),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));
        byte[] wrapped = Wrap(png);
        PngDecoderOptions options = new PngDecoderOptions();
        options.Limits.MaxAnimationFrames = 1;

        Assert.Throws<OverflowException>(() => Png.IdentifyBytes(wrapped, 2, options));
    }

    [Test]
    public void IdentifyBytes_UsesOptionsWithoutOffset()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1)),
            CreateChunkBytes(PngConstants.ChunkIDAT, new byte[] { 1 }),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));
        PngDecoderOptions options = new PngDecoderOptions();
        options.PreserveUnknownChunks = false;

        PngIdentifyResult result = Png.IdentifyBytes(png, options);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Metadata.Chunks, Has.Length.EqualTo(2));
    }

    [Test]
    public void IdentifyBytes_EnforcesPredictedDecompressedByteLimit()
    {
        byte[] text = new byte[] { 84, 105, 116, 108, 101, 0, 82, 84 };
        byte[] unknown = new byte[] { 9, 8, 7 };
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(2, 2)),
            CreateChunkBytes(PngConstants.ChunkGamma, new byte[] { 0, 0, 177, 143 }),
            CreateChunkBytes(PngConstants.ChunkIDAT, CreateRepeatedBytes(1024, 0xA5)),
            CreateChunkBytes(PngConstants.ChunkText, text),
            CreateChunkBytes("vpAg", unknown),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));
        PngDecoderOptions options = new PngDecoderOptions();
        options.Limits.MaxMetadataBytes = 15;
        options.Limits.MaxDecompressedBytes = 1;

        Assert.Throws<OverflowException>(() => Png.IdentifyBytes(png, options));
    }

    [Test]
    public void IdentifyStream_IdentifiesPngStream()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(4, 5)),
            CreateChunkBytes(PngConstants.ChunkIDAT, new byte[] { 1, 2 }),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]),
            new byte[] { 9, 8 });
        MemoryStream stream = new MemoryStream(png);

        PngIdentifyResult result = Png.IdentifyStream(stream);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Info.Width, Is.EqualTo(4));
        Assert.That(result.Info.Height, Is.EqualTo(5));
        Assert.That(stream.Position, Is.EqualTo(png.Length - 2));
    }

    [Test]
    public void IdentifyStream_SkipsMultipleIdatPayloadsAndKeepsMetadataChunks()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1)),
            CreateChunkBytes(PngConstants.ChunkIDAT, new byte[] { 1, 2, 3 }),
            CreateChunkBytes(PngConstants.ChunkIDAT, new byte[] { 4, 5 }),
            CreateChunkBytes(PngConstants.ChunkText, new byte[] { 75, 0, 86 }),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));

        PngIdentifyResult result = Png.IdentifyStream(new MemoryStream(png));

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.IdatBytes, Is.EqualTo(5));
        Assert.That(result.Metadata.TextBytes, Is.EqualTo(1));
        Assert.That(result.Metadata.TextEntryCount, Is.EqualTo(1));
        Assert.That(result.Metadata.Chunks, Has.Length.EqualTo(3));
        Assert.That(HasChunk(result.Metadata.Chunks, PngConstants.ChunkIDAT), Is.False);
        Assert.That(HasChunk(result.Metadata.Chunks, PngConstants.ChunkText), Is.True);
    }

    [Test]
    public void IdentifyStream_UsesOptions()
    {
        byte[] actl = new byte[] { 0, 0, 0, 2, 0, 0, 0, 0 };
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1)),
            CreateChunkBytes(PngConstants.ChunkAnimationControl, actl),
            CreateChunkBytes(PngConstants.ChunkIDAT, new byte[] { 1 }),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));
        PngDecoderOptions options = new PngDecoderOptions();
        options.Limits.MaxAnimationFrames = 1;

        Assert.Throws<OverflowException>(() => Png.IdentifyStream(new MemoryStream(png), options));
    }

    [Test]
    public void Identify_RejectsInvalidInputs()
    {
        byte[] png = CreatePng(CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));

        Assert.Throws<ArgumentNullException>(() => Png.IdentifyBytes(null!));
        Assert.Throws<ArgumentNullException>(() => Png.IdentifyBytes(png, null!));
        Assert.Throws<ArgumentNullException>(() => Png.IdentifyBytes(png, 0, null!));
        Assert.Throws<ArgumentNullException>(() => Png.IdentifyStream(null!));
        Assert.Throws<ArgumentNullException>(() => Png.IdentifyStream(new MemoryStream(png), null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => Png.IdentifyBytes(png, -1, new PngDecoderOptions()));
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
        PngChunk chunk = new PngChunk();
        chunk.Type = chunkType;
        chunk.Data = chunkData;
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

    static byte[] CreateRepeatedBytes(int length, byte value)
    {
        byte[] data = new byte[length];
        int index = 0;
        while (index < data.Length)
        {
            data[index] = value;
            index += 1;
        }

        return data;
    }

    static byte[] Wrap(byte[] data)
    {
        byte[] wrapped = new byte[data.Length + 4];
        wrapped[0] = 99;
        wrapped[1] = 88;
        wrapped[wrapped.Length - 2] = 77;
        wrapped[wrapped.Length - 1] = 66;
        Array.Copy(data, 0, wrapped, 2, data.Length);
        return wrapped;
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

    static bool HasChunk(PngChunk[] chunks, string chunkType)
    {
        int index = 0;
        while (index < chunks.Length)
        {
            if (chunks[index].Type == chunkType)
            {
                return true;
            }

            index += 1;
        }

        return false;
    }
}
