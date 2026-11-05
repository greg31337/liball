namespace chart.Tests;

[TestFixture]
public class ChartApiSmokeTests
{
    [TestCase(ChartOutputFormat.RawBitmap)]
    [TestCase(ChartOutputFormat.Bmp)]
    [TestCase(ChartOutputFormat.Png)]
    [TestCase(ChartOutputFormat.Svg)]
    public void RequiredOutputFormats_ExportRenderedScene(ChartOutputFormat format)
    {
        ChartRenderScene scene = CreateSmokeScene();

        if (format == ChartOutputFormat.RawBitmap)
        {
            RawBitmap bitmap = scene.ToRawBitmap();
            Assert.That(bitmap.Width, Is.EqualTo(32));
            Assert.That(bitmap.Height, Is.EqualTo(24));
            Assert.That(ChartExportOperations.ToRawBytes(bitmap).Length, Is.EqualTo(bitmap.Buffer.Length));
            AssertColor(bitmap.GetPixel(8, 8), 200, 40, 40, 255);
            return;
        }

        if (format == ChartOutputFormat.Bmp)
        {
            byte[] bytes = ChartExportOperations.ToBmpBytes(scene.ToRawBitmap());
            Assert.That(bytes[0], Is.EqualTo((byte)'B'));
            Assert.That(bytes[1], Is.EqualTo((byte)'M'));
            Assert.That(bytes.Length, Is.GreaterThan(54));
            return;
        }

        if (format == ChartOutputFormat.Png)
        {
            byte[] bytes = ChartExportOperations.ToPngBytes(scene.ToRawBitmap());
            Assert.That(new byte[] { bytes[0], bytes[1], bytes[2], bytes[3], bytes[4], bytes[5], bytes[6], bytes[7] }, Is.EqualTo(PngEncoder.Signature()));
            return;
        }

        string svg = scene.ToSvgString();
        Assert.That(svg, Does.Contain("<rect id=\"box\""));
        AssertValidXml(svg);
    }

    [Test]
    public void DirectSceneWorkflow_ExportsSvgWithAccessibilityAndStreams()
    {
        ChartRenderScene scene = CreateSmokeScene();
        ChartAccessibilityMetadata accessibility = new ChartAccessibilityMetadata();

        accessibility.SetTitleDescription("Smoke", "Direct scene workflow");
        string svg = ChartExportOperations.ToSvg(scene, accessibility);
        byte[] bytes = ChartExportOperations.ToSvgBytes(svg);
        System.IO.MemoryStream stream = ChartExportOperations.ToSvgStream(svg);

        Assert.That(svg, Does.Contain("<title>Smoke</title>"));
        Assert.That(svg, Does.Contain("<desc>Direct scene workflow</desc>"));
        Assert.That(System.Text.Encoding.UTF8.GetString(bytes), Is.EqualTo(svg));
        Assert.That(System.Text.Encoding.UTF8.GetString(stream.ToArray()), Is.EqualTo(svg));
    }

    [Test]
    public void DocumentCartesianWorkflow_RendersSeriesAndAllBinaryFormats()
    {
        ChartDocument document = new ChartDocument(64.0, 48.0);
        ChartPlot plot = document.AddPlotWithBounds("plot", new ChartRect(0.0, 0.0, 64.0, 48.0));
        ChartCartesianSeries line = ChartLineAreaChartsOperations.CreateLineSeries(
            "api-line",
            new double[] { 0.0, 1.0, 2.0 },
            new double[] { 0.0, 2.0, 1.0 },
            ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0));

        plot.SetMargins(new ChartPadding(0.0, 0.0, 0.0, 0.0));
        document.SetAccessibilityTitleDescription("Cartesian", "Document workflow");
        ChartRenderScene scene = document.RenderCartesian("plot", new ChartCartesianSeries[] { line }, new ChartRange(0.0, 2.0), new ChartRange(0.0, 2.0));
        string svg = document.ExportSceneSvg(scene);
        byte[] png = document.ExportScenePng(scene);
        byte[] bmp = ChartExportOperations.ToBmpBytes(scene.ToRawBitmap(), 24, false);

        Assert.That(scene.CommandAt(0).Kind, Is.EqualTo(ChartRenderCommandKind.Clear));
        Assert.That(svg, Does.Contain("<title>Cartesian</title>"));
        Assert.That(svg, Does.Contain("id=\"api-line-line-segment-0\""));
        Assert.That(new byte[] { png[0], png[1], png[2], png[3], png[4], png[5], png[6], png[7] }, Is.EqualTo(PngEncoder.Signature()));
        Assert.That(bmp[0], Is.EqualTo((byte)'B'));
        Assert.That(bmp[1], Is.EqualTo((byte)'M'));
    }

    [Test]
    public void DeclarativeWorkflow_RendersAllMarkTypesThroughCartesianRenderer()
    {
        ChartDataTable table = CreateApiTable();
        ChartVisualEncoding xy = XYEncoding();
        ChartVisualEncoding area = XYEncoding();
        ChartVisualEncoding bubble = XYEncoding();
        ChartVisualEncoding bar = XYEncoding();
        ChartVisualEncoding range = new ChartVisualEncoding();
        ChartCartesianRenderer renderer = new ChartCartesianRenderer(new ChartCartesianTransform(new ChartRect(0.0, 0.0, 100.0, 80.0), new ChartRange(0.0, 2.0), new ChartRange(0.0, 6.0)));

        xy.SetConstantNumber(ChartEncodingChannel.Size, 5.0);
        xy.SetConstantShape(ChartEncodingChannel.Shape, ChartMarkerShape.Square);
        area.SetY2("low", ChartDataKind.Number);
        bubble.SetSize("size", ChartDataKind.Number);
        bar.SetConstantNumber(ChartEncodingChannel.Size, 0.4);
        range.SetX("x", ChartDataKind.Number);
        range.SetY("low", ChartDataKind.Number);
        range.SetY2("high", ChartDataKind.Number);
        range.SetConstantNumber(ChartEncodingChannel.Size, 0.35);

        renderer.AddSeries(ChartDeclarativeChartsOperations.CreateSeries("api-line", ChartDeclarativeMark.Line, table, XYEncoding(), ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0)));
        renderer.AddSeries(ChartDeclarativeChartsOperations.CreateSeries("api-area", ChartDeclarativeMark.Area, table, area, ChartRenderStyle.CreateFill(new ChartColor(200, 220, 240, 180))));
        renderer.AddSeries(ChartDeclarativeChartsOperations.CreateSeries("api-scatter", ChartDeclarativeMark.Scatter, table, xy, ChartRenderStyle.CreateFill(ChartColor.FromRgb(30, 80, 200))));
        renderer.AddSeries(ChartDeclarativeChartsOperations.CreateSeries("api-bubble", ChartDeclarativeMark.Bubble, table, bubble, ChartRenderStyle.CreateFill(new ChartColor(100, 40, 180, 180))));
        renderer.AddSeries(ChartDeclarativeChartsOperations.CreateSeries("api-bar", ChartDeclarativeMark.VerticalBar, table, bar, ChartRenderStyle.CreateFill(ChartColor.FromRgb(200, 80, 40))));
        renderer.AddSeries(ChartDeclarativeChartsOperations.CreateSeries("api-hbar", ChartDeclarativeMark.HorizontalBar, table, bar, ChartRenderStyle.CreateFill(ChartColor.FromRgb(40, 150, 90))));
        renderer.AddSeries(ChartDeclarativeChartsOperations.CreateSeries("api-range", ChartDeclarativeMark.RangeBar, table, range, ChartRenderStyle.CreateFill(ChartColor.FromRgb(80, 80, 80))));

        ChartRenderScene scene = renderer.Render(100, 80);
        string svg = scene.ToSvgString();

        Assert.That(svg, Does.Contain("id=\"api-line-line-segment-0\""));
        Assert.That(svg, Does.Contain("id=\"api-area-area\""));
        Assert.That(svg, Does.Contain("id=\"api-scatter-point-0\""));
        Assert.That(svg, Does.Contain("id=\"api-bubble-bubble-2\""));
        Assert.That(svg, Does.Contain("id=\"api-bar-bar-1\""));
        Assert.That(svg, Does.Contain("id=\"api-hbar-bar-1\""));
        Assert.That(svg, Does.Contain("id=\"api-range-range-2\""));
        AssertValidXml(svg);
    }

    static ChartRenderScene CreateSmokeScene()
    {
        ChartRenderScene scene = new ChartRenderScene(32, 24);
        scene.AddClear(ChartColor.White());
        scene.AddRect("box", 4.0, 4.0, 12.0, 8.0, 0.0, 0.0, ChartRenderStyle.CreateFill(ChartColor.FromRgb(200, 40, 40)));
        scene.AddLine("rule", 0.0, 20.0, 32.0, 20.0, ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0));
        return scene;
    }

    static ChartDataTable CreateApiTable()
    {
        ChartDataTable table = new ChartDataTable();
        table.AddNumberColumn("x", new double[] { 0.0, 1.0, 2.0 });
        table.AddNumberColumn("y", new double[] { 2.0, 5.0, 3.0 });
        table.AddNumberColumn("low", new double[] { 1.0, 2.0, 2.0 });
        table.AddNumberColumn("high", new double[] { 3.0, 6.0, 4.0 });
        table.AddNumberColumn("size", new double[] { 1.0, 4.0, 9.0 });
        return table;
    }

    static ChartVisualEncoding XYEncoding()
    {
        ChartVisualEncoding encoding = new ChartVisualEncoding();
        encoding.SetX("x", ChartDataKind.Number);
        encoding.SetY("y", ChartDataKind.Number);
        return encoding;
    }

    static void AssertValidXml(string svg)
    {
        System.Xml.XmlDocument document = new System.Xml.XmlDocument();
        document.LoadXml(svg);
        Assert.That(document.DocumentElement!.Name, Is.EqualTo("svg"));
    }

    static void AssertColor(ChartColor color, int r, int g, int b, int a)
    {
        Assert.That(color.R, Is.EqualTo((byte)r));
        Assert.That(color.G, Is.EqualTo((byte)g));
        Assert.That(color.B, Is.EqualTo((byte)b));
        Assert.That(color.A, Is.EqualTo((byte)a));
    }
}
