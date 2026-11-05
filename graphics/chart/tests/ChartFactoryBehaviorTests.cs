namespace chart.Tests;

[TestFixture]
public class ChartFactoryBehaviorTests
{
    [Test]
    public void GeometryAndColorFactories_CreateExpectedValues()
    {
        ChartPointFactory points = new ChartPointFactory();
        ChartPoint left = new ChartPoint(1.0, 2.0);
        ChartPoint right = new ChartPoint(3.0, 5.0);
        ChartPoint added = points.Add(left, right);
        ChartPoint subtracted = points.Subtract(right, left);
        ChartPoint scaled = points.Scale(left, 2.0);
        ChartPoint interpolated = points.Lerp(left, right, 0.5);

        AssertPoint(added, 4.0, 7.0);
        AssertPoint(subtracted, 2.0, 3.0);
        AssertPoint(scaled, 2.0, 4.0);
        AssertPoint(interpolated, 2.0, 3.5);

        ChartMatrixFactory matrices = new ChartMatrixFactory();
        AssertPoint(matrices.Identity().TransformPoint(left), 1.0, 2.0);
        AssertPoint(matrices.Translation(4.0, 6.0).TransformPoint(left), 5.0, 8.0);
        AssertPoint(matrices.Scale(2.0, 3.0).TransformPoint(left), 2.0, 6.0);
        AssertPoint(matrices.Rotation(System.Math.PI * 0.5).TransformPoint(new ChartPoint(2.0, 0.0)), 0.0, 2.0);

        ChartPadding padding = new ChartPaddingFactory().All(3.0);
        Assert.That(padding.GetLeft(), Is.EqualTo(3.0));
        Assert.That(padding.GetTop(), Is.EqualTo(3.0));
        Assert.That(padding.GetRight(), Is.EqualTo(3.0));
        Assert.That(padding.GetBottom(), Is.EqualTo(3.0));

        ChartColorFactory colors = new ChartColorFactory();
        AssertColor(colors.FromRgb(1, 2, 3), 1, 2, 3, 255);
        AssertColor(colors.FromRgba(1, 2, 3, 4), 1, 2, 3, 4);
        AssertColor(colors.Transparent(), 0, 0, 0, 0);
        AssertColor(colors.Black(), 0, 0, 0, 255);
        AssertColor(colors.White(), 255, 255, 255, 255);
        AssertColor(colors.Lerp(colors.Black(), colors.White(), 0.5), 128, 128, 128, 255);
        AssertColor(colors.Parse("#0A141E"), 10, 20, 30, 255);
        Assert.That(colors.TryParse("invalid"), Is.Null);
        Assert.Throws<System.FormatException>(() => colors.Parse("invalid"));
    }

    [Test]
    public void DataFactories_CreateTypedIndependentValuesAndSidecarPlans()
    {
        ChartDataColumnFactory columns = new ChartDataColumnFactory();
        double[] numbers = new double[] { 1.0, 2.0 };
        string? [] text = new string? [] { "a", null };
        System.DateTime[] dates = new System.DateTime[] { new System.DateTime(2024, 1, 1), new System.DateTime(2024, 1, 2) };
        string? [] categories = new string? [] { "x", "y" };
        ChartDataMatrix matrix = new ChartDataMatrixFactory().FromValues(1, 2, numbers);

        ChartDataColumn numberColumn = columns.CreateNumber("number", numbers);
        ChartDataColumn textColumn = columns.CreateText("text", text);
        ChartDataColumn dateColumn = columns.CreateDateTime("date", dates);
        ChartDataColumn categoryColumn = columns.CreateCategory("category", categories);
        ChartDataColumn matrixColumn = columns.CreateMatrix("matrix", matrix);
        numbers[0] = 99.0;

        Assert.That(numberColumn.GetNumber(0), Is.EqualTo(1.0));
        Assert.That(textColumn.GetText(1), Is.Null);
        Assert.That(dateColumn.GetDateTime(1), Is.EqualTo(dates[1]));
        Assert.That(categoryColumn.GetCategory(0), Is.EqualTo("x"));
        Assert.That(matrixColumn.GetMatrix().GetValue(0, 1), Is.EqualTo(2.0));

        ChartDataTable table = new ChartDataTable();
        table.AddNumberColumn("number", new double[] { 1.0, 2.0 });
        table.AddCategoryColumn("category", new string[] { "x", "y" });
        ChartDataTableSidecarPlan plan = new ChartDataTableSidecarPlanFactory().FromTable("table", "Caption", table);

        Assert.That(plan.GetTableId(), Is.EqualTo("table"));
        Assert.That(plan.GetCaption(), Is.EqualTo("Caption"));
        Assert.That(plan.GetRowCount(), Is.EqualTo(2));
        Assert.That(plan.GetColumnNames(), Is.EqualTo(new string[] { "number", "category" }));
        Assert.That(plan.SummaryText(), Does.Contain("2 rows"));

        Assert.Throws<System.ArgumentNullException>(() => columns.CreateNumber("bad", null!));
        Assert.Throws<System.ArgumentException>(() => new ChartDataMatrixFactory().FromValues(1, 2, new double[] { 1.0 }));
        Assert.Throws<System.ArgumentNullException>(() => new ChartDataTableSidecarPlanFactory().FromTable("table", "Caption", null!));
    }

    [Test]
    public void AxisLayoutLegendTextAndTickFactories_CoverEveryVariant()
    {
        ChartAxisFactory axes = new ChartAxisFactory();
        ChartTick[] ticks = new ChartTick[] { new ChartTick(ChartTickKind.Numeric, 0.0, "0", 0) };
        ChartRange domain = new ChartRange(0.0, 1.0);
        ChartRect plot = new ChartRect(0.0, 0.0, 100.0, 50.0);
        ChartPoint center = new ChartPoint(50.0, 25.0);

        Assert.That(axes.CreateBottom(ticks, domain, plot).GetPlacement(), Is.EqualTo(ChartAxisPlacement.Bottom));
        Assert.That(axes.CreateTop(ticks, domain, plot).GetPlacement(), Is.EqualTo(ChartAxisPlacement.Top));
        Assert.That(axes.CreateLeft(ticks, domain, plot).GetPlacement(), Is.EqualTo(ChartAxisPlacement.Left));
        Assert.That(axes.CreateRight(ticks, domain, plot).GetPlacement(), Is.EqualTo(ChartAxisPlacement.Right));
        Assert.That(axes.CreateRadial(center, 20.0, ticks, domain).GetKind(), Is.EqualTo(ChartAxisKind.Radial));
        Assert.That(axes.CreateAngular(center, 20.0, ticks).GetKind(), Is.EqualTo(ChartAxisKind.Angular));
        Assert.That(axes.CreateTernary(new ChartPoint(0.0, 10.0), new ChartPoint(10.0, 10.0), new ChartPoint(5.0, 0.0), ticks).GetKind(), Is.EqualTo(ChartAxisKind.Ternary));

        ChartLayoutRequestFactory requests = new ChartLayoutRequestFactory();
        Assert.That(requests.CreateLegend("legend", ChartSide.Right, 10.0, 20.0, 2.0).GetKind(), Is.EqualTo(ChartLayoutItemKind.Legend));
        Assert.That(requests.CreateColorbar("colorbar", ChartSide.Left, 10.0, 20.0, 2.0).GetKind(), Is.EqualTo(ChartLayoutItemKind.Colorbar));
        Assert.That(requests.CreateAnnotation("annotation", ChartSide.Top, 10.0, 20.0, 2.0, true).GetOverlay(), Is.True);

        ChartLegendEntryFactory legends = new ChartLegendEntryFactory();
        ChartLegendEntry symbol = legends.CreateSymbol("symbol", "Symbol", ChartMarkerShape.Diamond, new ChartStyle());
        ChartLegendEntry gradient = legends.CreateGradient("gradient", "Gradient", new ChartColor[] { ChartColor.Black(), ChartColor.White() }, new double[] { 0.0, 1.0 });
        ChartLegendEntry custom = legends.CreateCustom("custom", "Custom", "<path/>");
        Assert.That(symbol.GetKind(), Is.EqualTo(ChartLegendEntryKind.Symbol));
        Assert.That(symbol.GetShape(), Is.EqualTo(ChartMarkerShape.Diamond));
        Assert.That(gradient.GetKind(), Is.EqualTo(ChartLegendEntryKind.Gradient));
        Assert.That(gradient.GetGradientColors().Length, Is.EqualTo(2));
        Assert.That(custom.GetCustomMarkup(), Is.EqualTo("<path/>"));

        ChartTextBlock block = new ChartTextBlockFactory().Create("hello");
        Assert.That(block.GetText(), Is.EqualTo("hello"));
        System.DateTime date = new System.DateTime(2024, 2, 3);
        ChartTick time = new ChartTickFactory().CreateTime(date, 4);
        Assert.That(time.GetKind(), Is.EqualTo(ChartTickKind.Time));
        Assert.That(time.GetDateTimeValue(), Is.EqualTo(date));
        Assert.That(time.GetIndex(), Is.EqualTo(4));
    }

    [Test]
    public void EncodingAndVisualMapFactories_CreateAllKinds()
    {
        ChartEncodingBindingFactory bindings = new ChartEncodingBindingFactory();
        ChartEncodingBinding number = bindings.CreateNumberConstant(ChartEncodingChannel.X, 4.0);
        ChartEncodingBinding text = bindings.CreateTextConstant(ChartEncodingChannel.Text, "label");
        ChartEncodingBinding color = bindings.CreateColorConstant(ChartEncodingChannel.Color, ChartColor.Black());
        ChartEncodingBinding shape = bindings.CreateShapeConstant(ChartEncodingChannel.Shape, ChartMarkerShape.Star);

        Assert.That(number.GetIsConstant(), Is.True);
        Assert.That(number.GetNumberValue(), Is.EqualTo(4.0));
        Assert.That(text.GetTextValue(), Is.EqualTo("label"));
        AssertColor(color.GetColorValue(), 0, 0, 0, 255);
        Assert.That(shape.GetShapeValue(), Is.EqualTo(ChartMarkerShape.Star));
        Assert.That(bindings.ChannelName(ChartEncodingChannel.X2), Is.EqualTo("x2"));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => bindings.ChannelName((ChartEncodingChannel)99));

        ChartVisualMapFactory maps = new ChartVisualMapFactory();
        ChartColorMap colorMap = new ChartColorMap("gray", new ChartColor[] { ChartColor.Black(), ChartColor.White() });
        ChartPalette palette = new ChartPalette("two", new ChartColor[] { ChartColor.Black(), ChartColor.White() });
        ChartVisualMap continuous = maps.CreateContinuous(ChartEncodingChannel.Color, 0.0, 10.0, colorMap);
        ChartVisualMap binned = maps.CreateBinned(ChartEncodingChannel.Color, 0.0, 10.0, 2, colorMap);
        ChartVisualMap threshold = maps.CreateThreshold(ChartEncodingChannel.Color, new double[] { 5.0 }, new ChartColor[] { ChartColor.Black(), ChartColor.White() });
        ChartVisualMap categorical = maps.CreateCategorical(ChartEncodingChannel.Color, new string[] { "a", "b" }, palette);

        Assert.That(continuous.GetKind(), Is.EqualTo(ChartVisualMapKind.Continuous));
        AssertColor(continuous.MapNumber(5.0), 128, 128, 128, 255);
        Assert.That(binned.GetKind(), Is.EqualTo(ChartVisualMapKind.Binned));
        Assert.That(binned.GetBinCount(), Is.EqualTo(2));
        Assert.That(threshold.GetKind(), Is.EqualTo(ChartVisualMapKind.Threshold));
        AssertColor(threshold.MapNumber(6.0), 255, 255, 255, 255);
        Assert.That(categorical.GetKind(), Is.EqualTo(ChartVisualMapKind.Categorical));
        AssertColor(categorical.MapCategory("b"), 255, 255, 255, 255);
    }

    [Test]
    public void PathAndRenderFactories_CreateEveryCommandKind()
    {
        ChartPathCommandFactory pathCommands = new ChartPathCommandFactory();
        Assert.That(pathCommands.MoveTo(1.0, 2.0).Kind, Is.EqualTo(ChartPathCommandKind.MoveTo));
        Assert.That(pathCommands.LineTo(1.0, 2.0).Kind, Is.EqualTo(ChartPathCommandKind.LineTo));
        Assert.That(pathCommands.QuadraticBezierTo(1.0, 2.0, 3.0, 4.0).Kind, Is.EqualTo(ChartPathCommandKind.QuadraticBezierTo));
        Assert.That(pathCommands.CubicBezierTo(1.0, 2.0, 3.0, 4.0, 5.0, 6.0).Kind, Is.EqualTo(ChartPathCommandKind.CubicBezierTo));
        Assert.That(pathCommands.Close().Kind, Is.EqualTo(ChartPathCommandKind.Close));

        ChartRenderStyleFactory styles = new ChartRenderStyleFactory();
        ChartRenderStyle fill = styles.CreateFill(ChartColor.White());
        ChartRenderStyle stroke = styles.CreateStroke(ChartColor.Black(), 2.0);
        ChartRenderStyle fillStroke = styles.CreateFillStroke(ChartColor.White(), ChartColor.Black(), 3.0);
        Assert.That(fill.GetHasFill(), Is.True);
        Assert.That(stroke.GetHasStroke(), Is.True);
        Assert.That(fillStroke.GetStrokeWidth(), Is.EqualTo(3.0));

        ChartRenderCommandFactory commands = new ChartRenderCommandFactory();
        ChartPath path = TrianglePath();
        ChartTextBlock block = new ChartTextBlockFactory().Create("text");
        Assert.That(commands.CreateClear(ChartColor.Black()).GetKind(), Is.EqualTo(ChartRenderCommandKind.Clear));
        Assert.That(commands.CreateLine("line", 0.0, 0.0, 1.0, 1.0, stroke).GetKind(), Is.EqualTo(ChartRenderCommandKind.Line));
        Assert.That(commands.CreatePolyline("polyline", new ChartPoint[] { new ChartPoint(0.0, 0.0), new ChartPoint(1.0, 1.0) }, stroke).GetKind(), Is.EqualTo(ChartRenderCommandKind.Polyline));
        Assert.That(commands.CreatePolygon("polygon", new ChartPoint[] { new ChartPoint(0.0, 0.0), new ChartPoint(1.0, 0.0), new ChartPoint(0.0, 1.0) }, fill).GetKind(), Is.EqualTo(ChartRenderCommandKind.Polygon));
        Assert.That(commands.CreateRect("rect", 0.0, 0.0, 2.0, 3.0, 0.0, 0.0, fill).GetKind(), Is.EqualTo(ChartRenderCommandKind.Rect));
        Assert.That(commands.CreateCircle("circle", 0.0, 0.0, 2.0, fill).GetKind(), Is.EqualTo(ChartRenderCommandKind.Circle));
        Assert.That(commands.CreateEllipse("ellipse", 0.0, 0.0, 2.0, 3.0, fill).GetKind(), Is.EqualTo(ChartRenderCommandKind.Ellipse));
        Assert.That(commands.CreatePath("path", path, fill).GetKind(), Is.EqualTo(ChartRenderCommandKind.Path));
        Assert.That(commands.CreateText("text", "text", 1.0, 2.0, fill).GetKind(), Is.EqualTo(ChartRenderCommandKind.Text));
        Assert.That(commands.CreateTextBlock("block", block, 1.0, 2.0).GetHasTextBlock(), Is.True);
        Assert.That(commands.CreateMarker("marker", ChartMarkerShape.Circle, 1.0, 2.0, 3.0, fill).GetMarkerShape(), Is.EqualTo(ChartMarkerShape.Circle));
        Assert.That(commands.CreateCustomMarker("custom", path, 1.0, 2.0, 3.0, fill).GetMarkerShape(), Is.EqualTo(ChartMarkerShape.CustomPath));
    }

    [Test]
    public void ThemePaletteAndBitmapFactories_ReturnIndependentFunctionalObjects()
    {
        ChartThemeFactory themes = new ChartThemeFactory();
        Assert.That(themes.LibraryDefaultStyle().GetHasFillColor(), Is.True);
        Assert.That(themes.LibraryDefault().GetName(), Is.EqualTo("library-default"));
        Assert.That(themes.Light().GetName(), Is.EqualTo("light"));
        Assert.That(themes.Dark().GetName(), Is.EqualTo("dark"));
        Assert.That(themes.Print().GetName(), Is.EqualTo("print"));
        Assert.That(themes.Monochrome().GetName(), Is.EqualTo("monochrome"));
        Assert.That(themes.HighContrast().GetName(), Is.EqualTo("high-contrast"));
        Assert.That(themes.ColorblindSafe().GetName(), Is.EqualTo("colorblind-safe"));

        ChartPalettes palettes = new ChartPalettes();
        Assert.That(palettes.Categorical().Count(), Is.EqualTo(10));
        Assert.That(palettes.ColorblindSafe().Count(), Is.EqualTo(8));
        Assert.That(palettes.HighContrast().Count(), Is.EqualTo(8));
        Assert.That(palettes.Grayscale().Count(), Is.EqualTo(5));
        Assert.That(palettes.Sequential().Count(), Is.EqualTo(5));
        Assert.That(palettes.Diverging().Count(), Is.EqualTo(5));
        Assert.That(palettes.GrayscaleMap().Count(), Is.EqualTo(2));
        Assert.That(palettes.ColorblindSafeMap().Count(), Is.EqualTo(3));
        Assert.That(palettes.HighContrastMap().Count(), Is.EqualTo(3));

        RawBitmapFactory bitmaps = new RawBitmapFactory();
        RawBitmap allocated = bitmaps.CreateWithStride(2, 1, 8, ChartPixelFormat.Rgba32);
        byte[] buffer = new byte[8];
        RawBitmap wrapped = bitmaps.Wrap(2, 1, 8, ChartPixelFormat.Rgba32, buffer);
        wrapped.SetPixel(0, 0, ChartColor.White());
        RawBitmap converted = bitmaps.Convert(wrapped, ChartPixelFormat.Rgb24);
        RawBitmap indexed = bitmaps.ConvertToIndexed(wrapped, new ChartColor[] { ChartColor.Black(), ChartColor.White() });

        Assert.That(allocated.GetStride(), Is.EqualTo(8));
        Assert.That(buffer[0], Is.EqualTo(255));
        Assert.That(converted.GetPixelFormat(), Is.EqualTo(ChartPixelFormat.Rgb24));
        Assert.That(indexed.GetPaletteIndex(0, 0), Is.EqualTo(1));
        Assert.That(bitmaps.CalculateStride(2, ChartPixelFormat.Rgb24), Is.EqualTo(6));
        Assert.That(bitmaps.BytesPerPixel(ChartPixelFormat.Rgba32), Is.EqualTo(4));
        Assert.Throws<System.ArgumentNullException>(() => bitmaps.Convert(null!, ChartPixelFormat.Rgba32));
    }

    [Test]
    public void CartesianFactories_CreateMappedPointsAndTransformsFromAxes()
    {
        ChartCartesianSeriesFactory series = new ChartCartesianSeriesFactory();
        ChartPoint[] points = series.CreateDataPoints(new double[] { 1.0, 2.0 }, new double[] { 3.0, 4.0 });
        AssertPoint(points[0], 1.0, 3.0);
        AssertPoint(points[1], 2.0, 4.0);
        Assert.Throws<System.ArgumentException>(() => series.CreateDataPoints(new double[] { 1.0 }, new double[] { 2.0, 3.0 }));

        ChartAxisFactory axes = new ChartAxisFactory();
        ChartTick[] ticks = new ChartTick[0];
        ChartRect plot = new ChartRect(10.0, 20.0, 100.0, 50.0);
        ChartAxis xAxis = axes.CreateBottom(ticks, new ChartRange(0.0, 10.0), plot);
        ChartAxis yAxis = axes.CreateLeft(ticks, new ChartRange(0.0, 5.0), plot);
        ChartCartesianTransform transform = new ChartCartesianTransformFactory().FromAxes(xAxis, yAxis);

        AssertPoint(transform.MapPoint(5.0, 2.5), 60.0, 45.0);
        Assert.Throws<System.ArgumentNullException>(() => new ChartCartesianTransformFactory().FromAxes(null!, yAxis));
    }

    static ChartPath TrianglePath()
    {
        ChartPath path = new ChartPath();
        path.MoveTo(0.0, 0.0);
        path.LineTo(1.0, 0.0);
        path.LineTo(0.0, 1.0);
        path.Close();
        return path;
    }

    static void AssertPoint(ChartPoint point, double x, double y)
    {
        Assert.That(point.GetX(), Is.EqualTo(x).Within(0.0000001));
        Assert.That(point.GetY(), Is.EqualTo(y).Within(0.0000001));
    }

    static void AssertColor(ChartColor color, int red, int green, int blue, int alpha)
    {
        Assert.That(color.GetR(), Is.EqualTo((byte)red));
        Assert.That(color.GetG(), Is.EqualTo((byte)green));
        Assert.That(color.GetB(), Is.EqualTo((byte)blue));
        Assert.That(color.GetA(), Is.EqualTo((byte)alpha));
    }
}
