using webp;

namespace webp.Tests;

[TestFixture]
public class WebpEncoderTests
{
    [Test]
    public void Encode_DefaultWritesLosslessWebpBytesThatDecode()
    {
        WebpPixelBuffer buffer = NewBuffer(2, 1);
        SetPixel(buffer, 0, 0, 10, 20, 30, 255);
        SetPixel(buffer, 1, 0, 40, 50, 60, 128);

        byte[] data = WebpEncoder.Encode(buffer);
        WebpDecodeResult decoded = WebpDecoder.Decode(data);

        Assert.That(data.Length, Is.GreaterThan(WebpConstants.RiffHeaderSize));
        Assert.That(decoded.IsValid, Is.True);
        Assert.That(decoded.Image.Frames[0].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossless));
        Assert.That(decoded.Image.Frames[0].Pixels, Is.Not.Null);
        Assert.That(decoded.Image.Frames[0].Pixels!.Rgba, Is.EqualTo(buffer.Rgba));
    }

    [Test]
    public void Encode_LosslessModeWritesBasicLosslessContainer()
    {
        WebpPixelBuffer buffer = NewBuffer(1, 1);
        SetPixel(buffer, 0, 0, 10, 20, 30, 255);
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Mode = WebpEncodeMode.Lossless;

        byte[] data = WebpEncoder.Encode(buffer, options);
        WebpBasicContainerParseResult parsed = WebpBasicContainerParser.Parse(data, WebpStrictness.Strict);

        Assert.That(parsed.IsValid, Is.True);
        Assert.That(parsed.ContainerKind, Is.EqualTo(WebpContainerKind.SimpleLossless));
        Assert.That(parsed.BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossless));
    }

    [Test]
    public void Encode_NearLosslessModeAppliesPreprocessingBeforeLosslessContainer()
    {
        WebpPixelBuffer buffer = NewBuffer(1, 1);
        SetPixel(buffer, 0, 0, 123, 45, 67, 255);
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Mode = WebpEncodeMode.NearLossless;
        options.NearLosslessQuality = 50;

        byte[] data = WebpEncoder.Encode(buffer, options);
        WebpDecodeResult decoded = WebpDecoder.Decode(data);

        Assert.That(decoded.IsValid, Is.True);
        Assert.That(decoded.Image.Frames[0].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossless));
        Assert.That(decoded.Image.Frames[0].Pixels!.Rgba[0], Is.EqualTo(120));
    }

    [Test]
    public void Encode_LossyModeWritesLossyContainer()
    {
        WebpPixelBuffer buffer = NewBuffer(1, 1);
        SetPixel(buffer, 0, 0, 10, 20, 30, 255);
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Mode = WebpEncodeMode.Lossy;

        byte[] data = WebpEncoder.Encode(buffer, options);
        WebpDecodeResult decoded = WebpDecoder.Decode(data);

        Assert.That(decoded.IsValid, Is.True);
        Assert.That(decoded.Image.Frames[0].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossy));
        Assert.That(decoded.Image.HasAlpha, Is.False);
    }

    [Test]
    public void Encode_LossyModeWithAlphaWritesExtendedAlphaContainer()
    {
        WebpPixelBuffer buffer = NewBuffer(1, 1);
        SetPixel(buffer, 0, 0, 10, 20, 30, 64);
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Mode = WebpEncodeMode.Lossy;
        options.AlphaCompression = WebpAlphaCompressionMethod.None;
        options.AlphaFilter = WebpAlphaFilterMode.None;

        byte[] data = WebpEncoder.Encode(buffer, options);
        WebpDecodeResult decoded = WebpDecoder.Decode(data);

        Assert.That(decoded.IsValid, Is.True);
        Assert.That(decoded.Image.HasAlpha, Is.True);
        Assert.That(decoded.Image.Frames[0].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossy));
        Assert.That(decoded.Image.Frames[0].Pixels!.Rgba[3], Is.EqualTo(64));
    }

    [Test]
    public void Encode_AnimatedImageEncodesRgbaFramesWithPerFrameModes()
    {
        WebpImage image = NewAnimatedRgbaImage();
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Mode = WebpEncodeMode.Lossless;
        options.AlphaCompression = WebpAlphaCompressionMethod.None;
        options.AlphaFilter = WebpAlphaFilterMode.None;

        byte[] data = WebpEncoder.Encode(image, options);
        WebpExtendedContainerParseResult parsed = WebpExtendedContainerParser.Parse(data, WebpStrictness.Strict);
        WebpDecodeResult decoded = WebpDecoder.Decode(data);

        Assert.That(parsed.IsValid, Is.True);
        Assert.That(parsed.Vp8X.HasAnimation, Is.True);
        Assert.That(parsed.Vp8X.HasAlpha, Is.True);
        Assert.That(parsed.AnimationLoopCount, Is.EqualTo(3));
        Assert.That(parsed.AnimationBackgroundColorBgra, Is.EqualTo(0x11223344U));
        Assert.That(parsed.Metadata.IccProfile, Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(parsed.UnknownChunks, Has.Count.EqualTo(1));
        Assert.That(parsed.UnknownChunks[0].FourCc, Is.EqualTo("zzzz"));
        Assert.That(parsed.Frames, Has.Count.EqualTo(2));
        Assert.That(parsed.Frames[0].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossless));
        Assert.That(parsed.Frames[0].X, Is.EqualTo(2));
        Assert.That(parsed.Frames[0].DurationMilliseconds, Is.EqualTo(50));
        Assert.That(parsed.Frames[0].BlendMode, Is.EqualTo(WebpFrameBlendMode.Replace));
        Assert.That(parsed.Frames[1].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossy));
        Assert.That(parsed.Frames[1].AlphaData, Is.Not.Null);
        Assert.That(parsed.Frames[1].DisposeMode, Is.EqualTo(WebpFrameDisposeMode.DisposeToBackground));
        Assert.That(decoded.IsValid, Is.True);
        Assert.That(decoded.Image.Frames[0].Pixels!.Rgba, Is.EqualTo(image.Frames[0].Pixels!.Rgba));
        Assert.That(decoded.Image.Frames[1].Pixels!.Rgba[3], Is.EqualTo(64));
    }

    [Test]
    public void Encode_AnimatedImageUsesGlobalLossyModeForUnknownFrameMode()
    {
        WebpImage image = new WebpImage();
        image.CanvasWidth = 1;
        image.CanvasHeight = 1;
        image.HasAnimation = true;
        image.Frames.Add(NewAnimationFrame(0, 0, 1, 1, 25, 10, 20, 30, 255));
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Mode = WebpEncodeMode.Lossy;

        byte[] data = WebpEncoder.Encode(image, options);
        WebpExtendedContainerParseResult parsed = WebpExtendedContainerParser.Parse(data, WebpStrictness.Strict);

        Assert.That(parsed.IsValid, Is.True);
        Assert.That(parsed.Vp8X.HasAnimation, Is.True);
        Assert.That(parsed.Frames, Has.Count.EqualTo(1));
        Assert.That(parsed.Frames[0].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossy));
    }

    [Test]
    public void Encode_AnimatedImagePreservesPreEncodedFrameBitstreams()
    {
        WebpPixelBuffer firstPixels = NewBuffer(1, 1);
        SetPixel(firstPixels, 0, 0, 10, 20, 30, 255);
        WebpPixelBuffer secondPixels = NewBuffer(1, 1);
        SetPixel(secondPixels, 0, 0, 90, 80, 70, 255);
        byte[] losslessPayload = WebpVp8LEncoder.Encode(firstPixels);
        byte[] lossyPayload = WebpVp8KeyFrameEncoder.Encode(secondPixels);
        byte[] alphaPayload = WebpAlphaEncoder.EncodeUncompressed(new byte[] { 64 }, 1, 1);

        WebpImage image = new WebpImage();
        image.CanvasWidth = 4;
        image.CanvasHeight = 1;
        image.HasAnimation = true;
        WebpFrame first = NewEncodedAnimationFrame(0, 0, 1, 1, 10, WebpBitstreamKind.Lossless, losslessPayload);
        image.Frames.Add(first);
        WebpFrame second = NewEncodedAnimationFrame(2, 0, 1, 1, 20, WebpBitstreamKind.Lossy, lossyPayload);
        second.AlphaData = alphaPayload;
        image.Frames.Add(second);

        byte[] data = WebpEncoder.Encode(image);
        WebpExtendedContainerParseResult parsed = WebpExtendedContainerParser.Parse(data, WebpStrictness.Strict);
        WebpDecodeResult decoded = WebpDecoder.Decode(data);

        Assert.That(parsed.IsValid, Is.True);
        Assert.That(parsed.Frames, Has.Count.EqualTo(2));
        Assert.That(parsed.Frames[0].EncodedBitstream, Is.EqualTo(losslessPayload));
        Assert.That(parsed.Frames[0].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossless));
        Assert.That(parsed.Frames[1].EncodedBitstream, Is.EqualTo(lossyPayload));
        Assert.That(parsed.Frames[1].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossy));
        Assert.That(parsed.Frames[1].AlphaData, Is.EqualTo(alphaPayload));
        Assert.That(decoded.IsValid, Is.True);
        Assert.That(decoded.Image.Frames[0].Pixels!.Rgba, Is.EqualTo(firstPixels.Rgba));
        Assert.That(decoded.Image.Frames[1].Pixels!.Rgba[3], Is.EqualTo(64));
    }

    [Test]
    public void Encode_AnimatedImageInfersPreservedBitstreamKind()
    {
        WebpPixelBuffer pixels = NewBuffer(1, 1);
        SetPixel(pixels, 0, 0, 10, 20, 30, 255);
        byte[] payload = WebpVp8LEncoder.Encode(pixels);

        WebpImage image = new WebpImage();
        image.CanvasWidth = 1;
        image.CanvasHeight = 1;
        image.HasAnimation = true;
        WebpFrame frame = NewEncodedAnimationFrame(0, 0, 0, 0, 10, WebpBitstreamKind.Unknown, payload);
        image.Frames.Add(frame);

        byte[] data = WebpEncoder.Encode(image);
        WebpExtendedContainerParseResult parsed = WebpExtendedContainerParser.Parse(data, WebpStrictness.Strict);

        Assert.That(parsed.IsValid, Is.True);
        Assert.That(parsed.Frames, Has.Count.EqualTo(1));
        Assert.That(parsed.Frames[0].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossless));
        Assert.That(parsed.Frames[0].Width, Is.EqualTo(1));
        Assert.That(parsed.Frames[0].Height, Is.EqualTo(1));
        Assert.That(parsed.Frames[0].EncodedBitstream, Is.EqualTo(payload));
    }

    [Test]
    public void SelectAutomaticMode_DefaultChoosesBasicLossless()
    {
        WebpPixelBuffer buffer = NewBuffer(1, 1);
        SetPixel(buffer, 0, 0, 10, 20, 30, 255);

        WebpAutomaticEncodeDecision decision = WebpEncoder.SelectAutomaticMode(buffer, new WebpEncodeOptions());

        Assert.That(decision.Mode, Is.EqualTo(WebpEncodeMode.Lossless));
        Assert.That(decision.BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossless));
        Assert.That(decision.ContainerKind, Is.EqualTo(WebpContainerKind.SimpleLossless));
        Assert.That(decision.UsesExtendedContainer, Is.False);
    }

    [Test]
    public void Encode_AutomaticUsesNearLosslessWhenRequested()
    {
        WebpPixelBuffer buffer = NewBuffer(1, 1);
        SetPixel(buffer, 0, 0, 123, 45, 67, 255);
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.NearLosslessQuality = 50;

        WebpAutomaticEncodeDecision decision = WebpEncoder.SelectAutomaticMode(buffer, options);
        byte[] data = WebpEncoder.Encode(buffer, options);
        WebpDecodeResult decoded = WebpDecoder.Decode(data);

        Assert.That(decision.Mode, Is.EqualTo(WebpEncodeMode.NearLossless));
        Assert.That(decision.ContainerKind, Is.EqualTo(WebpContainerKind.SimpleLossless));
        Assert.That(decoded.Image.Frames[0].Pixels!.Rgba[0], Is.EqualTo(120));
    }

    [Test]
    public void Encode_AutomaticUsesLossyForLowQualitySmallOpaqueImages()
    {
        WebpPixelBuffer buffer = NewBuffer(1, 1);
        SetPixel(buffer, 0, 0, 10, 20, 30, 255);
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Quality = 50;

        WebpAutomaticEncodeDecision decision = WebpEncoder.SelectAutomaticMode(buffer, options);
        byte[] data = WebpEncoder.Encode(buffer, options);
        WebpDecodeResult decoded = WebpDecoder.Decode(data);

        Assert.That(decision.Mode, Is.EqualTo(WebpEncodeMode.Lossy));
        Assert.That(decision.ContainerKind, Is.EqualTo(WebpContainerKind.SimpleLossy));
        Assert.That(decoded.Image.Frames[0].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossy));
    }

    [Test]
    public void Encode_AutomaticUsesExtendedLossyWhenAlphaIsNotLossless()
    {
        WebpPixelBuffer buffer = NewBuffer(1, 1);
        SetPixel(buffer, 0, 0, 10, 20, 30, 64);
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.LosslessAlpha = false;
        options.AlphaCompression = WebpAlphaCompressionMethod.None;
        options.AlphaFilter = WebpAlphaFilterMode.None;

        WebpAutomaticEncodeDecision decision = WebpEncoder.SelectAutomaticMode(buffer, options);
        byte[] data = WebpEncoder.Encode(buffer, options);
        WebpDecodeResult decoded = WebpDecoder.Decode(data);

        Assert.That(decision.Mode, Is.EqualTo(WebpEncodeMode.Lossy));
        Assert.That(decision.ContainerKind, Is.EqualTo(WebpContainerKind.ExtendedStill));
        Assert.That(decision.UsesExtendedContainer, Is.True);
        Assert.That(decoded.Image.HasAlpha, Is.True);
        Assert.That(decoded.Image.Frames[0].Pixels!.Rgba[3], Is.EqualTo(64));
    }

    [Test]
    public void Encode_AutomaticFallsBackToLosslessWhenSimpleLossyCannotRepresentSize()
    {
        WebpPixelBuffer buffer = NewBuffer(17, 1);
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Quality = 50;

        WebpAutomaticEncodeDecision decision = WebpEncoder.SelectAutomaticMode(buffer, options);
        byte[] data = WebpEncoder.Encode(buffer, options);
        WebpDecodeResult decoded = WebpDecoder.Decode(data);

        Assert.That(decision.Mode, Is.EqualTo(WebpEncodeMode.Lossless));
        Assert.That(decoded.IsValid, Is.True);
        Assert.That(decoded.Image.Frames[0].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossless));
    }

    [Test]
    public void Encode_StreamWritesWebpBytesAtCurrentPosition()
    {
        WebpPixelBuffer buffer = NewBuffer(1, 1);
        SetPixel(buffer, 0, 0, 10, 20, 30, 255);
        MemoryStream stream = new MemoryStream();
        stream.WriteByte(0xAA);

        WebpEncoder.Encode(buffer, stream);
        byte[] data = stream.ToArray();
        byte[] webp = CopyBytes(data, 1, data.Length - 1);
        WebpDecodeResult decoded = WebpDecoder.Decode(webp);

        Assert.That(data[0], Is.EqualTo(0xAA));
        Assert.That(decoded.IsValid, Is.True);
        Assert.That(decoded.Image.Frames[0].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossless));
    }

    [Test]
    public void Encode_StreamUsesOptions()
    {
        WebpPixelBuffer buffer = NewBuffer(1, 1);
        SetPixel(buffer, 0, 0, 10, 20, 30, 255);
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Mode = WebpEncodeMode.Lossy;
        MemoryStream stream = new MemoryStream();

        WebpEncoder.Encode(buffer, stream, options);
        WebpDecodeResult decoded = WebpDecoder.Decode(stream.ToArray());

        Assert.That(decoded.IsValid, Is.True);
        Assert.That(decoded.Image.Frames[0].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossy));
    }

    [Test]
    public void Encode_FilePathWritesWebpBytes()
    {
        WebpPixelBuffer buffer = NewBuffer(1, 1);
        SetPixel(buffer, 0, 0, 10, 20, 30, 255);
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "encode-path-" + Guid.NewGuid().ToString("N") + ".webp");

        WebpEncoder.Encode(buffer, path);
        WebpDecodeResult decoded = WebpDecoder.Decode(path);
        File.Delete(path);

        Assert.That(decoded.IsValid, Is.True);
        Assert.That(decoded.Image.Frames[0].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossless));
    }

    [Test]
    public void Encode_FilePathUsesOptions()
    {
        WebpPixelBuffer buffer = NewBuffer(1, 1);
        SetPixel(buffer, 0, 0, 10, 20, 30, 255);
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Mode = WebpEncodeMode.Lossy;
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "encode-path-" + Guid.NewGuid().ToString("N") + ".webp");

        WebpEncoder.Encode(buffer, path, options);
        WebpDecodeResult decoded = WebpDecoder.Decode(path);
        File.Delete(path);

        Assert.That(decoded.IsValid, Is.True);
        Assert.That(decoded.Image.Frames[0].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossy));
    }

    [Test]
    public void Encode_RejectsInvalidInputs()
    {
        WebpPixelBuffer buffer = NewBuffer(1, 1);
        WebpEncodeOptions options = new WebpEncodeOptions();

        Assert.Throws<ArgumentNullException>(() => WebpEncoder.Encode((WebpPixelBuffer)null!));
        Assert.Throws<ArgumentNullException>(() => WebpEncoder.Encode(buffer, (WebpEncodeOptions)null!));
        Assert.Throws<ArgumentNullException>(() => WebpEncoder.Encode((WebpImage)null!));
        Assert.Throws<ArgumentNullException>(() => WebpEncoder.Encode(new WebpImage(), (WebpEncodeOptions)null!));
        Assert.Throws<ArgumentNullException>(() => WebpEncoder.Encode(buffer, (Stream)null!));
        Assert.Throws<ArgumentNullException>(() => WebpEncoder.Encode(buffer, new MemoryStream(), null!));
        Assert.Throws<ArgumentNullException>(() => WebpEncoder.Encode(buffer, (string)null!));
        Assert.Throws<ArgumentNullException>(() => WebpEncoder.Encode(buffer, "out.webp", null!));
        Assert.Throws<ArgumentNullException>(() => WebpEncoder.Encode(new WebpImage(), (Stream)null!));
        Assert.Throws<ArgumentNullException>(() => WebpEncoder.Encode(new WebpImage(), (string)null!));
        Assert.Throws<ArgumentException>(() => WebpEncoder.Encode(buffer, new MemoryStream(new byte[4], false)));
        Assert.Throws<ArgumentException>(() => WebpEncoder.Encode(NewAnimatedFrameWithoutPixelsOrBitstream()));
        options.Mode = (WebpEncodeMode)99;
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpEncoder.Encode(buffer, options));
    }

    static byte[] CopyBytes(byte[] source, int offset, int length)
    {
        byte[] output = new byte[length];
        Array.Copy(source, offset, output, 0, length);
        return output;
    }

    static WebpImage NewAnimatedRgbaImage()
    {
        WebpImage image = new WebpImage();
        image.CanvasWidth = 4;
        image.CanvasHeight = 2;
        image.HasAnimation = true;
        image.LoopCount = 3;
        image.BackgroundColorBgra = 0x11223344U;
        image.Metadata.IccProfile = new byte[] { 1, 2, 3 };
        image.UnknownChunks.Add(new WebpUnknownChunk("zzzz", new byte[] { 9 }));

        WebpFrame first = NewAnimationFrame(2, 0, 1, 1, 50, 10, 20, 30, 255);
        first.BitstreamKind = WebpBitstreamKind.Lossless;
        first.BlendMode = WebpFrameBlendMode.Replace;
        image.Frames.Add(first);

        WebpFrame second = NewAnimationFrame(0, 0, 1, 1, 70, 90, 80, 70, 64);
        second.BitstreamKind = WebpBitstreamKind.Lossy;
        second.DisposeMode = WebpFrameDisposeMode.DisposeToBackground;
        image.Frames.Add(second);

        return image;
    }

    static WebpImage NewAnimatedFrameWithoutPixelsOrBitstream()
    {
        WebpImage image = new WebpImage();
        image.CanvasWidth = 1;
        image.CanvasHeight = 1;
        image.HasAnimation = true;
        WebpFrame frame = new WebpFrame();
        frame.Width = 1;
        frame.Height = 1;
        frame.BitstreamKind = WebpBitstreamKind.Lossless;
        image.Frames.Add(frame);
        return image;
    }

    static WebpFrame NewEncodedAnimationFrame(int x, int y, int width, int height, int duration, WebpBitstreamKind bitstreamKind, byte[] payload)
    {
        WebpFrame frame = new WebpFrame();
        frame.X = x;
        frame.Y = y;
        frame.Width = width;
        frame.Height = height;
        frame.DurationMilliseconds = duration;
        frame.BitstreamKind = bitstreamKind;
        frame.EncodedBitstream = WebpUnknownChunk.CopyBytes(payload);
        return frame;
    }

    static WebpFrame NewAnimationFrame(int x, int y, int width, int height, int duration, byte red, byte green, byte blue, byte alpha)
    {
        WebpFrame frame = new WebpFrame();
        frame.X = x;
        frame.Y = y;
        frame.Width = width;
        frame.Height = height;
        frame.DurationMilliseconds = duration;
        frame.Pixels = NewBuffer(width, height);
        SetPixel(frame.Pixels, 0, 0, red, green, blue, alpha);
        return frame;
    }

    static WebpPixelBuffer NewBuffer(int width, int height)
    {
        return WebpPixelBufferHelper.AllocateRgba(width, height);
    }

    static void SetPixel(WebpPixelBuffer buffer, int x, int y, byte red, byte green, byte blue, byte alpha)
    {
        int offset = (y * buffer.Stride) + (x * 4);
        buffer.Rgba[offset] = red;
        buffer.Rgba[offset + 1] = green;
        buffer.Rgba[offset + 2] = blue;
        buffer.Rgba[offset + 3] = alpha;
    }
}
