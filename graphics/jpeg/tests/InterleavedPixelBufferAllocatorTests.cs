namespace jpeg.Tests;

internal class InterleavedPixelBufferAllocatorTests
{
    [Test]
    public void Allocator_AllocatesRgbInterleavedPixelBuffer()
    {
        global::jpeg.JpegInterleavedPixelBufferAllocator allocator = new global::jpeg.JpegInterleavedPixelBufferAllocator();

        global::jpeg.JpegInterleavedPixelBuffer buffer = allocator.Allocate(17, 3, global::jpeg.JpegPixelFormat.Rgb24, 0);

        Assert.That(buffer.Width, Is.EqualTo(17));
        Assert.That(buffer.Height, Is.EqualTo(3));
        Assert.That(buffer.ComponentCount, Is.EqualTo(3));
        Assert.That(buffer.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.Rgb24));
        Assert.That(buffer.BytesPerPixel, Is.EqualTo(3));
        Assert.That(buffer.Stride, Is.EqualTo(51));
        Assert.That(buffer.Pixels.Length, Is.EqualTo(153));
    }

    [Test]
    public void Allocator_AllocatesAllFixedSizePixelFormats()
    {
        global::jpeg.JpegInterleavedPixelBufferAllocator allocator = new global::jpeg.JpegInterleavedPixelBufferAllocator();

        Assert.That(allocator.Allocate(2, 2, global::jpeg.JpegPixelFormat.L8, 0).Pixels.Length, Is.EqualTo(4));
        Assert.That(allocator.Allocate(2, 2, global::jpeg.JpegPixelFormat.Bgr24, 0).Pixels.Length, Is.EqualTo(12));
        Assert.That(allocator.Allocate(2, 2, global::jpeg.JpegPixelFormat.YCbCr24, 0).Pixels.Length, Is.EqualTo(12));
        Assert.That(allocator.Allocate(2, 2, global::jpeg.JpegPixelFormat.Rgba32, 0).Pixels.Length, Is.EqualTo(16));
        Assert.That(allocator.Allocate(2, 2, global::jpeg.JpegPixelFormat.Bgra32, 0).Pixels.Length, Is.EqualTo(16));
        Assert.That(allocator.Allocate(2, 2, global::jpeg.JpegPixelFormat.Cmyk32, 0).Pixels.Length, Is.EqualTo(16));
        Assert.That(allocator.Allocate(2, 2, global::jpeg.JpegPixelFormat.Ycck32, 0).Pixels.Length, Is.EqualTo(16));
        Assert.That(allocator.GetBytesPerPixel(global::jpeg.JpegPixelFormat.L8, 0), Is.EqualTo(1));
        Assert.That(allocator.GetBytesPerPixel(global::jpeg.JpegPixelFormat.Rgba32, 0), Is.EqualTo(4));
    }

    [Test]
    public void Allocator_AllocatesRawComponentPixelBuffers()
    {
        global::jpeg.JpegInterleavedPixelBufferAllocator allocator = new global::jpeg.JpegInterleavedPixelBufferAllocator();

        global::jpeg.JpegInterleavedPixelBuffer buffer = allocator.Allocate(4, 3, global::jpeg.JpegPixelFormat.RawComponents, 5);

        Assert.That(buffer.ComponentCount, Is.EqualTo(5));
        Assert.That(buffer.BytesPerPixel, Is.EqualTo(5));
        Assert.That(buffer.Stride, Is.EqualTo(20));
        Assert.That(buffer.Pixels.Length, Is.EqualTo(60));
        Assert.That(allocator.ResolveComponentCount(global::jpeg.JpegPixelFormat.RawComponents, 5), Is.EqualTo(5));
    }

    [Test]
    public void Allocator_AllocatesForImageAndCanCreateImage()
    {
        global::jpeg.JpegInterleavedPixelBufferAllocator allocator = new global::jpeg.JpegInterleavedPixelBufferAllocator();
        global::jpeg.JpegImage source = new global::jpeg.JpegImage();
        source.Width = 3;
        source.Height = 2;
        source.ComponentCount = 4;
        source.PixelFormat = global::jpeg.JpegPixelFormat.Cmyk32;

        global::jpeg.JpegInterleavedPixelBuffer buffer = allocator.AllocateForImage(source);
        global::jpeg.JpegImage image = allocator.CreateImage(3, 2, global::jpeg.JpegPixelFormat.Cmyk32, global::jpeg.JpegColorSpace.CMYK, 0);

        Assert.That(buffer.BytesPerPixel, Is.EqualTo(4));
        Assert.That(buffer.Pixels.Length, Is.EqualTo(24));
        Assert.That(image.Width, Is.EqualTo(3));
        Assert.That(image.Height, Is.EqualTo(2));
        Assert.That(image.ComponentCount, Is.EqualTo(4));
        Assert.That(image.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.Cmyk32));
        Assert.That(image.ColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.CMYK));
        Assert.That(image.Pixels.Length, Is.EqualTo(24));
    }

    [Test]
    public void Allocator_AllowsZeroHeightPixelBuffers()
    {
        global::jpeg.JpegInterleavedPixelBufferAllocator allocator = new global::jpeg.JpegInterleavedPixelBufferAllocator();

        global::jpeg.JpegInterleavedPixelBuffer buffer = allocator.Allocate(8, 0, global::jpeg.JpegPixelFormat.L8, 0);

        Assert.That(buffer.Width, Is.EqualTo(8));
        Assert.That(buffer.Height, Is.EqualTo(0));
        Assert.That(buffer.Stride, Is.EqualTo(8));
        Assert.That(buffer.Pixels.Length, Is.EqualTo(0));
    }

    [Test]
    public void Allocator_AllocatesIndependentZeroedPixelBuffers()
    {
        global::jpeg.JpegInterleavedPixelBufferAllocator allocator = new global::jpeg.JpegInterleavedPixelBufferAllocator();

        global::jpeg.JpegInterleavedPixelBuffer first = allocator.Allocate(2, 2, global::jpeg.JpegPixelFormat.L8, 0);
        global::jpeg.JpegInterleavedPixelBuffer second = allocator.Allocate(2, 2, global::jpeg.JpegPixelFormat.L8, 0);
        first.Pixels[0] = 42;

        Assert.That(first.Pixels[0], Is.EqualTo(42));
        Assert.That(second.Pixels[0], Is.EqualTo(0));
        Assert.That(object.ReferenceEquals(first.Pixels, second.Pixels), Is.False);
    }

    [Test]
    public void Allocator_RejectsInvalidInputs()
    {
        global::jpeg.JpegInterleavedPixelBufferAllocator allocator = new global::jpeg.JpegInterleavedPixelBufferAllocator();

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                allocator.Allocate(0, 1, global::jpeg.JpegPixelFormat.L8, 0);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                allocator.Allocate(1, -1, global::jpeg.JpegPixelFormat.L8, 0);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                allocator.Allocate(1, 1, global::jpeg.JpegPixelFormat.Unknown, 0);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                allocator.Allocate(1, 1, global::jpeg.JpegPixelFormat.RawComponents, 0);
            });
        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                allocator.AllocateForImage(null!);
            });
        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                allocator.GetStride(1, 0);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                allocator.GetBufferLength(50000, 50000, 4);
            });
    }
}
