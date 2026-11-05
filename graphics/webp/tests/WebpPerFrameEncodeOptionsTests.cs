using webp;

namespace webp.Tests;

[TestFixture]
public class WebpPerFrameEncodeOptionsTests
{
    [Test]
    public void EncodeImage_PerFrameModesOverrideGlobalModeForPixelFrames()
    {
        WebpImage image = NewAnimation(2, 2);
        WebpFrame lossy = NewPixelFrame(2, 2, 10, 123, 45, 67, 255);
        lossy.EncodeOptions = NewFrameOptions(WebpEncodeMode.Lossy);
        lossy.EncodeOptions.Quality = 40;
        WebpFrame lossless = NewPixelFrame(2, 2, 20, 123, 45, 67, 255);
        lossless.EncodeOptions = NewFrameOptions(WebpEncodeMode.Lossless);
        WebpFrame nearLossless = NewPixelFrame(2, 2, 30, 123, 45, 67, 255);
        nearLossless.EncodeOptions = NewFrameOptions(WebpEncodeMode.NearLossless);
        nearLossless.EncodeOptions.NearLosslessQuality = 50;
        image.Frames.Add(lossy);
        image.Frames.Add(lossless);
        image.Frames.Add(nearLossless);
        WebpEncodeOptions global = new WebpEncodeOptions();
        global.Mode = WebpEncodeMode.Lossless;

        byte[] data = new WebpCodec().EncodeImage(image, global);
        WebpExtendedContainerParseResult parsed = WebpExtendedContainerParser.Parse(data, WebpStrictness.Strict);
        WebpDecodeResult decoded = WebpDecoder.Decode(data);

        Assert.That(parsed.IsValid, Is.True);
        Assert.That(parsed.Frames[0].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossy));
        Assert.That(parsed.Frames[1].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossless));
        Assert.That(parsed.Frames[2].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossless));
        Assert.That(decoded.IsValid, Is.True);
        Assert.That(decoded.Image.Frames[1].Pixels!.Rgba[0], Is.EqualTo(123));
        Assert.That(decoded.Image.Frames[2].Pixels!.Rgba[0], Is.EqualTo(120));
    }

    [Test]
    public void EncodeImage_PerFrameLossyQualityProducesIndependentBitstreams()
    {
        WebpImage image = NewAnimation(2, 2);
        WebpFrame lowQuality = NewPixelFrame(2, 2, 10, 20, 100, 220, 255);
        lowQuality.EncodeOptions = NewFrameOptions(WebpEncodeMode.Lossy);
        lowQuality.EncodeOptions.Quality = 0;
        WebpFrame highQuality = NewPixelFrame(2, 2, 20, 20, 100, 220, 255);
        highQuality.EncodeOptions = NewFrameOptions(WebpEncodeMode.Lossy);
        highQuality.EncodeOptions.Quality = 100;
        image.Frames.Add(lowQuality);
        image.Frames.Add(highQuality);

        byte[] data = new WebpCodec().EncodeImage(image, new WebpEncodeOptions());
        WebpExtendedContainerParseResult parsed = WebpExtendedContainerParser.Parse(data, WebpStrictness.Strict);

        Assert.That(parsed.Frames[0].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossy));
        Assert.That(parsed.Frames[1].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossy));
        Assert.That(parsed.Frames[0].EncodedBitstream, Is.Not.EqualTo(parsed.Frames[1].EncodedBitstream));
    }

    [Test]
    public void EncodeImage_PerFrameAlphaSettingsOverrideAndUnsetFramesUseGlobals()
    {
        WebpImage image = NewAnimation(1, 1);
        WebpFrame frameOverride = NewPixelFrame(1, 1, 10, 10, 20, 30, 70);
        frameOverride.EncodeOptions = NewFrameOptions(WebpEncodeMode.Lossy);
        frameOverride.EncodeOptions.LosslessAlpha = false;
        frameOverride.EncodeOptions.AlphaQuality = 0;
        frameOverride.EncodeOptions.AlphaCompression = WebpAlphaCompressionMethod.None;
        frameOverride.EncodeOptions.AlphaFilter = WebpAlphaFilterMode.None;
        WebpFrame globalFrame = NewPixelFrame(1, 1, 20, 10, 20, 30, 70);
        image.Frames.Add(frameOverride);
        image.Frames.Add(globalFrame);
        WebpEncodeOptions global = new WebpEncodeOptions();
        global.Mode = WebpEncodeMode.Lossy;
        global.LosslessAlpha = true;
        global.AlphaCompression = WebpAlphaCompressionMethod.Lossless;
        global.AlphaFilter = WebpAlphaFilterMode.Fast;

        byte[] data = new WebpCodec().EncodeImage(image, global);
        WebpExtendedContainerParseResult parsed = WebpExtendedContainerParser.Parse(data, WebpStrictness.Strict);
        WebpDecodeResult decoded = WebpDecoder.Decode(data);

        Assert.That(parsed.Frames[0].AlphaData, Is.Not.Null);
        Assert.That(parsed.Frames[1].AlphaData, Is.Not.Null);
        Assert.That(parsed.Frames[0].AlphaData![0] & WebpConstants.AlphaCompressionMask, Is.EqualTo(WebpConstants.AlphaCompressionNone));
        Assert.That(parsed.Frames[1].AlphaData![0] & WebpConstants.AlphaCompressionMask, Is.EqualTo(WebpConstants.AlphaCompressionLossless));
        Assert.That(decoded.Image.Frames[0].Pixels!.Rgba[3], Is.EqualTo(64));
        Assert.That(decoded.Image.Frames[1].Pixels!.Rgba[3], Is.EqualTo(70));
    }

    [Test]
    public void EncodeImage_PreEncodedFrameIgnoresPixelEncodeSettingsAndPreservesPayload()
    {
        WebpPixelBuffer pixels = NewPixels(1, 1, 5, 6, 7, 255);
        byte[] payload = WebpVp8LEncoder.Encode(pixels);
        WebpImage image = NewAnimation(1, 1);
        WebpFrame frame = new WebpFrame();
        frame.Width = 1;
        frame.Height = 1;
        frame.DurationMilliseconds = 10;
        frame.BitstreamKind = WebpBitstreamKind.Lossless;
        frame.EncodedBitstream = payload;
        frame.EncodeOptions = NewFrameOptions(WebpEncodeMode.Lossy);
        frame.EncodeOptions.Quality = 0;
        image.Frames.Add(frame);

        byte[] data = new WebpCodec().EncodeImage(image, new WebpEncodeOptions());
        WebpExtendedContainerParseResult parsed = WebpExtendedContainerParser.Parse(data, WebpStrictness.Strict);

        Assert.That(parsed.Frames[0].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossless));
        Assert.That(parsed.Frames[0].EncodedBitstream, Is.EqualTo(payload));
    }

    [Test]
    public void EncodeImage_InvalidPerFrameModeIsRejectedForPixelFrame()
    {
        WebpImage image = NewAnimation(1, 1);
        WebpFrame frame = NewPixelFrame(1, 1, 10, 1, 2, 3, 255);
        frame.EncodeOptions = NewFrameOptions((WebpEncodeMode)99);
        image.Frames.Add(frame);

        Assert.Throws<ArgumentOutOfRangeException>(() => new WebpCodec().EncodeImage(image, new WebpEncodeOptions()));
    }

    static WebpFrameEncodeOptions NewFrameOptions(WebpEncodeMode mode)
    {
        WebpFrameEncodeOptions options = new WebpFrameEncodeOptions();
        options.Mode = mode;
        options.AlphaCompression = WebpAlphaCompressionMethod.None;
        options.AlphaFilter = WebpAlphaFilterMode.None;
        return options;
    }

    static WebpImage NewAnimation(int width, int height)
    {
        WebpImage image = new WebpImage();
        image.CanvasWidth = width;
        image.CanvasHeight = height;
        image.HasAnimation = true;
        return image;
    }

    static WebpFrame NewPixelFrame(int width, int height, int duration, byte red, byte green, byte blue, byte alpha)
    {
        WebpFrame frame = new WebpFrame();
        frame.Width = width;
        frame.Height = height;
        frame.DurationMilliseconds = duration;
        frame.Pixels = NewPixels(width, height, red, green, blue, alpha);
        return frame;
    }

    static WebpPixelBuffer NewPixels(int width, int height, byte red, byte green, byte blue, byte alpha)
    {
        WebpPixelBuffer buffer = new WebpPixelBuffer();
        buffer.Width = width;
        buffer.Height = height;
        buffer.Stride = width * 4;
        buffer.Rgba = new byte[buffer.Stride * height];
        int index = 0;
        while (index < width * height)
        {
            int offset = index * 4;
            buffer.Rgba[offset] = red;
            buffer.Rgba[offset + 1] = green;
            buffer.Rgba[offset + 2] = blue;
            buffer.Rgba[offset + 3] = alpha;
            index = index + 1;
        }

        return buffer;
    }
}
