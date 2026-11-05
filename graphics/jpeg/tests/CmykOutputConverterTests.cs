namespace jpeg.Tests;

internal class CmykOutputConverterTests
{
    [Test]
    public void CmykDecode_PacksPlanarComponentsWithoutInversion()
    {
        global::jpeg.JpegCmykOutputConverter converter = new global::jpeg.JpegCmykOutputConverter();
        global::jpeg.JpegPlanarImageBuffer source = CreatePlanarBuffer();

        byte[] pixels = converter.ConvertComponentsToCmyk(
            source.Components[0],
            source.Components[1],
            source.Components[2],
            source.Components[3],
            2,
            2,
            false);

        Assert.That(pixels, Is.EqualTo(CreateExpectedPixels()));
    }

    [Test]
    public void CmykDecode_InvertsAdobeCmykSamples()
    {
        global::jpeg.JpegCmykOutputConverter converter = new global::jpeg.JpegCmykOutputConverter();
        global::jpeg.JpegPlanarImageBuffer source = CreatePlanarBuffer();

        byte[] pixels = converter.ConvertComponentsToCmyk(
            source.Components[0],
            source.Components[1],
            source.Components[2],
            source.Components[3],
            2,
            2,
            true);

        Assert.That(pixels, Is.EqualTo(CreateExpectedInvertedPixels()));
        Assert.That(converter.ResolveDecodedSample(10, true), Is.EqualTo(245));
        Assert.That(converter.ResolveDecodedSample(10, false), Is.EqualTo(10));
    }

    [Test]
    public void CmykDecode_UsesAdobeMetadataInversionFlag()
    {
        global::jpeg.JpegCmykOutputConverter converter = new global::jpeg.JpegCmykOutputConverter();
        global::jpeg.JpegPlanarImageBuffer source = CreatePlanarBuffer();
        global::jpeg.JpegAdobeMetadata adobe = new global::jpeg.JpegAdobeMetadata();
        adobe.InvertsCmyk = true;

        global::jpeg.JpegInterleavedPixelBuffer buffer = converter.ConvertPlanarToCmykWithAdobeMetadata(source, adobe);

        Assert.That(converter.ShouldInvertAdobeCmyk(adobe), Is.True);
        Assert.That(converter.ShouldInvertAdobeCmyk(null), Is.False);
        Assert.That(buffer.Width, Is.EqualTo(2));
        Assert.That(buffer.Height, Is.EqualTo(2));
        Assert.That(buffer.ComponentCount, Is.EqualTo(4));
        Assert.That(buffer.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.Cmyk32));
        Assert.That(buffer.BytesPerPixel, Is.EqualTo(4));
        Assert.That(buffer.Stride, Is.EqualTo(8));
        Assert.That(buffer.Pixels, Is.EqualTo(CreateExpectedInvertedPixels()));
    }

    [Test]
    public void CmykDecode_ConvertsPlanarBufferToCmykImage()
    {
        global::jpeg.JpegCmykOutputConverter converter = new global::jpeg.JpegCmykOutputConverter();
        global::jpeg.JpegPlanarImageBuffer source = CreatePlanarBuffer();
        global::jpeg.JpegAdobeMetadata adobe = new global::jpeg.JpegAdobeMetadata();
        adobe.InvertsCmyk = true;

        global::jpeg.JpegImage image = converter.ConvertPlanarToImage(source, adobe);

        Assert.That(image.Width, Is.EqualTo(2));
        Assert.That(image.Height, Is.EqualTo(2));
        Assert.That(image.Precision, Is.EqualTo(8));
        Assert.That(image.ComponentCount, Is.EqualTo(4));
        Assert.That(image.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.Cmyk32));
        Assert.That(image.ColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.CMYK));
        Assert.That(image.Subsampling, Is.EqualTo(global::jpeg.JpegSubsampling.Ratio444));
        Assert.That(image.Pixels, Is.EqualTo(CreateExpectedInvertedPixels()));
    }

    [Test]
    public void CmykDecode_CopySupportsDestinationStride()
    {
        global::jpeg.JpegCmykOutputConverter converter = new global::jpeg.JpegCmykOutputConverter();
        global::jpeg.JpegPlanarImageBuffer source = CreatePlanarBuffer();
        byte[] destination = new byte[]
        {
            0, 0, 0, 0, 0, 0, 0, 0, 77,
            0, 0, 0, 0, 0, 0, 0, 0, 88
        };

        converter.CopyPlanarToCmyk(
            source.Components[0],
            source.Components[1],
            source.Components[2],
            source.Components[3],
            2,
            2,
            destination,
            9,
            false);

        Assert.That(
            destination,
            Is.EqualTo(new byte[] { 1, 101, 201, 251, 2, 102, 202, 252, 77, 3, 103, 203, 253, 4, 104, 204, 254, 88 }));
    }

    [Test]
    public void CmykDecode_AllowsZeroHeightOutput()
    {
        global::jpeg.JpegCmykOutputConverter converter = new global::jpeg.JpegCmykOutputConverter();
        global::jpeg.JpegPlanarImageBuffer source = new global::jpeg.JpegPlanarImageBuffer();
        source.Width = 4;
        source.Height = 0;
        source.ComponentCount = 4;
        source.Components.Add(CreateComponent(4, 0, 4, new byte[0]));
        source.Components.Add(CreateComponent(4, 0, 4, new byte[0]));
        source.Components.Add(CreateComponent(4, 0, 4, new byte[0]));
        source.Components.Add(CreateComponent(4, 0, 4, new byte[0]));

        global::jpeg.JpegInterleavedPixelBuffer buffer = converter.ConvertPlanarToCmykWithInversion(source, true);

        Assert.That(buffer.Width, Is.EqualTo(4));
        Assert.That(buffer.Height, Is.EqualTo(0));
        Assert.That(buffer.Stride, Is.EqualTo(16));
        Assert.That(buffer.Pixels.Length, Is.EqualTo(0));
    }

    [Test]
    public void CmykDecode_RejectsInvalidPlanarInputs()
    {
        global::jpeg.JpegCmykOutputConverter converter = new global::jpeg.JpegCmykOutputConverter();
        global::jpeg.JpegPlanarImageBuffer source = new global::jpeg.JpegPlanarImageBuffer();
        source.Width = 2;
        source.Height = 2;
        source.ComponentCount = 4;

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                converter.ConvertPlanarToCmykWithInversion(null!, false);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                converter.ConvertPlanarToCmykWithInversion(source, false);
            });

        source.Components = null!;
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                converter.ConvertPlanarToCmykWithInversion(source, false);
            });
    }

    [Test]
    public void CmykDecode_RejectsInvalidCopyInputs()
    {
        global::jpeg.JpegCmykOutputConverter converter = new global::jpeg.JpegCmykOutputConverter();
        global::jpeg.JpegPlanarComponentBuffer component = CreateComponent(2, 2, 2, new byte[] { 1, 2, 3, 4 });

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                converter.CopyPlanarToCmyk(null!, component, component, component, 2, 2, new byte[16], 8, false);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                converter.CopyPlanarToCmyk(component, component, component, CreateComponent(1, 2, 1, new byte[] { 1, 2 }), 2, 2, new byte[16], 8, false);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                converter.CopyPlanarToCmyk(component, component, component, component, 2, 2, new byte[15], 8, false);
            });
        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                converter.WriteCmykPixel(1, 2, 3, 4, null!, 0, false);
            });
        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                converter.WriteCmykPixel(-1, 2, 3, 4, new byte[4], 0, false);
            });
    }

    public static global::jpeg.JpegPlanarImageBuffer CreatePlanarBuffer()
    {
        global::jpeg.JpegPlanarImageBuffer source = new global::jpeg.JpegPlanarImageBuffer();
        source.Width = 2;
        source.Height = 2;
        source.ComponentCount = 4;
        source.Components.Add(CreateComponent(2, 2, 4, new byte[] { 1, 2, 99, 99, 3, 4, 98, 98 }));
        source.Components.Add(CreateComponent(2, 2, 4, new byte[] { 101, 102, 99, 99, 103, 104, 98, 98 }));
        source.Components.Add(CreateComponent(2, 2, 4, new byte[] { 201, 202, 99, 99, 203, 204, 98, 98 }));
        source.Components.Add(CreateComponent(2, 2, 4, new byte[] { 251, 252, 99, 99, 253, 254, 98, 98 }));
        return source;
    }

    public static byte[] CreateExpectedPixels()
    {
        return new byte[] { 1, 101, 201, 251, 2, 102, 202, 252, 3, 103, 203, 253, 4, 104, 204, 254 };
    }

    public static byte[] CreateExpectedInvertedPixels()
    {
        return new byte[] { 254, 154, 54, 4, 253, 153, 53, 3, 252, 152, 52, 2, 251, 151, 51, 1 };
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
