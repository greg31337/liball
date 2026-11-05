using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PptxContentTypesParserTests
{
    [Test]
    public void ParseString_ReadsDefaultsAndOverrides()
    {
        string xml =
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
            "<Default Extension=\"XML\" ContentType=\"application/xml\"/>" +
            "<Override PartName=\"/ppt/presentation.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.presentationml.presentation.main+xml\"/>" +
            "<Override PartName=\"/ppt/slides/slide1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.presentationml.slide+xml\"/>" +
            "</Types>";

        PptxContentTypeManifest manifest = PptxContentTypesParser.ParseString(xml);

        Assert.That(manifest.Succeeded(), Is.True);
        Assert.That(manifest.DefaultCount(), Is.EqualTo(2));
        Assert.That(manifest.OverrideCount(), Is.EqualTo(2));
        Assert.That(manifest.Defaults[1].Extension, Is.EqualTo("xml"));
        Assert.That(manifest.GetDefaultContentType("XML"), Is.EqualTo(PptxContentTypes.Xml));
        Assert.That(manifest.GetOverrideContentType("/ppt/presentation.xml"), Is.EqualTo(PptxContentTypes.Presentation));
        Assert.That(manifest.GetContentTypeForPartName("/ppt/slides/slide1.xml"), Is.EqualTo(PptxContentTypes.Slide));
        Assert.That(manifest.GetContentTypeForPartName("/ppt/_rels/presentation.xml.rels"), Is.EqualTo(PptxContentTypes.Relationships));
    }

    [Test]
    public void ParseString_ReadsRealisticPresentationManifestWithMixedMediaAndOverrides()
    {
        string xml =
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
            "<Default Extension=\"XML\" ContentType=\"application/xml\"/>" +
            "<Default Extension=\"jpeg\" ContentType=\"image/jpeg\"/>" +
            "<Default Extension=\"PNG\" ContentType=\"image/png\"/>" +
            "<Default Extension=\"mp4\" ContentType=\"video/mp4\"/>" +
            "<Override PartName=\"/docProps/core.xml\" ContentType=\"application/vnd.openxmlformats-package.core-properties+xml\"/>" +
            "<Override PartName=\"/docProps/app.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.extended-properties+xml\"/>" +
            "<Override PartName=\"/ppt/presentation.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.presentationml.presentation.main+xml\"/>" +
            "<Override PartName=\"/ppt/presProps.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.presentationml.presProps+xml\"/>" +
            "<Override PartName=\"/ppt/slides/slide1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.presentationml.slide+xml\"/>" +
            "<Override PartName=\"/ppt/slideLayouts/slideLayout1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.presentationml.slideLayout+xml\"/>" +
            "<Override PartName=\"/ppt/slideMasters/slideMaster1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.presentationml.slideMaster+xml\"/>" +
            "<Override PartName=\"/ppt/theme/theme1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.theme+xml\"/>" +
            "<Override PartName=\"/ppt/charts/chart1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.drawingml.chart+xml\"/>" +
            "</Types>";

        PptxContentTypeManifest manifest = PptxContentTypesParser.ParseString(xml);

        Assert.That(manifest.Succeeded(), Is.True, manifest.Diagnostics.FormatDiagnostics());
        Assert.That(manifest.DefaultCount(), Is.EqualTo(5));
        Assert.That(manifest.OverrideCount(), Is.EqualTo(9));
        Assert.That(manifest.Defaults[1].Extension, Is.EqualTo("xml"));
        Assert.That(manifest.Defaults[3].Extension, Is.EqualTo("png"));
        Assert.That(manifest.GetContentTypeForPartName("/ppt/media/photo1.JPEG"), Is.EqualTo(PptxContentTypes.Jpeg));
        Assert.That(manifest.GetContentTypeForPartName("/ppt/media/image1.png"), Is.EqualTo(PptxContentTypes.Png));
        Assert.That(manifest.GetContentTypeForPartName("/ppt/media/movie1.mp4"), Is.EqualTo(PptxContentTypes.Mp4));
        Assert.That(manifest.GetContentTypeForPartName("/ppt/charts/chart1.xml"), Is.EqualTo(PptxContentTypes.Chart));
        Assert.That(manifest.GetContentTypeForPartName("ppt/slides/slide1.xml"), Is.EqualTo(PptxContentTypes.Slide));
        Assert.That(manifest.GetContentTypeForPartName("/ppt/notesSlides/notesSlide1.xml"), Is.EqualTo(PptxContentTypes.Xml));
    }

    [Test]
    public void ParseBytes_UsesUtf8XmlBytes()
    {
        string xml =
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"png\" ContentType=\"image/png\"/>" +
            "</Types>";

        PptxContentTypeManifest manifest = PptxContentTypesParser.ParseBytes(Encoding.UTF8.GetBytes(xml));

        Assert.That(manifest.Succeeded(), Is.True);
        Assert.That(manifest.GetContentTypeForPartName("/ppt/media/image1.png"), Is.EqualTo(PptxContentTypes.Png));
    }

    [Test]
    public void ParseString_ReportsInvalidRoot()
    {
        PptxContentTypeManifest manifest = PptxContentTypesParser.ParseString("<Types/>");

        Assert.That(manifest.Failed(), Is.True);
        Assert.That(manifest.Diagnostics.FormatDiagnostics(), Does.Contain("CT003"));
    }

    [Test]
    public void ParseString_ReportsDuplicateDefaultsAndOverrides()
    {
        string xml =
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
            "<Default Extension=\"XML\" ContentType=\"application/xml\"/>" +
            "<Override PartName=\"/ppt/presentation.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.presentationml.presentation.main+xml\"/>" +
            "<Override PartName=\"ppt/presentation.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.presentationml.presentation.main+xml\"/>" +
            "</Types>";

        PptxContentTypeManifest manifest = PptxContentTypesParser.ParseString(xml);

        Assert.That(manifest.Failed(), Is.True);
        Assert.That(manifest.DefaultCount(), Is.EqualTo(1));
        Assert.That(manifest.OverrideCount(), Is.EqualTo(1));
        Assert.That(manifest.Diagnostics.FormatDiagnostics(), Does.Contain("CT020"));
        Assert.That(manifest.Diagnostics.FormatDiagnostics(), Does.Contain("CT021"));
    }

    [Test]
    public void ParseString_ReportsMissingAndUnsafeAttributes()
    {
        string xml =
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\".xml\" ContentType=\"application/xml\"/>" +
            "<Default Extension=\"bad/path\" ContentType=\"application/xml\"/>" +
            "<Default Extension=\"jpg\" ContentType=\"image\"/>" +
            "<Override PartName=\"/ppt/../presentation.xml\" ContentType=\"application/xml\"/>" +
            "<Override PartName=\"/ppt/slides/slide1.xml\" ContentType=\"\"/>" +
            "</Types>";

        PptxContentTypeManifest manifest = PptxContentTypesParser.ParseString(xml);

        Assert.That(manifest.Failed(), Is.True);
        Assert.That(manifest.DefaultCount(), Is.EqualTo(0));
        Assert.That(manifest.OverrideCount(), Is.EqualTo(0));
        Assert.That(manifest.Diagnostics.FormatDiagnostics(), Does.Contain("CT011"));
        Assert.That(manifest.Diagnostics.FormatDiagnostics(), Does.Contain("CT012"));
        Assert.That(manifest.Diagnostics.FormatDiagnostics(), Does.Contain("CT031"));
        Assert.That(manifest.Diagnostics.FormatDiagnostics(), Does.Contain("URI010"));
        Assert.That(manifest.Diagnostics.FormatDiagnostics(), Does.Contain("CT030"));
    }

    [Test]
    public void ParseString_ReportsComplexInvalidManifestAndKeepsValidDeclarations()
    {
        string xml =
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
            "<Default Extension=\"JPG\" ContentType=\"image/jpeg\"/>" +
            "<Default Extension=\"jpg\" ContentType=\"image/jpeg\"/>" +
            "<Default Extension=\"bad#ext\" ContentType=\"application/xml\"/>" +
            "<Default Extension=\"bin\" ContentType=\"application/octet stream\"/>" +
            "<Override PartName=\"/ppt/slides/slide1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.presentationml.slide+xml\"/>" +
            "<Override PartName=\"/ppt/slides/slide 2.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.presentationml.slide+xml\"/>" +
            "<Override PartName=\"/ppt/slides/slide3.xml?draft=true\" ContentType=\"application/vnd.openxmlformats-officedocument.presentationml.slide+xml\"/>" +
            "<Override PartName=\"/ppt/slides/slide4.xml\" ContentType=\"application/\"/>" +
            "<Override PartName=\"ppt/slides/slide1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.presentationml.slide+xml\"/>" +
            "</Types>";

        PptxContentTypeManifest manifest = PptxContentTypesParser.ParseString(xml);

        Assert.That(manifest.Failed(), Is.True);
        Assert.That(manifest.DefaultCount(), Is.EqualTo(2));
        Assert.That(manifest.OverrideCount(), Is.EqualTo(1));
        Assert.That(manifest.GetDefaultContentType("JPG"), Is.EqualTo(PptxContentTypes.Jpeg));
        Assert.That(manifest.GetContentTypeForPartName("/ppt/slides/slide1.xml"), Is.EqualTo(PptxContentTypes.Slide));

        string diagnostics = manifest.Diagnostics.FormatDiagnostics();
        Assert.That(diagnostics, Does.Contain("CT020"));
        Assert.That(diagnostics, Does.Contain("CT012"));
        Assert.That(diagnostics, Does.Contain("CT032"));
        Assert.That(diagnostics, Does.Contain("URI008"));
        Assert.That(diagnostics, Does.Contain("URI006"));
        Assert.That(diagnostics, Does.Contain("CT031"));
        Assert.That(diagnostics, Does.Contain("CT021"));
    }

    [Test]
    public void ParseString_IgnoresUnknownContentTypeElementsWithWarnings()
    {
        string xml =
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\" xmlns:x=\"urn:custom\">" +
            "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
            "<x:Default Extension=\"bin\" ContentType=\"application/octet-stream\"/>" +
            "<Metadata Name=\"ignored\"/>" +
            "<Override PartName=\"/ppt/presentation.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.presentationml.presentation.main+xml\"/>" +
            "</Types>";

        PptxContentTypeManifest manifest = PptxContentTypesParser.ParseString(xml);

        Assert.That(manifest.Succeeded(), Is.True, manifest.Diagnostics.FormatDiagnostics());
        Assert.That(manifest.DefaultCount(), Is.EqualTo(1));
        Assert.That(manifest.OverrideCount(), Is.EqualTo(1));
        Assert.That(manifest.GetContentTypeForPartName("/ppt/_rels/presentation.xml.rels"), Is.EqualTo(PptxContentTypes.Relationships));
        Assert.That(manifest.GetContentTypeForPartName("/ppt/presentation.xml"), Is.EqualTo(PptxContentTypes.Presentation));
        Assert.That(manifest.Diagnostics.Count(), Is.EqualTo(2));
        Assert.That(manifest.Diagnostics.Diagnostics[0].Severity, Is.EqualTo(PptxDiagnosticSeverity.Warning));
        Assert.That(manifest.Diagnostics.Diagnostics[0].Code, Is.EqualTo("CT004"));
        Assert.That(manifest.Diagnostics.Diagnostics[1].Severity, Is.EqualTo(PptxDiagnosticSeverity.Warning));
        Assert.That(manifest.Diagnostics.Diagnostics[1].Code, Is.EqualTo("CT005"));
    }
}
