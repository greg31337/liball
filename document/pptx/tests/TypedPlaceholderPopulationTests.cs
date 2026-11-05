using System;
using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class TypedPlaceholderPopulationTests
{
    [Test]
    public void TypedPopulation_ReplacesShapesAndPreservesPlaceholderInheritance()
    {
        PptxPresentation presentation = LoadPresentationWithTypedLayout();
        PptxPresentationOperations presentations = new PptxPresentationOperations();
        PptxSlideOperations slides = new PptxSlideOperations();
        PptxSlide slide = presentations.AddSlideWithLayout(presentation, "Typed");
        PptxChartData chartData = CreateChartData();

        slides.PopulateTitle(slide, "Typed Title");
        slides.PopulateBody(slide, "Typed Body");
        PptxShape image = slides.PopulatePlaceholderImage(slide, "picture", 3, CreatePngBytes());
        PptxTable table = slides.PopulatePlaceholderTable(slide, "table", 4, 2, 2);
        PptxChart chart = slides.PopulatePlaceholderChart(slide, "chart", 5, PptxChartType.Column, chartData);
        PptxMediaFile media = slides.PopulatePlaceholderMedia(slide, "media", 6, CreateVideoBytes(), "video/mp4", PptxMediaType.Video);
        PptxEmbeddedObject embeddedObject = slides.PopulatePlaceholderObject(slide, "content", 7, CreateOleBytes(), PptxContentTypes.OleObject, "Excel.Sheet.12", "Workbook");

        Assert.That(image.Id, Is.EqualTo(4));
        Assert.That(image.Name, Is.EqualTo("Picture Placeholder"));
        Assert.That(image.ElementType, Is.EqualTo("pic"));
        Assert.That(table.Shape.Id, Is.EqualTo(5));
        Assert.That(table.RowCount(), Is.EqualTo(2));
        Assert.That(table.ColumnCount(), Is.EqualTo(2));
        Assert.That(chart.Shape.Id, Is.EqualTo(6));
        Assert.That(media.Type, Is.EqualTo(PptxMediaType.Video));
        Assert.That(media.ContentType, Is.EqualTo("video/mp4"));
        Assert.That(embeddedObject.ProgramId, Is.EqualTo("Excel.Sheet.12"));
        Assert.That(embeddedObject.Name, Is.EqualTo("Workbook"));
        Assert.That(slide.Placeholders, Has.Count.EqualTo(7));
        Assert.That(slide.FindPlaceholderByIndex("picture", 3)!.Inherited, Is.True);
        Assert.That(slide.FindPlaceholderByIndex("table", 4)!.Inherited, Is.True);
        Assert.That(slide.FindPlaceholderByIndex("chart", 5)!.Inherited, Is.True);
        Assert.That(slide.FindPlaceholderByIndex("media", 6)!.Inherited, Is.True);
        Assert.That(slide.FindPlaceholderByIndex("content", 7)!.Inherited, Is.True);
        Assert.That(slides.InspectPlaceholderInheritance(slide, "content", 7).MasterPlaceholder, Is.Not.Null);

        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxSlide reopenedSlide = reopened.Slides[1];
        PptxShape reopenedImage = reopenedSlide.FindShapeById(4)!;

        Assert.That(reopenedSlide.GetPlaceholderText("title"), Is.EqualTo("Typed Title"));
        Assert.That(reopenedSlide.GetPlaceholderText("body"), Is.EqualTo("Typed Body"));
        Assert.That(reopenedSlide.GetTables(), Has.Count.EqualTo(1));
        Assert.That(reopenedSlide.GetCharts(), Has.Count.EqualTo(1));
        Assert.That(reopenedSlide.GetMedia(), Has.Count.EqualTo(1));
        Assert.That(reopenedSlide.GetEmbeddedObjects(), Has.Count.EqualTo(1));
        Assert.That(reopenedImage.ElementType, Is.EqualTo("pic"));
        Assert.That(reopenedImage.Element.OuterXml, Does.Contain("keep-placeholder"));
        Assert.That(reopenedSlide.FindPlaceholderByIndex("picture", 3)!.ShapeId, Is.EqualTo("4"));
        Assert.That(reopenedSlide.FindPlaceholderByIndex("picture", 3)!.ShapeName, Is.EqualTo("Picture Placeholder"));
        Assert.That(reopenedSlide.FindPlaceholderByIndex("picture", 3)!.InheritedFromPartName, Is.EqualTo(PptxPartNames.FirstSlideLayout));
        Assert.That(reopenedSlide.GetMedia()[0].Bytes, Is.EqualTo(CreateVideoBytes()));
        Assert.That(reopenedSlide.GetEmbeddedObjects()[0].Bytes, Is.EqualTo(CreateOleBytes()));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void GenericContentPlaceholder_AcceptsTypedTableContent()
    {
        PptxPresentation presentation = LoadPresentationWithTypedLayout();
        PptxSlideOperations operations = new PptxSlideOperations();
        PptxSlide slide = presentation.AddSlideWithLayout("Typed");

        PptxTable table = operations.PopulatePlaceholderTable(slide, "content", 7, 1, 3);
        PptxPresentation reopened = SaveReopenAndLoad(presentation);

        Assert.That(table.Shape.Id, Is.EqualTo(8));
        Assert.That(reopened.Slides[1].GetTables(), Has.Count.EqualTo(1));
        Assert.That(reopened.Slides[1].FindPlaceholderByIndex("content", 7), Is.Not.Null);
        Assert.That(reopened.Slides[1].FindPlaceholderByIndex("content", 7)!.Inherited, Is.True);
    }

    [Test]
    public void TypedPopulation_RejectsMissingPlaceholderAndInvalidMediaType()
    {
        PptxPresentation presentation = LoadPresentationWithTypedLayout();
        PptxSlideOperations operations = new PptxSlideOperations();
        PptxSlide slide = presentation.AddSlideWithLayout("Typed");

        Assert.That(() => operations.PopulatePlaceholderImage(slide, "subtitle", 99, CreatePngBytes()), Throws.ArgumentException);
        Assert.That(() => operations.PopulatePlaceholderMedia(slide, "media", 6, CreateVideoBytes(), "video/mp4", "unknown"), Throws.ArgumentException);
        Assert.That(slide.GetMedia(), Is.Empty);
    }

    static PptxPresentation LoadPresentationWithTypedLayout()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(PptxPartNames.FirstSlideMaster, PptxContentTypes.SlideMaster, Encoding.UTF8.GetBytes(CreateMasterXml()));
        package.ReplacePart(PptxPartNames.FirstSlideLayout, PptxContentTypes.SlideLayout, Encoding.UTF8.GetBytes(CreateLayoutXml()));
        return PptxDocument.OpenBytes(package.SaveBytes()).LoadPresentation();
    }

    static PptxPresentation SaveReopenAndLoad(PptxPresentation presentation)
    {
        PptxDocument document = new PptxDocument(presentation.Package);
        return PptxDocument.OpenBytes(document.SaveBytes()).LoadPresentation();
    }

    static PptxChartData CreateChartData()
    {
        PptxChartData data = new PptxChartData();
        data.Categories.Add("One");
        data.Categories.Add("Two");
        PptxChartSeries series = new PptxChartSeries();
        series.Name = "Values";
        series.Values.Add("10");
        series.Values.Add("20");
        data.Series.Add(series);
        return data;
    }

    static byte[] CreatePngBytes()
    {
        return Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
    }

    static byte[] CreateVideoBytes()
    {
        return new byte[] { 0, 0, 0, 24, 102, 116, 121, 112, 109, 112, 52, 50 };
    }

    static byte[] CreateOleBytes()
    {
        return new byte[] { 208, 207, 17, 224, 1, 2, 3, 4 };
    }

    static string CreateMasterXml()
    {
        return
            "<p:sldMaster xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:cSld name=\"Typed Master\"><p:spTree>" + GroupPropertiesXml() + CreatePlaceholderShapes(false) + "</p:spTree></p:cSld>" +
            "<p:clrMap bg1=\"lt1\" tx1=\"dk1\" bg2=\"lt2\" tx2=\"dk2\" accent1=\"accent1\" accent2=\"accent2\" accent3=\"accent3\" accent4=\"accent4\" accent5=\"accent5\" accent6=\"accent6\" hlink=\"hlink\" folHlink=\"folHlink\"/>" +
            "<p:sldLayoutIdLst><p:sldLayoutId id=\"2147483649\" r:id=\"rId2\"/></p:sldLayoutIdLst><p:txStyles><p:titleStyle/><p:bodyStyle/><p:otherStyle/></p:txStyles></p:sldMaster>";
    }

    static string CreateLayoutXml()
    {
        return
            "<p:sldLayout xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\" type=\"mixed\" preserve=\"1\">" +
            "<p:cSld name=\"Typed\"><p:spTree>" + GroupPropertiesXml() + CreatePlaceholderShapes(true) + "</p:spTree></p:cSld>" +
            "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr></p:sldLayout>";
    }

    static string CreatePlaceholderShapes(bool includeMarker)
    {
        return
            CreateShapeXml("2", "Title Placeholder", "title", "1", 0L, includeMarker) +
            CreateShapeXml("3", "Body Placeholder", "body", "2", 1000000L, includeMarker) +
            CreateShapeXml("4", "Picture Placeholder", "pic", "3", 2000000L, includeMarker) +
            CreateShapeXml("5", "Table Placeholder", "tbl", "4", 3000000L, includeMarker) +
            CreateShapeXml("6", "Chart Placeholder", "chart", "5", 4000000L, includeMarker) +
            CreateShapeXml("7", "Media Placeholder", "media", "6", 5000000L, includeMarker) +
            CreateShapeXml("8", "Content Placeholder", "obj", "7", 6000000L, includeMarker);
    }

    static string CreateShapeXml(string id, string name, string type, string index, long x, bool includeMarker)
    {
        string marker = "";
        if (includeMarker)
        {
            marker = "<a:extLst><a:ext uri=\"keep-placeholder\"/></a:extLst>";
        }

        return
            "<p:sp><p:nvSpPr><p:cNvPr id=\"" + id + "\" name=\"" + name + "\">" + marker + "</p:cNvPr><p:cNvSpPr/><p:nvPr><p:ph type=\"" + type + "\" idx=\"" + index + "\"/></p:nvPr></p:nvSpPr>" +
            "<p:spPr><a:xfrm><a:off x=\"" + x.ToString() + "\" y=\"100000\"/><a:ext cx=\"900000\" cy=\"600000\"/></a:xfrm></p:spPr>" +
            "<p:txBody><a:bodyPr/><a:lstStyle/><a:p/></p:txBody></p:sp>";
    }

    static string GroupPropertiesXml()
    {
        return "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr/>";
    }
}
