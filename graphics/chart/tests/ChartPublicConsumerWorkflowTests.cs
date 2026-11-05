namespace chart.Tests;

[TestFixture]
public class ChartPublicConsumerWorkflowTests
{
    [Test]
    public void RawBitmapWorkflow_UsesFactoryAndBitmapInterfaces()
    {
        IRawBitmapFactory factory = new RawBitmapFactory();
        RawBitmap created = factory.CreateWithStride(2, 2, 12, ChartPixelFormat.Rgba32);
        IRawBitmap bitmap = created;
        ChartColor red = new ChartColor(255, 0, 0, 255);
        ChartColor green = new ChartColor(0, 255, 0, 255);

        bitmap.SetPixel(0, 0, red);
        bitmap.SetPixel(1, 1, green);
        System.Span<byte> bufferSpan = bitmap.GetBufferSpan();
        System.Span<byte> rowSpan = bitmap.GetRowSpan(0);
        System.Span<byte> pixelRowSpan = bitmap.GetPixelRowSpan(0);
        System.ReadOnlySpan<byte> readOnlyBuffer = bitmap.GetReadOnlyBufferSpan();
        System.ReadOnlySpan<byte> readOnlyRow = bitmap.GetReadOnlyRowSpan(0);
        System.ReadOnlySpan<byte> readOnlyPixelRow = bitmap.GetReadOnlyPixelRowSpan(0);

        Assert.That(bitmap.BytesPerPixel(), Is.EqualTo(4));
        Assert.That(bitmap.GetByteOffset(1, 1), Is.EqualTo(16));
        Assert.That(bitmap.ContainsPixel(1, 1), Is.True);
        Assert.That(bitmap.ContainsPixel(2, 1), Is.False);
        Assert.That(bitmap.GetPixel(0, 0).EqualsColor(red), Is.True);
        Assert.That(bufferSpan.Length, Is.EqualTo(24));
        Assert.That(rowSpan.Length, Is.EqualTo(12));
        Assert.That(pixelRowSpan.Length, Is.EqualTo(8));
        Assert.That(readOnlyBuffer.Length, Is.EqualTo(24));
        Assert.That(readOnlyRow.Length, Is.EqualTo(12));
        Assert.That(readOnlyPixelRow.Length, Is.EqualTo(8));

        byte[] copiedBuffer = bitmap.CopyBuffer();
        RawBitmap copied = bitmap.Copy();
        RawBitmap rgb = bitmap.ConvertTo(ChartPixelFormat.Rgb24);
        ChartColor[] palette = new ChartColor[] { ChartColor.Black(), red, green };
        RawBitmap indexed = bitmap.ConvertToIndexed(palette);
        IRawBitmap indexedInterface = indexed;
        indexedInterface.SetPaletteIndex(0, 1, 2);
        indexedInterface.SetPaletteColor(2, new ChartColor(0, 200, 0, 255));
        indexedInterface.SetPalette(indexedInterface.GetPalette());

        Assert.That(copied.GetStride(), Is.EqualTo(12));
        Assert.That(copied.CopyBuffer(), Is.EqualTo(copiedBuffer));
        Assert.That(rgb.GetPixelFormat(), Is.EqualTo(ChartPixelFormat.Rgb24));
        Assert.That(indexedInterface.GetPaletteCount(), Is.EqualTo(3));
        Assert.That(indexedInterface.GetPaletteIndex(0, 1), Is.EqualTo(2));
        Assert.That(indexedInterface.GetPaletteColor(2).G, Is.EqualTo(200));

        bitmap.SetWidth(bitmap.GetWidth());
        bitmap.SetHeight(bitmap.GetHeight());
        bitmap.SetStride(bitmap.GetStride());
        bitmap.SetPixelFormat(bitmap.GetPixelFormat());
        bitmap.SetBuffer(bitmap.GetBuffer());

        RawBitmap wrapped = factory.Wrap(2, 1, 8, ChartPixelFormat.Rgba32, new byte[8]);
        RawBitmap indexedCreated = factory.CreateIndexedWithStride(2, 1, 4, palette);
        RawBitmap indexedWrapped = factory.WrapIndexed(2, 1, 4, new byte[4], palette);
        RawBitmap converted = factory.Convert(created, ChartPixelFormat.Bgra32);
        RawBitmap convertedIndexed = factory.ConvertToIndexed(created, palette);

        Assert.That(factory.CalculateStride(3, ChartPixelFormat.Rgb24), Is.EqualTo(9));
        Assert.That(factory.BytesPerPixel(ChartPixelFormat.Bgra32), Is.EqualTo(4));
        Assert.That(wrapped.GetWidth(), Is.EqualTo(2));
        Assert.That(indexedCreated.GetPaletteCount(), Is.EqualTo(3));
        Assert.That(indexedWrapped.GetPixelFormat(), Is.EqualTo(ChartPixelFormat.Indexed8));
        Assert.That(converted.GetPixelFormat(), Is.EqualTo(ChartPixelFormat.Bgra32));
        Assert.That(convertedIndexed.GetPaletteCount(), Is.EqualTo(3));
    }

    [Test]
    public void ExportWorkflow_UsesEveryPublicExportDestination()
    {
        ChartRenderScene scene = ConsumerScene();
        RawBitmap bitmap = scene.ToRawBitmap();
        IChartExport export = new ChartExport();
        ChartBmpOptions bmpOptions = new ChartBmpOptions();
        ChartPngOptions pngOptions = new ChartPngOptions();
        ChartAccessibilityMetadata accessibility = new ChartAccessibilityMetadata();
        ChartSvgOptions svgOptions = new ChartSvgOptions();
        string bmpPath = TestPath("consumer-export.bmp");
        string bmpOptionsPath = TestPath("consumer-export-options.bmp");
        string bmpUsingOptionsPath = TestPath("consumer-export-using-options.bmp");
        string pngPath = TestPath("consumer-export.png");
        string pngOptionsPath = TestPath("consumer-export-options.png");
        string svgPath = TestPath("consumer-export.svg");

        byte[] rawBytes = export.ToRawBytes(bitmap);
        RawBitmap rawCopy = export.ToRawBitmap(bitmap);
        byte[] bmp = export.ToBmpBytes(bitmap);
        byte[] bmpWithOptions = export.ToBmpBytesWithOptions(bitmap, 24, false);
        byte[] bmpUsingOptions = export.ToBmpBytesUsingOptions(bitmap, bmpOptions);
        System.IO.MemoryStream bmpStream = new System.IO.MemoryStream();
        export.WriteBmp(bitmap, bmpStream, bmpOptions);
        export.SaveBmp(bitmap, bmpPath);
        export.SaveBmpWithOptions(bitmap, bmpOptionsPath, 24, false);
        export.SaveBmpUsingOptions(bitmap, bmpUsingOptionsPath, bmpOptions);

        byte[] png = export.ToPngBytes(bitmap);
        byte[] pngWithOptions = export.ToPngBytesWithOptions(bitmap, pngOptions);
        System.IO.MemoryStream pngStream = new System.IO.MemoryStream();
        export.WritePng(bitmap, pngStream, pngOptions);
        export.SavePng(bitmap, pngPath);
        export.SavePngWithOptions(bitmap, pngOptionsPath, pngOptions);

        accessibility.SetTitleDescription("Consumer chart", "Public SVG workflow");
        string svg = export.ToSvg(scene, accessibility);
        string svgWithOptions = export.ToSvgWithOptions(scene, accessibility, svgOptions);
        string emptySvg = export.CreateEmptySvg(4, 3);
        byte[] svgBytes = export.ToSvgBytes(svg);
        System.IO.MemoryStream svgInputStream = export.ToSvgStream(svg);
        System.IO.MemoryStream svgOutputStream = new System.IO.MemoryStream();
        export.WriteSvg(svg, svgOutputStream);
        export.SaveSvg(svg, svgPath);

        Assert.That(rawCopy.CopyBuffer(), Is.EqualTo(rawBytes));
        AssertBmp(bmp);
        AssertBmp(bmpWithOptions);
        AssertBmp(bmpUsingOptions);
        Assert.That(bmpStream.ToArray(), Is.EqualTo(bmpUsingOptions));
        Assert.That(System.IO.File.ReadAllBytes(bmpPath), Is.EqualTo(bmp));
        Assert.That(System.IO.File.ReadAllBytes(bmpOptionsPath), Is.EqualTo(bmpWithOptions));
        Assert.That(System.IO.File.ReadAllBytes(bmpUsingOptionsPath), Is.EqualTo(bmpUsingOptions));
        AssertPng(png);
        AssertPng(pngWithOptions);
        Assert.That(pngStream.ToArray(), Is.EqualTo(pngWithOptions));
        Assert.That(System.IO.File.ReadAllBytes(pngPath), Is.EqualTo(png));
        Assert.That(System.IO.File.ReadAllBytes(pngOptionsPath), Is.EqualTo(pngWithOptions));
        Assert.That(svgWithOptions, Does.Contain("Consumer chart"));
        Assert.That(emptySvg, Does.Contain("width=\"4\" height=\"3\""));
        Assert.That(svgBytes, Is.EqualTo(System.Text.Encoding.UTF8.GetBytes(svg)));
        Assert.That(svgInputStream.ToArray(), Is.EqualTo(svgBytes));
        Assert.That(svgOutputStream.ToArray(), Is.EqualTo(svgBytes));
        Assert.That(System.IO.File.ReadAllText(svgPath), Is.EqualTo(svg));
    }

    [Test]
    public void UnifiedExportWorkflow_UsesScenesTimelinesDocumentsStreamsAndFiles()
    {
        IChartExport export = new ChartExport();
        ChartRenderScene scene = ConsumerScene();
        ChartExportOptions options = new ChartExportOptions();

        options.Format = ChartOutputFormat.RawBitmap;
        ChartExportResult raw = export.ExportScene(scene, options);
        options.Format = ChartOutputFormat.Bmp;
        ChartExportResult bmp = export.ExportScene(scene, options);
        options.Format = ChartOutputFormat.Png;
        ChartExportResult png = export.ExportScene(scene, options);
        options.Format = ChartOutputFormat.Svg;
        ChartExportResult svg = export.ExportScene(scene, options);

        ChartTimeline timeline = new ChartTimeline();
        timeline.AddFrame(new ChartTimelineFrame("first", scene, 25));
        timeline.AddFrame(new ChartTimelineFrame("second", scene.Copy(), 35));
        ChartFrameExportResult[] frames = export.ExportTimeline(timeline, options);

        RecordingRenderer renderer = new RecordingRenderer();
        ChartDocument document = new ChartDocument(4.0, 3.0);
        document.SetAccessibilityTitleDescription("Document", "Custom renderer");
        ChartExportResult documentResult = export.ExportDocument(document, renderer, options);
        System.IO.MemoryStream sceneStream = new System.IO.MemoryStream();
        System.IO.MemoryStream documentStream = new System.IO.MemoryStream();
        export.WriteSceneExport(scene, sceneStream, options);
        export.WriteDocumentExport(document, renderer, documentStream, options);
        string scenePath = TestPath("consumer-unified-scene.svg");
        string documentPath = TestPath("consumer-unified-document.svg");
        export.SaveSceneExport(scene, scenePath, options);
        export.SaveDocumentExport(document, renderer, documentPath, options);

        Assert.That(raw.Bitmap, Is.Not.Null);
        AssertBmp(bmp.Bytes);
        AssertPng(png.Bytes);
        Assert.That(svg.Text, Does.Contain("consumer-rect"));
        Assert.That(frames.Length, Is.EqualTo(2));
        Assert.That(frames[0].DurationMilliseconds, Is.EqualTo(25));
        Assert.That(documentResult.Text, Does.Contain("custom-renderer"));
        Assert.That(System.Text.Encoding.UTF8.GetString(sceneStream.ToArray()), Does.Contain("consumer-rect"));
        Assert.That(System.Text.Encoding.UTF8.GetString(documentStream.ToArray()), Does.Contain("custom-renderer"));
        Assert.That(System.IO.File.ReadAllText(scenePath), Does.Contain("consumer-rect"));
        Assert.That(System.IO.File.ReadAllText(documentPath), Does.Contain("custom-renderer"));
        Assert.That(renderer.RenderCount, Is.EqualTo(3));
    }

    [Test]
    public void DirectSceneWorkflow_UsesRenderTargetAndRetainedSceneInterfaces()
    {
        ChartRenderScene concreteScene = new ChartRenderScene(64, 48);
        IChartRenderTarget target = concreteScene;
        IChartRenderScene scene = concreteScene;
        ChartRenderStyle stroke = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);
        ChartRenderStyle fill = ChartRenderStyle.CreateFill(new ChartColor(20, 80, 160, 255));
        ChartPoint[] points = new ChartPoint[] { new ChartPoint(2.0, 2.0), new ChartPoint(10.0, 8.0), new ChartPoint(16.0, 2.0) };
        ChartPath path = new ChartPath();
        path.MoveTo(0.0, 0.0);
        path.LineTo(1.0, 0.0);
        path.LineTo(0.5, 1.0);
        path.Close();
        RawBitmap image = new RawBitmap(1, 1, ChartPixelFormat.Rgba32);
        image.SetPixel(0, 0, new ChartColor(255, 0, 255, 255));

        target.AddClear(ChartColor.White());
        target.BeginGroup("consumer-group");
        target.PushTransform("consumer-transform", new ChartMatrix(1.0, 0.0, 0.0, 1.0, 1.0, 1.0));
        target.PushClip("consumer-clip", new ChartRect(0.0, 0.0, 60.0, 44.0));
        target.AddLine("line", 2.0, 2.0, 20.0, 2.0, stroke);
        target.AddPolyline("polyline", points, stroke);
        target.AddPolygon("polygon", points, fill);
        target.AddRect("rect", 2.0, 10.0, 12.0, 8.0, 1.0, 1.0, fill);
        target.AddCircle("circle", 24.0, 12.0, 4.0, fill);
        target.AddEllipse("ellipse", 36.0, 12.0, 6.0, 3.0, fill);
        target.AddPath("path", path, fill);
        target.AddText("text", "public", 2.0, 28.0, fill);
        target.AddMarker("marker", ChartMarkerShape.Circle, 22.0, 28.0, 5.0, fill);
        target.AddCustomMarker("custom-marker", path, 32.0, 28.0, 5.0, fill);
        target.AddArc("arc", 44.0, 28.0, 5.0, 0.0, 3.14, stroke);
        target.AddWedge("wedge", 54.0, 28.0, 2.0, 6.0, 0.0, 1.5, fill);
        target.AddImage("image", image, 2.0, 34.0, 4.0, 4.0, fill);
        target.PopClip();
        target.PopTransform();
        target.EndGroup();

        ChartTextBlock textBlock = new ChartTextBlock();
        textBlock.SetText("block");
        textBlock.SetStyle(ChartColor.Black(), "sans-serif", 10);
        scene.AddTextBlock("text-block", textBlock, 10.0, 42.0);

        ChartRenderCommand[] commands = scene.CopyCommands();
        ChartRenderScene copy = scene.Copy();
        IChartRenderScene replay = new ChartRenderScene(target.GetWidth(), target.GetHeight());
        replay.AddCommand(commands[0]);
        replay.SetCommands(scene.GetCommands());
        replay.SetCommandCount(scene.GetCommandCount());
        replay.SetWidth(target.GetWidth());
        replay.SetHeight(target.GetHeight());
        RawBitmap raster = scene.ToRawBitmap();
        RawBitmap destination = new RawBitmap(64, 48, ChartPixelFormat.Rgba32);
        scene.RenderRaster(destination);
        string svg = scene.ToSvgString();
        string trace = scene.ToTraceString();

        Assert.That(target.GetWidth(), Is.EqualTo(64));
        Assert.That(target.GetHeight(), Is.EqualTo(48));
        Assert.That(scene.CommandAt(0).GetKind(), Is.EqualTo(ChartRenderCommandKind.Clear));
        Assert.That(scene.GetCommandCount(), Is.EqualTo(commands.Length));
        Assert.That(copy.GetCommandCount(), Is.EqualTo(commands.Length));
        Assert.That(replay.GetCommandCount(), Is.EqualTo(commands.Length));
        Assert.That(raster.GetWidth(), Is.EqualTo(64));
        Assert.That(destination.GetPixel(0, 0).EqualsColor(ChartColor.White()), Is.True);
        Assert.That(svg, Does.Contain("consumer-group"));
        Assert.That(trace, Does.Contain("Text id=text-block text=\"block\""));
    }

    [Test]
    public void DeclarativeDocumentWorkflow_UsesPublicFacadesAndCustomTickFormatter()
    {
        ChartDataTable table = new ChartDataTable();
        IChartDataTable tableApi = table;
        tableApi.AddNumberColumn("x", new double[] { 0.0, 1.0, 2.0 });
        tableApi.AddNumberColumn("y", new double[] { 1.0, 3.0, 2.0 });
        tableApi.AddNumberColumn("low", new double[] { 0.0, 1.0, 1.0 });
        tableApi.AddNumberColumn("high", new double[] { 2.0, 4.0, 3.0 });
        tableApi.AddNumberColumn("size", new double[] { 1.0, 2.0, 3.0 });
        tableApi.AddCategoryColumn("facet", new string? [] { "A", "B", "A" });

        ChartVisualEncoding xy = Encoding("x", "y");
        ChartVisualEncoding areaEncoding = Encoding("x", "y");
        areaEncoding.SetY2("low", ChartDataKind.Number);
        ChartVisualEncoding bubbleEncoding = Encoding("x", "y");
        bubbleEncoding.SetSize("size", ChartDataKind.Number);
        ChartVisualEncoding barEncoding = Encoding("x", "y");
        barEncoding.SetConstantNumber(ChartEncodingChannel.Size, 0.5);
        ChartVisualEncoding rangeEncoding = Encoding("x", "low");
        rangeEncoding.SetY2("high", ChartDataKind.Number);
        rangeEncoding.SetConstantNumber(ChartEncodingChannel.Size, 0.5);
        ChartVisualEncoding facetEncoding = Encoding("x", "y");
        facetEncoding.SetWrap("facet", ChartDataKind.Category);
        ChartRenderStyle stroke = ChartRenderStyle.CreateStroke(new ChartColor(0, 80, 180, 255), 2.0);
        ChartRenderStyle fill = ChartRenderStyle.CreateFill(new ChartColor(40, 120, 200, 255));
        IChartDeclarativeCharts charts = new ChartDeclarativeCharts();

        ChartCartesianSeries line = charts.CreateLineSeries("line", table, xy, stroke);
        ChartCartesianSeries area = charts.CreateAreaSeries("area", table, areaEncoding, fill);
        ChartCartesianSeries scatter = charts.CreateScatterSeries("scatter", table, xy, fill);
        ChartCartesianSeries bubble = charts.CreateBubbleSeries("bubble", table, bubbleEncoding, 2.0, 8.0, fill);
        ChartCartesianSeries verticalBars = charts.CreateVerticalBarSeries("vertical", table, barEncoding, fill);
        ChartCartesianSeries horizontalBars = charts.CreateHorizontalBarSeries("horizontal", table, barEncoding, fill);
        ChartCartesianSeries rangeBars = charts.CreateRangeBarSeries("range", table, rangeEncoding, fill);
        ChartCartesianSeries generic = charts.CreateSeries("generic", ChartDeclarativeMark.Point, table, xy, fill);
        ChartCartesianSeries[] facets = charts.CreateWrapFacetSeries("facet", ChartDeclarativeMark.Point, table, facetEncoding, fill);
        ChartDeclarativeLayer layer = new ChartDeclarativeLayer("layer", ChartDeclarativeMark.Line, table, xy, stroke);
        ChartDeclarativeView view = charts.CreateView(new ChartDeclarativeLayer[] { layer });

        IChartDocument document = new ChartDocument(240.0, 160.0);
        document.SetMargins(new ChartPadding(12.0, 12.0, 12.0, 12.0));
        document.SetTitle("Public consumer");
        ChartPlot plot = document.AddPlotById("plot");
        plot.SetCartesianDomains(new ChartRange(0.0, 2.0), new ChartRange(0.0, 4.0));
        ChartAxis bottom = plot.AddBottomAxis(0.0, 2.0, 3);
        PrefixFormatter formatter = new PrefixFormatter("x=");
        bottom.SetTickFormatter(formatter);
        bottom.SetTickLabels(true, false, 0.0);
        plot.AddSeries(line);
        ChartRenderScene scene = document.RenderComposition();
        string svg = document.ExportSceneSvg(scene);
        byte[] png = document.ExportScenePng(scene);

        Assert.That(tableApi.GetRowCount(), Is.EqualTo(3));
        Assert.That(area.GetCommandCount(), Is.EqualTo(1));
        Assert.That(scatter.GetCommandCount(), Is.EqualTo(3));
        Assert.That(bubble.GetCommandCount(), Is.EqualTo(3));
        Assert.That(verticalBars.GetCommandCount(), Is.EqualTo(3));
        Assert.That(horizontalBars.GetCommandCount(), Is.EqualTo(3));
        Assert.That(rangeBars.GetCommandCount(), Is.EqualTo(3));
        Assert.That(generic.GetCommandCount(), Is.EqualTo(3));
        Assert.That(facets.Length, Is.EqualTo(2));
        Assert.That(view.Series.Length, Is.EqualTo(1));
        Assert.That(formatter.FormatCount, Is.EqualTo(3));
        Assert.That(svg, Does.Contain(">x=0<"));
        AssertPng(png);
    }

    static ChartRenderScene ConsumerScene()
    {
        ChartRenderScene scene = new ChartRenderScene(4, 3);
        scene.AddClear(ChartColor.White());
        scene.AddRect("consumer-rect", 1.0, 1.0, 2.0, 1.0, 0.0, 0.0, ChartRenderStyle.CreateFill(new ChartColor(10, 20, 30, 255)));
        return scene;
    }

    static ChartVisualEncoding Encoding(string x, string y)
    {
        ChartVisualEncoding encoding = new ChartVisualEncoding();
        IChartVisualEncoding encodingApi = encoding;
        encodingApi.SetX(x, ChartDataKind.Number);
        encodingApi.SetY(y, ChartDataKind.Number);
        return encoding;
    }

    static string TestPath(string name)
    {
        return System.IO.Path.Combine(TestContext.CurrentContext.WorkDirectory, name);
    }

    static void AssertBmp(byte[] bytes)
    {
        Assert.That(bytes.Length, Is.GreaterThan(2));
        Assert.That(bytes[0], Is.EqualTo(66));
        Assert.That(bytes[1], Is.EqualTo(77));
    }

    static void AssertPng(byte[] bytes)
    {
        Assert.That(bytes.Length, Is.GreaterThan(8));
        Assert.That(bytes[0], Is.EqualTo(137));
        Assert.That(bytes[1], Is.EqualTo(80));
        Assert.That(bytes[2], Is.EqualTo(78));
        Assert.That(bytes[3], Is.EqualTo(71));
    }

    class RecordingRenderer : IChartRenderer
    {
        public int RenderCount;

        public void RenderTo(IChartRenderTarget target)
        {
            RenderCount = RenderCount + 1;
            target.AddRect("custom-renderer", 0.0, 0.0, 3.0, 2.0, 0.0, 0.0, ChartRenderStyle.CreateFill(ChartColor.Black()));
        }
    }

    class PrefixFormatter : IChartTickFormatter
    {
        public string Prefix;
        public int FormatCount;

        public PrefixFormatter(string prefix)
        {
            Prefix = prefix;
            FormatCount = 0;
        }

        public string FormatTick(ChartTick tick)
        {
            FormatCount = FormatCount + 1;
            return Prefix + tick.GetValue().ToString("0", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
