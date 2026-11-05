using raw;

namespace raw.Tests;

public class OptionTests
{
    [Test]
    public void DefaultLimitsArePositiveConsistentAndBounded()
    {
        RawLimits limits = new RawLimits();
        RawValidationReport report = limits.Validate();

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid(), Is.True);
            Assert.That(limits.MaxFileSizeBytes, Is.GreaterThan(0));
            Assert.That(limits.MaxPixelCount, Is.GreaterThan(0));
            Assert.That(limits.MaxMetadataBytes, Is.LessThanOrEqualTo(limits.MaxFileSizeBytes));
            Assert.That(limits.MaxXmpBytes, Is.LessThanOrEqualTo(limits.MaxMetadataBytes));
            Assert.That(limits.MaxAllocationBytes, Is.LessThanOrEqualTo(limits.MaxDecodedBytes));
        });
    }

    [Test]
    public void LimitsValidationReportsEveryInvalidRelationship()
    {
        RawLimits limits = new RawLimits();
        limits.MaxWidth = 0;
        limits.MaxMetadataBytes = 1024;
        limits.MaxTagValueBytes = 2048;
        limits.MaxXmpBytes = 4096;
        limits.MaxDecodedBytes = 8192;
        limits.MaxAllocationBytes = 16384;

        RawValidationReport report = limits.Validate();
        string[]? contexts = (report.Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Select(message => message.Context).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid(), Is.False);
            Assert.That(contexts, Does.Contain("Limits.MaxWidth"));
            Assert.That(contexts, Does.Contain("Limits.MaxTagValueBytes"));
            Assert.That(contexts, Does.Contain("Limits.MaxXmpBytes"));
            Assert.That(contexts, Does.Contain("Limits.MaxAllocationBytes"));
        });
    }

    [Test]
    public void EveryDefaultOptionSetIsValidAndConservative()
    {
        RawOpenOptions open = new RawOpenOptions();
        RawDecodeOptions decode = new RawDecodeOptions();
        RawRenderOptions render = new RawRenderOptions();
        RawMetadataOptions metadata = new RawMetadataOptions();
        RawWriteOptions write = new RawWriteOptions();
        RawValidationOptions validation = new RawValidationOptions();

        Assert.Multiple(() =>
        {
            Assert.That(open.Validate().IsValid(), Is.True);
            Assert.That(open.DeferPayloads, Is.True);
            Assert.That(open.Tolerant, Is.False);
            Assert.That(open.AllowExperimentalProfiles, Is.False);
            Assert.That(decode.Validate().IsValid(), Is.True);
            Assert.That(decode.NormalizeSamples, Is.True);
            Assert.That(decode.AllowExperimentalProfiles, Is.False);
            Assert.That(render.Validate().IsValid(), Is.True);
            Assert.That(render.DemosaicMode, Is.EqualTo(RawDemosaicMode.Bilinear));
            Assert.That(render.ChromaticAdaptation, Is.EqualTo(RawChromaticAdaptationMode.Bradford));
            Assert.That(metadata.Validate().IsValid(), Is.True);
            Assert.That(metadata.PreserveRawPackets, Is.True);
            Assert.That(metadata.Reconcile, Is.False);
            Assert.That(write.Validate().IsValid(), Is.True);
            Assert.That(write.Mode, Is.EqualTo(RawWriteMode.PreservingRewrite));
            Assert.That(write.UnknownDataPolicy, Is.EqualTo(RawUnknownDataPolicy.Preserve));
            Assert.That(write.AllowLossy, Is.False);
            Assert.That(write.ValidateAfterWrite, Is.True);
            Assert.That(write.VerifyPixelIdentity, Is.True);
            Assert.That(validation.Validate().IsValid(), Is.True);
            Assert.That(validation.Strict, Is.True);
            Assert.That(validation.AllowRecovery, Is.False);
        });
    }

    [Test]
    public void OptionInstancesDoNotShareMutableLimits()
    {
        RawOpenOptions first = new RawOpenOptions();
        RawOpenOptions second = new RawOpenOptions();
        long secondFileLimit = (second.Limits ?? throw new System.InvalidOperationException("A required value is unavailable.")).MaxFileSizeBytes;

        (first.Limits ?? throw new System.InvalidOperationException("A required value is unavailable.")).MaxFileSizeBytes = 1;

        Assert.Multiple(() =>
        {
            Assert.That(first.Limits, Is.Not.SameAs(second.Limits));
            Assert.That(second.Limits.MaxFileSizeBytes, Is.EqualTo(secondFileLimit));
        });
    }

    [Test]
    public void DecodeValidationRejectsNegativeSelectionsAndMissingLimits()
    {
        RawDecodeOptions options = new RawDecodeOptions();
        options.Limits = null;
        options.ImageIndex = -1;
        options.PlaneIndex = -2;

        RawValidationReport report = options.Validate();
        string[]? contexts = (report.Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Select(message => message.Context).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid(), Is.False);
            Assert.That(contexts, Does.Contain("Limits"));
            Assert.That(contexts, Does.Contain("Decode.ImageIndex"));
            Assert.That(contexts, Does.Contain("Decode.PlaneIndex"));
        });
    }

    [Test]
    public void RenderValidationRejectsInvalidModesGeometryAndNumbers()
    {
        RawRenderOptions options = new RawRenderOptions();
        options.DemosaicMode = (RawDemosaicMode)999;
        options.ChromaticAdaptation = (RawChromaticAdaptationMode)999;
        options.OutputBitDepth = 12;
        options.OutputWidth = 100;
        options.OutputHeight = 0;
        options.ExposureCompensation = double.NaN;
        options.ToneCurve = new double[] { 0.0, 0.8, 0.7 };
        options.Contrast = -1.0;
        options.GamutMode = (RawGamutMode)999;
        options.Gamma = 0.0;

        RawValidationReport report = options.Validate();
        string[]? contexts = (report.Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Select(message => message.Context).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid(), Is.False);
            Assert.That(contexts, Does.Contain("Render.DemosaicMode"));
            Assert.That(contexts, Does.Contain("Render.ChromaticAdaptation"));
            Assert.That(contexts, Does.Contain("Render.OutputBitDepth"));
            Assert.That(contexts, Does.Contain("Render.OutputDimensions"));
            Assert.That(contexts, Does.Contain("Render.ExposureCompensation"));
            Assert.That(contexts, Does.Contain("Render.ToneCurve"));
            Assert.That(contexts, Does.Contain("Render.Contrast"));
            Assert.That(contexts, Does.Contain("Render.GamutMode"));
            Assert.That(contexts, Does.Contain("Render.Gamma"));
        });
    }

    [Test]
    public void MetadataValidationEnforcesExplicitReconciliationPolicy()
    {
        RawMetadataOptions options = new RawMetadataOptions();
        options.Reconcile = true;
        options.Precedence = RawMetadataPrecedence.None;

        Assert.That(options.Validate().IsValid(), Is.False);

        options.Precedence = RawMetadataPrecedence.Exif;

        Assert.That(options.Validate().IsValid(), Is.True);
    }

    [Test]
    public void WriteValidationRequiresConsentForIrreversibleChoices()
    {
        RawWriteOptions options = new RawWriteOptions();
        options.UnknownDataPolicy = RawUnknownDataPolicy.DropWithConsent;
        options.AllowLossy = true;

        Assert.That(options.Validate().IsValid(), Is.False);

        options.DataLossConsent = true;

        Assert.That(options.Validate().IsValid(), Is.True);
    }

    [Test]
    public void ValidationOptionsRejectRecoveryInStrictModeAndEmptySelection()
    {
        RawValidationOptions options = new RawValidationOptions();
        options.AllowRecovery = true;
        options.ValidateStructure = false;
        options.ValidateCodec = false;
        options.ValidateSemantic = false;
        options.ValidateProfile = false;
        options.ValidateMetadata = false;
        options.ValidateWriter = false;

        RawValidationReport report = options.Validate();
        string[]? contexts = (report.Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Select(message => message.Context).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid(), Is.False);
            Assert.That(contexts, Does.Contain("Validation.Selection"));
            Assert.That(contexts, Does.Contain("Validation.AllowRecovery"));
        });

        options.Strict = false;
        options.ValidateStructure = true;

        Assert.That(options.Validate().IsValid(), Is.True);
    }

    [Test]
    public void EveryOperationOptionChecksCancellationSynchronously()
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();
        RawOpenOptions open = new RawOpenOptions();
        RawDecodeOptions decode = new RawDecodeOptions();
        RawRenderOptions render = new RawRenderOptions();
        RawMetadataOptions metadata = new RawMetadataOptions();
        RawWriteOptions write = new RawWriteOptions();
        RawValidationOptions validation = new RawValidationOptions();
        open.CancellationToken = source.Token;
        decode.CancellationToken = source.Token;
        render.CancellationToken = source.Token;
        metadata.CancellationToken = source.Token;
        write.CancellationToken = source.Token;
        validation.CancellationToken = source.Token;

        Assert.Multiple(() =>
        {
            Assert.Throws<OperationCanceledException>(() => open.CheckCancellation());
            Assert.Throws<OperationCanceledException>(() => decode.CheckCancellation());
            Assert.Throws<OperationCanceledException>(() => render.CheckCancellation());
            Assert.Throws<OperationCanceledException>(() => metadata.CheckCancellation());
            Assert.Throws<OperationCanceledException>(() => write.CheckCancellation());
            Assert.Throws<OperationCanceledException>(() => validation.CheckCancellation());
        });
    }
}
