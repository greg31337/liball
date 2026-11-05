using System.Text;
using System.Xml;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class MinimumPresentationGenerationTests
{
    [Test]
    public void SavedMinimumPresentation_ReopensWithRequiredPartsAndValidates()
    {
        PptxDocument generated = PptxDocument.CreateMinimumPresentation();

        byte[] bytes = generated.SaveBytes();
        PptxDocument reopened = PptxDocument.OpenBytes(bytes);
        PptxPackage package = reopened.GetPackage();
        PptxResult validation = reopened.Validate();

        Assert.That(package.Succeeded(), Is.True, package.Diagnostics.FormatDiagnostics());
        Assert.That(validation.Succeeded(), Is.True, validation.FormatDiagnostics());
        Assert.That(package.PartCount(), Is.EqualTo(10));
        AssertRequiredPart(package, PptxPartNames.CoreProperties, PptxContentTypes.CoreProperties);
        AssertRequiredPart(package, PptxPartNames.ExtendedProperties, PptxContentTypes.ExtendedProperties);
        AssertRequiredPart(package, PptxPartNames.Presentation, PptxContentTypes.Presentation);
        AssertRequiredPart(package, PptxPartNames.PresentationProperties, PptxContentTypes.PresentationProperties);
        AssertRequiredPart(package, PptxPartNames.ViewProperties, PptxContentTypes.ViewProperties);
        AssertRequiredPart(package, PptxPartNames.TableStyles, PptxContentTypes.TableStyles);
        AssertRequiredPart(package, PptxPartNames.FirstSlideMaster, PptxContentTypes.SlideMaster);
        AssertRequiredPart(package, PptxPartNames.FirstSlideLayout, PptxContentTypes.SlideLayout);
        AssertRequiredPart(package, PptxPartNames.FirstTheme, PptxContentTypes.Theme);
        AssertRequiredPart(package, PptxPartNames.FirstSlide, PptxContentTypes.Slide);
    }

    [Test]
    public void SavedMinimumPresentation_ReopensWithRequiredZipEntries()
    {
        PptxDocument generated = PptxDocument.CreateMinimumPresentation();

        byte[] bytes = generated.SaveBytes();
        PptxDocument reopened = PptxDocument.OpenBytes(bytes);
        PptxPackage package = reopened.GetPackage();

        Assert.That(package.RawEntryCount(), Is.EqualTo(16));
        AssertRawEntry(package, "[Content_Types].xml");
        AssertRawEntry(package, "_rels/.rels");
        AssertRawEntry(package, "docProps/core.xml");
        AssertRawEntry(package, "docProps/app.xml");
        AssertRawEntry(package, "ppt/presentation.xml");
        AssertRawEntry(package, "ppt/_rels/presentation.xml.rels");
        AssertRawEntry(package, "ppt/presProps.xml");
        AssertRawEntry(package, "ppt/viewProps.xml");
        AssertRawEntry(package, "ppt/tableStyles.xml");
        AssertRawEntry(package, "ppt/slideMasters/slideMaster1.xml");
        AssertRawEntry(package, "ppt/slideMasters/_rels/slideMaster1.xml.rels");
        AssertRawEntry(package, "ppt/slideLayouts/slideLayout1.xml");
        AssertRawEntry(package, "ppt/slideLayouts/_rels/slideLayout1.xml.rels");
        AssertRawEntry(package, "ppt/theme/theme1.xml");
        AssertRawEntry(package, "ppt/slides/slide1.xml");
        AssertRawEntry(package, "ppt/slides/_rels/slide1.xml.rels");
    }

    [Test]
    public void SavedMinimumPresentation_ReopenedRelationshipsReachRequiredParts()
    {
        PptxPackage package = ReopenGeneratedPackage();

        AssertRelationship(package.RootRelationships, "rId1", PptxRelationshipTypes.OfficeDocument, PptxPartNames.Presentation);
        AssertRelationship(package.RootRelationships, "rId2", PptxRelationshipTypes.CoreProperties, PptxPartNames.CoreProperties);
        AssertRelationship(package.RootRelationships, "rId3", PptxRelationshipTypes.ExtendedProperties, PptxPartNames.ExtendedProperties);
        AssertRelationship(package.GetRelationships(PptxPartNames.Presentation), "rId1", PptxRelationshipTypes.SlideMaster, PptxPartNames.FirstSlideMaster);
        AssertRelationship(package.GetRelationships(PptxPartNames.Presentation), "rId2", PptxRelationshipTypes.Slide, PptxPartNames.FirstSlide);
        AssertRelationship(package.GetRelationships(PptxPartNames.Presentation), "rId3", PptxRelationshipTypes.PresentationProperties, PptxPartNames.PresentationProperties);
        AssertRelationship(package.GetRelationships(PptxPartNames.Presentation), "rId4", PptxRelationshipTypes.ViewProperties, PptxPartNames.ViewProperties);
        AssertRelationship(package.GetRelationships(PptxPartNames.Presentation), "rId5", PptxRelationshipTypes.TableStyles, PptxPartNames.TableStyles);
        AssertRelationship(package.GetRelationships(PptxPartNames.FirstSlideMaster), "rId1", PptxRelationshipTypes.Theme, PptxPartNames.FirstTheme);
        AssertRelationship(package.GetRelationships(PptxPartNames.FirstSlideMaster), "rId2", PptxRelationshipTypes.SlideLayout, PptxPartNames.FirstSlideLayout);
        AssertRelationship(package.GetRelationships(PptxPartNames.FirstSlideLayout), "rId1", PptxRelationshipTypes.SlideMaster, PptxPartNames.FirstSlideMaster);
        AssertRelationship(package.GetRelationships(PptxPartNames.FirstSlide), "rId1", PptxRelationshipTypes.SlideLayout, PptxPartNames.FirstSlideLayout);
    }

    [Test]
    public void SavedMinimumPresentation_ReopenedRequiredXmlPartsAreLoadable()
    {
        PptxPackage package = ReopenGeneratedPackage();

        AssertXmlRoot(package, PptxPartNames.CoreProperties, "coreProperties", PptxNamespaces.CoreProperties);
        AssertXmlRoot(package, PptxPartNames.ExtendedProperties, "Properties", PptxNamespaces.ExtendedProperties);
        AssertXmlRoot(package, PptxPartNames.Presentation, "presentation", PptxNamespaces.Presentation);
        AssertXmlRoot(package, PptxPartNames.PresentationProperties, "presentationPr", PptxNamespaces.Presentation);
        AssertXmlRoot(package, PptxPartNames.ViewProperties, "viewPr", PptxNamespaces.Presentation);
        AssertXmlRoot(package, PptxPartNames.TableStyles, "tblStyleLst", PptxNamespaces.Drawing);
        AssertXmlRoot(package, PptxPartNames.FirstSlideMaster, "sldMaster", PptxNamespaces.Presentation);
        AssertXmlRoot(package, PptxPartNames.FirstSlideLayout, "sldLayout", PptxNamespaces.Presentation);
        AssertXmlRoot(package, PptxPartNames.FirstTheme, "theme", PptxNamespaces.Drawing);
        AssertXmlRoot(package, PptxPartNames.FirstSlide, "sld", PptxNamespaces.Presentation);
    }

    static PptxPackage ReopenGeneratedPackage()
    {
        PptxDocument generated = PptxDocument.CreateMinimumPresentation();
        byte[] bytes = generated.SaveBytes();
        PptxDocument reopened = PptxDocument.OpenBytes(bytes);
        return reopened.GetPackage();
    }

    static void AssertRequiredPart(PptxPackage package, string partName, string contentType)
    {
        Assert.That(package.HasPart(partName), Is.True);
        Assert.That(package.GetPart(partName).ContentType, Is.EqualTo(contentType));
        Assert.That(package.ContentTypes.GetOverrideContentType(partName), Is.EqualTo(contentType));
    }

    static void AssertRawEntry(PptxPackage package, string entryName)
    {
        PptxZipEntry? entry = PptxPackage.FindRawEntry(package, entryName);

        Assert.That(entry, Is.Not.Null);
        Assert.That(entry!.Bytes.Length, Is.GreaterThan(0));
    }

    static void AssertRelationship(PptxRelationshipCollection relationships, string id, string type, string targetPartName)
    {
        int index = relationships.IndexOfId(id);

        Assert.That(index, Is.GreaterThanOrEqualTo(0));
        Assert.That(relationships.Relationships[index].Type, Is.EqualTo(type));
        Assert.That(relationships.Relationships[index].TargetMode, Is.EqualTo(PptxRelationshipTargetMode.Internal));
        Assert.That(relationships.Relationships[index].TargetPartName, Is.EqualTo(targetPartName));
    }

    static void AssertXmlRoot(PptxPackage package, string partName, string localName, string namespaceUri)
    {
        PptxPart part = package.GetPart(partName);
        XmlDocument document = PptxXml.LoadString(Encoding.UTF8.GetString(part.GetBytes()));

        Assert.That(document.DocumentElement, Is.Not.Null);
        Assert.That(document.DocumentElement!.LocalName, Is.EqualTo(localName));
        Assert.That(document.DocumentElement.NamespaceURI, Is.EqualTo(namespaceUri));
    }
}
