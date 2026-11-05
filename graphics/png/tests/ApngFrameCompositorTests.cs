using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class ApngFrameCompositorTests
{
    [Test]
    public void Compose_SourceReplacesFrameRectangle()
    {
        PngPixelBuffer canvas = CreateBuffer(PngPixelFormat.Rgba8, 3, 2, 16, new byte[]
        {
            1, 2, 3, 255, 4, 5, 6, 255, 7, 8, 9, 255, 99, 99, 99, 99,
            10, 11, 12, 255, 13, 14, 15, 255, 16, 17, 18, 255, 88, 88, 88, 88
        });
        PngPixelBuffer framePixels = CreateBuffer(PngPixelFormat.Rgba8, 2, 1, 12, new byte[]
        {
            40, 41, 42, 43, 50, 51, 52, 53, 77, 77, 77, 77
        });

        PngPixelBuffer result = PngApngFrameCompositor.Compose(canvas, framePixels, 1, 1, PngApngBlendOperation.Source);

        AssertBuffer(result, PngPixelFormat.Rgba8, 3, 2, 12, new byte[]
        {
            1, 2, 3, 255, 4, 5, 6, 255, 7, 8, 9, 255,
            10, 11, 12, 255, 40, 41, 42, 43, 50, 51, 52, 53
        });
        Assert.That(canvas.Data[20], Is.EqualTo(13));
    }

    [Test]
    public void Compose_OverBlendsStraightAlphaRgba8()
    {
        PngPixelBuffer canvas = CreateBuffer(PngPixelFormat.Rgba8, 2, 1, 8, new byte[]
        {
            10, 20, 30, 255, 200, 100, 50, 128
        });
        PngPixelBuffer framePixels = CreateBuffer(PngPixelFormat.Rgba8, 2, 1, 8, new byte[]
        {
            210, 20, 30, 128, 20, 200, 220, 64
        });

        PngPixelBuffer result = PngApngFrameCompositor.Compose(canvas, framePixels, 0, 0, PngApngBlendOperation.Over);

        AssertBuffer(result, PngPixelFormat.Rgba8, 2, 1, 8, new byte[]
        {
            110, 20, 30, 255, 128, 140, 118, 160
        });
    }

    [Test]
    public void Compose_OverUsesFrameOffsetAndIgnoresRowPadding()
    {
        PngPixelBuffer canvas = CreateBuffer(PngPixelFormat.Rgba8, 3, 2, 16, new byte[]
        {
            10, 20, 30, 255, 40, 50, 60, 128, 70, 80, 90, 0, 99, 99, 99, 99,
            100, 110, 120, 255, 130, 140, 150, 128, 160, 170, 180, 0, 88, 88, 88, 88
        });
        PngPixelBuffer framePixels = CreateBuffer(PngPixelFormat.Rgba8, 2, 2, 12, new byte[]
        {
            1, 2, 3, 0, 200, 50, 0, 255, 77, 77, 77, 77,
            10, 20, 30, 128, 250, 240, 230, 64, 66, 66, 66, 66
        });

        PngPixelBuffer result = PngApngFrameCompositor.Compose(canvas, framePixels, 1, 0, PngApngBlendOperation.Over);

        AssertBuffer(result, PngPixelFormat.Rgba8, 3, 2, 12, new byte[]
        {
            10, 20, 30, 255, 40, 50, 60, 128, 200, 50, 0, 255,
            100, 110, 120, 255, 50, 60, 70, 192, 250, 240, 230, 64
        });
        Assert.That(canvas.Data[12], Is.EqualTo(99));
        Assert.That(framePixels.Data[8], Is.EqualTo(77));
    }

    [Test]
    public void Compose_OverHandlesOpaqueAndTransparentRgba16()
    {
        PngPixelBuffer canvas = CreateBuffer(PngPixelFormat.Rgba16, 2, 1, 16, CreateBigEndianSamples(
            0x1111, 0x2222, 0x3333, 0x4444,
            0x5555, 0x6666, 0x7777, 0x8888));
        PngPixelBuffer framePixels = CreateBuffer(PngPixelFormat.Rgba16, 2, 1, 16, CreateBigEndianSamples(
            0xAAAA, 0xBBBB, 0xCCCC, 0xFFFF,
            0xDDDD, 0xEEEE, 0xFFFF, 0x0000));

        PngPixelBuffer result = PngApngFrameCompositor.Compose(canvas, framePixels, 0, 0, PngApngBlendOperation.Over);

        AssertBuffer(result, PngPixelFormat.Rgba16, 2, 1, 16, CreateBigEndianSamples(
            0xAAAA, 0xBBBB, 0xCCCC, 0xFFFF,
            0x5555, 0x6666, 0x7777, 0x8888));
    }

    [Test]
    public void Compose_OverBlendsGrayscaleAlpha8()
    {
        PngPixelBuffer canvas = CreateBuffer(PngPixelFormat.GrayscaleAlpha8, 1, 1, 2, new byte[] { 40, 255 });
        PngPixelBuffer framePixels = CreateBuffer(PngPixelFormat.GrayscaleAlpha8, 1, 1, 2, new byte[] { 200, 128 });

        PngPixelBuffer result = PngApngFrameCompositor.Compose(canvas, framePixels, 0, 0, PngApngBlendOperation.Over);

        AssertBuffer(result, PngPixelFormat.GrayscaleAlpha8, 1, 1, 2, new byte[] { 120, 255 });
    }

    [Test]
    public void Compose_FrameOverloadUsesFramePlacementAndBlendOperation()
    {
        PngPixelBuffer canvas = CreateBuffer(PngPixelFormat.Rgba8, 2, 1, 8, new byte[]
        {
            1, 2, 3, 255, 4, 5, 6, 255
        });
        PngFrame frame = new PngFrame();
        frame.Width = 1;
        frame.Height = 1;
        frame.XOffset = 1;
        frame.YOffset = 0;
        frame.BlendOperation = PngApngBlendOperation.Source;
        frame.Pixels = CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 7, 8, 9, 10 });

        PngPixelBuffer result = PngApngFrameCompositor.Compose(canvas, frame);

        AssertBuffer(result, PngPixelFormat.Rgba8, 2, 1, 8, new byte[]
        {
            1, 2, 3, 255, 7, 8, 9, 10
        });
    }

    [Test]
    public void Compose_ThenDisposePreviousRestoresSavedCanvas()
    {
        PngPixelBuffer previousCanvas = CreateBuffer(PngPixelFormat.Rgba8, 2, 1, 8, new byte[]
        {
            10, 20, 30, 255, 40, 50, 60, 255
        });
        PngFrame frame = new PngFrame();
        frame.Width = 1;
        frame.Height = 1;
        frame.XOffset = 0;
        frame.YOffset = 0;
        frame.BlendOperation = PngApngBlendOperation.Source;
        frame.DisposeOperation = PngApngDisposeOperation.Previous;
        frame.Pixels = CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 200, 100, 50, 128 });

        PngPixelBuffer composed = PngApngFrameCompositor.Compose(previousCanvas, frame);
        PngPixelBuffer restored = PngApngFrameDisposer.Apply(composed, frame, previousCanvas);

        AssertBuffer(composed, PngPixelFormat.Rgba8, 2, 1, 8, new byte[]
        {
            200, 100, 50, 128, 40, 50, 60, 255
        });
        AssertBuffer(restored, PngPixelFormat.Rgba8, 2, 1, 8, new byte[]
        {
            10, 20, 30, 255, 40, 50, 60, 255
        });
        Assert.That(restored.Data, Is.Not.SameAs(previousCanvas.Data));
    }

    [Test]
    public void Compose_RejectsInvalidInputs()
    {
        PngPixelBuffer canvas = CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 0, 0, 0, 255 });
        PngPixelBuffer framePixels = CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 255, 0, 0, 128 });
        PngPixelBuffer hugeCanvas = CreateBuffer(PngPixelFormat.Rgba8, int.MaxValue, 1, int.MaxValue, new byte[0]);
        PngFrame nullPixelsFrame = new PngFrame();
        nullPixelsFrame.Pixels = null!;

        Assert.Throws<ArgumentNullException>(() => PngApngFrameCompositor.Compose(null!, framePixels, 0, 0, PngApngBlendOperation.Source));
        Assert.Throws<ArgumentNullException>(() => PngApngFrameCompositor.Compose(canvas, null!, 0, 0, PngApngBlendOperation.Source));
        Assert.Throws<ArgumentNullException>(() => PngApngFrameCompositor.Compose(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, null!), framePixels, 0, 0, PngApngBlendOperation.Source));
        Assert.Throws<ArgumentNullException>(() => PngApngFrameCompositor.Compose(canvas, CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, null!), 0, 0, PngApngBlendOperation.Source));
        Assert.Throws<ArgumentNullException>(() => PngApngFrameCompositor.Compose(canvas, (PngFrame)null!));
        Assert.Throws<ArgumentNullException>(() => PngApngFrameCompositor.Compose(canvas, nullPixelsFrame));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameCompositor.Compose(CreateBuffer(PngPixelFormat.Rgb8, 1, 1, 3, new byte[] { 1, 2, 3 }), framePixels, 0, 0, PngApngBlendOperation.Source));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameCompositor.Compose(canvas, CreateBuffer(PngPixelFormat.GrayscaleAlpha8, 1, 1, 2, new byte[] { 1, 2 }), 0, 0, PngApngBlendOperation.Source));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameCompositor.Compose(CreateBuffer(PngPixelFormat.Rgba8, 0, 1, 0, new byte[0]), framePixels, 0, 0, PngApngBlendOperation.Source));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameCompositor.Compose(canvas, CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 3, new byte[] { 1, 2, 3, 4 }), 0, 0, PngApngBlendOperation.Source));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameCompositor.Compose(canvas, CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 1, 2, 3 }), 0, 0, PngApngBlendOperation.Source));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameCompositor.Compose(canvas, framePixels, -1, 0, PngApngBlendOperation.Source));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameCompositor.Compose(canvas, framePixels, 0, -1, PngApngBlendOperation.Source));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameCompositor.Compose(canvas, framePixels, 1, 0, PngApngBlendOperation.Source));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameCompositor.Compose(canvas, framePixels, 0, 1, PngApngBlendOperation.Source));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameCompositor.Compose(canvas, framePixels, 0, 0, (PngApngBlendOperation)9));
        Assert.Throws<OverflowException>(() => PngApngFrameCompositor.Compose(hugeCanvas, framePixels, 0, 0, PngApngBlendOperation.Source));
    }

    static void AssertBuffer(PngPixelBuffer buffer, PngPixelFormat format, int width, int height, int stride, byte[] data)
    {
        Assert.That(buffer.Format, Is.EqualTo(format));
        Assert.That(buffer.Width, Is.EqualTo(width));
        Assert.That(buffer.Height, Is.EqualTo(height));
        Assert.That(buffer.Stride, Is.EqualTo(stride));
        Assert.That(buffer.Data, Is.EqualTo(data));
    }

    static PngPixelBuffer CreateBuffer(PngPixelFormat format, int width, int height, int stride, byte[] data)
    {
        PngPixelBuffer buffer = new PngPixelBuffer();
        buffer.Format = format;
        buffer.Width = width;
        buffer.Height = height;
        buffer.Stride = stride;
        buffer.Data = data;
        return buffer;
    }

    static byte[] CreateBigEndianSamples(int s0, int s1, int s2, int s3, int s4, int s5, int s6, int s7)
    {
        byte[] data = new byte[16];
        WriteSample(data, 0, s0);
        WriteSample(data, 2, s1);
        WriteSample(data, 4, s2);
        WriteSample(data, 6, s3);
        WriteSample(data, 8, s4);
        WriteSample(data, 10, s5);
        WriteSample(data, 12, s6);
        WriteSample(data, 14, s7);
        return data;
    }

    static void WriteSample(byte[] data, int offset, int value)
    {
        data[offset] = (byte)(value / 256);
        data[offset + 1] = (byte)(value % 256);
    }
}
