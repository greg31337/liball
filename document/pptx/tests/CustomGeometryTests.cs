using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class CustomGeometryTests
{
    [Test]
    public void Shape_LoadsAndPreservesCustomGeometry()
    {
        PptxPresentation presentation = LoadCustomGeometryPresentation();
        PptxShape shape = presentation.GetSlide(0).GetShapeTree().Get(0);

        Assert.That(shape.IsCustomGeometry(), Is.True);
        Assert.That(shape.IsFreeform(), Is.True);
        Assert.That(shape.IsAutoShape(), Is.False);
        Assert.That(shape.PresetGeometry, Is.Empty);
        Assert.That(shape.GetCustomGeometryXml(), Does.Contain("custGeom"));
        Assert.That(shape.GetCustomGeometryXml(), Does.Contain("pathLst"));
        Assert.That(shape.GetCustomGeometryXml(), Does.Contain("customGuide"));

        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxShape reopenedShape = reopened.GetSlide(0).GetShapeTree().Get(0);

        Assert.That(reopenedShape.IsCustomGeometry(), Is.True);
        Assert.That(reopenedShape.GetCustomGeometryXml(), Does.Contain("customGuide"));
        Assert.That(reopenedShape.GetBounds().Width, Is.EqualTo(300L));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void AddFreeform_CreatesClosedCustomGeometryPath()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxSlide slide = presentation.GetSlide(0);
        List<PptxPoint> points = new List<PptxPoint>();
        points.Add(new PptxPoint(0L, 0L));
        points.Add(new PptxPoint(200L, 0L));
        points.Add(new PptxPoint(100L, 100L));

        PptxShape shape = slide.AddFreeform(new PptxBounds(10L, 20L, 200L, 100L), points);
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxShape reopenedShape = reopened.GetSlide(0).GetShapeTree().Get(0);

        Assert.That(shape.IsCustomGeometry(), Is.True);
        Assert.That(reopenedShape.Name, Is.EqualTo("Freeform 2"));
        Assert.That(reopenedShape.IsCustomGeometry(), Is.True);
        Assert.That(reopenedShape.PresetGeometry, Is.Empty);
        Assert.That(reopenedShape.GetCustomGeometryXml(), Does.Contain("moveTo"));
        Assert.That(reopenedShape.GetCustomGeometryXml(), Does.Contain("lnTo"));
        Assert.That(reopenedShape.GetCustomGeometryXml(), Does.Contain("close"));
        Assert.That(reopenedShape.GetCustomGeometryXml(), Does.Contain("w=\"200\""));
        Assert.That(reopenedShape.GetCustomGeometryXml(), Does.Contain("h=\"100\""));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Shape_SetCustomGeometryXmlAndPresetGeometrySwapGeometryKinds()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxShape shape = presentation.GetSlide(0).AddAutoShape(PptxShapeType.Rectangle, new PptxBounds(10L, 20L, 200L, 100L));

        shape.SetCustomGeometryXml(CreateCustomGeometryXml());

        Assert.That(shape.IsCustomGeometry(), Is.True);
        Assert.That(shape.IsAutoShape(), Is.False);
        Assert.That(shape.PresetGeometry, Is.Empty);
        Assert.That(shape.GetCustomGeometryXml(), Does.Contain("customGuide"));

        shape.SetPresetGeometry(PptxShapeType.Heart);
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxShape reopenedShape = reopened.GetSlide(0).GetShapeTree().Get(0);

        Assert.That(reopenedShape.IsCustomGeometry(), Is.False);
        Assert.That(reopenedShape.GetCustomGeometryXml(), Is.Empty);
        Assert.That(reopenedShape.PresetGeometry, Is.EqualTo(PptxShapeType.Heart));
    }

    [Test]
    public void AddFreeform_RequiresAtLeastTwoPoints()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        List<PptxPoint> points = new List<PptxPoint>();
        points.Add(new PptxPoint(0L, 0L));

        Assert.Throws<System.ArgumentException>(() => presentation.GetSlide(0).AddFreeform(new PptxBounds(0L, 0L, 100L, 100L), points));
    }

    static PptxPresentation LoadGeneratedPresentation()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        byte[] bytes = document.SaveBytes();
        PptxDocument reopened = PptxDocument.OpenBytes(bytes);
        return reopened.LoadPresentation();
    }

    static PptxPresentation LoadCustomGeometryPresentation()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(PptxPartNames.FirstSlide, PptxContentTypes.Slide, Encoding.UTF8.GetBytes(CreateCustomGeometrySlideXml()));
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

    static string CreateCustomGeometrySlideXml()
    {
        return
            "<p:sld xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:cSld>" +
            "<p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>" +
            "<p:sp>" +
            "<p:nvSpPr><p:cNvPr id=\"2\" name=\"Custom Geometry\"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>" +
            "<p:spPr>" +
            "<a:xfrm><a:off x=\"10\" y=\"20\"/><a:ext cx=\"300\" cy=\"200\"/></a:xfrm>" +
            CreateCustomGeometryXml() +
            "</p:spPr>" +
            "<p:txBody><a:bodyPr/><a:lstStyle/><a:p/></p:txBody>" +
            "</p:sp>" +
            "</p:spTree>" +
            "</p:cSld>" +
            "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>" +
            "</p:sld>";
    }

    static string CreateCustomGeometryXml()
    {
        return
            "<a:custGeom xmlns:a=\"" + PptxNamespaces.Drawing + "\">" +
            "<a:avLst/>" +
            "<a:gdLst><a:gd name=\"customGuide\" fmla=\"val 50000\"/></a:gdLst>" +
            "<a:ahLst/>" +
            "<a:cxnLst/>" +
            "<a:rect l=\"l\" t=\"t\" r=\"r\" b=\"b\"/>" +
            "<a:pathLst>" +
            "<a:path w=\"300\" h=\"200\">" +
            "<a:moveTo><a:pt x=\"0\" y=\"0\"/></a:moveTo>" +
            "<a:lnTo><a:pt x=\"300\" y=\"0\"/></a:lnTo>" +
            "<a:lnTo><a:pt x=\"150\" y=\"200\"/></a:lnTo>" +
            "<a:close/>" +
            "</a:path>" +
            "</a:pathLst>" +
            "</a:custGeom>";
    }
}
