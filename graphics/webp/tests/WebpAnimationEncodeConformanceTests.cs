using webp;

namespace webp.Tests;

[TestFixture]
public class WebpAnimationEncodeConformanceTests
{
    [Test]
    public void EncodeImage_MixedFrameModesPreserveAnimationSemanticsThroughBothDecodeModes()
    {
        WebpCodec codec = new WebpCodec();
        WebpImage source = NewMixedAnimation();
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Mode = WebpEncodeMode.Lossless;

        byte[] data = codec.EncodeImage(source, options);
        WebpExtendedContainerParseResult parsed = WebpExtendedContainerParser.Parse(data, WebpStrictness.Strict);
        WebpDecodeResult independent = codec.Decode(data, new WebpDecodeOptions());
        WebpDecodeOptions compositedOptions = new WebpDecodeOptions();
        compositedOptions.AnimationDecodeMode = WebpAnimationDecodeMode.CompositedFrames;
        WebpDecodeResult composited = codec.Decode(data, compositedOptions);
        List<WebpFrame> flattened = codec.FlattenAnimationFrames(independent.Image);

        Assert.That(parsed.IsValid, Is.True);
        Assert.That(parsed.Vp8X.HasAnimation, Is.True);
        Assert.That(parsed.Vp8X.HasAlpha, Is.True);
        Assert.That(parsed.AnimationLoopCount, Is.EqualTo(7));
        Assert.That(parsed.AnimationBackgroundColorBgra, Is.EqualTo(0xFF010203U));
        Assert.That(parsed.Frames, Has.Count.EqualTo(4));
        AssertParsedFrame(parsed.Frames[0], 0, 0, 10, WebpFrameBlendMode.Replace, WebpFrameDisposeMode.DisposeToBackground, WebpBitstreamKind.Lossless);
        AssertParsedFrame(parsed.Frames[1], 2, 0, 20, WebpFrameBlendMode.AlphaBlend, WebpFrameDisposeMode.DoNotDispose, WebpBitstreamKind.Lossy);
        AssertParsedFrame(parsed.Frames[2], 0, 2, 30, WebpFrameBlendMode.Replace, WebpFrameDisposeMode.DoNotDispose, WebpBitstreamKind.Lossless);
        AssertParsedFrame(parsed.Frames[3], 2, 2, 40, WebpFrameBlendMode.Replace, WebpFrameDisposeMode.DisposeToBackground, WebpBitstreamKind.Lossy);
        Assert.That(parsed.Frames[0].AlphaData, Is.Null);
        Assert.That(parsed.Frames[1].AlphaData![0] & WebpConstants.AlphaCompressionMask, Is.EqualTo(WebpConstants.AlphaCompressionLossless));
        Assert.That(parsed.Frames[2].AlphaData, Is.Null);
        Assert.That(parsed.Frames[3].AlphaData![0] & WebpConstants.AlphaCompressionMask, Is.EqualTo(WebpConstants.AlphaCompressionNone));

        Assert.That(independent.IsValid, Is.True);
        Assert.That(independent.Image.HasAnimation, Is.True);
        Assert.That(independent.Image.HasAlpha, Is.True);
        Assert.That(independent.Image.LoopCount, Is.EqualTo(7));
        Assert.That(independent.Image.BackgroundColorBgra, Is.EqualTo(0xFF010203U));
        Assert.That(independent.Image.Frames, Has.Count.EqualTo(4));
        Assert.That(ReadAlpha(independent.Image.Frames[0].Pixels!), Is.EqualTo(new byte[] { 128, 128, 128, 128 }));
        Assert.That(ReadAlpha(independent.Image.Frames[1].Pixels!), Is.EqualTo(new byte[] { 64, 64, 64, 64 }));
        Assert.That(independent.Image.Frames[2].Pixels!.Rgba[0], Is.EqualTo(120));
        Assert.That(ReadAlpha(independent.Image.Frames[3].Pixels!), Is.EqualTo(new byte[] { 160, 160, 160, 160 }));

        Assert.That(composited.IsValid, Is.True);
        Assert.That(composited.Image.Frames, Has.Count.EqualTo(flattened.Count));
        int index = 0;
        while (index < flattened.Count)
        {
            Assert.That(composited.Image.Frames[index].X, Is.EqualTo(0));
            Assert.That(composited.Image.Frames[index].Y, Is.EqualTo(0));
            Assert.That(composited.Image.Frames[index].Width, Is.EqualTo(4));
            Assert.That(composited.Image.Frames[index].Height, Is.EqualTo(4));
            Assert.That(composited.Image.Frames[index].DurationMilliseconds, Is.EqualTo(flattened[index].DurationMilliseconds));
            Assert.That(composited.Image.Frames[index].Pixels!.Rgba, Is.EqualTo(flattened[index].Pixels!.Rgba));
            index = index + 1;
        }

        Assert.That(source.Frames[0].EncodedBitstream, Is.Null);
        Assert.That(source.Frames[1].AlphaData, Is.Null);
    }

    [Test]
    public void EncodeImage_PreservesEncodedFramesAlphaAndOwnershipWithoutRecompression()
    {
        WebpCodec codec = new WebpCodec();
        WebpPixelBuffer losslessPixels = NewSolidPixels(1, 1, 10, 20, 30, 255);
        WebpPixelBuffer lossyPixels = NewSolidPixels(1, 1, 90, 80, 70, 255);
        byte[] losslessPayload = WebpVp8LEncoder.Encode(losslessPixels);
        byte[] lossyPayload = WebpVp8KeyFrameEncoder.Encode(lossyPixels);
        byte[] alphaPayload = WebpAlphaEncoder.EncodeUncompressed(new byte[] { 70 }, 1, 1);
        WebpImage image = new WebpImage();
        image.CanvasWidth = 4;
        image.CanvasHeight = 2;
        image.HasAnimation = true;
        image.LoopCount = 0;
        image.BackgroundColorBgra = 0x11223344U;
        WebpFrame lossless = NewEncodedFrame(0, 0, 15, WebpBitstreamKind.Unknown, losslessPayload, null);
        lossless.BlendMode = WebpFrameBlendMode.Replace;
        WebpFrame lossy = NewEncodedFrame(2, 0, 25, WebpBitstreamKind.Lossy, lossyPayload, alphaPayload);
        lossy.DisposeMode = WebpFrameDisposeMode.DisposeToBackground;
        image.Frames.Add(lossless);
        image.Frames.Add(lossy);
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Mode = WebpEncodeMode.NearLossless;
        options.NearLosslessQuality = 0;

        byte[] data = codec.EncodeImage(image, options);
        losslessPayload[0] = 0;
        lossyPayload[0] = 0;
        alphaPayload[1] = 0;
        WebpExtendedContainerParseResult parsed = WebpExtendedContainerParser.Parse(data, WebpStrictness.Strict);
        List<WebpFrame> extracted = codec.ExtractFrames(data);
        WebpDecodeResult decoded = codec.Decode(data, new WebpDecodeOptions());

        Assert.That(parsed.Frames[0].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossless));
        Assert.That(parsed.Frames[0].EncodedBitstream, Is.EqualTo(extracted[0].EncodedBitstream));
        Assert.That(parsed.Frames[0].Width, Is.EqualTo(1));
        Assert.That(parsed.Frames[0].Height, Is.EqualTo(1));
        Assert.That(parsed.Frames[1].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossy));
        Assert.That(parsed.Frames[1].EncodedBitstream, Is.EqualTo(extracted[1].EncodedBitstream));
        Assert.That(parsed.Frames[1].AlphaData, Is.EqualTo(extracted[1].AlphaData));
        Assert.That(decoded.IsValid, Is.True);
        Assert.That(decoded.Image.Frames[0].Pixels!.Rgba, Is.EqualTo(losslessPixels.Rgba));
        Assert.That(decoded.Image.Frames[1].Pixels!.Rgba[3], Is.EqualTo(70));
        Assert.That(decoded.Image.LoopCount, Is.EqualTo(0));
        Assert.That(decoded.Image.BackgroundColorBgra, Is.EqualTo(0x11223344U));
    }

    [Test]
    public void EncodeImage_DeterministicMixedAnimationIsStableAndDoesNotMutateTheModel()
    {
        WebpCodec codec = new WebpCodec();
        WebpImage image = NewMixedAnimation();
        byte[] firstFramePixels = WebpUnknownChunk.CopyBytes(image.Frames[0].Pixels!.Rgba);
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Mode = WebpEncodeMode.Lossless;
        options.Deterministic = true;

        byte[] first = codec.EncodeImage(image, options);
        byte[] second = codec.EncodeImage(image, options);
        byte[] third = codec.EncodeImage(image, options);

        Assert.That(second, Is.EqualTo(first));
        Assert.That(third, Is.EqualTo(first));
        Assert.That(image.Frames[0].Pixels!.Rgba, Is.EqualTo(firstFramePixels));
        Assert.That(image.Frames[0].EncodedBitstream, Is.Null);
        Assert.That(image.Frames[1].AlphaData, Is.Null);
        Assert.That(codec.Decode(first, new WebpDecodeOptions()).IsValid, Is.True);
    }

    static WebpImage NewMixedAnimation()
    {
        WebpImage image = new WebpImage();
        image.CanvasWidth = 4;
        image.CanvasHeight = 4;
        image.HasAnimation = true;
        image.LoopCount = 7;
        image.BackgroundColorBgra = 0xFF010203U;

        WebpFrame lossless = NewPixelFrame(0, 0, 10, 255, 0, 0, 128);
        lossless.BlendMode = WebpFrameBlendMode.Replace;
        lossless.DisposeMode = WebpFrameDisposeMode.DisposeToBackground;
        lossless.EncodeOptions = NewFrameOptions(WebpEncodeMode.Lossless);
        image.Frames.Add(lossless);

        WebpFrame compressedAlpha = NewPixelFrame(2, 0, 20, 0, 255, 0, 64);
        compressedAlpha.EncodeOptions = NewFrameOptions(WebpEncodeMode.Lossy);
        compressedAlpha.EncodeOptions.AlphaCompression = WebpAlphaCompressionMethod.Lossless;
        compressedAlpha.EncodeOptions.AlphaFilter = WebpAlphaFilterMode.Best;
        image.Frames.Add(compressedAlpha);

        WebpFrame nearLossless = NewPixelFrame(0, 2, 30, 123, 45, 67, 255);
        nearLossless.BlendMode = WebpFrameBlendMode.Replace;
        nearLossless.EncodeOptions = NewFrameOptions(WebpEncodeMode.NearLossless);
        nearLossless.EncodeOptions.NearLosslessQuality = 50;
        image.Frames.Add(nearLossless);

        WebpFrame uncompressedAlpha = NewPixelFrame(2, 2, 40, 0, 0, 255, 160);
        uncompressedAlpha.BlendMode = WebpFrameBlendMode.Replace;
        uncompressedAlpha.DisposeMode = WebpFrameDisposeMode.DisposeToBackground;
        uncompressedAlpha.EncodeOptions = NewFrameOptions(WebpEncodeMode.Lossy);
        uncompressedAlpha.EncodeOptions.AlphaCompression = WebpAlphaCompressionMethod.None;
        uncompressedAlpha.EncodeOptions.AlphaFilter = WebpAlphaFilterMode.None;
        image.Frames.Add(uncompressedAlpha);

        return image;
    }

    static WebpFrame NewPixelFrame(int x, int y, int duration, byte red, byte green, byte blue, byte alpha)
    {
        WebpFrame frame = new WebpFrame();
        frame.X = x;
        frame.Y = y;
        frame.Width = 2;
        frame.Height = 2;
        frame.DurationMilliseconds = duration;
        frame.Pixels = NewSolidPixels(2, 2, red, green, blue, alpha);
        return frame;
    }

    static WebpFrameEncodeOptions NewFrameOptions(WebpEncodeMode mode)
    {
        WebpFrameEncodeOptions options = new WebpFrameEncodeOptions();
        options.Mode = mode;
        return options;
    }

    static WebpFrame NewEncodedFrame(int x, int y, int duration, WebpBitstreamKind kind, byte[] payload, byte[]? alphaData)
    {
        WebpFrame frame = new WebpFrame();
        frame.X = x;
        frame.Y = y;
        frame.DurationMilliseconds = duration;
        frame.BitstreamKind = kind;
        frame.EncodedBitstream = payload;
        frame.AlphaData = alphaData;
        return frame;
    }

    static WebpPixelBuffer NewSolidPixels(int width, int height, byte red, byte green, byte blue, byte alpha)
    {
        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(width, height);
        int y = 0;
        while (y < height)
        {
            int x = 0;
            while (x < width)
            {
                int offset = (y * buffer.Stride) + (x * 4);
                buffer.Rgba[offset] = red;
                buffer.Rgba[offset + 1] = green;
                buffer.Rgba[offset + 2] = blue;
                buffer.Rgba[offset + 3] = alpha;
                x = x + 1;
            }

            y = y + 1;
        }

        return buffer;
    }

    static void AssertParsedFrame(
        WebpFrame frame,
        int x,
        int y,
        int duration,
        WebpFrameBlendMode blend,
        WebpFrameDisposeMode dispose,
        WebpBitstreamKind kind)
    {
        Assert.That(frame.X, Is.EqualTo(x));
        Assert.That(frame.Y, Is.EqualTo(y));
        Assert.That(frame.Width, Is.EqualTo(2));
        Assert.That(frame.Height, Is.EqualTo(2));
        Assert.That(frame.DurationMilliseconds, Is.EqualTo(duration));
        Assert.That(frame.BlendMode, Is.EqualTo(blend));
        Assert.That(frame.DisposeMode, Is.EqualTo(dispose));
        Assert.That(frame.BitstreamKind, Is.EqualTo(kind));
    }

    static byte[] ReadAlpha(WebpPixelBuffer buffer)
    {
        byte[] alpha = new byte[buffer.Width * buffer.Height];
        int y = 0;
        while (y < buffer.Height)
        {
            int x = 0;
            while (x < buffer.Width)
            {
                alpha[(y * buffer.Width) + x] = buffer.Rgba[(y * buffer.Stride) + (x * 4) + 3];
                x = x + 1;
            }

            y = y + 1;
        }

        return alpha;
    }
}
