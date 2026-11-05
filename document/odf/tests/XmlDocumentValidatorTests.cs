namespace odf.Tests;

public class XmlDocumentValidatorTests
{
    [Test]
    public void AcceptsFactoryPackageFlatAndFormulaDocuments()
    {
        OdfDocumentFactory factory = new OdfDocumentFactory();
        OdfDocument package = factory.CreatePackageDocument(OdfDocumentKind.Text);
        OdfDocument flat = factory.CreateFlatDocument(OdfDocumentKind.Spreadsheet);
        OdfDocument formula = factory.CreatePackageDocument(OdfDocumentKind.Formula);

        XmlValidationResult packageResult = Validate(
            package,
            OdfValidationMode.Complete);
        XmlValidationResult flatResult = Validate(
            flat,
            OdfValidationMode.Complete);
        XmlValidationResult formulaResult = Validate(
            formula,
            OdfValidationMode.Complete);

        Assert.Multiple(() =>
        {
            Assert.That(packageResult.IsValid, Is.True);
            Assert.That(packageResult.Diagnostics, Is.Empty);
            Assert.That(packageResult.ValidatedPartCount, Is.EqualTo(1));
            Assert.That(flatResult.IsValid, Is.True);
            Assert.That(flatResult.Diagnostics, Is.Empty);
            Assert.That(formulaResult.IsValid, Is.True);
            Assert.That(formulaResult.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void CompleteModeCollectsIndependentPartNamespaceIdAndSchemaFailures()
    {
        OdfDocument document = InvalidDocument();

        XmlValidationResult result = Validate(
            document,
            OdfValidationMode.Complete);
        string[] codes = result.Diagnostics
            .Select(diagnostic => diagnostic.Code)
            .ToArray();
        OdfDiagnostic duplicate = result.Diagnostics.Single(
            diagnostic => diagnostic.Code == OdfDiagnosticCodes.DuplicateXmlId);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.StoppedEarly, Is.False);
            Assert.That(codes, Does.Contain(OdfDiagnosticCodes.UnexpectedElement));
            Assert.That(codes, Does.Contain(OdfDiagnosticCodes.InvalidNamespace));
            Assert.That(codes, Does.Contain(OdfDiagnosticCodes.DuplicateXmlId));
            Assert.That(codes, Does.Contain(OdfDiagnosticCodes.InvalidDatatype));
            Assert.That(duplicate.HasRelatedLocation, Is.True);
            Assert.That(duplicate.Location.PartPath, Is.EqualTo(OdfPartNames.Styles));
            Assert.That(duplicate.Location.NodePath, Is.Not.Empty);
        });
    }

    [Test]
    public void StrictModeStopsAtFirstConformanceFailure()
    {
        OdfDocument document = InvalidDocument();

        XmlValidationResult result = Validate(
            document,
            OdfValidationMode.Strict);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.StoppedEarly, Is.True);
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(
                result.Diagnostics[0].Code,
                Is.EqualTo(OdfDiagnosticCodes.UnexpectedElement));
        });
    }

    [Test]
    public void AcceptsUnprefixedNoNamespaceNamesWithoutADefaultBinding()
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Text);
        OdfNode text = document.Parts[0].Root!.Children[0].Children[0];
        OdfNode foreign = new OdfNode();
        foreign.Kind = OdfNodeKind.Element;
        foreign.Name = new OdfQualifiedName();
        foreign.Name.LocalName = "foreign";
        foreign.Parent = text;
        text.Children = [.. text.Children, foreign];

        XmlValidationResult result = Validate(
            document,
            OdfValidationMode.Complete);

        Assert.That(
            result.Diagnostics.Select(diagnostic => diagnostic.Code),
            Does.Not.Contain(OdfDiagnosticCodes.InvalidNamespace));
    }

    [Test]
    public void ReportsAnEmptyXmlIdAsAnInvalidXmlName()
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Text);
        OdfNode root = document.Parts[0].Root!;
        root.Attributes =
        [
            .. root.Attributes,
            Attribute(OdfNamespaces.Xml, "id", "xml", "")
        ];

        XmlValidationResult result = Validate(
            document,
            OdfValidationMode.Complete);

        Assert.That(
            result.Diagnostics.Select(diagnostic => diagnostic.Code),
            Does.Contain(OdfDiagnosticCodes.InvalidXmlName));
    }

    [Test]
    public void ReportsTreeResourceLimitsWithoutThrowingInCompleteMode()
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Text);
        OdfValidationOptions options = Options(OdfValidationMode.Complete);
        options.SecurityLimits.MaximumXmlDepth = 2;

        XmlValidationResult result =
            new XmlDocumentValidator(options).ValidateDocument(document);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(
                result.Diagnostics.Select(diagnostic => diagnostic.Code),
                Does.Contain(OdfDiagnosticCodes.XmlDepthLimitExceeded));
            Assert.That(
                result.Diagnostics.Single(
                    diagnostic => diagnostic.Code
                        == OdfDiagnosticCodes.XmlDepthLimitExceeded).Severity,
                Is.EqualTo(OdfValidationSeverity.Fatal));
        });
    }

    [Test]
    public void HonorsCancellationBeforeVisitingAnyPart()
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Text);
        OdfValidationOptions options = Options(OdfValidationMode.Strict);
        options.Cancellation.IsCancellationRequested = true;
        options.Cancellation.Reason = "caller stopped validation";

        XmlValidationResult result =
            new XmlDocumentValidator(options).ValidateDocument(document);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.StoppedEarly, Is.True);
            Assert.That(result.ValidatedPartCount, Is.Zero);
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(
                result.Diagnostics[0].Code,
                Is.EqualTo(OdfDiagnosticCodes.OperationCancelled));
            Assert.That(
                result.Diagnostics[0].Message,
                Does.Contain("caller stopped validation"));
        });
    }

    static XmlValidationResult Validate(
        OdfDocument document,
        OdfValidationMode mode)
    {
        return new XmlDocumentValidator(Options(mode))
            .ValidateDocument(document);
    }

    static OdfValidationOptions Options(OdfValidationMode mode)
    {
        OdfValidationOptions result = new OdfValidationOptions();
        result.Mode = mode;
        result.Version = OdfVersion.Version14;
        return result;
    }

    static OdfDocument InvalidDocument()
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Text);
        OdfPart part = document.Parts[0];
        part.Path = OdfPartNames.Styles;
        OdfNode root = part.Root!;
        OdfNode body = root.Children.Single(
            child => child.Kind == OdfNodeKind.Element
                && child.Name!.LocalName == "body");

        OdfAttribute version = root.Attributes.Single(
            attribute => attribute.Name.NamespaceName == OdfNamespaces.Office
                && attribute.Name.LocalName == "version");
        version.Value = "invalid-version";
        root.Attributes =
        [
            .. root.Attributes,
            Attribute("urn:unbound", "option", "v", "on"),
            Attribute(OdfNamespaces.Xml, "id", "xml", "duplicate-id")
        ];
        root.XmlId = "duplicate-id";
        body.Attributes =
        [
            .. body.Attributes,
            Attribute(OdfNamespaces.Xml, "id", "xml", "duplicate-id")
        ];
        body.XmlId = "duplicate-id";
        return document;
    }

    static OdfAttribute Attribute(
        string namespaceName,
        string localName,
        string prefix,
        string value)
    {
        OdfAttribute result = new OdfAttribute();
        result.Name.NamespaceName = namespaceName;
        result.Name.LocalName = localName;
        result.Name.Prefix = prefix;
        result.Value = value;
        return result;
    }
}
