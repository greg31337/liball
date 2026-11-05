using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class LineTests
{
    [Test]
    public void Shape_LoadsLineColorWidthDashCapCompoundJoinArrowsAndTransparency()
    {
        PptxPresentation presentation = LoadLinePresentation();
        PptxShapeTree tree = presentation.GetSlide(0).GetShapeTree();
        PptxLine detailed = tree.Get(0).GetLine();
        PptxLine none = tree.Get(1).GetLine();

        Assert.That(detailed.Type, Is.EqualTo(PptxLine.Solid));
        Assert.That(detailed.Width, Is.EqualTo(12700L));
        Assert.That(detailed.Cap, Is.EqualTo("round"));
        Assert.That(detailed.Compound, Is.EqualTo("dbl"));
        Assert.That(detailed.Alignment, Is.EqualTo("ctr"));
        Assert.That(detailed.Color.Value, Is.EqualTo("123456"));
        Assert.That(detailed.Transparency, Is.EqualTo(25000));
        Assert.That(detailed.Dash, Is.EqualTo("dash"));
        Assert.That(detailed.Join, Is.EqualTo(PptxLine.JoinRound));
        Assert.That(detailed.HeadArrowType, Is.EqualTo("triangle"));
        Assert.That(detailed.TailArrowType, Is.EqualTo("stealth"));
        Assert.That(detailed.Xml, Does.Contain("headEnd"));
        Assert.That(none.Type, Is.EqualTo(PptxLine.None));
        Assert.That(none.Xml, Does.Contain("noFill"));
    }

    [Test]
    public void Shape_SetLineWritesCommonLineFormattingAndRoundTrips()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxShape shape = presentation.GetSlide(0).AddAutoShape(PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 100L, 100L));
        PptxLine line = PptxLine.CreateSolidRgb("#112233", 25400L);
        line.Cap = "square";
        line.Compound = "dbl";
        line.Alignment = "ctr";
        line.Dash = "sysDot";
        line.Join = PptxLine.JoinBevel;
        line.HeadArrowType = "triangle";
        line.TailArrowType = "stealth";
        line.Transparency = 40000;

        shape.SetLine(line);
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxLine reopenedLine = reopened.GetSlide(0).GetShapeTree().Get(0).GetLine();

        Assert.That(reopenedLine.Type, Is.EqualTo(PptxLine.Solid));
        Assert.That(reopenedLine.Width, Is.EqualTo(25400L));
        Assert.That(reopenedLine.Color.Value, Is.EqualTo("112233"));
        Assert.That(reopenedLine.Color.Alpha, Is.EqualTo(60000));
        Assert.That(reopenedLine.Transparency, Is.EqualTo(40000));
        Assert.That(reopenedLine.Cap, Is.EqualTo("square"));
        Assert.That(reopenedLine.Compound, Is.EqualTo("dbl"));
        Assert.That(reopenedLine.Alignment, Is.EqualTo("ctr"));
        Assert.That(reopenedLine.Dash, Is.EqualTo("sysDot"));
        Assert.That(reopenedLine.Join, Is.EqualTo(PptxLine.JoinBevel));
        Assert.That(reopenedLine.HeadArrowType, Is.EqualTo("triangle"));
        Assert.That(reopenedLine.TailArrowType, Is.EqualTo("stealth"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Shape_SetLineReplacesExistingLineWithNoLine()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxShape shape = presentation.GetSlide(0).AddAutoShape(PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 100L, 100L));

        shape.SetLine(PptxLine.CreateSolidRgb("FF0000", 12700L));
        shape.SetLine(PptxLine.CreateNone());

        Assert.That(shape.GetLine().Type, Is.EqualTo(PptxLine.None));
        Assert.That(shape.GetLine().Xml, Does.Contain("noFill"));
        Assert.That(shape.GetLine().Xml, Does.Not.Contain("solidFill"));
    }

    static PptxPresentation LoadGeneratedPresentation()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        byte[] bytes = document.SaveBytes();
        PptxDocument reopened = PptxDocument.OpenBytes(bytes);
        return reopened.LoadPresentation();
    }

    static PptxPresentation LoadLinePresentation()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(PptxPartNames.FirstSlide, PptxContentTypes.Slide, Encoding.UTF8.GetBytes(CreateLineSlideXml()));
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

    static string CreateLineSlideXml()
    {
        return
            "<p:sld xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:cSld>" +
            "<p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>" +
            CreateShapeXml("2", "Detailed Line", CreateDetailedLineXml()) +
            CreateShapeXml("3", "No Line", "<a:ln><a:noFill/></a:ln>") +
            "</p:spTree>" +
            "</p:cSld>" +
            "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>" +
            "</p:sld>";
    }

    static string CreateDetailedLineXml()
    {
        return
            "<a:ln w=\"12700\" cap=\"round\" cmpd=\"dbl\" algn=\"ctr\">" +
            "<a:solidFill><a:srgbClr val=\"123456\"><a:alpha val=\"75000\"/></a:srgbClr></a:solidFill>" +
            "<a:prstDash val=\"dash\"/>" +
            "<a:round/>" +
            "<a:headEnd type=\"triangle\"/>" +
            "<a:tailEnd type=\"stealth\"/>" +
            "</a:ln>";
    }

    static string CreateShapeXml(string id, string name, string lineXml)
    {
        return
            "<p:sp>" +
            "<p:nvSpPr><p:cNvPr id=\"" + id + "\" name=\"" + name + "\"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>" +
            "<p:spPr>" +
            "<a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"100\" cy=\"100\"/></a:xfrm>" +
            "<a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom>" +
            lineXml +
            "</p:spPr>" +
            "<p:txBody><a:bodyPr/><a:lstStyle/><a:p/></p:txBody>" +
            "</p:sp>";
    }
}
