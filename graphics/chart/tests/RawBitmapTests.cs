namespace chart.Tests;

[TestFixture]
public class RawBitmapTests
{
    [Test]
    public void Constructor_InitializesDimensionsStrideFormatAndBuffer()
    {
        RawBitmap bitmap = new RawBitmap(3, 2, ChartPixelFormat.Rgba32);

        Assert.That(bitmap.Width, Is.EqualTo(3));
        Assert.That(bitmap.Height, Is.EqualTo(2));
        Assert.That(bitmap.Stride, Is.EqualTo(12));
        Assert.That(bitmap.PixelFormat, Is.EqualTo(ChartPixelFormat.Rgba32));
        Assert.That(bitmap.Buffer.Length, Is.EqualTo(24));
        Assert.That(bitmap.BytesPerPixel(), Is.EqualTo(4));
        Assert.That(bitmap.ContainsPixel(2, 1), Is.True);
        Assert.That(bitmap.ContainsPixel(3, 1), Is.False);
    }

    [Test]
    public void Factories_AcceptExplicitStrideAndBuffer()
    {
        byte[] buffer = new byte[20];
        RawBitmap allocated = RawBitmap.CreateWithStride(2, 2, 10, ChartPixelFormat.Rgb24);
        RawBitmap bitmap = RawBitmap.Wrap(2, 2, 10, ChartPixelFormat.Rgb24, buffer);

        bitmap.SetPixel(1, 1, new ChartColor(1, 2, 3, 4));

        Assert.That(bitmap.Stride, Is.EqualTo(10));
        Assert.That(bitmap.Buffer, Is.SameAs(buffer));
        Assert.That(allocated.Stride, Is.EqualTo(10));
        Assert.That(allocated.Buffer.Length, Is.EqualTo(20));
        Assert.That(bitmap.GetByteOffset(1, 1), Is.EqualTo(13));
        Assert.That(buffer[13], Is.EqualTo(1));
        Assert.That(buffer[14], Is.EqualTo(2));
        Assert.That(buffer[15], Is.EqualTo(3));
    }

    [Test]
    public void SetPixel_WritesRgbaBgraAndRgbLayouts()
    {
        ChartColor color = new ChartColor(10, 20, 30, 40);
        RawBitmap rgba = new RawBitmap(1, 1, ChartPixelFormat.Rgba32);
        RawBitmap bgra = new RawBitmap(1, 1, ChartPixelFormat.Bgra32);
        RawBitmap rgb = new RawBitmap(1, 1, ChartPixelFormat.Rgb24);

        rgba.SetPixel(0, 0, color);
        bgra.SetPixel(0, 0, color);
        rgb.SetPixel(0, 0, color);

        Assert.That(rgba.Buffer, Is.EqualTo(new byte[] { 10, 20, 30, 40 }));
        Assert.That(bgra.Buffer, Is.EqualTo(new byte[] { 30, 20, 10, 40 }));
        Assert.That(rgb.Buffer, Is.EqualTo(new byte[] { 10, 20, 30 }));
        AssertColor(rgba.GetPixel(0, 0), 10, 20, 30, 40);
        AssertColor(bgra.GetPixel(0, 0), 10, 20, 30, 40);
        AssertColor(rgb.GetPixel(0, 0), 10, 20, 30, 255);
    }

    [Test]
    public void SetPixel_WritesSingleByteFormats()
    {
        ChartColor color = new ChartColor(255, 0, 0, 123);
        RawBitmap grayscale = new RawBitmap(1, 1, ChartPixelFormat.Grayscale8);
        RawBitmap alpha = new RawBitmap(1, 1, ChartPixelFormat.Alpha8);
        RawBitmap indexed = new RawBitmap(1, 1, ChartPixelFormat.Indexed8);

        grayscale.SetPixel(0, 0, color);
        alpha.SetPixel(0, 0, color);
        indexed.SetPixel(0, 0, color);

        Assert.That(grayscale.Buffer[0], Is.EqualTo(76));
        Assert.That(alpha.Buffer[0], Is.EqualTo(123));
        Assert.That(indexed.Buffer[0], Is.EqualTo(76));
        AssertColor(grayscale.GetPixel(0, 0), 76, 76, 76, 255);
        AssertColor(alpha.GetPixel(0, 0), 0, 0, 0, 123);
        AssertColor(indexed.GetPixel(0, 0), 76, 76, 76, 255);
    }

    [Test]
    public void CopyBuffer_ReturnsIndependentCopy()
    {
        RawBitmap bitmap = new RawBitmap(1, 1, ChartPixelFormat.Rgba32);
        bitmap.SetPixel(0, 0, new ChartColor(1, 2, 3, 4));

        byte[] copy = bitmap.CopyBuffer();
        copy[0] = 99;

        Assert.That(copy, Is.EqualTo(new byte[] { 99, 2, 3, 4 }));
        Assert.That(bitmap.Buffer, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
    }

    [Test]
    public void StaticHelpers_ReturnExpectedByteCountsAndStrides()
    {
        Assert.That(RawBitmap.BytesPerPixel(ChartPixelFormat.Rgba32), Is.EqualTo(4));
        Assert.That(RawBitmap.BytesPerPixel(ChartPixelFormat.Bgra32), Is.EqualTo(4));
        Assert.That(RawBitmap.BytesPerPixel(ChartPixelFormat.Rgb24), Is.EqualTo(3));
        Assert.That(RawBitmap.BytesPerPixel(ChartPixelFormat.Grayscale8), Is.EqualTo(1));
        Assert.That(RawBitmap.BytesPerPixel(ChartPixelFormat.Alpha8), Is.EqualTo(1));
        Assert.That(RawBitmap.BytesPerPixel(ChartPixelFormat.Indexed8), Is.EqualTo(1));
        Assert.That(RawBitmap.CalculateStride(5, ChartPixelFormat.Rgb24), Is.EqualTo(15));
        Assert.That(RawBitmap.Luminance(new ChartColor(0, 255, 0, 255)), Is.EqualTo(150));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => RawBitmap.CalculateStride(int.MaxValue, ChartPixelFormat.Rgba32));
    }

    [Test]
    public void Validation_RejectsInvalidDimensionsStrideBufferAndCoordinates()
    {
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new RawBitmap(0, 1, ChartPixelFormat.Rgba32));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new RawBitmap(1, 0, ChartPixelFormat.Rgba32));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => RawBitmap.CreateWithStride(2, 2, 5, ChartPixelFormat.Rgb24));
        Assert.Throws<System.ArgumentNullException>(() => RawBitmap.Wrap(2, 2, 6, ChartPixelFormat.Rgb24, null!));
        Assert.Throws<System.ArgumentException>(() => RawBitmap.Wrap(2, 2, 6, ChartPixelFormat.Rgb24, new byte[11]));
        Assert.Throws<System.ArgumentException>(() => RawBitmap.BytesPerPixel((ChartPixelFormat)999));

        RawBitmap bitmap = new RawBitmap(1, 1, ChartPixelFormat.Rgba32);

        Assert.Throws<System.ArgumentOutOfRangeException>(() => bitmap.GetPixel(-1, 0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => bitmap.SetPixel(0, 1, ChartColor.Black()));
    }

    static void AssertColor(ChartColor color, int r, int g, int b, int a)
    {
        Assert.That(color.R, Is.EqualTo((byte)r));
        Assert.That(color.G, Is.EqualTo((byte)g));
        Assert.That(color.B, Is.EqualTo((byte)b));
        Assert.That(color.A, Is.EqualTo((byte)a));
    }
}
