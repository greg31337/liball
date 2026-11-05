using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class TextExtractionTests
{
    [Test]
    public void Presentation_ExtractTextIncludesSlidesNotesCommentsGroupsTablesAndChartText()
    {
        PptxPresentation presentation = LoadExtractionPresentation();

        presentation.SetSlideNotesText(0, "Notes Text");
        PptxPresentation reopened = SaveReopenAndLoad(presentation);

        Assert.That(reopened.GetSlide(0).ExtractText(), Is.EqualTo("Slide Shape\nGroup Shape\nTable Cell"));
        Assert.That(reopened.ExtractSlideText(0), Is.EqualTo("Slide Shape\nGroup Shape\nTable Cell"));
        Assert.That(reopened.ExtractNotesText(0), Is.EqualTo("Notes Text"));
        Assert.That(reopened.ExtractCommentsText(0), Is.EqualTo("Comment Text"));
        Assert.That(reopened.ExtractChartsText(0), Does.Contain("Chart Title"));
        Assert.That(reopened.ExtractChartsText(0), Does.Contain("Series Name"));
        Assert.That(reopened.ExtractChartsText(0), Does.Contain("Category One"));
        Assert.That(reopened.ExtractText(), Does.Contain("Slide Shape"));
        Assert.That(reopened.ExtractText(), Does.Contain("Group Shape"));
        Assert.That(reopened.ExtractText(), Does.Contain("Table Cell"));
        Assert.That(reopened.ExtractText(), Does.Contain("Notes Text"));
        Assert.That(reopened.ExtractText(), Does.Contain("Comment Text"));
        Assert.That(reopened.ExtractText(), Does.Contain("Chart Title"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    static PptxPresentation LoadExtractionPresentation()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(PptxPartNames.FirstSlide, PptxContentTypes.Slide, Encoding.UTF8.GetBytes(CreateExtractionSlideXml()));
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

    static string CreateExtractionSlideXml()
    {
        return
            "<p:sld xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:c=\"" + PptxNamespaces.Chart + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:cSld>" +
            "<p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>" +
            CreateShapeXml("2", "Slide Shape", "Slide Shape") +
            CreateGroupXml() +
            CreateTableFrameXml() +
            CreateChartFrameXml() +
            "</p:spTree>" +
            "</p:cSld>" +
            "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>" +
            "</p:sld>";
    }

    static string CreateGroupXml()
    {
        return
            "<p:grpSp>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"3\" name=\"Group\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"100\" cy=\"100\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"100\" cy=\"100\"/></a:xfrm></p:grpSpPr>" +
            CreateShapeXml("4", "Grouped Shape", "Group Shape") +
            "</p:grpSp>";
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
            "<p:nvGraphicFramePr><p:cNvPr id=\"5\" name=\"Table\"/><p:cNvGraphicFramePr/><p:nvPr/></p:nvGraphicFramePr>" +
            "<p:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"400\" cy=\"200\"/></p:xfrm>" +
            "<a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/table\">" +
            "<a:tbl><a:tblPr/><a:tblGrid><a:gridCol w=\"200\"/></a:tblGrid>" +
            "<a:tr h=\"100\"><a:tc><a:txBody><a:bodyPr/><a:lstStyle/><a:p><a:r><a:t>Table Cell</a:t></a:r></a:p></a:txBody><a:tcPr/></a:tc></a:tr>" +
            "</a:tbl>" +
            "</a:graphicData></a:graphic>" +
            "</p:graphicFrame>";
    }

    static string CreateChartFrameXml()
    {
        return
            "<p:graphicFrame>" +
            "<p:nvGraphicFramePr><p:cNvPr id=\"6\" name=\"Chart\"/><p:cNvGraphicFramePr/><p:nvPr/></p:nvGraphicFramePr>" +
            "<p:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"400\" cy=\"200\"/></p:xfrm>" +
            "<a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/chart\"><c:chart r:id=\"rIdChart\"/></a:graphicData></a:graphic>" +
            "</p:graphicFrame>";
    }

    static string CreateCommentsXml()
    {
        return
            "<p:cmLst xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:cm authorId=\"0\" dt=\"2026-01-01T00:00:00Z\" idx=\"0\"><p:text>Comment Text</p:text></p:cm>" +
            "</p:cmLst>";
    }

    static string CreateChartXml()
    {
        return
            "<c:chartSpace xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:c=\"" + PptxNamespaces.Chart + "\">" +
            "<c:chart>" +
            "<c:title><c:tx><c:rich><a:bodyPr/><a:lstStyle/><a:p><a:r><a:t>Chart Title</a:t></a:r></a:p></c:rich></c:tx></c:title>" +
            "<c:plotArea><c:barChart><c:ser>" +
            "<c:tx><c:strRef><c:strCache><c:pt idx=\"0\"><c:v>Series Name</c:v></c:pt></c:strCache></c:strRef></c:tx>" +
            "<c:cat><c:strRef><c:strCache><c:pt idx=\"0\"><c:v>Category One</c:v></c:pt></c:strCache></c:strRef></c:cat>" +
            "</c:ser></c:barChart></c:plotArea>" +
            "</c:chart>" +
            "</c:chartSpace>";
    }
}
