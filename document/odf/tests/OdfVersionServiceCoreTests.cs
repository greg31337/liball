namespace odf.Tests;

public class OdfVersionServiceCoreTests
{
    [Test]
    public void CatalogProvidesEveryAdjacentVersionDelta()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                OdfSchemaRuleCatalog.GetVersionDelta(
                    OdfVersion.Version10,
                    OdfVersion.Version11),
                Is.SameAs(Odf11Rules.DeltasFrom10));
            Assert.That(
                OdfSchemaRuleCatalog.GetVersionDelta(
                    OdfVersion.Version11,
                    OdfVersion.Version12),
                Is.SameAs(Odf12Rules.DeltasFrom11));
            Assert.That(
                OdfSchemaRuleCatalog.GetVersionDelta(
                    OdfVersion.Version12,
                    OdfVersion.Version13),
                Is.SameAs(Odf13Rules.DeltasFrom12));
            Assert.That(
                OdfSchemaRuleCatalog.GetVersionDelta(
                    OdfVersion.Version13,
                    OdfVersion.Version14),
                Is.SameAs(Odf14Rules.DeltasFrom13));
            Assert.That(
                OdfSchemaRuleCatalog.GetVersionDelta(
                    OdfVersion.Version10,
                    OdfVersion.Version14),
                Is.Null);
        });
    }

    [Test]
    public void MigrationAnalysisWalksAllVersionsFromInferredVersion10()
    {
        OdfDocument document = TextDocument();
        document.VersionInfo = null;
        foreach (OdfPart part in document.Parts)
        {
            part.VersionInfo = null;
        }

        VersionAnalysisResult result = Analyze(
            document,
            OdfVersion.Version14);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IsUpgrade, Is.True);
            Assert.That(result.IsDowngrade, Is.False);
            Assert.That(result.VersionWasInferred, Is.True);
            Assert.That(result.SourceVersion, Is.EqualTo(OdfVersion.Version10));
            Assert.That(result.TargetVersion, Is.EqualTo(OdfVersion.Version14));
            Assert.That(result.Steps, Has.Length.EqualTo(4));
            Assert.That(
                result.Steps.All(step => step.Changes.Length > 0),
                Is.True);
            Assert.That(
                result.Steps.Select(step => step.SourceVersion),
                Is.EqualTo(new[]
                {
                    OdfVersion.Version10,
                    OdfVersion.Version11,
                    OdfVersion.Version12,
                    OdfVersion.Version13
                }));
            Assert.That(
                result.Steps.Sum(
                    step => step.AddedCount
                        + step.RemovedCount
                        + step.ChangedCount),
                Is.GreaterThan(0));
        });
    }

    [Test]
    public void DowngradeReportsConcreteVersion14AttributeLoss()
    {
        OdfDocument document = TextDocument();
        OdfAttribute attribute = new OdfAttribute
        {
            Name = new OdfQualifiedName
            {
                NamespaceName = OdfNamespaces.Drawing,
                LocalName = "decorative",
                Prefix = OdfNamespaces.DrawingPrefix
            },
            Value = "true",
            Location = new OdfLocation
            {
                Line = 12,
                Column = 7
            }
        };
        OdfNode root = document.Parts[0].Root!;
        root.Attributes = [.. root.Attributes, attribute];

        VersionAnalysisResult result = Analyze(
            document,
            OdfVersion.Version13);
        OdfDiagnostic diagnostic = result.Diagnostics.Single(
            item => item.Code == OdfDiagnosticCodes.DowngradeLoss);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.IsDowngrade, Is.True);
            Assert.That(result.CanConvertWithoutLoss, Is.False);
            Assert.That(result.Steps, Has.Length.EqualTo(1));
            Assert.That(
                result.Steps[0].IncompatibleConstructCount,
                Is.EqualTo(1));
            Assert.That(diagnostic.IsRecoverable, Is.False);
            Assert.That(diagnostic.Version, Is.EqualTo(OdfVersion.Version13));
            Assert.That(diagnostic.Message, Does.Contain("decorative"));
            Assert.That(diagnostic.Location.PartPath, Is.EqualTo("content.xml"));
            Assert.That(diagnostic.Location.NodePath, Does.Contain("@draw:decorative"));
            Assert.That(diagnostic.Location.Line, Is.EqualTo(12));
            Assert.That(diagnostic.Location.Column, Is.EqualTo(7));
        });
    }

    [Test]
    public void FutureVersionIsPreservedWithoutKnownSchemaValidation()
    {
        OdfDocument document = FutureTextDocument();
        OdfValidationOptions options = new OdfValidationOptions();
        options.Mode = OdfValidationMode.XmlOnly;
        options.Version = OdfVersion.Unknown;

        ValidationPipelineResult result = new OdfValidationPipeline(options)
            .ValidateDocument(document);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.VersionValidated, Is.True);
            Assert.That(result.XmlValidated, Is.True);
            Assert.That(result.VersionAnalysis, Is.Not.Null);
            Assert.That(
                result.VersionAnalysis!.PreservesFutureContent,
                Is.True);
            Assert.That(
                result.Diagnostics.Select(item => item.Code),
                Is.EqualTo(new[] { OdfDiagnosticCodes.VersionUnsupported }));
            Assert.That(
                result.Diagnostics[0].Severity,
                Is.EqualTo(OdfValidationSeverity.Warning));
        });
    }

    [Test]
    public void FutureVersionCannotBeValidatedWhenPreservationIsDisabled()
    {
        OdfDocument document = FutureTextDocument();
        document.SaveOptions.PreserveUnknownContent = false;

        VersionAnalysisResult result = Analyze(document, OdfVersion.Future);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.PreservesFutureContent, Is.False);
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(
                result.Diagnostics[0].Code,
                Is.EqualTo(OdfDiagnosticCodes.VersionUnsupported));
            Assert.That(result.Diagnostics[0].IsRecoverable, Is.False);
        });
    }

    [Test]
    public void ExplicitFutureDowngradeStopsStrictPipelineBeforeXml()
    {
        OdfDocument document = FutureTextDocument();
        OdfValidationOptions options = new OdfValidationOptions();
        options.Mode = OdfValidationMode.Strict;
        options.ValidatePackage = false;
        options.Version = OdfVersion.Version14;

        ValidationPipelineResult result = new OdfValidationPipeline(options)
            .ValidateDocument(document);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.StoppedEarly, Is.True);
            Assert.That(result.VersionValidated, Is.True);
            Assert.That(result.XmlValidated, Is.False);
            Assert.That(result.SemanticsValidated, Is.False);
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(
                result.Diagnostics[0].Code,
                Is.EqualTo(OdfDiagnosticCodes.DowngradeLoss));
            Assert.That(result.Diagnostics[0].IsRecoverable, Is.False);
        });
    }

    [Test]
    public void FutureDowngradeRemainsAnErrorInRecoveryMode()
    {
        OdfDocument document = FutureTextDocument();
        OdfValidationOptions options = new OdfValidationOptions();
        options.Mode = OdfValidationMode.Recovery;
        options.ValidatePackage = false;
        options.Version = OdfVersion.Version14;

        ValidationPipelineResult result = new OdfValidationPipeline(options)
            .ValidateDocument(document);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.RecoveredDiagnosticCount, Is.Zero);
            Assert.That(
                result.Diagnostics.Single(
                    item => item.Code == OdfDiagnosticCodes.DowngradeLoss)
                    .Severity,
                Is.EqualTo(OdfValidationSeverity.Error));
        });
    }

    [Test]
    public void PartVersionMismatchProducesLocatedGateDiagnostic()
    {
        OdfDocument document = TextDocument();
        document.Parts[0].VersionInfo = OdfVersionRules.ParseVersion("1.3");

        VersionAnalysisResult result = Analyze(
            document,
            OdfVersion.Version14);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(
                result.Diagnostics[0].Code,
                Is.EqualTo(OdfDiagnosticCodes.VersionUnsupported));
            Assert.That(
                result.Diagnostics[0].Location.PartPath,
                Is.EqualTo("content.xml"));
        });
    }

    static VersionAnalysisResult Analyze(
        OdfDocument document,
        OdfVersion targetVersion)
    {
        return new OdfVersionServiceCore(new OdfValidationOptions())
            .AnalyzeMigration(document, targetVersion);
    }

    static OdfDocument TextDocument()
    {
        return new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Text);
    }

    static OdfDocument FutureTextDocument()
    {
        OdfDocument result = TextDocument();
        OdfVersionInfo version = OdfVersionRules.ParseVersion("1.5");
        result.VersionInfo = version;
        result.IsFutureVersion = true;
        foreach (OdfPart part in result.Parts)
        {
            part.VersionInfo = OdfVersionRules.ParseVersion("1.5");
            part.IsFutureVersion = true;
        }
        return result;
    }
}
