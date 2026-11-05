using System.IO;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class PngTopLevelDecodeTests
{
    [Test]
    public void DecodeBytes_DecodesNonInterlacedRgba8Png()
    {
        byte[] pixels = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(2, 1, PngConstants.InterlaceMethodNone)),
            CreateChunkBytes(PngConstants.ChunkIDAT, PngZlibCompressor.Compress(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 })),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));

        PngImage image = Png.DecodeBytes(png);

        AssertImage(image, 2, 1, PngInterlaceMethod.None, PngPixelFormat.Rgba8, 8, pixels);
        Assert.That(image.Metadata.Chunks, Has.Length.EqualTo(3));
        Assert.That(image.Warnings, Is.Empty);
    }

    [Test]
    public void DecodeBytes_UsesOffsetAndOptions()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1, PngConstants.InterlaceMethodNone)),
            CreateChunkBytes(PngConstants.ChunkIDAT, PngZlibCompressor.Compress(new byte[] { 0, 9, 8, 7, 6 })),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));
        byte[] wrapped = Wrap(png);
        PngDecoderOptions options = new PngDecoderOptions();
        options.IgnoreTrailingBytes = true;

        PngImage image = Png.DecodeBytes(wrapped, 2, options);

        AssertImage(image, 1, 1, PngInterlaceMethod.None, PngPixelFormat.Rgba8, 4, new byte[] { 9, 8, 7, 6 });
    }

    [Test]
    public void DecodeStream_DecodesAdam7Png()
    {
        PngIhdr ihdr = CreateIhdr(1, 1, PngInterlaceMethod.Adam7);
        byte[] pixels = new byte[] { 11, 12, 13, 14 };
        byte[] filteredScanlines = PngAdam7Encoder.Encode(pixels, ihdr);
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, PngIhdrWriter.WriteData(ihdr)),
            CreateChunkBytes(PngConstants.ChunkIDAT, PngZlibCompressor.Compress(filteredScanlines)),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));

        PngImage image = Png.DecodeStream(new MemoryStream(png));

        AssertImage(image, 1, 1, PngInterlaceMethod.Adam7, PngPixelFormat.Rgba8, 4, pixels);
    }

    [Test]
    public void DecodeBytes_ReadsAnimationInfo()
    {
        byte[] actl = new byte[] { 0, 0, 0, 2, 0, 0, 0, 3 };
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1, PngConstants.InterlaceMethodNone)),
            CreateChunkBytes(PngConstants.ChunkAnimationControl, actl),
            CreateChunkBytes(PngConstants.ChunkIDAT, PngZlibCompressor.Compress(new byte[] { 0, 1, 2, 3, 4 })),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));

        PngImage image = Png.DecodeBytes(png);

        Assert.That(image.Info.IsAnimated, Is.True);
        Assert.That(image.Info.FrameCount, Is.EqualTo(2));
        Assert.That(image.Info.PlayCount, Is.EqualTo(3));
    }

    [Test]
    public void DecodeBytes_RejectsIdatWithUnexpectedDecompressedScanlineByteCount()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(2, 1, PngConstants.InterlaceMethodNone)),
            CreateChunkBytes(PngConstants.ChunkIDAT, PngZlibCompressor.Compress(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 })),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));

        Assert.Throws<InvalidDataException>(() => Png.DecodeBytes(png));
    }

    [Test]
    public void DecodeStream_RejectsSplitIdatWithInvalidScanlineFilterByte()
    {
        byte[] compressed = PngZlibCompressor.Compress(new byte[]
        {
            PngConstants.FilterTypeNone, 1, 2, 3, 4,
            9, 5, 6, 7, 8
        });
        byte[][] idatParts = Split(compressed, compressed.Length / 2);
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 2, PngConstants.InterlaceMethodNone)),
            CreateChunkBytes(PngConstants.ChunkIDAT, idatParts[0]),
            CreateChunkBytes(PngConstants.ChunkIDAT, idatParts[1]),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));

        Assert.Throws<InvalidDataException>(() => Png.DecodeStream(new MemoryStream(png)));
    }

    [Test]
    public void Decode_RejectsInvalidInputsAndData()
    {
        byte[] png = CreatePng(
            CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1, PngConstants.InterlaceMethodNone)),
            CreateChunkBytes(PngConstants.ChunkIDAT, PngZlibCompressor.Compress(new byte[] { 0, 1, 2, 3, 4 })),
            CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]));
        byte[] trailing = Join(png, new byte[] { 1 });
        PngDecoderOptions nullLimits = new PngDecoderOptions();
        nullLimits.Limits = null!;
        PngDecoderOptions lowLimit = new PngDecoderOptions();
        lowLimit.Limits.MaxDecompressedBytes = 4;

        Assert.Throws<ArgumentNullException>(() => Png.DecodeBytes(null!));
        Assert.Throws<ArgumentNullException>(() => Png.DecodeBytes(png, null!));
        Assert.Throws<ArgumentNullException>(() => Png.DecodeBytes(png, 0, null!));
        Assert.Throws<ArgumentNullException>(() => Png.DecodeBytes(png, nullLimits));
        Assert.Throws<ArgumentNullException>(() => Png.DecodeStream(null!));
        Assert.Throws<ArgumentNullException>(() => Png.DecodeStream(new MemoryStream(png), null!));
        Assert.Throws<ArgumentNullException>(() => Png.DecodeStream(new MemoryStream(png), nullLimits));
        Assert.Throws<ArgumentOutOfRangeException>(() => Png.DecodeBytes(png, -1, new PngDecoderOptions()));
        Assert.Throws<InvalidDataException>(() => Png.DecodeBytes(new byte[] { 1, 2, 3 }));
        Assert.Throws<InvalidDataException>(() => Png.DecodeBytes(trailing));
        Assert.Throws<InvalidDataException>(() => Png.DecodeBytes(CreatePng(CreateChunkBytes(PngConstants.ChunkIHDR, CreateIhdrData(1, 1, PngConstants.InterlaceMethodNone)))));
        Assert.Throws<OverflowException>(() => Png.DecodeBytes(png, lowLimit));
    }

    static void AssertImage(PngImage image, int width, int height, PngInterlaceMethod interlaceMethod, PngPixelFormat format, int stride, byte[] data)
    {
        Assert.That(image.Info.Width, Is.EqualTo(width));
        Assert.That(image.Info.Height, Is.EqualTo(height));
        Assert.That(image.Info.ColorType, Is.EqualTo(PngColorType.TruecolorAlpha));
        Assert.That(image.Info.BitDepth, Is.EqualTo(PngBitDepth.Eight));
        Assert.That(image.Info.InterlaceMethod, Is.EqualTo(interlaceMethod));
        Assert.That(image.Info.PixelFormat, Is.EqualTo(format));
        Assert.That(image.Pixels.Format, Is.EqualTo(format));
        Assert.That(image.Pixels.Width, Is.EqualTo(width));
        Assert.That(image.Pixels.Height, Is.EqualTo(height));
        Assert.That(image.Pixels.Stride, Is.EqualTo(stride));
        Assert.That(image.Pixels.Data, Is.EqualTo(data));
    }

    static PngIhdr CreateIhdr(int width, int height, PngInterlaceMethod interlaceMethod)
    {
        PngIhdr ihdr = new PngIhdr();
        ihdr.Width = width;
        ihdr.Height = height;
        ihdr.BitDepth = PngBitDepth.Eight;
        ihdr.ColorType = PngColorType.TruecolorAlpha;
        ihdr.CompressionMethod = PngCompressionMethod.Deflate;
        ihdr.FilterMethod = PngFilterMethod.Adaptive;
        ihdr.InterlaceMethod = interlaceMethod;
        return ihdr;
    }

    static byte[] CreateIhdrData(int width, int height, int interlaceMethod)
    {
        byte[] data = new byte[13];
        PngBinaryWriter.WriteUInt32(data, 0, (uint)width);
        PngBinaryWriter.WriteUInt32(data, 4, (uint)height);
        data[8] = PngConstants.BitDepth8;
        data[9] = PngConstants.ColorTypeTruecolorAlpha;
        data[10] = PngConstants.CompressionMethodDeflate;
        data[11] = PngConstants.FilterMethodAdaptive;
        data[12] = (byte)interlaceMethod;
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

    static byte[][] Split(byte[] data, int firstLength)
    {
        byte[] first = new byte[firstLength];
        byte[] second = new byte[data.Length - firstLength];
        Array.Copy(data, 0, first, 0, first.Length);
        Array.Copy(data, first.Length, second, 0, second.Length);
        return new byte[][] { first, second };
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
}
