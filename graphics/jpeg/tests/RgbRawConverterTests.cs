namespace jpeg.Tests;

internal class RgbRawConverterTests
{
    [Test]
    public void RawRgbDecode_PacksPlanarComponentsWithoutColorConversion()
    {
        global::jpeg.JpegRgbRawConverter converter = new global::jpeg.JpegRgbRawConverter();
        global::jpeg.JpegPlanarImageBuffer source = CreatePlanarBuffer();

        byte[] pixels = converter.ConvertComponentsToRgb(source.Components[0], source.Components[1], source.Components[2], 2, 2);

        Assert.That(pixels, Is.EqualTo(CreateExpectedPixels()));
    }

    [Test]
    public void RawRgbDecode_ConvertsPlanarBufferToRgb24Buffer()
    {
        global::jpeg.JpegRgbRawConverter converter = new global::jpeg.JpegRgbRawConverter();
        global::jpeg.JpegPlanarImageBuffer source = CreatePlanarBuffer();

        global::jpeg.JpegInterleavedPixelBuffer buffer = converter.ConvertPlanarToRgb(source);

        Assert.That(buffer.Width, Is.EqualTo(2));
        Assert.That(buffer.Height, Is.EqualTo(2));
        Assert.That(buffer.ComponentCount, Is.EqualTo(3));
        Assert.That(buffer.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.Rgb24));
        Assert.That(buffer.BytesPerPixel, Is.EqualTo(3));
        Assert.That(buffer.Stride, Is.EqualTo(6));
        Assert.That(buffer.Pixels, Is.EqualTo(CreateExpectedPixels()));
    }

    [Test]
    public void RawRgbDecode_ConvertsPlanarBufferToRgbImage()
    {
        global::jpeg.JpegRgbRawConverter converter = new global::jpeg.JpegRgbRawConverter();
        global::jpeg.JpegPlanarImageBuffer source = CreatePlanarBuffer();

        global::jpeg.JpegImage image = converter.ConvertPlanarToImage(source);

        Assert.That(image.Width, Is.EqualTo(2));
        Assert.That(image.Height, Is.EqualTo(2));
        Assert.That(image.Precision, Is.EqualTo(8));
        Assert.That(image.ComponentCount, Is.EqualTo(3));
        Assert.That(image.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.Rgb24));
        Assert.That(image.ColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.RGB));
        Assert.That(image.Subsampling, Is.EqualTo(global::jpeg.JpegSubsampling.Ratio444));
        Assert.That(image.Pixels, Is.EqualTo(CreateExpectedPixels()));
    }

    [Test]
    public void RawRgbDecode_CopySupportsDestinationStride()
    {
        global::jpeg.JpegRgbRawConverter converter = new global::jpeg.JpegRgbRawConverter();
        global::jpeg.JpegPlanarImageBuffer source = CreatePlanarBuffer();
        byte[] destination = new byte[] { 0, 0, 0, 0, 0, 0, 77, 0, 0, 0, 0, 0, 0, 88 };

        converter.CopyPlanarToRgb(source.Components[0], source.Components[1], source.Components[2], 2, 2, destination, 7);

        Assert.That(destination, Is.EqualTo(new byte[] { 1, 101, 201, 2, 102, 202, 77, 3, 103, 203, 4, 104, 204, 88 }));
    }

    [Test]
    public void RawRgbEncode_SplitsRgbImageIntoPlanarComponents()
    {
        global::jpeg.JpegRgbRawConverter converter = new global::jpeg.JpegRgbRawConverter();
        global::jpeg.JpegImage source = CreateRgbImage();

        global::jpeg.JpegPlanarImageBuffer buffer = converter.ConvertRgbImageToPlanar(source);

        Assert.That(buffer.Width, Is.EqualTo(2));
        Assert.That(buffer.Height, Is.EqualTo(2));
        Assert.That(buffer.ComponentCount, Is.EqualTo(3));
        AssertComponent(buffer.Components[0], 0, 1, new byte[] { 1, 2, 3, 4 });
        AssertComponent(buffer.Components[1], 1, 2, new byte[] { 101, 102, 103, 104 });
        AssertComponent(buffer.Components[2], 2, 3, new byte[] { 201, 202, 203, 204 });
    }

    [Test]
    public void RawRgbEncode_SplitsRgbSourceWithStride()
    {
        global::jpeg.JpegRgbRawConverter converter = new global::jpeg.JpegRgbRawConverter();
        byte[] source = new byte[]
        {
            1, 101, 201, 2, 102, 202, 99, 99,
            3, 103, 203, 4, 104, 204, 98, 98
        };
        global::jpeg.JpegPlanarComponentBuffer red = CreateComponent(2, 2, 3, new byte[6]);
        global::jpeg.JpegPlanarComponentBuffer green = CreateComponent(2, 2, 3, new byte[6]);
        global::jpeg.JpegPlanarComponentBuffer blue = CreateComponent(2, 2, 3, new byte[6]);

        converter.SplitPackedRgbToPlanar(source, 2, 2, 8, red, green, blue);

        Assert.That(red.Samples, Is.EqualTo(new byte[] { 1, 2, 0, 3, 4, 0 }));
        Assert.That(green.Samples, Is.EqualTo(new byte[] { 101, 102, 0, 103, 104, 0 }));
        Assert.That(blue.Samples, Is.EqualTo(new byte[] { 201, 202, 0, 203, 204, 0 }));
    }

    [Test]
    public void RawRgb_AllowsZeroHeightDecodeAndEncode()
    {
        global::jpeg.JpegRgbRawConverter converter = new global::jpeg.JpegRgbRawConverter();
        global::jpeg.JpegPlanarImageBuffer planar = new global::jpeg.JpegPlanarImageBuffer();
        planar.Width = 4;
        planar.Height = 0;
        planar.ComponentCount = 3;
        planar.Components.Add(CreateComponent(4, 0, 4, new byte[0]));
        planar.Components.Add(CreateComponent(4, 0, 4, new byte[0]));
        planar.Components.Add(CreateComponent(4, 0, 4, new byte[0]));
        global::jpeg.JpegImage image = new global::jpeg.JpegImage();
        image.Width = 4;
        image.Height = 0;
        image.ComponentCount = 3;
        image.PixelFormat = global::jpeg.JpegPixelFormat.Rgb24;
        image.Pixels = new byte[0];

        global::jpeg.JpegInterleavedPixelBuffer rgb = converter.ConvertPlanarToRgb(planar);
        global::jpeg.JpegPlanarImageBuffer encoded = converter.ConvertRgbImageToPlanar(image);

        Assert.That(rgb.Pixels.Length, Is.EqualTo(0));
        Assert.That(encoded.Components[0].Samples.Length, Is.EqualTo(0));
    }

    [Test]
    public void RawRgb_RejectsInvalidDecodeInputs()
    {
        global::jpeg.JpegRgbRawConverter converter = new global::jpeg.JpegRgbRawConverter();
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
    public void RawRgb_RejectsInvalidEncodeInputs()
    {
        global::jpeg.JpegRgbRawConverter converter = new global::jpeg.JpegRgbRawConverter();
        global::jpeg.JpegImage image = CreateRgbImage();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                converter.ConvertRgbImageToPlanar(null!);
            });

        image.PixelFormat = global::jpeg.JpegPixelFormat.Bgr24;
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                converter.ConvertRgbImageToPlanar(image);
            });

        image = CreateRgbImage();
        image.Pixels = new byte[3];
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                converter.ConvertRgbImageToPlanar(image);
            });
    }

    [Test]
    public void RawRgb_RejectsInvalidCopyInputs()
    {
        global::jpeg.JpegRgbRawConverter converter = new global::jpeg.JpegRgbRawConverter();
        global::jpeg.JpegPlanarComponentBuffer component = CreateComponent(2, 2, 2, new byte[] { 1, 2, 3, 4 });

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
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                converter.SplitPackedRgbToPlanar(new byte[6], 2, 1, 5, component, component, component);
            });
        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                converter.WriteRgbPixel(-1, 2, 3, new byte[3], 0);
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

    public static global::jpeg.JpegImage CreateRgbImage()
    {
        global::jpeg.JpegImage image = new global::jpeg.JpegImage();
        image.Width = 2;
        image.Height = 2;
        image.Precision = 8;
        image.ComponentCount = 3;
        image.PixelFormat = global::jpeg.JpegPixelFormat.Rgb24;
        image.ColorSpace = global::jpeg.JpegColorSpace.RGB;
        image.Pixels = CreateExpectedPixels();
        return image;
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

    public static void AssertComponent(
        global::jpeg.JpegPlanarComponentBuffer component,
        int componentIndex,
        int identifier,
        byte[] samples)
    {
        Assert.That(component.ComponentIndex, Is.EqualTo(componentIndex));
        Assert.That(component.Identifier, Is.EqualTo(identifier));
        Assert.That(component.Width, Is.EqualTo(2));
        Assert.That(component.Height, Is.EqualTo(2));
        Assert.That(component.PaddedWidth, Is.EqualTo(2));
        Assert.That(component.PaddedHeight, Is.EqualTo(2));
        Assert.That(component.Stride, Is.EqualTo(2));
        Assert.That(component.Samples, Is.EqualTo(samples));
    }
}
