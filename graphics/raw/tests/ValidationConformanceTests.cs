using raw;

namespace raw.Tests;

public class ValidationConformanceTests
{
    [Test]
    public void ReportsSummarizeEveryCategoryAndStrictWarningsRejectConformance()
    {
        RawValidationMessage information = new RawValidationMessage(RawValidationCategory.Structural, RawDiagnosticCode.None, RawSeverity.Information, RawFormat.Tiff, 0, "TIFF", "Structure inspected.", false);
        RawValidationMessage warning = new RawValidationMessage(RawValidationCategory.Metadata, RawDiagnosticCode.RecoveryApplied, RawSeverity.Warning, RawFormat.Tiff, 24, "IFD0/XMP", "Named recovery applied.", true);
        RawValidationReport strict = new RawValidationReport(true, new RawValidationMessage[] { information, warning });
        RawValidationReport tolerant = new RawValidationReport(false, new RawValidationMessage[] { information, warning });

        Assert.Multiple(() =>
        {
            Assert.That(((strict.Categories ?? throw new System.InvalidOperationException("A required value is unavailable.")).Select(value => value.Category) ?? throw new System.InvalidOperationException("A required value is unavailable.")), Is.EqualTo(new RawValidationCategory[]
            {
                RawValidationCategory.Structural,
                RawValidationCategory.Codec,
                RawValidationCategory.Semantic,
                RawValidationCategory.Profile,
                RawValidationCategory.Metadata,
                RawValidationCategory.Writer
            }));
            Assert.That((strict.GetCategory(RawValidationCategory.Structural) ?? throw new System.InvalidOperationException("A required value is unavailable.")).InformationCount, Is.EqualTo(1));
            Assert.That((strict.GetCategory(RawValidationCategory.Metadata) ?? throw new System.InvalidOperationException("A required value is unavailable.")).WarningCount, Is.EqualTo(1));
            Assert.That((strict.GetCategory(RawValidationCategory.Metadata) ?? throw new System.InvalidOperationException("A required value is unavailable.")).RecoveredCount, Is.EqualTo(1));
            Assert.That((strict.GetCategory(RawValidationCategory.Metadata) ?? throw new System.InvalidOperationException("A required value is unavailable.")).IsConformant(true), Is.False);
            Assert.That(strict.IsValid(), Is.True);
            Assert.That(strict.IsConformant(), Is.False);
            Assert.That(tolerant.IsConformant(), Is.True);
        });
    }

    [Test]
    public void ProfileOnlyValidationRejectsANameWithoutAnAttachedDefinition()
    {
        RawDescriptor descriptor = RawDescriptor.CreateGray(2, 1, 8, 8, RawByteOrder.LittleEndian);
        RawDocument document = RawCodec.Open(new byte[] { 1, 2 }, descriptor, new RawOpenOptions());
        document.ProfileName = "detached-profile";
        RawValidationOptions options = Select(false, false, false, true, false, false);

        RawValidationReport report = document.Validate(options);

        Assert.Multiple(() =>
        {
            Assert.That(report.Messages, Has.Length.EqualTo(1));
            Assert.That((report.Messages ?? throw new System.InvalidOperationException("A required value is unavailable."))[0].Category, Is.EqualTo(RawValidationCategory.Profile));
            Assert.That(report.Messages[0].Code, Is.EqualTo(RawDiagnosticCode.InvalidProfile));
            Assert.That((report.GetCategory(RawValidationCategory.Profile) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Selected, Is.True);
            Assert.That((report.GetCategory(RawValidationCategory.Profile) ?? throw new System.InvalidOperationException("A required value is unavailable.")).ErrorCount, Is.EqualTo(1));
            Assert.That(((report.Categories ?? throw new System.InvalidOperationException("A required value is unavailable.")).Where(value => value.Category != RawValidationCategory.Profile).All(value => !value.Selected)), Is.True);
        });
    }

    [Test]
    public void NamedRecoveriesAreWarningsOnlyWhenTolerantRecoveryWasRequested()
    {
        RawDescriptor descriptor = RawDescriptor.CreateGray(2, 1, 8, 8, RawByteOrder.LittleEndian);
        RawDocument document = RawCodec.Open(new byte[] { 1, 2 }, descriptor, new RawOpenOptions());
        document.Diagnostics = new RawDiagnostic[]
        {
            new RawDiagnostic(RawDiagnosticCode.RecoveryApplied, RawSeverity.Warning, RawFormat.Cr2, 64, "IFD1", 513, "Cr2Parser", "The malformed optional thumbnail was omitted.", true)
        };
        RawValidationOptions strictOptions = Select(true, false, false, false, false, false);
        RawValidationOptions tolerantOptions = Select(true, false, false, false, false, false);
        tolerantOptions.Strict = false;
        tolerantOptions.AllowRecovery = true;

        RawValidationReport strict = document.Validate(strictOptions);
        RawValidationReport tolerant = document.Validate(tolerantOptions);
        document.Diagnostics[0].Code = RawDiagnosticCode.FormatMismatch;
        RawValidationReport unnamed = document.Validate(tolerantOptions);

        Assert.Multiple(() =>
        {
            Assert.That(((strict.Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(value => value.Recovered).Severity), Is.EqualTo(RawSeverity.Error));
            Assert.That(strict.IsValid(), Is.False);
            Assert.That(strict.IsConformant(), Is.False);
            Assert.That(((tolerant.Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(value => value.Recovered).Severity), Is.EqualTo(RawSeverity.Warning));
            Assert.That((tolerant.GetCategory(RawValidationCategory.Structural) ?? throw new System.InvalidOperationException("A required value is unavailable.")).RecoveredCount, Is.EqualTo(1));
            Assert.That(tolerant.IsValid(), Is.True);
            Assert.That(tolerant.IsConformant(), Is.True);
            Assert.That(((unnamed.Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(value => value.Recovered).Code), Is.EqualTo(RawDiagnosticCode.ValidationFailed));
            Assert.That(unnamed.IsValid(), Is.False);
        });
    }

    [Test]
    public void WritePlanValidationChecksProfileEvidenceLayoutAndPixelGuarantees()
    {
        RawWriteCompatibilityEntry compatibility = new RawWriteCompatibilityEntry("writer-a", RawFormat.Cr2, "Independent App", "1.2.3", "sample-a", "Open and render", RawProfileEvidenceResult.Passed, "Accepted", false);
        RawWriteChange change = new RawWriteChange(RawWriteChangeKind.Copied, "IFD3/Strip0", "Copy raw payload", 16, 8, 8, 8, false);
        RawWritePlan plan = RawWritePlan.CreateWithCompatibility(true, 16, "writer-a", new RawWriteCompatibilityEntry[] { compatibility }, new RawWriteChange[] { change }, null);
        RawValidationOptions options = Select(false, false, false, true, false, true);

        RawValidationReport valid = plan.Validate(options);
        compatibility.ProfileId = "writer-b";
        change.PlannedLength = 9;
        plan.PixelValidation = new RawPixelValidationReport(RawPixelValidationKind.Identity, true, true, false, 4, 1, 0, "Mismatch");
        RawValidationReport invalid = RawCodec.Validate(plan, options);

        Assert.Multiple(() =>
        {
            Assert.That(valid.IsValid(), Is.True);
            Assert.That(valid.IsConformant(), Is.True);
            Assert.That((valid.GetCategory(RawValidationCategory.Profile) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Selected, Is.True);
            Assert.That((valid.GetCategory(RawValidationCategory.Writer) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Selected, Is.True);
            Assert.That(((invalid.Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Any(value => value.Category == RawValidationCategory.Profile && value.Code == RawDiagnosticCode.InvalidProfile)), Is.True);
            Assert.That(invalid.Messages.Any(value => value.Category == RawValidationCategory.Writer && value.Code == RawDiagnosticCode.InvalidOffset), Is.True);
            Assert.That(invalid.Messages.Any(value => value.Category == RawValidationCategory.Writer && value.Code == RawDiagnosticCode.OutputValidationFailed), Is.True);
            Assert.That(invalid.IsValid(), Is.False);
        });
    }

    private static RawValidationOptions Select(bool structure, bool codec, bool semantic, bool profile, bool metadata, bool writer)
    {
        RawValidationOptions options = new RawValidationOptions();
        options.ValidateStructure = structure;
        options.ValidateCodec = codec;
        options.ValidateSemantic = semantic;
        options.ValidateProfile = profile;
        options.ValidateMetadata = metadata;
        options.ValidateWriter = writer;
        return options;
    }
}
