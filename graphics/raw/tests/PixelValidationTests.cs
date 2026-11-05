using raw;

namespace raw.Tests;

public class PixelValidationTests
{
    [Test]
    public void Cr2ReopenReportPerformsIdentityAndRejectsDifferentStoredPixels()
    {
        RawWriteProfile profile = Cr2EncoderTests.SmallProfile();
        RawWriteOptions options = Cr2EncoderTests.Options();
        RawCr2WriteInput input = Cr2EncoderTests.Input(8, 4, 8);
        RawCr2WriteInput changed = Cr2EncoderTests.Input(8, 4, 8);
        changed.StoredSamples[7]++;
        byte[] encoded = Cr2Encoder.Encode(input, profile, options);
        byte[] changedOutput = Cr2Encoder.Encode(changed, profile, options);

        RawPixelValidationReport report = Cr2Encoder.ValidateOutput(encoded, input, profile, options);
        RawWritePlan plan = Cr2Encoder.Plan(input, profile, options);
        RawWriteException? exception = Assert.Throws<RawWriteException>(() => Cr2Encoder.ValidateOutput(changedOutput, input, profile, options));

        Assert.Multiple(() =>
        {
            Assert.That(report.Kind, Is.EqualTo(RawPixelValidationKind.Identity));
            Assert.That(report.Required, Is.True);
            Assert.That(report.Performed, Is.True);
            Assert.That(report.Passed, Is.True);
            Assert.That(report.SampleCount, Is.EqualTo(32));
            Assert.That(report.MaximumAbsoluteError, Is.Zero);
            Assert.That(report.AllowedMaximumAbsoluteError, Is.Zero);
            Assert.That(plan.PixelValidation.Kind, Is.EqualTo(RawPixelValidationKind.Identity));
            Assert.That(plan.PixelValidation.Required, Is.True);
            Assert.That(plan.PixelValidation.Performed, Is.False);
            Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.OutputValidationFailed));
        });
    }

    [Test]
    public void UncompressedNefReopenReportPerformsStoredPixelIdentity()
    {
        RawWriteProfile profile = NefEncoderTests.SmallProfile();
        RawWriteOptions options = NefEncoderTests.Options();
        RawNefWriteInput input = NefEncoderTests.Input(8, 4, 8);
        byte[] encoded = NefEncoder.Encode(input, profile, options);

        RawPixelValidationReport report = NefEncoder.ValidateOutput(encoded, input, profile, options);
        RawWritePlan plan = NefEncoder.Plan(input, profile, options);

        Assert.Multiple(() =>
        {
            Assert.That(report.Kind, Is.EqualTo(RawPixelValidationKind.Identity));
            Assert.That(report.Performed, Is.True);
            Assert.That(report.Passed, Is.True);
            Assert.That(report.SampleCount, Is.EqualTo(32));
            Assert.That(report.MaximumAbsoluteError, Is.Zero);
            Assert.That(report.Details, Does.Contain("identical"));
            Assert.That(plan.PixelValidation.Kind, Is.EqualTo(RawPixelValidationKind.Identity));
            Assert.That(plan.PixelValidation.AllowedMaximumAbsoluteError, Is.Zero);
        });
    }

    [Test]
    public void LossyNefPlanAndReopenReportExposeObservedAndAllowedErrorBounds()
    {
        RawProfileCatalogue catalogue = NefEncoderTests.QualifiedLossyCatalogue();
        RawWriteProfile? profile = catalogue.FindWriteProfile("test.nikon.nef.lossy.write");
        RawNefWriteInput input = NefEncoderTests.ExtremeInput(8, 4, 8);
        RawWriteOptions options = NefEncoderTests.Options();
        options.AllowExperimentalProfiles = false;
        options.ProfileName = (profile ?? throw new System.InvalidOperationException("A required value is unavailable.")).Id;
        options.AllowLossy = true;
        options.DataLossConsent = true;
        byte[] encoded = NefEncoder.Encode(input, profile, options);

        RawPixelValidationReport report = NefEncoder.ValidateOutput(encoded, input, profile, options);
        RawWritePlan plan = RawWriter.PlanNef(input, catalogue, options);

        Assert.Multiple(() =>
        {
            Assert.That(report.Kind, Is.EqualTo(RawPixelValidationKind.ErrorBound));
            Assert.That(report.Required, Is.True);
            Assert.That(report.Performed, Is.True);
            Assert.That(report.Passed, Is.True);
            Assert.That(report.SampleCount, Is.EqualTo(32));
            Assert.That(report.MaximumAbsoluteError, Is.EqualTo(1));
            Assert.That(report.AllowedMaximumAbsoluteError, Is.EqualTo(1));
            Assert.That(plan.PixelValidation.Kind, Is.EqualTo(RawPixelValidationKind.ErrorBound));
            Assert.That(plan.PixelValidation.Required, Is.True);
            Assert.That(plan.PixelValidation.Performed, Is.False);
            Assert.That(plan.PixelValidation.MaximumAbsoluteError, Is.EqualTo(-1));
            Assert.That(plan.PixelValidation.AllowedMaximumAbsoluteError, Is.EqualTo(1));
            Assert.That(plan.PixelValidation.Details, Does.Contain("declared absolute-error bound"));
        });
    }

    [Test]
    public void DisabledLosslessCheckIsExplicitlyReportedAsNotPerformed()
    {
        RawWriteProfile profile = Cr2EncoderTests.SmallProfile();
        RawWriteOptions options = Cr2EncoderTests.Options();
        options.VerifyPixelIdentity = false;
        RawCr2WriteInput input = Cr2EncoderTests.Input(8, 4, 8);
        byte[] encoded = Cr2Encoder.Encode(input, profile, options);

        RawPixelValidationReport report = Cr2Encoder.ValidateOutput(encoded, input, profile, options);
        RawWritePlan plan = Cr2Encoder.Plan(input, profile, options);

        Assert.Multiple(() =>
        {
            Assert.That(report.Kind, Is.EqualTo(RawPixelValidationKind.Identity));
            Assert.That(report.Required, Is.False);
            Assert.That(report.Performed, Is.False);
            Assert.That(report.Passed, Is.False);
            Assert.That(report.MaximumAbsoluteError, Is.EqualTo(-1));
            Assert.That(plan.PixelValidation.Required, Is.False);
        });
    }
}
