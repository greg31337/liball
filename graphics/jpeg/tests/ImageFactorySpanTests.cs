namespace jpeg.Tests;

internal class ImageFactorySpanTests
{
    [Test]
    public void Factory_CreatesCopiedPackedByteImageWithRowStride()
    {
        byte[] samples = new byte[]
        {
            10, 20, 30, 40, 50, 60, 201, 202,
            70, 80, 90, 100, 110, 120, 203, 204
        };
        global::jpeg.IJpegImageFactory factory = new global::jpeg.JpegImageFactory();

        global::jpeg.JpegImage image = factory.CreatePackedImageFromByteSamples(
            samples.AsSpan(),
            2,
            2,
            3,
            8,
            global::jpeg.JpegPixelFormat.Rgb24,
            global::jpeg.JpegColorSpace.RGB);
        samples[0] = 255;

        Assert.That(image.Width, Is.EqualTo(2));
        Assert.That(image.Height, Is.EqualTo(2));
        Assert.That(image.Precision, Is.EqualTo(8));
        Assert.That(image.ComponentCount, Is.EqualTo(3));
        Assert.That(image.RowStride, Is.EqualTo(8));
        Assert.That(image.BufferLayout, Is.EqualTo(global::jpeg.JpegBufferLayout.Interleaved));
        Assert.That(image.BufferOwnership, Is.EqualTo(global::jpeg.JpegBufferOwnership.LibraryOwned));
        Assert.That(image.Pixels, Is.SameAs(image.InterleavedBuffer.Pixels));
        Assert.That(image.Pixels[0], Is.EqualTo(10));
        Assert.That(image.Pixels[8], Is.EqualTo(70));
        Assert.That(image.Pixels[6], Is.EqualTo(201));
    }

    [Test]
    public void Factory_PackedByteImageCanBeEncodedWithoutReadingRowPaddingAsPixels()
    {
        byte[] samples = new byte[10 * 8];
        int y = 0;
        while (y < 8)
        {
            int x = 0;
            while (x < 8)
            {
                samples[y * 10 + x] = 138;
                x = x + 1;
            }

            samples[y * 10 + 8] = 0;
            samples[y * 10 + 9] = 255;
            y = y + 1;
        }

        global::jpeg.JpegImage image = new global::jpeg.JpegImageFactory().CreatePackedImageFromByteSamples(
            samples.AsSpan(),
            8,
            8,
            1,
            10,
            global::jpeg.JpegPixelFormat.L8,
            global::jpeg.JpegColorSpace.Grayscale);

        global::jpeg.JpegImage decoded = global::jpeg.Jpeg.Read(global::jpeg.Jpeg.Write(image));

        Assert.That(decoded.Pixels[0], Is.EqualTo(138));
        Assert.That(decoded.Pixels[7], Is.EqualTo(138));
        Assert.That(decoded.Pixels[8], Is.EqualTo(138));
        Assert.That(decoded.Pixels[63], Is.EqualTo(138));
    }

    [Test]
    public void Factory_CreatesCopiedPackedTwelveAndSixteenBitImages()
    {
        int[] twelveSamples = new int[]
        {
            0, 1, 2, 3, 4, 5, 4000,
            4095, 10, 20, 30, 40, 50, 3999
        };
        int[] sixteenSamples = new int[] { 0, 65535, 1234, 50000 };
        global::jpeg.IJpegImageFactory factory = new global::jpeg.JpegImageFactory();

        global::jpeg.JpegImage twelveBit = factory.CreatePackedImageFromIntSamples(
            twelveSamples.AsSpan(), 2, 2, 3, 7, 12, global::jpeg.JpegColorSpace.YCbCr);
        global::jpeg.JpegImage sixteenBit = factory.CreatePackedImageFromIntSamples(
            sixteenSamples.AsSpan(), 2, 2, 1, 2, 16, global::jpeg.JpegColorSpace.Grayscale);
        twelveSamples[0] = 999;
        sixteenSamples[1] = 1;

        Assert.That(twelveBit.Precision, Is.EqualTo(12));
        Assert.That(twelveBit.ComponentCount, Is.EqualTo(3));
        Assert.That(twelveBit.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.RawComponents));
        Assert.That(twelveBit.RowStride, Is.EqualTo(7));
        Assert.That(twelveBit.InterleavedBuffer.SampleStorage, Is.EqualTo(global::jpeg.JpegSampleStorage.Int32));
        Assert.That(twelveBit.InterleavedBuffer.IntSamples[0], Is.EqualTo(0));
        Assert.That(twelveBit.InterleavedBuffer.IntSamples[7], Is.EqualTo(4095));
        Assert.That(twelveBit.InterleavedBuffer.Ownership, Is.EqualTo(global::jpeg.JpegBufferOwnership.LibraryOwned));

        Assert.That(sixteenBit.Precision, Is.EqualTo(16));
        Assert.That(sixteenBit.ComponentCount, Is.EqualTo(1));
        Assert.That(sixteenBit.InterleavedBuffer.IntSamples[1], Is.EqualTo(65535));
        Assert.That(sixteenBit.Subsampling, Is.EqualTo(global::jpeg.JpegSubsampling.Grayscale));
    }

    [Test]
    public void Factory_CreatesCopiedPlanarByteImageFromConcatenatedPlanes()
    {
        global::jpeg.JpegPlanarImageBuffer layout = CreateThreePlaneLayout(8);
        byte[] samples = new byte[]
        {
            1, 2, 3, 4, 90,
            5, 6, 7, 8, 91,
            20, 21, 92,
            30, 31, 93
        };
        global::jpeg.IJpegImageFactory factory = new global::jpeg.JpegImageFactory();

        global::jpeg.JpegImage image = factory.CreatePlanarImageFromByteSamples(samples.AsSpan(), layout);
        samples[0] = 255;
        layout.Components[0].Identifier = 99;

        Assert.That(image.BufferLayout, Is.EqualTo(global::jpeg.JpegBufferLayout.Planar));
        Assert.That(image.BufferOwnership, Is.EqualTo(global::jpeg.JpegBufferOwnership.LibraryOwned));
        Assert.That(image.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.RawComponents));
        Assert.That(image.PlanarBuffer.ComponentCount, Is.EqualTo(3));
        Assert.That(image.PlanarBuffer.Ownership, Is.EqualTo(global::jpeg.JpegBufferOwnership.LibraryOwned));
        Assert.That(image.PlanarBuffer.Components[0].Identifier, Is.EqualTo(1));
        Assert.That(image.PlanarBuffer.Components[0].Stride, Is.EqualTo(5));
        Assert.That(image.PlanarBuffer.Components[0].Samples, Is.EqualTo(new byte[] { 1, 2, 3, 4, 90, 5, 6, 7, 8, 91 }));
        Assert.That(image.PlanarBuffer.Components[1].Samples, Is.EqualTo(new byte[] { 20, 21, 92 }));
        Assert.That(image.PlanarBuffer.Components[2].Samples, Is.EqualTo(new byte[] { 30, 31, 93 }));
        Assert.That(image.PlanarBuffer.Components[1].HorizontalSamplingFactor, Is.EqualTo(1));
        Assert.That(image.Pixels, Is.Empty);
    }

    [Test]
    public void Factory_CreatesCopiedPlanarTwelveAndSixteenBitImages()
    {
        global::jpeg.JpegPlanarImageBuffer twelveLayout = CreateSinglePlaneLayout(12);
        global::jpeg.JpegPlanarImageBuffer sixteenLayout = CreateSinglePlaneLayout(16);
        int[] twelveSamples = new int[] { 0, 4095, 99, 100, 101, 102 };
        int[] sixteenSamples = new int[] { 0, 65535, 9, 1000, 2000, 8 };
        global::jpeg.IJpegImageFactory factory = new global::jpeg.JpegImageFactory();

        global::jpeg.JpegImage twelveBit = factory.CreatePlanarImageFromIntSamples(twelveSamples.AsSpan(), twelveLayout);
        global::jpeg.JpegImage sixteenBit = factory.CreatePlanarImageFromIntSamples(sixteenSamples.AsSpan(), sixteenLayout);
        twelveSamples[1] = 1;
        sixteenSamples[1] = 1;

        Assert.That(twelveBit.Precision, Is.EqualTo(12));
        Assert.That(twelveBit.PlanarBuffer.Components[0].SampleStorage, Is.EqualTo(global::jpeg.JpegSampleStorage.Int32));
        Assert.That(twelveBit.PlanarBuffer.Components[0].IntSamples[1], Is.EqualTo(4095));
        Assert.That(twelveBit.PlanarBuffer.Components[0].Ownership, Is.EqualTo(global::jpeg.JpegBufferOwnership.LibraryOwned));
        Assert.That(sixteenBit.Precision, Is.EqualTo(16));
        Assert.That(sixteenBit.PlanarBuffer.Components[0].IntSamples[1], Is.EqualTo(65535));
    }

    [Test]
    public void Factory_RejectsInvalidPackedSpanDescriptionsAndWideSamples()
    {
        global::jpeg.IJpegImageFactory factory = new global::jpeg.JpegImageFactory();

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                factory.CreatePackedImageFromByteSamples(
                    new byte[12].AsSpan(), 2, 2, 2, 6, global::jpeg.JpegPixelFormat.Rgb24, global::jpeg.JpegColorSpace.RGB);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                factory.CreatePackedImageFromByteSamples(
                    new byte[11].AsSpan(), 2, 2, 3, 6, global::jpeg.JpegPixelFormat.Rgb24, global::jpeg.JpegColorSpace.RGB);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                factory.CreatePackedImageFromIntSamples(
                    new int[4].AsSpan(), 2, 2, 1, 2, 8, global::jpeg.JpegColorSpace.Grayscale);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                factory.CreatePackedImageFromIntSamples(
                    new int[] { 0, 4096, 0, 0 }.AsSpan(), 2, 2, 1, 2, 12, global::jpeg.JpegColorSpace.Grayscale);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                factory.CreatePackedImageFromIntSamples(
                    new int[] { 0, -1, 0, 0 }.AsSpan(), 2, 2, 1, 2, 16, global::jpeg.JpegColorSpace.Grayscale);
            });
    }

    [Test]
    public void Factory_RejectsInvalidPlanarLayoutsAndSampleSpans()
    {
        global::jpeg.IJpegImageFactory factory = new global::jpeg.JpegImageFactory();
        global::jpeg.JpegPlanarImageBuffer layout = CreateSinglePlaneLayout(8);

        Assert.Throws<System.ArgumentNullException>(
            delegate { factory.CreatePlanarImageFromByteSamples(new byte[0].AsSpan(), null!); });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate { factory.CreatePlanarImageFromByteSamples(new byte[5].AsSpan(), layout); });

        layout.Components[0].HorizontalSamplingFactor = 0;
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate { factory.CreatePlanarImageFromByteSamples(new byte[6].AsSpan(), layout); });

        global::jpeg.JpegPlanarImageBuffer wideLayout = CreateSinglePlaneLayout(12);
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                factory.CreatePlanarImageFromIntSamples(
                    new int[] { 0, 4096, 0, 0, 0, 0 }.AsSpan(), wideLayout);
            });
    }

    internal static global::jpeg.JpegPlanarImageBuffer CreateThreePlaneLayout(int precision)
    {
        global::jpeg.JpegPlanarImageBuffer layout = new global::jpeg.JpegPlanarImageBuffer();
        layout.Width = 4;
        layout.Height = 2;
        layout.Precision = precision;
        layout.ComponentCount = 3;
        layout.ColorSpace = global::jpeg.JpegColorSpace.YCbCr;
        layout.Subsampling = global::jpeg.JpegSubsampling.Ratio422;
        layout.Components.Add(CreateComponent(0, 1, precision, 2, 1, 4, 2, 4, 2, 5));
        layout.Components.Add(CreateComponent(1, 2, precision, 1, 1, 2, 1, 2, 1, 3));
        layout.Components.Add(CreateComponent(2, 3, precision, 1, 1, 2, 1, 2, 1, 3));
        return layout;
    }

    internal static global::jpeg.JpegPlanarImageBuffer CreateSinglePlaneLayout(int precision)
    {
        global::jpeg.JpegPlanarImageBuffer layout = new global::jpeg.JpegPlanarImageBuffer();
        layout.Width = 2;
        layout.Height = 2;
        layout.Precision = precision;
        layout.ComponentCount = 1;
        layout.ColorSpace = global::jpeg.JpegColorSpace.Grayscale;
        layout.Subsampling = global::jpeg.JpegSubsampling.Grayscale;
        layout.Components.Add(CreateComponent(0, 1, precision, 1, 1, 2, 2, 2, 2, 3));
        return layout;
    }

    internal static global::jpeg.JpegPlanarComponentBuffer CreateComponent(
        int componentIndex,
        int identifier,
        int precision,
        int horizontalSampling,
        int verticalSampling,
        int width,
        int height,
        int paddedWidth,
        int paddedHeight,
        int stride)
    {
        global::jpeg.JpegPlanarComponentBuffer component = new global::jpeg.JpegPlanarComponentBuffer();
        component.ComponentIndex = componentIndex;
        component.Identifier = identifier;
        component.Precision = precision;
        component.HorizontalSamplingFactor = horizontalSampling;
        component.VerticalSamplingFactor = verticalSampling;
        component.Width = width;
        component.Height = height;
        component.PaddedWidth = paddedWidth;
        component.PaddedHeight = paddedHeight;
        component.Stride = stride;
        return component;
    }
}
