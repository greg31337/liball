namespace jpeg.Tests;

internal class YCbCrToRgbConverterTests
{
    [Test]
    public void Converter_ConvertsIndividualSamples()
    {
        global::jpeg.JpegYCbCrToRgbConverter converter = new global::jpeg.JpegYCbCrToRgbConverter();

        Assert.That(converter.ConvertSampleToRed(128, 128, 128), Is.EqualTo(128));
        Assert.That(converter.ConvertSampleToGreen(128, 128, 128), Is.EqualTo(128));
        Assert.That(converter.ConvertSampleToBlue(128, 128, 128), Is.EqualTo(128));

        Assert.That(CreateRgbPixel(converter, 0, 128, 128), Is.EqualTo(new byte[] { 0, 0, 0 }));
        Assert.That(CreateRgbPixel(converter, 255, 128, 128), Is.EqualTo(new byte[] { 255, 255, 255 }));
        Assert.That(CreateRgbPixel(converter, 76, 85, 255), Is.EqualTo(new byte[] { 254, 0, 0 }));
        Assert.That(CreateRgbPixel(converter, 29, 255, 107), Is.EqualTo(new byte[] { 0, 0, 254 }));
    }

    [Test]
    public void Converter_ClampsOutOfRangeRgbResults()
    {
        global::jpeg.JpegYCbCrToRgbConverter converter = new global::jpeg.JpegYCbCrToRgbConverter();

        Assert.That(converter.ConvertSampleToRed(250, 128, 255), Is.EqualTo(255));
        Assert.That(converter.ConvertSampleToGreen(5, 255, 255), Is.EqualTo(0));
        Assert.That(converter.ConvertSampleToBlue(250, 255, 128), Is.EqualTo(255));
        Assert.That(converter.ClampToByte(-1), Is.EqualTo(0));
        Assert.That(converter.ClampToByte(256), Is.EqualTo(255));
    }

    [Test]
    public void Converter_ConvertsPackedYCbCrToRgbWithStrides()
    {
        global::jpeg.JpegYCbCrToRgbConverter converter = new global::jpeg.JpegYCbCrToRgbConverter();
        byte[] source = new byte[]
        {
            0, 128, 128, 255, 128, 128, 99, 99,
            76, 85, 255, 29, 255, 107, 98, 98
        };
        byte[] destination = new byte[] { 0, 0, 0, 0, 0, 0, 77, 0, 0, 0, 0, 0, 0, 88 };

        converter.CopyPackedYCbCrToRgb(source, 2, 2, 8, destination, 7);

        Assert.That(destination, Is.EqualTo(new byte[] { 0, 0, 0, 255, 255, 255, 77, 254, 0, 0, 0, 0, 254, 88 }));
    }

    [Test]
    public void Converter_ConvertsPackedYCbCrToRgbBuffer()
    {
        global::jpeg.JpegYCbCrToRgbConverter converter = new global::jpeg.JpegYCbCrToRgbConverter();
        byte[] source = new byte[] { 0, 128, 128, 255, 128, 128, 76, 85, 255 };

        byte[] destination = converter.ConvertPackedYCbCrToRgb(source, 3, 1);

        Assert.That(destination, Is.EqualTo(new byte[] { 0, 0, 0, 255, 255, 255, 254, 0, 0 }));
    }

    [Test]
    public void Converter_ConvertsPlanarYCbCrToRgbBuffer()
    {
        global::jpeg.JpegYCbCrToRgbConverter converter = new global::jpeg.JpegYCbCrToRgbConverter();
        global::jpeg.JpegPlanarImageBuffer source = CreatePlanarBuffer();

        global::jpeg.JpegInterleavedPixelBuffer buffer = converter.ConvertPlanarToRgb(source);

        Assert.That(buffer.Width, Is.EqualTo(2));
        Assert.That(buffer.Height, Is.EqualTo(2));
        Assert.That(buffer.ComponentCount, Is.EqualTo(3));
        Assert.That(buffer.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.Rgb24));
        Assert.That(buffer.BytesPerPixel, Is.EqualTo(3));
        Assert.That(buffer.Stride, Is.EqualTo(6));
        Assert.That(buffer.Pixels, Is.EqualTo(new byte[] { 0, 0, 0, 255, 255, 255, 254, 0, 0, 0, 0, 254 }));
    }

    [Test]
    public void Converter_ConvertsPlanarYCbCrToImage()
    {
        global::jpeg.JpegYCbCrToRgbConverter converter = new global::jpeg.JpegYCbCrToRgbConverter();
        global::jpeg.JpegPlanarImageBuffer source = CreatePlanarBuffer();

        global::jpeg.JpegImage image = converter.ConvertPlanarToImage(source);

        Assert.That(image.Width, Is.EqualTo(2));
        Assert.That(image.Height, Is.EqualTo(2));
        Assert.That(image.Precision, Is.EqualTo(8));
        Assert.That(image.ComponentCount, Is.EqualTo(3));
        Assert.That(image.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.Rgb24));
        Assert.That(image.ColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.RGB));
        Assert.That(image.Pixels, Is.EqualTo(new byte[] { 0, 0, 0, 255, 255, 255, 254, 0, 0, 0, 0, 254 }));
    }

    [Test]
    public void Converter_AllowsZeroHeightOutput()
    {
        global::jpeg.JpegYCbCrToRgbConverter converter = new global::jpeg.JpegYCbCrToRgbConverter();
        global::jpeg.JpegPlanarImageBuffer source = new global::jpeg.JpegPlanarImageBuffer();
        source.Width = 4;
        source.Height = 0;
        source.ComponentCount = 3;
        source.Components.Add(CreateComponent(4, 0, 4, new byte[0]));
        source.Components.Add(CreateComponent(4, 0, 4, new byte[0]));
        source.Components.Add(CreateComponent(4, 0, 4, new byte[0]));

        global::jpeg.JpegInterleavedPixelBuffer buffer = converter.ConvertPlanarToRgb(source);

        Assert.That(buffer.Width, Is.EqualTo(4));
        Assert.That(buffer.Height, Is.EqualTo(0));
        Assert.That(buffer.Stride, Is.EqualTo(12));
        Assert.That(buffer.Pixels.Length, Is.EqualTo(0));
    }

    [Test]
    public void Converter_RejectsInvalidPlanarInputs()
    {
        global::jpeg.JpegYCbCrToRgbConverter converter = new global::jpeg.JpegYCbCrToRgbConverter();
        global::jpeg.JpegPlanarImageBuffer source = new global::jpeg.JpegPlanarImageBuffer();
        source.Width = 2;
        source.Height = 2;
        source.ComponentCount = 3;

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                converter.ConvertPlanarToRgb(null!);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                converter.ConvertPlanarToRgb(source);
            });

        source.Components = null!;
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                converter.ConvertPlanarToRgb(source);
            });
    }

    [Test]
    public void Converter_RejectsInvalidCopyInputs()
    {
        global::jpeg.JpegYCbCrToRgbConverter converter = new global::jpeg.JpegYCbCrToRgbConverter();
        global::jpeg.JpegPlanarComponentBuffer component = CreateComponent(2, 2, 2, new byte[] { 0, 1, 2, 3 });

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                converter.CopyPackedYCbCrToRgb(null!, 1, 1, 3, new byte[3], 3);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                converter.CopyPackedYCbCrToRgb(new byte[3], 1, 1, 2, new byte[3], 3);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                converter.CopyPackedYCbCrToRgb(new byte[3], 1, 1, 3, new byte[2], 3);
            });
        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                converter.CopyPlanarToRgb(null!, component, component, 2, 2, new byte[12], 6);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                converter.CopyPlanarToRgb(component, component, CreateComponent(1, 2, 1, new byte[] { 1, 2 }), 2, 2, new byte[12], 6);
            });
        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                converter.ConvertSampleToRed(-1, 128, 128);
            });
    }

    public static byte[] CreateRgbPixel(global::jpeg.JpegYCbCrToRgbConverter converter, int y, int cb, int cr)
    {
        byte[] destination = new byte[3];
        converter.WriteRgbPixel(y, cb, cr, destination, 0);
        return destination;
    }

    public static global::jpeg.JpegPlanarImageBuffer CreatePlanarBuffer()
    {
        global::jpeg.JpegPlanarImageBuffer source = new global::jpeg.JpegPlanarImageBuffer();
        source.Width = 2;
        source.Height = 2;
        source.ComponentCount = 3;
        source.Components.Add(CreateComponent(2, 2, 4, new byte[] { 0, 255, 99, 99, 76, 29, 98, 98 }));
        source.Components.Add(CreateComponent(2, 2, 4, new byte[] { 128, 128, 99, 99, 85, 255, 98, 98 }));
        source.Components.Add(CreateComponent(2, 2, 4, new byte[] { 128, 128, 99, 99, 255, 107, 98, 98 }));
        return source;
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
