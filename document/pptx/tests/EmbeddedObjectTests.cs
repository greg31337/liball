using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class EmbeddedObjectTests
{
    [Test]
    public void Slide_LoadsEmbeddedOlePackageAndLinkedOleInventory()
    {
        PptxPresentation presentation = LoadEmbeddedObjectPresentation();
        PptxSlide slide = presentation.GetSlide(0);
        List<PptxEmbeddedObject> objects = slide.GetEmbeddedObjects();
        PptxEmbeddedObject ole = objects[0];
        PptxEmbeddedObject package = objects[1];
        PptxEmbeddedObject linked = objects[2];

        Assert.That(objects.Count, Is.EqualTo(3));
        Assert.That(presentation.GetEmbeddedObjects().Count, Is.EqualTo(3));
        Assert.That(slide.GetEmbeddedObject(0).RelationshipId, Is.EqualTo("rIdOle"));
        Assert.That(ole.Shape.IsEmbeddedObject(), Is.True);
        Assert.That(ole.Shape.IsChart(), Is.False);
        Assert.That(ole.Shape.IsTable(), Is.False);
        Assert.That(ole.RelationshipType, Is.EqualTo(PptxRelationshipTypes.OleObject));
        Assert.That(ole.PartName, Is.EqualTo("/ppt/embeddings/oleObject1.bin"));
        Assert.That(ole.ContentType, Is.EqualTo(PptxContentTypes.OleObject));
        Assert.That(ole.Bytes, Is.EqualTo(CreateOleBytes()));
        Assert.That(ole.ProgramId, Is.EqualTo("Excel.Sheet.12"));
        Assert.That(ole.Name, Is.EqualTo("Workbook"));
        Assert.That(ole.ShowAsIcon, Is.True);
        Assert.That(ole.Embedded, Is.True);
        Assert.That(ole.Linked, Is.False);
        Assert.That(ole.IsOleObject(), Is.True);
        Assert.That(ole.IsEmbeddedPackage(), Is.False);
        Assert.That(package.RelationshipType, Is.EqualTo(PptxRelationshipTypes.Package));
        Assert.That(package.PartName, Is.EqualTo("/ppt/embeddings/package1.bin"));
        Assert.That(package.ContentType, Is.EqualTo(PptxContentTypes.Binary));
        Assert.That(package.Bytes, Is.EqualTo(CreatePackageBytes()));
        Assert.That(package.ProgramId, Is.EqualTo("Package"));
        Assert.That(package.IsOleObject(), Is.False);
        Assert.That(package.IsEmbeddedPackage(), Is.True);
        Assert.That(linked.Embedded, Is.False);
        Assert.That(linked.Linked, Is.True);
        Assert.That(linked.Target, Is.EqualTo("https://example.com/source.xlsx"));
        Assert.That(linked.IsOleObject(), Is.True);
        Assert.That(linked.ScanSecurity().FormatDiagnostics(), Does.Contain("SEC001"));
        Assert.That(linked.ScanSecurity().FormatDiagnostics(), Does.Contain("SEC030"));
    }

    [Test]
    public void EmbeddedObjectReplacementAndPartsPreserveOnRoundTrip()
    {
        PptxPresentation presentation = LoadEmbeddedObjectPresentation();
        PptxEmbeddedObject ole = presentation.GetSlide(0).GetEmbeddedObject(0);
        PptxEmbeddedObject package = presentation.GetSlide(0).GetEmbeddedObject(1);

        ole.ReplaceBytes(CreateReplacementOleBytes(), PptxContentTypes.OleObject);
        package.ReplaceBytes(CreateReplacementPackageBytes(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxEmbeddedObject reopenedOle = reopened.GetSlide(0).GetEmbeddedObject(0);
        PptxEmbeddedObject reopenedPackage = reopened.GetSlide(0).GetEmbeddedObject(1);
        PptxRelationshipCollection relationships = reopened.Package.GetRelationships(PptxPartNames.FirstSlide);
        string diagnostics = reopened.Package.ScanSecurity().FormatDiagnostics();

        Assert.That(reopened.Package.HasPart("/ppt/embeddings/oleObject1.bin"), Is.True);
        Assert.That(reopened.Package.HasPart("/ppt/embeddings/package1.bin"), Is.True);
        Assert.That(reopenedOle.GetBytes(), Is.EqualTo(CreateReplacementOleBytes()));
        Assert.That(reopenedOle.ContentType, Is.EqualTo(PptxContentTypes.OleObject));
        Assert.That(reopenedPackage.GetBytes(), Is.EqualTo(CreateReplacementPackageBytes()));
        Assert.That(reopenedPackage.ContentType, Is.EqualTo("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));
        Assert.That(FindRelationship(relationships, "rIdOle").TargetPartName, Is.EqualTo("/ppt/embeddings/oleObject1.bin"));
        Assert.That(FindRelationship(relationships, "rIdPackage").TargetPartName, Is.EqualTo("/ppt/embeddings/package1.bin"));
        Assert.That(FindRelationship(relationships, "rIdLinkedOle").Target, Is.EqualTo("https://example.com/source.xlsx"));
        Assert.That(reopened.GetSlide(0).Part.GetBytes(), Is.EqualTo(Encoding.UTF8.GetBytes(CreateEmbeddedObjectSlideXml())));
        Assert.That(reopenedOle.ScanSecurity().FormatDiagnostics(), Does.Contain("SEC030"));
        Assert.That(reopenedPackage.ScanSecurity().FormatDiagnostics(), Does.Contain("SEC040"));
        Assert.That(diagnostics, Does.Contain("SEC001"));
        Assert.That(diagnostics, Does.Contain("SEC030"));
        Assert.That(diagnostics, Does.Contain("SEC040"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void EmbeddedObjectReplacementRejectsLinkedObjects()
    {
        PptxPresentation presentation = LoadEmbeddedObjectPresentation();
        PptxEmbeddedObject linked = presentation.GetSlide(0).GetEmbeddedObject(2);

        Assert.Throws<System.ArgumentException>(() => linked.ReplaceBytes(new byte[] { 1 }, PptxContentTypes.OleObject));
    }

    static PptxPresentation LoadEmbeddedObjectPresentation()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(PptxPartNames.FirstSlide, PptxContentTypes.Slide, Encoding.UTF8.GetBytes(CreateEmbeddedObjectSlideXml()));
        package.AddPart("/ppt/embeddings/oleObject1.bin", PptxContentTypes.OleObject, CreateOleBytes());
        package.AddPart("/ppt/embeddings/package1.bin", PptxContentTypes.Binary, CreatePackageBytes());
        package.AddRelationshipWithId(PptxPartNames.FirstSlide, "rIdOle", PptxRelationshipTypes.OleObject, "../embeddings/oleObject1.bin", PptxRelationshipTargetMode.Internal);
        package.AddRelationshipWithId(PptxPartNames.FirstSlide, "rIdPackage", PptxRelationshipTypes.Package, "../embeddings/package1.bin", PptxRelationshipTargetMode.Internal);
        package.AddRelationshipWithId(PptxPartNames.FirstSlide, "rIdLinkedOle", PptxRelationshipTypes.OleObject, "https://example.com/source.xlsx", PptxRelationshipTargetMode.External);
        PptxDocument document = PptxDocument.OpenBytes(package.SaveBytes());
        return document.LoadPresentation();
    }

    static PptxPresentation SaveReopenAndLoad(PptxPresentation presentation)
    {
        PptxDocument document = new PptxDocument(presentation.Package);
        byte[] bytes = document.SaveBytes();
        PptxDocument reopened = PptxDocument.OpenBytes(bytes);
        return reopened.LoadPresentation();
    }

    static string CreateEmbeddedObjectSlideXml()
    {
        return
            "<p:sld xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:cSld>" +
            "<p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>" +
            CreateEmbeddedObjectFrameXml("2", "Ole Object", "rIdOle", "Excel.Sheet.12", "Workbook", "1", "10") +
            CreateEmbeddedObjectFrameXml("3", "Package Object", "rIdPackage", "Package", "Report", "0", "500") +
            CreateEmbeddedObjectFrameXml("4", "Linked Object", "rIdLinkedOle", "Excel.Sheet.12", "Linked Workbook", "1", "990") +
            "</p:spTree>" +
            "</p:cSld>" +
            "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>" +
            "</p:sld>";
    }

    static string CreateEmbeddedObjectFrameXml(string id, string frameName, string relationshipId, string programId, string objectName, string showAsIcon, string x)
    {
        return
            "<p:graphicFrame>" +
            "<p:nvGraphicFramePr><p:cNvPr id=\"" + id + "\" name=\"" + frameName + "\"/><p:cNvGraphicFramePr/><p:nvPr/></p:nvGraphicFramePr>" +
            "<p:xfrm><a:off x=\"" + x + "\" y=\"20\"/><a:ext cx=\"400\" cy=\"200\"/></p:xfrm>" +
            "<a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/presentationml/2006/ole\">" +
            "<p:oleObj r:id=\"" + relationshipId + "\" progId=\"" + programId + "\" name=\"" + objectName + "\" showAsIcon=\"" + showAsIcon + "\" imgW=\"400\" imgH=\"200\"><p:embed/></p:oleObj>" +
            "</a:graphicData></a:graphic>" +
            "</p:graphicFrame>";
    }

    static PptxRelationship FindRelationship(PptxRelationshipCollection relationships, string id)
    {
        for (int index = 0; index < relationships.Relationships.Count; index++)
        {
            if (relationships.Relationships[index].Id == id)
            {
                return relationships.Relationships[index];
            }
        }

        throw new System.ArgumentException("Relationship was not found.");
    }

    static byte[] CreateOleBytes()
    {
        return new byte[] { 208, 207, 17, 224, 1, 2, 3, 4 };
    }

    static byte[] CreatePackageBytes()
    {
        return new byte[] { 80, 75, 3, 4, 5, 6, 7, 8 };
    }

    static byte[] CreateReplacementOleBytes()
    {
        return new byte[] { 208, 207, 17, 224, 9, 8, 7, 6 };
    }

    static byte[] CreateReplacementPackageBytes()
    {
        return new byte[] { 80, 75, 3, 4, 9, 9, 9, 9 };
    }
}
