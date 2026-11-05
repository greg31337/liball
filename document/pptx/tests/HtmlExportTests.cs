using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class HtmlExportTests
{
    [Test]
    public void Presentation_ExportHtmlWritesStructuralSlidesShapesTablesImagesAndCharts()
    {
        PptxPresentation presentation = LoadHtmlPresentation();

        string html = presentation.ExportHtml();

        Assert.That(html, Does.StartWith("<!doctype html>"));
        Assert.That(html, Does.Contain("<main class=\"pptx-document\" data-slide-count=\"1\">"));
        Assert.That(html, Does.Contain("<section class=\"pptx-slide\" data-slide-index=\"0\" data-part-name=\"/ppt/slides/slide1.xml\">"));
        Assert.That(html, Does.Contain("data-name=\"Text &lt;Shape&gt;\""));
        Assert.That(html, Does.Contain("<p>Hello &lt;World&gt; &amp; &quot;Deck&quot;</p>"));
        Assert.That(html, Does.Not.Contain("Hello <World>"));
        Assert.That(html, Does.Contain("class=\"pptx-shape pptx-table-shape\""));
        Assert.That(html, Does.Contain("<td>Cell &lt;A&gt; &amp; B</td>"));
        Assert.That(html, Does.Contain("class=\"pptx-shape pptx-image-placeholder\""));
        Assert.That(html, Does.Contain("data-relationship-id=\"rIdImage\""));
        Assert.That(html, Does.Contain("data-part-name=\"/ppt/media/image1.png\""));
        Assert.That(html, Does.Contain("class=\"pptx-shape pptx-chart-placeholder\""));
        Assert.That(html, Does.Contain("data-part-name=\"/ppt/charts/chart1.xml\""));
        Assert.That(html, Does.Contain("class=\"pptx-shape pptx-shape-placeholder\""));
        Assert.That(html, Does.Contain("data-preset-geometry=\"rect\""));
    }

    [Test]
    public void Slide_ExportHtmlWritesOnlyTheSlideSection()
    {
        PptxPresentation presentation = LoadHtmlPresentation();

        string html = presentation.GetSlide(0).ExportHtml();

        Assert.That(html, Does.StartWith("<section class=\"pptx-slide\""));
        Assert.That(html, Does.Contain("<h2>Slide 1</h2>"));
        Assert.That(html, Does.Contain("Image placeholder"));
        Assert.That(html, Does.Not.Contain("<!doctype html>"));
        Assert.That(html, Does.Not.Contain("<main class=\"pptx-document\""));
    }

    static PptxPresentation LoadHtmlPresentation()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(PptxPartNames.FirstSlide, PptxContentTypes.Slide, Encoding.UTF8.GetBytes(CreateHtmlSlideXml()));
        package.AddPart("/ppt/media/image1.png", PptxContentTypes.Png, CreatePngBytes());
        package.AddPart("/ppt/charts/chart1.xml", PptxContentTypes.Chart, Encoding.UTF8.GetBytes(CreateChartXml()));
        package.AddRelationshipWithId(PptxPartNames.FirstSlide, "rIdImage", PptxRelationshipTypes.Image, "../media/image1.png", PptxRelationshipTargetMode.Internal);
        package.AddRelationshipWithId(PptxPartNames.FirstSlide, "rIdChart", PptxRelationshipTypes.Chart, "../charts/chart1.xml", PptxRelationshipTargetMode.Internal);
        PptxDocument document = PptxDocument.OpenBytes(package.SaveBytes());
        return document.LoadPresentation();
    }

    static string CreateHtmlSlideXml()
    {
        return
            "<p:sld xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:c=\"" + PptxNamespaces.Chart + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:cSld>" +
            "<p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>" +
            CreateTextShapeXml() +
            CreateImageShapeXml() +
            CreateTableShapeXml() +
            CreateChartShapeXml() +
            CreatePlainShapeXml() +
            "</p:spTree>" +
            "</p:cSld>" +
            "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>" +
            "</p:sld>";
    }

    static string CreateTextShapeXml()
    {
        return
            "<p:sp>" +
            "<p:nvSpPr><p:cNvPr id=\"2\" name=\"Text &lt;Shape&gt;\"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>" +
            "<p:spPr><a:xfrm><a:off x=\"10\" y=\"20\"/><a:ext cx=\"300\" cy=\"100\"/></a:xfrm><a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom></p:spPr>" +
            "<p:txBody><a:bodyPr/><a:lstStyle/><a:p><a:r><a:t>Hello &lt;World&gt; &amp; &quot;Deck&quot;</a:t></a:r></a:p></p:txBody>" +
            "</p:sp>";
    }

    static string CreateImageShapeXml()
    {
        return
            "<p:pic>" +
            "<p:nvPicPr><p:cNvPr id=\"3\" name=\"Image\"/><p:cNvPicPr/><p:nvPr/></p:nvPicPr>" +
            "<p:blipFill><a:blip r:embed=\"rIdImage\"/><a:stretch><a:fillRect/></a:stretch></p:blipFill>" +
            "<p:spPr><a:xfrm><a:off x=\"320\" y=\"20\"/><a:ext cx=\"160\" cy=\"90\"/></a:xfrm><a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom></p:spPr>" +
            "</p:pic>";
    }

    static string CreateTableShapeXml()
    {
        return
            "<p:graphicFrame>" +
            "<p:nvGraphicFramePr><p:cNvPr id=\"4\" name=\"Table\"/><p:cNvGraphicFramePr/><p:nvPr/></p:nvGraphicFramePr>" +
            "<p:xfrm><a:off x=\"10\" y=\"140\"/><a:ext cx=\"400\" cy=\"120\"/></p:xfrm>" +
            "<a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/table\">" +
            "<a:tbl><a:tblPr/><a:tblGrid><a:gridCol w=\"200\"/></a:tblGrid>" +
            "<a:tr h=\"100\"><a:tc><a:txBody><a:bodyPr/><a:lstStyle/><a:p><a:r><a:t>Cell &lt;A&gt; &amp; B</a:t></a:r></a:p></a:txBody><a:tcPr/></a:tc></a:tr>" +
            "</a:tbl>" +
            "</a:graphicData></a:graphic>" +
            "</p:graphicFrame>";
    }

    static string CreateChartShapeXml()
    {
        return
            "<p:graphicFrame>" +
            "<p:nvGraphicFramePr><p:cNvPr id=\"5\" name=\"Chart\"/><p:cNvGraphicFramePr/><p:nvPr/></p:nvGraphicFramePr>" +
            "<p:xfrm><a:off x=\"420\" y=\"140\"/><a:ext cx=\"400\" cy=\"200\"/></p:xfrm>" +
            "<a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/chart\"><c:chart r:id=\"rIdChart\"/></a:graphicData></a:graphic>" +
            "</p:graphicFrame>";
    }

    static string CreatePlainShapeXml()
    {
        return
            "<p:sp>" +
            "<p:nvSpPr><p:cNvPr id=\"6\" name=\"Plain Shape\"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>" +
            "<p:spPr><a:xfrm><a:off x=\"20\" y=\"300\"/><a:ext cx=\"100\" cy=\"100\"/></a:xfrm><a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom></p:spPr>" +
            "</p:sp>";
    }

    static string CreateChartXml()
    {
        return
            "<c:chartSpace xmlns:c=\"" + PptxNamespaces.Chart + "\">" +
            "<c:chart><c:plotArea/></c:chart>" +
            "</c:chartSpace>";
    }

    static byte[] CreatePngBytes()
    {
        byte[] bytes = new byte[24];
        bytes[0] = 137;
        bytes[1] = 80;
        bytes[2] = 78;
        bytes[3] = 71;
        bytes[4] = 13;
        bytes[5] = 10;
        bytes[6] = 26;
        bytes[7] = 10;
        bytes[12] = 73;
        bytes[13] = 72;
        bytes[14] = 68;
        bytes[15] = 82;
        return bytes;
    }
}
