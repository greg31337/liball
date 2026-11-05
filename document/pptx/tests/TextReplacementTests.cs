using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class TextReplacementTests
{
    [Test]
    public void Shape_ReplaceTextAcrossRunsPreservesRunFormatting()
    {
        PptxPresentation presentation = LoadRunReplacementPresentation();
        PptxShape shape = presentation.GetSlide(0).GetShapeTree().Get(0);

        int count = shape.ReplaceText("Hello World", "Greeting");
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxShape reopenedShape = reopened.GetSlide(0).GetShapeTree().Get(0);
        PptxParagraph paragraph = reopenedShape.GetTextFrame().GetParagraph(0);

        Assert.That(count, Is.EqualTo(1));
        Assert.That(reopenedShape.GetText(), Is.EqualTo("Greeting"));
        Assert.That(paragraph.GetRun(0).Text, Is.EqualTo("Greeting"));
        Assert.That(paragraph.GetRun(0).Font.Bold, Is.True);
        Assert.That(paragraph.GetRun(1).Text, Is.Empty);
        Assert.That(paragraph.GetRun(1).Font.Italic, Is.True);
        Assert.That(reopenedShape.Element.OuterXml, Does.Contain("b=\"1\""));
        Assert.That(reopenedShape.Element.OuterXml, Does.Contain("i=\"1\""));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Presentation_ReplaceTextUpdatesSlidesNotesCommentsTablesAndChartText()
    {
        PptxPresentation presentation = LoadPackageReplacementPresentation();

        presentation.SetSlideNotesText(0, "Alpha Notes");
        int count = presentation.ReplaceText("Alpha", "Beta");
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        string extracted = reopened.ExtractText();

        Assert.That(count, Is.EqualTo(6));
        Assert.That(extracted, Does.Contain("Beta Slide"));
        Assert.That(extracted, Does.Contain("Beta Table"));
        Assert.That(extracted, Does.Contain("Beta Notes"));
        Assert.That(extracted, Does.Contain("Beta Comment"));
        Assert.That(extracted, Does.Contain("Beta Chart"));
        Assert.That(extracted, Does.Contain("Beta Series"));
        Assert.That(extracted, Does.Not.Contain("Alpha"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    static PptxPresentation LoadRunReplacementPresentation()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(PptxPartNames.FirstSlide, PptxContentTypes.Slide, Encoding.UTF8.GetBytes(CreateRunReplacementSlideXml()));
        PptxDocument document = PptxDocument.OpenBytes(package.SaveBytes());
        return document.LoadPresentation();
    }

    static PptxPresentation LoadPackageReplacementPresentation()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(PptxPartNames.FirstSlide, PptxContentTypes.Slide, Encoding.UTF8.GetBytes(CreatePackageReplacementSlideXml()));
        package.AddPart("/ppt/comments/comment1.xml", PptxContentTypes.Comments, Encoding.UTF8.GetBytes(CreateCommentsXml()));
        package.AddPart("/ppt/charts/chart1.xml", PptxContentTypes.Chart, Encoding.UTF8.GetBytes(CreateChartXml()));
        package.AddRelationshipWithId(PptxPartNames.FirstSlide, "rIdComments", PptxRelationshipTypes.Comments, "../comments/comment1.xml", PptxRelationshipTargetMode.Internal);
        package.AddRelationshipWithId(PptxPartNames.FirstSlide, "rIdChart", PptxRelationshipTypes.Chart, "../charts/chart1.xml", PptxRelationshipTargetMode.Internal);
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

    static string CreateRunReplacementSlideXml()
    {
        return
            "<p:sld xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:cSld>" +
            "<p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>" +
            "<p:sp>" +
            "<p:nvSpPr><p:cNvPr id=\"2\" name=\"Formatted\"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>" +
            "<p:spPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"100\" cy=\"100\"/></a:xfrm><a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom></p:spPr>" +
            "<p:txBody><a:bodyPr/><a:lstStyle/><a:p>" +
            "<a:r><a:rPr b=\"1\"/><a:t>Hello </a:t></a:r>" +
            "<a:r><a:rPr i=\"1\"/><a:t>World</a:t></a:r>" +
            "</a:p></p:txBody>" +
            "</p:sp>" +
            "</p:spTree>" +
            "</p:cSld>" +
            "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>" +
            "</p:sld>";
    }

    static string CreatePackageReplacementSlideXml()
    {
        return
            "<p:sld xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:c=\"" + PptxNamespaces.Chart + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:cSld>" +
            "<p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>" +
            CreateShapeXml("2", "Slide", "Alpha Slide") +
            CreateTableFrameXml() +
            CreateChartFrameXml() +
            "</p:spTree>" +
            "</p:cSld>" +
            "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>" +
            "</p:sld>";
    }

    static string CreateShapeXml(string id, string name, string text)
    {
        return
            "<p:sp>" +
            "<p:nvSpPr><p:cNvPr id=\"" + id + "\" name=\"" + name + "\"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>" +
            "<p:spPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"100\" cy=\"100\"/></a:xfrm><a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom></p:spPr>" +
            "<p:txBody><a:bodyPr/><a:lstStyle/><a:p><a:r><a:t>" + text + "</a:t></a:r></a:p></p:txBody>" +
            "</p:sp>";
    }

    static string CreateTableFrameXml()
    {
        return
            "<p:graphicFrame>" +
            "<p:nvGraphicFramePr><p:cNvPr id=\"3\" name=\"Table\"/><p:cNvGraphicFramePr/><p:nvPr/></p:nvGraphicFramePr>" +
            "<p:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"400\" cy=\"200\"/></p:xfrm>" +
            "<a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/table\">" +
            "<a:tbl><a:tblPr/><a:tblGrid><a:gridCol w=\"200\"/></a:tblGrid>" +
            "<a:tr h=\"100\"><a:tc><a:txBody><a:bodyPr/><a:lstStyle/><a:p><a:r><a:t>Alpha Table</a:t></a:r></a:p></a:txBody><a:tcPr/></a:tc></a:tr>" +
            "</a:tbl>" +
            "</a:graphicData></a:graphic>" +
            "</p:graphicFrame>";
    }

    static string CreateChartFrameXml()
    {
        return
            "<p:graphicFrame>" +
            "<p:nvGraphicFramePr><p:cNvPr id=\"4\" name=\"Chart\"/><p:cNvGraphicFramePr/><p:nvPr/></p:nvGraphicFramePr>" +
            "<p:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"400\" cy=\"200\"/></p:xfrm>" +
            "<a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/chart\"><c:chart r:id=\"rIdChart\"/></a:graphicData></a:graphic>" +
            "</p:graphicFrame>";
    }

    static string CreateCommentsXml()
    {
        return
            "<p:cmLst xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:cm authorId=\"0\" dt=\"2026-01-01T00:00:00Z\" idx=\"0\"><p:text>Alpha Comment</p:text></p:cm>" +
            "</p:cmLst>";
    }

    static string CreateChartXml()
    {
        return
            "<c:chartSpace xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:c=\"" + PptxNamespaces.Chart + "\">" +
            "<c:chart>" +
            "<c:title><c:tx><c:rich><a:bodyPr/><a:lstStyle/><a:p><a:r><a:t>Alpha Chart</a:t></a:r></a:p></c:rich></c:tx></c:title>" +
            "<c:plotArea><c:barChart><c:ser><c:tx><c:strRef><c:strCache><c:pt idx=\"0\"><c:v>Alpha Series</c:v></c:pt></c:strCache></c:strRef></c:tx></c:ser></c:barChart></c:plotArea>" +
            "</c:chart>" +
            "</c:chartSpace>";
    }
}
