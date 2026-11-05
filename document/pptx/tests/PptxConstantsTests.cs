using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PptxConstantsTests
{
    [Test]
    public void NamespaceConstants_ExposeCoreOpenXmlNamespaces()
    {
        Assert.That(PptxNamespaces.ContentTypes, Is.EqualTo("http://schemas.openxmlformats.org/package/2006/content-types"));
        Assert.That(PptxNamespaces.PackageRelationships, Is.EqualTo("http://schemas.openxmlformats.org/package/2006/relationships"));
        Assert.That(PptxNamespaces.Presentation, Is.EqualTo("http://schemas.openxmlformats.org/presentationml/2006/main"));
        Assert.That(PptxNamespaces.Drawing, Is.EqualTo("http://schemas.openxmlformats.org/drawingml/2006/main"));
        Assert.That(PptxNamespaces.Relationships, Is.EqualTo("http://schemas.openxmlformats.org/officeDocument/2006/relationships"));
    }

    [Test]
    public void RelationshipTypeConstants_ExposePackageAndPresentationRelationships()
    {
        Assert.That(PptxRelationshipTypes.OfficeDocument, Is.EqualTo("http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"));
        Assert.That(PptxRelationshipTypes.CoreProperties, Is.EqualTo("http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties"));
        Assert.That(PptxRelationshipTypes.Slide, Is.EqualTo("http://schemas.openxmlformats.org/officeDocument/2006/relationships/slide"));
        Assert.That(PptxRelationshipTypes.SlideMaster, Is.EqualTo("http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideMaster"));
        Assert.That(PptxRelationshipTypes.Theme, Is.EqualTo("http://schemas.openxmlformats.org/officeDocument/2006/relationships/theme"));
        Assert.That(PptxRelationshipTypes.Hyperlink, Is.EqualTo("http://schemas.openxmlformats.org/officeDocument/2006/relationships/hyperlink"));
    }

    [Test]
    public void ContentTypeConstants_ExposeDefaultsAndPresentationOverrides()
    {
        Assert.That(PptxContentTypes.Relationships, Is.EqualTo("application/vnd.openxmlformats-package.relationships+xml"));
        Assert.That(PptxContentTypes.Xml, Is.EqualTo("application/xml"));
        Assert.That(PptxContentTypes.Presentation, Is.EqualTo("application/vnd.openxmlformats-officedocument.presentationml.presentation.main+xml"));
        Assert.That(PptxContentTypes.Slide, Is.EqualTo("application/vnd.openxmlformats-officedocument.presentationml.slide+xml"));
        Assert.That(PptxContentTypes.SlideLayout, Is.EqualTo("application/vnd.openxmlformats-officedocument.presentationml.slideLayout+xml"));
        Assert.That(PptxContentTypes.Theme, Is.EqualTo("application/vnd.openxmlformats-officedocument.theme+xml"));
        Assert.That(PptxContentTypes.Png, Is.EqualTo("image/png"));
        Assert.That(PptxContentTypes.Jpeg, Is.EqualTo("image/jpeg"));
    }

    [Test]
    public void PartNameConstants_ExposeCommonPackageLocations()
    {
        Assert.That(PptxPartNames.ContentTypesEntry, Is.EqualTo("[Content_Types].xml"));
        Assert.That(PptxPartNames.RootRelationships, Is.EqualTo("/_rels/.rels"));
        Assert.That(PptxPartNames.CoreProperties, Is.EqualTo("/docProps/core.xml"));
        Assert.That(PptxPartNames.ExtendedProperties, Is.EqualTo("/docProps/app.xml"));
        Assert.That(PptxPartNames.Presentation, Is.EqualTo("/ppt/presentation.xml"));
        Assert.That(PptxPartNames.FirstSlide, Is.EqualTo("/ppt/slides/slide1.xml"));
        Assert.That(PptxPartNames.FirstSlideLayout, Is.EqualTo("/ppt/slideLayouts/slideLayout1.xml"));
        Assert.That(PptxPartNames.FirstTheme, Is.EqualTo("/ppt/theme/theme1.xml"));
    }
}
