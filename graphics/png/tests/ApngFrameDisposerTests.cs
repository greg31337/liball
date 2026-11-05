using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class ApngFrameDisposerTests
{
    [Test]
    public void ApplyNone_ReturnsIndependentUnchangedCanvas()
    {
        PngPixelBuffer canvas = CreateBuffer(PngPixelFormat.Rgba8, 2, 2, 12, new byte[]
        {
            1, 2, 3, 4, 5, 6, 7, 8, 99, 99, 99, 99,
            10, 11, 12, 13, 14, 15, 16, 17, 88, 88, 88, 88
        });

        PngPixelBuffer result = PngApngFrameDisposer.ApplyNone(canvas);

        AssertBuffer(result, PngPixelFormat.Rgba8, 2, 2, 12, new byte[]
        {
            1, 2, 3, 4, 5, 6, 7, 8, 99, 99, 99, 99,
            10, 11, 12, 13, 14, 15, 16, 17, 88, 88, 88, 88
        });
        Assert.That(result.Data, Is.Not.SameAs(canvas.Data));

        result.Data[0] = 200;
        Assert.That(canvas.Data[0], Is.EqualTo(1));
    }

    [Test]
    public void Apply_UsesFrameDisposeOperationNone()
    {
        PngPixelBuffer canvas = CreateBuffer(PngPixelFormat.Rgb8, 1, 1, 3, new byte[] { 20, 30, 40 });
        PngFrame frame = new PngFrame();
        frame.DisposeOperation = PngApngDisposeOperation.None;

        PngPixelBuffer result = PngApngFrameDisposer.Apply(canvas, frame);

        AssertBuffer(result, PngPixelFormat.Rgb8, 1, 1, 3, new byte[] { 20, 30, 40 });
    }

    [Test]
    public void ApplyNone_SupportsPackedPixelFormats()
    {
        PngPixelBuffer canvas = CreateBuffer(PngPixelFormat.Grayscale1, 8, 2, 2, new byte[]
        {
            0xAA, 0x99,
            0x55, 0x88
        });

        PngPixelBuffer result = PngApngFrameDisposer.Apply(canvas, PngApngDisposeOperation.None);

        AssertBuffer(result, PngPixelFormat.Grayscale1, 8, 2, 2, new byte[]
        {
            0xAA, 0x99,
            0x55, 0x88
        });
    }

    [Test]
    public void ApplyBackground_ClearsFrameRectangleToTransparentBlack()
    {
        PngPixelBuffer canvas = CreateBuffer(PngPixelFormat.Rgba8, 3, 2, 16, new byte[]
        {
            1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 99, 99, 99, 99,
            13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 88, 88, 88, 88
        });
        PngFrame frame = CreateFrame(2, 1, 1, 1, PngApngDisposeOperation.Background);

        PngPixelBuffer result = PngApngFrameDisposer.Apply(canvas, frame);

        AssertBuffer(result, PngPixelFormat.Rgba8, 3, 2, 16, new byte[]
        {
            1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 99, 99, 99, 99,
            13, 14, 15, 16, 0, 0, 0, 0, 0, 0, 0, 0, 88, 88, 88, 88
        });
        Assert.That(result.Data, Is.Not.SameAs(canvas.Data));
        Assert.That(canvas.Data[20], Is.EqualTo(17));
    }

    [Test]
    public void ApplyBackground_ClearsGrayscaleAlpha16Rectangle()
    {
        PngPixelBuffer canvas = CreateBuffer(PngPixelFormat.GrayscaleAlpha16, 2, 1, 8, new byte[]
        {
            0x11, 0x11, 0x22, 0x22, 0x33, 0x33, 0x44, 0x44
        });
        PngFrame frame = CreateFrame(1, 1, 1, 0, PngApngDisposeOperation.Background);

        PngPixelBuffer result = PngApngFrameDisposer.ApplyBackground(canvas, frame);

        AssertBuffer(result, PngPixelFormat.GrayscaleAlpha16, 2, 1, 8, new byte[]
        {
            0x11, 0x11, 0x22, 0x22, 0, 0, 0, 0
        });
    }

    [Test]
    public void ApplyPrevious_RestoresSavedCanvas()
    {
        PngPixelBuffer canvas = CreateBuffer(PngPixelFormat.Rgba8, 2, 1, 8, new byte[]
        {
            90, 91, 92, 93, 94, 95, 96, 97
        });
        PngPixelBuffer previousCanvas = CreateBuffer(PngPixelFormat.Rgba8, 2, 1, 12, new byte[]
        {
            1, 2, 3, 4, 5, 6, 7, 8, 99, 99, 99, 99
        });

        PngPixelBuffer result = PngApngFrameDisposer.ApplyPrevious(canvas, previousCanvas);

        AssertBuffer(result, PngPixelFormat.Rgba8, 2, 1, 12, new byte[]
        {
            1, 2, 3, 4, 5, 6, 7, 8, 99, 99, 99, 99
        });
        Assert.That(result.Data, Is.Not.SameAs(previousCanvas.Data));

        result.Data[0] = 200;
        Assert.That(previousCanvas.Data[0], Is.EqualTo(1));
    }

    [Test]
    public void Apply_WithPreviousCanvasUsesFrameDisposeOperationPrevious()
    {
        PngPixelBuffer canvas = CreateBuffer(PngPixelFormat.Grayscale1, 8, 1, 1, new byte[] { 0xAA });
        PngPixelBuffer previousCanvas = CreateBuffer(PngPixelFormat.Grayscale1, 8, 1, 1, new byte[] { 0x55 });
        PngFrame frame = CreateFrame(8, 1, 0, 0, PngApngDisposeOperation.Previous);

        PngPixelBuffer result = PngApngFrameDisposer.Apply(canvas, frame, previousCanvas);

        AssertBuffer(result, PngPixelFormat.Grayscale1, 8, 1, 1, new byte[] { 0x55 });
    }

    [Test]
    public void Apply_RejectsInvalidInputs()
    {
        PngPixelBuffer canvas = CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 1, 2, 3, 4 });
        PngPixelBuffer previousCanvas = CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 5, 6, 7, 8 });
        PngFrame previousFrame = new PngFrame();
        previousFrame.DisposeOperation = PngApngDisposeOperation.Previous;
        PngFrame negativeX = CreateFrame(1, 1, -1, 0, PngApngDisposeOperation.Background);
        PngFrame negativeY = CreateFrame(1, 1, 0, -1, PngApngDisposeOperation.Background);
        PngFrame zeroWidth = CreateFrame(0, 1, 0, 0, PngApngDisposeOperation.Background);
        PngFrame zeroHeight = CreateFrame(1, 0, 0, 0, PngApngDisposeOperation.Background);
        PngFrame tooFarRight = CreateFrame(1, 1, 1, 0, PngApngDisposeOperation.Background);
        PngFrame tooFarDown = CreateFrame(1, 1, 0, 1, PngApngDisposeOperation.Background);

        Assert.Throws<ArgumentNullException>(() => PngApngFrameDisposer.Apply(null!, PngApngDisposeOperation.None));
        Assert.Throws<ArgumentNullException>(() => PngApngFrameDisposer.Apply(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, null!), PngApngDisposeOperation.None));
        Assert.Throws<ArgumentNullException>(() => PngApngFrameDisposer.Apply(canvas, null!));
        Assert.Throws<ArgumentNullException>(() => PngApngFrameDisposer.ApplyNone(null!));
        Assert.Throws<ArgumentNullException>(() => PngApngFrameDisposer.ApplyNone(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, null!)));
        Assert.Throws<ArgumentNullException>(() => PngApngFrameDisposer.ApplyBackground(null!, CreateFrame(1, 1, 0, 0, PngApngDisposeOperation.Background)));
        Assert.Throws<ArgumentNullException>(() => PngApngFrameDisposer.ApplyBackground(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, null!), CreateFrame(1, 1, 0, 0, PngApngDisposeOperation.Background)));
        Assert.Throws<ArgumentNullException>(() => PngApngFrameDisposer.ApplyBackground(canvas, null!));
        Assert.Throws<ArgumentNullException>(() => PngApngFrameDisposer.Apply(canvas, null!, previousCanvas));
        Assert.Throws<ArgumentNullException>(() => PngApngFrameDisposer.Apply(canvas, previousFrame, null!));
        Assert.Throws<ArgumentNullException>(() => PngApngFrameDisposer.ApplyPrevious(null!, previousCanvas));
        Assert.Throws<ArgumentNullException>(() => PngApngFrameDisposer.ApplyPrevious(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, null!), previousCanvas));
        Assert.Throws<ArgumentNullException>(() => PngApngFrameDisposer.ApplyPrevious(canvas, null!));
        Assert.Throws<ArgumentNullException>(() => PngApngFrameDisposer.ApplyPrevious(canvas, CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, null!)));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameDisposer.Apply(CreateBuffer(PngPixelFormat.Unknown, 1, 1, 1, new byte[] { 0 }), PngApngDisposeOperation.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameDisposer.Apply(CreateBuffer(PngPixelFormat.Rgba8, 0, 1, 0, new byte[0]), PngApngDisposeOperation.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameDisposer.Apply(CreateBuffer(PngPixelFormat.Rgba8, 1, 0, 0, new byte[0]), PngApngDisposeOperation.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameDisposer.Apply(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 3, new byte[] { 1, 2, 3, 4 }), PngApngDisposeOperation.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameDisposer.Apply(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 1, 2, 3 }), PngApngDisposeOperation.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameDisposer.Apply(canvas, PngApngDisposeOperation.Background));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameDisposer.Apply(canvas, PngApngDisposeOperation.Previous));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameDisposer.Apply(canvas, (PngApngDisposeOperation)9));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameDisposer.Apply(canvas, previousFrame));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameDisposer.ApplyBackground(CreateBuffer(PngPixelFormat.Rgb8, 1, 1, 3, new byte[] { 1, 2, 3 }), CreateFrame(1, 1, 0, 0, PngApngDisposeOperation.Background)));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameDisposer.ApplyBackground(canvas, negativeX));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameDisposer.ApplyBackground(canvas, negativeY));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameDisposer.ApplyBackground(canvas, zeroWidth));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameDisposer.ApplyBackground(canvas, zeroHeight));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameDisposer.ApplyBackground(canvas, tooFarRight));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameDisposer.ApplyBackground(canvas, tooFarDown));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameDisposer.ApplyPrevious(canvas, CreateBuffer(PngPixelFormat.Rgb8, 1, 1, 3, new byte[] { 1, 2, 3 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameDisposer.ApplyPrevious(canvas, CreateBuffer(PngPixelFormat.Rgba8, 2, 1, 8, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameDisposer.ApplyPrevious(canvas, CreateBuffer(PngPixelFormat.Rgba8, 1, 2, 4, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameDisposer.ApplyPrevious(canvas, CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 3, new byte[] { 1, 2, 3, 4 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameDisposer.ApplyPrevious(canvas, CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 1, 2, 3 })));
        Assert.Throws<OverflowException>(() => PngApngFrameDisposer.Apply(CreateBuffer(PngPixelFormat.Rgba16, int.MaxValue, 1, int.MaxValue, new byte[0]), PngApngDisposeOperation.None));
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

    static PngFrame CreateFrame(int width, int height, int xOffset, int yOffset, PngApngDisposeOperation disposeOperation)
    {
        PngFrame frame = new PngFrame();
        frame.Width = width;
        frame.Height = height;
        frame.XOffset = xOffset;
        frame.YOffset = yOffset;
        frame.DisposeOperation = disposeOperation;
        return frame;
    }
}
