using System.Text;

namespace odf.Tests;

public class OdfCompatibilityProfileTests
{
    [TestCase(OdfCompatibilityProfiles.StrictOasis)]
    [TestCase(OdfCompatibilityProfiles.Interoperable)]
    public void StandardOutputProfilesRemoveProducerExtensions(string profile)
    {
        OdfPart part = ExtensionPart();

        string written = WriteProfile(part, profile, true);

        Assert.Multiple(() =>
        {
            Assert.That(written, Does.Contain("text:p"));
            Assert.That(written, Does.Not.Contain("lo:feature"));
            Assert.That(written, Does.Not.Contain("oo:feature"));
            Assert.That(written, Does.Not.Contain("ms:feature"));
            Assert.That(written, Does.Not.Contain("ext:feature"));
            Assert.That(written, Does.Not.Contain("lo:flag"));
            Assert.That(written, Does.Not.Contain("ms:flag"));
            Assert.That(written, Does.Not.Contain("documentfoundation"));
            Assert.That(written, Does.Not.Contain("schemas.microsoft.com"));
            Assert.That(
                Descendants(part.Root!).Select(node => node.Name?.Prefix),
                Does.Contain("lo"));
        });
    }

    [Test]
    public void LibreOfficeProfileRetainsLibreOfficeAndOpenOfficeContent()
    {
        string written = WriteProfile(
            ExtensionPart(),
            OdfCompatibilityProfiles.LibreOfficeExtended,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(written, Does.Contain("lo:feature"));
            Assert.That(written, Does.Contain("lo:root-feature"));
            Assert.That(written, Does.Contain("oo:feature"));
            Assert.That(written, Does.Contain("lo:flag"));
            Assert.That(written, Does.Not.Contain("ms:feature"));
            Assert.That(written, Does.Not.Contain("ext:feature"));
        });
    }

    [Test]
    public void OpenOfficeProfileRetainsOnlyItsProducerFamily()
    {
        string written = WriteProfile(
            ExtensionPart(),
            OdfCompatibilityProfiles.OpenOfficeExtended,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(written, Does.Contain("oo:feature"));
            Assert.That(written, Does.Not.Contain("lo:feature"));
            Assert.That(written, Does.Not.Contain("ms:feature"));
            Assert.That(written, Does.Not.Contain("ext:feature"));
        });
    }

    [Test]
    public void MicrosoftProfileRetainsOnlyMicrosoftExtensions()
    {
        string written = WriteProfile(
            ExtensionPart(),
            OdfCompatibilityProfiles.MicrosoftOfficeExtended,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(written, Does.Contain("ms:feature"));
            Assert.That(written, Does.Contain("ms:flag"));
            Assert.That(written, Does.Not.Contain("lo:feature"));
            Assert.That(written, Does.Not.Contain("oo:feature"));
            Assert.That(written, Does.Not.Contain("ext:feature"));
        });
    }

    [Test]
    public void PreserveExistingOverridesUnknownContentRemoval()
    {
        string written = WriteProfile(
            ExtensionPart(),
            OdfCompatibilityProfiles.PreserveExisting,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(written, Does.Contain("lo:feature"));
            Assert.That(written, Does.Contain("oo:feature"));
            Assert.That(written, Does.Contain("ms:feature"));
            Assert.That(written, Does.Contain("ext:feature"));
            Assert.That(written, Does.Contain("lo:flag"));
            Assert.That(written, Does.Contain("ms:flag"));
        });
    }

    [Test]
    public void InvalidOutputProfileIsRejected()
    {
        OdfSaveOptions options = new OdfSaveOptions();
        options.CompatibilityProfile = "not-a-profile";

        Assert.That(
            () => new OdfXmlWriter().WriteXmlBytes(
                ExtensionPart(),
                options),
            Throws.TypeOf<OdfDocumentException>()
                .With.Message.Contains("not recognized"));
    }

    [Test]
    public void OutputProfileRejectsAConflictingConformanceTarget()
    {
        OdfSaveOptions options = new OdfSaveOptions();
        options.CompatibilityProfile = OdfCompatibilityProfiles.StrictOasis;
        options.TargetConformanceClass = OdfConformanceClass.Extended;

        Assert.That(
            () => new OdfXmlWriter().WriteXmlBytes(
                ExtensionPart(),
                options),
            Throws.TypeOf<OdfDocumentException>()
                .With.Message.Contains("conflicts"));
    }

    [Test]
    public void ManifestOutputUsesTheSameProducerNamespacePolicy()
    {
        string xml = "<m:manifest xmlns:m='"
            + OdfNamespaces.Manifest
            + "' xmlns:lo='urn:org:documentfoundation:names:experimental:office:xmlns:loext:1.0'"
            + " xmlns:ms='http://schemas.microsoft.com/office/word/2012/wordml'"
            + " m:version='1.4' lo:flag='keep' ms:flag='drop'>"
            + "<lo:feature/><ms:feature/>"
            + "<m:file-entry m:full-path='content.xml' m:media-type='text/xml'/>"
            + "</m:manifest>";
        OdfManifest manifest = ManifestParser.ParseText(xml);
        OdfSaveOptions options = new OdfSaveOptions();
        options.CompatibilityProfile =
            OdfCompatibilityProfiles.LibreOfficeExtended;

        string written = ManifestWriter.WriteText(manifest, options);

        Assert.Multiple(() =>
        {
            Assert.That(written, Does.Contain("lo:flag"));
            Assert.That(written, Does.Contain("lo:feature"));
            Assert.That(written, Does.Not.Contain("ms:flag"));
            Assert.That(written, Does.Not.Contain("ms:feature"));
            Assert.That(ManifestParser.ParseText(written).IsValid, Is.True);
        });
    }

    [Test]
    public void ProfileValidationReportsOnlyDisallowedProducerFamilies()
    {
        OdfDocument document = ExtensionDocument();
        OdfValidationOptions strictOptions = ProfileOptions(
            OdfCompatibilityProfiles.StrictOasis);
        OdfValidationOptions libreOfficeOptions = ProfileOptions(
            OdfCompatibilityProfiles.LibreOfficeExtended);

        ValidationPipelineResult strict =
            new OdfValidationPipeline(strictOptions)
                .ValidateDocument(document);
        ValidationPipelineResult libreOffice =
            new OdfValidationPipeline(libreOfficeOptions)
                .ValidateDocument(document);

        Assert.Multiple(() =>
        {
            Assert.That(strict.CompatibilityValidated, Is.True);
            Assert.That(strict.IsValid, Is.False);
            Assert.That(
                strict.Diagnostics.Count(
                    item => item.Code
                        == OdfDiagnosticCodes.FeatureNotAvailable),
                Is.EqualTo(7));
            Assert.That(libreOffice.CompatibilityValidated, Is.True);
            Assert.That(libreOffice.IsValid, Is.False);
            Assert.That(
                libreOffice.Diagnostics.Count(
                    item => item.Code
                        == OdfDiagnosticCodes.FeatureNotAvailable),
                Is.EqualTo(3));
            Assert.That(
                libreOffice.Diagnostics.Select(item => item.Message),
                Has.None.Contains("LibreOffice"));
        });
    }

    [Test]
    public void PreserveExistingProfileAcceptsEveryExtensionFamily()
    {
        OdfValidationOptions options = ProfileOptions(
            OdfCompatibilityProfiles.PreserveExisting);

        ValidationPipelineResult result = new OdfValidationPipeline(options)
            .ValidateDocument(ExtensionDocument());

        Assert.Multiple(() =>
        {
            Assert.That(result.CompatibilityValidated, Is.True);
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(
                result.Profile,
                Is.EqualTo(OdfCompatibilityProfiles.PreserveExisting));
        });
    }

    [Test]
    public void ProfileValidationIncludesManifestExtensionLocations()
    {
        OdfDocument document = ExtensionDocument();
        document.Manifest = ManifestParser.ParseText(
            "<m:manifest xmlns:m='"
                + OdfNamespaces.Manifest
                + "' xmlns:ms='http://schemas.microsoft.com/office/word/2012/wordml'"
                + " m:version='1.4'><ms:feature/></m:manifest>");
        OdfValidationOptions options = ProfileOptions(
            OdfCompatibilityProfiles.LibreOfficeExtended);

        ValidationPipelineResult result = new OdfValidationPipeline(options)
            .ValidateDocument(document);

        OdfDiagnostic diagnostic = result.Diagnostics.Single(
            item => item.Location.PartPath == OdfPartNames.Manifest);
        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Code, Is.EqualTo(OdfDiagnosticCodes.FeatureNotAvailable));
            Assert.That(diagnostic.Location.NodePath, Does.Contain("ms:feature"));
        });
    }

    [Test]
    public void UnknownValidationProfileProducesAProfileDiagnostic()
    {
        OdfValidationOptions options = ProfileOptions("unknown-profile");

        ValidationPipelineResult result = new OdfValidationPipeline(options)
            .ValidateDocument(ExtensionDocument());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.CompatibilityValidated, Is.True);
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(
                result.Diagnostics[0].Code,
                Is.EqualTo(OdfDiagnosticCodes.FeatureNotAvailable));
            Assert.That(result.Diagnostics[0].Message, Does.Contain("not recognized"));
        });
    }

    [Test]
    public void ProfileValidationRejectsConflictingConformanceClass()
    {
        OdfValidationOptions options = ProfileOptions(
            OdfCompatibilityProfiles.StrictOasis);
        options.ConformanceClass = OdfConformanceClass.Extended;

        ValidationPipelineResult result = new OdfValidationPipeline(options)
            .ValidateDocument(
                new OdfDocumentFactory().CreateFlatDocument(
                    OdfDocumentKind.Text));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(result.Diagnostics[0].Message, Does.Contain("conflicts"));
        });
    }

    static OdfValidationOptions ProfileOptions(string profile)
    {
        OdfValidationOptions result = new OdfValidationOptions();
        result.Mode = OdfValidationMode.Profile;
        result.CompatibilityProfile = profile;
        result.ValidatePackage = false;
        result.ValidateXml = false;
        result.ValidateSemantics = false;
        return result;
    }

    static string WriteProfile(
        OdfPart part,
        string profile,
        bool preserveUnknownContent)
    {
        OdfSaveOptions options = new OdfSaveOptions();
        options.CompatibilityProfile = profile;
        options.PreserveUnknownContent = preserveUnknownContent;
        return Encoding.UTF8.GetString(
            new OdfXmlWriter().WriteXmlBytes(part, options));
    }

    static OdfDocument ExtensionDocument()
    {
        return OdfDocumentAssembly.AssembleFlatDocument(ExtensionNodes());
    }

    static OdfPart ExtensionPart()
    {
        return OdfDocumentModel.CreateStandalonePart(
            ExtensionNodes(),
            "",
            OdfMediaTypes.Text);
    }

    static OdfNode[] ExtensionNodes()
    {
        string xml = "<office:document xmlns:office='"
            + OdfNamespaces.Office
            + "' xmlns:text='"
            + OdfNamespaces.Text
            + "' xmlns:lo='urn:org:documentfoundation:names:experimental:office:xmlns:loext:1.0'"
            + " xmlns:oo='http://openoffice.org/2009/office'"
            + " xmlns:ms='http://schemas.microsoft.com/office/word/2012/wordml'"
            + " xmlns:ext='urn:example:extension' office:version='1.4'>"
            + "<lo:root-feature/><office:body><office:text><text:p lo:flag='yes' ms:flag='yes'>"
            + "<lo:feature/><oo:feature/><ms:feature/><ext:feature/>"
            + "</text:p></office:text></office:body></office:document>";
        return OdfTreeBuilder.ReadBytes(
            Encoding.UTF8.GetBytes(xml),
            "profile.fodt",
            new OdfLoadOptions());
    }

    static IEnumerable<OdfNode> Descendants(OdfNode node)
    {
        yield return node;
        foreach (OdfNode child in node.Children)
        {
            foreach (OdfNode descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }
}
