using System.Xml;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PptxMinimumPresentationTests
{
    [Test]
    public void CreatePackage_AddsMinimumPresentationParts()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();

        Assert.That(package.HasPart(PptxPartNames.CoreProperties), Is.True);
        Assert.That(package.HasPart(PptxPartNames.ExtendedProperties), Is.True);
        Assert.That(package.HasPart(PptxPartNames.Presentation), Is.True);
        Assert.That(package.HasPart(PptxPartNames.PresentationProperties), Is.True);
        Assert.That(package.HasPart(PptxPartNames.ViewProperties), Is.True);
        Assert.That(package.HasPart(PptxPartNames.TableStyles), Is.True);
        Assert.That(package.HasPart(PptxPartNames.FirstSlideMaster), Is.True);
        Assert.That(package.HasPart(PptxPartNames.FirstSlideLayout), Is.True);
        Assert.That(package.HasPart(PptxPartNames.FirstTheme), Is.True);
        Assert.That(package.HasPart(PptxPartNames.FirstSlide), Is.True);
        Assert.That(package.ContentTypes.GetOverrideContentType(PptxPartNames.Presentation), Is.EqualTo(PptxContentTypes.Presentation));
        Assert.That(package.ContentTypes.GetOverrideContentType(PptxPartNames.FirstSlide), Is.EqualTo(PptxContentTypes.Slide));
        Assert.That(package.ContentTypes.GetOverrideContentType(PptxPartNames.FirstSlideMaster), Is.EqualTo(PptxContentTypes.SlideMaster));
        Assert.That(package.ContentTypes.GetOverrideContentType(PptxPartNames.FirstSlideLayout), Is.EqualTo(PptxContentTypes.SlideLayout));
        Assert.That(package.ContentTypes.GetOverrideContentType(PptxPartNames.FirstTheme), Is.EqualTo(PptxContentTypes.Theme));
    }

    [Test]
    public void CreatePackage_AddsMinimumPresentationRelationships()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();

        AssertRelationship(package.RootRelationships, "rId1", PptxRelationshipTypes.OfficeDocument, "/ppt/presentation.xml");
        AssertRelationship(package.RootRelationships, "rId2", PptxRelationshipTypes.CoreProperties, "/docProps/core.xml");
        AssertRelationship(package.RootRelationships, "rId3", PptxRelationshipTypes.ExtendedProperties, "/docProps/app.xml");

        AssertRelationship(package.GetRelationships(PptxPartNames.Presentation), "rId1", PptxRelationshipTypes.SlideMaster, "/ppt/slideMasters/slideMaster1.xml");
        AssertRelationship(package.GetRelationships(PptxPartNames.Presentation), "rId2", PptxRelationshipTypes.Slide, "/ppt/slides/slide1.xml");
        AssertRelationship(package.GetRelationships(PptxPartNames.Presentation), "rId3", PptxRelationshipTypes.PresentationProperties, "/ppt/presProps.xml");
        AssertRelationship(package.GetRelationships(PptxPartNames.Presentation), "rId4", PptxRelationshipTypes.ViewProperties, "/ppt/viewProps.xml");
        AssertRelationship(package.GetRelationships(PptxPartNames.Presentation), "rId5", PptxRelationshipTypes.TableStyles, "/ppt/tableStyles.xml");

        AssertRelationship(package.GetRelationships(PptxPartNames.FirstSlideMaster), "rId1", PptxRelationshipTypes.Theme, "/ppt/theme/theme1.xml");
        AssertRelationship(package.GetRelationships(PptxPartNames.FirstSlideMaster), "rId2", PptxRelationshipTypes.SlideLayout, "/ppt/slideLayouts/slideLayout1.xml");
        AssertRelationship(package.GetRelationships(PptxPartNames.FirstSlideLayout), "rId1", PptxRelationshipTypes.SlideMaster, "/ppt/slideMasters/slideMaster1.xml");
        AssertRelationship(package.GetRelationships(PptxPartNames.FirstSlide), "rId1", PptxRelationshipTypes.SlideLayout, "/ppt/slideLayouts/slideLayout1.xml");
    }

    [Test]
    public void CreateMinimumPresentation_SaveReopenAndValidateSucceeds()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();

        byte[] bytes = document.SaveBytes();
        PptxDocument opened = PptxDocument.OpenBytes(bytes);
        PptxResult result = opened.Validate();

        Assert.That(bytes.Length, Is.GreaterThan(0));
        Assert.That(opened.GetPackage().HasPart(PptxPartNames.Presentation), Is.True);
        Assert.That(opened.GetPackage().HasPart(PptxPartNames.FirstSlide), Is.True);
        Assert.That(result.Succeeded(), Is.True, result.FormatDiagnostics());
    }

    [Test]
    public void CreatePresentationAlias_ReturnsMinimumPresentation()
    {
        PptxDocument document = PptxDocument.CreatePresentation();

        Assert.That(document.GetPackage().HasPart(PptxPartNames.Presentation), Is.True);
        Assert.That(document.GetPackage().HasPart(PptxPartNames.FirstSlide), Is.True);
        Assert.That(document.Validate().Succeeded(), Is.True, document.Validate().FormatDiagnostics());
    }

    [Test]
    public void PresentationXml_ReferencesGeneratedMasterAndSlideRelationshipIds()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        PptxPart part = package.GetPart(PptxPartNames.Presentation);
        XmlDocument document = PptxXml.LoadBytes(part.GetBytes());
        XmlNamespaceManager namespaces = new XmlNamespaceManager(document.NameTable);
        namespaces.AddNamespace("p", PptxNamespaces.Presentation);
        namespaces.AddNamespace("r", PptxNamespaces.Relationships);

        XmlNode? master = document.SelectSingleNode("/p:presentation/p:sldMasterIdLst/p:sldMasterId", namespaces);
        XmlNode? slide = document.SelectSingleNode("/p:presentation/p:sldIdLst/p:sldId", namespaces);

        Assert.That(master, Is.Not.Null);
        Assert.That(slide, Is.Not.Null);
        Assert.That(((XmlElement)master!).GetAttribute("id"), Is.EqualTo("2147483648"));
        Assert.That(((XmlElement)master).GetAttribute("id", PptxNamespaces.Relationships), Is.EqualTo("rId1"));
        Assert.That(((XmlElement)slide!).GetAttribute("id"), Is.EqualTo("256"));
        Assert.That(((XmlElement)slide).GetAttribute("id", PptxNamespaces.Relationships), Is.EqualTo("rId2"));
    }

    public static void AssertRelationship(PptxRelationshipCollection relationships, string id, string type, string targetPartName)
    {
        int index = relationships.IndexOfId(id);

        Assert.That(index, Is.GreaterThanOrEqualTo(0));
        Assert.That(relationships.Relationships[index].Type, Is.EqualTo(type));
        Assert.That(relationships.Relationships[index].TargetMode, Is.EqualTo(PptxRelationshipTargetMode.Internal));
        Assert.That(relationships.Relationships[index].TargetPartName, Is.EqualTo(targetPartName));
    }
}
