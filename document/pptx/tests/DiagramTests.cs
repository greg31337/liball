using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class DiagramTests
{
    [Test]
    public void Slide_LoadsDiagramInventoryAndExtractsText()
    {
        PptxPresentation presentation = LoadDiagramPresentation();
        PptxSlide slide = presentation.GetSlide(0);
        List<PptxDiagram> diagrams = slide.GetDiagrams();
        PptxDiagram diagram = diagrams[0];

        Assert.That(diagrams.Count, Is.EqualTo(1));
        Assert.That(slide.GetDiagram(0).DataRelationshipId, Is.EqualTo("rIdData"));
        Assert.That(diagram.Shape.IsDiagram(), Is.True);
        Assert.That(diagram.Shape.IsChart(), Is.False);
        Assert.That(diagram.Shape.IsTable(), Is.False);
        Assert.That(diagram.Shape.GetDiagramRelationshipId("dm"), Is.EqualTo("rIdData"));
        Assert.That(diagram.Shape.GetDiagramRelationshipId("lo"), Is.EqualTo("rIdLayout"));
        Assert.That(diagram.Shape.GetDiagramRelationshipId("qs"), Is.EqualTo("rIdQuick"));
        Assert.That(diagram.Shape.GetDiagramRelationshipId("cs"), Is.EqualTo("rIdColors"));
        Assert.That(diagram.DataPart, Is.Not.Null);
        Assert.That(diagram.LayoutPart, Is.Not.Null);
        Assert.That(diagram.QuickStylePart, Is.Not.Null);
        Assert.That(diagram.ColorsPart, Is.Not.Null);
        Assert.That(diagram.DataPart!.PartName, Is.EqualTo("/ppt/diagrams/data1.xml"));
        Assert.That(diagram.LayoutPart!.PartName, Is.EqualTo("/ppt/diagrams/layout1.xml"));
        Assert.That(diagram.QuickStylePart!.PartName, Is.EqualTo("/ppt/diagrams/quickStyle1.xml"));
        Assert.That(diagram.ColorsPart!.PartName, Is.EqualTo("/ppt/diagrams/colors1.xml"));
        Assert.That(diagram.GetParts().Count, Is.EqualTo(4));
        Assert.That(diagram.ExtractText(), Is.EqualTo("SmartArt Node One\nSmartArt Node Two"));
        Assert.That(presentation.ExtractDiagramsText(0), Is.EqualTo("SmartArt Node One\nSmartArt Node Two"));
        Assert.That(presentation.ExtractText(), Does.Contain("SmartArt Node One"));
        Assert.That(presentation.ExtractText(), Does.Contain("SmartArt Node Two"));
    }

    [Test]
    public void DiagramPartsPreserveOnRoundTrip()
    {
        PptxPresentation presentation = LoadDiagramPresentation();
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxDiagram diagram = reopened.GetSlide(0).GetDiagram(0);
        PptxRelationshipCollection relationships = reopened.Package.GetRelationships(PptxPartNames.FirstSlide);

        Assert.That(reopened.Package.HasPart("/ppt/diagrams/data1.xml"), Is.True);
        Assert.That(reopened.Package.HasPart("/ppt/diagrams/layout1.xml"), Is.True);
        Assert.That(reopened.Package.HasPart("/ppt/diagrams/quickStyle1.xml"), Is.True);
        Assert.That(reopened.Package.HasPart("/ppt/diagrams/colors1.xml"), Is.True);
        Assert.That(reopened.Package.GetPart("/ppt/diagrams/data1.xml").GetBytes(), Is.EqualTo(Encoding.UTF8.GetBytes(CreateDiagramDataXml())));
        Assert.That(FindRelationship(relationships, PptxRelationshipTypes.DiagramData).TargetPartName, Is.EqualTo("/ppt/diagrams/data1.xml"));
        Assert.That(FindRelationship(relationships, PptxRelationshipTypes.DiagramLayout).TargetPartName, Is.EqualTo("/ppt/diagrams/layout1.xml"));
        Assert.That(FindRelationship(relationships, PptxRelationshipTypes.DiagramQuickStyle).TargetPartName, Is.EqualTo("/ppt/diagrams/quickStyle1.xml"));
        Assert.That(FindRelationship(relationships, PptxRelationshipTypes.DiagramColors).TargetPartName, Is.EqualTo("/ppt/diagrams/colors1.xml"));
        Assert.That(diagram.ExtractText(), Does.Contain("SmartArt Node Two"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    static PptxPresentation LoadDiagramPresentation()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(PptxPartNames.FirstSlide, PptxContentTypes.Slide, Encoding.UTF8.GetBytes(CreateDiagramSlideXml()));
        package.AddPart("/ppt/diagrams/data1.xml", PptxContentTypes.DiagramData, Encoding.UTF8.GetBytes(CreateDiagramDataXml()));
        package.AddPart("/ppt/diagrams/layout1.xml", PptxContentTypes.DiagramLayout, Encoding.UTF8.GetBytes(CreateDiagramLayoutXml()));
        package.AddPart("/ppt/diagrams/quickStyle1.xml", PptxContentTypes.DiagramStyle, Encoding.UTF8.GetBytes(CreateDiagramStyleXml()));
        package.AddPart("/ppt/diagrams/colors1.xml", PptxContentTypes.DiagramColors, Encoding.UTF8.GetBytes(CreateDiagramColorsXml()));
        package.AddRelationshipWithId(PptxPartNames.FirstSlide, "rIdData", PptxRelationshipTypes.DiagramData, "../diagrams/data1.xml", PptxRelationshipTargetMode.Internal);
        package.AddRelationshipWithId(PptxPartNames.FirstSlide, "rIdLayout", PptxRelationshipTypes.DiagramLayout, "../diagrams/layout1.xml", PptxRelationshipTargetMode.Internal);
        package.AddRelationshipWithId(PptxPartNames.FirstSlide, "rIdQuick", PptxRelationshipTypes.DiagramQuickStyle, "../diagrams/quickStyle1.xml", PptxRelationshipTargetMode.Internal);
        package.AddRelationshipWithId(PptxPartNames.FirstSlide, "rIdColors", PptxRelationshipTypes.DiagramColors, "../diagrams/colors1.xml", PptxRelationshipTargetMode.Internal);
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

    static string CreateDiagramSlideXml()
    {
        return
            "<p:sld xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:dgm=\"" + PptxNamespaces.Diagram + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:cSld>" +
            "<p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>" +
            "<p:graphicFrame>" +
            "<p:nvGraphicFramePr><p:cNvPr id=\"2\" name=\"SmartArt\"/><p:cNvGraphicFramePr/><p:nvPr/></p:nvGraphicFramePr>" +
            "<p:xfrm><a:off x=\"10\" y=\"20\"/><a:ext cx=\"400\" cy=\"200\"/></p:xfrm>" +
            "<a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/diagram\"><dgm:relIds r:dm=\"rIdData\" r:lo=\"rIdLayout\" r:qs=\"rIdQuick\" r:cs=\"rIdColors\"/></a:graphicData></a:graphic>" +
            "</p:graphicFrame>" +
            "</p:spTree>" +
            "</p:cSld>" +
            "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>" +
            "</p:sld>";
    }

    static string CreateDiagramDataXml()
    {
        return
            "<dgm:dataModel xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:dgm=\"" + PptxNamespaces.Diagram + "\">" +
            "<dgm:ptLst>" +
            "<dgm:pt modelId=\"1\"><dgm:t><a:p><a:r><a:t>SmartArt Node One</a:t></a:r></a:p></dgm:t></dgm:pt>" +
            "<dgm:pt modelId=\"2\"><dgm:t>SmartArt Node Two</dgm:t></dgm:pt>" +
            "</dgm:ptLst>" +
            "<dgm:extLst><dgm:ext uri=\"preserve\"><dgm:unknown>Preserved Diagram Extension</dgm:unknown></dgm:ext></dgm:extLst>" +
            "</dgm:dataModel>";
    }

    static string CreateDiagramLayoutXml()
    {
        return "<dgm:layoutDef xmlns:dgm=\"" + PptxNamespaces.Diagram + "\"><dgm:title val=\"Preserve Layout\"/></dgm:layoutDef>";
    }

    static string CreateDiagramStyleXml()
    {
        return "<dgm:styleDef xmlns:dgm=\"" + PptxNamespaces.Diagram + "\"><dgm:title val=\"Preserve Style\"/></dgm:styleDef>";
    }

    static string CreateDiagramColorsXml()
    {
        return "<dgm:colorsDef xmlns:dgm=\"" + PptxNamespaces.Diagram + "\"><dgm:title val=\"Preserve Colors\"/></dgm:colorsDef>";
    }

    static PptxRelationship FindRelationship(PptxRelationshipCollection relationships, string type)
    {
        for (int index = 0; index < relationships.Relationships.Count; index++)
        {
            if (relationships.Relationships[index].Type == type)
            {
                return relationships.Relationships[index];
            }
        }

        throw new System.ArgumentException("Relationship was not found.");
    }
}
