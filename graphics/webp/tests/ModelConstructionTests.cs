using webp;

namespace webp.Tests;

[TestFixture]
public class ModelConstructionTests
{
    [Test]
    public void DecodeOptions_DefaultsAreConservativeAndUsable()
    {
        WebpDecodeOptions options = new WebpDecodeOptions();

        Assert.That(options.Strictness, Is.EqualTo(WebpStrictness.Permissive));
        Assert.That(options.UnknownChunkPolicy, Is.EqualTo(WebpUnknownChunkPolicy.Preserve));
        Assert.That(options.AnimationDecodeMode, Is.EqualTo(WebpAnimationDecodeMode.IndependentFrames));
        Assert.That(options.IdentifyOnly, Is.False);
        Assert.That(options.FirstFrameOnly, Is.False);
        Assert.That(options.FrameIndex, Is.EqualTo(-1));
        Assert.That(options.DecodeMetadata, Is.True);
        Assert.That(options.CropX, Is.EqualTo(0));
        Assert.That(options.CropY, Is.EqualTo(0));
        Assert.That(options.CropWidth, Is.EqualTo(0));
        Assert.That(options.CropHeight, Is.EqualTo(0));
        Assert.That(options.TargetWidth, Is.EqualTo(0));
        Assert.That(options.TargetHeight, Is.EqualTo(0));
        Assert.That(options.AspectRatioMode, Is.EqualTo(WebpAspectRatioMode.Preserve));
        Assert.That(options.Limits, Is.Not.Null);
        Assert.That(options.Limits.MaxCanvasPixels, Is.GreaterThan(0));
        Assert.That(options.Limits.MaxTotalPixels, Is.GreaterThan(0));
    }

    [Test]
    public void EncodeOptions_DefaultsMatchGeneralPurposeEncoding()
    {
        WebpEncodeOptions options = new WebpEncodeOptions();

        Assert.That(options.Mode, Is.EqualTo(WebpEncodeMode.Automatic));
        Assert.That(options.Preset, Is.EqualTo(WebpLossyPreset.Default));
        Assert.That(options.Quality, Is.EqualTo(75));
        Assert.That(options.Method, Is.EqualTo(4));
        Assert.That(options.FilterStrength, Is.EqualTo(60));
        Assert.That(options.FilterSharpness, Is.EqualTo(0));
        Assert.That(options.AutoFilter, Is.False);
        Assert.That(options.SpatialNoiseShaping, Is.EqualTo(50));
        Assert.That(options.Preprocessing, Is.EqualTo(WebpLossyPreprocessing.None));
        Assert.That(options.TargetSize, Is.EqualTo(0));
        Assert.That(options.TargetPsnr, Is.EqualTo(0.0));
        Assert.That(options.PassCount, Is.EqualTo(1));
        Assert.That(options.NearLosslessQuality, Is.EqualTo(100));
        Assert.That(options.ExactTransparentRgb, Is.False);
        Assert.That(options.LosslessAlpha, Is.True);
        Assert.That(options.AlphaQuality, Is.EqualTo(100));
        Assert.That(options.AlphaCompression, Is.EqualTo(WebpAlphaCompressionMethod.Lossless));
        Assert.That(options.AlphaFilter, Is.EqualTo(WebpAlphaFilterMode.Fast));
        Assert.That(options.LossyAlphaMode, Is.EqualTo(WebpLossyAlphaMode.Preserve));
        Assert.That(options.AlphaBackgroundColorBgra, Is.EqualTo(0xFF000000U));
        Assert.That(options.Deterministic, Is.False);
        Assert.That(options.MetadataCopyPolicy, Is.EqualTo(WebpMetadataCopyPolicy.All));
        Assert.That(options.UnknownChunkPolicy, Is.EqualTo(WebpUnknownChunkPolicy.Preserve));
        Assert.That(options.Limits, Is.Not.Null);
    }

    [Test]
    public void ImageAndInfo_ConstructIndependentCollections()
    {
        WebpImage firstImage = new WebpImage();
        WebpImage secondImage = new WebpImage();
        WebpInfo firstInfo = new WebpInfo();
        WebpInfo secondInfo = new WebpInfo();

        firstImage.Frames.Add(new WebpFrame());
        firstImage.UnknownChunks.Add(new WebpUnknownChunk("test", Array.Empty<byte>()));
        firstInfo.Chunks.Add(new WebpChunkInfo());
        firstInfo.Diagnostics.Add(new WebpDiagnostic(WebpDiagnosticSeverity.Info, -1, "", ""));

        Assert.That(secondImage.Frames, Is.Empty);
        Assert.That(secondImage.UnknownChunks, Is.Empty);
        Assert.That(secondInfo.Chunks, Is.Empty);
        Assert.That(secondInfo.Diagnostics, Is.Empty);
        Assert.That(firstImage.Metadata, Is.Not.SameAs(secondImage.Metadata));
    }

    [Test]
    public void Diagnostics_CanRepresentWarningsAndErrors()
    {
        WebpDiagnostic diagnostic = new WebpDiagnostic(
            WebpDiagnosticSeverity.Error,
            42,
            "VP8X",
            "reserved feature bits are set");
        WebpValidationResult result = new WebpValidationResult();

        result.Diagnostics.Add(diagnostic);
        result.IsValid = !result.HasErrors();

        Assert.That(diagnostic.Severity, Is.EqualTo(WebpDiagnosticSeverity.Error));
        Assert.That(diagnostic.Offset, Is.EqualTo(42));
        Assert.That(diagnostic.FourCc, Is.EqualTo("VP8X"));
        Assert.That(diagnostic.Message, Does.Contain("reserved"));
        Assert.That(result.HasErrors(), Is.True);
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void UnknownChunk_CopiesPayloadBytes()
    {
        byte[] payload = new byte[] { 1, 2, 3 };
        WebpUnknownChunk chunk = new WebpUnknownChunk("abcd", payload);

        payload[0] = 99;

        Assert.That(chunk.FourCc, Is.EqualTo("abcd"));
        Assert.That(chunk.Data, Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(chunk.Data, Is.Not.SameAs(payload));
    }

    [Test]
    public void Frame_DefaultsMatchWebpAnimationSemantics()
    {
        WebpFrame frame = new WebpFrame();

        Assert.That(frame.X, Is.EqualTo(0));
        Assert.That(frame.Y, Is.EqualTo(0));
        Assert.That(frame.Width, Is.EqualTo(0));
        Assert.That(frame.Height, Is.EqualTo(0));
        Assert.That(frame.DurationMilliseconds, Is.EqualTo(0));
        Assert.That(frame.BlendMode, Is.EqualTo(WebpFrameBlendMode.AlphaBlend));
        Assert.That(frame.DisposeMode, Is.EqualTo(WebpFrameDisposeMode.DoNotDispose));
        Assert.That(frame.BitstreamKind, Is.EqualTo(WebpBitstreamKind.Unknown));
        Assert.That(frame.Pixels, Is.Null);
        Assert.That(frame.EncodedBitstream, Is.Null);
        Assert.That(frame.AlphaData, Is.Null);
        Assert.That(frame.UnknownChunks, Is.Empty);
        Assert.That(frame.EncodeOptions, Is.Null);
    }

    [Test]
    public void FrameEncodeOptions_DefaultsMatchGlobalAlphaAndQualitySettings()
    {
        WebpFrameEncodeOptions options = new WebpFrameEncodeOptions();

        Assert.That(options.Mode, Is.EqualTo(WebpEncodeMode.Automatic));
        Assert.That(options.Quality, Is.EqualTo(75));
        Assert.That(options.NearLosslessQuality, Is.EqualTo(100));
        Assert.That(options.ExactTransparentRgb, Is.False);
        Assert.That(options.LosslessAlpha, Is.True);
        Assert.That(options.AlphaQuality, Is.EqualTo(100));
        Assert.That(options.AlphaCompression, Is.EqualTo(WebpAlphaCompressionMethod.Lossless));
        Assert.That(options.AlphaFilter, Is.EqualTo(WebpAlphaFilterMode.Fast));
        Assert.That(options.LossyAlphaMode, Is.EqualTo(WebpLossyAlphaMode.Preserve));
        Assert.That(options.AlphaBackgroundColorBgra, Is.EqualTo(0xFF000000U));
    }

    [Test]
    public void PixelBufferAndMetadata_DefaultsAreEmpty()
    {
        WebpPixelBuffer buffer = new WebpPixelBuffer();
        WebpPremultipliedPixelBuffer premultiplied = new WebpPremultipliedPixelBuffer();
        WebpMetadata metadata = new WebpMetadata();

        Assert.That(buffer.Width, Is.EqualTo(0));
        Assert.That(buffer.Height, Is.EqualTo(0));
        Assert.That(buffer.Stride, Is.EqualTo(0));
        Assert.That(buffer.Rgba, Is.Empty);
        Assert.That(premultiplied.Width, Is.EqualTo(0));
        Assert.That(premultiplied.Height, Is.EqualTo(0));
        Assert.That(premultiplied.Stride, Is.EqualTo(0));
        Assert.That(premultiplied.Rgba, Is.Empty);
        Assert.That(metadata.IccProfile, Is.Null);
        Assert.That(metadata.Exif, Is.Null);
        Assert.That(metadata.Xmp, Is.Null);
    }

    [Test]
    public void Image_DefaultsMatchEmptyStillModel()
    {
        WebpImage image = new WebpImage();

        Assert.That(image.CanvasWidth, Is.EqualTo(0));
        Assert.That(image.CanvasHeight, Is.EqualTo(0));
        Assert.That(image.HasAlpha, Is.False);
        Assert.That(image.HasAnimation, Is.False);
        Assert.That(image.LoopCount, Is.EqualTo(WebpConstants.AnimDefaultLoopCount));
        Assert.That(image.BackgroundColorBgra, Is.EqualTo(WebpConstants.AnimDefaultBackgroundColorBgra));
        Assert.That(image.Metadata, Is.Not.Null);
        Assert.That(image.Frames, Is.Empty);
        Assert.That(image.UnknownChunks, Is.Empty);
        Assert.That(image.Transform, Is.Not.Null);
        Assert.That(image.Transform.SourceCanvasWidth, Is.EqualTo(0));
        Assert.That(image.Transform.OutputWidth, Is.EqualTo(0));
        Assert.That(image.Transform.AspectRatioMode, Is.EqualTo(WebpAspectRatioMode.Preserve));
    }

    [Test]
    public void InfoAndChunkInfo_DefaultsAreUnknownAndEmpty()
    {
        WebpInfo info = new WebpInfo();
        WebpChunkInfo chunk = new WebpChunkInfo();

        Assert.That(info.Width, Is.EqualTo(0));
        Assert.That(info.Height, Is.EqualTo(0));
        Assert.That(info.HasAlpha, Is.False);
        Assert.That(info.HasAnimation, Is.False);
        Assert.That(info.HasIccProfile, Is.False);
        Assert.That(info.HasExif, Is.False);
        Assert.That(info.HasXmp, Is.False);
        Assert.That(info.LoopCount, Is.EqualTo(1));
        Assert.That(info.BackgroundColorBgra, Is.EqualTo(0));
        Assert.That(info.DeclaredFeatureFlags, Is.EqualTo(WebpVp8XFeatureFlags.None));
        Assert.That(info.ReservedFeatureBits, Is.EqualTo(0));
        Assert.That(info.HasReservedFeatureBits, Is.False);
        Assert.That(info.HasReservedBytes, Is.False);
        Assert.That(info.ContainerKind, Is.EqualTo(WebpContainerKind.Unknown));
        Assert.That(info.BitstreamKind, Is.EqualTo(WebpBitstreamKind.Unknown));
        Assert.That(info.Chunks, Is.Empty);
        Assert.That(info.Frames, Is.Empty);
        Assert.That(info.Diagnostics, Is.Empty);
        Assert.That(chunk.FourCc, Is.EqualTo(""));
        Assert.That(chunk.Offset, Is.EqualTo(0));
        Assert.That(chunk.PayloadOffset, Is.EqualTo(0));
        Assert.That(chunk.PayloadLength, Is.EqualTo(0));
        Assert.That(chunk.HasPadding, Is.False);
        Assert.That(chunk.PaddingOffset, Is.EqualTo(0));
        Assert.That(chunk.PaddedEndOffset, Is.EqualTo(0));
        Assert.That(chunk.IsPaddingValid, Is.True);
        Assert.That(chunk.IsKnown, Is.False);
    }

    [Test]
    public void TransformAndNormalizationResult_DefaultsAreEmpty()
    {
        WebpDecodeTransform transform = new WebpDecodeTransform();
        WebpNormalizationResult normalization = new WebpNormalizationResult();

        Assert.That(transform.SourceCanvasWidth, Is.EqualTo(0));
        Assert.That(transform.SourceCanvasHeight, Is.EqualTo(0));
        Assert.That(transform.SourceX, Is.EqualTo(0));
        Assert.That(transform.SourceY, Is.EqualTo(0));
        Assert.That(transform.SourceWidth, Is.EqualTo(0));
        Assert.That(transform.SourceHeight, Is.EqualTo(0));
        Assert.That(transform.OutputWidth, Is.EqualTo(0));
        Assert.That(transform.OutputHeight, Is.EqualTo(0));
        Assert.That(transform.AspectRatioMode, Is.EqualTo(WebpAspectRatioMode.Preserve));
        Assert.That(normalization.Data, Is.Empty);
        Assert.That(normalization.IsNormalized, Is.False);
        Assert.That(normalization.WasChanged, Is.False);
        Assert.That(normalization.Diagnostics, Is.Empty);
    }

    [Test]
    public void SecurityLimits_DefaultsMatchConfiguredCeilings()
    {
        WebpSecurityLimits limits = new WebpSecurityLimits();

        Assert.That(limits.MaxCanvasWidth, Is.EqualTo(WebpConstants.Vp8XMaxCanvasDimension));
        Assert.That(limits.MaxCanvasHeight, Is.EqualTo(WebpConstants.Vp8XMaxCanvasDimension));
        Assert.That(limits.MaxCanvasPixels, Is.EqualTo(WebpConstants.DefaultMaxCanvasPixels));
        Assert.That(limits.MaxTotalPixels, Is.EqualTo(WebpConstants.DefaultMaxTotalPixels));
        Assert.That(limits.MaxRiffSize, Is.EqualTo(WebpConstants.MaxRiffFileSize));
        Assert.That(limits.MaxChunkCount, Is.EqualTo(WebpConstants.DefaultMaxChunkCount));
        Assert.That(limits.MaxFrameCount, Is.EqualTo(WebpConstants.DefaultMaxFrameCount));
        Assert.That(limits.MaxRecursionDepth, Is.EqualTo(WebpConstants.DefaultMaxRecursionDepth));
        Assert.That(limits.MaxMetadataBytes, Is.EqualTo(WebpConstants.DefaultMaxMetadataBytes));
        Assert.That(limits.MaxUnknownChunkBytes, Is.EqualTo(WebpConstants.DefaultMaxUnknownChunkBytes));
        Assert.That(limits.MaxAggregateAllocationBytes, Is.EqualTo(WebpConstants.DefaultMaxAggregateAllocationBytes));
    }

    [Test]
    public void DecodeAndValidationResults_StartValidWithIndependentDiagnostics()
    {
        WebpDecodeResult firstDecode = new WebpDecodeResult();
        WebpDecodeResult secondDecode = new WebpDecodeResult();
        WebpValidationResult validation = new WebpValidationResult();

        firstDecode.Diagnostics.Add(new WebpDiagnostic(WebpDiagnosticSeverity.Error, 0, "RIFF", "bad"));

        Assert.That(firstDecode.IsValid, Is.True);
        Assert.That(firstDecode.Image, Is.Not.Null);
        Assert.That(firstDecode.HasErrors(), Is.True);
        Assert.That(secondDecode.Diagnostics, Is.Empty);
        Assert.That(validation.IsValid, Is.True);
        Assert.That(validation.Diagnostics, Is.Empty);
        Assert.That(validation.HasErrors(), Is.False);
    }

    [Test]
    public void AutomaticEncodeDecision_DefaultsToLosslessStill()
    {
        WebpAutomaticEncodeDecision decision = new WebpAutomaticEncodeDecision();

        Assert.That(decision.Mode, Is.EqualTo(WebpEncodeMode.Lossless));
        Assert.That(decision.BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossless));
        Assert.That(decision.ContainerKind, Is.EqualTo(WebpContainerKind.SimpleLossless));
        Assert.That(decision.HasAlpha, Is.False);
        Assert.That(decision.UsesExtendedContainer, Is.False);
    }

    [Test]
    public void Constructors_RejectNullReferenceInputs()
    {
        Assert.Throws<ArgumentNullException>(() => new WebpDiagnostic(WebpDiagnosticSeverity.Error, 0, null!, "message"));
        Assert.Throws<ArgumentNullException>(() => new WebpDiagnostic(WebpDiagnosticSeverity.Error, 0, "RIFF", null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new WebpDiagnostic((WebpDiagnosticSeverity)99, 0, "RIFF", "message"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new WebpDiagnostic(WebpDiagnosticSeverity.Error, -2, "RIFF", "message"));
        Assert.Throws<ArgumentException>(() => new WebpDiagnostic(WebpDiagnosticSeverity.Error, 0, "bad", "message"));
        Assert.Throws<ArgumentNullException>(() => new WebpUnknownChunk(null!, new byte[] { 1 }));
        Assert.Throws<ArgumentNullException>(() => new WebpUnknownChunk("abcd", null!));
        Assert.Throws<ArgumentNullException>(() => WebpUnknownChunk.CopyBytes(null!));
    }
}
