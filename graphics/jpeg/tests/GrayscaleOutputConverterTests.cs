namespace jpeg.Tests;

internal class GrayscaleOutputConverterTests
{
    [Test]
    public void Grayscale_CopiesLogicalSamplesFromPaddedComponent()
    {
        global::jpeg.JpegGrayscaleOutputConverter converter = new global::jpeg.JpegGrayscaleOutputConverter();
        global::jpeg.JpegPlanarComponentBuffer component = CreatePaddedComponent();

        byte[] pixels = converter.ConvertComponentToL8(component, 3, 2);

        Assert.That(pixels, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6 }));
    }

    [Test]
    public void Grayscale_ConvertsPlanarBufferToL8Buffer()
    {
        global::jpeg.JpegGrayscaleOutputConverter converter = new global::jpeg.JpegGrayscaleOutputConverter();
        global::jpeg.JpegPlanarImageBuffer source = CreatePlanarBuffer();

        global::jpeg.JpegInterleavedPixelBuffer buffer = converter.ConvertPlanarToL8(source);

        Assert.That(buffer.Width, Is.EqualTo(3));
        Assert.That(buffer.Height, Is.EqualTo(2));
        Assert.That(buffer.ComponentCount, Is.EqualTo(1));
        Assert.That(buffer.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.L8));
        Assert.That(buffer.BytesPerPixel, Is.EqualTo(1));
        Assert.That(buffer.Stride, Is.EqualTo(3));
        Assert.That(buffer.Pixels, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6 }));
    }

    [Test]
    public void Grayscale_ConvertsPlanarBufferToImage()
    {
        global::jpeg.JpegGrayscaleOutputConverter converter = new global::jpeg.JpegGrayscaleOutputConverter();
        global::jpeg.JpegPlanarImageBuffer source = CreatePlanarBuffer();

        global::jpeg.JpegImage image = converter.ConvertPlanarToImage(source);

        Assert.That(image.Width, Is.EqualTo(3));
        Assert.That(image.Height, Is.EqualTo(2));
        Assert.That(image.Precision, Is.EqualTo(8));
        Assert.That(image.ComponentCount, Is.EqualTo(1));
        Assert.That(image.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.L8));
        Assert.That(image.ColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.Grayscale));
        Assert.That(image.Subsampling, Is.EqualTo(global::jpeg.JpegSubsampling.Grayscale));
        Assert.That(image.Pixels, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6 }));
    }

    [Test]
    public void Grayscale_CopySupportsDestinationStride()
    {
        global::jpeg.JpegGrayscaleOutputConverter converter = new global::jpeg.JpegGrayscaleOutputConverter();
        global::jpeg.JpegPlanarComponentBuffer component = CreatePaddedComponent();
        byte[] destination = new byte[] { 0, 0, 0, 77, 0, 0, 0, 88 };

        converter.CopyComponentToL8(component, 3, 2, destination, 4);

        Assert.That(destination, Is.EqualTo(new byte[] { 1, 2, 3, 77, 4, 5, 6, 88 }));
    }

    [Test]
    public void Grayscale_AllowsZeroHeightOutput()
    {
        global::jpeg.JpegGrayscaleOutputConverter converter = new global::jpeg.JpegGrayscaleOutputConverter();
        global::jpeg.JpegPlanarImageBuffer source = new global::jpeg.JpegPlanarImageBuffer();
        source.Width = 4;
        source.Height = 0;
        source.ComponentCount = 1;
        source.Components.Add(CreateComponent(4, 0, 8, new byte[0]));

        global::jpeg.JpegInterleavedPixelBuffer buffer = converter.ConvertPlanarToL8(source);

        Assert.That(buffer.Width, Is.EqualTo(4));
        Assert.That(buffer.Height, Is.EqualTo(0));
        Assert.That(buffer.Stride, Is.EqualTo(4));
        Assert.That(buffer.Pixels.Length, Is.EqualTo(0));
    }

    [Test]
    public void Grayscale_RejectsInvalidPlanarInputs()
    {
        global::jpeg.JpegGrayscaleOutputConverter converter = new global::jpeg.JpegGrayscaleOutputConverter();
        global::jpeg.JpegPlanarImageBuffer source = new global::jpeg.JpegPlanarImageBuffer();
        source.Width = 3;
        source.Height = 2;
        source.ComponentCount = 1;

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                converter.ConvertPlanarToL8(null!);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                converter.ConvertPlanarToL8(source);
            });

        source.Components = null!;
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                converter.ConvertPlanarToL8(source);
            });
    }

    [Test]
    public void Grayscale_RejectsInvalidCopyInputs()
    {
        global::jpeg.JpegGrayscaleOutputConverter converter = new global::jpeg.JpegGrayscaleOutputConverter();
        global::jpeg.JpegPlanarComponentBuffer component = CreatePaddedComponent();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                converter.ConvertComponentToL8(null!, 3, 2);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                converter.ConvertComponentToL8(component, 4, 2);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                converter.CopyLuminanceToL8(component.Samples, 3, 2, 2, new byte[6], 3);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                converter.CopyLuminanceToL8(component.Samples, 3, 2, 5, new byte[5], 3);
            });
        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                converter.CopyLuminanceToL8(null!, 3, 2, 5, new byte[6], 3);
            });
        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                converter.CopyLuminanceToL8(component.Samples, 3, 2, 5, null!, 3);
            });
    }

    public static global::jpeg.JpegPlanarImageBuffer CreatePlanarBuffer()
    {
        global::jpeg.JpegPlanarImageBuffer source = new global::jpeg.JpegPlanarImageBuffer();
        source.Width = 3;
        source.Height = 2;
        source.ComponentCount = 1;
        source.Components.Add(CreatePaddedComponent());
        return source;
    }

    public static global::jpeg.JpegPlanarComponentBuffer CreatePaddedComponent()
    {
        return CreateComponent(
            3,
            2,
            5,
            new byte[]
            {
                1, 2, 3, 99, 99,
                4, 5, 6, 98, 98,
                97, 97, 97, 97, 97,
                96, 96, 96, 96, 96
            });
    }

    public static global::jpeg.JpegPlanarComponentBuffer CreateComponent(int width, int height, int stride, byte[] samples)
    {
        global::jpeg.JpegPlanarComponentBuffer component = new global::jpeg.JpegPlanarComponentBuffer();
        component.ComponentIndex = 0;
        component.Identifier = 1;
        component.Width = width;
        component.Height = height;
        component.PaddedWidth = stride;
        component.PaddedHeight = height;
        component.Stride = stride;
        component.Samples = samples;
        return component;
    }
}
