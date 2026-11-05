using webp;

namespace webp.Tests;

[TestFixture]
public class WebpLossyAlphaModeTests
{
    [Test]
    public void PrepareLossyColorBuffer_KeepsPreserveDiscardBlendAndExactRgbDistinct()
    {
        WebpPixelBuffer source = NewBuffer(2, 1);
        SetPixel(source, 0, 200, 30, 40, 0);
        SetPixel(source, 1, 210, 110, 10, 128);
        WebpEncodeOptions preserve = NewLossyOptions(WebpLossyAlphaMode.Preserve);
        WebpEncodeOptions exact = NewLossyOptions(WebpLossyAlphaMode.Preserve);
        exact.ExactTransparentRgb = true;
        WebpEncodeOptions discard = NewLossyOptions(WebpLossyAlphaMode.Discard);
        WebpEncodeOptions blend = NewLossyOptions(WebpLossyAlphaMode.BlendWithBackground);
        blend.ExactTransparentRgb = true;
        blend.AlphaBackgroundColorBgra = 0x000A141EU;

        WebpPixelBuffer preserved = WebpLossyAlphaEncoder.PrepareLossyColorBuffer(source, preserve);
        WebpPixelBuffer exactPreserved = WebpLossyAlphaEncoder.PrepareLossyColorBuffer(source, exact);
        WebpPixelBuffer discarded = WebpLossyAlphaEncoder.PrepareLossyColorBuffer(source, discard);
        WebpPixelBuffer blended = WebpLossyAlphaEncoder.PrepareLossyColorBuffer(source, blend);

        Assert.That(Pixel(preserved, 0), Is.EqualTo(new byte[] { 0, 0, 0, 255 }));
        Assert.That(Pixel(exactPreserved, 0), Is.EqualTo(new byte[] { 200, 30, 40, 255 }));
        Assert.That(Pixel(discarded, 0), Is.EqualTo(new byte[] { 200, 30, 40, 255 }));
        Assert.That(Pixel(discarded, 1), Is.EqualTo(new byte[] { 210, 110, 10, 255 }));
        Assert.That(Pixel(blended, 0), Is.EqualTo(new byte[] { 10, 20, 30, 255 }));
        Assert.That(Pixel(blended, 1), Is.EqualTo(new byte[] { 110, 65, 20, 255 }));
        Assert.That(Pixel(source, 0), Is.EqualTo(new byte[] { 200, 30, 40, 0 }));
    }

    [Test]
    public void Encode_LossyAlphaModesSelectExtendedOrOpaqueBasicContainers()
    {
        WebpPixelBuffer source = NewBuffer(1, 1);
        SetPixel(source, 0, 200, 30, 40, 64);
        WebpCodec codec = new WebpCodec();

        byte[] preserveData = codec.Encode(source, NewLossyOptions(WebpLossyAlphaMode.Preserve));
        byte[] discardData = codec.Encode(source, NewLossyOptions(WebpLossyAlphaMode.Discard));
        WebpEncodeOptions blendOptions = NewLossyOptions(WebpLossyAlphaMode.BlendWithBackground);
        blendOptions.AlphaBackgroundColorBgra = 0xFF102030U;
        byte[] blendData = codec.Encode(source, blendOptions);
        WebpDecodeResult preserve = codec.Decode(preserveData, new WebpDecodeOptions());
        WebpDecodeResult discard = codec.Decode(discardData, new WebpDecodeOptions());
        WebpDecodeResult blend = codec.Decode(blendData, new WebpDecodeOptions());

        Assert.That(preserve.Image.HasAlpha, Is.True);
        Assert.That(preserve.Image.Frames[0].Pixels!.Rgba[3], Is.EqualTo(64));
        Assert.That(discard.Image.HasAlpha, Is.False);
        Assert.That(discard.Image.Frames[0].Pixels!.Rgba[3], Is.EqualTo(255));
        Assert.That(blend.Image.HasAlpha, Is.False);
        Assert.That(blend.Image.Frames[0].Pixels!.Rgba[3], Is.EqualTo(255));
        Assert.That(WebpBasicContainerParser.Parse(discardData, WebpStrictness.Strict).ContainerKind, Is.EqualTo(WebpContainerKind.SimpleLossy));
        Assert.That(WebpBasicContainerParser.Parse(blendData, WebpStrictness.Strict).ContainerKind, Is.EqualTo(WebpContainerKind.SimpleLossy));
    }

    [Test]
    public void EncodeImage_PerFrameAlphaModesOverrideGlobalPreservation()
    {
        WebpImage image = NewAnimation();
        WebpFrame discard = NewFrame(10, 70);
        discard.EncodeOptions = NewFrameOptions(WebpLossyAlphaMode.Discard);
        WebpFrame blend = NewFrame(20, 70);
        blend.EncodeOptions = NewFrameOptions(WebpLossyAlphaMode.BlendWithBackground);
        blend.EncodeOptions.AlphaBackgroundColorBgra = 0xFFFFFFFFU;
        WebpFrame preserve = NewFrame(30, 70);
        image.Frames.Add(discard);
        image.Frames.Add(blend);
        image.Frames.Add(preserve);
        WebpEncodeOptions global = NewLossyOptions(WebpLossyAlphaMode.Preserve);

        byte[] data = new WebpCodec().EncodeImage(image, global);
        WebpExtendedContainerParseResult parsed = WebpExtendedContainerParser.Parse(data, WebpStrictness.Strict);
        WebpDecodeResult decoded = WebpDecoder.Decode(data);

        Assert.That(parsed.Vp8X.HasAlpha, Is.True);
        Assert.That(parsed.Frames[0].AlphaData, Is.Null);
        Assert.That(parsed.Frames[1].AlphaData, Is.Null);
        Assert.That(parsed.Frames[2].AlphaData, Is.Not.Null);
        Assert.That(decoded.Image.Frames[0].Pixels!.Rgba[3], Is.EqualTo(255));
        Assert.That(decoded.Image.Frames[1].Pixels!.Rgba[3], Is.EqualTo(255));
        Assert.That(decoded.Image.Frames[2].Pixels!.Rgba[3], Is.EqualTo(70));
    }

    [Test]
    public void EncodeImage_AllFlattenedFramesClearDerivedAnimationAlphaFlag()
    {
        WebpImage image = NewAnimation();
        image.HasAlpha = true;
        WebpFrame discard = NewFrame(10, 64);
        discard.EncodeOptions = NewFrameOptions(WebpLossyAlphaMode.Discard);
        WebpFrame blend = NewFrame(20, 64);
        blend.EncodeOptions = NewFrameOptions(WebpLossyAlphaMode.BlendWithBackground);
        image.Frames.Add(discard);
        image.Frames.Add(blend);

        byte[] data = new WebpCodec().EncodeImage(image, NewLossyOptions(WebpLossyAlphaMode.Preserve));
        WebpExtendedContainerParseResult parsed = WebpExtendedContainerParser.Parse(data, WebpStrictness.Strict);

        Assert.That(parsed.Vp8X.HasAlpha, Is.False);
        Assert.That(parsed.Frames[0].AlphaData, Is.Null);
        Assert.That(parsed.Frames[1].AlphaData, Is.Null);
    }

    [Test]
    public void Encode_InvalidGlobalAndPerFrameAlphaModesAreRejected()
    {
        WebpPixelBuffer source = NewBuffer(1, 1);
        SetPixel(source, 0, 1, 2, 3, 4);
        WebpEncodeOptions invalidGlobal = NewLossyOptions((WebpLossyAlphaMode)99);
        WebpImage image = NewAnimation();
        WebpFrame frame = NewFrame(10, 4);
        frame.EncodeOptions = NewFrameOptions((WebpLossyAlphaMode)99);
        image.Frames.Add(frame);

        Assert.Throws<ArgumentOutOfRangeException>(() => new WebpCodec().Encode(source, invalidGlobal));
        Assert.Throws<ArgumentOutOfRangeException>(() => new WebpCodec().EncodeImage(image, NewLossyOptions(WebpLossyAlphaMode.Preserve)));
    }

    static WebpEncodeOptions NewLossyOptions(WebpLossyAlphaMode mode)
    {
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Mode = WebpEncodeMode.Lossy;
        options.Quality = 100;
        options.AlphaCompression = WebpAlphaCompressionMethod.None;
        options.AlphaFilter = WebpAlphaFilterMode.None;
        options.LossyAlphaMode = mode;
        return options;
    }

    static WebpFrameEncodeOptions NewFrameOptions(WebpLossyAlphaMode mode)
    {
        WebpFrameEncodeOptions options = new WebpFrameEncodeOptions();
        options.Mode = WebpEncodeMode.Lossy;
        options.Quality = 100;
        options.AlphaCompression = WebpAlphaCompressionMethod.None;
        options.AlphaFilter = WebpAlphaFilterMode.None;
        options.LossyAlphaMode = mode;
        return options;
    }

    static WebpImage NewAnimation()
    {
        WebpImage image = new WebpImage();
        image.CanvasWidth = 1;
        image.CanvasHeight = 1;
        image.HasAnimation = true;
        return image;
    }

    static WebpFrame NewFrame(int duration, byte alpha)
    {
        WebpFrame frame = new WebpFrame();
        frame.Width = 1;
        frame.Height = 1;
        frame.DurationMilliseconds = duration;
        frame.Pixels = NewBuffer(1, 1);
        SetPixel(frame.Pixels, 0, 200, 30, 40, alpha);
        return frame;
    }

    static WebpPixelBuffer NewBuffer(int width, int height)
    {
        WebpPixelBuffer buffer = new WebpPixelBuffer();
        buffer.Width = width;
        buffer.Height = height;
        buffer.Stride = width * 4;
        buffer.Rgba = new byte[buffer.Stride * height];
        return buffer;
    }

    static void SetPixel(WebpPixelBuffer buffer, int index, byte red, byte green, byte blue, byte alpha)
    {
        int offset = index * 4;
        buffer.Rgba[offset] = red;
        buffer.Rgba[offset + 1] = green;
        buffer.Rgba[offset + 2] = blue;
        buffer.Rgba[offset + 3] = alpha;
    }

    static byte[] Pixel(WebpPixelBuffer buffer, int index)
    {
        int offset = index * 4;
        return new byte[]
        {
            buffer.Rgba[offset],
            buffer.Rgba[offset + 1],
            buffer.Rgba[offset + 2],
            buffer.Rgba[offset + 3]
        };
    }
}
