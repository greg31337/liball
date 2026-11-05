namespace jpeg.Tests;

internal class SampleBufferModelTests
{
    [Test]
    public void InterleavedAllocator_RepresentsEightTwelveAndSixteenBitStridedSamples()
    {
        global::jpeg.JpegInterleavedPixelBufferAllocator allocator = new global::jpeg.JpegInterleavedPixelBufferAllocator();

        global::jpeg.JpegInterleavedPixelBuffer eightBit = allocator.AllocateWithPrecisionAndStride(
            2, 3, global::jpeg.JpegPixelFormat.RawComponents, 5, 8, 12);
        global::jpeg.JpegInterleavedPixelBuffer twelveBit = allocator.AllocateWithPrecisionAndStride(
            2, 3, global::jpeg.JpegPixelFormat.RawComponents, 5, 12, 11);
        global::jpeg.JpegInterleavedPixelBuffer sixteenBit = allocator.AllocateWithPrecisionAndStride(
            2, 3, global::jpeg.JpegPixelFormat.RawComponents, 5, 16, 10);

        Assert.That(eightBit.Precision, Is.EqualTo(8));
        Assert.That(eightBit.ComponentCount, Is.EqualTo(5));
        Assert.That(eightBit.SamplesPerPixel, Is.EqualTo(5));
        Assert.That(eightBit.Stride, Is.EqualTo(12));
        Assert.That(eightBit.SampleStorage, Is.EqualTo(global::jpeg.JpegSampleStorage.Byte));
        Assert.That(eightBit.Ownership, Is.EqualTo(global::jpeg.JpegBufferOwnership.LibraryOwned));
        Assert.That(eightBit.Pixels.Length, Is.EqualTo(36));
        Assert.That(eightBit.IntSamples, Is.Empty);

        Assert.That(twelveBit.Precision, Is.EqualTo(12));
        Assert.That(twelveBit.Stride, Is.EqualTo(11));
        Assert.That(twelveBit.SampleStorage, Is.EqualTo(global::jpeg.JpegSampleStorage.Int32));
        Assert.That(twelveBit.Pixels, Is.Empty);
        Assert.That(twelveBit.IntSamples.Length, Is.EqualTo(33));
        twelveBit.IntSamples[0] = 4095;
        Assert.That(twelveBit.IntSamples[0], Is.EqualTo(4095));

        Assert.That(sixteenBit.Precision, Is.EqualTo(16));
        Assert.That(sixteenBit.Stride, Is.EqualTo(10));
        Assert.That(sixteenBit.IntSamples.Length, Is.EqualTo(30));
        sixteenBit.IntSamples[0] = 65535;
        Assert.That(sixteenBit.IntSamples[0], Is.EqualTo(65535));
    }

    [Test]
    public void InterleavedAllocator_RejectsUnsupportedPrecisionAndShortStride()
    {
        global::jpeg.JpegInterleavedPixelBufferAllocator allocator = new global::jpeg.JpegInterleavedPixelBufferAllocator();

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                allocator.AllocateWithPrecisionAndStride(2, 2, global::jpeg.JpegPixelFormat.RawComponents, 3, 10, 6);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                allocator.AllocateWithPrecisionAndStride(2, 2, global::jpeg.JpegPixelFormat.RawComponents, 3, 12, 5);
            });
    }

    [Test]
    public void PlanarAllocator_RepresentsArbitraryComponentsSamplingAndWideSamples()
    {
        global::jpeg.JpegMcuLayout layout = CreateFiveComponentLayout();
        global::jpeg.JpegPlanarComponentBufferAllocator allocator = new global::jpeg.JpegPlanarComponentBufferAllocator();

        global::jpeg.JpegPlanarImageBuffer buffer = allocator.AllocateWithPrecision(layout, 12);

        Assert.That(buffer.Width, Is.EqualTo(16));
        Assert.That(buffer.Height, Is.EqualTo(8));
        Assert.That(buffer.Precision, Is.EqualTo(12));
        Assert.That(buffer.ComponentCount, Is.EqualTo(5));
        Assert.That(buffer.Components.Count, Is.EqualTo(5));
        Assert.That(buffer.Subsampling, Is.EqualTo(global::jpeg.JpegSubsampling.Custom));
        Assert.That(buffer.Ownership, Is.EqualTo(global::jpeg.JpegBufferOwnership.LibraryOwned));

        global::jpeg.JpegPlanarComponentBuffer first = buffer.Components[0];
        Assert.That(first.Identifier, Is.EqualTo(10));
        Assert.That(first.HorizontalSamplingFactor, Is.EqualTo(4));
        Assert.That(first.VerticalSamplingFactor, Is.EqualTo(2));
        Assert.That(first.Precision, Is.EqualTo(12));
        Assert.That(first.SampleStorage, Is.EqualTo(global::jpeg.JpegSampleStorage.Int32));
        Assert.That(first.Ownership, Is.EqualTo(global::jpeg.JpegBufferOwnership.LibraryOwned));
        Assert.That(first.Stride, Is.EqualTo(16));
        Assert.That(first.IntSamples.Length, Is.EqualTo(128));
        Assert.That(first.Samples, Is.Empty);

        global::jpeg.JpegPlanarComponentBuffer fifth = buffer.Components[4];
        Assert.That(fifth.Identifier, Is.EqualTo(14));
        Assert.That(fifth.HorizontalSamplingFactor, Is.EqualTo(1));
        Assert.That(fifth.VerticalSamplingFactor, Is.EqualTo(1));
        Assert.That(fifth.IntSamples.Length, Is.EqualTo(64));
    }

    [Test]
    public void PlanarAllocator_SupportsExplicitRowStrideForSixteenBitPlane()
    {
        global::jpeg.JpegMcuComponentLayout layout = CreateComponentLayout(0, 7, 2, 1, 8, 4, 8, 8);
        global::jpeg.JpegPlanarComponentBufferAllocator allocator = new global::jpeg.JpegPlanarComponentBufferAllocator();

        global::jpeg.JpegPlanarComponentBuffer component = allocator.AllocateComponentWithPrecisionAndStride(layout, 16, 12);

        Assert.That(component.Precision, Is.EqualTo(16));
        Assert.That(component.Stride, Is.EqualTo(12));
        Assert.That(component.IntSamples.Length, Is.EqualTo(96));
        component.IntSamples[3 * component.Stride + 7] = 65535;
        Assert.That(component.IntSamples[43], Is.EqualTo(65535));
        Assert.That(component.Samples, Is.Empty);
    }

    [Test]
    public void ImageModels_DescribeCallerOwnedPlanarSixteenBitData()
    {
        int[] samples = new int[] { 0, 65535, 1024, 32768, 9, 9 };
        global::jpeg.JpegPlanarComponentBuffer component = new global::jpeg.JpegPlanarComponentBuffer();
        component.ComponentIndex = 0;
        component.Identifier = 1;
        component.Precision = 16;
        component.HorizontalSamplingFactor = 1;
        component.VerticalSamplingFactor = 1;
        component.Width = 2;
        component.Height = 2;
        component.PaddedWidth = 2;
        component.PaddedHeight = 2;
        component.Stride = 3;
        component.SampleStorage = global::jpeg.JpegSampleStorage.Int32;
        component.Ownership = global::jpeg.JpegBufferOwnership.CallerOwned;
        component.IntSamples = samples;
        global::jpeg.JpegPlanarImageBuffer planar = new global::jpeg.JpegPlanarImageBuffer();
        planar.Width = 2;
        planar.Height = 2;
        planar.Precision = 16;
        planar.ComponentCount = 1;
        planar.ColorSpace = global::jpeg.JpegColorSpace.Grayscale;
        planar.Subsampling = global::jpeg.JpegSubsampling.Grayscale;
        planar.Ownership = global::jpeg.JpegBufferOwnership.CallerOwned;
        planar.Components.Add(component);
        global::jpeg.JpegImage image = new global::jpeg.JpegImage();
        image.Width = 2;
        image.Height = 2;
        image.Precision = 16;
        image.ComponentCount = 1;
        image.BufferLayout = global::jpeg.JpegBufferLayout.Planar;
        image.BufferOwnership = global::jpeg.JpegBufferOwnership.CallerOwned;
        image.PlanarBuffer = planar;

        Assert.That(image.PlanarBuffer.Components[0].IntSamples, Is.SameAs(samples));
        Assert.That(image.PlanarBuffer.Components[0].IntSamples[1], Is.EqualTo(65535));
        Assert.That(image.BufferOwnership, Is.EqualTo(global::jpeg.JpegBufferOwnership.CallerOwned));
        Assert.That(image.RowStride, Is.EqualTo(0));
    }

    [Test]
    public void FactoryAndDecoder_ReportLibraryOwnedInterleavedBuffers()
    {
        byte[] pixels = new byte[] { 10, 20, 30, 40 };
        global::jpeg.JpegImage created = new global::jpeg.JpegImageFactory().CreateImage(
            pixels, 2, 2, global::jpeg.JpegPixelFormat.L8);
        pixels[0] = 99;

        Assert.That(created.BufferLayout, Is.EqualTo(global::jpeg.JpegBufferLayout.Interleaved));
        Assert.That(created.BufferOwnership, Is.EqualTo(global::jpeg.JpegBufferOwnership.LibraryOwned));
        Assert.That(created.RowStride, Is.EqualTo(2));
        Assert.That(created.InterleavedBuffer.Precision, Is.EqualTo(8));
        Assert.That(created.InterleavedBuffer.SampleStorage, Is.EqualTo(global::jpeg.JpegSampleStorage.Byte));
        Assert.That(created.InterleavedBuffer.Pixels, Is.SameAs(created.Pixels));
        Assert.That(created.Pixels[0], Is.EqualTo(10));

        global::jpeg.JpegImage decoded = new global::jpeg.JpegReader().Read(
            JpegReadApiTests.CreateReadableGrayscaleJpeg(2));
        Assert.That(decoded.BufferLayout, Is.EqualTo(global::jpeg.JpegBufferLayout.Interleaved));
        Assert.That(decoded.BufferOwnership, Is.EqualTo(global::jpeg.JpegBufferOwnership.LibraryOwned));
        Assert.That(decoded.RowStride, Is.EqualTo(8));
        Assert.That(decoded.InterleavedBuffer.Pixels, Is.SameAs(decoded.Pixels));
    }

    internal static global::jpeg.JpegMcuLayout CreateFiveComponentLayout()
    {
        global::jpeg.JpegMcuLayout layout = new global::jpeg.JpegMcuLayout();
        layout.Width = 16;
        layout.Height = 8;
        layout.ComponentCount = 5;
        layout.Components.Add(CreateComponentLayout(0, 10, 4, 2, 16, 8, 16, 8));
        layout.Components.Add(CreateComponentLayout(1, 11, 2, 1, 8, 4, 8, 8));
        layout.Components.Add(CreateComponentLayout(2, 12, 1, 1, 4, 4, 8, 8));
        layout.Components.Add(CreateComponentLayout(3, 13, 1, 1, 4, 4, 8, 8));
        layout.Components.Add(CreateComponentLayout(4, 14, 1, 1, 4, 4, 8, 8));
        return layout;
    }

    internal static global::jpeg.JpegMcuComponentLayout CreateComponentLayout(
        int index,
        int identifier,
        int horizontalSampling,
        int verticalSampling,
        int width,
        int height,
        int paddedWidth,
        int paddedHeight)
    {
        global::jpeg.JpegMcuComponentLayout layout = new global::jpeg.JpegMcuComponentLayout();
        layout.ComponentIndex = index;
        layout.Identifier = identifier;
        layout.HorizontalSamplingFactor = horizontalSampling;
        layout.VerticalSamplingFactor = verticalSampling;
        layout.SampleWidth = width;
        layout.SampleHeight = height;
        layout.PaddedSampleWidth = paddedWidth;
        layout.PaddedSampleHeight = paddedHeight;
        return layout;
    }
}
