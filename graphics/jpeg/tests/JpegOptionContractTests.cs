namespace jpeg.Tests;

internal class JpegOptionContractTests
{
    [Test]
    public void ReadOptionConstructor_ProvidesEveryDocumentedDefault()
    {
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();

        Assert.Multiple(
            delegate
            {
                Assert.That(options.MetadataPolicy, Is.EqualTo(global::jpeg.JpegMetadataPolicy.PreserveAll));
                Assert.That(options.ColorPolicy, Is.EqualTo(global::jpeg.JpegColorPolicy.PreserveSource));
                Assert.That(options.IccBehaviorPolicy, Is.EqualTo(global::jpeg.JpegIccBehaviorPolicy.Preserve));
                Assert.That(options.ErrorPolicy, Is.EqualTo(global::jpeg.JpegErrorPolicy.Strict));
                Assert.That(options.OutputPixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.Unknown));
                Assert.That(options.ChromaUpsamplingMode, Is.EqualTo(global::jpeg.JpegChromaUpsamplingMode.Box));
                Assert.That(options.RawComponentOutput, Is.False);
                Assert.That(options.UnknownColorHandling, Is.EqualTo(global::jpeg.JpegUnknownColorHandling.BestEffort));
                Assert.That(options.AdobeInversionMode, Is.EqualTo(global::jpeg.JpegAdobeInversionMode.Automatic));
                Assert.That(options.YCbCrRange, Is.EqualTo(global::jpeg.JpegYCbCrRange.Full));
                Assert.That(options.InverseDctMethod, Is.EqualTo(global::jpeg.JpegInverseDctMethod.Accurate));
                Assert.That(options.IdctScaleNumerator, Is.EqualTo(1));
                Assert.That(options.IdctScaleDenominator, Is.EqualTo(1));
                Assert.That(options.IdctScaleRatio, Is.EqualTo(global::jpeg.JpegIdctScaleRatio.Automatic));
                Assert.That(options.TargetWidth, Is.Zero);
                Assert.That(options.TargetHeight, Is.Zero);
                Assert.That(options.DecodeRegion, Is.Not.Null);
                Assert.That(options.DecodeRegion.X, Is.Zero);
                Assert.That(options.DecodeRegion.Y, Is.Zero);
                Assert.That(options.DecodeRegion.Width, Is.Zero);
                Assert.That(options.DecodeRegion.Height, Is.Zero);
                Assert.That(options.FastMcuCrop, Is.False);
                Assert.That(options.MaxWidth, Is.EqualTo(global::jpeg.JpegConstants.DefaultMaxWidth));
                Assert.That(options.MaxHeight, Is.EqualTo(global::jpeg.JpegConstants.DefaultMaxHeight));
                Assert.That(options.MaxComponents, Is.EqualTo(global::jpeg.JpegConstants.DefaultMaxComponents));
                Assert.That(options.MaxScans, Is.EqualTo(global::jpeg.JpegConstants.DefaultMaxScans));
                Assert.That(options.MaxMarkerPayloadBytes, Is.EqualTo(global::jpeg.JpegConstants.DefaultMaxMarkerPayloadBytes));
                Assert.That(options.MaxRetainedMarkers, Is.EqualTo(global::jpeg.JpegConstants.DefaultMaxRetainedMarkers));
                Assert.That(options.MaxTables, Is.EqualTo(global::jpeg.JpegConstants.DefaultMaxTables));
                Assert.That(options.MaxMetadataBytes, Is.EqualTo(global::jpeg.JpegConstants.DefaultMaxMetadataBytes));
                Assert.That(options.MaxEntropyBytes, Is.EqualTo(global::jpeg.JpegConstants.DefaultMaxEntropyBytes));
                Assert.That(options.MaxOutputPixels, Is.EqualTo(global::jpeg.JpegConstants.DefaultMaxOutputPixels));
                Assert.That(options.MaxOutputBytes, Is.EqualTo(global::jpeg.JpegConstants.DefaultMaxOutputBytes));
                Assert.That(options.MaxComponentBufferBytes, Is.EqualTo(global::jpeg.JpegConstants.DefaultMaxComponentBufferBytes));
                Assert.That(options.MaxProgressiveStateBytes, Is.EqualTo(global::jpeg.JpegConstants.DefaultMaxProgressiveStateBytes));
                Assert.That(options.MaxCallbackOutputBytes, Is.EqualTo(global::jpeg.JpegConstants.DefaultMaxCallbackOutputBytes));
                Assert.That(options.AutoOrient, Is.False);
                Assert.That(options.SkipMetadata, Is.False);
            });
    }

    [Test]
    public void WriteOptionConstructor_ProvidesEveryDocumentedDefault()
    {
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();

        Assert.Multiple(
            delegate
            {
                Assert.That(options.Preset, Is.EqualTo(global::jpeg.JpegEncoderPreset.None));
                Assert.That(options.Deterministic, Is.False);
                Assert.That(options.MetadataPolicy, Is.EqualTo(global::jpeg.JpegMetadataPolicy.PreserveAll));
                Assert.That(options.ColorPolicy, Is.EqualTo(global::jpeg.JpegColorPolicy.PreserveSource));
                Assert.That(options.IccBehaviorPolicy, Is.EqualTo(global::jpeg.JpegIccBehaviorPolicy.Preserve));
                Assert.That(options.ErrorPolicy, Is.EqualTo(global::jpeg.JpegErrorPolicy.Strict));
                Assert.That(options.Process, Is.EqualTo(global::jpeg.JpegProcess.Unknown));
                Assert.That(options.SamplePrecision, Is.Zero);
                Assert.That(options.EntropyCoding, Is.EqualTo(global::jpeg.JpegEntropyCoding.Automatic));
                Assert.That(options.LosslessPredictor, Is.EqualTo(global::jpeg.JpegLosslessPredictorSelection.Left));
                Assert.That(options.PointTransform, Is.Zero);
                Assert.That(options.Hierarchical, Is.False);
                Assert.That(options.Components, Is.Empty);
                Assert.That(options.Tables, Is.Not.Null);
                Assert.That(options.PreserveQuantizationTables, Is.False);
                Assert.That(options.ScanTableAssignments, Is.Empty);
                Assert.That(options.ScanLayout, Is.EqualTo(global::jpeg.JpegScanLayout.Automatic));
                Assert.That(options.Subsampling, Is.EqualTo(global::jpeg.JpegSubsampling.Unknown));
                Assert.That(options.ChromaDownsamplingFilter, Is.EqualTo(global::jpeg.JpegChromaDownsamplingFilter.Box));
                Assert.That(options.Quality, Is.EqualTo(global::jpeg.JpegConstants.DefaultQuality));
                Assert.That(options.LuminanceQuality, Is.Zero);
                Assert.That(options.ChrominanceQuality, Is.Zero);
                Assert.That(options.Progressive, Is.False);
                Assert.That(options.ProgressiveScanScript, Is.Not.Null);
                Assert.That(options.ProgressiveScanScript.Scans, Is.Empty);
                Assert.That(options.OptimizeHuffmanTables, Is.False);
                Assert.That(options.RestartInterval, Is.Zero);
                Assert.That(options.RestartIntervalUnit, Is.EqualTo(global::jpeg.JpegRestartIntervalUnit.McuBlocks));
            });
    }

    [Test]
    public void TransformOptionConstructor_ProvidesEveryDocumentedDefault()
    {
        global::jpeg.JpegTransformOptions options = new global::jpeg.JpegTransformOptions();

        Assert.That(options.RequirePerfectTransform, Is.True);
        Assert.That(options.McuCropMode, Is.EqualTo(global::jpeg.JpegMcuCropMode.Exact));
        Assert.That(options.MetadataPolicy, Is.EqualTo(global::jpeg.JpegMetadataPolicy.PreserveAll));
    }

    [Test]
    public void Identifier_RejectsEveryUnknownReadPolicyAtThePublicBoundary()
    {
        AssertReadPolicyRejected(delegate(global::jpeg.JpegReadOptions options) { options.MetadataPolicy = (global::jpeg.JpegMetadataPolicy)99; });
        AssertReadPolicyRejected(delegate(global::jpeg.JpegReadOptions options) { options.ColorPolicy = (global::jpeg.JpegColorPolicy)99; });
        AssertReadPolicyRejected(delegate(global::jpeg.JpegReadOptions options) { options.IccBehaviorPolicy = (global::jpeg.JpegIccBehaviorPolicy)99; });
        AssertReadPolicyRejected(delegate(global::jpeg.JpegReadOptions options) { options.ErrorPolicy = (global::jpeg.JpegErrorPolicy)99; });
        AssertReadPolicyRejected(delegate(global::jpeg.JpegReadOptions options) { options.OutputPixelFormat = (global::jpeg.JpegPixelFormat)99; });
        AssertReadPolicyRejected(delegate(global::jpeg.JpegReadOptions options) { options.ChromaUpsamplingMode = (global::jpeg.JpegChromaUpsamplingMode)99; });
        AssertReadPolicyRejected(delegate(global::jpeg.JpegReadOptions options) { options.UnknownColorHandling = (global::jpeg.JpegUnknownColorHandling)99; });
        AssertReadPolicyRejected(delegate(global::jpeg.JpegReadOptions options) { options.AdobeInversionMode = (global::jpeg.JpegAdobeInversionMode)99; });
        AssertReadPolicyRejected(delegate(global::jpeg.JpegReadOptions options) { options.YCbCrRange = (global::jpeg.JpegYCbCrRange)99; });
        AssertReadPolicyRejected(delegate(global::jpeg.JpegReadOptions options) { options.InverseDctMethod = (global::jpeg.JpegInverseDctMethod)99; });
    }

    [Test]
    public void Reader_RejectsInvalidScaleAndMissingRegionWithArgumentExceptions()
    {
        global::jpeg.JpegReadOptions scale = new global::jpeg.JpegReadOptions();
        scale.IdctScaleRatio = (global::jpeg.JpegIdctScaleRatio)99;
        global::jpeg.JpegReadOptions region = new global::jpeg.JpegReadOptions();
        region.DecodeRegion = null!;
        global::jpeg.JpegIdentifierService identifier = new global::jpeg.JpegIdentifierService();

        Assert.Throws<System.ArgumentOutOfRangeException>(delegate { identifier.IdentifyWithOptions(new byte[0], scale); });
        Assert.Throws<System.ArgumentNullException>(delegate { identifier.IdentifyWithOptions(new byte[0], region); });
    }

    [Test]
    public void Writer_RejectsEveryUnknownWritePolicyAtThePublicBoundary()
    {
        AssertWritePolicyRejected(delegate(global::jpeg.JpegWriteOptions options) { options.MetadataPolicy = (global::jpeg.JpegMetadataPolicy)99; });
        AssertWritePolicyRejected(delegate(global::jpeg.JpegWriteOptions options) { options.ColorPolicy = (global::jpeg.JpegColorPolicy)99; });
        AssertWritePolicyRejected(delegate(global::jpeg.JpegWriteOptions options) { options.IccBehaviorPolicy = (global::jpeg.JpegIccBehaviorPolicy)99; });
        AssertWritePolicyRejected(delegate(global::jpeg.JpegWriteOptions options) { options.ErrorPolicy = (global::jpeg.JpegErrorPolicy)99; });
        AssertWritePolicyRejected(delegate(global::jpeg.JpegWriteOptions options) { options.ScanLayout = (global::jpeg.JpegScanLayout)99; });
        AssertWritePolicyRejected(delegate(global::jpeg.JpegWriteOptions options) { options.Subsampling = (global::jpeg.JpegSubsampling)99; });
        AssertWritePolicyRejected(delegate(global::jpeg.JpegWriteOptions options) { options.ChromaDownsamplingFilter = (global::jpeg.JpegChromaDownsamplingFilter)99; });
        AssertWritePolicyRejected(delegate(global::jpeg.JpegWriteOptions options) { options.RestartIntervalUnit = (global::jpeg.JpegRestartIntervalUnit)99; });
    }

    private static void AssertReadPolicyRejected(System.Action<global::jpeg.JpegReadOptions> change)
    {
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        change(options);

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate { new global::jpeg.JpegIdentifierService().IdentifyWithOptions(new byte[0], options); });
    }

    private static void AssertWritePolicyRejected(System.Action<global::jpeg.JpegWriteOptions> change)
    {
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        change(options);

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                new global::jpeg.JpegWriter().WriteWithOptions(
                    JpegWriteApiTests.CreateGrayscaleImage(8, 8, 138), options);
            });
    }
}
