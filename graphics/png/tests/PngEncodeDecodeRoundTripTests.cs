using System.IO;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class PngEncodeDecodeRoundTripTests
{
    [Test]
    public void EncodeStreamDecodeStream_RoundTripsPixelsAndPreservedMetadata()
    {
        PngImage image = CreateImage(CreateBuffer(PngPixelFormat.GrayscaleAlpha8, 2, 2, 6, new byte[]
        {
            1, 255, 2, 128, 99, 99,
            3, 64, 4, 0, 88, 88
        }));
        image.Metadata.Chunks = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkGamma, new byte[] { 0, 0, 177, 143 }),
            CreateChunk(PngConstants.ChunkPhysicalPixels, new byte[] { 0, 0, 11, 19, 0, 0, 11, 19, 1 }),
            CreateChunk(PngConstants.ChunkText, new byte[] { 84, 105, 116, 108, 101, 0, 82, 84 })
        };
        MemoryStream encodedStream = new MemoryStream();

        Png.EncodeToStream(encodedStream, image, CreateOptions(PngColorType.GrayscaleAlpha, PngBitDepth.Eight, PngInterlaceMethod.None));
        byte[] encoded = encodedStream.ToArray();
        PngChunkListResult chunkList = Png.ReadChunks(new MemoryStream(encoded));
        PngImage decoded = Png.DecodeStream(new MemoryStream(encoded));

        Assert.That(Png.ValidateBytes(encoded).IsValid, Is.True);
        AssertChunkTypes(chunkList.Chunks, new string[]
        {
            PngConstants.ChunkIHDR,
            PngConstants.ChunkGamma,
            PngConstants.ChunkPhysicalPixels,
            PngConstants.ChunkIDAT,
            PngConstants.ChunkText,
            PngConstants.ChunkIEND
        });
        AssertImage(decoded, 2, 2, PngInterlaceMethod.None, PngPixelFormat.GrayscaleAlpha8, PngColorType.GrayscaleAlpha, PngBitDepth.Eight, 4, new byte[]
        {
            1, 255, 2, 128,
            3, 64, 4, 0
        });
        Assert.That(HasChunk(decoded.Metadata.Chunks, PngConstants.ChunkGamma), Is.True);
        Assert.That(HasChunk(decoded.Metadata.Chunks, PngConstants.ChunkPhysicalPixels), Is.True);
        Assert.That(HasChunk(decoded.Metadata.Chunks, PngConstants.ChunkText), Is.True);
    }

    [Test]
    public void ReadChunksWriteChunksDecode_RoundTripsEncodedIndexedDatastream()
    {
        PngImage image = CreateImage(CreateBuffer(PngPixelFormat.Indexed4, 3, 2, 2, new byte[]
        {
            0x12, 0x30,
            0x21, 0x30
        }));
        image.Metadata.Palette = CreatePalette(
            CreatePaletteEntry(0, 0, 0),
            CreatePaletteEntry(10, 20, 30),
            CreatePaletteEntry(40, 50, 60),
            CreatePaletteEntry(70, 80, 90));

        byte[] encoded = Png.EncodeToBytes(image, CreateOptions(PngColorType.IndexedColor, PngBitDepth.Four, PngInterlaceMethod.None));
        PngChunkListResult chunkList = Png.ReadChunks(encoded);
        byte[] rewritten = Png.WriteChunks(chunkList.Chunks);
        PngImage decoded = Png.DecodeBytes(rewritten);

        Assert.That(Png.ValidateBytes(rewritten).IsValid, Is.True);
        AssertChunkTypes(Png.ReadChunks(rewritten).Chunks, new string[]
        {
            PngConstants.ChunkIHDR,
            PngConstants.ChunkPLTE,
            PngConstants.ChunkIDAT,
            PngConstants.ChunkIEND
        });
        AssertImage(decoded, 3, 2, PngInterlaceMethod.None, PngPixelFormat.Indexed4, PngColorType.IndexedColor, PngBitDepth.Four, 2, new byte[]
        {
            0x12, 0x30,
            0x21, 0x30
        });
        AssertPaletteEntry(decoded.Metadata.Palette.Entries[2], 40, 50, 60);
    }

    [Test]
    public void DecodeThenReencode_RoundTripsAdam7Rgba8Image()
    {
        byte[] pixels = new byte[]
        {
            1, 2, 3, 255, 4, 5, 6, 128, 7, 8, 9, 0,
            10, 11, 12, 255, 13, 14, 15, 128, 16, 17, 18, 0
        };
        PngImage original = Png.CreateImage(3, 2, PngPixelFormat.Rgba8, pixels);
        PngEncoderOptions options = CreateOptions(PngColorType.TruecolorAlpha, PngBitDepth.Eight, PngInterlaceMethod.Adam7);

        byte[] encoded = Png.EncodeToBytes(original, options);
        PngImage decoded = Png.DecodeBytes(encoded);
        byte[] reencoded = Png.EncodeToBytes(decoded, options);
        PngImage decodedAgain = Png.DecodeBytes(reencoded);

        Assert.That(Png.ValidateBytes(encoded).IsValid, Is.True);
        Assert.That(Png.ValidateBytes(reencoded).IsValid, Is.True);
        AssertImage(decoded, 3, 2, PngInterlaceMethod.Adam7, PngPixelFormat.Rgba8, PngColorType.TruecolorAlpha, PngBitDepth.Eight, 12, pixels);
        AssertImage(decodedAgain, 3, 2, PngInterlaceMethod.Adam7, PngPixelFormat.Rgba8, PngColorType.TruecolorAlpha, PngBitDepth.Eight, 12, pixels);
    }

    static PngImage CreateImage(PngPixelBuffer pixels)
    {
        PngImage image = new PngImage();
        image.Pixels = pixels;
        return image;
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

    static PngEncoderOptions CreateOptions(PngColorType colorType, PngBitDepth bitDepth, PngInterlaceMethod interlaceMethod)
    {
        PngEncoderOptions options = new PngEncoderOptions();
        options.ColorType = colorType;
        options.BitDepth = bitDepth;
        options.InterlaceMethod = interlaceMethod;
        options.UseAutomaticColorType = false;
        options.UseAutomaticBitDepth = false;
        options.UseFixedFilter = true;
        options.FixedFilterType = PngFilterType.None;
        return options;
    }

    static PngChunk CreateChunk(string type, byte[] data)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = type;
        chunk.Data = data;
        chunk.Length = (uint)data.Length;
        return chunk;
    }

    static PngPalette CreatePalette(params PngPaletteEntry[] entries)
    {
        PngPalette palette = new PngPalette();
        palette.Entries = entries;
        return palette;
    }

    static PngPaletteEntry CreatePaletteEntry(int red, int green, int blue)
    {
        PngPaletteEntry entry = new PngPaletteEntry();
        entry.Red = (byte)red;
        entry.Green = (byte)green;
        entry.Blue = (byte)blue;
        return entry;
    }

    static void AssertImage(PngImage image, int width, int height, PngInterlaceMethod interlaceMethod, PngPixelFormat format, PngColorType colorType, PngBitDepth bitDepth, int stride, byte[] data)
    {
        Assert.That(image.Info.Width, Is.EqualTo(width));
        Assert.That(image.Info.Height, Is.EqualTo(height));
        Assert.That(image.Info.ColorType, Is.EqualTo(colorType));
        Assert.That(image.Info.BitDepth, Is.EqualTo(bitDepth));
        Assert.That(image.Info.InterlaceMethod, Is.EqualTo(interlaceMethod));
        Assert.That(image.Info.PixelFormat, Is.EqualTo(format));
        Assert.That(image.Pixels.Format, Is.EqualTo(format));
        Assert.That(image.Pixels.Width, Is.EqualTo(width));
        Assert.That(image.Pixels.Height, Is.EqualTo(height));
        Assert.That(image.Pixels.Stride, Is.EqualTo(stride));
        Assert.That(image.Pixels.Data, Is.EqualTo(data));
    }

    static void AssertChunkTypes(PngChunk[] chunks, string[] expectedTypes)
    {
        Assert.That(chunks, Has.Length.EqualTo(expectedTypes.Length));
        int index = 0;
        while (index < expectedTypes.Length)
        {
            Assert.That(chunks[index].Type, Is.EqualTo(expectedTypes[index]));
            index += 1;
        }
    }

    static void AssertPaletteEntry(PngPaletteEntry entry, int red, int green, int blue)
    {
        Assert.That(entry.Red, Is.EqualTo(red));
        Assert.That(entry.Green, Is.EqualTo(green));
        Assert.That(entry.Blue, Is.EqualTo(blue));
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
