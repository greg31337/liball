using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfAbsoluteLayoutAndContentLayerTests
{
    [Test]
    public void AbsoluteElementMeasuresPositionedFrameAndChildFit()
    {
        PdfLayoutFactory factory = new PdfLayoutFactory();
        PdfAbsoluteLayoutElement absolute = factory.CreateAbsoluteElement(factory.CreateFixedBlock(20, 10), 5, 7, 20, 10);

        PdfLayoutResult fitting = absolute.Measure(new PdfLayoutContext(25, 17));
        PdfLayoutResult narrow = absolute.Measure(new PdfLayoutContext(24, 17));
        PdfAbsoluteLayoutElement overflowingChild = factory.CreateAbsoluteElement(factory.CreateFixedBlock(21, 10), 5, 7, 20, 10);

        Assert.That(fitting.Size.Width, Is.EqualTo(25));
        Assert.That(fitting.Size.Height, Is.EqualTo(17));
        Assert.That(fitting.Box.GetContentSize().Width, Is.EqualTo(25));
        Assert.That(fitting.Box.GetContentSize().Height, Is.EqualTo(17));
        Assert.That(fitting.Fits, Is.True);
        Assert.That(narrow.Fits, Is.False);
        Assert.That(overflowingChild.Measure(new PdfLayoutContext(25, 17)).Fits, Is.False);
    }

    [Test]
    public void AbsoluteElementDrawsAtOffsetAndClipsToItsFrame()
    {
        PdfLayoutFactory factory = new PdfLayoutFactory();
        PdfElementStyle style = new PdfElementStyle();
        style.HasBackground = true;
        style.BackgroundColor = PdfColor.Gray(0.25);
        PdfBlock block = factory.CreateFixedBlock(20, 10);
        block.SetStyle(style);
        PdfAbsoluteLayoutElement absolute = factory.CreateAbsoluteElement(block, 5, 7, 20, 10);
        PdfPage page = new PdfPage(100, 100);

        absolute.Draw(new PdfLayoutDrawContext(page, 10, 20, 50, 50));

        string content = page.GetCanvas().ToPdfString();
        Assert.That(content, Does.Contain("15 27 20 10 re\nW\nn"));
        Assert.That(content, Does.Contain("15 27 20 10 re f"));

        PdfPage unclippedPage = new PdfPage(100, 100);
        absolute.DisableClipping();
        absolute.Draw(new PdfLayoutDrawContext(unclippedPage, 10, 20, 50, 50));
        Assert.That(unclippedPage.GetCanvas().ToPdfString(), Does.Not.Contain("\nW\n"));
    }

    [Test]
    public void AbsoluteElementFlowsByOccupiedSizeAndDebugsPositionedFrame()
    {
        PdfDocument document = new PdfDocument();
        PdfLayoutDocument layout = new PdfLayoutDocument(document, new PdfPageDimensions(100, 100), new PdfPageMargins(0, 0, 0, 0));
        PdfAbsoluteLayoutElement absolute = new PdfLayoutFactory().CreateAbsoluteElement(new PdfLayoutFactory().CreateFixedBlock(20, 10), 5, 7, 20, 10);
        layout.EnableDebugOverlayWithStyle(PdfColor.Rgb(1, 0, 0), 0.5);
        layout.AddItem(absolute);

        PdfPageCursor cursor = layout.Render();

        string content = document.GetPage(0).GetCanvas().ToPdfString();
        Assert.That(cursor.CurrentY, Is.EqualTo(83));
        Assert.That(content, Does.Contain("5 90 20 10 re\nW\nn"));
        Assert.That(content, Does.Contain("5 90 20 10 re S"));
        Assert.That(content, Does.Not.Contain("0 83 25 17 re S"));
    }

    [Test]
    public void OrderedContentLayersRenderInStablePlacementBands()
    {
        PdfDocument document = new PdfDocument();
        PdfLayoutDocument layout = new PdfLayoutDocument(document, new PdfPageDimensions(100, 100), new PdfPageMargins(0, 0, 0, 0));
        PdfLayoutFactory factory = new PdfLayoutFactory();
        PdfElementStyle bodyStyle = new PdfElementStyle();
        bodyStyle.HasBackground = true;
        bodyStyle.BackgroundColor = PdfColor.Gray(0.3);
        PdfBlock body = factory.CreateFixedBlock(10, 10);
        body.SetStyle(bodyStyle);

        layout.AddContentLayer(CreateGrayLayer(factory, 0.2, PdfContentLayerPlacement.Background, 20));
        layout.AddContentLayer(CreateGrayLayer(factory, 0.1, PdfContentLayerPlacement.Background, 10));
        layout.AddContentLayer(CreateGrayLayer(factory, 0.5, PdfContentLayerPlacement.Watermark, 20));
        layout.AddContentLayer(CreateGrayLayer(factory, 0.4, PdfContentLayerPlacement.Watermark, 10));
        layout.AddContentLayer(CreateGrayLayer(factory, 0.6, PdfContentLayerPlacement.Foreground, 10));
        layout.AddBlock(body);
        layout.Render();

        string content = document.GetPage(0).GetCanvas().ToPdfString();
        Assert.That(content.IndexOf("0.1 g"), Is.LessThan(content.IndexOf("0.2 g")));
        Assert.That(content.IndexOf("0.2 g"), Is.LessThan(content.IndexOf("0.3 g")));
        Assert.That(content.IndexOf("0.3 g"), Is.LessThan(content.IndexOf("0.4 g")));
        Assert.That(content.IndexOf("0.4 g"), Is.LessThan(content.IndexOf("0.5 g")));
        Assert.That(content.IndexOf("0.5 g"), Is.LessThan(content.IndexOf("0.6 g")));
    }

    [Test]
    public void ContentLayerRegionControlsMeasurementClippingAndDebugRendering()
    {
        PdfDocument document = new PdfDocument();
        PdfLayoutDocument layout = new PdfLayoutDocument(document, new PdfPageDimensions(100, 100), new PdfPageMargins(0, 0, 0, 0));
        PdfLayoutFactory factory = new PdfLayoutFactory();
        PdfContentLayer layer = factory.CreateContentLayer(
            factory.CreateBackgroundTemplate(PdfColor.Gray(0.7), 100),
            PdfContentLayerPlacement.Background,
            0);
        layer.SetBounds(new PdfRectangle(10, 15, 30, 20));
        layout.EnableDebugOverlayWithStyle(PdfColor.Rgb(1, 0, 0), 0.5);
        layout.AddContentLayer(layer);
        PdfElementStyle bodyStyle = new PdfElementStyle();
        bodyStyle.HasBackground = true;
        bodyStyle.BackgroundColor = PdfColor.Gray(0.3);
        PdfBlock body = factory.CreateFixedBlock(20, 10);
        body.SetStyle(bodyStyle);
        layout.AddBlock(body);

        layout.Render();

        string content = document.GetPage(0).GetCanvas().ToPdfString();
        Assert.That(content, Does.Contain("10 15 30 20 re\nW\nn"));
        Assert.That(content, Does.Contain("10 15 30 20 re f"));
        Assert.That(content, Does.Contain("0.5 w"));
        Assert.That(content, Does.Contain("10 15 30 20 re S"));
        Assert.That(content.IndexOf("0.3 g"), Is.LessThan(content.IndexOf("10 15 30 20 re S")));
    }

    [Test]
    public void ContentLayerCanBeReusedAndClearedWithoutLeakingConfiguration()
    {
        PdfLayoutFactory factory = new PdfLayoutFactory();
        PdfContentLayer shared = CreateGrayLayer(factory, 0.8, PdfContentLayerPlacement.Background, 0);
        PdfDocument firstDocument = new PdfDocument();
        PdfLayoutDocument firstLayout = new PdfLayoutDocument(firstDocument, new PdfPageDimensions(80, 60), new PdfPageMargins(0, 0, 0, 0));
        PdfDocument secondDocument = new PdfDocument();
        PdfLayoutDocument secondLayout = new PdfLayoutDocument(secondDocument, new PdfPageDimensions(120, 70), new PdfPageMargins(0, 0, 0, 0));

        firstLayout.AddContentLayer(shared);
        secondLayout.AddContentLayer(shared);
        firstLayout.Render();
        secondLayout.ClearContentLayers();
        secondLayout.Render();

        Assert.That(firstDocument.GetPage(0).GetCanvas().ToPdfString(), Does.Contain("0.8 g"));
        Assert.That(secondDocument.GetPage(0).GetCanvas().ToPdfString(), Does.Not.Contain("0.8 g"));
    }

    [Test]
    public void AbsoluteElementsAndContentLayersRejectInvalidConfiguration()
    {
        PdfLayoutFactory factory = new PdfLayoutFactory();
        PdfBlock block = factory.CreateFixedBlock(10, 10);
        PdfPageTemplate template = factory.CreateBackgroundTemplate(PdfColor.Gray(0.5), 10);
        PdfLayoutDocument layout = new PdfLayoutDocument(new PdfDocument(), new PdfPageDimensions(100, 100), new PdfPageMargins(0, 0, 0, 0));

        Assert.Throws<ArgumentNullException>(() => factory.CreateAbsoluteElement(null!, 0, 0, 10, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateAbsoluteElement(block, -1, 0, 10, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateAbsoluteElement(block, 0, -1, 10, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateAbsoluteElement(block, 0, 0, 0, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateAbsoluteElement(block, 0, 0, 10, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateAbsoluteElement(block, double.NaN, 0, 10, 10));
        Assert.Throws<ArgumentNullException>(() => factory.CreateContentLayer(null!, PdfContentLayerPlacement.Background, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateContentLayer(template, (PdfContentLayerPlacement)99, 0));
        PdfContentLayer layer = factory.CreateContentLayer(template, PdfContentLayerPlacement.Background, 0);
        Assert.Throws<ArgumentNullException>(() => layer.SetBounds(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => layer.SetBounds(new PdfRectangle(0, 0, 0, 10)));
        Assert.Throws<ArgumentOutOfRangeException>(() => layer.SetBounds(new PdfRectangle(0, 0, 10, 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => layer.SetBounds(new PdfRectangle(double.PositiveInfinity, 0, 10, 10)));
        Assert.Throws<ArgumentNullException>(() => layout.AddContentLayer(null!));
    }

    static PdfContentLayer CreateGrayLayer(PdfLayoutFactory factory, double gray, PdfContentLayerPlacement placement, int order)
    {
        return factory.CreateContentLayer(factory.CreateBackgroundTemplate(PdfColor.Gray(gray), 100), placement, order);
    }
}
