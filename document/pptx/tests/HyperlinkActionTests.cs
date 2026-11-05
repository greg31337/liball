using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class HyperlinkActionTests
{
    [Test]
    public void Shape_LoadsExternalFileAndActionHyperlinks()
    {
        PptxPresentation presentation = LoadHyperlinkPresentation();
        List<PptxShape> shapes = presentation.GetSlide(0).GetShapeTree().GetAllShapes();
        PptxHyperlink url = shapes[0].GetHyperlink();
        PptxHyperlink file = shapes[1].GetHyperlink();
        PptxHyperlink next = shapes[2].GetHyperlink();
        PptxHyperlink customShow = shapes[3].GetHyperlink();

        Assert.That(url.Type, Is.EqualTo("url"));
        Assert.That(url.Target, Is.EqualTo("https://example.com"));
        Assert.That(url.RelationshipType, Is.EqualTo(PptxRelationshipTypes.Hyperlink));
        Assert.That(file.Type, Is.EqualTo("file"));
        Assert.That(file.Target, Is.EqualTo("file:///tmp/report.pdf"));
        Assert.That(next.Type, Is.EqualTo("next-slide"));
        Assert.That(next.Action, Is.EqualTo("ppaction://hlinkshowjump?jump=nextslide"));
        Assert.That(customShow.Type, Is.EqualTo("custom-show"));
        Assert.That(customShow.CustomShowName, Is.EqualTo("Demo Show"));
    }

    [Test]
    public void Shape_WritesUrlFileSlideAndActionLinks()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxSlide firstSlide = presentation.GetSlide(0);
        PptxSlide secondSlide = presentation.AddSlide();
        PptxShape urlShape = firstSlide.AddShape(PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 100L, 100L));
        PptxShape fileShape = firstSlide.AddShape(PptxShapeType.Rectangle, new PptxBounds(110L, 0L, 100L, 100L));
        PptxShape slideShape = firstSlide.AddShape(PptxShapeType.Rectangle, new PptxBounds(220L, 0L, 100L, 100L));
        PptxShape actionShape = firstSlide.AddShape(PptxShapeType.Rectangle, new PptxBounds(330L, 0L, 100L, 100L));

        urlShape.SetHyperlink("https://example.com/one");
        fileShape.SetFileHyperlink("file:///tmp/report.pdf");
        slideShape.SetSlideHyperlink(1);
        actionShape.SetLastSlideAction();
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        List<PptxShape> shapes = reopened.GetSlide(0).GetShapeTree().GetAllShapes();
        PptxHyperlink reopenedUrl = shapes[0].GetHyperlink();
        PptxHyperlink reopenedFile = shapes[1].GetHyperlink();
        PptxHyperlink reopenedSlide = shapes[2].GetHyperlink();
        PptxHyperlink reopenedAction = shapes[3].GetHyperlink();

        Assert.That(secondSlide.SlideId, Is.GreaterThan(0));
        Assert.That(reopenedUrl.Type, Is.EqualTo("url"));
        Assert.That(reopenedUrl.Target, Is.EqualTo("https://example.com/one"));
        Assert.That(reopenedFile.Type, Is.EqualTo("file"));
        Assert.That(reopenedFile.Target, Is.EqualTo("file:///tmp/report.pdf"));
        Assert.That(reopenedSlide.Type, Is.EqualTo("slide"));
        Assert.That(reopenedSlide.SlideIndex, Is.EqualTo(1));
        Assert.That(reopenedSlide.Action, Is.EqualTo("ppaction://hlinksldjump"));
        Assert.That(reopenedSlide.TargetPartName, Is.EqualTo(reopened.GetSlide(1).PartName));
        Assert.That(reopenedAction.Type, Is.EqualTo("last-slide"));
        Assert.That(reopenedAction.Action, Is.EqualTo("ppaction://hlinkshowjump?jump=lastslide"));
        Assert.That(CountRelationships(reopened.GetSlide(0).Part.Relationships, PptxRelationshipTypes.Hyperlink), Is.EqualTo(2));
        Assert.That(CountRelationships(reopened.GetSlide(0).Part.Relationships, PptxRelationshipTypes.Slide), Is.EqualTo(1));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Shape_SwitchesLinkKindsAndRemovesStaleRelationships()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        presentation.AddSlide();
        PptxShape shape = presentation.GetSlide(0).AddShape(PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 100L, 100L));

        shape.SetHyperlink("https://example.com/one");
        shape.SetSlideHyperlink(1);
        shape.SetCustomShowAction("Demo Show");
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxShape reopenedShape = reopened.GetSlide(0).GetShapeTree().Get(0);
        PptxHyperlink hyperlink = reopenedShape.GetHyperlink();

        Assert.That(hyperlink.Type, Is.EqualTo("custom-show"));
        Assert.That(hyperlink.CustomShowName, Is.EqualTo("Demo Show"));
        Assert.That(hyperlink.RelationshipId, Is.EqualTo(""));
        Assert.That(CountRelationships(reopened.GetSlide(0).Part.Relationships, PptxRelationshipTypes.Hyperlink), Is.EqualTo(0));
        Assert.That(CountRelationships(reopened.GetSlide(0).Part.Relationships, PptxRelationshipTypes.Slide), Is.EqualTo(0));

        reopenedShape.RemoveHyperlink();
        PptxPresentation removed = SaveReopenAndLoad(reopened);

        Assert.That(removed.GetSlide(0).GetShapeTree().Get(0).GetHyperlink().HasLink(), Is.False);
        Assert.That(removed.Package.Validate().Succeeded(), Is.True, removed.Package.Validate().FormatDiagnostics());
    }

    static PptxPresentation LoadHyperlinkPresentation()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(PptxPartNames.FirstSlide, PptxContentTypes.Slide, Encoding.UTF8.GetBytes(CreateHyperlinkSlideXml()));
        package.AddRelationshipWithId(PptxPartNames.FirstSlide, "rIdUrl", PptxRelationshipTypes.Hyperlink, "https://example.com", PptxRelationshipTargetMode.External);
        package.AddRelationshipWithId(PptxPartNames.FirstSlide, "rIdFile", PptxRelationshipTypes.Hyperlink, "file:///tmp/report.pdf", PptxRelationshipTargetMode.External);
        PptxDocument document = PptxDocument.OpenBytes(package.SaveBytes());
        return document.LoadPresentation();
    }

    static PptxPresentation LoadGeneratedPresentation()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        byte[] bytes = document.SaveBytes();
        PptxDocument reopened = PptxDocument.OpenBytes(bytes);
        return reopened.LoadPresentation();
    }

    static PptxPresentation SaveReopenAndLoad(PptxPresentation presentation)
    {
        PptxDocument document = new PptxDocument(presentation.Package);
        byte[] bytes = document.SaveBytes();
        PptxDocument reopened = PptxDocument.OpenBytes(bytes);
        return reopened.LoadPresentation();
    }

    static string CreateHyperlinkSlideXml()
    {
        return
            "<p:sld xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:cSld>" +
            "<p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>" +
            CreateShapeXml("2", "Url", "<a:hlinkClick r:id=\"rIdUrl\"/>") +
            CreateShapeXml("3", "File", "<a:hlinkClick r:id=\"rIdFile\"/>") +
            CreateShapeXml("4", "Next", "<a:hlinkClick action=\"ppaction://hlinkshowjump?jump=nextslide\"/>") +
            CreateShapeXml("5", "Custom", "<a:hlinkClick action=\"ppaction://customshow?id=Demo Show\"/>") +
            "</p:spTree>" +
            "</p:cSld>" +
            "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>" +
            "</p:sld>";
    }

    static string CreateShapeXml(string id, string name, string hyperlinkXml)
    {
        return
            "<p:sp>" +
            "<p:nvSpPr><p:cNvPr id=\"" + id + "\" name=\"" + name + "\">" + hyperlinkXml + "</p:cNvPr><p:cNvSpPr/><p:nvPr/></p:nvSpPr>" +
            "<p:spPr><a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom></p:spPr>" +
            "</p:sp>";
    }

    static int CountRelationships(PptxRelationshipCollection relationships, string type)
    {
        int count = 0;
        for (int index = 0; index < relationships.Relationships.Count; index++)
        {
            if (relationships.Relationships[index].Type == type)
            {
                count = count + 1;
            }
        }

        return count;
    }
}
