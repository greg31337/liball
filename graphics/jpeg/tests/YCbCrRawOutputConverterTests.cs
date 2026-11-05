namespace jpeg.Tests;

internal class YCbCrRawOutputConverterTests
{
    [Test]
    public void RawOutput_PacksPlanarComponentsWithoutColorConversion()
    {
        global::jpeg.JpegYCbCrRawOutputConverter converter = new global::jpeg.JpegYCbCrRawOutputConverter();
        global::jpeg.JpegPlanarImageBuffer source = CreatePlanarBuffer();

        byte[] pixels = converter.ConvertComponentsToYCbCr(source.Components[0], source.Components[1], source.Components[2], 2, 2);

        Assert.That(pixels, Is.EqualTo(CreateExpectedPixels()));
    }

    [Test]
    public void RawOutput_ConvertsPlanarBufferToYCbCr24Buffer()
    {
        global::jpeg.JpegYCbCrRawOutputConverter converter = new global::jpeg.JpegYCbCrRawOutputConverter();
        global::jpeg.JpegPlanarImageBuffer source = CreatePlanarBuffer();

        global::jpeg.JpegInterleavedPixelBuffer buffer = converter.ConvertPlanarToYCbCr(source);

        Assert.That(buffer.Width, Is.EqualTo(2));
        Assert.That(buffer.Height, Is.EqualTo(2));
        Assert.That(buffer.ComponentCount, Is.EqualTo(3));
        Assert.That(buffer.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.YCbCr24));
        Assert.That(buffer.BytesPerPixel, Is.EqualTo(3));
        Assert.That(buffer.Stride, Is.EqualTo(6));
        Assert.That(buffer.Pixels, Is.EqualTo(CreateExpectedPixels()));
    }

    [Test]
    public void RawOutput_ConvertsPlanarBufferToYCbCrImage()
    {
        global::jpeg.JpegYCbCrRawOutputConverter converter = new global::jpeg.JpegYCbCrRawOutputConverter();
        global::jpeg.JpegPlanarImageBuffer source = CreatePlanarBuffer();

        global::jpeg.JpegImage image = converter.ConvertPlanarToImage(source);

        Assert.That(image.Width, Is.EqualTo(2));
        Assert.That(image.Height, Is.EqualTo(2));
        Assert.That(image.Precision, Is.EqualTo(8));
        Assert.That(image.ComponentCount, Is.EqualTo(3));
        Assert.That(image.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.YCbCr24));
        Assert.That(image.ColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.YCbCr));
        Assert.That(image.Subsampling, Is.EqualTo(global::jpeg.JpegSubsampling.Ratio444));
        Assert.That(image.Pixels, Is.EqualTo(CreateExpectedPixels()));
    }

    [Test]
    public void RawOutput_CopySupportsDestinationStride()
    {
        global::jpeg.JpegYCbCrRawOutputConverter converter = new global::jpeg.JpegYCbCrRawOutputConverter();
        global::jpeg.JpegPlanarImageBuffer source = CreatePlanarBuffer();
        byte[] destination = new byte[] { 0, 0, 0, 0, 0, 0, 77, 0, 0, 0, 0, 0, 0, 88 };

        converter.CopyPlanarToYCbCr(source.Components[0], source.Components[1], source.Components[2], 2, 2, destination, 7);

        Assert.That(destination, Is.EqualTo(new byte[] { 1, 101, 201, 2, 102, 202, 77, 3, 103, 203, 4, 104, 204, 88 }));
    }

    [Test]
    public void RawOutput_AllowsZeroHeightOutput()
    {
        global::jpeg.JpegYCbCrRawOutputConverter converter = new global::jpeg.JpegYCbCrRawOutputConverter();
        global::jpeg.JpegPlanarImageBuffer source = new global::jpeg.JpegPlanarImageBuffer();
        source.Width = 4;
        source.Height = 0;
        source.ComponentCount = 3;
        source.Components.Add(CreateComponent(4, 0, 4, new byte[0]));
        source.Components.Add(CreateComponent(4, 0, 4, new byte[0]));
        source.Components.Add(CreateComponent(4, 0, 4, new byte[0]));

        global::jpeg.JpegInterleavedPixelBuffer buffer = converter.ConvertPlanarToYCbCr(source);

        Assert.That(buffer.Width, Is.EqualTo(4));
        Assert.That(buffer.Height, Is.EqualTo(0));
        Assert.That(buffer.Stride, Is.EqualTo(12));
        Assert.That(buffer.Pixels.Length, Is.EqualTo(0));
    }

    [Test]
    public void RawOutput_RejectsInvalidPlanarInputs()
    {
        global::jpeg.JpegYCbCrRawOutputConverter converter = new global::jpeg.JpegYCbCrRawOutputConverter();
        global::jpeg.JpegPlanarImageBuffer source = new global::jpeg.JpegPlanarImageBuffer();
        source.Width = 2;
        source.Height = 2;
        source.ComponentCount = 3;

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                converter.ConvertPlanarToYCbCr(null!);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                converter.ConvertPlanarToYCbCr(source);
            });

        source.Components = null!;
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                converter.ConvertPlanarToYCbCr(source);
            });
    }

    [Test]
    public void RawOutput_RejectsInvalidCopyInputs()
    {
        global::jpeg.JpegYCbCrRawOutputConverter converter = new global::jpeg.JpegYCbCrRawOutputConverter();
        global::jpeg.JpegPlanarComponentBuffer component = CreateComponent(2, 2, 2, new byte[] { 1, 2, 3, 4 });

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                converter.CopyPlanarToYCbCr(null!, component, component, 2, 2, new byte[12], 6);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                converter.CopyPlanarToYCbCr(component, component, CreateComponent(1, 2, 1, new byte[] { 1, 2 }), 2, 2, new byte[12], 6);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                converter.CopyPlanarToYCbCr(component, component, component, 2, 2, new byte[11], 6);
            });
        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                converter.WriteYCbCrPixel(1, 2, 3, null!, 0);
            });
        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                converter.WriteYCbCrPixel(-1, 2, 3, new byte[3], 0);
            });
    }

    public static global::jpeg.JpegPlanarImageBuffer CreatePlanarBuffer()
    {
        global::jpeg.JpegPlanarImageBuffer source = new global::jpeg.JpegPlanarImageBuffer();
        source.Width = 2;
        source.Height = 2;
        source.ComponentCount = 3;
        source.Components.Add(CreateComponent(2, 2, 4, new byte[] { 1, 2, 99, 99, 3, 4, 98, 98 }));
        source.Components.Add(CreateComponent(2, 2, 4, new byte[] { 101, 102, 99, 99, 103, 104, 98, 98 }));
        source.Components.Add(CreateComponent(2, 2, 4, new byte[] { 201, 202, 99, 99, 203, 204, 98, 98 }));
        return source;
    }

    public static byte[] CreateExpectedPixels()
    {
        return new byte[] { 1, 101, 201, 2, 102, 202, 3, 103, 203, 4, 104, 204 };
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
