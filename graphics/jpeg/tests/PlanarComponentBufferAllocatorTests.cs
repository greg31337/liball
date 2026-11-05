namespace jpeg.Tests;

internal class PlanarComponentBufferAllocatorTests
{
    [Test]
    public void Allocator_AllocatesPlanarBuffersFromMcuLayout()
    {
        global::jpeg.JpegFrameHeader frame = FrameHeaderWriterTests.CreateBaselineFrame();
        frame.Width = 17;
        frame.Height = 17;
        global::jpeg.JpegMcuLayout layout = new global::jpeg.JpegMcuLayoutCalculator().Calculate(frame);
        global::jpeg.JpegPlanarComponentBufferAllocator allocator = new global::jpeg.JpegPlanarComponentBufferAllocator();

        global::jpeg.JpegPlanarImageBuffer buffer = allocator.Allocate(layout);

        Assert.That(buffer.Width, Is.EqualTo(17));
        Assert.That(buffer.Height, Is.EqualTo(17));
        Assert.That(buffer.ComponentCount, Is.EqualTo(3));
        Assert.That(buffer.Components.Count, Is.EqualTo(3));
        AssertComponent(buffer.Components[0], 0, 1, 17, 17, 24, 24, 576);
        AssertComponent(buffer.Components[1], 1, 2, 9, 9, 16, 16, 256);
        AssertComponent(buffer.Components[2], 2, 3, 9, 9, 16, 16, 256);
    }

    [Test]
    public void Allocator_AllocatesPlanarBuffersForFrame()
    {
        global::jpeg.JpegFrameHeader frame = FrameHeaderWriterTests.CreateBaselineFrame();
        global::jpeg.JpegPlanarComponentBufferAllocator allocator = new global::jpeg.JpegPlanarComponentBufferAllocator();

        global::jpeg.JpegPlanarImageBuffer buffer = allocator.AllocateForFrame(frame);

        Assert.That(buffer.Width, Is.EqualTo(32));
        Assert.That(buffer.Height, Is.EqualTo(16));
        Assert.That(buffer.ComponentCount, Is.EqualTo(3));
        AssertComponent(buffer.Components[0], 0, 1, 32, 16, 32, 16, 512);
        AssertComponent(buffer.Components[1], 1, 2, 16, 8, 16, 8, 128);
    }

    [Test]
    public void Allocator_AllocatesIndependentZeroedComponentBuffers()
    {
        global::jpeg.JpegFrameHeader frame = FrameHeaderWriterTests.CreateBaselineFrame();
        global::jpeg.JpegPlanarComponentBufferAllocator allocator = new global::jpeg.JpegPlanarComponentBufferAllocator();
        global::jpeg.JpegPlanarImageBuffer buffer = allocator.AllocateForFrame(frame);

        buffer.Components[0].Samples[0] = 42;

        Assert.That(buffer.Components[0].Samples[0], Is.EqualTo(42));
        Assert.That(buffer.Components[1].Samples[0], Is.EqualTo(0));
        Assert.That(object.ReferenceEquals(buffer.Components[0].Samples, buffer.Components[1].Samples), Is.False);
    }

    [Test]
    public void Allocator_FindsComponentBufferByIdentifier()
    {
        global::jpeg.JpegFrameHeader frame = FrameHeaderWriterTests.CreateBaselineFrame();
        global::jpeg.JpegPlanarComponentBufferAllocator allocator = new global::jpeg.JpegPlanarComponentBufferAllocator();
        global::jpeg.JpegPlanarImageBuffer buffer = allocator.AllocateForFrame(frame);

        global::jpeg.JpegPlanarComponentBuffer component = allocator.FindComponentBuffer(buffer, 2);

        Assert.That(component.Identifier, Is.EqualTo(2));
        Assert.That(component.ComponentIndex, Is.EqualTo(1));
        Assert.That(component.Width, Is.EqualTo(16));
        Assert.That(component.Stride, Is.EqualTo(16));
    }

    [Test]
    public void Allocator_AllowsZeroHeightLayoutWithEmptyComponentBuffers()
    {
        global::jpeg.JpegFrameHeader frame = FrameHeaderWriterTests.CreateBaselineFrame();
        frame.Height = 0;
        global::jpeg.JpegPlanarComponentBufferAllocator allocator = new global::jpeg.JpegPlanarComponentBufferAllocator();

        global::jpeg.JpegPlanarImageBuffer buffer = allocator.AllocateForFrame(frame);

        Assert.That(buffer.Height, Is.EqualTo(0));
        Assert.That(buffer.Components[0].Width, Is.EqualTo(32));
        Assert.That(buffer.Components[0].Height, Is.EqualTo(0));
        Assert.That(buffer.Components[0].PaddedWidth, Is.EqualTo(32));
        Assert.That(buffer.Components[0].PaddedHeight, Is.EqualTo(0));
        Assert.That(buffer.Components[0].Samples.Length, Is.EqualTo(0));
    }

    [Test]
    public void Allocator_RejectsInvalidLayouts()
    {
        global::jpeg.JpegPlanarComponentBufferAllocator allocator = new global::jpeg.JpegPlanarComponentBufferAllocator();
        global::jpeg.JpegMcuLayout layout = new global::jpeg.JpegMcuLayout();
        layout.Width = 16;
        layout.Height = 16;
        layout.ComponentCount = 1;

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                allocator.Allocate(null!);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                allocator.Allocate(layout);
            });

        layout.Components.Add(new global::jpeg.JpegMcuComponentLayout());
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                allocator.Allocate(layout);
            });

        global::jpeg.JpegPlanarImageBuffer buffer = new global::jpeg.JpegPlanarImageBuffer();
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                allocator.FindComponentBuffer(buffer, 1);
            });

        buffer.Components = null!;
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                allocator.FindComponentBuffer(buffer, 1);
            });
    }

    public static void AssertComponent(
        global::jpeg.JpegPlanarComponentBuffer component,
        int componentIndex,
        int identifier,
        int width,
        int height,
        int paddedWidth,
        int paddedHeight,
        int sampleLength)
    {
        Assert.That(component.ComponentIndex, Is.EqualTo(componentIndex));
        Assert.That(component.Identifier, Is.EqualTo(identifier));
        Assert.That(component.Width, Is.EqualTo(width));
        Assert.That(component.Height, Is.EqualTo(height));
        Assert.That(component.PaddedWidth, Is.EqualTo(paddedWidth));
        Assert.That(component.PaddedHeight, Is.EqualTo(paddedHeight));
        Assert.That(component.Stride, Is.EqualTo(paddedWidth));
        Assert.That(component.Samples.Length, Is.EqualTo(sampleLength));
    }
}
