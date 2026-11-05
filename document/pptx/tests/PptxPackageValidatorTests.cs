using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PptxPackageValidatorTests
{
    [Test]
    public void Validate_SucceedsForPackageWithRootPresentationRelationshipAndExistingTargets()
    {
        PptxPackage package = CreateValidPackage();

        PptxResult result = PptxPackageValidator.Validate(package);
        PptxResult instanceResult = package.Validate();

        Assert.That(result.Succeeded(), Is.True, result.FormatDiagnostics());
        Assert.That(instanceResult.Succeeded(), Is.True, instanceResult.FormatDiagnostics());
    }

    [Test]
    public void Validate_ReportsMissingRootPresentationRelationship()
    {
        PptxPackage package = PptxPackage.Create();
        package.AddPart("/ppt/presentation.xml", PptxContentTypes.Presentation, Encoding.UTF8.GetBytes("<p:presentation/>"));

        PptxResult result = PptxPackageValidator.Validate(package);

        Assert.That(result.Failed(), Is.True);
        Assert.That(result.FormatDiagnostics(), Does.Contain("PKGV010"));
    }

    [Test]
    public void Validate_ReportsMissingRootPresentationTargetPart()
    {
        PptxPackage package = PptxPackage.Create();
        package.AddRelationship("", PptxRelationshipTypes.OfficeDocument, "ppt/missing.xml", PptxRelationshipTargetMode.Internal);

        PptxResult result = PptxPackageValidator.Validate(package);

        Assert.That(result.Failed(), Is.True);
        Assert.That(result.FormatDiagnostics(), Does.Contain("PKGV012"));
        Assert.That(result.FormatDiagnostics(), Does.Contain("PKGV020"));
    }

    [Test]
    public void Validate_ReportsRootPresentationTargetWithWrongContentType()
    {
        PptxPackage package = PptxPackage.Create();
        package.AddPart("/ppt/custom.xml", PptxContentTypes.Xml, Encoding.UTF8.GetBytes("<root/>"));
        package.AddRelationship("", PptxRelationshipTypes.OfficeDocument, "ppt/custom.xml", PptxRelationshipTargetMode.Internal);

        PptxResult result = PptxPackageValidator.Validate(package);

        Assert.That(result.Failed(), Is.True);
        Assert.That(result.FormatDiagnostics(), Does.Contain("PKGV013"));
        Assert.That(result.FormatDiagnostics(), Does.Not.Contain("PKGV020"));
    }

    [Test]
    public void Validate_ReportsBrokenInternalPartRelationshipsButIgnoresExternalTargets()
    {
        PptxPackage package = CreateValidPackage();
        package.AddRelationship("/ppt/presentation.xml", PptxRelationshipTypes.Slide, "slides/missing.xml", PptxRelationshipTargetMode.Internal);
        package.AddRelationship("/ppt/presentation.xml", PptxRelationshipTypes.Hyperlink, "https://example.com", PptxRelationshipTargetMode.External);

        PptxResult result = PptxPackageValidator.Validate(package);

        Assert.That(result.Failed(), Is.True);
        Assert.That(result.FormatDiagnostics(), Does.Contain("PKGV020"));
        Assert.That(result.FormatDiagnostics(), Does.Contain("/ppt/presentation.xml"));
        Assert.That(result.FormatDiagnostics(), Does.Not.Contain("https://example.com"));
    }

    [Test]
    public void Validate_ReportsMultipleRootPresentationRelationships()
    {
        PptxPackage package = CreateValidPackage();
        package.AddPart("/ppt/presentation2.xml", PptxContentTypes.Presentation, Encoding.UTF8.GetBytes("<p:presentation/>"));
        package.AddRelationship("", PptxRelationshipTypes.OfficeDocument, "ppt/presentation2.xml", PptxRelationshipTargetMode.Internal);

        PptxResult result = PptxPackageValidator.Validate(package);

        Assert.That(result.Failed(), Is.True);
        Assert.That(result.FormatDiagnostics(), Does.Contain("PKGV014"));
    }

    [Test]
    public void Validate_ReportsMissingRequiredContentTypeDefaults()
    {
        PptxPackage package = CreateValidPackage();
        package.ContentTypes.Defaults.Clear();
        package.ContentTypes.AddDefault("xml", PptxContentTypes.Xml);

        PptxResult result = PptxPackageValidator.Validate(package);

        Assert.That(result.Failed(), Is.True);
        Assert.That(result.FormatDiagnostics(), Does.Contain("PKGV030"));
        Assert.That(result.FormatDiagnostics(), Does.Contain("rels"));
    }

    [Test]
    public void Validate_ReportsWrongRequiredContentTypeDefaults()
    {
        PptxPackage package = CreateValidPackage();
        package.ContentTypes.Defaults.Clear();
        package.ContentTypes.AddDefault("rels", PptxContentTypes.Xml);
        package.ContentTypes.AddDefault("xml", PptxContentTypes.Xml);

        PptxResult result = PptxPackageValidator.Validate(package);

        Assert.That(result.Failed(), Is.True);
        Assert.That(result.FormatDiagnostics(), Does.Contain("PKGV031"));
    }

    [Test]
    public void Validate_ReportsMissingContentTypeDeclarationForPart()
    {
        PptxPackage package = CreateValidPackage();
        package.AddPart("/ppt/media/image1.png", PptxContentTypes.Png, new byte[] { 1, 2, 3 });
        package.RemoveContentTypeOverride("/ppt/media/image1.png");

        PptxResult result = PptxPackageValidator.Validate(package);

        Assert.That(result.Failed(), Is.True);
        Assert.That(result.FormatDiagnostics(), Does.Contain("PKGV032"));
        Assert.That(result.FormatDiagnostics(), Does.Contain("/ppt/media/image1.png"));
    }

    [Test]
    public void Validate_ReportsMissingRequiredOverrideForSpecificXmlPart()
    {
        PptxPackage package = CreateValidPackage();
        package.RemoveContentTypeOverride("/ppt/slides/slide1.xml");

        PptxResult result = PptxPackageValidator.Validate(package);

        Assert.That(result.Failed(), Is.True);
        Assert.That(result.FormatDiagnostics(), Does.Contain("PKGV033"));
        Assert.That(result.FormatDiagnostics(), Does.Contain("PKGV034"));
        Assert.That(result.FormatDiagnostics(), Does.Contain("/ppt/slides/slide1.xml"));
    }

    [Test]
    public void Validate_AllowsDefaultCoveredMediaContentType()
    {
        PptxPackage package = CreateValidPackage();
        package.AddPart("/ppt/media/image1.png", PptxContentTypes.Png, new byte[] { 1, 2, 3 });
        package.RemoveContentTypeOverride("/ppt/media/image1.png");
        package.ContentTypes.AddDefault("png", PptxContentTypes.Png);

        PptxResult result = PptxPackageValidator.Validate(package);

        Assert.That(result.Succeeded(), Is.True, result.FormatDiagnostics());
    }

    [Test]
    public void Validate_ReportsOverridesForMissingParts()
    {
        PptxPackage package = CreateValidPackage();
        package.ContentTypes.AddOverride("/ppt/missing.xml", PptxContentTypes.Xml);

        PptxResult result = PptxPackageValidator.Validate(package);

        Assert.That(result.Failed(), Is.True);
        Assert.That(result.FormatDiagnostics(), Does.Contain("PKGV035"));
        Assert.That(result.FormatDiagnostics(), Does.Contain("/ppt/missing.xml"));
    }

    [Test]
    public void Validate_ReportsMalformedXmlParts()
    {
        PptxPackage package = CreateValidPackage();
        package.GetPart("/ppt/slides/slide1.xml").SetBytes(Encoding.UTF8.GetBytes("<p:sld><p:cSld></p:sld>"));

        PptxResult result = PptxPackageValidator.Validate(package);

        Assert.That(result.Failed(), Is.True);
        Assert.That(result.FormatDiagnostics(), Does.Contain("PKGV042"));
        Assert.That(result.FormatDiagnostics(), Does.Contain("/ppt/slides/slide1.xml"));
    }

    [Test]
    public void Validate_ReportsUnclosedXmlParts()
    {
        PptxPackage package = CreateValidPackage();
        package.GetPart("/ppt/slides/slide1.xml").SetBytes(Encoding.UTF8.GetBytes("<p:sld><p:cSld>"));

        PptxResult result = PptxPackageValidator.Validate(package);

        Assert.That(result.Failed(), Is.True);
        Assert.That(result.FormatDiagnostics(), Does.Contain("PKGV043"));
    }

    [Test]
    public void Validate_SkipsNonXmlPartBytes()
    {
        PptxPackage package = CreateValidPackage();
        package.AddPart("/ppt/media/image1.png", PptxContentTypes.Png, Encoding.UTF8.GetBytes("<not><xml>"));
        package.ContentTypes.AddDefault("png", PptxContentTypes.Png);

        PptxResult result = PptxPackageValidator.Validate(package);

        Assert.That(result.Succeeded(), Is.True, result.FormatDiagnostics());
    }

    public static PptxPackage CreateValidPackage()
    {
        return PptxMinimumPresentation.CreatePackage();
    }
}
