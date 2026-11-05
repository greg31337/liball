using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class FillTests
{
    [Test]
    public void Shape_LoadsNoneSolidGradientPicturePatternAndGroupFills()
    {
        PptxPresentation presentation = LoadFillPresentation();
        PptxShapeTree tree = presentation.GetSlide(0).GetShapeTree();

        Assert.That(tree.Count(), Is.EqualTo(6));
        Assert.That(tree.Get(0).GetFill().Type, Is.EqualTo(PptxFill.None));
        Assert.That(tree.Get(1).GetFill().Type, Is.EqualTo(PptxFill.Solid));
        Assert.That(tree.Get(1).GetFill().Color.Value, Is.EqualTo("FF0000"));
        Assert.That(tree.Get(1).GetFill().Color.Alpha, Is.EqualTo(50000));
        Assert.That(tree.Get(2).GetFill().Type, Is.EqualTo(PptxFill.Gradient));
        Assert.That(tree.Get(2).GetFill().Color.Value, Is.EqualTo("000000"));
        Assert.That(tree.Get(2).GetFill().SecondaryColor.Value, Is.EqualTo("FFFFFF"));
        Assert.That(tree.Get(2).GetFill().GradientAngleUnits, Is.EqualTo(5400000));
        Assert.That(tree.Get(3).GetFill().Type, Is.EqualTo(PptxFill.Picture));
        Assert.That(tree.Get(3).GetFill().RelationshipId, Is.EqualTo("rIdImage"));
        Assert.That(tree.Get(4).GetFill().Type, Is.EqualTo(PptxFill.Pattern));
        Assert.That(tree.Get(4).GetFill().PatternType, Is.EqualTo("pct5"));
        Assert.That(tree.Get(4).GetFill().SecondaryColor.Value, Is.EqualTo("FFFF00"));
        Assert.That(tree.Get(5).GetFill().Type, Is.EqualTo(PptxFill.Group));
        Assert.That(tree.Get(3).GetFill().Xml, Does.Contain("blipFill"));
    }

    [Test]
    public void Shape_SetFillWritesCommonFillKindsAndRoundTrips()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxSlide slide = presentation.GetSlide(0);
        PptxShape none = slide.AddAutoShape(PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 100L, 100L));
        PptxShape solid = slide.AddAutoShape(PptxShapeType.Rectangle, new PptxBounds(100L, 0L, 100L, 100L));
        PptxShape gradient = slide.AddAutoShape(PptxShapeType.Rectangle, new PptxBounds(200L, 0L, 100L, 100L));
        PptxShape pattern = slide.AddAutoShape(PptxShapeType.Rectangle, new PptxBounds(300L, 0L, 100L, 100L));
        PptxShape group = slide.AddAutoShape(PptxShapeType.Rectangle, new PptxBounds(400L, 0L, 100L, 100L));

        none.SetFill(PptxFill.CreateNone());
        solid.SetFill(PptxFill.CreateSolidRgb("#00FF00"));
        gradient.SetFill(PptxFill.CreateGradient(PptxColor.CreateRgb("000000"), PptxColor.CreateRgb("FFFFFF"), 90.0));
        pattern.SetFill(PptxFill.CreatePattern("diagCross", PptxColor.CreateRgb("112233"), PptxColor.CreateRgb("445566")));
        group.SetFill(PptxFill.CreateGroup());
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxShapeTree tree = reopened.GetSlide(0).GetShapeTree();

        Assert.That(tree.Get(0).GetFill().Type, Is.EqualTo(PptxFill.None));
        Assert.That(tree.Get(1).GetFill().Color.Value, Is.EqualTo("00FF00"));
        Assert.That(tree.Get(2).GetFill().GradientAngleUnits, Is.EqualTo(5400000));
        Assert.That(tree.Get(3).GetFill().PatternType, Is.EqualTo("diagCross"));
        Assert.That(tree.Get(3).GetFill().Color.Value, Is.EqualTo("112233"));
        Assert.That(tree.Get(3).GetFill().SecondaryColor.Value, Is.EqualTo("445566"));
        Assert.That(tree.Get(4).GetFill().Type, Is.EqualTo(PptxFill.Group));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Shape_SetFillReplacesExistingFillAndCanWritePictureFill()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxShape shape = presentation.GetSlide(0).AddAutoShape(PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 100L, 100L));

        shape.SetFill(PptxFill.CreateSolidRgb("FF0000"));
        shape.SetFill(PptxFill.CreatePicture("rId9"));

        Assert.That(shape.GetFill().Type, Is.EqualTo(PptxFill.Picture));
        Assert.That(shape.GetFill().RelationshipId, Is.EqualTo("rId9"));
        Assert.That(shape.GetFill().Xml, Does.Contain("blipFill"));
        Assert.That(shape.GetFill().Xml, Does.Not.Contain("solidFill"));
    }

    [Test]
    public void Slide_SetBackgroundFillRoundTrips()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        presentation.GetSlide(0).SetBackground(PptxFill.CreateSolidRgb("336699"));

        PptxPresentation reopened = SaveReopenAndLoad(presentation);

        Assert.That(reopened.GetSlide(0).GetBackgroundFill().Type, Is.EqualTo(PptxFill.Solid));
        Assert.That(reopened.GetSlide(0).GetBackgroundFill().Color.Value, Is.EqualTo("336699"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    static PptxPresentation LoadGeneratedPresentation()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        byte[] bytes = document.SaveBytes();
        PptxDocument reopened = PptxDocument.OpenBytes(bytes);
        return reopened.LoadPresentation();
    }

    static PptxPresentation LoadFillPresentation()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(PptxPartNames.FirstSlide, PptxContentTypes.Slide, Encoding.UTF8.GetBytes(CreateFillSlideXml()));
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

    static string CreateFillSlideXml()
    {
        return
            "<p:sld xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:cSld>" +
            "<p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>" +
            CreateShapeXml("2", "No Fill", "<a:noFill/>") +
            CreateShapeXml("3", "Solid Fill", "<a:solidFill><a:srgbClr val=\"FF0000\"><a:alpha val=\"50000\"/></a:srgbClr></a:solidFill>") +
            CreateShapeXml("4", "Gradient Fill", CreateGradientFillXml()) +
            CreateShapeXml("5", "Picture Fill", "<a:blipFill><a:blip r:embed=\"rIdImage\"/><a:stretch><a:fillRect/></a:stretch></a:blipFill>") +
            CreateShapeXml("6", "Pattern Fill", "<a:pattFill prst=\"pct5\"><a:fgClr><a:srgbClr val=\"0000FF\"/></a:fgClr><a:bgClr><a:srgbClr val=\"FFFF00\"/></a:bgClr></a:pattFill>") +
            CreateShapeXml("7", "Group Fill", "<a:grpFill/>") +
            "</p:spTree>" +
            "</p:cSld>" +
            "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>" +
            "</p:sld>";
    }

    static string CreateGradientFillXml()
    {
        return
            "<a:gradFill>" +
            "<a:gsLst>" +
            "<a:gs pos=\"0\"><a:srgbClr val=\"000000\"/></a:gs>" +
            "<a:gs pos=\"100000\"><a:srgbClr val=\"FFFFFF\"/></a:gs>" +
            "</a:gsLst>" +
            "<a:lin ang=\"5400000\" scaled=\"1\"/>" +
            "</a:gradFill>";
    }

    static string CreateShapeXml(string id, string name, string fillXml)
    {
        return
            "<p:sp>" +
            "<p:nvSpPr><p:cNvPr id=\"" + id + "\" name=\"" + name + "\"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>" +
            "<p:spPr>" +
            "<a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"100\" cy=\"100\"/></a:xfrm>" +
            "<a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom>" +
            fillXml +
            "</p:spPr>" +
            "<p:txBody><a:bodyPr/><a:lstStyle/><a:p/></p:txBody>" +
            "</p:sp>";
    }
}
