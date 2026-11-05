using webp;

namespace webp.Tests;

[TestFixture]
public class ConstantsTests
{
    [Test]
    public void FourCcConstants_ExposeStringAndLittleEndianValues()
    {
        Assert.That(WebpConstants.FourCcRiffString, Is.EqualTo("RIFF"));
        Assert.That(WebpConstants.FourCcWebpString, Is.EqualTo("WEBP"));
        Assert.That(WebpConstants.FourCcVp8String, Is.EqualTo("VP8 "));
        Assert.That(WebpConstants.FourCcVp8LString, Is.EqualTo("VP8L"));
        Assert.That(WebpConstants.FourCcVp8XString, Is.EqualTo("VP8X"));
        Assert.That(WebpConstants.FourCcAlphaString, Is.EqualTo("ALPH"));
        Assert.That(WebpConstants.FourCcAnimString, Is.EqualTo("ANIM"));
        Assert.That(WebpConstants.FourCcAnimationFrameString, Is.EqualTo("ANMF"));
        Assert.That(WebpConstants.FourCcIccProfileString, Is.EqualTo("ICCP"));
        Assert.That(WebpConstants.FourCcExifString, Is.EqualTo("EXIF"));
        Assert.That(WebpConstants.FourCcXmpString, Is.EqualTo("XMP "));

        Assert.That(WebpConstants.FourCcRiff, Is.EqualTo(0x46464952U));
        Assert.That(WebpConstants.FourCcWebp, Is.EqualTo(0x50424557U));
        Assert.That(WebpConstants.FourCcVp8, Is.EqualTo(0x20385056U));
        Assert.That(WebpConstants.FourCcVp8L, Is.EqualTo(0x4C385056U));
        Assert.That(WebpConstants.FourCcVp8X, Is.EqualTo(0x58385056U));
        Assert.That(WebpConstants.FourCcAlpha, Is.EqualTo(0x48504C41U));
        Assert.That(WebpConstants.FourCcAnim, Is.EqualTo(0x4D494E41U));
        Assert.That(WebpConstants.FourCcAnimationFrame, Is.EqualTo(0x464D4E41U));
        Assert.That(WebpConstants.FourCcIccProfile, Is.EqualTo(0x50434349U));
        Assert.That(WebpConstants.FourCcExif, Is.EqualTo(0x46495845U));
        Assert.That(WebpConstants.FourCcXmp, Is.EqualTo(0x20504D58U));
    }

    [Test]
    public void Vp8XConstants_ExposeFeatureMasksAndCanvasLimits()
    {
        byte combinedFeatures = (byte)(
            WebpConstants.Vp8XFeatureIccProfile
            | WebpConstants.Vp8XFeatureAlpha
            | WebpConstants.Vp8XFeatureExif
            | WebpConstants.Vp8XFeatureXmp
            | WebpConstants.Vp8XFeatureAnimation);

        Assert.That(WebpConstants.Vp8XChunkPayloadSize, Is.EqualTo(10));
        Assert.That(WebpConstants.Vp8XCanvasDimensionFieldBytes, Is.EqualTo(3));
        Assert.That(WebpConstants.Vp8XMaxCanvasDimension, Is.EqualTo(16777216));
        Assert.That(WebpConstants.Vp8XMaxCanvasPixels, Is.EqualTo(uint.MaxValue));
        Assert.That(combinedFeatures, Is.EqualTo(WebpConstants.Vp8XFeatureKnownMask));
        Assert.That((byte)(WebpConstants.Vp8XFeatureKnownMask | WebpConstants.Vp8XFeatureReservedMask), Is.EqualTo(0xFF));
        Assert.That((byte)(WebpConstants.Vp8XFeatureKnownMask & WebpConstants.Vp8XFeatureReservedMask), Is.EqualTo(0));
    }

    [Test]
    public void BitstreamConstants_ExposeLossyAndLosslessLimits()
    {
        Assert.That(WebpConstants.Vp8LossyMaxDimension, Is.EqualTo(16383));
        Assert.That(WebpConstants.Vp8LossyFrameTagSize, Is.EqualTo(3));
        Assert.That(WebpConstants.Vp8LossyKeyFrameHeaderSize, Is.EqualTo(10));
        Assert.That(WebpConstants.Vp8LossyKeyFrameSignature0, Is.EqualTo(0x9D));
        Assert.That(WebpConstants.Vp8LossyKeyFrameSignature1, Is.EqualTo(0x01));
        Assert.That(WebpConstants.Vp8LossyKeyFrameSignature2, Is.EqualTo(0x2A));

        Assert.That(WebpConstants.Vp8LMaxDimension, Is.EqualTo(16384));
        Assert.That(WebpConstants.Vp8LHeaderSize, Is.EqualTo(5));
        Assert.That(WebpConstants.Vp8LSignature, Is.EqualTo(0x2F));
        Assert.That(WebpConstants.Vp8LVersion, Is.EqualTo(0));
        Assert.That(WebpConstants.Vp8LMinColorCacheBits, Is.EqualTo(1));
        Assert.That(WebpConstants.Vp8LMaxColorCacheBits, Is.EqualTo(11));
    }

    [Test]
    public void AlphaAndAnimationConstants_ExposePackedFlagLayouts()
    {
        Assert.That(WebpConstants.AlphaCompressionMask, Is.EqualTo(0x03));
        Assert.That(WebpConstants.AlphaFilterMask, Is.EqualTo(0x0C));
        Assert.That(WebpConstants.AlphaPreprocessingMask, Is.EqualTo(0x30));
        Assert.That(WebpConstants.AlphaReservedMask, Is.EqualTo(0xC0));
        Assert.That(WebpConstants.AlphaFilterShift, Is.EqualTo(2));
        Assert.That(WebpConstants.AlphaPreprocessingShift, Is.EqualTo(4));
        Assert.That(WebpConstants.AlphaCompressionNone, Is.EqualTo(0));
        Assert.That(WebpConstants.AlphaCompressionLossless, Is.EqualTo(1));
        Assert.That(WebpConstants.AlphaFilterNone, Is.EqualTo(0));
        Assert.That(WebpConstants.AlphaFilterHorizontal, Is.EqualTo(1));
        Assert.That(WebpConstants.AlphaFilterVertical, Is.EqualTo(2));
        Assert.That(WebpConstants.AlphaFilterGradient, Is.EqualTo(3));

        Assert.That(WebpConstants.AnimChunkPayloadSize, Is.EqualTo(6));
        Assert.That(WebpConstants.AnimFrameHeaderSize, Is.EqualTo(16));
        Assert.That(WebpConstants.AnimFrameDisposeBackgroundFlag, Is.EqualTo(0x01));
        Assert.That(WebpConstants.AnimFrameNoBlendFlag, Is.EqualTo(0x02));
        Assert.That(WebpConstants.AnimFrameReservedMask, Is.EqualTo(0xFC));
        Assert.That(WebpConstants.AnimInfiniteLoopCount, Is.EqualTo(0));
        Assert.That(WebpConstants.AnimDefaultLoopCount, Is.EqualTo(1));
        Assert.That(WebpConstants.AnimDefaultBackgroundColorBgra, Is.EqualTo(0));
    }

    [Test]
    public void DefaultConstants_MatchModelDefaults()
    {
        WebpEncodeOptions encodeOptions = new WebpEncodeOptions();
        WebpDecodeOptions decodeOptions = new WebpDecodeOptions();
        WebpImage image = new WebpImage();

        Assert.That(encodeOptions.Quality, Is.EqualTo(WebpConstants.DefaultQuality));
        Assert.That(encodeOptions.Method, Is.EqualTo(WebpConstants.DefaultMethod));
        Assert.That(encodeOptions.NearLosslessQuality, Is.EqualTo(WebpConstants.DefaultNearLosslessQuality));
        Assert.That(encodeOptions.Mode, Is.EqualTo(WebpConstants.DefaultEncodeMode));
        Assert.That(encodeOptions.Preset, Is.EqualTo(WebpConstants.DefaultLossyPreset));
        Assert.That(encodeOptions.AlphaQuality, Is.EqualTo(WebpConstants.DefaultAlphaQuality));
        Assert.That(encodeOptions.AlphaCompression, Is.EqualTo(WebpConstants.DefaultAlphaCompression));
        Assert.That(encodeOptions.AlphaFilter, Is.EqualTo(WebpConstants.DefaultAlphaFilter));
        Assert.That(encodeOptions.MetadataCopyPolicy, Is.EqualTo(WebpConstants.DefaultMetadataCopyPolicy));
        Assert.That(encodeOptions.UnknownChunkPolicy, Is.EqualTo(WebpConstants.DefaultUnknownChunkPolicy));
        Assert.That(decodeOptions.UnknownChunkPolicy, Is.EqualTo(WebpConstants.DefaultUnknownChunkPolicy));
        Assert.That(decodeOptions.Limits.MaxCanvasPixels, Is.EqualTo(WebpConstants.DefaultMaxCanvasPixels));
        Assert.That(image.LoopCount, Is.EqualTo(WebpConstants.AnimDefaultLoopCount));
        Assert.That(image.BackgroundColorBgra, Is.EqualTo(WebpConstants.AnimDefaultBackgroundColorBgra));
    }

    [Test]
    public void RiffConstants_ExposeHeaderSizesAndMaximums()
    {
        Assert.That(WebpConstants.MimeType, Is.EqualTo("image/webp"));
        Assert.That(WebpConstants.FileExtension, Is.EqualTo(".webp"));
        Assert.That(WebpConstants.RiffHeaderSize, Is.EqualTo(12));
        Assert.That(WebpConstants.ChunkHeaderSize, Is.EqualTo(8));
        Assert.That(WebpConstants.FourCcByteCount, Is.EqualTo(4));
        Assert.That(WebpConstants.MaxRiffSizeField, Is.EqualTo(4294967286L));
        Assert.That(WebpConstants.MaxRiffFileSize, Is.EqualTo(4294967294L));
    }
}
