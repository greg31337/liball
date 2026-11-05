namespace jpeg.Tests;

internal class YcckToRgbConverterTests
{
    [Test]
    public void Converter_ConvertsIndividualSamples()
    {
        global::jpeg.JpegYcckToRgbConverter converter = new global::jpeg.JpegYcckToRgbConverter();

        Assert.That(CreateRgbPixel(converter, 255, 128, 128, 10), Is.EqualTo(new byte[] { 245, 245, 245 }));
        Assert.That(CreateRgbPixel(converter, 0, 128, 128, 20), Is.EqualTo(new byte[] { 0, 0, 0 }));
        Assert.That(CreateRgbPixel(converter, 76, 85, 255, 30), Is.EqualTo(new byte[] { 224, 0, 0 }));
        Assert.That(CreateRgbPixel(converter, 29, 255, 107, 40), Is.EqualTo(new byte[] { 0, 0, 214 }));
    }

    [Test]
    public void Converter_ConvertsPackedYcckToRgbWithStrides()
    {
        global::jpeg.JpegYcckToRgbConverter converter = new global::jpeg.JpegYcckToRgbConverter();
        byte[] source = new byte[]
        {
            255, 128, 128, 10, 0, 128, 128, 20, 99, 99,
            76, 85, 255, 30, 29, 255, 107, 40, 98, 98
        };
        byte[] destination = new byte[] { 0, 0, 0, 0, 0, 0, 77, 0, 0, 0, 0, 0, 0, 88 };

        converter.CopyPackedYcckToRgb(source, 2, 2, 10, destination, 7);

        Assert.That(destination, Is.EqualTo(new byte[] { 245, 245, 245, 0, 0, 0, 77, 224, 0, 0, 0, 0, 214, 88 }));
    }

    [Test]
    public void Converter_ConvertsPackedYcckToRgbBuffer()
    {
        global::jpeg.JpegYcckToRgbConverter converter = new global::jpeg.JpegYcckToRgbConverter();
        byte[] source = new byte[] { 255, 128, 128, 10, 0, 128, 128, 20, 76, 85, 255, 30 };

        byte[] destination = converter.ConvertPackedYcckToRgb(source, 3, 1);

        Assert.That(destination, Is.EqualTo(new byte[] { 245, 245, 245, 0, 0, 0, 224, 0, 0 }));
    }

    [Test]
    public void Converter_ConvertsYcckImageToRgbBuffer()
    {
        global::jpeg.JpegYcckToRgbConverter converter = new global::jpeg.JpegYcckToRgbConverter();
        global::jpeg.JpegImage source = YcckToCmykConverterTests.CreateYcckImage();

        global::jpeg.JpegInterleavedPixelBuffer buffer = converter.ConvertImageToRgbBuffer(source);

        Assert.That(buffer.Width, Is.EqualTo(2));
        Assert.That(buffer.Height, Is.EqualTo(2));
        Assert.That(buffer.ComponentCount, Is.EqualTo(3));
        Assert.That(buffer.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.Rgb24));
        Assert.That(buffer.BytesPerPixel, Is.EqualTo(3));
        Assert.That(buffer.Stride, Is.EqualTo(6));
        Assert.That(buffer.Pixels, Is.EqualTo(CreateExpectedRgbPixels()));
    }

    [Test]
    public void Converter_ConvertsYcckImageToRgbImage()
    {
        global::jpeg.JpegYcckToRgbConverter converter = new global::jpeg.JpegYcckToRgbConverter();
        global::jpeg.JpegImage source = YcckToCmykConverterTests.CreateYcckImage();

        global::jpeg.JpegImage image = converter.ConvertImageToRgbImage(source);

        Assert.That(image.Width, Is.EqualTo(2));
        Assert.That(image.Height, Is.EqualTo(2));
        Assert.That(image.Precision, Is.EqualTo(8));
        Assert.That(image.ComponentCount, Is.EqualTo(3));
        Assert.That(image.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.Rgb24));
        Assert.That(image.ColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.RGB));
        Assert.That(image.Pixels, Is.EqualTo(CreateExpectedRgbPixels()));
    }

    [Test]
    public void Converter_ConvertsPlanarYcckToRgbBuffer()
    {
        global::jpeg.JpegYcckToRgbConverter converter = new global::jpeg.JpegYcckToRgbConverter();
        global::jpeg.JpegPlanarImageBuffer source = YcckToCmykConverterTests.CreatePlanarBuffer();

        global::jpeg.JpegInterleavedPixelBuffer buffer = converter.ConvertPlanarToRgb(source);

        Assert.That(buffer.Width, Is.EqualTo(2));
        Assert.That(buffer.Height, Is.EqualTo(2));
        Assert.That(buffer.Pixels, Is.EqualTo(CreateExpectedRgbPixels()));
    }

    [Test]
    public void Converter_AllowsZeroHeightOutput()
    {
        global::jpeg.JpegYcckToRgbConverter converter = new global::jpeg.JpegYcckToRgbConverter();
        global::jpeg.JpegImage source = new global::jpeg.JpegImage();
        source.Width = 4;
        source.Height = 0;
        source.Precision = 8;
        source.ComponentCount = 4;
        source.PixelFormat = global::jpeg.JpegPixelFormat.Ycck32;
        source.Pixels = new byte[0];

        global::jpeg.JpegInterleavedPixelBuffer buffer = converter.ConvertImageToRgbBuffer(source);

        Assert.That(buffer.Width, Is.EqualTo(4));
        Assert.That(buffer.Height, Is.EqualTo(0));
        Assert.That(buffer.Stride, Is.EqualTo(12));
        Assert.That(buffer.Pixels.Length, Is.EqualTo(0));
    }

    [Test]
    public void Converter_RejectsInvalidInputs()
    {
        global::jpeg.JpegYcckToRgbConverter converter = new global::jpeg.JpegYcckToRgbConverter();
        global::jpeg.JpegImage source = YcckToCmykConverterTests.CreateYcckImage();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                converter.ConvertImageToRgbBuffer(null!);
            });

        source.PixelFormat = global::jpeg.JpegPixelFormat.Cmyk32;
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                converter.ConvertImageToRgbBuffer(source);
            });

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                converter.CopyPackedYcckToRgb(null!, 1, 1, 4, new byte[3], 3);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                converter.CopyPackedYcckToRgb(new byte[4], 1, 1, 3, new byte[3], 3);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                converter.CopyPackedYcckToRgb(new byte[4], 1, 1, 4, new byte[2], 3);
            });
        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                converter.WriteRgbPixel(255, 128, 128, 10, null!, 0);
            });
        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                converter.ConvertSampleToRed(-1, 128, 128, 10);
            });
    }

    public static byte[] CreateRgbPixel(global::jpeg.JpegYcckToRgbConverter converter, int luminance, int cb, int cr, int black)
    {
        byte[] destination = new byte[3];
        converter.WriteRgbPixel(luminance, cb, cr, black, destination, 0);
        return destination;
    }

    public static byte[] CreateExpectedRgbPixels()
    {
        return new byte[] { 245, 245, 245, 0, 0, 0, 224, 0, 0, 0, 0, 214 };
    }
}
