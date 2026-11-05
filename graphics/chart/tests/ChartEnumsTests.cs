namespace chart.Tests;

[TestFixture]
public class ChartEnumsTests
{
    [Test]
    public void OutputAndRenderEnums_HaveStableWireValues()
    {
        Assert.That((int)ChartOutputFormat.RawBitmap, Is.EqualTo(0));
        Assert.That((int)ChartOutputFormat.Bmp, Is.EqualTo(1));
        Assert.That((int)ChartOutputFormat.Png, Is.EqualTo(2));
        Assert.That((int)ChartOutputFormat.Svg, Is.EqualTo(3));

        Assert.That((int)ChartRenderBackend.Raster, Is.EqualTo(0));
        Assert.That((int)ChartRenderBackend.Svg, Is.EqualTo(1));
        Assert.That((int)ChartRenderQuality.Aliased, Is.EqualTo(0));
        Assert.That((int)ChartRenderQuality.Antialiased, Is.EqualTo(1));
    }

    [Test]
    public void RenderEnums_CoverRasterAndVectorPrimitiveModes()
    {
        Assert.That(Enum.GetNames(typeof(ChartPixelFormat)), Does.Contain("Rgba32"));
        Assert.That(Enum.GetNames(typeof(ChartPixelFormat)), Does.Contain("Bgra32"));
        Assert.That(Enum.GetNames(typeof(ChartPixelFormat)), Does.Contain("Rgb24"));
        Assert.That(Enum.GetNames(typeof(ChartPixelFormat)), Does.Contain("Grayscale8"));
        Assert.That(Enum.GetNames(typeof(ChartPixelFormat)), Does.Contain("Alpha8"));
        Assert.That(Enum.GetNames(typeof(ChartPixelFormat)), Does.Contain("Indexed8"));

        Assert.That(Enum.GetNames(typeof(ChartBlendMode)), Does.Contain("SourceOver"));
        Assert.That(Enum.GetNames(typeof(ChartBlendMode)), Does.Contain("Source"));
        Assert.That(Enum.GetNames(typeof(ChartBlendMode)), Does.Contain("Multiply"));
        Assert.That(Enum.GetNames(typeof(ChartBlendMode)), Does.Contain("Screen"));
        Assert.That(Enum.GetNames(typeof(ChartBlendMode)), Does.Contain("Darken"));
        Assert.That(Enum.GetNames(typeof(ChartBlendMode)), Does.Contain("Lighten"));

        Assert.That(Enum.GetNames(typeof(ChartLineCap)), Is.EquivalentTo(new string[] { "Butt", "Square", "Round" }));
        Assert.That(Enum.GetNames(typeof(ChartLineJoin)), Is.EquivalentTo(new string[] { "Miter", "Bevel", "Round" }));
        Assert.That(Enum.GetNames(typeof(ChartFillRule)), Is.EquivalentTo(new string[] { "Nonzero", "EvenOdd" }));
    }

    [Test]
    public void LayoutEnums_CoverPositioningAndTextModes()
    {
        Assert.That(Enum.GetNames(typeof(ChartTextAlignment)), Is.EquivalentTo(new string[] { "Left", "Center", "Right", "Start", "Middle", "End" }));
        Assert.That(Enum.GetNames(typeof(ChartVerticalTextAlignment)), Is.EquivalentTo(new string[] { "Top", "Middle", "Baseline", "Bottom" }));
        Assert.That(Enum.GetNames(typeof(ChartOrientation)), Is.EquivalentTo(new string[] { "Horizontal", "Vertical", "Radial" }));
        Assert.That(Enum.GetNames(typeof(ChartSide)), Is.EquivalentTo(new string[] { "Left", "Right", "Top", "Bottom" }));
        Assert.That(Enum.GetNames(typeof(ChartDirection)), Is.EquivalentTo(new string[] { "Normal", "Reversed" }));
        Assert.That(Enum.GetNames(typeof(ChartCoordinateUnit)), Is.EquivalentTo(new string[] { "Pixel", "Plot", "Paper", "Data", "Axis", "Fraction" }));
    }

    [Test]
    public void ScaleAxisAndSeriesEnums_CoverPlannedChartFamilies()
    {
        Assert.That(Enum.GetNames(typeof(ChartScaleKind)), Does.Contain("Linear"));
        Assert.That(Enum.GetNames(typeof(ChartScaleKind)), Does.Contain("Log"));
        Assert.That(Enum.GetNames(typeof(ChartScaleKind)), Does.Contain("Time"));
        Assert.That(Enum.GetNames(typeof(ChartScaleKind)), Does.Contain("CategoryBand"));
        Assert.That(Enum.GetNames(typeof(ChartScaleKind)), Does.Contain("Threshold"));
        Assert.That(Enum.GetNames(typeof(ChartScaleKind)), Does.Contain("Color"));

        Assert.That(Enum.GetNames(typeof(ChartAxisKind)), Is.EquivalentTo(new string[] { "X", "Y", "Radial", "Angular", "Ternary" }));
        Assert.That(Enum.GetNames(typeof(ChartAxisPlacement)), Does.Contain("Bottom"));
        Assert.That(Enum.GetNames(typeof(ChartAxisPlacement)), Does.Contain("Top"));
        Assert.That(Enum.GetNames(typeof(ChartAxisPlacement)), Does.Contain("Left"));
        Assert.That(Enum.GetNames(typeof(ChartAxisPlacement)), Does.Contain("Right"));
        Assert.That(Enum.GetNames(typeof(ChartTickKind)), Does.Contain("Automatic"));
        Assert.That(Enum.GetNames(typeof(ChartTickKind)), Does.Contain("Explicit"));

        Assert.That(Enum.GetNames(typeof(ChartSeriesKind)), Does.Contain("Line"));
        Assert.That(Enum.GetNames(typeof(ChartSeriesKind)), Does.Contain("Scatter"));
        Assert.That(Enum.GetNames(typeof(ChartSeriesKind)), Does.Contain("Bar"));
        Assert.That(Enum.GetNames(typeof(ChartSeriesKind)), Does.Contain("Heatmap"));
        Assert.That(Enum.GetNames(typeof(ChartSeriesKind)), Does.Contain("Pie"));
        Assert.That(Enum.GetNames(typeof(ChartSeriesKind)), Does.Contain("Candlestick"));
        Assert.That(Enum.GetNames(typeof(ChartSeriesKind)), Does.Contain("Treemap"));
        Assert.That(Enum.GetNames(typeof(ChartSeriesKind)), Does.Contain("Sankey"));
    }

    [Test]
    public void DataPolicyEnums_CoverMissingSortingStackingAndAggregation()
    {
        Assert.That(Enum.GetNames(typeof(ChartDataKind)), Is.EquivalentTo(new string[] { "Number", "Text", "DateTime", "Category", "Matrix" }));
        Assert.That(Enum.GetNames(typeof(ChartValidationSeverity)), Is.EquivalentTo(new string[] { "Warning", "Error" }));
        Assert.That(Enum.GetNames(typeof(ChartHistogramBinStrategy)), Is.EquivalentTo(new string[] { "FixedCount", "FixedWidth", "Sturges", "Scott", "FreedmanDiaconis" }));
        Assert.That(Enum.GetNames(typeof(ChartMarkerShape)), Does.Contain("CustomPath"));
        Assert.That(Enum.GetNames(typeof(ChartStackMode)), Is.EquivalentTo(new string[] { "None", "Zero", "Percent", "Diverging", "Streamgraph" }));
        Assert.That(Enum.GetNames(typeof(ChartMissingValuePolicy)), Is.EquivalentTo(new string[] { "Skip", "Gap", "Zero", "Interpolate", "CarryForward" }));
        Assert.That(Enum.GetNames(typeof(ChartSortKind)), Does.Contain("Custom"));
        Assert.That(Enum.GetNames(typeof(ChartAggregationKind)), Does.Contain("StandardDeviation"));
        Assert.That(Enum.GetNames(typeof(ChartAggregationKind)), Does.Contain("Percentile"));
    }
}
