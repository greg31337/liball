# chart

`chart` is a small .NET charting library that builds retained render scenes and exports them as raw bitmaps, BMP, PNG, or SVG. Public operations are exposed through interfaces and implementation classes, while record-like models use public data fields.

## Quickstart

Start with a `ChartDocument`, render a scene once, then choose the export format that matches the caller.

```csharp
ChartDocument document = new ChartDocument(640.0, 360.0);
document.SetTheme(new ChartThemeFactory().ColorblindSafe());

ChartPlot plot = document.AddPlotWithBounds("main", new ChartRect(48.0, 24.0, 560.0, 300.0));
plot.SetMargins(new ChartPadding(0.0, 0.0, 0.0, 0.0));

ChartCartesianSeries quickLine = new ChartLineAreaCharts().CreateLineSeries(
    "quick-line",
    new double[] { 0.0, 1.0, 2.0, 3.0 },
    new double[] { 12.0, 18.0, 15.0, 24.0 },
    new ChartRenderStyleFactory().CreateStroke(new ChartColorFactory().FromRgb(36, 96, 160), 2.0));

ChartRenderScene quickScene = document.RenderCartesian(
    "main",
    new ChartCartesianSeries[] { quickLine },
    new ChartRange(0.0, 3.0),
    new ChartRange(0.0, 30.0));

RawBitmap quickBitmap = quickScene.ToRawBitmap();
byte[] quickBmp = new ChartExport().ToBmpBytes(quickBitmap);
byte[] quickPng = new ChartExport().ToPngBytes(quickBitmap);
string quickSvg = quickScene.ToSvgString();
```

Declarative encodings let data tables describe fields once and then create chart series from channel mappings.

```csharp
ChartDataTable table = new ChartDataTable();
table.AddNumberColumn("x", new double[] { 0.0, 1.0, 2.0, 3.0 });
table.AddNumberColumn("y", new double[] { 12.0, 18.0, 15.0, 24.0 });

ChartVisualEncoding encoding = new ChartVisualEncoding();
encoding.SetX("x", ChartDataKind.Number);
encoding.SetY("y", ChartDataKind.Number);

ChartCartesianSeries declarativeLine = new ChartDeclarativeCharts().CreateSeries(
    "quick-declarative",
    ChartDeclarativeMark.Line,
    table,
    encoding,
    new ChartRenderStyleFactory().CreateStroke(new ChartColorFactory().Black(), 2.0));
```

Themes and document styles can be set before rendering so every plot uses the same defaults.

```csharp
ChartDocument themedDocument = new ChartDocument(640.0, 360.0);
themedDocument.SetTheme(new ChartThemeFactory().ColorblindSafe());
themedDocument.GetStyle().SetFont("Inter", 12);
themedDocument.GetStyle().SetOpacity(0.95);
```

## Interface Extension Points

Scene-oriented builders accept `IChartRenderTarget`, while `ChartRenderScene` remains the built-in retained implementation. This lets applications record drawing operations for tests or adapt them to another target without changing chart construction code. Custom renderers implement the single-method `IChartRenderer` contract and can use the document's size and background initialization through `ChartDocument.Render`.

```csharp
public class CustomRenderer : IChartRenderer
{
    public void RenderTo(IChartRenderTarget target)
    {
        target.AddLine("custom", 0.0, 0.0, target.GetWidth(), target.GetHeight(), new ChartRenderStyleFactory().CreateStroke(new ChartColorFactory().Black(), 1.0));
    }
}

ChartDocument customDocument = new ChartDocument(320.0, 180.0);
ChartRenderScene customScene = customDocument.Render(new CustomRenderer());
```

Axis label formatting is also replaceable without changing tick generation. Implement `IChartTickFormatter` and assign it with `ChartAxis.SetTickFormatter`; `ChartDefaultTickFormatter` preserves the library's standard deterministic formatting.

```csharp
public class CurrencyTickFormatter : IChartTickFormatter
{
    public string FormatTick(ChartTick tick)
    {
        return "$" + new ChartTickFormatter().FormatNumber(tick.GetValue(), 2);
    }
}

ChartAxis currencyAxis = new ChartAxisFactory().CreateLeft(ticks, domain, plotArea);
currencyAxis.SetTickFormatter(new CurrencyTickFormatter());
```

Subplots and faceted views are created with grid plots or small multiples, then laid out before rendering.

```csharp
ChartDocument facetDocument = new ChartDocument(800.0, 480.0);
facetDocument.SetMargins(new ChartPadding(16.0, 16.0, 16.0, 16.0));
facetDocument.CreateSmallMultiples("facet", 2, 2);
facetDocument.GetGrid().SetGaps(20.0, 24.0);

ChartPlot summaryPlot = facetDocument.AddGridPlot("summary", 0, 0, 1, 2);
ChartLayoutResult layout = new ChartLayoutEngine().Layout(facetDocument);
ChartRect firstFacet = layout.ItemById("facet-0").GetBounds();
```

## Compatibility Notes

The production library targets `net10.0` in `src/chart.csproj` and has no `PackageReference` or `FrameworkReference` entries. Runtime rendering and export are implemented in this repository using only built-in .NET APIs:

| Namespace | APIs used |
| --- | --- |
| `System` | exceptions, `Array.Copy`, `Array.Sort`, `DateTime`, `TimeSpan`, `StringComparison.Ordinal`, `String.CompareOrdinal`, and `Math` |
| `System.Collections.Generic` | `List<T>` for internal layout and render collections |
| `System.Globalization` | `CultureInfo.InvariantCulture` for deterministic numeric text |
| `System.IO` | `File.WriteAllBytes`, `MemoryStream`, and `Stream` for optional export helpers |
| `System.IO.Compression` | `DeflateStream` with `CompressionLevel.Optimal` for PNG DEFLATE data |
| `System.Text` | `Encoding.ASCII`, `Encoding.UTF8`, and `StringBuilder` for encoder bytes and SVG markup |

There is no dependency on `System.Drawing`, SkiaSharp, ImageSharp, Cairo, browser engines, native codecs, platform-specific drawing stacks, or any external runtime library.

## Minimal Cartesian Example

```csharp
ChartDocument document = new ChartDocument(640.0, 360.0);
ChartPlot plot = document.AddPlotWithBounds("main", new ChartRect(48.0, 24.0, 560.0, 300.0));
plot.SetMargins(new ChartPadding(0.0, 0.0, 0.0, 0.0));

ChartCartesianSeries line = new ChartLineAreaCharts().CreateLineSeries(
    "revenue",
    new double[] { 0.0, 1.0, 2.0, 3.0 },
    new double[] { 12.0, 18.0, 15.0, 24.0 },
    new ChartRenderStyleFactory().CreateStroke(new ChartColorFactory().Black(), 2.0));

ChartRenderScene scene = document.RenderCartesian(
    "main",
    new ChartCartesianSeries[] { line },
    new ChartRange(0.0, 3.0),
    new ChartRange(0.0, 30.0));

string svg = document.ExportSceneSvg(scene);
byte[] png = document.ExportScenePng(scene);
```

## Output Formats

All required output formats are available from a rendered `ChartRenderScene` or `RawBitmap`.

| Format | Enum | Example |
| --- | --- | --- |
| Raw bitmap | `ChartOutputFormat.RawBitmap` | `RawBitmap bitmap = scene.ToRawBitmap();` |
| BMP | `ChartOutputFormat.Bmp` | `byte[] bmp = new ChartExport().ToBmpBytes(scene.ToRawBitmap());` |
| PNG | `ChartOutputFormat.Png` | `byte[] png = new ChartExport().ToPngBytes(scene.ToRawBitmap());` |
| SVG | `ChartOutputFormat.Svg` | `string svg = scene.ToSvgString();` |

File helpers are also available:

```csharp
RawBitmap bitmap = scene.ToRawBitmap();
new ChartExport().SaveBmp(bitmap, "chart.bmp");
new ChartExport().SavePng(bitmap, "chart.png");
new ChartExport().SaveSvg(scene.ToSvgString(), "chart.svg");
```

## Chart Type Examples

The supported chart types are represented by `ChartSeriesKind`. Most Cartesian chart builders return `ChartCartesianSeries`; scene-oriented chart builders add commands directly to `ChartRenderScene`.

| Chart type | Example API |
| --- | --- |
| `ChartSeriesKind.Line` | `new ChartLineAreaCharts().CreateLineSeries("line", xValues, yValues, lineStyle)` |
| `ChartSeriesKind.StepLine` | `new ChartLineAreaCharts().CreateStepLineSeries("step", xValues, yValues, ChartLineInterpolation.StepAfter, lineStyle)` |
| `ChartSeriesKind.SplineLine` | `new ChartLineAreaCharts().CreateSplineLineSeries("spline", xValues, yValues, 8, lineStyle)` |
| `ChartSeriesKind.Area` | `new ChartLineAreaCharts().CreateAreaSeries("area", xValues, upperValues, lowerValues, fillStyle)` |
| `ChartSeriesKind.Scatter` | `new ChartScatterCharts().CreateScatterSeries("scatter", xValues, yValues, ChartMarkerShape.Circle, 6.0, fillStyle)` |
| `ChartSeriesKind.Bubble` | `new ChartScatterCharts().CreateBubbleSeries("bubble", xValues, yValues, sizeValues, 4.0, 18.0, fillStyle)` |
| `ChartSeriesKind.Bar` | `new ChartBarCharts().CreateVerticalBarSeries("bar", xValues, yValues, 0.8, 0.0, fillStyle)` |
| `ChartSeriesKind.Histogram` | `new ChartStatisticalCharts().CreateHistogramSeries("hist", values, 12, fillStyle)` |
| `ChartSeriesKind.BoxPlot` | `new ChartStatisticalCharts().CreateBoxPlotSeries("box", 1.0, 0.5, values, boxStyle, lineStyle)` |
| `ChartSeriesKind.Heatmap` | `new ChartGridCharts().CreateHeatmapSeries("heat", xCenters, yCenters, matrix, 1.0, 1.0, visualMap)` |
| `ChartSeriesKind.Contour` | `new ChartGridCharts().CreateContourLineSeries("contour", xCenters, yCenters, matrix, levels, lineStyle)` |
| `ChartSeriesKind.Pie` | `new ChartPieCharts().CreatePieScene("pie", 400, 400, new ChartPieCharts().LayoutPie(labels, values, 200.0, 200.0, 120.0, 0.0, ChartPieSliceOrder.Original, palette))` |
| `ChartSeriesKind.Donut` | `new ChartPieCharts().LayoutDonut(labels, values, 200.0, 200.0, 60.0, 120.0, 0.0, ChartPieSliceOrder.Original, palette)` |
| `ChartSeriesKind.PolarLine` | `new ChartPolarCharts().AddPolarLine(scene, "polar", angleValues, radiusValues, transform, false, lineStyle)` |
| `ChartSeriesKind.PolarScatter` | `new ChartPolarCharts().AddPolarScatter(scene, "polar", angleValues, radiusValues, transform, ChartMarkerShape.Circle, 6.0, fillStyle)` |
| `ChartSeriesKind.Radar` | `new ChartPolarCharts().AddRadar(scene, "radar", values, transform, fillStyle)` |
| `ChartSeriesKind.Gauge` | `new ChartPolarCharts().CreateGaugeScene("gauge", 320, 220, 160.0, 160.0, 80.0, 120.0, 0.0, 100.0, 72.0, -90.0, 90.0, trackStyle, valueStyle, needleStyle)` |
| `ChartSeriesKind.Candlestick` | `new ChartFinancialCharts().CreateCandlestickSeries("candle", xValues, openValues, highValues, lowValues, closeValues, 0.6, upStyle, downStyle, wickStyle)` |
| `ChartSeriesKind.Ohlc` | `new ChartFinancialCharts().CreateOhlcSeries("ohlc", xValues, openValues, highValues, lowValues, closeValues, 0.6, upStroke, downStroke)` |
| `ChartSeriesKind.Treemap` | `new ChartHierarchyCharts().AddRectLayoutToScene(scene, "tree", new ChartHierarchyCharts().LayoutTreemap(data, bounds, palette))` |
| `ChartSeriesKind.Sunburst` | `new ChartHierarchyCharts().AddSunburstToScene(scene, "sun", new ChartHierarchyCharts().LayoutSunburst(data, 200.0, 200.0, 20.0, 160.0, 0.0, 360.0, palette))` |
| `ChartSeriesKind.Icicle` | `new ChartHierarchyCharts().AddRectLayoutToScene(scene, "icicle", new ChartHierarchyCharts().LayoutIcicle(data, bounds, palette))` |
| `ChartSeriesKind.Sankey` | `new ChartRelationshipCharts().LayoutSankey(labels, links, bounds, 12.0, 6.0)` |
| `ChartSeriesKind.NodeLink` | `new ChartRelationshipCharts().LayoutNodeLink(labels, links, bounds)` |
| `ChartSeriesKind.Tree` | `new ChartRelationshipCharts().LayoutTree(data, bounds, ChartOrientation.Horizontal)` |
| `ChartSeriesKind.Dendrogram` | `new ChartRelationshipCharts().LayoutDendrogram(data, bounds, ChartOrientation.Horizontal)` |
| `ChartSeriesKind.ParallelCoordinates` | `new ChartRelationshipCharts().LayoutParallelCoordinates(axisLabels, matrix, bounds)` |
| `ChartSeriesKind.Slope` | `new ChartCompactCharts().CreateSlopeSeries("slope", labels, startValues, endValues, 0.0, 1.0, palette, 1.0)` |
| `ChartSeriesKind.Dumbbell` | `new ChartCompactCharts().CreateDumbbellSeries("dumbbell", yValues, startValues, endValues, 6.0, lineStyle, startStyle, endStyle)` |
| `ChartSeriesKind.Bump` | `new ChartCompactCharts().CreateBumpSeries("bump", labels, xValues, ranks, palette, 1.0)` |
| `ChartSeriesKind.Table` | `new ChartCompactCharts().AddTableToScene(scene, "table", new ChartCompactCharts().LayoutTable(headers, values, rowCount, columnCount, bounds, 20.0), cellStyle, textStyle)` |
| `ChartSeriesKind.Sparkline` | `new ChartCompactCharts().CreateSparklineScene("spark", 160, 40, values, lineStyle)` |

### Cartesian Builders

```csharp
ChartCartesianTransform transform = new ChartCartesianTransform(
    new ChartRect(40.0, 20.0, 520.0, 300.0),
    new ChartRange(0.0, 10.0),
    new ChartRange(0.0, 100.0));
ChartCartesianRenderer renderer = new ChartCartesianRenderer(transform);

renderer.AddSeries(new ChartLineAreaCharts().CreateLineSeries("line", xValues, yValues, lineStyle));
renderer.AddSeries(new ChartLineAreaCharts().CreateAreaSeries("area", xValues, upperValues, lowerValues, areaStyle));
renderer.AddSeries(new ChartScatterCharts().CreateScatterSeries("scatter", xValues, yValues, ChartMarkerShape.Diamond, 7.0, pointStyle));
renderer.AddSeries(new ChartBarCharts().CreateVerticalBarSeries("bar", xValues, yValues, 0.7, 0.0, barStyle));
renderer.AddSeries(new ChartGridCharts().CreateHeatmapSeries("heat", xCenters, yCenters, matrix, 1.0, 1.0, visualMap));

ChartRenderScene scene = renderer.Render(640, 360);
```

### Statistical Charts

```csharp
renderer.AddSeries(new ChartStatisticalCharts().CreateHistogramSeries("hist", values, 20, fillStyle));
renderer.AddSeries(new ChartStatisticalCharts().CreateDensitySeries("density", values, 100, lineStyle));
renderer.AddSeries(new ChartStatisticalCharts().CreateEcdfSeries("ecdf", values, lineStyle));
renderer.AddSeries(new ChartStatisticalCharts().CreateBoxPlotSeries("box", 1.0, 0.5, values, boxStyle, lineStyle));
renderer.AddSeries(new ChartStatisticalCharts().CreateViolinSeries("violin", 2.0, 0.8, values, 80, 0.4, fillStyle));
renderer.AddSeries(new ChartStatisticalCharts().CreateErrorBarSeries("error", xValues, yValues, lowerErrors, upperErrors, 0.2, lineStyle));
renderer.AddSeries(new ChartStatisticalCharts().CreateStemSeries("stem", xValues, yValues, 0.0, lineStyle));
```

### Pie, Donut, And Rose Charts

```csharp
ChartPieLayout pie = new ChartPieCharts().LayoutPie(labels, values, 200.0, 200.0, 120.0, 0.0, ChartPieSliceOrder.Original, palette);
ChartPieLayout donut = new ChartPieCharts().LayoutDonut(labels, values, 200.0, 200.0, 60.0, 120.0, 0.0, ChartPieSliceOrder.Original, palette);
ChartPieLayout rose = new ChartPieCharts().LayoutRose(labels, values, 200.0, 200.0, 40.0, 120.0, 0.0, ChartPieSliceOrder.Descending, palette);

ChartRenderScene scene = new ChartPieCharts().CreatePieScene("pie", 400, 400, pie);
new ChartPieCharts().AddLabelsAndLeaderLines(scene, "pie", pie, 150.0, labelStyle, leaderStyle);
```

### Polar Charts

```csharp
ChartRenderScene scene = new ChartRenderScene(400, 400);
ChartPolarTransform transform = new ChartPolarTransform(200.0, 200.0, 20.0, 160.0, 0.0, 100.0, 0.0, true);

new ChartPolarCharts().AddPolarLine(scene, "line", angleValues, radiusValues, transform, false, lineStyle);
new ChartPolarCharts().AddPolarScatter(scene, "points", angleValues, radiusValues, transform, ChartMarkerShape.Circle, 6.0, pointStyle);
new ChartPolarCharts().AddPolarBars(scene, "bars", angleValues, radiusValues, transform, 12.0, 0.0, fillStyle);
new ChartPolarCharts().AddRadarSpokes(scene, "radar", 5, transform, 100.0, lineStyle);
new ChartPolarCharts().AddRadar(scene, "radar", values, transform, fillStyle);
new ChartPolarCharts().AddGauge(scene, "gauge", 200.0, 200.0, 90.0, 120.0, 0.0, 100.0, 75.0, -90.0, 90.0, trackStyle, valueStyle, needleStyle);
new ChartPolarCharts().AddBullet(scene, "bullet", 20.0, 340.0, 180.0, 20.0, 0.0, 100.0, 75.0, 90.0, ChartOrientation.Horizontal, trackStyle, valueStyle, needleStyle);
```

### Financial Charts

```csharp
renderer.AddSeries(new ChartFinancialCharts().CreateCandlestickSeries("candles", xValues, openValues, highValues, lowValues, closeValues, 0.6, upStyle, downStyle, wickStyle));
renderer.AddSeries(new ChartFinancialCharts().CreateOhlcSeries("ohlc", xValues, openValues, highValues, lowValues, closeValues, 0.6, upStroke, downStroke));
renderer.AddSeries(new ChartFinancialCharts().CreateVolumeSeries("volume", xValues, volumeValues, 0.8, 0.0, volumeStyle));
renderer.AddSeries(new ChartFinancialCharts().CreateMovingAverageSeries("ma", xValues, closeValues, 20, lineStyle));
```

### Hierarchy And Relationship Charts

```csharp
ChartHierarchyData data = new ChartHierarchyData(nodes);
ChartRenderScene scene = new ChartRenderScene(640, 480);

new ChartHierarchyCharts().AddRectLayoutToScene(scene, "treemap", new ChartHierarchyCharts().LayoutTreemap(data, new ChartRect(0.0, 0.0, 300.0, 220.0), palette));
new ChartHierarchyCharts().AddRectLayoutToScene(scene, "icicle", new ChartHierarchyCharts().LayoutIcicle(data, new ChartRect(0.0, 240.0, 300.0, 220.0), palette));
new ChartHierarchyCharts().AddSunburstToScene(scene, "sunburst", new ChartHierarchyCharts().LayoutSunburst(data, 480.0, 240.0, 30.0, 180.0, 0.0, 360.0, palette));

ChartGraphLayout sankey = new ChartRelationshipCharts().LayoutSankey(labels, links, new ChartRect(0.0, 0.0, 640.0, 260.0), 16.0, 8.0);
ChartGraphLayout nodeLink = new ChartRelationshipCharts().LayoutNodeLink(labels, links, new ChartRect(0.0, 0.0, 300.0, 300.0));
ChartGraphLayout tree = new ChartRelationshipCharts().LayoutTree(data, new ChartRect(0.0, 0.0, 300.0, 240.0), ChartOrientation.Horizontal);
ChartGraphLayout dendrogram = new ChartRelationshipCharts().LayoutDendrogram(data, new ChartRect(0.0, 0.0, 300.0, 240.0), ChartOrientation.Horizontal);
ChartParallelCoordinatesLayout parallel = new ChartRelationshipCharts().LayoutParallelCoordinates(axisLabels, matrix, new ChartRect(0.0, 0.0, 640.0, 300.0));
ChartParallelCategoriesLayout categories = new ChartRelationshipCharts().LayoutParallelCategories(axisLabels, categoryValues, rowCount, axisCount, new ChartRect(0.0, 0.0, 640.0, 300.0));
```

### Compact Charts, Tables, And Sparklines

```csharp
renderer.AddSeries(new ChartCompactCharts().CreateSlopeSeries("slope", labels, startValues, endValues, 0.0, 1.0, palette, 1.0));
renderer.AddSeries(new ChartCompactCharts().CreateDumbbellSeries("dumbbell", yValues, startValues, endValues, 6.0, lineStyle, startStyle, endStyle));

ChartCartesianSeries[] bump = new ChartCompactCharts().CreateBumpSeries("bump", labels, xValues, ranks, palette, 1.0);
ChartTableLayout table = new ChartCompactCharts().LayoutTable(headers, values, rowCount, columnCount, bounds, 20.0);
new ChartCompactCharts().AddTableToScene(scene, "table", table, cellStyle, textStyle);
ChartRenderScene sparkline = new ChartCompactCharts().CreateSparklineScene("spark", 160, 40, values, lineStyle);
```

### Declarative Encodings

```csharp
ChartDataTable table = new ChartDataTable();
table.AddNumberColumn("x", xValues);
table.AddNumberColumn("y", yValues);
table.AddNumberColumn("size", sizeValues);

ChartVisualEncoding encoding = new ChartVisualEncoding();
encoding.SetX("x", ChartDataKind.Number);
encoding.SetY("y", ChartDataKind.Number);
encoding.SetSize("size", ChartDataKind.Number);
encoding.SetConstantShape(ChartEncodingChannel.Shape, ChartMarkerShape.Circle);

ChartCartesianSeries scatter = new ChartDeclarativeCharts().CreateSeries(
    "declarative",
    ChartDeclarativeMark.Bubble,
    table,
    encoding,
    new ChartRenderStyleFactory().CreateFill(new ChartColorFactory().FromRgb(40, 90, 200)));
```

### Overlays, Annotations, And Labels

```csharp
renderer.AddSeries(new ChartOverlayCharts().CreateTrendlineSeries("trend", xValues, yValues, 0.0, 10.0, lineStyle));
renderer.AddSeries(new ChartOverlayCharts().CreateConfidenceBand("confidence", xValues, lowerValues, upperValues, fillStyle));
renderer.AddSeries(new ChartOverlayCharts().CreateMeanLine("mean", 0.0, 10.0, values, lineStyle));

new ChartAnnotationCharts().AddArrow(scene, "arrow", 20.0, 20.0, 80.0, 60.0, 10.0, 6.0, lineStyle);
new ChartAnnotationCharts().AddCallout(scene, "callout", "Peak", new ChartRect(90.0, 20.0, 60.0, 28.0), 80.0, 60.0, 4.0, boxStyle, leaderStyle, textStyle);

ChartPlacedLabel[] labels = new ChartLabelPlacementCharts().LayoutPointLabels("label", labelTexts, xValues, yValues, 32.0, 12.0, 4.0, ChartLabelPosition.Right, true);
renderer.AddSeries(new ChartLabelPlacementCharts().CreateLabelSeries("labels", labels, textStyle));
```
