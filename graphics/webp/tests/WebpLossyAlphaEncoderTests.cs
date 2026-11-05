using webp;

namespace webp.Tests;

[TestFixture]
public class WebpLossyAlphaEncoderTests
{
    static WebpPixelBuffer ObservedLosslessBuffer = new WebpPixelBuffer();
    static byte[] LosslessStream = Array.Empty<byte>();

    [Test]
    public void ExtractAlpha_DetectsAndCopiesNonOpaqueAlpha()
    {
        WebpPixelBuffer buffer = NewBuffer(2, 1);
        SetPixel(buffer, 0, 0, 10, 20, 30, 255);
        SetPixel(buffer, 1, 0, 40, 50, 60, 128);

        byte[] alpha = WebpLossyAlphaEncoder.ExtractAlpha(buffer);

        Assert.That(WebpLossyAlphaEncoder.HasNonOpaqueAlpha(buffer), Is.True);
        Assert.That(alpha, Is.EqualTo(new byte[] { 255, 128 }));
    }

    [Test]
    public void HasNonOpaqueAlpha_ReturnsFalseForOpaqueImage()
    {
        WebpPixelBuffer buffer = NewBuffer(1, 1);
        buffer.Rgba[3] = 255;

        Assert.That(WebpLossyAlphaEncoder.HasNonOpaqueAlpha(buffer), Is.False);
    }

    [Test]
    public void SelectAlphaFilter_MapsModesAndBestChoosesSmallestResidual()
    {
        byte[] alpha = new byte[] { 10, 15, 20, 40 };

        Assert.That(WebpLossyAlphaEncoder.SelectAlphaFilter(alpha, 2, 2, WebpAlphaFilterMode.None), Is.EqualTo(WebpConstants.AlphaFilterNone));
        Assert.That(WebpLossyAlphaEncoder.SelectAlphaFilter(alpha, 2, 2, WebpAlphaFilterMode.Fast), Is.EqualTo(WebpConstants.AlphaFilterHorizontal));
        Assert.That(WebpLossyAlphaEncoder.SelectAlphaFilter(alpha, 2, 2, WebpAlphaFilterMode.Best), Is.EqualTo(WebpConstants.AlphaFilterGradient));
    }

    [Test]
    public void QuantizeAlpha_UsesAlphaQualityWhenLosslessAlphaDisabled()
    {
        byte[] alpha = new byte[] { 7, 128, 250 };

        byte[] quantized = WebpLossyAlphaEncoder.QuantizeAlpha(alpha, 0);

        Assert.That(quantized, Is.EqualTo(new byte[] { 0, 128, 255 }));
        Assert.That(alpha, Is.EqualTo(new byte[] { 7, 128, 250 }));
    }

    [Test]
    public void PrepareColorBuffer_ControlsTransparentRgbAndForcesOpaqueVp8Input()
    {
        WebpPixelBuffer buffer = NewBuffer(2, 1);
        SetPixel(buffer, 0, 0, 200, 30, 40, 0);
        SetPixel(buffer, 1, 0, 20, 40, 60, 255);

        WebpPixelBuffer nonExact = WebpLossyAlphaEncoder.PrepareColorBuffer(buffer, false);
        WebpPixelBuffer exact = WebpLossyAlphaEncoder.PrepareColorBuffer(buffer, true);

        Assert.That(nonExact.Rgba[0], Is.EqualTo(0));
        Assert.That(nonExact.Rgba[1], Is.EqualTo(0));
        Assert.That(nonExact.Rgba[2], Is.EqualTo(0));
        Assert.That(nonExact.Rgba[3], Is.EqualTo(255));
        Assert.That(exact.Rgba[0], Is.EqualTo(200));
        Assert.That(exact.Rgba[1], Is.EqualTo(30));
        Assert.That(exact.Rgba[2], Is.EqualTo(40));
        Assert.That(exact.Rgba[3], Is.EqualTo(255));
    }

    [Test]
    public void EncodeAlpha_DefaultsToCompressedLosslessAlphaPayload()
    {
        ObservedLosslessBuffer = new WebpPixelBuffer();
        LosslessStream = new byte[] { 1, 2, 3 };
        WebpPixelBuffer buffer = NewBuffer(2, 2);
        SetPixel(buffer, 0, 0, 0, 0, 0, 10);
        SetPixel(buffer, 1, 0, 0, 0, 0, 15);
        SetPixel(buffer, 0, 1, 0, 0, 0, 20);
        SetPixel(buffer, 1, 1, 0, 0, 0, 40);
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.AlphaFilter = WebpAlphaFilterMode.Fast;

        byte[] alphaPayload = WebpLossyAlphaEncoder.EncodeAlpha(buffer, options, EncodeLosslessForTest) ?? throw new InvalidOperationException("expected an alpha payload");

        Assert.That(alphaPayload, Is.Not.Null);
        Assert.That(alphaPayload[0], Is.EqualTo(0x05));
        Assert.That(ObservedLosslessBuffer.Rgba[1], Is.EqualTo(10));
        Assert.That(ObservedLosslessBuffer.Rgba[5], Is.EqualTo(5));
        Assert.That(ObservedLosslessBuffer.Rgba[9], Is.EqualTo(10));
        Assert.That(ObservedLosslessBuffer.Rgba[13], Is.EqualTo(20));
    }

    [Test]
    public void EncodeAlpha_CanWriteQuantizedUncompressedAlpha()
    {
        WebpPixelBuffer buffer = NewBuffer(2, 1);
        SetPixel(buffer, 0, 0, 0, 0, 0, 7);
        SetPixel(buffer, 1, 0, 0, 0, 0, 250);
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.LosslessAlpha = false;
        options.AlphaQuality = 0;
        options.AlphaCompression = WebpAlphaCompressionMethod.None;
        options.AlphaFilter = WebpAlphaFilterMode.None;

        byte[] alphaPayload = WebpLossyAlphaEncoder.EncodeAlpha(buffer, options, EncodeLosslessForTest) ?? throw new InvalidOperationException("expected an alpha payload");
        WebpAlphaDecodeResult decoded = WebpAlphaDecoder.Decode(alphaPayload, 2, 1, WebpStrictness.Strict);

        Assert.That(alphaPayload[0], Is.EqualTo(0));
        Assert.That(decoded.IsValid, Is.True);
        Assert.That(decoded.Alpha, Is.EqualTo(new byte[] { 0, 255 }));
    }

    [Test]
    public void EncodeAlpha_ReturnsNullForOpaqueImage()
    {
        WebpPixelBuffer buffer = NewBuffer(1, 1);
        buffer.Rgba[3] = 255;

        Assert.That(WebpLossyAlphaEncoder.EncodeAlpha(buffer, new WebpEncodeOptions()), Is.Null);
    }

    [Test]
    public void EncodeStill_WritesExtendedLossyContainerWithAlphChunk()
    {
        WebpPixelBuffer buffer = NewBuffer(1, 1);
        SetPixel(buffer, 0, 0, 20, 30, 40, 128);
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.AlphaCompression = WebpAlphaCompressionMethod.None;
        options.AlphaFilter = WebpAlphaFilterMode.None;

        byte[] data = WebpLossyAlphaEncoder.EncodeStill(buffer, options);
        WebpExtendedContainerParseResult result = WebpExtendedContainerParser.Parse(data, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Vp8X.HasAlpha, Is.True);
        Assert.That(result.BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossy));
        Assert.That(result.AlphaData, Is.EqualTo(new byte[] { 0, 128 }));
        Assert.That(result.ImagePayload, Is.Not.Null);
        Assert.That(result.ImagePayload![3], Is.EqualTo(WebpConstants.Vp8LossyKeyFrameSignature0));
    }

    [Test]
    public void EncodeStill_WritesBasicLossyContainerForOpaqueImage()
    {
        WebpPixelBuffer buffer = NewBuffer(1, 1);
        SetPixel(buffer, 0, 0, 20, 30, 40, 255);

        byte[] data = WebpLossyAlphaEncoder.EncodeStill(buffer, new WebpEncodeOptions());
        WebpBasicContainerParseResult result = WebpBasicContainerParser.Parse(data, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.ContainerKind, Is.EqualTo(WebpContainerKind.SimpleLossy));
        Assert.That(result.HasAlpha, Is.False);
    }

    [Test]
    public void LossyAlphaEncoder_RejectsInvalidInputs()
    {
        WebpPixelBuffer buffer = NewBuffer(1, 1);
        WebpEncodeOptions options = new WebpEncodeOptions();

        Assert.Throws<ArgumentNullException>(() => WebpLossyAlphaEncoder.HasNonOpaqueAlpha(null!));
        Assert.Throws<ArgumentNullException>(() => WebpLossyAlphaEncoder.ExtractAlpha(null!));
        Assert.Throws<ArgumentNullException>(() => WebpLossyAlphaEncoder.EncodeAlpha(null!, options));
        Assert.Throws<ArgumentNullException>(() => WebpLossyAlphaEncoder.EncodeAlpha(buffer, null!));
        Assert.Throws<ArgumentNullException>(() => WebpLossyAlphaEncoder.PrepareColorBuffer(null!, false));
        Assert.Throws<ArgumentNullException>(() => WebpLossyAlphaEncoder.QuantizeAlpha(null!, 100));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpLossyAlphaEncoder.QuantizeAlpha(new byte[] { 1 }, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpLossyAlphaEncoder.SelectAlphaFilter(new byte[] { 1 }, 1, 1, (WebpAlphaFilterMode)9));
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

    static byte[] EncodeLosslessForTest(WebpPixelBuffer buffer)
    {
        ObservedLosslessBuffer = WebpPixelBufferHelper.Copy(buffer);
        return WebpUnknownChunk.CopyBytes(LosslessStream);
    }
}
