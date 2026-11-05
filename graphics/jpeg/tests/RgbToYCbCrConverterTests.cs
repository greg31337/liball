namespace jpeg.Tests;

internal class RgbToYCbCrConverterTests
{
    [Test]
    public void Converter_ConvertsIndividualSamples()
    {
        global::jpeg.JpegRgbToYCbCrConverter converter = new global::jpeg.JpegRgbToYCbCrConverter();

        Assert.That(CreateYCbCrPixel(converter, 0, 0, 0), Is.EqualTo(new byte[] { 0, 128, 128 }));
        Assert.That(CreateYCbCrPixel(converter, 255, 255, 255), Is.EqualTo(new byte[] { 255, 128, 128 }));
        Assert.That(CreateYCbCrPixel(converter, 128, 128, 128), Is.EqualTo(new byte[] { 128, 128, 128 }));
        Assert.That(CreateYCbCrPixel(converter, 255, 0, 0), Is.EqualTo(new byte[] { 76, 85, 255 }));
        Assert.That(CreateYCbCrPixel(converter, 0, 255, 0), Is.EqualTo(new byte[] { 150, 44, 21 }));
        Assert.That(CreateYCbCrPixel(converter, 0, 0, 255), Is.EqualTo(new byte[] { 29, 255, 107 }));
    }

    [Test]
    public void Converter_ClampsOutOfRangeYCbCrResults()
    {
        global::jpeg.JpegRgbToYCbCrConverter converter = new global::jpeg.JpegRgbToYCbCrConverter();

        Assert.That(converter.ConvertSampleToCb(0, 0, 255), Is.EqualTo(255));
        Assert.That(converter.ConvertSampleToCr(255, 0, 0), Is.EqualTo(255));
        Assert.That(converter.ClampToByte(-1), Is.EqualTo(0));
        Assert.That(converter.ClampToByte(256), Is.EqualTo(255));
    }

    [Test]
    public void Converter_ConvertsPackedRgbToYCbCrWithStrides()
    {
        global::jpeg.JpegRgbToYCbCrConverter converter = new global::jpeg.JpegRgbToYCbCrConverter();
        byte[] source = new byte[]
        {
            0, 0, 0, 255, 255, 255, 99, 99,
            255, 0, 0, 0, 0, 255, 98, 98
        };
        byte[] destination = new byte[] { 0, 0, 0, 0, 0, 0, 77, 0, 0, 0, 0, 0, 0, 88 };

        converter.CopyPackedRgbToYCbCr(source, 2, 2, 8, destination, 7);

        Assert.That(destination, Is.EqualTo(new byte[] { 0, 128, 128, 255, 128, 128, 77, 76, 85, 255, 29, 255, 107, 88 }));
    }

    [Test]
    public void Converter_ConvertsPackedRgbToYCbCrBuffer()
    {
        global::jpeg.JpegRgbToYCbCrConverter converter = new global::jpeg.JpegRgbToYCbCrConverter();
        byte[] source = new byte[] { 0, 0, 0, 255, 255, 255, 255, 0, 0 };

        byte[] destination = converter.ConvertPackedRgbToYCbCr(source, 3, 1);

        Assert.That(destination, Is.EqualTo(new byte[] { 0, 128, 128, 255, 128, 128, 76, 85, 255 }));
    }

    [Test]
    public void Converter_ConvertsRgbImageToYCbCrBuffer()
    {
        global::jpeg.JpegRgbToYCbCrConverter converter = new global::jpeg.JpegRgbToYCbCrConverter();
        global::jpeg.JpegImage source = CreateRgbImage();

        global::jpeg.JpegInterleavedPixelBuffer buffer = converter.ConvertImageToYCbCrBuffer(source);

        Assert.That(buffer.Width, Is.EqualTo(2));
        Assert.That(buffer.Height, Is.EqualTo(2));
        Assert.That(buffer.ComponentCount, Is.EqualTo(3));
        Assert.That(buffer.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.YCbCr24));
        Assert.That(buffer.BytesPerPixel, Is.EqualTo(3));
        Assert.That(buffer.Stride, Is.EqualTo(6));
        Assert.That(buffer.Pixels, Is.EqualTo(new byte[] { 0, 128, 128, 255, 128, 128, 76, 85, 255, 29, 255, 107 }));
    }

    [Test]
    public void Converter_ConvertsRgbImageToYCbCrImage()
    {
        global::jpeg.JpegRgbToYCbCrConverter converter = new global::jpeg.JpegRgbToYCbCrConverter();
        global::jpeg.JpegImage source = CreateRgbImage();

        global::jpeg.JpegImage image = converter.ConvertRgbImageToYCbCrImage(source);

        Assert.That(image.Width, Is.EqualTo(2));
        Assert.That(image.Height, Is.EqualTo(2));
        Assert.That(image.Precision, Is.EqualTo(8));
        Assert.That(image.ComponentCount, Is.EqualTo(3));
        Assert.That(image.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.YCbCr24));
        Assert.That(image.ColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.YCbCr));
        Assert.That(image.Subsampling, Is.EqualTo(global::jpeg.JpegSubsampling.Ratio444));
        Assert.That(image.Pixels, Is.EqualTo(new byte[] { 0, 128, 128, 255, 128, 128, 76, 85, 255, 29, 255, 107 }));
    }

    [Test]
    public void Converter_AllowsZeroHeightOutput()
    {
        global::jpeg.JpegRgbToYCbCrConverter converter = new global::jpeg.JpegRgbToYCbCrConverter();
        global::jpeg.JpegImage source = new global::jpeg.JpegImage();
        source.Width = 4;
        source.Height = 0;
        source.Precision = 8;
        source.ComponentCount = 3;
        source.PixelFormat = global::jpeg.JpegPixelFormat.Rgb24;
        source.Pixels = new byte[0];

        global::jpeg.JpegInterleavedPixelBuffer buffer = converter.ConvertImageToYCbCrBuffer(source);

        Assert.That(buffer.Width, Is.EqualTo(4));
        Assert.That(buffer.Height, Is.EqualTo(0));
        Assert.That(buffer.Stride, Is.EqualTo(12));
        Assert.That(buffer.Pixels.Length, Is.EqualTo(0));
    }

    [Test]
    public void Converter_RejectsInvalidImageInputs()
    {
        global::jpeg.JpegRgbToYCbCrConverter converter = new global::jpeg.JpegRgbToYCbCrConverter();
        global::jpeg.JpegImage source = CreateRgbImage();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                converter.ConvertImageToYCbCrBuffer(null!);
            });

        source.PixelFormat = global::jpeg.JpegPixelFormat.Bgr24;
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                converter.ConvertImageToYCbCrBuffer(source);
            });

        source = CreateRgbImage();
        source.Pixels = new byte[3];
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                converter.ConvertImageToYCbCrBuffer(source);
            });
    }

    [Test]
    public void Converter_RejectsInvalidCopyInputs()
    {
        global::jpeg.JpegRgbToYCbCrConverter converter = new global::jpeg.JpegRgbToYCbCrConverter();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                converter.CopyPackedRgbToYCbCr(null!, 1, 1, 3, new byte[3], 3);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                converter.CopyPackedRgbToYCbCr(new byte[3], 1, 1, 2, new byte[3], 3);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                converter.CopyPackedRgbToYCbCr(new byte[3], 1, 1, 3, new byte[2], 3);
            });
        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                converter.WriteYCbCrPixel(0, 0, 0, null!, 0);
            });
        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                converter.ConvertSampleToY(-1, 0, 0);
            });
    }

    public static byte[] CreateYCbCrPixel(global::jpeg.JpegRgbToYCbCrConverter converter, int red, int green, int blue)
    {
        byte[] destination = new byte[3];
        converter.WriteYCbCrPixel(red, green, blue, destination, 0);
        return destination;
    }

    public static global::jpeg.JpegImage CreateRgbImage()
    {
        global::jpeg.JpegImage image = new global::jpeg.JpegImage();
        image.Width = 2;
        image.Height = 2;
        image.Precision = 8;
        image.ComponentCount = 3;
        image.PixelFormat = global::jpeg.JpegPixelFormat.Rgb24;
        image.ColorSpace = global::jpeg.JpegColorSpace.RGB;
        image.Pixels = new byte[]
        {
            0, 0, 0, 255, 255, 255,
            255, 0, 0, 0, 0, 255
        };
        return image;
    }
}
