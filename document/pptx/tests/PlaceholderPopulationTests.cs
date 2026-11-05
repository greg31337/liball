using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PlaceholderPopulationTests
{
    [Test]
    public void SlidePlaceholders_LoadAndFindRequestedPlaceholderTypes()
    {
        PptxPresentation presentation = LoadPresentationWithMixedLayout();

        PptxSlide slide = presentation.AddSlideWithLayout("Mixed");

        Assert.That(slide.PlaceholderCount(), Is.EqualTo(7));
        Assert.That(slide.FindPlaceholder("title"), Is.Not.Null);
        Assert.That(slide.FindPlaceholderByIndex("body", 2), Is.Not.Null);
        Assert.That(slide.FindPlaceholder("picture")!.Type, Is.EqualTo("pic"));
        Assert.That(slide.FindPlaceholder("table")!.Type, Is.EqualTo("tbl"));
        Assert.That(slide.FindPlaceholder("chart")!.Type, Is.EqualTo("chart"));
        Assert.That(slide.FindPlaceholder("media")!.Type, Is.EqualTo("media"));
        Assert.That(slide.FindPlaceholder("content")!.Type, Is.EqualTo("obj"));
        Assert.That(slide.HasPlaceholder("content"), Is.True);
        Assert.That(slide.FindPlaceholder("unknown"), Is.Null);
    }

    [Test]
    public void PopulatePlaceholderText_WritesAllSupportedPlaceholderTypesAndRoundTrips()
    {
        PptxPresentation presentation = LoadPresentationWithMixedLayout();
        PptxSlide slide = presentation.AddSlideWithLayout("Mixed");

        slide.PopulateTitle("Quarterly Review");
        slide.PopulateBody("Body Copy");
        slide.PopulatePicture("Picture Placeholder");
        slide.PopulateTable("Table Placeholder");
        slide.PopulateChart("Chart Placeholder");
        slide.PopulateMedia("Media Placeholder");
        slide.PopulateContent("Content Placeholder");
        PptxPresentation reopened = SaveReopenAndLoad(presentation);

        PptxSlide reopenedSlide = reopened.GetSlide(1);
        Assert.That(reopenedSlide.GetPlaceholderText("title"), Is.EqualTo("Quarterly Review"));
        Assert.That(reopenedSlide.GetPlaceholderText("body"), Is.EqualTo("Body Copy"));
        Assert.That(reopenedSlide.GetPlaceholderText("picture"), Is.EqualTo("Picture Placeholder"));
        Assert.That(reopenedSlide.GetPlaceholderText("table"), Is.EqualTo("Table Placeholder"));
        Assert.That(reopenedSlide.GetPlaceholderText("chart"), Is.EqualTo("Chart Placeholder"));
        Assert.That(reopenedSlide.GetPlaceholderText("media"), Is.EqualTo("Media Placeholder"));
        Assert.That(reopenedSlide.GetPlaceholderText("content"), Is.EqualTo("Content Placeholder"));
        Assert.That(reopenedSlide.FindPlaceholder("content")!.Text, Is.EqualTo("Content Placeholder"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void PopulatePlaceholder_WithIndexTargetsMatchingPlaceholder()
    {
        PptxPresentation presentation = LoadPresentationWithMixedLayout();
        PptxSlide slide = presentation.AddSlideWithLayout("Mixed");

        slide.PopulatePlaceholderByIndex("body", 2, "Indexed Body");

        Assert.That(slide.GetPlaceholderTextByIndex("body", 2), Is.EqualTo("Indexed Body"));
    }

    [Test]
    public void PopulatePlaceholder_ThrowsWhenPlaceholderIsMissing()
    {
        PptxPresentation presentation = LoadPresentationWithMixedLayout();
        PptxSlide slide = presentation.AddSlideWithLayout("Mixed");

        Assert.Throws<System.ArgumentException>(() => slide.PopulatePlaceholder("subtitle", "Missing"));
    }

    static PptxPresentation LoadPresentationWithMixedLayout()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(PptxPartNames.FirstSlideLayout, PptxContentTypes.SlideLayout, Encoding.UTF8.GetBytes(CreateMixedLayoutXml()));
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

    static string CreateMixedLayoutXml()
    {
        return
            "<p:sldLayout xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\" type=\"mixed\" preserve=\"1\">" +
            "<p:cSld name=\"Mixed\"><p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr/>" +
            CreateShapeXml("2", "Title", "title", "1") +
            CreateShapeXml("3", "Body", "body", "2") +
            CreateShapeXml("4", "Picture", "pic", "3") +
            CreateShapeXml("5", "Table", "tbl", "4") +
            CreateShapeXml("6", "Chart", "chart", "5") +
            CreateShapeXml("7", "Media", "media", "6") +
            CreateShapeXml("8", "Content", "obj", "7") +
            "</p:spTree></p:cSld>" +
            "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>" +
            "</p:sldLayout>";
    }

    static string CreateShapeXml(string id, string name, string type, string index)
    {
        return
            "<p:sp>" +
            "<p:nvSpPr><p:cNvPr id=\"" + id + "\" name=\"" + name + "\"/><p:cNvSpPr/><p:nvPr><p:ph type=\"" + type + "\" idx=\"" + index + "\"/></p:nvPr></p:nvSpPr>" +
            "<p:spPr/><p:txBody><a:bodyPr/><a:lstStyle/><a:p><a:r><a:t/></a:r></a:p></p:txBody>" +
            "</p:sp>";
    }
}
