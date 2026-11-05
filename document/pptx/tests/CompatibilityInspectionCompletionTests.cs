using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class CompatibilityInspectionCompletionTests
{
    [Test]
    public void InspectDocument_ClassifiesEditableAndReadableFeaturesInStandardDeck()
    {
        PptxDocument document = new PptxDocumentFactory().CreatePresentation();
        PptxCompatibilityInspection inspection = new PptxCompatibilityInspector().InspectDocument(document);

        Assert.That(inspection.Editable.Count, Is.GreaterThan(0));
        Assert.That(inspection.Readable.Count, Is.GreaterThan(0));
        Assert.That(inspection.PreservedOnly, Is.Empty);
        Assert.That(inspection.Unsupported, Is.Empty);
        Assert.That(inspection.Unsafe, Is.Empty);
        Assert.That(FindFeature(inspection, PptxCompatibilityLevel.Editable, PptxPartNames.FirstSlide, ""), Is.Not.Null);
        Assert.That(FindFeature(inspection, PptxCompatibilityLevel.Readable, PptxPartNames.FirstSlideMaster, ""), Is.Not.Null);
        Assert.That(FindFeature(inspection, PptxCompatibilityLevel.Readable, PptxPartNames.FirstSlideLayout, ""), Is.Not.Null);
        Assert.That(FindFeature(inspection, PptxCompatibilityLevel.Readable, PptxPartNames.FirstTheme, ""), Is.Not.Null);
        Assert.That(inspection.Features.Count, Is.EqualTo(inspection.Editable.Count + inspection.Readable.Count));
        Assert.That(inspection.SecurityDiagnostics.Diagnostics, Is.Empty);
    }

    [Test]
    public void Inspect_ClassifiesPreservedUnsupportedAndUnsafeFeaturesWithLocations()
    {
        PptxPackage package = CreateMixedCompatibilityPackage();
        PptxCompatibilityInspection inspection = new PptxCompatibilityInspector().Inspect(package);
        PptxCompatibilityFeature? unknownPart = FindFeature(inspection, PptxCompatibilityLevel.PreservedOnly, "/custom/unknown.bin", "");
        PptxCompatibilityFeature? extensionMarkup = FindNamedFeature(inspection, "Extension markup", PptxPartNames.FirstSlide);
        PptxCompatibilityFeature? unsupportedPart = FindFeature(inspection, PptxCompatibilityLevel.Unsupported, "/ppt/model3d/model1.bin", "");
        PptxCompatibilityFeature? unsafePart = FindFeature(inspection, PptxCompatibilityLevel.Unsafe, "/ppt/vbaProject.bin", "");
        PptxCompatibilityFeature? unsafeRelationship = FindFeature(inspection, PptxCompatibilityLevel.Unsafe, PptxPartNames.FirstSlide, "rIdExternal");

        Assert.That(unknownPart, Is.Not.Null);
        Assert.That(unknownPart!.Details, Does.Contain("retained"));
        Assert.That(extensionMarkup, Is.Not.Null);
        Assert.That(unsupportedPart, Is.Not.Null);
        Assert.That(unsupportedPart!.Details, Does.Contain("outside"));
        Assert.That(unsafePart, Is.Not.Null);
        Assert.That(unsafeRelationship, Is.Not.Null);
        Assert.That(unsafeRelationship!.Details, Does.Contain("external"));
        Assert.That(inspection.SecurityDiagnostics.FormatDiagnostics(), Does.Contain("SEC001"));
        Assert.That(inspection.SecurityDiagnostics.FormatDiagnostics(), Does.Contain("SEC010"));
    }

    [Test]
    public void Inspect_DoesNotLoseUnknownXmlBinaryPartsOrRelationshipsOnRoundTrip()
    {
        PptxPackage package = CreateMixedCompatibilityPackage();
        byte[] unknownBytes = package.GetPart("/custom/unknown.bin").GetBytes();
        byte[] unsupportedBytes = package.GetPart("/ppt/model3d/model1.bin").GetBytes();
        byte[] slideBytes = package.GetPart(PptxPartNames.FirstSlide).GetBytes();

        new PptxCompatibilityInspector().Inspect(package);
        PptxPackage reopened = new PptxPackageOperations().OpenBytes(package.SaveBytes());

        Assert.That(reopened.GetPart("/custom/unknown.bin").GetBytes(), Is.EqualTo(unknownBytes));
        Assert.That(reopened.GetPart("/ppt/model3d/model1.bin").GetBytes(), Is.EqualTo(unsupportedBytes));
        Assert.That(reopened.GetPart(PptxPartNames.FirstSlide).GetBytes(), Is.EqualTo(slideBytes));
        Assert.That(reopened.GetRelationships(PptxPartNames.Presentation).ContainsId("rIdUnknown"), Is.True);
        Assert.That(reopened.GetRelationships(PptxPartNames.Presentation).ContainsId("rIdWebExtension"), Is.True);
        Assert.That(reopened.GetRelationships(PptxPartNames.FirstSlide).ContainsId("rIdExternal"), Is.True);
    }

    static PptxPackage CreateMixedCompatibilityPackage()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(PptxPartNames.FirstSlide, PptxContentTypes.Slide, Encoding.UTF8.GetBytes(CreateExtensionSlideXml()));
        package.AddPart("/custom/unknown.bin", "application/vnd.example.unknown", new byte[] { 0, 1, 2, 254, 255 });
        package.AddPart("/ppt/model3d/model1.bin", "application/vnd.ms-office.model3dmodel", new byte[] { 4, 3, 2, 1 });
        package.AddPart("/ppt/vbaProject.bin", PptxContentTypes.VbaProject, new byte[] { 9, 8, 7 });
        package.AddRelationshipWithId(PptxPartNames.Presentation, "rIdUnknown", "http://example.test/relationships/opaque", "../custom/unknown.bin", PptxRelationshipTargetMode.Internal);
        package.AddRelationshipWithId(PptxPartNames.Presentation, "rIdWebExtension", "http://schemas.microsoft.com/office/2011/relationships/webextension", "model3d/model1.bin", PptxRelationshipTargetMode.Internal);
        package.AddRelationshipWithId(PptxPartNames.Presentation, "rIdVba", PptxRelationshipTypes.VbaProject, "vbaProject.bin", PptxRelationshipTargetMode.Internal);
        package.AddRelationshipWithId(PptxPartNames.FirstSlide, "rIdExternal", PptxRelationshipTypes.Hyperlink, "https://example.test/resource", PptxRelationshipTargetMode.External);
        return package;
    }

    static string CreateExtensionSlideXml()
    {
        return
            "<p:sld xmlns:p=\"" + PptxNamespaces.Presentation + "\" xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:x=\"urn:example:extension\">" +
            "<p:cSld><p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>" +
            "</p:spTree></p:cSld>" +
            "<p:extLst><p:ext uri=\"urn:example:preserve\"><x:data value=\"opaque\"/></p:ext></p:extLst>" +
            "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>" +
            "</p:sld>";
    }

    static PptxCompatibilityFeature? FindFeature(PptxCompatibilityInspection inspection, PptxCompatibilityLevel level, string partName, string relationshipId)
    {
        for (int index = 0; index < inspection.Features.Count; index++)
        {
            PptxCompatibilityFeature feature = inspection.Features[index];
            if (feature.Level == level && feature.PartName == partName && feature.RelationshipId == relationshipId)
            {
                return feature;
            }
        }

        return null;
    }

    static PptxCompatibilityFeature? FindNamedFeature(PptxCompatibilityInspection inspection, string name, string partName)
    {
        for (int index = 0; index < inspection.Features.Count; index++)
        {
            PptxCompatibilityFeature feature = inspection.Features[index];
            if (feature.Name == name && feature.PartName == partName)
            {
                return feature;
            }
        }

        return null;
    }
}
