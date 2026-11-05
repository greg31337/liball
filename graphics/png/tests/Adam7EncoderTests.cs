using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class Adam7EncoderTests
{
    [Test]
    public void Encode_Maps8BitFinalRowsToAdam7PassOrder()
    {
        PngIhdr ihdr = CreateIhdr(5, 3, PngColorType.Grayscale, PngBitDepth.Eight);
        PngEncoderOptions options = CreateFixedOptions(PngFilterType.None);
        byte[] imageData = new byte[]
        {
            0, 1, 2, 3, 4,
            5, 6, 7, 8, 9,
            10, 11, 12, 13, 14
        };

        byte[] filteredScanlines = PngAdam7Encoder.Encode(imageData, ihdr, options);

        Assert.That(filteredScanlines, Is.EqualTo(new byte[]
        {
            PngConstants.FilterTypeNone, 0,
            PngConstants.FilterTypeNone, 4,
            PngConstants.FilterTypeNone, 2,
            PngConstants.FilterTypeNone, 10, 12, 14,
            PngConstants.FilterTypeNone, 1, 3,
            PngConstants.FilterTypeNone, 11, 13,
            PngConstants.FilterTypeNone, 5, 6, 7, 8, 9
        }));
    }

    [Test]
    public void Encode_MapsMultiBytePixelsToAdam7PassOrder()
    {
        PngIhdr ihdr = CreateIhdr(2, 2, PngColorType.Truecolor, PngBitDepth.Eight);
        PngEncoderOptions options = CreateFixedOptions(PngFilterType.None);
        byte[] imageData = new byte[]
        {
            1, 2, 3, 4, 5, 6,
            7, 8, 9, 10, 11, 12
        };

        byte[] filteredScanlines = PngAdam7Encoder.Encode(imageData, ihdr, options);

        Assert.That(filteredScanlines, Is.EqualTo(new byte[]
        {
            PngConstants.FilterTypeNone, 1, 2, 3,
            PngConstants.FilterTypeNone, 4, 5, 6,
            PngConstants.FilterTypeNone, 7, 8, 9, 10, 11, 12
        }));
    }

    [Test]
    public void Encode_AppliesFiltersWithinEachPass()
    {
        PngIhdr ihdr = CreateIhdr(1, 4, PngColorType.Grayscale, PngBitDepth.Eight);
        PngEncoderOptions options = CreateFixedOptions(PngFilterType.Up);
        byte[] imageData = new byte[] { 10, 20, 30, 25 };

        byte[] filteredScanlines = PngAdam7Encoder.Encode(imageData, ihdr, options);

        Assert.That(filteredScanlines, Is.EqualTo(new byte[]
        {
            PngConstants.FilterTypeUp, 10,
            PngConstants.FilterTypeUp, 30,
            PngConstants.FilterTypeUp, 20,
            PngConstants.FilterTypeUp, 5
        }));
        Assert.That(PngAdam7Decoder.Decode(filteredScanlines, ihdr), Is.EqualTo(imageData));
    }

    [Test]
    public void Encode_MapsPackedOneBitSamplesToAdam7PassOrder()
    {
        PngIhdr ihdr = CreateIhdr(5, 3, PngColorType.Grayscale, PngBitDepth.One);
        PngEncoderOptions options = CreateFixedOptions(PngFilterType.None);
        byte[] imageData = new byte[] { 168, 80, 200 };

        byte[] filteredScanlines = PngAdam7Encoder.Encode(imageData, ihdr, options);

        Assert.That(filteredScanlines, Is.EqualTo(new byte[]
        {
            PngConstants.FilterTypeNone, 128,
            PngConstants.FilterTypeNone, 128,
            PngConstants.FilterTypeNone, 128,
            PngConstants.FilterTypeNone, 160,
            PngConstants.FilterTypeNone, 0,
            PngConstants.FilterTypeNone, 128,
            PngConstants.FilterTypeNone, 80
        }));
        Assert.That(PngAdam7Decoder.Decode(filteredScanlines, ihdr), Is.EqualTo(imageData));
    }

    [Test]
    public void Encode_UsesOffset()
    {
        PngIhdr ihdr = CreateIhdr(1, 1, PngColorType.Grayscale, PngBitDepth.Eight);
        PngEncoderOptions options = CreateFixedOptions(PngFilterType.None);
        byte[] imageData = new byte[] { 99, 42 };

        byte[] filteredScanlines = PngAdam7Encoder.Encode(imageData, 1, ihdr, options);

        Assert.That(filteredScanlines, Is.EqualTo(new byte[] { PngConstants.FilterTypeNone, 42 }));
    }

    [Test]
    public void Encode_DefaultOptionsRoundTripThroughDecoder()
    {
        PngIhdr ihdr = CreateIhdr(5, 3, PngColorType.Grayscale, PngBitDepth.Eight);
        byte[] imageData = new byte[]
        {
            0, 1, 2, 3, 4,
            5, 6, 7, 8, 9,
            10, 11, 12, 13, 14
        };

        byte[] filteredScanlines = PngAdam7Encoder.Encode(imageData, ihdr);
        byte[] decoded = PngAdam7Decoder.Decode(filteredScanlines, ihdr);

        Assert.That(decoded, Is.EqualTo(imageData));
    }

    [Test]
    public void Encode_FastStrategyRestartsAtEachAdam7Pass()
    {
        PngIhdr ihdr = CreateIhdr(1, 4, PngColorType.Grayscale, PngBitDepth.Eight);
        PngEncoderOptions options = new PngEncoderOptions();
        options.RowFilterStrategy = PngRowFilterStrategy.Fast;
        byte[] imageData = new byte[] { 10, 20, 30, 25 };

        byte[] filteredScanlines = PngAdam7Encoder.Encode(imageData, ihdr, options);

        Assert.That(filteredScanlines, Is.EqualTo(new byte[]
        {
            PngConstants.FilterTypeSub, 10,
            PngConstants.FilterTypeSub, 30,
            PngConstants.FilterTypeSub, 20,
            PngConstants.FilterTypeUp, 5
        }));
        Assert.That(PngAdam7Decoder.Decode(filteredScanlines, ihdr), Is.EqualTo(imageData));
    }

    [Test]
    public void Encode_RejectsInvalidInputs()
    {
        PngIhdr ihdr = CreateIhdr(1, 1, PngColorType.Grayscale, PngBitDepth.Eight);
        PngIhdr nonInterlaced = CreateIhdr(1, 1, PngColorType.Grayscale, PngBitDepth.Eight);
        nonInterlaced.InterlaceMethod = PngInterlaceMethod.None;
        PngEncoderOptions options = CreateFixedOptions(PngFilterType.None);
        PngEncoderOptions invalidOptions = new PngEncoderOptions();
        invalidOptions.FilterMethod = (PngFilterMethod)99;

        Assert.Throws<ArgumentNullException>(() => PngAdam7Encoder.Encode(null!, ihdr));
        Assert.Throws<ArgumentNullException>(() => PngAdam7Encoder.Encode(new byte[0], null!));
        Assert.Throws<ArgumentNullException>(() => PngAdam7Encoder.Encode(new byte[1], ihdr, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAdam7Encoder.Encode(new byte[1], -1, ihdr, options));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAdam7Encoder.Encode(new byte[1], 2, ihdr, options));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAdam7Encoder.Encode(new byte[1], nonInterlaced));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAdam7Encoder.Encode(new byte[0], ihdr));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAdam7Encoder.Encode(new byte[] { 1, 2 }, ihdr));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngAdam7Encoder.Encode(new byte[] { 1 }, ihdr, invalidOptions));
    }

    static PngEncoderOptions CreateFixedOptions(PngFilterType filterType)
    {
        PngEncoderOptions options = new PngEncoderOptions();
        options.UseFixedFilter = true;
        options.FixedFilterType = filterType;
        return options;
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
