namespace avif.Tests;

public class OptionDefaultsTests
{
    [Test]
    public void DecoderDefaultsAreStrictBoundedAndDeterministic()
    {
        AvifDecoderOptions options = new AvifDecoderOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.ValidationMode, Is.EqualTo(AvifValidationMode.Strict));
            Assert.That(options.SourceSelection, Is.EqualTo(AvifSourceSelection.Automatic));
            Assert.That(options.ContentSelection, Is.EqualTo(AvifContentSelection.ColorAndAlpha));
            Assert.That(options.AllowProgressive, Is.False);
            Assert.That(options.AllowIncremental, Is.False);
            Assert.That(options.ApplyFilmGrain, Is.True);
            Assert.That(options.OperatingPoint, Is.EqualTo(-1));
            Assert.That(options.WorkerCount, Is.EqualTo(1));
            Assert.That(options.RenderOptions, Is.Not.Null);
            Assert.That(options.Limits, Is.Not.Null);
            Assert.That(options.Cancellation, Is.Not.Null);
        });
    }

    [Test]
    public void EncoderDefaultsFavorDeterministicValidatedOutput()
    {
        AvifEncoderOptions options = new AvifEncoderOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.Mode, Is.EqualTo(AvifEncoderMode.ConstantQuality));
            Assert.That(options.Preset, Is.EqualTo(AvifEncoderPreset.Balanced));
            Assert.That(options.HeaderFormat, Is.EqualTo(AvifHeaderFormat.Automatic));
            Assert.That(options.Quality, Is.EqualTo(75));
            Assert.That(options.AlphaQuality, Is.EqualTo(100));
            Assert.That(options.GainMapQuality, Is.EqualTo(75));
            Assert.That(options.Quantizer, Is.EqualTo(-1));
            Assert.That(options.TargetSizeBytes, Is.Zero);
            Assert.That(options.TargetBitrate, Is.Zero);
            Assert.That(options.KeyframeInterval, Is.Zero);
            Assert.That(options.AutoTiling, Is.True);
            Assert.That(options.TileRowsLog2, Is.Zero);
            Assert.That(options.TileColumnsLog2, Is.Zero);
            Assert.That(options.WorkerCount, Is.EqualTo(1));
            Assert.That(options.Deterministic, Is.True);
            Assert.That(options.ValidateOutput, Is.True);
            Assert.That(options.Limits, Is.Not.Null);
        });
    }

    [Test]
    public void ValidationDefaultsEnforceBaselineWithoutPixelAllocation()
    {
        AvifValidationOptions options = new AvifValidationOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.Mode, Is.EqualTo(AvifValidationMode.Strict));
            Assert.That(options.AllowUnknownNonEssentialData, Is.True);
            Assert.That(options.ValidateEncodedPayload, Is.True);
            Assert.That(options.DecodePixelData, Is.False);
            Assert.That(options.CollectInformation, Is.False);
            Assert.That(options.CollectWarnings, Is.True);
            Assert.That(options.StopAfterFirstError, Is.False);
            Assert.That(options.Limits, Is.Not.Null);
        });
    }

    [Test]
    public void RenderAndColorDefaultsPreserveAppearanceAndAlpha()
    {
        AvifRenderOptions render = new AvifRenderOptions();
        AvifColorConversionOptions color = render.ColorConversion;

        Assert.Multiple(() =>
        {
            Assert.That(render.ApplyCleanAperture, Is.True);
            Assert.That(render.ApplyRotation, Is.True);
            Assert.That(render.ApplyMirror, Is.True);
            Assert.That(render.ApplyExifOrientation, Is.True);
            Assert.That(render.ToneMapMode, Is.EqualTo(AvifToneMapMode.Automatic));
            Assert.That(render.DisplayHeadroom, Is.EqualTo(1.0f));
            Assert.That(render.PreserveAlpha, Is.True);
            Assert.That(render.Limits, Is.Not.Null);
            Assert.That(color.ResamplingFilter, Is.EqualTo(AvifResamplingFilter.Bilinear));
            Assert.That(color.DitherMode, Is.EqualTo(AvifDitherMode.None));
            Assert.That(color.UseEmbeddedIccProfile, Is.True);
            Assert.That(color.ClampOutOfGamut, Is.True);
            Assert.That(color.PreserveAlpha, Is.True);
            Assert.That(color.OutputPremultipliedAlpha, Is.False);
            Assert.That(color.WorkerCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void RewriteDefaultsPreserveOpaqueDataAndValidateOutput()
    {
        AvifRewriteOptions options = new AvifRewriteOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.HeaderFormat, Is.EqualTo(AvifHeaderFormat.PreserveExisting));
            Assert.That(options.PreserveUnknownBoxes, Is.True);
            Assert.That(options.PreserveUnknownProperties, Is.True);
            Assert.That(options.PreserveUnknownMetadata, Is.True);
            Assert.That(options.PreserveProtectedData, Is.True);
            Assert.That(options.Canonicalize, Is.False);
            Assert.That(options.ValidateOutput, Is.True);
            Assert.That(options.Limits, Is.Not.Null);
        });
    }

    [Test]
    public void LowLevelAv1DefaultsAreStrictDeterministicAndBounded()
    {
        Av1DecoderOptions decoder = new Av1DecoderOptions();
        Av1EncoderOptions encoder = new Av1EncoderOptions();

        Assert.Multiple(() =>
        {
            Assert.That(decoder.OperatingPoint, Is.EqualTo(-1));
            Assert.That(decoder.SpatialLayer, Is.EqualTo(-1));
            Assert.That(decoder.TemporalLayer, Is.EqualTo(-1));
            Assert.That(decoder.AllowProgressive, Is.False);
            Assert.That(decoder.DecodeLargeScaleTiles, Is.False);
            Assert.That(decoder.AllowIncremental, Is.False);
            Assert.That(decoder.ApplyFilmGrain, Is.True);
            Assert.That(decoder.Strict, Is.True);
            Assert.That(decoder.WorkerCount, Is.EqualTo(1));
            Assert.That(decoder.Limits, Is.Not.Null);
            Assert.That(decoder.Cancellation, Is.Not.Null);
            Assert.That(encoder.Mode, Is.EqualTo(AvifEncoderMode.ConstantQuality));
            Assert.That(encoder.Preset, Is.EqualTo(AvifEncoderPreset.Balanced));
            Assert.That(encoder.Quality, Is.EqualTo(75));
            Assert.That(encoder.Quantizer, Is.EqualTo(-1));
            Assert.That(encoder.ReducedStillPictureHeader, Is.True);
            Assert.That(encoder.AutoTiling, Is.True);
            Assert.That(encoder.WorkerCount, Is.EqualTo(1));
            Assert.That(encoder.Deterministic, Is.True);
            Assert.That(encoder.Limits, Is.Not.Null);
        });
    }

    [Test]
    public void OptionInstancesDoNotShareNestedMutableState()
    {
        AvifDecoderOptions firstDecoder = new AvifDecoderOptions();
        AvifDecoderOptions secondDecoder = new AvifDecoderOptions();
        AvifRenderOptions firstRender = new AvifRenderOptions();
        AvifRenderOptions secondRender = new AvifRenderOptions();
        Av1DecoderOptions firstAv1Decoder = new Av1DecoderOptions();
        Av1DecoderOptions secondAv1Decoder = new Av1DecoderOptions();

        firstDecoder.Limits.MaxItemCount = 1;
        firstDecoder.RenderOptions.DisplayHeadroom = 4.0f;
        firstRender.ColorConversion.WorkerCount = 8;

        Assert.Multiple(() =>
        {
            Assert.That(secondDecoder.Limits.MaxItemCount, Is.EqualTo(AvifSecurityLimits.DefaultMaxItemCount));
            Assert.That(secondDecoder.RenderOptions.DisplayHeadroom, Is.EqualTo(1.0f));
            Assert.That(secondRender.ColorConversion.WorkerCount, Is.EqualTo(1));
            Assert.That(firstDecoder.Limits, Is.Not.SameAs(secondDecoder.Limits));
            Assert.That(firstDecoder.RenderOptions, Is.Not.SameAs(secondDecoder.RenderOptions));
            Assert.That(firstDecoder.Cancellation, Is.Not.SameAs(secondDecoder.Cancellation));
            Assert.That(firstAv1Decoder.Cancellation, Is.Not.SameAs(secondAv1Decoder.Cancellation));
            Assert.That(firstRender.ColorConversion, Is.Not.SameAs(secondRender.ColorConversion));
        });
    }
}
