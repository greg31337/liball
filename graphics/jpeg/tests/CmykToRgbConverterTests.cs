namespace jpeg.Tests;

internal class CmykToRgbConverterTests
{
    [Test]
    public void Converter_ConvertsIndividualSamples()
    {
        global::jpeg.JpegCmykToRgbConverter converter = new global::jpeg.JpegCmykToRgbConverter();

        Assert.That(CreateRgbPixel(converter, 0, 0, 0, 0), Is.EqualTo(new byte[] { 255, 255, 255 }));
        Assert.That(CreateRgbPixel(converter, 0, 0, 0, 255), Is.EqualTo(new byte[] { 0, 0, 0 }));
        Assert.That(CreateRgbPixel(converter, 0, 255, 255, 0), Is.EqualTo(new byte[] { 255, 0, 0 }));
        Assert.That(CreateRgbPixel(converter, 255, 0, 255, 0), Is.EqualTo(new byte[] { 0, 255, 0 }));
        Assert.That(CreateRgbPixel(converter, 255, 255, 0, 0), Is.EqualTo(new byte[] { 0, 0, 255 }));
        Assert.That(CreateRgbPixel(converter, 128, 0, 0, 128), Is.EqualTo(new byte[] { 63, 127, 127 }));
    }

    [Test]
    public void Converter_ConvertsPackedCmykToRgbWithStrides()
    {
        global::jpeg.JpegCmykToRgbConverter converter = new global::jpeg.JpegCmykToRgbConverter();
        byte[] source = new byte[]
        {
            0, 0, 0, 0, 0, 0, 0, 255, 99, 99,
            0, 255, 255, 0, 255, 255, 0, 0, 98, 98
        };
        byte[] destination = new byte[] { 0, 0, 0, 0, 0, 0, 77, 0, 0, 0, 0, 0, 0, 88 };

        converter.CopyPackedCmykToRgb(source, 2, 2, 10, destination, 7);

        Assert.That(destination, Is.EqualTo(new byte[] { 255, 255, 255, 0, 0, 0, 77, 255, 0, 0, 0, 0, 255, 88 }));
    }

    [Test]
    public void Converter_ConvertsPackedCmykToRgbBuffer()
    {
        global::jpeg.JpegCmykToRgbConverter converter = new global::jpeg.JpegCmykToRgbConverter();
        byte[] source = new byte[] { 0, 0, 0, 0, 0, 0, 0, 255, 128, 0, 0, 128 };

        byte[] destination = converter.ConvertPackedCmykToRgb(source, 3, 1);

        Assert.That(destination, Is.EqualTo(new byte[] { 255, 255, 255, 0, 0, 0, 63, 127, 127 }));
    }

    [Test]
    public void Converter_ConvertsCmykImageToRgbBuffer()
    {
        global::jpeg.JpegCmykToRgbConverter converter = new global::jpeg.JpegCmykToRgbConverter();
        global::jpeg.JpegImage source = CreateCmykImage();

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
    public void Converter_ConvertsCmykImageToRgbImage()
    {
        global::jpeg.JpegCmykToRgbConverter converter = new global::jpeg.JpegCmykToRgbConverter();
        global::jpeg.JpegImage source = CreateCmykImage();

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
    public void Converter_AllowsZeroHeightOutput()
    {
        global::jpeg.JpegCmykToRgbConverter converter = new global::jpeg.JpegCmykToRgbConverter();
        global::jpeg.JpegImage source = new global::jpeg.JpegImage();
        source.Width = 4;
        source.Height = 0;
        source.Precision = 8;
        source.ComponentCount = 4;
        source.PixelFormat = global::jpeg.JpegPixelFormat.Cmyk32;
        source.Pixels = new byte[0];

        global::jpeg.JpegInterleavedPixelBuffer buffer = converter.ConvertImageToRgbBuffer(source);

        Assert.That(buffer.Width, Is.EqualTo(4));
        Assert.That(buffer.Height, Is.EqualTo(0));
        Assert.That(buffer.Stride, Is.EqualTo(12));
        Assert.That(buffer.Pixels.Length, Is.EqualTo(0));
    }

    [Test]
    public void Converter_RejectsInvalidImageInputs()
    {
        global::jpeg.JpegCmykToRgbConverter converter = new global::jpeg.JpegCmykToRgbConverter();
        global::jpeg.JpegImage source = CreateCmykImage();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                converter.ConvertImageToRgbBuffer(null!);
            });

        source.PixelFormat = global::jpeg.JpegPixelFormat.Rgb24;
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                converter.ConvertImageToRgbBuffer(source);
            });

        source = CreateCmykImage();
        source.Pixels = new byte[4];
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                converter.ConvertImageToRgbBuffer(source);
            });
    }

    [Test]
    public void Converter_RejectsInvalidCopyInputs()
    {
        global::jpeg.JpegCmykToRgbConverter converter = new global::jpeg.JpegCmykToRgbConverter();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                converter.CopyPackedCmykToRgb(null!, 1, 1, 4, new byte[3], 3);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                converter.CopyPackedCmykToRgb(new byte[4], 1, 1, 3, new byte[3], 3);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                converter.CopyPackedCmykToRgb(new byte[4], 1, 1, 4, new byte[2], 3);
            });
        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                converter.WriteRgbPixel(0, 0, 0, 0, null!, 0);
            });
        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                converter.ConvertSampleToRed(-1, 0, 0, 0);
            });
    }

    public static byte[] CreateRgbPixel(global::jpeg.JpegCmykToRgbConverter converter, int cyan, int magenta, int yellow, int black)
    {
        byte[] destination = new byte[3];
        converter.WriteRgbPixel(cyan, magenta, yellow, black, destination, 0);
        return destination;
    }

    public static global::jpeg.JpegImage CreateCmykImage()
    {
        global::jpeg.JpegImage image = new global::jpeg.JpegImage();
        image.Width = 2;
        image.Height = 2;
        image.Precision = 8;
        image.ComponentCount = 4;
        image.PixelFormat = global::jpeg.JpegPixelFormat.Cmyk32;
        image.ColorSpace = global::jpeg.JpegColorSpace.CMYK;
        image.Pixels = new byte[]
        {
            0, 0, 0, 0,
            0, 0, 0, 255,
            0, 255, 255, 0,
            255, 255, 0, 0
        };
        return image;
    }

    public static byte[] CreateExpectedRgbPixels()
    {
        return new byte[] { 255, 255, 255, 0, 0, 0, 255, 0, 0, 0, 0, 255 };
    }
}
