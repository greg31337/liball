using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class NonInterlacedImageDecoderTests
{
    [Test]
    public void Decode_RemovesFiltersAndReturnsNativePixelBuffer()
    {
        PngIhdr ihdr = CreateIhdr(3, 2, PngColorType.Grayscale, PngBitDepth.Eight);
        byte[] filteredScanlines = new byte[]
        {
            PngConstants.FilterTypeNone, 1, 2, 3,
            PngConstants.FilterTypeSub, 5, 2, 3
        };

        PngPixelBuffer buffer = PngNonInterlacedImageDecoder.Decode(filteredScanlines, ihdr);

        Assert.That(buffer.Format, Is.EqualTo(PngPixelFormat.Grayscale8));
        Assert.That(buffer.Width, Is.EqualTo(3));
        Assert.That(buffer.Height, Is.EqualTo(2));
        Assert.That(buffer.Stride, Is.EqualTo(3));
        Assert.That(buffer.Data, Is.EqualTo(new byte[] { 1, 2, 3, 5, 7, 10 }));
    }

    [Test]
    public void Decode_DecodesRowsUsingPreviousRows()
    {
        PngIhdr ihdr = CreateIhdr(3, 2, PngColorType.Grayscale, PngBitDepth.Eight);
        byte[] filteredScanlines = new byte[]
        {
            PngConstants.FilterTypeNone, 10, 20, 30,
            PngConstants.FilterTypeUp, 1, 2, 3
        };

        PngPixelBuffer buffer = PngNonInterlacedImageDecoder.Decode(filteredScanlines, ihdr);

        Assert.That(buffer.Data, Is.EqualTo(new byte[] { 10, 20, 30, 11, 22, 33 }));
    }

    [Test]
    public void Decode_DecodesMixedFiltersAcrossRgbRows()
    {
        PngIhdr ihdr = CreateIhdr(3, 5, PngColorType.Truecolor, PngBitDepth.Eight);
        byte[] filteredScanlines = new byte[]
        {
            PngConstants.FilterTypeNone, 10, 20, 30, 40, 50, 60, 250, 5, 15,
            PngConstants.FilterTypeSub, 15, 25, 35, 45, 55, 65, 201, 196, 191,
            PngConstants.FilterTypeUp, 5, 251, 5, 10, 10, 10, 5, 5, 10,
            PngConstants.FilterTypeAverage, 20, 30, 30, 40, 35, 40, 226, 229, 229,
            PngConstants.FilterTypePaeth, 5, 5, 15, 5, 15, 15, 20, 10, 15
        };

        PngPixelBuffer buffer = PngNonInterlacedImageDecoder.Decode(filteredScanlines, ihdr);

        Assert.That(buffer.Format, Is.EqualTo(PngPixelFormat.Rgb8));
        Assert.That(buffer.Width, Is.EqualTo(3));
        Assert.That(buffer.Height, Is.EqualTo(5));
        Assert.That(buffer.Stride, Is.EqualTo(9));
        Assert.That(buffer.Data, Is.EqualTo(new byte[]
        {
            10, 20, 30, 40, 50, 60, 250, 5, 15,
            15, 25, 35, 60, 80, 100, 5, 20, 35,
            20, 20, 40, 70, 90, 110, 10, 25, 45,
            30, 40, 50, 90, 100, 120, 20, 35, 55,
            35, 45, 65, 95, 115, 135, 40, 45, 70
        }));
    }

    [Test]
    public void Decode_KeepsPackedRowsInNativeBuffer()
    {
        PngIhdr ihdr = CreateIhdr(5, 2, PngColorType.Grayscale, PngBitDepth.One);
        byte[] filteredScanlines = new byte[]
        {
            PngConstants.FilterTypeNone, 168,
            PngConstants.FilterTypeNone, 80
        };

        PngPixelBuffer buffer = PngNonInterlacedImageDecoder.Decode(filteredScanlines, ihdr);

        Assert.That(buffer.Format, Is.EqualTo(PngPixelFormat.Grayscale1));
        Assert.That(buffer.Stride, Is.EqualTo(1));
        Assert.That(buffer.Data, Is.EqualTo(new byte[] { 168, 80 }));
    }

    [Test]
    public void Decode_UsesOffset()
    {
        PngIhdr ihdr = CreateIhdr(1, 1, PngColorType.Grayscale, PngBitDepth.Eight);
        byte[] filteredScanlines = new byte[] { 99, PngConstants.FilterTypeNone, 42 };

        PngPixelBuffer buffer = PngNonInterlacedImageDecoder.Decode(filteredScanlines, 1, ihdr);

        Assert.That(buffer.Data, Is.EqualTo(new byte[] { 42 }));
    }

    [TestCase(PngColorType.Grayscale, PngBitDepth.One, PngPixelFormat.Grayscale1)]
    [TestCase(PngColorType.Grayscale, PngBitDepth.Two, PngPixelFormat.Grayscale2)]
    [TestCase(PngColorType.Grayscale, PngBitDepth.Four, PngPixelFormat.Grayscale4)]
    [TestCase(PngColorType.Grayscale, PngBitDepth.Eight, PngPixelFormat.Grayscale8)]
    [TestCase(PngColorType.Grayscale, PngBitDepth.Sixteen, PngPixelFormat.Grayscale16)]
    [TestCase(PngColorType.IndexedColor, PngBitDepth.One, PngPixelFormat.Indexed1)]
    [TestCase(PngColorType.IndexedColor, PngBitDepth.Two, PngPixelFormat.Indexed2)]
    [TestCase(PngColorType.IndexedColor, PngBitDepth.Four, PngPixelFormat.Indexed4)]
    [TestCase(PngColorType.IndexedColor, PngBitDepth.Eight, PngPixelFormat.Indexed8)]
    [TestCase(PngColorType.Truecolor, PngBitDepth.Eight, PngPixelFormat.Rgb8)]
    [TestCase(PngColorType.Truecolor, PngBitDepth.Sixteen, PngPixelFormat.Rgb16)]
    [TestCase(PngColorType.GrayscaleAlpha, PngBitDepth.Eight, PngPixelFormat.GrayscaleAlpha8)]
    [TestCase(PngColorType.GrayscaleAlpha, PngBitDepth.Sixteen, PngPixelFormat.GrayscaleAlpha16)]
    [TestCase(PngColorType.TruecolorAlpha, PngBitDepth.Eight, PngPixelFormat.Rgba8)]
    [TestCase(PngColorType.TruecolorAlpha, PngBitDepth.Sixteen, PngPixelFormat.Rgba16)]
    public void GetNativePixelFormat_MapsValidPngFormats(PngColorType colorType, PngBitDepth bitDepth, PngPixelFormat pixelFormat)
    {
        Assert.That(PngNonInterlacedImageDecoder.GetNativePixelFormat(colorType, bitDepth), Is.EqualTo(pixelFormat));
    }

    [Test]
    public void Decode_RejectsInvalidInputs()
    {
        PngIhdr ihdr = CreateIhdr(1, 1, PngColorType.Grayscale, PngBitDepth.Eight);
        PngIhdr interlaced = CreateIhdr(1, 1, PngColorType.Grayscale, PngBitDepth.Eight);
        interlaced.InterlaceMethod = PngInterlaceMethod.Adam7;

        Assert.Throws<ArgumentNullException>(() => PngNonInterlacedImageDecoder.Decode(null!, ihdr));
        Assert.Throws<ArgumentNullException>(() => PngNonInterlacedImageDecoder.Decode(new byte[0], null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngNonInterlacedImageDecoder.Decode(new byte[2], -1, ihdr));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngNonInterlacedImageDecoder.Decode(new byte[2], 3, ihdr));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngNonInterlacedImageDecoder.Decode(new byte[2], interlaced));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngNonInterlacedImageDecoder.Decode(new byte[] { PngConstants.FilterTypeNone }, ihdr));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngNonInterlacedImageDecoder.Decode(new byte[] { PngConstants.FilterTypeNone, 1, 2 }, ihdr));
        Assert.Throws<InvalidDataException>(() => PngNonInterlacedImageDecoder.Decode(new byte[] { 9, 1 }, ihdr));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngNonInterlacedImageDecoder.GetNativePixelFormat(PngColorType.Truecolor, PngBitDepth.Four));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngNonInterlacedImageDecoder.GetNativePixelFormat((PngColorType)99, PngBitDepth.Eight));
    }

    static PngIhdr CreateIhdr(int width, int height, PngColorType colorType, PngBitDepth bitDepth)
    {
        PngIhdr ihdr = new PngIhdr();
        ihdr.Width = width;
        ihdr.Height = height;
        ihdr.ColorType = colorType;
        ihdr.BitDepth = bitDepth;
        ihdr.CompressionMethod = PngCompressionMethod.Deflate;
        ihdr.FilterMethod = PngFilterMethod.Adaptive;
        ihdr.InterlaceMethod = PngInterlaceMethod.None;
        return ihdr;
    }
}
