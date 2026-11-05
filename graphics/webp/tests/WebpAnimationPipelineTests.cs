using webp;

namespace webp.Tests;

[TestFixture]
public class WebpAnimationPipelineTests
{
    [Test]
    public void AnimatedEncodeParseExtractAndDecodePipelinePreservesFrameSemantics()
    {
        WebpPixelBuffer firstPixels = NewBuffer(1, 1, 255, 0, 0, 255);
        WebpPixelBuffer secondPixels = NewBuffer(1, 1, 0, 0, 255, 255);
        WebpImage image = NewAnimatedImage(firstPixels, secondPixels);
        WebpEncodeOptions encodeOptions = new WebpEncodeOptions();
        encodeOptions.Mode = WebpEncodeMode.Lossless;

        byte[] data = WebpEncoder.Encode(image, encodeOptions);
        WebpExtendedContainerParseResult parsed = WebpExtendedContainerParser.Parse(data, WebpStrictness.Strict);
        List<WebpFrame> extracted = WebpMux.ExtractFrames(data);
        WebpDecodeResult independent = WebpDecoder.Decode(data);
        WebpDecodeOptions compositedOptions = new WebpDecodeOptions();
        compositedOptions.AnimationDecodeMode = WebpAnimationDecodeMode.CompositedFrames;
        WebpDecodeResult composited = WebpDecoder.Decode(data, compositedOptions);

        Assert.That(parsed.IsValid, Is.True);
        Assert.That(parsed.Vp8X.HasAnimation, Is.True);
        Assert.That(parsed.AnimationLoopCount, Is.EqualTo(4));
        Assert.That(parsed.AnimationBackgroundColorBgra, Is.EqualTo(0xFF010203U));
        Assert.That(parsed.Frames, Has.Count.EqualTo(2));
        AssertParsedFrame(parsed.Frames[0], 0, 0, 1, 1, 25, WebpFrameBlendMode.Replace, WebpFrameDisposeMode.DoNotDispose);
        AssertParsedFrame(parsed.Frames[1], 2, 0, 1, 1, 40, WebpFrameBlendMode.Replace, WebpFrameDisposeMode.DoNotDispose);

        Assert.That(extracted, Has.Count.EqualTo(2));
        AssertParsedFrame(extracted[0], 0, 0, 1, 1, 25, WebpFrameBlendMode.Replace, WebpFrameDisposeMode.DoNotDispose);
        AssertParsedFrame(extracted[1], 2, 0, 1, 1, 40, WebpFrameBlendMode.Replace, WebpFrameDisposeMode.DoNotDispose);
        Assert.That(extracted[0].EncodedBitstream, Is.EqualTo(parsed.Frames[0].EncodedBitstream));
        Assert.That(extracted[1].EncodedBitstream, Is.EqualTo(parsed.Frames[1].EncodedBitstream));

        Assert.That(independent.IsValid, Is.True);
        Assert.That(independent.Image.HasAnimation, Is.True);
        Assert.That(independent.Image.Frames, Has.Count.EqualTo(2));
        Assert.That(independent.Image.Frames[0].Pixels!.Rgba, Is.EqualTo(firstPixels.Rgba));
        Assert.That(independent.Image.Frames[1].Pixels!.Rgba, Is.EqualTo(secondPixels.Rgba));

        Assert.That(composited.IsValid, Is.True);
        Assert.That(composited.Image.Frames, Has.Count.EqualTo(2));
        Assert.That(composited.Image.Frames[0].X, Is.EqualTo(0));
        Assert.That(composited.Image.Frames[0].Y, Is.EqualTo(0));
        Assert.That(composited.Image.Frames[0].Width, Is.EqualTo(4));
        Assert.That(composited.Image.Frames[0].Height, Is.EqualTo(1));
        Assert.That(composited.Image.Frames[0].Pixels!.Rgba, Is.EqualTo(new byte[]
        {
            255, 0, 0, 255,
            1, 2, 3, 255,
            1, 2, 3, 255,
            1, 2, 3, 255
        }));
        Assert.That(composited.Image.Frames[1].Pixels!.Rgba, Is.EqualTo(new byte[]
        {
            255, 0, 0, 255,
            1, 2, 3, 255,
            0, 0, 255, 255,
            1, 2, 3, 255
        }));
    }

    [Test]
    public void AnimationComposerPipelineHandlesDisposeToBackgroundBeforeNextFrame()
    {
        WebpFrame first = NewFrame(0, 0, 1, 1, NewBuffer(1, 1, 255, 0, 0, 255));
        first.DisposeMode = WebpFrameDisposeMode.DisposeToBackground;
        WebpFrame second = NewFrame(1, 0, 1, 1, NewBuffer(1, 1, 0, 255, 0, 255));
        second.BlendMode = WebpFrameBlendMode.Replace;
        List<WebpFrame> frames = new List<WebpFrame>();
        frames.Add(first);
        frames.Add(second);

        List<WebpFrame> composited = WebpAnimationComposer.ComposeFrames(frames, 2, 1, 0xFF010203U);

        Assert.That(composited, Has.Count.EqualTo(2));
        Assert.That(composited[0].Pixels!.Rgba, Is.EqualTo(new byte[]
        {
            255, 0, 0, 255,
            1, 2, 3, 255
        }));
        Assert.That(composited[1].Pixels!.Rgba, Is.EqualTo(new byte[]
        {
            1, 2, 3, 255,
            0, 255, 0, 255
        }));
    }

    static void AssertParsedFrame(WebpFrame frame, int x, int y, int width, int height, int duration, WebpFrameBlendMode blendMode, WebpFrameDisposeMode disposeMode)
    {
        Assert.That(frame.X, Is.EqualTo(x));
        Assert.That(frame.Y, Is.EqualTo(y));
        Assert.That(frame.Width, Is.EqualTo(width));
        Assert.That(frame.Height, Is.EqualTo(height));
        Assert.That(frame.DurationMilliseconds, Is.EqualTo(duration));
        Assert.That(frame.BlendMode, Is.EqualTo(blendMode));
        Assert.That(frame.DisposeMode, Is.EqualTo(disposeMode));
        Assert.That(frame.BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossless));
        Assert.That(frame.EncodedBitstream, Is.Not.Null);
    }

    static WebpImage NewAnimatedImage(WebpPixelBuffer firstPixels, WebpPixelBuffer secondPixels)
    {
        WebpImage image = new WebpImage();
        image.CanvasWidth = 4;
        image.CanvasHeight = 1;
        image.HasAnimation = true;
        image.LoopCount = 4;
        image.BackgroundColorBgra = 0xFF010203U;
        image.Frames.Add(NewFrame(0, 0, 1, 1, firstPixels));
        image.Frames.Add(NewFrame(2, 0, 1, 1, secondPixels));
        image.Frames[0].DurationMilliseconds = 25;
        image.Frames[1].DurationMilliseconds = 40;
        return image;
    }

    static WebpFrame NewFrame(int x, int y, int width, int height, WebpPixelBuffer pixels)
    {
        WebpFrame frame = new WebpFrame();
        frame.X = x;
        frame.Y = y;
        frame.Width = width;
        frame.Height = height;
        frame.BlendMode = WebpFrameBlendMode.Replace;
        frame.DisposeMode = WebpFrameDisposeMode.DoNotDispose;
        frame.BitstreamKind = WebpBitstreamKind.Lossless;
        frame.Pixels = pixels;
        return frame;
    }

    static WebpPixelBuffer NewBuffer(int width, int height, byte red, byte green, byte blue, byte alpha)
    {
        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(width, height);
        int pixel = 0;
        while (pixel < buffer.Rgba.Length)
        {
            buffer.Rgba[pixel] = red;
            buffer.Rgba[pixel + 1] = green;
            buffer.Rgba[pixel + 2] = blue;
            buffer.Rgba[pixel + 3] = alpha;
            pixel = pixel + 4;
        }

        return buffer;
    }
}
