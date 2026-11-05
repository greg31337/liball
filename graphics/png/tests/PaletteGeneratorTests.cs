using System.IO;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class PaletteGeneratorTests
{
    [Test]
    public void Generate_DeduplicatesRgb8ColorsInFirstSeenOrder()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Rgb8, 4, 2, 12, new byte[]
        {
            10, 20, 30, 40, 50, 60, 10, 20, 30, 70, 80, 90,
            40, 50, 60, 1, 2, 3, 70, 80, 90, 1, 2, 3
        });

        PngPalette palette = PngPaletteGenerator.Generate(buffer);

        Assert.That(palette.Entries, Has.Length.EqualTo(4));
        AssertEntry(palette.Entries[0], 10, 20, 30);
        AssertEntry(palette.Entries[1], 40, 50, 60);
        AssertEntry(palette.Entries[2], 70, 80, 90);
        AssertEntry(palette.Entries[3], 1, 2, 3);
    }

    [Test]
    public void Generate_IgnoresAlphaForRgba8AndGrayscaleAlpha8()
    {
        PngPixelBuffer rgba = CreateBuffer(PngPixelFormat.Rgba8, 3, 1, 12, new byte[]
        {
            1, 2, 3, 0,
            1, 2, 3, 255,
            4, 5, 6, 128
        });
        PngPixelBuffer grayscaleAlpha = CreateBuffer(PngPixelFormat.GrayscaleAlpha8, 3, 1, 6, new byte[]
        {
            7, 0,
            7, 255,
            9, 128
        });

        PngPalette rgbaPalette = PngPaletteGenerator.Generate(rgba);
        PngPalette grayscaleAlphaPalette = PngPaletteGenerator.Generate(grayscaleAlpha);

        Assert.That(rgbaPalette.Entries, Has.Length.EqualTo(2));
        AssertEntry(rgbaPalette.Entries[0], 1, 2, 3);
        AssertEntry(rgbaPalette.Entries[1], 4, 5, 6);
        Assert.That(grayscaleAlphaPalette.Entries, Has.Length.EqualTo(2));
        AssertEntry(grayscaleAlphaPalette.Entries[0], 7, 7, 7);
        AssertEntry(grayscaleAlphaPalette.Entries[1], 9, 9, 9);
    }

    [Test]
    public void Generate_ConvertsGrayscale8SamplesToRgbPaletteEntries()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Grayscale8, 4, 1, 4, new byte[] { 0, 128, 255, 128 });

        PngPalette palette = PngPaletteGenerator.Generate(buffer);

        Assert.That(palette.Entries, Has.Length.EqualTo(3));
        AssertEntry(palette.Entries[0], 0, 0, 0);
        AssertEntry(palette.Entries[1], 128, 128, 128);
        AssertEntry(palette.Entries[2], 255, 255, 255);
    }

    [Test]
    public void Generate_ReadsRowsUsingStride()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Rgb8, 2, 2, 8, new byte[]
        {
            1, 2, 3, 4, 5, 6, 99, 99,
            7, 8, 9, 1, 2, 3, 88, 88
        });

        PngPalette palette = PngPaletteGenerator.Generate(buffer);

        Assert.That(palette.Entries, Has.Length.EqualTo(3));
        AssertEntry(palette.Entries[0], 1, 2, 3);
        AssertEntry(palette.Entries[1], 4, 5, 6);
        AssertEntry(palette.Entries[2], 7, 8, 9);
    }

    [Test]
    public void Generate_AllowsExactly256Colors()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Rgb8, 256, 1, 768, CreateRgbRamp(256));

        PngPalette palette = PngPaletteGenerator.Generate(buffer);

        Assert.That(palette.Entries, Has.Length.EqualTo(256));
        AssertEntry(palette.Entries[0], 0, 0, 255);
        AssertEntry(palette.Entries[255], 255, 0, 0);
    }

    [Test]
    public void Generate_RejectsMoreThan256Colors()
    {
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Rgb8, 257, 1, 771, CreateRgbRamp(257));

        Assert.Throws<InvalidDataException>(() => PngPaletteGenerator.Generate(buffer));
    }

    [Test]
    public void Generate_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => PngPaletteGenerator.Generate(null!));
        Assert.Throws<ArgumentNullException>(() => PngPaletteGenerator.Generate(CreateBuffer(PngPixelFormat.Rgb8, 1, 1, 3, null!)));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPaletteGenerator.Generate(CreateBuffer(PngPixelFormat.Indexed8, 1, 1, 1, new byte[] { 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPaletteGenerator.Generate(CreateBuffer(PngPixelFormat.Rgb8, 0, 1, 3, new byte[] { 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPaletteGenerator.Generate(CreateBuffer(PngPixelFormat.Rgb8, 1, 0, 3, new byte[] { 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPaletteGenerator.Generate(CreateBuffer(PngPixelFormat.Rgb8, 2, 1, 5, new byte[] { 0, 0, 0, 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPaletteGenerator.Generate(CreateBuffer(PngPixelFormat.Rgb8, 2, 2, 6, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 })));
    }

    static byte[] CreateRgbRamp(int count)
    {
        byte[] data = new byte[count * 3];
        int index = 0;
        while (index < count)
        {
            data[index * 3] = (byte)index;
            data[index * 3 + 1] = (byte)(index >> 8);
            data[index * 3 + 2] = (byte)(255 - (index & 0xFF));
            index += 1;
        }

        return data;
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

    static void AssertEntry(PngPaletteEntry entry, int red, int green, int blue)
    {
        Assert.That(entry.Red, Is.EqualTo(red));
        Assert.That(entry.Green, Is.EqualTo(green));
        Assert.That(entry.Blue, Is.EqualTo(blue));
    }
}
