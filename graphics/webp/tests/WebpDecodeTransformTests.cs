using webp;

namespace webp.Tests;

[TestFixture]
public class WebpDecodeTransformTests
{
    [Test]
    public void StillDecodeCropsPixelsAndReportsAppliedSourceRectangle()
    {
        WebpCodec codec = new WebpCodec();
        byte[] data = EncodeStill(codec, NewGridBuffer(4, 2));
        WebpDecodeOptions options = new WebpDecodeOptions();
        options.CropX = 1;
        options.CropY = 0;
        options.CropWidth = 2;
        options.CropHeight = 2;

        WebpDecodeResult result = codec.Decode(data, options);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Image.CanvasWidth, Is.EqualTo(2));
        Assert.That(result.Image.CanvasHeight, Is.EqualTo(2));
        Assert.That(result.Image.Frames[0].X, Is.EqualTo(0));
        Assert.That(result.Image.Frames[0].Y, Is.EqualTo(0));
        Assert.That(result.Image.Frames[0].Width, Is.EqualTo(2));
        Assert.That(result.Image.Frames[0].Height, Is.EqualTo(2));
        Assert.That(result.Image.Frames[0].Pixels!.Rgba, Is.EqualTo(new byte[]
        {
            1, 0, 0, 255, 2, 0, 0, 255,
            11, 0, 0, 255, 12, 0, 0, 255
        }));
        Assert.That(result.Image.Frames[0].EncodedBitstream, Is.Null);
        AssertTransform(result.Image.Transform, 4, 2, 1, 0, 2, 2, 2, 2, WebpAspectRatioMode.Preserve);
    }

    [Test]
    public void StillDecodeSupportsStretchAndPreservedAspectRatioScaling()
    {
        WebpCodec codec = new WebpCodec();
        byte[] data = EncodeStill(codec, NewGridBuffer(4, 2));
        WebpDecodeOptions stretchOptions = new WebpDecodeOptions();
        stretchOptions.TargetWidth = 2;
        stretchOptions.TargetHeight = 2;
        stretchOptions.AspectRatioMode = WebpAspectRatioMode.Stretch;
        WebpDecodeOptions preserveOptions = new WebpDecodeOptions();
        preserveOptions.TargetWidth = 2;
        preserveOptions.TargetHeight = 2;
        preserveOptions.AspectRatioMode = WebpAspectRatioMode.Preserve;

        WebpDecodeResult stretched = codec.Decode(data, stretchOptions);
        WebpDecodeResult preserved = codec.Decode(data, preserveOptions);

        Assert.That(stretched.Image.CanvasWidth, Is.EqualTo(2));
        Assert.That(stretched.Image.CanvasHeight, Is.EqualTo(2));
        Assert.That(stretched.Image.Frames[0].Pixels!.Rgba, Is.EqualTo(new byte[]
        {
            0, 0, 0, 255, 2, 0, 0, 255,
            10, 0, 0, 255, 12, 0, 0, 255
        }));
        Assert.That(preserved.Image.CanvasWidth, Is.EqualTo(2));
        Assert.That(preserved.Image.CanvasHeight, Is.EqualTo(1));
        Assert.That(preserved.Image.Frames[0].Pixels!.Rgba, Is.EqualTo(new byte[]
        {
            0, 0, 0, 255, 2, 0, 0, 255
        }));
        Assert.That(stretched.Image.Transform.AspectRatioMode, Is.EqualTo(WebpAspectRatioMode.Stretch));
        Assert.That(preserved.Image.Transform.AspectRatioMode, Is.EqualTo(WebpAspectRatioMode.Preserve));
    }

    [Test]
    public void IndependentAnimationCroppingClipsTranslatesAndScalesFrameRectangles()
    {
        WebpCodec codec = new WebpCodec();
        byte[] data = EncodeAnimation(codec);
        WebpDecodeOptions options = NewAnimationTransformOptions();

        WebpDecodeResult result = codec.Decode(data, options);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Image.CanvasWidth, Is.EqualTo(6));
        Assert.That(result.Image.CanvasHeight, Is.EqualTo(4));
        Assert.That(result.Image.Frames, Has.Count.EqualTo(2));
        AssertFrame(result.Image.Frames[0], 0, 0, 2, 4);
        AssertFrame(result.Image.Frames[1], 2, 0, 4, 4);
        Assert.That(ReadRedRow(result.Image.Frames[0].Pixels!), Is.EqualTo(new byte[] { 20, 20 }));
        Assert.That(ReadRedRow(result.Image.Frames[1].Pixels!), Is.EqualTo(new byte[] { 30, 30, 40, 40 }));
        Assert.That(result.Image.Frames[0].EncodedBitstream, Is.Null);
        Assert.That(result.Image.Frames[1].EncodedBitstream, Is.Null);
        AssertTransform(result.Image.Transform, 4, 2, 1, 0, 3, 2, 6, 4, WebpAspectRatioMode.Stretch);
    }

    [Test]
    public void CompositedAnimationCroppingReturnsFullOutputCanvasSnapshots()
    {
        WebpCodec codec = new WebpCodec();
        byte[] data = EncodeAnimation(codec);
        WebpDecodeOptions options = new WebpDecodeOptions();
        options.AnimationDecodeMode = WebpAnimationDecodeMode.CompositedFrames;
        options.CropX = 1;
        options.CropY = 0;
        options.CropWidth = 3;
        options.CropHeight = 2;

        WebpDecodeResult result = codec.Decode(data, options);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Image.CanvasWidth, Is.EqualTo(3));
        Assert.That(result.Image.CanvasHeight, Is.EqualTo(2));
        AssertFrame(result.Image.Frames[0], 0, 0, 3, 2);
        AssertFrame(result.Image.Frames[1], 0, 0, 3, 2);
        Assert.That(ReadRedRow(result.Image.Frames[0].Pixels!), Is.EqualTo(new byte[] { 20, 0, 0 }));
        Assert.That(ReadRedRow(result.Image.Frames[1].Pixels!), Is.EqualTo(new byte[] { 20, 30, 40 }));
    }

    [Test]
    public void IndependentCropRetainsNonIntersectingFramesAsTimingEntries()
    {
        WebpCodec codec = new WebpCodec();
        byte[] data = EncodeAnimation(codec);
        WebpDecodeOptions options = new WebpDecodeOptions();
        options.CropX = 2;
        options.CropY = 0;
        options.CropWidth = 2;
        options.CropHeight = 2;

        WebpDecodeResult result = codec.Decode(data, options);

        Assert.That(result.Image.Frames, Has.Count.EqualTo(2));
        Assert.That(result.Image.Frames[0].DurationMilliseconds, Is.EqualTo(25));
        Assert.That(result.Image.Frames[0].Width, Is.EqualTo(0));
        Assert.That(result.Image.Frames[0].Height, Is.EqualTo(0));
        Assert.That(result.Image.Frames[0].Pixels, Is.Not.Null);
        Assert.That(result.Image.Frames[0].Pixels!.Rgba, Is.Empty);
        AssertFrame(result.Image.Frames[1], 0, 0, 2, 2);
    }

    [Test]
    public void DecodeRejectsInvalidOrUnsafeTransformRequests()
    {
        WebpCodec codec = new WebpCodec();
        byte[] data = EncodeStill(codec, NewGridBuffer(4, 2));
        WebpDecodeOptions outsideCrop = new WebpDecodeOptions();
        outsideCrop.CropX = 3;
        outsideCrop.CropWidth = 2;
        outsideCrop.CropHeight = 2;
        WebpDecodeOptions incompleteStretch = new WebpDecodeOptions();
        incompleteStretch.TargetWidth = 2;
        incompleteStretch.AspectRatioMode = WebpAspectRatioMode.Stretch;
        WebpDecodeOptions identifyCrop = new WebpDecodeOptions();
        identifyCrop.IdentifyOnly = true;
        identifyCrop.CropWidth = 1;
        identifyCrop.CropHeight = 1;
        WebpDecodeOptions unsafeTarget = new WebpDecodeOptions();
        unsafeTarget.TargetWidth = 10;
        unsafeTarget.TargetHeight = 10;
        unsafeTarget.AspectRatioMode = WebpAspectRatioMode.Stretch;
        unsafeTarget.Limits.MaxCanvasPixels = 8;

        Assert.Throws<ArgumentException>(() => codec.Decode(data, outsideCrop));
        Assert.Throws<ArgumentException>(() => codec.Decode(data, incompleteStretch));
        Assert.Throws<ArgumentException>(() => codec.Decode(data, identifyCrop));
        Assert.Throws<ArgumentException>(() => codec.Decode(data, unsafeTarget));
    }

    static WebpDecodeOptions NewAnimationTransformOptions()
    {
        WebpDecodeOptions options = new WebpDecodeOptions();
        options.CropX = 1;
        options.CropY = 0;
        options.CropWidth = 3;
        options.CropHeight = 2;
        options.TargetWidth = 6;
        options.TargetHeight = 4;
        options.AspectRatioMode = WebpAspectRatioMode.Stretch;
        return options;
    }

    static void AssertFrame(WebpFrame frame, int x, int y, int width, int height)
    {
        Assert.That(frame.X, Is.EqualTo(x));
        Assert.That(frame.Y, Is.EqualTo(y));
        Assert.That(frame.Width, Is.EqualTo(width));
        Assert.That(frame.Height, Is.EqualTo(height));
        Assert.That(frame.Pixels, Is.Not.Null);
        Assert.That(frame.Pixels!.Width, Is.EqualTo(width));
        Assert.That(frame.Pixels!.Height, Is.EqualTo(height));
    }

    static void AssertTransform(WebpDecodeTransform transform, int canvasWidth, int canvasHeight, int sourceX, int sourceY, int sourceWidth, int sourceHeight, int outputWidth, int outputHeight, WebpAspectRatioMode mode)
    {
        Assert.That(transform.SourceCanvasWidth, Is.EqualTo(canvasWidth));
        Assert.That(transform.SourceCanvasHeight, Is.EqualTo(canvasHeight));
        Assert.That(transform.SourceX, Is.EqualTo(sourceX));
        Assert.That(transform.SourceY, Is.EqualTo(sourceY));
        Assert.That(transform.SourceWidth, Is.EqualTo(sourceWidth));
        Assert.That(transform.SourceHeight, Is.EqualTo(sourceHeight));
        Assert.That(transform.OutputWidth, Is.EqualTo(outputWidth));
        Assert.That(transform.OutputHeight, Is.EqualTo(outputHeight));
        Assert.That(transform.AspectRatioMode, Is.EqualTo(mode));
    }

    static byte[] ReadRedRow(WebpPixelBuffer buffer)
    {
        byte[] red = new byte[buffer.Width];
        int x = 0;
        while (x < buffer.Width)
        {
            red[x] = buffer.Rgba[x * 4];
            x = x + 1;
        }

        return red;
    }

    static byte[] EncodeStill(WebpCodec codec, WebpPixelBuffer buffer)
    {
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Mode = WebpEncodeMode.Lossless;
        return codec.Encode(buffer, options);
    }

    static byte[] EncodeAnimation(WebpCodec codec)
    {
        WebpImage image = new WebpImage();
        image.CanvasWidth = 4;
        image.CanvasHeight = 2;
        image.HasAnimation = true;
        image.BackgroundColorBgra = 0xFF000000U;
        image.Frames.Add(NewFrame(0, NewFrameBuffer(10, 20)));
        image.Frames.Add(NewFrame(2, NewFrameBuffer(30, 40)));
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Mode = WebpEncodeMode.Lossless;
        return codec.EncodeImage(image, options);
    }

    static WebpFrame NewFrame(int x, WebpPixelBuffer pixels)
    {
        WebpFrame frame = new WebpFrame();
        frame.X = x;
        frame.Y = 0;
        frame.Width = 2;
        frame.Height = 2;
        frame.DurationMilliseconds = 25;
        frame.BlendMode = WebpFrameBlendMode.Replace;
        frame.DisposeMode = WebpFrameDisposeMode.DoNotDispose;
        frame.BitstreamKind = WebpBitstreamKind.Lossless;
        frame.Pixels = pixels;
        return frame;
    }

    static WebpPixelBuffer NewFrameBuffer(byte leftRed, byte rightRed)
    {
        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(2, 2);
        int y = 0;
        while (y < 2)
        {
            SetPixel(buffer, 0, y, leftRed);
            SetPixel(buffer, 1, y, rightRed);
            y = y + 1;
        }

        return buffer;
    }

    static WebpPixelBuffer NewGridBuffer(int width, int height)
    {
        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(width, height);
        int y = 0;
        while (y < height)
        {
            int x = 0;
            while (x < width)
            {
                SetPixel(buffer, x, y, (byte)(y * 10 + x));
                x = x + 1;
            }

            y = y + 1;
        }

        return buffer;
    }

    static void SetPixel(WebpPixelBuffer buffer, int x, int y, byte red)
    {
        int offset = y * buffer.Stride + x * 4;
        buffer.Rgba[offset] = red;
        buffer.Rgba[offset + 1] = 0;
        buffer.Rgba[offset + 2] = 0;
        buffer.Rgba[offset + 3] = 255;
    }
}
