namespace jpeg.Tests;

internal class YcckToCmykConverterTests
{
    [Test]
    public void Converter_ConvertsIndividualSamples()
    {
        global::jpeg.JpegYcckToCmykConverter converter = new global::jpeg.JpegYcckToCmykConverter();

        Assert.That(CreateCmykPixel(converter, 255, 128, 128, 10), Is.EqualTo(new byte[] { 0, 0, 0, 10 }));
        Assert.That(CreateCmykPixel(converter, 0, 128, 128, 20), Is.EqualTo(new byte[] { 255, 255, 255, 20 }));
        Assert.That(CreateCmykPixel(converter, 76, 85, 255, 30), Is.EqualTo(new byte[] { 1, 255, 255, 30 }));
        Assert.That(CreateCmykPixel(converter, 29, 255, 107, 40), Is.EqualTo(new byte[] { 255, 255, 1, 40 }));
        Assert.That(converter.ConvertSampleToBlack(76, 85, 255, 77), Is.EqualTo(77));
    }

    [Test]
    public void Converter_ConvertsPackedYcckToCmykWithStrides()
    {
        global::jpeg.JpegYcckToCmykConverter converter = new global::jpeg.JpegYcckToCmykConverter();
        byte[] source = new byte[]
        {
            255, 128, 128, 10, 0, 128, 128, 20, 99, 99,
            76, 85, 255, 30, 29, 255, 107, 40, 98, 98
        };
        byte[] destination = new byte[]
        {
            0, 0, 0, 0, 0, 0, 0, 0, 77,
            0, 0, 0, 0, 0, 0, 0, 0, 88
        };

        converter.CopyPackedYcckToCmyk(source, 2, 2, 10, destination, 9);

        Assert.That(
            destination,
            Is.EqualTo(new byte[] { 0, 0, 0, 10, 255, 255, 255, 20, 77, 1, 255, 255, 30, 255, 255, 1, 40, 88 }));
    }

    [Test]
    public void Converter_ConvertsPackedYcckToCmykBuffer()
    {
        global::jpeg.JpegYcckToCmykConverter converter = new global::jpeg.JpegYcckToCmykConverter();
        byte[] source = new byte[] { 255, 128, 128, 10, 0, 128, 128, 20, 76, 85, 255, 30 };

        byte[] destination = converter.ConvertPackedYcckToCmyk(source, 3, 1);

        Assert.That(destination, Is.EqualTo(new byte[] { 0, 0, 0, 10, 255, 255, 255, 20, 1, 255, 255, 30 }));
    }

    [Test]
    public void Converter_ConvertsYcckImageToCmykBuffer()
    {
        global::jpeg.JpegYcckToCmykConverter converter = new global::jpeg.JpegYcckToCmykConverter();
        global::jpeg.JpegImage source = CreateYcckImage();

        global::jpeg.JpegInterleavedPixelBuffer buffer = converter.ConvertImageToCmykBuffer(source);

        Assert.That(buffer.Width, Is.EqualTo(2));
        Assert.That(buffer.Height, Is.EqualTo(2));
        Assert.That(buffer.ComponentCount, Is.EqualTo(4));
        Assert.That(buffer.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.Cmyk32));
        Assert.That(buffer.BytesPerPixel, Is.EqualTo(4));
        Assert.That(buffer.Stride, Is.EqualTo(8));
        Assert.That(buffer.Pixels, Is.EqualTo(CreateExpectedCmykPixels()));
    }

    [Test]
    public void Converter_ConvertsYcckImageToCmykImage()
    {
        global::jpeg.JpegYcckToCmykConverter converter = new global::jpeg.JpegYcckToCmykConverter();
        global::jpeg.JpegImage source = CreateYcckImage();

        global::jpeg.JpegImage image = converter.ConvertImageToCmykImage(source);

        Assert.That(image.Width, Is.EqualTo(2));
        Assert.That(image.Height, Is.EqualTo(2));
        Assert.That(image.Precision, Is.EqualTo(8));
        Assert.That(image.ComponentCount, Is.EqualTo(4));
        Assert.That(image.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.Cmyk32));
        Assert.That(image.ColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.CMYK));
        Assert.That(image.Subsampling, Is.EqualTo(global::jpeg.JpegSubsampling.Ratio444));
        Assert.That(image.Pixels, Is.EqualTo(CreateExpectedCmykPixels()));
    }

    [Test]
    public void Converter_ConvertsPlanarYcckToCmykBuffer()
    {
        global::jpeg.JpegYcckToCmykConverter converter = new global::jpeg.JpegYcckToCmykConverter();
        global::jpeg.JpegPlanarImageBuffer source = CreatePlanarBuffer();

        global::jpeg.JpegInterleavedPixelBuffer buffer = converter.ConvertPlanarToCmyk(source);

        Assert.That(buffer.Width, Is.EqualTo(2));
        Assert.That(buffer.Height, Is.EqualTo(2));
        Assert.That(buffer.Pixels, Is.EqualTo(CreateExpectedCmykPixels()));
    }

    [Test]
    public void Converter_AllowsZeroHeightOutput()
    {
        global::jpeg.JpegYcckToCmykConverter converter = new global::jpeg.JpegYcckToCmykConverter();
        global::jpeg.JpegImage source = new global::jpeg.JpegImage();
        source.Width = 4;
        source.Height = 0;
        source.Precision = 8;
        source.ComponentCount = 4;
        source.PixelFormat = global::jpeg.JpegPixelFormat.Ycck32;
        source.Pixels = new byte[0];

        global::jpeg.JpegInterleavedPixelBuffer buffer = converter.ConvertImageToCmykBuffer(source);

        Assert.That(buffer.Width, Is.EqualTo(4));
        Assert.That(buffer.Height, Is.EqualTo(0));
        Assert.That(buffer.Stride, Is.EqualTo(16));
        Assert.That(buffer.Pixels.Length, Is.EqualTo(0));
    }

    [Test]
    public void Converter_RejectsInvalidImageInputs()
    {
        global::jpeg.JpegYcckToCmykConverter converter = new global::jpeg.JpegYcckToCmykConverter();
        global::jpeg.JpegImage source = CreateYcckImage();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                converter.ConvertImageToCmykBuffer(null!);
            });

        source.PixelFormat = global::jpeg.JpegPixelFormat.Cmyk32;
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                converter.ConvertImageToCmykBuffer(source);
            });

        source = CreateYcckImage();
        source.Pixels = new byte[4];
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                converter.ConvertImageToCmykBuffer(source);
            });
    }

    [Test]
    public void Converter_RejectsInvalidCopyInputs()
    {
        global::jpeg.JpegYcckToCmykConverter converter = new global::jpeg.JpegYcckToCmykConverter();
        global::jpeg.JpegPlanarComponentBuffer component = CreateComponent(2, 2, 2, new byte[] { 1, 2, 3, 4 });

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                converter.CopyPackedYcckToCmyk(null!, 1, 1, 4, new byte[4], 4);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                converter.CopyPackedYcckToCmyk(new byte[4], 1, 1, 3, new byte[4], 4);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                converter.CopyPackedYcckToCmyk(new byte[4], 1, 1, 4, new byte[3], 4);
            });
        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                converter.CopyPlanarToCmyk(null!, component, component, component, 2, 2, new byte[16], 8);
            });
        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                converter.WriteCmykPixel(255, 128, 128, 10, null!, 0);
            });
        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                converter.ConvertSampleToCyan(-1, 128, 128, 10);
            });
    }

    public static byte[] CreateCmykPixel(global::jpeg.JpegYcckToCmykConverter converter, int luminance, int cb, int cr, int black)
    {
        byte[] destination = new byte[4];
        converter.WriteCmykPixel(luminance, cb, cr, black, destination, 0);
        return destination;
    }

    public static global::jpeg.JpegImage CreateYcckImage()
    {
        global::jpeg.JpegImage image = new global::jpeg.JpegImage();
        image.Width = 2;
        image.Height = 2;
        image.Precision = 8;
        image.ComponentCount = 4;
        image.PixelFormat = global::jpeg.JpegPixelFormat.Ycck32;
        image.ColorSpace = global::jpeg.JpegColorSpace.YCCK;
        image.Pixels = new byte[]
        {
            255, 128, 128, 10,
            0, 128, 128, 20,
            76, 85, 255, 30,
            29, 255, 107, 40
        };
        return image;
    }

    public static global::jpeg.JpegPlanarImageBuffer CreatePlanarBuffer()
    {
        global::jpeg.JpegPlanarImageBuffer source = new global::jpeg.JpegPlanarImageBuffer();
        source.Width = 2;
        source.Height = 2;
        source.ComponentCount = 4;
        source.Components.Add(CreateComponent(2, 2, 4, new byte[] { 255, 0, 99, 99, 76, 29, 98, 98 }));
        source.Components.Add(CreateComponent(2, 2, 4, new byte[] { 128, 128, 99, 99, 85, 255, 98, 98 }));
        source.Components.Add(CreateComponent(2, 2, 4, new byte[] { 128, 128, 99, 99, 255, 107, 98, 98 }));
        source.Components.Add(CreateComponent(2, 2, 4, new byte[] { 10, 20, 99, 99, 30, 40, 98, 98 }));
        return source;
    }

    public static byte[] CreateExpectedCmykPixels()
    {
        return new byte[] { 0, 0, 0, 10, 255, 255, 255, 20, 1, 255, 255, 30, 255, 255, 1, 40 };
    }

    public static global::jpeg.JpegPlanarComponentBuffer CreateComponent(int width, int height, int stride, byte[] samples)
    {
        global::jpeg.JpegPlanarComponentBuffer component = new global::jpeg.JpegPlanarComponentBuffer();
        component.Width = width;
        component.Height = height;
        component.PaddedWidth = stride;
        component.PaddedHeight = height;
        component.Stride = stride;
        component.Samples = samples;
        return component;
    }
}
