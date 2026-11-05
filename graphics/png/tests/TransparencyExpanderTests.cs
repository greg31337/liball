using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class TransparencyExpanderTests
{
    [Test]
    public void ExpandIndexedToRgba8_CombinesPaletteAndTransparency()
    {
        PngPixelBuffer indexed = CreateBuffer(PngPixelFormat.Indexed8, 4, 1, 4, new byte[] { 0, 1, 2, 3 });
        PngPalette palette = CreatePalette();
        PngIndexedTransparency transparency = CreateIndexedTransparency(0, 128);

        PngPixelBuffer rgba = PngTransparencyExpander.ExpandIndexedToRgba8(indexed, palette, transparency);

        AssertBuffer(rgba, PngPixelFormat.Rgba8, 4, 1, 16, new byte[]
        {
            10, 20, 30, 0,
            40, 50, 60, 128,
            70, 80, 90, 255,
            100, 110, 120, 255
        });
    }

    [Test]
    public void ExpandIndexedToRgba8_ExpandsPackedIndexedRowsUsingStride()
    {
        PngPixelBuffer indexed = CreateBuffer(PngPixelFormat.Indexed2, 3, 2, 2, new byte[] { 0b00011000, 99, 0b11100100, 99 });
        PngPalette palette = CreatePalette();
        PngIndexedTransparency transparency = CreateIndexedTransparency(255, 0, 128, 64);

        PngPixelBuffer rgba = PngTransparencyExpander.ExpandIndexedToRgba8(indexed, palette, transparency);

        AssertBuffer(rgba, PngPixelFormat.Rgba8, 3, 2, 12, new byte[]
        {
            10, 20, 30, 255,
            40, 50, 60, 0,
            70, 80, 90, 128,
            100, 110, 120, 64,
            70, 80, 90, 128,
            40, 50, 60, 0
        });
    }

    [Test]
    public void ExpandIndexedToRgba8_AcceptsChunkInputs()
    {
        PngPixelBuffer indexed = CreateBuffer(PngPixelFormat.Indexed8, 2, 1, 2, new byte[] { 1, 0 });
        PngChunk plte = CreatePlteChunk();
        PngChunk trns = PngTransparencyWriter.WriteIndexedChunk(CreateIndexedTransparency(0, 200));

        PngPixelBuffer rgba = PngTransparencyExpander.ExpandIndexedToRgba8(indexed, plte, trns);

        AssertBuffer(rgba, PngPixelFormat.Rgba8, 2, 1, 8, new byte[] { 40, 50, 60, 200, 10, 20, 30, 0 });
    }

    [Test]
    public void ExpandGrayscaleToAlpha_ExpandsGrayscale8UsingTransparentSample()
    {
        PngPixelBuffer grayscale = CreateBuffer(PngPixelFormat.Grayscale8, 3, 2, 4, new byte[] { 7, 8, 9, 99, 8, 7, 1, 99 });
        PngGrayscaleTransparency transparency = CreateGrayscaleTransparency(7);

        PngPixelBuffer result = PngTransparencyExpander.ExpandGrayscaleToAlpha(grayscale, transparency);

        AssertBuffer(result, PngPixelFormat.GrayscaleAlpha8, 3, 2, 6, new byte[]
        {
            7, 0, 8, 255, 9, 255,
            8, 255, 7, 0, 1, 255
        });
    }

    [Test]
    public void ExpandGrayscaleToAlpha_ExpandsPackedGrayscaleToGrayscaleAlpha8()
    {
        PngPixelBuffer grayscale = CreateBuffer(PngPixelFormat.Grayscale2, 3, 1, 1, new byte[] { 0b00011000 });
        PngGrayscaleTransparency transparency = CreateGrayscaleTransparency(1);

        PngPixelBuffer result = PngTransparencyExpander.ExpandGrayscaleToAlpha(grayscale, transparency);

        AssertBuffer(result, PngPixelFormat.GrayscaleAlpha8, 3, 1, 6, new byte[] { 0, 255, 85, 0, 170, 255 });
    }

    [Test]
    public void ExpandGrayscaleToAlpha_ExpandsOneBitSamplesAndAppliesTransparentSample()
    {
        PngPixelBuffer grayscale = CreateBuffer(PngPixelFormat.Grayscale1, 4, 1, 1, new byte[] { 0b10100000 });
        PngGrayscaleTransparency transparency = CreateGrayscaleTransparency(1);

        PngPixelBuffer result = PngTransparencyExpander.ExpandGrayscaleToAlpha(grayscale, transparency);

        AssertBuffer(result, PngPixelFormat.GrayscaleAlpha8, 4, 1, 8, new byte[]
        {
            255, 0,
            0, 255,
            255, 0,
            0, 255
        });
    }

    [Test]
    public void ExpandGrayscaleToAlpha_ExpandsFourBitSamplesAndAppliesTransparentSample()
    {
        PngPixelBuffer grayscale = CreateBuffer(PngPixelFormat.Grayscale4, 4, 1, 2, new byte[] { 0xA0, 0xF1 });
        PngGrayscaleTransparency transparency = CreateGrayscaleTransparency(10);

        PngPixelBuffer result = PngTransparencyExpander.ExpandGrayscaleToAlpha(grayscale, transparency);

        AssertBuffer(result, PngPixelFormat.GrayscaleAlpha8, 4, 1, 8, new byte[]
        {
            170, 0,
            0, 255,
            255, 255,
            17, 255
        });
    }

    [Test]
    public void ExpandGrayscaleToAlpha_KeepsOpaqueWhenTransparentSampleExceedsBitDepth()
    {
        PngPixelBuffer grayscale = CreateBuffer(PngPixelFormat.Grayscale1, 2, 1, 1, new byte[] { 0b10000000 });
        PngGrayscaleTransparency transparency = CreateGrayscaleTransparency(2);

        PngPixelBuffer result = PngTransparencyExpander.ExpandGrayscaleToAlpha(grayscale, transparency);

        AssertBuffer(result, PngPixelFormat.GrayscaleAlpha8, 2, 1, 4, new byte[] { 255, 255, 0, 255 });
    }

    [Test]
    public void ExpandGrayscaleToAlpha_ExpandsGrayscale16UsingTransparentSample()
    {
        PngPixelBuffer grayscale = CreateBuffer(PngPixelFormat.Grayscale16, 2, 1, 4, CreateBigEndianSamples(0x1234, 0x8001));
        PngGrayscaleTransparency transparency = CreateGrayscaleTransparency(0x1234);

        PngPixelBuffer result = PngTransparencyExpander.ExpandGrayscaleToAlpha(grayscale, transparency);

        AssertBuffer(result, PngPixelFormat.GrayscaleAlpha16, 2, 1, 8, new byte[] { 18, 52, 0, 0, 128, 1, 255, 255 });
    }

    [Test]
    public void ExpandGrayscaleToAlpha_AcceptsChunkInput()
    {
        PngPixelBuffer grayscale = CreateBuffer(PngPixelFormat.Grayscale8, 2, 1, 2, new byte[] { 1, 2 });
        PngChunk trns = PngTransparencyWriter.WriteGrayscaleChunk(CreateGrayscaleTransparency(2));

        PngPixelBuffer result = PngTransparencyExpander.ExpandGrayscaleToAlpha(grayscale, trns);

        AssertBuffer(result, PngPixelFormat.GrayscaleAlpha8, 2, 1, 4, new byte[] { 1, 255, 2, 0 });
    }

    [Test]
    public void ExpandTruecolorToAlpha_ExpandsRgb8UsingTransparentColor()
    {
        PngPixelBuffer rgb = CreateBuffer(PngPixelFormat.Rgb8, 2, 2, 7, new byte[]
        {
            10, 20, 30, 1, 2, 3, 99,
            1, 2, 3, 10, 20, 30, 99
        });
        PngTruecolorTransparency transparency = CreateTruecolorTransparency(10, 20, 30);

        PngPixelBuffer result = PngTransparencyExpander.ExpandTruecolorToAlpha(rgb, transparency);

        AssertBuffer(result, PngPixelFormat.Rgba8, 2, 2, 8, new byte[]
        {
            10, 20, 30, 0, 1, 2, 3, 255,
            1, 2, 3, 255, 10, 20, 30, 0
        });
    }

    [Test]
    public void ExpandTruecolorToAlpha_KeepsRgb8OpaqueWhenTransparentColorExceeds8BitRange()
    {
        PngPixelBuffer rgb = CreateBuffer(PngPixelFormat.Rgb8, 2, 1, 6, new byte[] { 10, 20, 30, 44, 20, 30 });
        PngTruecolorTransparency transparency = CreateTruecolorTransparency(300, 20, 30);

        PngPixelBuffer result = PngTransparencyExpander.ExpandTruecolorToAlpha(rgb, transparency);

        AssertBuffer(result, PngPixelFormat.Rgba8, 2, 1, 8, new byte[]
        {
            10, 20, 30, 255,
            44, 20, 30, 255
        });
    }

    [Test]
    public void ExpandTruecolorToAlpha_ExpandsRgb16UsingTransparentColor()
    {
        PngPixelBuffer rgb = CreateBuffer(PngPixelFormat.Rgb16, 2, 1, 12, CreateBigEndianSamples(0x1234, 0x4567, 0x89AB, 0x1234, 0x4567, 0x0001));
        PngTruecolorTransparency transparency = CreateTruecolorTransparency(0x1234, 0x4567, 0x89AB);

        PngPixelBuffer result = PngTransparencyExpander.ExpandTruecolorToAlpha(rgb, transparency);

        AssertBuffer(result, PngPixelFormat.Rgba16, 2, 1, 16, new byte[]
        {
            18, 52, 69, 103, 137, 171, 0, 0,
            18, 52, 69, 103, 0, 1, 255, 255
        });
    }

    [Test]
    public void ExpandTruecolorToAlpha_AcceptsChunkInput()
    {
        PngPixelBuffer rgb = CreateBuffer(PngPixelFormat.Rgb8, 1, 1, 3, new byte[] { 3, 4, 5 });
        PngChunk trns = PngTransparencyWriter.WriteTruecolorChunk(CreateTruecolorTransparency(3, 4, 5));

        PngPixelBuffer result = PngTransparencyExpander.ExpandTruecolorToAlpha(rgb, trns);

        AssertBuffer(result, PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 3, 4, 5, 0 });
    }

    [Test]
    public void Expand_ReturnsIndependentBuffers()
    {
        PngPixelBuffer indexed = CreateBuffer(PngPixelFormat.Indexed8, 1, 1, 1, new byte[] { 0 });
        PngPalette palette = CreatePalette();
        PngIndexedTransparency transparency = CreateIndexedTransparency(0);

        PngPixelBuffer result = PngTransparencyExpander.ExpandIndexedToRgba8(indexed, palette, transparency);
        indexed.Data[0] = 1;
        palette.Entries[0].Red = 99;
        transparency.Alpha[0] = 255;

        AssertBuffer(result, PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 10, 20, 30, 0 });
    }

    [Test]
    public void ExpandIndexedToRgba8_RejectsInvalidInputs()
    {
        PngPixelBuffer indexed = CreateBuffer(PngPixelFormat.Indexed8, 1, 1, 1, new byte[] { 0 });
        PngPalette palette = CreatePalette();
        PngPalette emptyPalette = new PngPalette();
        PngPalette nullEntries = new PngPalette();
        nullEntries.Entries = null!;
        PngPalette nullEntry = new PngPalette();
        nullEntry.Entries = new PngPaletteEntry[] { null! };
        PngIndexedTransparency transparency = CreateIndexedTransparency(0);
        PngIndexedTransparency nullAlpha = new PngIndexedTransparency();
        nullAlpha.Alpha = null!;

        Assert.Throws<ArgumentNullException>(() => PngTransparencyExpander.ExpandIndexedToRgba8(null!, palette, transparency));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyExpander.ExpandIndexedToRgba8(indexed, (PngPalette)null!, transparency));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyExpander.ExpandIndexedToRgba8(indexed, palette, null!));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyExpander.ExpandIndexedToRgba8(indexed, palette, nullAlpha));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyExpander.ExpandIndexedToRgba8(indexed, nullEntries, transparency));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyExpander.ExpandIndexedToRgba8(indexed, nullEntry, transparency));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyExpander.ExpandIndexedToRgba8(indexed, (PngChunk)null!, PngTransparencyWriter.WriteIndexedChunk(transparency)));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyExpander.ExpandIndexedToRgba8(indexed, CreatePlteChunk(), null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTransparencyExpander.ExpandIndexedToRgba8(indexed, emptyPalette, transparency));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTransparencyExpander.ExpandIndexedToRgba8(CreateBuffer(PngPixelFormat.Grayscale8, 1, 1, 1, new byte[] { 0 }), palette, transparency));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTransparencyExpander.ExpandIndexedToRgba8(CreateBuffer(PngPixelFormat.Indexed8, 1, 1, 1, new byte[] { 4 }), palette, transparency));
    }

    [Test]
    public void ExpandGrayscaleToAlpha_RejectsInvalidInputs()
    {
        PngGrayscaleTransparency transparency = CreateGrayscaleTransparency(0);

        Assert.Throws<ArgumentNullException>(() => PngTransparencyExpander.ExpandGrayscaleToAlpha(null!, transparency));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyExpander.ExpandGrayscaleToAlpha(CreateBuffer(PngPixelFormat.Grayscale8, 1, 1, 1, null!), transparency));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyExpander.ExpandGrayscaleToAlpha(CreateBuffer(PngPixelFormat.Grayscale8, 1, 1, 1, new byte[] { 0 }), (PngGrayscaleTransparency)null!));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyExpander.ExpandGrayscaleToAlpha(CreateBuffer(PngPixelFormat.Grayscale8, 1, 1, 1, new byte[] { 0 }), (PngChunk)null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTransparencyExpander.ExpandGrayscaleToAlpha(CreateBuffer(PngPixelFormat.Rgb8, 1, 1, 3, new byte[] { 0, 0, 0 }), transparency));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTransparencyExpander.ExpandGrayscaleToAlpha(CreateBuffer(PngPixelFormat.Grayscale8, 0, 1, 1, new byte[] { 0 }), transparency));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTransparencyExpander.ExpandGrayscaleToAlpha(CreateBuffer(PngPixelFormat.Grayscale8, 1, 0, 1, new byte[] { 0 }), transparency));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTransparencyExpander.ExpandGrayscaleToAlpha(CreateBuffer(PngPixelFormat.Grayscale8, 2, 1, 1, new byte[] { 0, 0 }), transparency));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTransparencyExpander.ExpandGrayscaleToAlpha(CreateBuffer(PngPixelFormat.Grayscale8, 2, 2, 2, new byte[] { 0, 0, 0 }), transparency));
    }

    [Test]
    public void ExpandTruecolorToAlpha_RejectsInvalidInputs()
    {
        PngTruecolorTransparency transparency = CreateTruecolorTransparency(0, 0, 0);

        Assert.Throws<ArgumentNullException>(() => PngTransparencyExpander.ExpandTruecolorToAlpha(null!, transparency));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyExpander.ExpandTruecolorToAlpha(CreateBuffer(PngPixelFormat.Rgb8, 1, 1, 3, null!), transparency));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyExpander.ExpandTruecolorToAlpha(CreateBuffer(PngPixelFormat.Rgb8, 1, 1, 3, new byte[] { 0, 0, 0 }), (PngTruecolorTransparency)null!));
        Assert.Throws<ArgumentNullException>(() => PngTransparencyExpander.ExpandTruecolorToAlpha(CreateBuffer(PngPixelFormat.Rgb8, 1, 1, 3, new byte[] { 0, 0, 0 }), (PngChunk)null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTransparencyExpander.ExpandTruecolorToAlpha(CreateBuffer(PngPixelFormat.Grayscale8, 1, 1, 1, new byte[] { 0 }), transparency));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTransparencyExpander.ExpandTruecolorToAlpha(CreateBuffer(PngPixelFormat.Rgb8, 0, 1, 3, new byte[] { 0, 0, 0 }), transparency));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTransparencyExpander.ExpandTruecolorToAlpha(CreateBuffer(PngPixelFormat.Rgb8, 1, 0, 3, new byte[] { 0, 0, 0 }), transparency));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTransparencyExpander.ExpandTruecolorToAlpha(CreateBuffer(PngPixelFormat.Rgb8, 2, 1, 5, new byte[] { 0, 0, 0, 0, 0, 0 }), transparency));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTransparencyExpander.ExpandTruecolorToAlpha(CreateBuffer(PngPixelFormat.Rgb8, 2, 2, 6, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 }), transparency));
    }

    static void AssertBuffer(PngPixelBuffer buffer, PngPixelFormat format, int width, int height, int stride, byte[] data)
    {
        Assert.That(buffer.Format, Is.EqualTo(format));
        Assert.That(buffer.Width, Is.EqualTo(width));
        Assert.That(buffer.Height, Is.EqualTo(height));
        Assert.That(buffer.Stride, Is.EqualTo(stride));
        Assert.That(buffer.Data, Is.EqualTo(data));
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

    static PngPalette CreatePalette()
    {
        PngPalette palette = new PngPalette();
        palette.Entries = new PngPaletteEntry[]
        {
            CreateEntry(10, 20, 30),
            CreateEntry(40, 50, 60),
            CreateEntry(70, 80, 90),
            CreateEntry(100, 110, 120)
        };
        return palette;
    }

    static PngPaletteEntry CreateEntry(byte red, byte green, byte blue)
    {
        PngPaletteEntry entry = new PngPaletteEntry();
        entry.Red = red;
        entry.Green = green;
        entry.Blue = blue;
        return entry;
    }

    static PngIndexedTransparency CreateIndexedTransparency(params int[] alpha)
    {
        PngIndexedTransparency transparency = new PngIndexedTransparency();
        transparency.Alpha = new byte[alpha.Length];
        int index = 0;
        while (index < alpha.Length)
        {
            transparency.Alpha[index] = (byte)alpha[index];
            index += 1;
        }

        return transparency;
    }

    static PngGrayscaleTransparency CreateGrayscaleTransparency(int gray)
    {
        PngGrayscaleTransparency transparency = new PngGrayscaleTransparency();
        transparency.Gray = (ushort)gray;
        return transparency;
    }

    static PngTruecolorTransparency CreateTruecolorTransparency(int red, int green, int blue)
    {
        PngTruecolorTransparency transparency = new PngTruecolorTransparency();
        transparency.Red = (ushort)red;
        transparency.Green = (ushort)green;
        transparency.Blue = (ushort)blue;
        return transparency;
    }

    static PngChunk CreatePlteChunk()
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = PngConstants.ChunkPLTE;
        chunk.Data = new byte[] { 10, 20, 30, 40, 50, 60 };
        chunk.Length = 6;
        return chunk;
    }

    static byte[] CreateBigEndianSamples(params int[] samples)
    {
        byte[] data = new byte[samples.Length * 2];
        int index = 0;
        int offset = 0;
        while (index < samples.Length)
        {
            PngBinaryWriter.WriteUInt16(data, offset, (ushort)samples[index]);
            offset += 2;
            index += 1;
        }

        return data;
    }
}
