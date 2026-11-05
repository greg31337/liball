namespace odf.Tests;

public class OdfValidationPipelineTests
{
    [Test]
    public void StrictModeStopsAfterFirstPackageFailure()
    {
        OdfDocument document = InvalidAtEveryLayer();

        ValidationPipelineResult result = Validate(
            document,
            Options(OdfValidationMode.Strict));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.StoppedEarly, Is.True);
            Assert.That(result.PackageValidated, Is.True);
            Assert.That(result.XmlValidated, Is.False);
            Assert.That(result.SemanticsValidated, Is.False);
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(
                result.Diagnostics[0].Code,
                Is.EqualTo(OdfDiagnosticCodes.InvalidPackage));
        });
    }

    [Test]
    public void CompleteModeCollectsAllEnabledLayersInOrder()
    {
        OdfDocument document = InvalidAtEveryLayer();

        ValidationPipelineResult result = Validate(
            document,
            Options(OdfValidationMode.Complete));
        string[] codes = result.Diagnostics
            .Select(diagnostic => diagnostic.Code)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.StoppedEarly, Is.False);
            Assert.That(result.PackageValidated, Is.True);
            Assert.That(result.XmlValidated, Is.True);
            Assert.That(result.SemanticsValidated, Is.True);
            Assert.That(codes, Does.Contain(OdfDiagnosticCodes.InvalidPackage));
            Assert.That(codes, Does.Contain(OdfDiagnosticCodes.UnexpectedElement));
            Assert.That(codes, Does.Contain(OdfDiagnosticCodes.InvalidStyleReference));
            Assert.That(
                Array.IndexOf(codes, OdfDiagnosticCodes.InvalidPackage),
                Is.LessThan(Array.IndexOf(codes, OdfDiagnosticCodes.UnexpectedElement)));
            Assert.That(
                Array.IndexOf(codes, OdfDiagnosticCodes.UnexpectedElement),
                Is.LessThan(Array.IndexOf(codes, OdfDiagnosticCodes.InvalidStyleReference)));
        });
    }

    [TestCase(OdfValidationMode.PackageOnly)]
    [TestCase(OdfValidationMode.XmlOnly)]
    [TestCase(OdfValidationMode.SemanticOnly)]
    public void LayerOnlyModesOverrideDisabledLayerSwitches(
        OdfValidationMode mode)
    {
        OdfValidationOptions options = Options(mode);
        options.ValidatePackage = false;
        options.ValidateXml = false;
        options.ValidateSemantics = false;

        ValidationPipelineResult result = Validate(
            InvalidAtEveryLayer(),
            options);
        string[] codes = result.Diagnostics
            .Select(diagnostic => diagnostic.Code)
            .ToArray();

        if (mode == OdfValidationMode.PackageOnly)
        {
            Assert.Multiple(() =>
            {
                Assert.That(result.PackageValidated, Is.True);
                Assert.That(result.XmlValidated, Is.False);
                Assert.That(result.SemanticsValidated, Is.False);
                Assert.That(codes, Does.Contain(OdfDiagnosticCodes.InvalidPackage));
                Assert.That(codes, Does.Not.Contain(OdfDiagnosticCodes.UnexpectedElement));
                Assert.That(codes, Does.Not.Contain(OdfDiagnosticCodes.InvalidStyleReference));
            });
        }
        else if (mode == OdfValidationMode.XmlOnly)
        {
            Assert.Multiple(() =>
            {
                Assert.That(result.PackageValidated, Is.False);
                Assert.That(result.XmlValidated, Is.True);
                Assert.That(result.SemanticsValidated, Is.False);
                Assert.That(codes, Does.Contain(OdfDiagnosticCodes.UnexpectedElement));
                Assert.That(codes, Does.Not.Contain(OdfDiagnosticCodes.InvalidPackage));
                Assert.That(codes, Does.Not.Contain(OdfDiagnosticCodes.InvalidStyleReference));
            });
        }
        else
        {
            Assert.Multiple(() =>
            {
                Assert.That(result.PackageValidated, Is.False);
                Assert.That(result.XmlValidated, Is.False);
                Assert.That(result.SemanticsValidated, Is.True);
                Assert.That(codes, Does.Contain(OdfDiagnosticCodes.InvalidStyleReference));
                Assert.That(codes, Does.Not.Contain(OdfDiagnosticCodes.InvalidPackage));
                Assert.That(codes, Does.Not.Contain(OdfDiagnosticCodes.UnexpectedElement));
            });
        }
    }

    [Test]
    public void RecoveryModeDemotesRecoverableErrorsButRetainsDiagnostics()
    {
        OdfDocument document = InvalidAtEveryLayer();

        ValidationPipelineResult result = Validate(
            document,
            Options(OdfValidationMode.Recovery));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.StoppedEarly, Is.False);
            Assert.That(result.PackageValidated, Is.True);
            Assert.That(result.XmlValidated, Is.True);
            Assert.That(result.SemanticsValidated, Is.True);
            Assert.That(result.RecoveredDiagnosticCount, Is.GreaterThanOrEqualTo(3));
            Assert.That(result.Diagnostics, Is.Not.Empty);
            Assert.That(
                result.Diagnostics.All(
                    diagnostic => diagnostic.Severity
                        == OdfValidationSeverity.Warning),
                Is.True);
            Assert.That(
                result.Diagnostics.All(diagnostic => diagnostic.IsRecoverable),
                Is.True);
        });
    }

    [Test]
    public void RecoveryModeKeepsFatalDiagnosticsInvalid()
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Text);
        OdfValidationOptions options = Options(OdfValidationMode.Recovery);
        options.ValidatePackage = false;
        options.ValidateSemantics = false;
        options.SecurityLimits.MaximumXmlDepth = 2;

        ValidationPipelineResult result = Validate(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(
                result.Diagnostics.Select(diagnostic => diagnostic.Code),
                Does.Contain(OdfDiagnosticCodes.XmlDepthLimitExceeded));
            Assert.That(
                result.Diagnostics.First(
                    diagnostic => diagnostic.Code
                        == OdfDiagnosticCodes.XmlDepthLimitExceeded).Severity,
                Is.EqualTo(OdfValidationSeverity.Fatal));
        });
    }

    [Test]
    public void ProfileModeSelectsProfileAndRunsEnabledLayers()
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Text);
        OdfValidationOptions options = Options(OdfValidationMode.Profile);
        options.CompatibilityProfile = "interoperable";

        ValidationPipelineResult result = Validate(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.PackageValidated, Is.True);
            Assert.That(result.XmlValidated, Is.True);
            Assert.That(result.SemanticsValidated, Is.True);
            Assert.That(result.Profile, Is.EqualTo("interoperable"));
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void CompleteModeHonorsIndividualLayerSwitches()
    {
        OdfValidationOptions options = Options(OdfValidationMode.Complete);
        options.ValidatePackage = false;
        options.ValidateXml = false;

        ValidationPipelineResult result = Validate(
            InvalidAtEveryLayer(),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.PackageValidated, Is.False);
            Assert.That(result.XmlValidated, Is.False);
            Assert.That(result.SemanticsValidated, Is.True);
            Assert.That(
                result.Diagnostics.Select(diagnostic => diagnostic.Code),
                Does.Contain(OdfDiagnosticCodes.InvalidStyleReference));
        });
    }

    [Test]
    public void PackageModeFiltersSecurityWarningsAndInformation()
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Text);
        document.Package!.Diagnostics =
        [
            OdfDiagnosticFactory.Create(
                OdfDiagnosticCodes.SignatureInvalid,
                "signature diagnostic",
                OdfValidationSeverity.Error,
                null,
                null,
                OdfVersion.Unknown),
            OdfDiagnosticFactory.Create(
                OdfDiagnosticCodes.FeatureNotAvailable,
                "compatibility warning",
                OdfValidationSeverity.Warning,
                null,
                null,
                OdfVersion.Version14),
            OdfDiagnosticFactory.Create(
                OdfDiagnosticCodes.OperationCancelled,
                "informational note",
                OdfValidationSeverity.Information,
                null,
                null,
                OdfVersion.Version14)
        ];
        OdfValidationOptions filtered = Options(OdfValidationMode.PackageOnly);
        filtered.ValidateSecurity = false;
        filtered.IncludeWarnings = false;
        filtered.IncludeInformation = false;

        ValidationPipelineResult filteredResult = Validate(document, filtered);
        filtered.ValidateSecurity = true;
        filtered.IncludeWarnings = true;
        filtered.IncludeInformation = true;
        ValidationPipelineResult includedResult = Validate(document, filtered);

        Assert.Multiple(() =>
        {
            Assert.That(filteredResult.IsValid, Is.True);
            Assert.That(filteredResult.Diagnostics, Is.Empty);
            Assert.That(includedResult.IsValid, Is.False);
            Assert.That(includedResult.Diagnostics, Has.Length.EqualTo(3));
        });
    }

    [Test]
    public void CancellationStopsBeforeAnyLayerRuns()
    {
        OdfValidationOptions options = Options(OdfValidationMode.Complete);
        options.Cancellation.IsCancellationRequested = true;
        options.Cancellation.Reason = "requested by test";

        ValidationPipelineResult result = Validate(
            InvalidAtEveryLayer(),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.StoppedEarly, Is.True);
            Assert.That(result.PackageValidated, Is.False);
            Assert.That(result.XmlValidated, Is.False);
            Assert.That(result.SemanticsValidated, Is.False);
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(
                result.Diagnostics[0].Code,
                Is.EqualTo(OdfDiagnosticCodes.OperationCancelled));
            Assert.That(
                result.Diagnostics[0].Message,
                Does.Contain("requested by test"));
        });
    }

    static ValidationPipelineResult Validate(
        OdfDocument document,
        OdfValidationOptions options)
    {
        return new OdfValidationPipeline(options).ValidateDocument(document);
    }

    static OdfValidationOptions Options(OdfValidationMode mode)
    {
        OdfValidationOptions result = new OdfValidationOptions();
        result.Mode = mode;
        result.Version = OdfVersion.Version14;
        return result;
    }

    static OdfDocument InvalidAtEveryLayer()
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Text);
        document.Package!.IsValid = false;
        OdfPart part = document.Parts[0];
        part.Path = OdfPartNames.Styles;
        OdfNode root = part.Root!;
        root.NamespaceDeclarations =
        [
            .. root.NamespaceDeclarations,
            new OdfNamespaceDeclaration
            {
                Prefix = OdfNamespaces.TextPrefix,
                NamespaceName = OdfNamespaces.Text
            }
        ];
        OdfNode text = root.Children[0].Children[0];
        OdfNode paragraph = new OdfNode();
        paragraph.Kind = OdfNodeKind.Element;
        paragraph.Name = new OdfQualifiedName
        {
            NamespaceName = OdfNamespaces.Text,
            LocalName = "p",
            Prefix = OdfNamespaces.TextPrefix
        };
        paragraph.Attributes =
        [
            new OdfAttribute
            {
                Name = new OdfQualifiedName
                {
                    NamespaceName = OdfNamespaces.Text,
                    LocalName = "style-name",
                    Prefix = OdfNamespaces.TextPrefix
                },
                Value = "MissingStyle"
            }
        ];
        paragraph.Parent = text;
        text.Children = [.. text.Children, paragraph];
        return document;
    }
}
