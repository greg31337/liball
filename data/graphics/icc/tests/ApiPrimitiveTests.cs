using icc;

namespace icc.Tests;

public class ApiPrimitiveTests
{
    [Test]
    public void ResourceLimitsUseConservativePositiveDefaults()
    {
        IccResourceLimits limits = new IccResourceLimits();

        Assert.Multiple(() =>
        {
            Assert.That(limits.MaximumProfileBytes, Is.EqualTo(64 * 1024 * 1024));
            Assert.That(limits.MaximumCarrierBytes, Is.EqualTo(64 * 1024 * 1024));
            Assert.That(limits.MaximumTagCount, Is.EqualTo(4096));
            Assert.That(limits.MaximumTagBytes, Is.EqualTo(32 * 1024 * 1024));
            Assert.That(limits.MaximumTotalTagBytes, Is.EqualTo(64 * 1024 * 1024));
            Assert.That(limits.MaximumNestingDepth, Is.EqualTo(32));
            Assert.That(limits.MaximumDecompressedBytes, Is.EqualTo(64 * 1024 * 1024));
            Assert.That(limits.MaximumCalculatorInstructions, Is.EqualTo(100000));
            Assert.That(limits.MaximumCalculatorStackDepth, Is.EqualTo(1024));
            Assert.That(limits.MaximumCalculatorStorageValues, Is.EqualTo(65536));
            Assert.That(limits.MaximumDiagnosticCount, Is.EqualTo(1024));
        });
    }

    [Test]
    public void InspectionEmbeddingAndTransformOptionsHaveStableDefaults()
    {
        IccInspectionOptions inspection = new IccInspectionOptions();
        IccEmbeddingOptions embedding = new IccEmbeddingOptions();
        IccTransformOptions transform = new IccTransformOptions();
        IccProofingOptions proofing = new IccProofingOptions();

        Assert.Multiple(() =>
        {
            Assert.That(inspection.Detail,
                Is.EqualTo(IccInspectionDetail.Normal));
            Assert.That(inspection.MaximumDepth, Is.EqualTo(32));
            Assert.That(inspection.MaximumChildren, Is.EqualTo(1024));
            Assert.That(inspection.MaximumRawBytes, Is.EqualTo(256));
            Assert.That(inspection.IncludeRawBytes, Is.False);
            Assert.That(embedding.Format,
                Is.EqualTo(IccEmbeddedFormat.Unknown));
            Assert.That(embedding.ProfileIndex, Is.Zero);
            Assert.That(embedding.AllowCompression, Is.True);
            Assert.That(embedding.StrictCarrierValidation, Is.True);
            Assert.That(embedding.ResourceLimits, Is.Not.Null);
            Assert.That(transform.Direction,
                Is.EqualTo(IccTransformDirection.DeviceToPcs));
            Assert.That(transform.RenderingIntent,
                Is.EqualTo(IccRenderingIntent.Perceptual));
            Assert.That(transform.IntentFallback,
                Is.EqualTo(IccIntentFallback.None));
            Assert.That(transform.Interpolation,
                Is.EqualTo(IccInterpolationKind.Multilinear));
            Assert.That(transform.PcsEncoding,
                Is.EqualTo(IccPcsEncodingKind.Normalized));
            Assert.That(transform.BlackPointCompensation, Is.False);
            Assert.That(transform.ClipOutput, Is.True);
            Assert.That(transform.EnableTrace, Is.False);
            Assert.That(proofing.RenderingIntent,
                Is.EqualTo(IccRenderingIntent.Perceptual));
            Assert.That(proofing.ProofingIntent,
                Is.EqualTo(IccRenderingIntent.MediaRelativeColorimetric));
            Assert.That(proofing.IntentFallback,
                Is.EqualTo(IccIntentFallback.None));
            Assert.That(proofing.Interpolation,
                Is.EqualTo(IccInterpolationKind.Multilinear));
            Assert.That(proofing.EnableGamutCheck, Is.False);
            Assert.That(proofing.AlarmColor, Is.Empty);
            Assert.That(proofing.ClipOutput, Is.True);
            Assert.That(proofing.EnableTrace, Is.False);
        });
    }

    [Test]
    public void ConstructionLinkAndValidationOptionsHaveStableDefaults()
    {
        IccProfileBuildOptions build = new IccProfileBuildOptions();
        IccProfileLinkOptions link = new IccProfileLinkOptions();
        IccValidationPolicy validation = new IccValidationPolicy();

        Assert.Multiple(() =>
        {
            Assert.That(build.Version.Kind,
                Is.EqualTo(IccVersionKind.Version4));
            Assert.That(build.Version.Major, Is.EqualTo(4));
            Assert.That(build.ProfileClass,
                Is.EqualTo(IccProfileClass.Display));
            Assert.That(build.RenderingIntent,
                Is.EqualTo(IccRenderingIntent.Perceptual));
            Assert.That(build.DataColorSpaceSignature,
                Is.EqualTo(IccColorSpaceSignatures.Rgb));
            Assert.That(build.ProfileConnectionSpaceSignature,
                Is.EqualTo(IccColorSpaceSignatures.Xyz));
            Assert.That(build.Descriptions, Is.Empty);
            Assert.That(build.Copyright, Is.Not.Null);
            Assert.That(build.MediaWhitePoint.X,
                Is.EqualTo(IccStandardColorimetry.D50X));
            Assert.That(build.MediaWhitePoint.Y,
                Is.EqualTo(IccStandardColorimetry.D50Y));
            Assert.That(build.MediaWhitePoint.Z,
                Is.EqualTo(IccStandardColorimetry.D50Z));
            Assert.That(build.ChromaticAdaptationMatrix, Is.Null);
            Assert.That(build.CreationDateTime.Year, Is.EqualTo(2000));
            Assert.That(build.CreationDateTime.Month, Is.EqualTo(1));
            Assert.That(build.CreationDateTime.Day, Is.EqualTo(1));
            Assert.That(build.RgbDefinition, Is.Null);
            Assert.That(build.GrayDefinition, Is.Null);
            Assert.That(build.AbstractDefinition, Is.Null);
            Assert.That(build.AdaptiveGainCurve, Is.Null);
            Assert.That(build.NamedColorDefinition, Is.Null);
            Assert.That(build.OutputDefinition, Is.Null);
            Assert.That(build.ColorEncodingDefinition, Is.Null);
            Assert.That(link.RenderingIntent,
                Is.EqualTo(IccRenderingIntent.Perceptual));
            Assert.That(link.IntentFallback,
                Is.EqualTo(IccIntentFallback.None));
            Assert.That(link.Version.Kind,
                Is.EqualTo(IccVersionKind.Version4));
            Assert.That(link.Version.Major, Is.EqualTo(4));
            Assert.That(link.GridPointCount, Is.EqualTo(17));
            Assert.That(link.EncodingTypeSignature,
                Is.EqualTo(IccTypeSignatures.MultiProcessElements));
            Assert.That(link.Interpolation,
                Is.EqualTo(IccInterpolationKind.Multilinear));
            Assert.That(link.Descriptions, Is.Empty);
            Assert.That(link.IncludeProfileSequence, Is.True);
            Assert.That(validation.Mode,
                Is.EqualTo(IccValidationMode.Conforming));
            Assert.That(validation.RequiredRuleIds, Is.Empty);
            Assert.That(validation.SuppressedRuleIds, Is.Empty);
            Assert.That(validation.AllowPrivateSignatures, Is.True);
            Assert.That(validation.Icc2RequiredTagSignatures, Is.Empty);
            Assert.That(validation.Icc2OptionalTagSignatures, Is.Empty);
            Assert.That(validation.Icc2ForbiddenTagSignatures, Is.Empty);
            Assert.That(validation.Icc2ForbidUnlistedTags, Is.False);
            Assert.That(validation.Icc2SupportedProcessingElementSignatures,
                Is.Empty);
            Assert.That(validation.Icc2ForbidUnlistedProcessingElements,
                Is.False);
            Assert.That(validation.Icc2ChannelRules, Is.Empty);
            Assert.That(validation.Icc2RequireProfileSubclass, Is.False);
            Assert.That(validation.Icc2ProfileSubclassRules, Is.Empty);
            Assert.That(validation.ResourceLimits, Is.Not.Null);
        });
    }

    [Test]
    public void ReadOptionsDefaultToStrictOwnedAndBoundedInput()
    {
        IccReadOptions options = new IccReadOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.Mode, Is.EqualTo(IccReadMode.Strict));
            Assert.That(options.ValidationMode, Is.EqualTo(IccValidationMode.SecurityStrict));
            Assert.That(options.ResourceLimits, Is.Not.Null);
            Assert.That(options.CopyInputBytes, Is.True);
            Assert.That(options.DecodeTagsLazily, Is.True);
            Assert.That(options.RequireExactProfileSize, Is.True);
            Assert.That(options.PreserveOriginalBytes, Is.True);
        });
    }

    [Test]
    public void WriteOptionsDefaultToCanonicalLosslessOutput()
    {
        IccWriteOptions options = new IccWriteOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.Mode, Is.EqualTo(IccWriteMode.Canonical));
            Assert.That(options.ResourceLimits, Is.Not.Null);
            Assert.That(options.InternIdenticalTagPayloads, Is.True);
            Assert.That(options.ComputeProfileId, Is.True);
            Assert.That(options.PreserveOriginalCreationDateTime, Is.True);
            Assert.That(options.PreserveUnknownData, Is.True);
            Assert.That(options.RejectInvalidProfile, Is.True);
        });
    }

    [Test]
    public void OptionsOwnIndependentResourceLimits()
    {
        IccReadOptions first = new IccReadOptions();
        IccReadOptions second = new IccReadOptions();
        IccWriteOptions writer = new IccWriteOptions();

        first.ResourceLimits.MaximumTagCount = 1;

        Assert.Multiple(() =>
        {
            Assert.That(second.ResourceLimits.MaximumTagCount, Is.EqualTo(4096));
            Assert.That(writer.ResourceLimits.MaximumTagCount, Is.EqualTo(4096));
            Assert.That(first.ResourceLimits, Is.Not.SameAs(second.ResourceLimits));
            Assert.That(first.ResourceLimits, Is.Not.SameAs(writer.ResourceLimits));
        });
    }

    [Test]
    public void OperationResultCannotDefaultToSuccess()
    {
        IccOperationResult result = new IccOperationResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(IccOperationStatus.Unspecified));
            Assert.That(result.Message, Is.Empty);
            Assert.That(result.Location, Is.Not.Null);
            Assert.That(result.Location.Offset, Is.EqualTo(-1));
            Assert.That(result.Location.Length, Is.Zero);
        });
    }

    [Test]
    public void OperationResultCarriesExpectedFailureDetailsWithoutAnException()
    {
        IccOperationResult result = new IccOperationResult();
        result.Status = IccOperationStatus.ResourceLimitExceeded;
        result.Message = "Profile exceeds the configured size limit.";
        result.Location.Offset = 128;
        result.Location.Length = 12;

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(IccOperationStatus.ResourceLimitExceeded));
            Assert.That(result.Message, Is.EqualTo("Profile exceeds the configured size limit."));
            Assert.That(result.Location.Offset, Is.EqualTo(128));
            Assert.That(result.Location.Length, Is.EqualTo(12));
        });
    }
}
