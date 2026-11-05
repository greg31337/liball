using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class Adam7DecoderTests
{
    [Test]
    public void Decode_Maps8BitAdam7PassPixelsToFinalRows()
    {
        PngIhdr ihdr = CreateIhdr(5, 3, PngColorType.Grayscale, PngBitDepth.Eight);
        byte[] filteredScanlines = new byte[]
        {
            PngConstants.FilterTypeNone, 0,
            PngConstants.FilterTypeNone, 4,
            PngConstants.FilterTypeNone, 2,
            PngConstants.FilterTypeNone, 10, 12, 14,
            PngConstants.FilterTypeNone, 1, 3,
            PngConstants.FilterTypeNone, 11, 13,
            PngConstants.FilterTypeNone, 5, 6, 7, 8, 9
        };

        byte[] imageData = PngAdam7Decoder.Decode(filteredScanlines, ihdr);

        Assert.That(imageData, Is.EqualTo(new byte[]
        {
            0, 1, 2, 3, 4,
            5, 6, 7, 8, 9,
            10, 11, 12, 13, 14
        }));
    }

    [Test]
    public void Decode_MapsMultiBytePixelsToFinalRows()
    {
        PngIhdr ihdr = CreateIhdr(2, 2, PngColorType.Truecolor, PngBitDepth.Eight);
        byte[] filteredScanlines = new byte[]
        {
            PngConstants.FilterTypeNone, 1, 2, 3,
            PngConstants.FilterTypeNone, 4, 5, 6,
            PngConstants.FilterTypeNone, 7, 8, 9, 10, 11, 12
        };

        byte[] imageData = PngAdam7Decoder.Decode(filteredScanlines, ihdr);

        Assert.That(imageData, Is.EqualTo(new byte[]
        {
            1, 2, 3, 4, 5, 6,
            7, 8, 9, 10, 11, 12
        }));
    }

    [Test]
    public void Decode_DecodesFilteredRowsWithinEachPass()
    {
        PngIhdr ihdr = CreateIhdr(1, 4, PngColorType.Grayscale, PngBitDepth.Eight);
        byte[] filteredScanlines = new byte[]
        {
            PngConstants.FilterTypeNone, 10,
            PngConstants.FilterTypeNone, 30,
            PngConstants.FilterTypeNone, 20,
            PngConstants.FilterTypeUp, 5
        };

        byte[] imageData = PngAdam7Decoder.Decode(filteredScanlines, ihdr);

        Assert.That(imageData, Is.EqualTo(new byte[] { 10, 20, 30, 25 }));
    }

    [Test]
    public void Decode_MapsPackedOneBitSamplesToFinalRows()
    {
        PngIhdr ihdr = CreateIhdr(5, 3, PngColorType.Grayscale, PngBitDepth.One);
        byte[] filteredScanlines = new byte[]
        {
            PngConstants.FilterTypeNone, 128,
            PngConstants.FilterTypeNone, 128,
            PngConstants.FilterTypeNone, 128,
            PngConstants.FilterTypeNone, 160,
            PngConstants.FilterTypeNone, 0,
            PngConstants.FilterTypeNone, 128,
            PngConstants.FilterTypeNone, 80
        };

        byte[] imageData = PngAdam7Decoder.Decode(filteredScanlines, ihdr);

        Assert.That(imageData, Is.EqualTo(new byte[] { 168, 80, 200 }));
    }

    [Test]
    public void Decode_UsesOffset()
    {
        PngIhdr ihdr = CreateIhdr(1, 1, PngColorType.Grayscale, PngBitDepth.Eight);
        byte[] filteredScanlines = new byte[] { 99, PngConstants.FilterTypeNone, 42 };

        byte[] imageData = PngAdam7Decoder.Decode(filteredScanlines, 1, ihdr);

        Assert.That(imageData, Is.EqualTo(new byte[] { 42 }));
    }

    [Test]
    public void Decode_RejectsInvalidInputs()
    {
        PngIhdr ihdr = CreateIhdr(1, 1, PngColorType.Grayscale, PngBitDepth.Eight);
        PngIhdr nonInterlaced = CreateIhdr(1, 1, PngColorType.Grayscale, PngBitDepth.Eight);
        nonInterlaced.InterlaceMethod = PngInterlaceMethod.None;

        Assert.Throws<ArgumentNullException>(() => PngAdam7Decoder.Decode(null!, ihdr));
        Assert.Throws<ArgumentNullException>(() => PngAdam7Decoder.Decode(new byte[0], null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAdam7Decoder.Decode(new byte[2], -1, ihdr));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAdam7Decoder.Decode(new byte[2], 3, ihdr));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAdam7Decoder.Decode(new byte[2], nonInterlaced));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAdam7Decoder.Decode(new byte[] { PngConstants.FilterTypeNone }, ihdr));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAdam7Decoder.Decode(new byte[] { PngConstants.FilterTypeNone, 1, 2 }, ihdr));
        Assert.Throws<InvalidDataException>(() => PngAdam7Decoder.Decode(new byte[] { 9, 1 }, ihdr));
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
        ihdr.InterlaceMethod = PngInterlaceMethod.Adam7;
        return ihdr;
    }
}
