using raw;

namespace raw.Tests;

public class PublicResultTests
{
    [Test]
    public void CapabilitiesAreQueriedIndependently()
    {
        RawCapabilities capabilities = new RawCapabilities(false, true, false, true, false, true, false);

        Assert.Multiple(() =>
        {
            Assert.That(capabilities.Supports(RawSupportLevel.Unsupported), Is.False);
            Assert.That(capabilities.Supports(RawSupportLevel.Recognized), Is.False);
            Assert.That(capabilities.Supports(RawSupportLevel.Inspected), Is.True);
            Assert.That(capabilities.Supports(RawSupportLevel.Decoded), Is.False);
            Assert.That(capabilities.Supports(RawSupportLevel.Rendered), Is.True);
            Assert.That(capabilities.Supports(RawSupportLevel.PreservingRewrite), Is.False);
            Assert.That(capabilities.Supports(RawSupportLevel.Encoded), Is.True);
            Assert.That(capabilities.Supports(RawSupportLevel.VendorConfirmed), Is.False);
            Assert.That(capabilities.GetHighestSupportLevel(), Is.EqualTo(RawSupportLevel.Encoded));
        });
    }

    [Test]
    public void OperationResultEvaluatesCurrentDiagnostics()
    {
        RawDiagnostic warning = CreateDiagnostic(RawDiagnosticCode.FormatMismatch, RawSeverity.Warning);
        RawDiagnostic error = CreateDiagnostic(RawDiagnosticCode.TruncatedInput, RawSeverity.Error);
        RawOperationResult result = new RawOperationResult("payload", new RawDiagnostic[] { warning });

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded(), Is.True);
            Assert.That(result.HasWarnings(), Is.True);
            Assert.That(result.Value, Is.EqualTo("payload"));
        });

        result.Diagnostics = new RawDiagnostic[] { warning, error };

        Assert.That(result.Succeeded(), Is.False);
    }

    [Test]
    public void ResultContainersNormalizeNullCollections()
    {
        RawOperationResult operation = new RawOperationResult(null, null);
        RawValidationReport validation = new RawValidationReport(true, null);
        RawWritePlan plan = new RawWritePlan(true, 0, null, null, null);

        Assert.Multiple(() =>
        {
            Assert.That(operation.Diagnostics, Is.Empty);
            Assert.That(operation.Succeeded(), Is.True);
            Assert.That(validation.Messages, Is.Empty);
            Assert.That(validation.IsValid(), Is.True);
            Assert.That(plan.ProfileName, Is.Empty);
            Assert.That(plan.Changes, Is.Empty);
            Assert.That(plan.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void DiagnosticReportsOptionalLocationContext()
    {
        RawDiagnostic located = new RawDiagnostic(RawDiagnosticCode.InvalidOffset, RawSeverity.Error, RawFormat.Cr2, 42, "IFD0/Exif", 37500, "TIFF", "Invalid offset", false);
        RawDiagnostic unlocated = new RawDiagnostic(RawDiagnosticCode.UnknownFormat, RawSeverity.Error, RawFormat.Unknown, -1, null, -1, null, null, false);

        Assert.Multiple(() =>
        {
            Assert.That(located.HasOffset(), Is.True);
            Assert.That(located.HasTag(), Is.True);
            Assert.That(unlocated.HasOffset(), Is.False);
            Assert.That(unlocated.HasTag(), Is.False);
            Assert.That(unlocated.IfdPath, Is.Empty);
            Assert.That(unlocated.Stage, Is.Empty);
            Assert.That(unlocated.Message, Is.Empty);
        });
    }

    [Test]
    public void ValidationReportAndDiagnosticPreserveFindingDetails()
    {
        RawValidationMessage warning = new RawValidationMessage(RawValidationCategory.Metadata, RawDiagnosticCode.RecoveryApplied, RawSeverity.Warning, RawFormat.Nef, 128, "IFD0/Exif", "Recovered metadata", true);
        RawValidationMessage error = new RawValidationMessage(RawValidationCategory.Codec, RawDiagnosticCode.InvalidCodecData, RawSeverity.Error, RawFormat.Nef, 512, "RawIFD/Strip0", "Invalid code", false);
        RawValidationReport report = new RawValidationReport(false, new RawValidationMessage[] { warning });
        RawDiagnostic diagnostic = warning.ToDiagnostic();

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid(), Is.True);
            Assert.That(report.HasWarnings(), Is.True);
            Assert.That(diagnostic.Code, Is.EqualTo(warning.Code));
            Assert.That(diagnostic.Severity, Is.EqualTo(warning.Severity));
            Assert.That(diagnostic.Offset, Is.EqualTo(warning.Offset));
            Assert.That(diagnostic.IfdPath, Is.EqualTo(warning.Context));
            Assert.That(diagnostic.Stage, Is.EqualTo("Metadata"));
            Assert.That(diagnostic.Recovered, Is.True);
        });

        report.Messages = new RawValidationMessage[] { warning, error };

        Assert.That(report.IsValid(), Is.False);
    }

    [Test]
    public void WritePlanFindsConsentAndErrors()
    {
        RawWriteChange copied = new RawWriteChange(RawWriteChangeKind.Copied, "RawIFD/Strip0", "Copy payload", 100, 20, 200, 20, false);
        RawWriteChange dropped = new RawWriteChange(RawWriteChangeKind.Dropped, "MakerNote/Unknown", "Drop unknown data", 120, 8, -1, 0, true);
        RawDiagnostic error = CreateDiagnostic(RawDiagnosticCode.DataLossConsentRequired, RawSeverity.Error);
        RawWritePlan plan = new RawWritePlan(false, 220, "test-profile", new RawWriteChange[] { copied, dropped }, new RawDiagnostic[] { error });

        Assert.Multiple(() =>
        {
            Assert.That(plan.RequiresDataLossConsent(), Is.True);
            Assert.That(plan.HasErrors(), Is.True);
            Assert.That(plan.PredictedSize, Is.EqualTo(220));
            Assert.That(plan.ProfileName, Is.EqualTo("test-profile"));
        });
    }

    [Test]
    public void ExceptionTypesRetainMachineReadableFailureContext()
    {
        InvalidOperationException inner = new InvalidOperationException("inner");
        RawException general = new RawException(RawDiagnosticCode.Unknown, "general", -1, inner);
        RawException generalWithoutInner = RawException.Create(RawDiagnosticCode.Unknown, "general", -1);
        RawFormatException format = new RawFormatException(RawDiagnosticCode.InvalidSignature, "format", 4);
        RawLimitException limit = new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "limit", 8);
        RawUnsupportedException unsupported = new RawUnsupportedException(RawDiagnosticCode.UnsupportedCodec, "unsupported", 12);
        RawProfileException profile = new RawProfileException(RawDiagnosticCode.AmbiguousProfile, "profile", 16);
        RawWriteException write = new RawWriteException(RawDiagnosticCode.WriteFailed, "write", 20);

        Assert.Multiple(() =>
        {
            Assert.That(general.Code, Is.EqualTo(RawDiagnosticCode.Unknown));
            Assert.That(general.Offset, Is.EqualTo(-1));
            Assert.That(general.InnerException, Is.SameAs(inner));
            Assert.That(generalWithoutInner.InnerException, Is.Null);
            Assert.That(format.Code, Is.EqualTo(RawDiagnosticCode.InvalidSignature));
            Assert.That(format.Offset, Is.EqualTo(4));
            Assert.That(limit.Code, Is.EqualTo(RawDiagnosticCode.ResourceLimitExceeded));
            Assert.That(limit.Offset, Is.EqualTo(8));
            Assert.That(unsupported.Code, Is.EqualTo(RawDiagnosticCode.UnsupportedCodec));
            Assert.That(unsupported.Offset, Is.EqualTo(12));
            Assert.That(profile.Code, Is.EqualTo(RawDiagnosticCode.AmbiguousProfile));
            Assert.That(profile.Offset, Is.EqualTo(16));
            Assert.That(write.Code, Is.EqualTo(RawDiagnosticCode.WriteFailed));
            Assert.That(write.Offset, Is.EqualTo(20));
        });
    }

    private static RawDiagnostic CreateDiagnostic(RawDiagnosticCode code, RawSeverity severity)
    {
        return new RawDiagnostic(code, severity, RawFormat.GenericRaw, -1, string.Empty, -1, "test", "test message", false);
    }
}
