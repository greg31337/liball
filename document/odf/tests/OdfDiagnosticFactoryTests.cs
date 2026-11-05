using System.Text;

namespace odf.Tests;

public class OdfDiagnosticFactoryTests
{
    [Test]
    public void CreatesCompleteDiagnosticsWithDefensiveLocationCopies()
    {
        OdfLocation primary = Location(
            OdfPartNames.Content,
            "/office:document-content[1]/text:p[2]",
            "paragraph-2",
            12,
            7,
            1024,
            3,
            9);
        OdfLocation related = Location(
            OdfPartNames.Styles,
            "/office:document-styles[1]/style:style[1]",
            "style-1",
            4,
            2,
            128,
            -1,
            0);

        OdfDiagnostic diagnostic = OdfDiagnosticFactory.Create(
            OdfDiagnosticCodes.InvalidStyleReference,
            "The style is missing.",
            OdfValidationSeverity.Error,
            primary,
            related,
            OdfVersion.Version14);
        primary.PartPath = "changed.xml";
        primary.Line = 99;
        related.NodePath = "/changed";

        Assert.Multiple(() =>
        {
            Assert.That(
                diagnostic.Code,
                Is.EqualTo(OdfDiagnosticCodes.InvalidStyleReference));
            Assert.That(diagnostic.Message, Is.EqualTo("The style is missing."));
            Assert.That(diagnostic.Severity, Is.EqualTo(OdfValidationSeverity.Error));
            Assert.That(diagnostic.IsRecoverable, Is.True);
            Assert.That(diagnostic.Version, Is.EqualTo(OdfVersion.Version14));
            Assert.That(
                diagnostic.SpecificationSection,
                Is.EqualTo("OpenDocument Part 3: Semantic constraints"));
            Assert.That(diagnostic.Location.PartPath, Is.EqualTo(OdfPartNames.Content));
            Assert.That(diagnostic.Location.Line, Is.EqualTo(12));
            Assert.That(diagnostic.Location.Column, Is.EqualTo(7));
            Assert.That(diagnostic.Location.ByteOffset, Is.EqualTo(1024));
            Assert.That(diagnostic.Location.TextOffset, Is.EqualTo(3));
            Assert.That(diagnostic.Location.TextLength, Is.EqualTo(9));
            Assert.That(diagnostic.Location.XmlId, Is.EqualTo("paragraph-2"));
            Assert.That(diagnostic.HasRelatedLocation, Is.True);
            Assert.That(
                diagnostic.RelatedLocation.NodePath,
                Is.EqualTo("/office:document-styles[1]/style:style[1]"));
        });
    }

    [Test]
    public void NormalizesLegacyDiagnosticsWithFallbackMetadata()
    {
        OdfDiagnostic legacy = new OdfDiagnostic();
        legacy.Code = OdfDiagnosticCodes.XmlDepthLimitExceeded;
        legacy.Message = "too deep";
        legacy.Severity = OdfValidationSeverity.Fatal;
        legacy.Location.PartPath = OdfPartNames.Content;
        legacy.Location.NodePath = "/office:document-content[1]";
        legacy.Location.Line = 8;
        legacy.Location.Column = 11;

        OdfDiagnostic normalized = OdfDiagnosticFactory.Normalize(
            legacy,
            OdfVersion.Version13);
        legacy.Location.Line = 100;

        Assert.Multiple(() =>
        {
            Assert.That(normalized.Version, Is.EqualTo(OdfVersion.Version13));
            Assert.That(normalized.Severity, Is.EqualTo(OdfValidationSeverity.Fatal));
            Assert.That(normalized.IsRecoverable, Is.False);
            Assert.That(
                normalized.SpecificationSection,
                Is.EqualTo("XML 1.0 and Namespaces in XML"));
            Assert.That(normalized.Location.Line, Is.EqualTo(8));
            Assert.That(normalized.Location.Column, Is.EqualTo(11));
        });
    }

    [Test]
    public void PackageValidationEmitsStablePackageMetadata()
    {
        OdfPackage package = new OdfPackage();
        PackageValidationResult result =
            new PackageConformanceValidator(new OdfSecurityLimits())
                .ValidatePackage(package);
        OdfDiagnostic diagnostic = result.Diagnostics.First(
            item => item.Code == OdfDiagnosticCodes.InvalidPackage);

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Severity, Is.EqualTo(OdfValidationSeverity.Error));
            Assert.That(
                diagnostic.SpecificationSection,
                Is.EqualTo("OpenDocument Part 2: Packages"));
            Assert.That(diagnostic.Location, Is.Not.Null);
            Assert.That(diagnostic.RelatedLocation, Is.Not.Null);
        });
    }

    [Test]
    public void XmlValidationCarriesSourceAndVersionMetadata()
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Text);
        document.Parts[0].Path = OdfPartNames.Styles;
        OdfValidationOptions options = new OdfValidationOptions();
        options.Mode = OdfValidationMode.Complete;
        options.Version = OdfVersion.Version14;

        OdfDiagnostic diagnostic =
            new XmlDocumentValidator(options)
                .ValidateDocument(document)
                .Diagnostics
                .First(item => item.Code == OdfDiagnosticCodes.UnexpectedElement);

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Version, Is.EqualTo(OdfVersion.Version14));
            Assert.That(
                diagnostic.SpecificationSection,
                Is.EqualTo("OpenDocument Part 3: Schema"));
            Assert.That(diagnostic.Location.PartPath, Is.EqualTo(OdfPartNames.Styles));
            Assert.That(diagnostic.Location.NodePath, Is.Not.Empty);
            Assert.That(diagnostic.Location.Line, Is.GreaterThan(0));
            Assert.That(diagnostic.Location.Column, Is.GreaterThan(0));
        });
    }

    [Test]
    public void SemanticValidationCarriesAttributeSourceMetadata()
    {
        string xml = "<office:document-content xmlns:office='"
            + OdfNamespaces.Office
            + "' xmlns:text='"
            + OdfNamespaces.Text
            + "'><office:body><office:text>\n"
            + "<text:p text:style-name='Missing'/></office:text></office:body>"
            + "</office:document-content>";
        OdfNode[] nodes = OdfTreeBuilder.ReadBytes(
            Encoding.UTF8.GetBytes(xml),
            OdfPartNames.Content,
            new OdfLoadOptions());
        OdfPart part = new OdfPart();
        part.Path = OdfPartNames.Content;
        part.Nodes = nodes;
        OdfDocument document = new OdfDocument();
        document.Parts = [part];
        OdfValidationOptions options = new OdfValidationOptions();
        options.Mode = OdfValidationMode.Complete;
        options.Version = OdfVersion.Version14;

        OdfDiagnostic diagnostic =
            new SemanticDocumentValidator(options)
                .ValidateDocument(document)
                .Diagnostics
                .Single();

        Assert.Multiple(() =>
        {
            Assert.That(
                diagnostic.Code,
                Is.EqualTo(OdfDiagnosticCodes.InvalidStyleReference));
            Assert.That(diagnostic.Version, Is.EqualTo(OdfVersion.Version14));
            Assert.That(
                diagnostic.SpecificationSection,
                Is.EqualTo("OpenDocument Part 3: Semantic constraints"));
            Assert.That(diagnostic.Location.PartPath, Is.EqualTo(OdfPartNames.Content));
            Assert.That(diagnostic.Location.NodePath, Does.Contain("text:p"));
            Assert.That(diagnostic.Location.Line, Is.EqualTo(2));
            Assert.That(diagnostic.Location.Column, Is.GreaterThan(0));
        });
    }

    static OdfLocation Location(
        string partPath,
        string nodePath,
        string xmlId,
        int line,
        int column,
        long byteOffset,
        int textOffset,
        int textLength)
    {
        OdfLocation result = new OdfLocation();
        result.PartPath = partPath;
        result.NodePath = nodePath;
        result.XmlId = xmlId;
        result.Line = line;
        result.Column = column;
        result.ByteOffset = byteOffset;
        result.TextOffset = textOffset;
        result.TextLength = textLength;
        return result;
    }
}
