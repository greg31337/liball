using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class PaletteIndexPixelConverterTests
{
    [Test]
    public void IsIndexedFormat_AcceptsOnlyIndexedFormats()
    {
        Assert.That(PngPaletteIndexPixelConverter.IsIndexedFormat(PngPixelFormat.Indexed1), Is.True);
        Assert.That(PngPaletteIndexPixelConverter.IsIndexedFormat(PngPixelFormat.Indexed2), Is.True);
        Assert.That(PngPaletteIndexPixelConverter.IsIndexedFormat(PngPixelFormat.Indexed4), Is.True);
        Assert.That(PngPaletteIndexPixelConverter.IsIndexedFormat(PngPixelFormat.Indexed8), Is.True);
        Assert.That(PngPaletteIndexPixelConverter.IsIndexedFormat(PngPixelFormat.Grayscale8), Is.False);
        Assert.That(PngPaletteIndexPixelConverter.IsIndexedFormat(PngPixelFormat.Rgba8), Is.False);
    }

    [Test]
    public void ToIndexed8_ExpandsIndexed1Rows()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Indexed1, 5, 2, 1, new byte[] { 168, 80 });

        PngPixelBuffer result = PngPaletteIndexPixelConverter.ToIndexed8(source);

        AssertIndexed8(result, 5, 2, new byte[] { 1, 0, 1, 0, 1, 0, 1, 0, 1, 0 });
    }

    [Test]
    public void ToIndexed8_ExpandsIndexed2RowsUsingStride()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Indexed2, 3, 2, 2, new byte[] { 0b00011000, 99, 0b11100100, 99 });

        PngPixelBuffer result = PngPaletteIndexPixelConverter.ToIndexed8(source);

        AssertIndexed8(result, 3, 2, new byte[] { 0, 1, 2, 3, 2, 1 });
    }

    [Test]
    public void ToIndexed8_ExpandsIndexed4Rows()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Indexed4, 3, 1, 2, new byte[] { 0x12, 0x30 });

        PngPixelBuffer result = PngPaletteIndexPixelConverter.ToIndexed8(source);

        AssertIndexed8(result, 3, 1, new byte[] { 1, 2, 3 });
    }

    [Test]
    public void ToIndexed8_CopiesIndexed8RowsUsingStride()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Indexed8, 3, 2, 4, new byte[] { 1, 2, 3, 99, 4, 5, 6, 99 });

        PngPixelBuffer result = PngPaletteIndexPixelConverter.ToIndexed8(source);

        AssertIndexed8(result, 3, 2, new byte[] { 1, 2, 3, 4, 5, 6 });
    }

    [Test]
    public void ToIndexed8_ReturnsIndependentBuffer()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Indexed8, 2, 1, 2, new byte[] { 1, 2 });

        PngPixelBuffer result = PngPaletteIndexPixelConverter.ToIndexed8(source);
        source.Data[0] = 99;

        AssertIndexed8(result, 2, 1, new byte[] { 1, 2 });
    }

    [Test]
    public void ToIndexed8_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => PngPaletteIndexPixelConverter.ToIndexed8(null!));
        Assert.Throws<ArgumentNullException>(() => PngPaletteIndexPixelConverter.ToIndexed8(CreateBuffer(PngPixelFormat.Indexed1, 1, 1, 1, null!)));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPaletteIndexPixelConverter.ToIndexed8(CreateBuffer(PngPixelFormat.Grayscale8, 1, 1, 1, new byte[] { 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPaletteIndexPixelConverter.ToIndexed8(CreateBuffer(PngPixelFormat.Indexed1, 0, 1, 1, new byte[] { 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPaletteIndexPixelConverter.ToIndexed8(CreateBuffer(PngPixelFormat.Indexed1, 1, 0, 1, new byte[] { 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPaletteIndexPixelConverter.ToIndexed8(CreateBuffer(PngPixelFormat.Indexed8, 2, 1, 1, new byte[] { 0, 1 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPaletteIndexPixelConverter.ToIndexed8(CreateBuffer(PngPixelFormat.Indexed8, 2, 2, 2, new byte[] { 0, 1, 2 })));
    }

    static void AssertIndexed8(PngPixelBuffer result, int width, int height, byte[] data)
    {
        Assert.That(result.Format, Is.EqualTo(PngPixelFormat.Indexed8));
        Assert.That(result.Width, Is.EqualTo(width));
        Assert.That(result.Height, Is.EqualTo(height));
        Assert.That(result.Stride, Is.EqualTo(width));
        Assert.That(result.Data, Is.EqualTo(data));
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
}
