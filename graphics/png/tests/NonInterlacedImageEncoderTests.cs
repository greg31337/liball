using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class NonInterlacedImageEncoderTests
{
    [Test]
    public void Encode_WritesNativeBufferRowsAsFilteredScanlines()
    {
        PngIhdr ihdr = CreateIhdr(3, 2, PngColorType.Grayscale, PngBitDepth.Eight);
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Grayscale8, 3, 2, 3, new byte[] { 1, 2, 3, 5, 7, 10 });
        PngEncoderOptions options = CreateFixedOptions(PngFilterType.None);

        byte[] filteredScanlines = PngNonInterlacedImageEncoder.Encode(buffer, ihdr, options);

        Assert.That(filteredScanlines, Is.EqualTo(new byte[]
        {
            PngConstants.FilterTypeNone, 1, 2, 3,
            PngConstants.FilterTypeNone, 5, 7, 10
        }));
    }

    [Test]
    public void Encode_UsesPreviousRowsWhenFiltering()
    {
        PngIhdr ihdr = CreateIhdr(3, 2, PngColorType.Grayscale, PngBitDepth.Eight);
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Grayscale8, 3, 2, 3, new byte[] { 10, 20, 30, 11, 22, 33 });
        PngEncoderOptions options = CreateFixedOptions(PngFilterType.Up);

        byte[] filteredScanlines = PngNonInterlacedImageEncoder.Encode(buffer, ihdr, options);

        Assert.That(filteredScanlines, Is.EqualTo(new byte[]
        {
            PngConstants.FilterTypeUp, 10, 20, 30,
            PngConstants.FilterTypeUp, 1, 2, 3
        }));
        Assert.That(PngNonInterlacedImageDecoder.Decode(filteredScanlines, ihdr).Data, Is.EqualTo(buffer.Data));
    }

    [Test]
    public void Encode_ReadsRowsUsingStride()
    {
        PngIhdr ihdr = CreateIhdr(3, 2, PngColorType.Grayscale, PngBitDepth.Eight);
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Grayscale8, 3, 2, 4, new byte[] { 1, 2, 3, 99, 4, 5, 6, 99 });
        PngEncoderOptions options = CreateFixedOptions(PngFilterType.None);

        byte[] filteredScanlines = PngNonInterlacedImageEncoder.Encode(buffer, ihdr, options);

        Assert.That(filteredScanlines, Is.EqualTo(new byte[]
        {
            PngConstants.FilterTypeNone, 1, 2, 3,
            PngConstants.FilterTypeNone, 4, 5, 6
        }));
    }

    [Test]
    public void Encode_KeepsPackedRowsInNativeLayout()
    {
        PngIhdr ihdr = CreateIhdr(5, 2, PngColorType.Grayscale, PngBitDepth.One);
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Grayscale1, 5, 2, 1, new byte[] { 168, 80 });
        PngEncoderOptions options = CreateFixedOptions(PngFilterType.None);

        byte[] filteredScanlines = PngNonInterlacedImageEncoder.Encode(buffer, ihdr, options);

        Assert.That(filteredScanlines, Is.EqualTo(new byte[]
        {
            PngConstants.FilterTypeNone, 168,
            PngConstants.FilterTypeNone, 80
        }));
        Assert.That(PngNonInterlacedImageDecoder.Decode(filteredScanlines, ihdr).Data, Is.EqualTo(buffer.Data));
    }

    [Test]
    public void Encode_DefaultOptionsRoundTripThroughDecoder()
    {
        PngIhdr ihdr = CreateIhdr(3, 2, PngColorType.Grayscale, PngBitDepth.Eight);
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Grayscale8, 3, 2, 3, new byte[] { 1, 2, 3, 5, 7, 10 });

        byte[] filteredScanlines = PngNonInterlacedImageEncoder.Encode(buffer, ihdr);
        PngPixelBuffer decoded = PngNonInterlacedImageDecoder.Decode(filteredScanlines, ihdr);

        Assert.That(decoded.Format, Is.EqualTo(buffer.Format));
        Assert.That(decoded.Width, Is.EqualTo(buffer.Width));
        Assert.That(decoded.Height, Is.EqualTo(buffer.Height));
        Assert.That(decoded.Stride, Is.EqualTo(buffer.Stride));
        Assert.That(decoded.Data, Is.EqualTo(buffer.Data));
    }

    [Test]
    public void Encode_FastStrategyUsesSubThenUpAcrossRows()
    {
        PngIhdr ihdr = CreateIhdr(3, 2, PngColorType.Grayscale, PngBitDepth.Eight);
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Grayscale8, 3, 2, 3, new byte[] { 10, 20, 30, 11, 22, 33 });
        PngEncoderOptions options = new PngEncoderOptions();
        options.RowFilterStrategy = PngRowFilterStrategy.Fast;

        byte[] filteredScanlines = PngNonInterlacedImageEncoder.Encode(buffer, ihdr, options);

        Assert.That(filteredScanlines, Is.EqualTo(new byte[]
        {
            PngConstants.FilterTypeSub, 10, 10, 10,
            PngConstants.FilterTypeUp, 1, 2, 3
        }));
        Assert.That(PngNonInterlacedImageDecoder.Decode(filteredScanlines, ihdr).Data, Is.EqualTo(buffer.Data));
    }

    [Test]
    public void Encode_RejectsInvalidInputs()
    {
        PngIhdr ihdr = CreateIhdr(1, 1, PngColorType.Grayscale, PngBitDepth.Eight);
        PngIhdr interlaced = CreateIhdr(1, 1, PngColorType.Grayscale, PngBitDepth.Eight);
        interlaced.InterlaceMethod = PngInterlaceMethod.Adam7;
        PngPixelBuffer buffer = CreateBuffer(PngPixelFormat.Grayscale8, 1, 1, 1, new byte[] { 1 });
        PngEncoderOptions options = CreateFixedOptions(PngFilterType.None);
        PngEncoderOptions invalidOptions = new PngEncoderOptions();
        invalidOptions.FilterMethod = (PngFilterMethod)99;

        Assert.Throws<ArgumentNullException>(() => PngNonInterlacedImageEncoder.Encode(null!, ihdr));
        Assert.Throws<ArgumentNullException>(() => PngNonInterlacedImageEncoder.Encode(buffer, null!));
        Assert.Throws<ArgumentNullException>(() => PngNonInterlacedImageEncoder.Encode(buffer, ihdr, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngNonInterlacedImageEncoder.Encode(buffer, interlaced));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngNonInterlacedImageEncoder.Encode(CreateBuffer(PngPixelFormat.Grayscale8, 2, 1, 2, new byte[] { 1, 2 }), ihdr, options));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngNonInterlacedImageEncoder.Encode(CreateBuffer(PngPixelFormat.Grayscale8, 1, 2, 1, new byte[] { 1, 2 }), ihdr, options));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngNonInterlacedImageEncoder.Encode(CreateBuffer(PngPixelFormat.Rgb8, 1, 1, 3, new byte[] { 1, 2, 3 }), ihdr, options));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngNonInterlacedImageEncoder.Encode(CreateBuffer(PngPixelFormat.Grayscale8, 1, 1, 0, new byte[] { 1 }), ihdr, options));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngNonInterlacedImageEncoder.Encode(CreateBuffer(PngPixelFormat.Grayscale8, 1, 1, 1, new byte[0]), ihdr, options));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngNonInterlacedImageEncoder.Encode(buffer, ihdr, invalidOptions));
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
        ihdr.InterlaceMethod = PngInterlaceMethod.None;
        return ihdr;
    }
}
