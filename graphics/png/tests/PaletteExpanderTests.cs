using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class PaletteExpanderTests
{
    [Test]
    public void ExpandToRgb8_ExpandsIndexed8PixelsUsingPaletteEntries()
    {
        PngPixelBuffer indexed = CreateBuffer(PngPixelFormat.Indexed8, 3, 2, 3, new byte[] { 0, 2, 1, 2, 0, 1 });
        PngPalette palette = CreatePalette();

        PngPixelBuffer rgb = PngPaletteExpander.ExpandToRgb8(indexed, palette);

        AssertRgb(rgb, 3, 2, new byte[]
        {
            10, 20, 30, 70, 80, 90, 40, 50, 60,
            70, 80, 90, 10, 20, 30, 40, 50, 60
        });
    }

    [Test]
    public void ExpandToRgb8_ExpandsPackedIndexedPixelsUsingStride()
    {
        PngPixelBuffer indexed = CreateBuffer(PngPixelFormat.Indexed2, 3, 2, 2, new byte[] { 0b00011000, 99, 0b11100100, 99 });
        PngPalette palette = CreatePalette();

        PngPixelBuffer rgb = PngPaletteExpander.ExpandToRgb8(indexed, palette);

        AssertRgb(rgb, 3, 2, new byte[]
        {
            10, 20, 30, 40, 50, 60, 70, 80, 90,
            100, 110, 120, 70, 80, 90, 40, 50, 60
        });
    }

    [Test]
    public void ExpandToRgb8_AcceptsPlteChunk()
    {
        PngPixelBuffer indexed = CreateBuffer(PngPixelFormat.Indexed8, 2, 1, 2, new byte[] { 1, 0 });
        PngChunk plte = CreatePlteChunk();

        PngPixelBuffer rgb = PngPaletteExpander.ExpandToRgb8(indexed, plte);

        AssertRgb(rgb, 2, 1, new byte[] { 40, 50, 60, 10, 20, 30 });
    }

    [Test]
    public void ExpandToRgb8_ReturnsIndependentBuffer()
    {
        PngPixelBuffer indexed = CreateBuffer(PngPixelFormat.Indexed8, 1, 1, 1, new byte[] { 0 });
        PngPalette palette = CreatePalette();

        PngPixelBuffer rgb = PngPaletteExpander.ExpandToRgb8(indexed, palette);
        indexed.Data[0] = 1;
        palette.Entries[0].Red = 99;

        AssertRgb(rgb, 1, 1, new byte[] { 10, 20, 30 });
    }

    [Test]
    public void ExpandToRgb8_RejectsInvalidInputs()
    {
        PngPixelBuffer indexed = CreateBuffer(PngPixelFormat.Indexed8, 1, 1, 1, new byte[] { 0 });
        PngPalette palette = CreatePalette();
        PngPalette emptyPalette = new PngPalette();
        PngPalette nullEntries = new PngPalette();
        nullEntries.Entries = null!;
        PngPalette nullEntry = new PngPalette();
        nullEntry.Entries = new PngPaletteEntry[] { null! };

        Assert.Throws<ArgumentNullException>(() => PngPaletteExpander.ExpandToRgb8(null!, palette));
        Assert.Throws<ArgumentNullException>(() => PngPaletteExpander.ExpandToRgb8(indexed, (PngPalette)null!));
        Assert.Throws<ArgumentNullException>(() => PngPaletteExpander.ExpandToRgb8(indexed, (PngChunk)null!));
        Assert.Throws<ArgumentNullException>(() => PngPaletteExpander.ExpandToRgb8(indexed, nullEntries));
        Assert.Throws<ArgumentNullException>(() => PngPaletteExpander.ExpandToRgb8(indexed, nullEntry));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPaletteExpander.ExpandToRgb8(indexed, emptyPalette));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPaletteExpander.ExpandToRgb8(CreateBuffer(PngPixelFormat.Grayscale8, 1, 1, 1, new byte[] { 0 }), palette));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPaletteExpander.ExpandToRgb8(CreateBuffer(PngPixelFormat.Indexed8, 1, 1, 1, new byte[] { 4 }), palette));
    }

    static void AssertRgb(PngPixelBuffer rgb, int width, int height, byte[] data)
    {
        Assert.That(rgb.Format, Is.EqualTo(PngPixelFormat.Rgb8));
        Assert.That(rgb.Width, Is.EqualTo(width));
        Assert.That(rgb.Height, Is.EqualTo(height));
        Assert.That(rgb.Stride, Is.EqualTo(width * 3));
        Assert.That(rgb.Data, Is.EqualTo(data));
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

    static PngChunk CreatePlteChunk()
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = PngConstants.ChunkPLTE;
        chunk.Data = new byte[] { 10, 20, 30, 40, 50, 60 };
        chunk.Length = 6;
        return chunk;
    }

    static PngPaletteEntry CreateEntry(byte red, byte green, byte blue)
    {
        PngPaletteEntry entry = new PngPaletteEntry();
        entry.Red = red;
        entry.Green = green;
        entry.Blue = blue;
        return entry;
    }
}
