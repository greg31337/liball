using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class SvgExportTests
{
    [Test]
    public void Presentation_ExportSvgWritesStructuralVectorTextAndPlaceholders()
    {
        PptxPresentation presentation = LoadSvgPresentation();

        string svg = presentation.ExportSvg();

        Assert.That(svg, Does.StartWith("<svg xmlns=\"http://www.w3.org/2000/svg\" class=\"pptx-svg-document\""));
        Assert.That(svg, Does.Contain("data-slide-count=\"1\""));
        Assert.That(svg, Does.Contain("<title>PowerPoint structural SVG export</title>"));
        Assert.That(svg, Does.Contain("Structural SVG export supports common vector geometry and text only"));
        Assert.That(svg, Does.Contain("<rect class=\"pptx-svg-shape pptx-svg-rect\""));
        Assert.That(svg, Does.Contain("data-name=\"Title &lt;Box&gt;\""));
        Assert.That(svg, Does.Contain("Hello &lt;SVG&gt; &amp; &quot;Deck&quot;"));
        Assert.That(svg, Does.Not.Contain("Hello <SVG>"));
        Assert.That(svg, Does.Contain("<ellipse class=\"pptx-svg-shape pptx-svg-ellipse\""));
        Assert.That(svg, Does.Contain("<polygon class=\"pptx-svg-shape pptx-svg-diamond\""));
        Assert.That(svg, Does.Contain("<polygon class=\"pptx-svg-shape pptx-svg-triangle\""));
        Assert.That(svg, Does.Contain("<line class=\"pptx-svg-shape pptx-svg-connector\" x1=\"1000\" y1=\"1100\" x2=\"1800\" y2=\"1900\""));
        Assert.That(svg, Does.Contain("class=\"pptx-svg-placeholder pptx-svg-image-placeholder\""));
        Assert.That(svg, Does.Contain("Image placeholder"));
        Assert.That(svg, Does.Contain("class=\"pptx-svg-placeholder pptx-svg-table-placeholder\""));
        Assert.That(svg, Does.Contain("Table placeholder"));
        Assert.That(svg, Does.Contain("class=\"pptx-svg-placeholder pptx-svg-chart-placeholder\""));
        Assert.That(svg, Does.Contain("Chart placeholder"));
    }

    [Test]
    public void Slide_ExportSvgWritesOnlySingleSlideSvg()
    {
        PptxPresentation presentation = LoadSvgPresentation();

        string svg = presentation.GetSlide(0).ExportSvg();

        Assert.That(svg, Does.StartWith("<svg xmlns=\"http://www.w3.org/2000/svg\" class=\"pptx-svg-slide\""));
        Assert.That(svg, Does.Contain("data-slide-index=\"0\""));
        Assert.That(svg, Does.Contain("data-part-name=\"/ppt/slides/slide1.xml\""));
        Assert.That(svg, Does.Contain("<rect class=\"pptx-svg-slide-background\""));
        Assert.That(svg, Does.Not.Contain("class=\"pptx-svg-document\""));
        Assert.That(svg, Does.Not.Contain("data-slide-count=\"1\""));
    }

    static PptxPresentation LoadSvgPresentation()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxSlide slide = presentation.GetSlide(0);
        PptxShape text = slide.AddTextBox("Hello <SVG> & \"Deck\"", new PptxBounds(10L, 20L, 300L, 100L));
        text.SetName("Title <Box>");
        slide.AddAutoShape(PptxShapeType.Ellipse, new PptxBounds(400L, 20L, 200L, 100L));
        slide.AddAutoShape(PptxShapeType.Diamond, new PptxBounds(620L, 20L, 160L, 160L));
        slide.AddAutoShape(PptxShapeType.Triangle, new PptxBounds(800L, 20L, 160L, 160L));
        slide.AddConnector(PptxConnector.Straight, new PptxPoint(1000L, 1100L), new PptxPoint(1800L, 1900L));
        slide.AddImage(CreatePngBytes(), new PptxBounds(10L, 220L, 160L, 90L));
        slide.AddTable(1, 1, new PptxBounds(200L, 220L, 220L, 120L));
        slide.AddChart(PptxChartType.Column, CreateChartData(), new PptxBounds(450L, 220L, 260L, 160L));
        PptxDocument reopened = PptxDocument.OpenBytes(presentation.Package.SaveBytes());
        return reopened.LoadPresentation();
    }

    static PptxPresentation LoadGeneratedPresentation()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        byte[] bytes = document.SaveBytes();
        PptxDocument reopened = PptxDocument.OpenBytes(bytes);
        return reopened.LoadPresentation();
    }

    static PptxChartData CreateChartData()
    {
        PptxChartData data = new PptxChartData();
        data.Categories.Add("Q1");
        PptxChartSeries series = new PptxChartSeries();
        series.Name = "Revenue";
        series.Values.Add("10");
        data.Series.Add(series);
        return data;
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
