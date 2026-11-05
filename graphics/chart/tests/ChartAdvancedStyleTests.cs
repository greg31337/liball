namespace chart.Tests;

[TestFixture]
public class ChartAdvancedStyleTests
{
    [Test]
    public void StyleCascade_ResolvesAdvancedOverridesThroughPointLevel()
    {
        ChartTheme theme = new ChartTheme("advanced");
        ChartStyle chartStyle = new ChartStyle();
        ChartStyle plotStyle = new ChartStyle();
        ChartStyle seriesStyle = new ChartStyle();
        ChartStyle pointStyle = new ChartStyle();
        ChartPath marker = CreateTriangleMarker();

        theme.GetStyle().SetDashArray(new double[] { 6.0, 2.0 });
        theme.GetStyle().SetMiterLimit(8.0);
        chartStyle.SetFillOpacity(0.7);
        plotStyle.SetStrokeOpacity(0.6);
        seriesStyle.SetBorder(ChartColor.FromRgb(10, 20, 30), 3.0);
        seriesStyle.SetFontWeight(ChartFontWeight.Bold);
        pointStyle.SetFontStyle(ChartFontStyle.Italic);
        pointStyle.SetTextOutline(ChartColor.White(), 2.0);
        pointStyle.SetTextShadow(ChartColor.FromRgb(40, 50, 60), 1.0, 2.0, 3.0);
        pointStyle.SetCustomMarker(marker);
        marker.LineTo(9.0, 9.0);

        ChartResolvedStyle resolved = ChartStyleCascade.ResolveStyle(theme, chartStyle, plotStyle, seriesStyle, pointStyle, 0);

        Assert.That(resolved.DashArray, Is.EqualTo(new double[] { 6.0, 2.0 }));
        Assert.That(resolved.MiterLimit, Is.EqualTo(8.0));
        Assert.That(resolved.FillOpacity, Is.EqualTo(0.7));
        Assert.That(resolved.StrokeOpacity, Is.EqualTo(0.6));
        Assert.That(resolved.BorderNone, Is.False);
        AssertColor(resolved.BorderColor, 10, 20, 30, 255);
        Assert.That(resolved.BorderWidth, Is.EqualTo(3.0));
        Assert.That(resolved.StrokeWidth, Is.EqualTo(3.0));
        Assert.That(resolved.FontWeight, Is.EqualTo(ChartFontWeight.Bold));
        Assert.That(resolved.FontStyle, Is.EqualTo(ChartFontStyle.Italic));
        Assert.That(resolved.TextOutlineWidth, Is.EqualTo(2.0));
        Assert.That(resolved.TextShadowOffsetX, Is.EqualTo(1.0));
        Assert.That(resolved.TextShadowOffsetY, Is.EqualTo(2.0));
        Assert.That(resolved.TextShadowBlur, Is.EqualTo(3.0));
        Assert.That(resolved.CustomMarkerPath.GetCommands().Count, Is.EqualTo(4));
    }

    [Test]
    public void RenderStyle_SerializesAdvancedStrokeAndTextStylesToSvg()
    {
        ChartRenderStyle style = ChartRenderStyle.CreateFill(ChartColor.FromRgb(20, 40, 60));
        ChartRenderScene scene = new ChartRenderScene(120, 40);

        style.SetBorder(ChartColor.FromRgb(80, 100, 120), 2.0);
        style.SetDashArray(new double[] { 5.0, 3.0 });
        style.SetMiterLimit(7.0);
        style.SetFillOpacity(0.4);
        style.SetStrokeOpacity(0.6);
        style.SetFont("Inter", 14);
        style.SetFontWeight(ChartFontWeight.Bold);
        style.SetFontStyle(ChartFontStyle.Oblique);
        style.SetTextOutline(ChartColor.White(), 1.5);
        style.SetTextShadow(ChartColor.Black(), 2.0, 3.0, 4.0);
        scene.AddText("label", "Styled", 10.0, 20.0, style);

        string svg = scene.ToSvgString();

        Assert.That(svg, Does.Contain("fill:#14283C"));
        Assert.That(svg, Does.Contain("fill-opacity:0.4"));
        Assert.That(svg, Does.Contain("stroke-opacity:0.6"));
        Assert.That(svg, Does.Contain("stroke-dasharray:5 3"));
        Assert.That(svg, Does.Contain("stroke-miterlimit:7"));
        Assert.That(svg, Does.Contain("font-weight:bold"));
        Assert.That(svg, Does.Contain("font-style:oblique"));
        Assert.That(svg, Does.Contain("paint-order:stroke"));
        Assert.That(svg, Does.Contain("filter:drop-shadow(2px 3px 4px #000000)"));
        Assert.That(style.GetHasBorder(), Is.True);
        AssertColor(style.GetBorderColor(), 80, 100, 120, 255);
    }

    [Test]
    public void RenderStyle_AppliesFillAndStrokeOpacityIndependently()
    {
        ChartRenderStyle style = new ChartRenderStyle();

        style.SetFill(new ChartColor(1, 2, 3, 200));
        style.SetStroke(new ChartColor(4, 5, 6, 200), 1.0);
        style.SetOpacity(0.5);
        style.SetFillOpacity(0.25);
        style.SetStrokeOpacity(0.4);

        ChartColor fill = style.RasterFillColor();
        ChartColor stroke = style.RasterStrokeColor();

        Assert.That(fill.GetA(), Is.EqualTo(25));
        Assert.That(stroke.GetA(), Is.EqualTo(40));
        Assert.That(style.GetFillOpacity(), Is.EqualTo(0.25));
        Assert.That(style.GetStrokeOpacity(), Is.EqualTo(0.4));
    }

    [Test]
    public void CartesianSeries_PointStyleOverridesAreCopiedIndependently()
    {
        ChartCartesianSeries series = new ChartCartesianSeries("points", ChartCartesianSeriesKind.Scatter);
        ChartRenderStyle red = ChartRenderStyle.CreateFill(ChartColor.FromRgb(200, 0, 0));
        ChartRenderStyle blue = ChartRenderStyle.CreateFill(ChartColor.FromRgb(0, 0, 200));

        series.AddMarker("first", ChartMarkerShape.Circle, 1.0, 1.0, 4.0, red);
        series.AddMarker("second", ChartMarkerShape.Circle, 2.0, 2.0, 4.0, red);
        series.SetPointStyle(1, blue);
        blue.SetFill(ChartColor.FromRgb(0, 200, 0));

        AssertColor(series.CommandAt(0).GetStyle().GetFillColor(), 200, 0, 0, 255);
        AssertColor(series.CommandAt(1).GetStyle().GetFillColor(), 0, 0, 200, 255);
        Assert.Throws<System.ArgumentOutOfRangeException>(() => series.SetPointStyle(2, red));
        Assert.Throws<System.ArgumentNullException>(() => series.SetPointStyle(0, null!));
    }

    [Test]
    public void CartesianRenderer_RetainsCustomMarkerDefinitionsInDataCoordinates()
    {
        ChartCartesianTransform transform = new ChartCartesianTransform(new ChartRect(0.0, 0.0, 100.0, 100.0), new ChartRange(0.0, 10.0), new ChartRange(0.0, 10.0));
        ChartCartesianRenderer renderer = new ChartCartesianRenderer(transform);
        ChartCartesianSeries series = new ChartCartesianSeries("custom", ChartCartesianSeriesKind.Scatter);
        ChartPath marker = CreateTriangleMarker();

        series.AddCustomMarker("triangle", marker, 5.0, 5.0, 8.0, ChartRenderStyle.CreateFill(ChartColor.Black()));
        renderer.AddSeries(series);
        ChartRenderScene scene = renderer.Render(100, 100);
        marker.LineTo(10.0, 10.0);

        ChartRenderCommand command = scene.CommandAt(0);
        Assert.That(command.GetMarkerShape(), Is.EqualTo(ChartMarkerShape.CustomPath));
        Assert.That(command.GetX1(), Is.EqualTo(50.0));
        Assert.That(command.GetY1(), Is.EqualTo(50.0));
        Assert.That(command.GetMarkerPath().GetCommands().Count, Is.EqualTo(4));
    }

    [Test]
    public void AdvancedStyles_CopyArraysPathsAndValidateValues()
    {
        ChartStyle style = new ChartStyle();
        ChartPath marker = CreateTriangleMarker();
        double[] dashArray = new double[] { 4.0, 2.0 };

        style.SetDashArray(dashArray);
        style.SetCustomMarker(marker);
        ChartStyle copy = style.Copy();
        dashArray[0] = 99.0;
        marker.LineTo(9.0, 9.0);

        Assert.That(copy.GetDashArray(), Is.EqualTo(new double[] { 4.0, 2.0 }));
        Assert.That(copy.GetCustomMarker().GetCommands().Count, Is.EqualTo(4));
        Assert.Throws<System.ArgumentException>(() => style.SetDashArray(new double[0]));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => style.SetDashArray(new double[] { 1.0, 0.0 }));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => style.SetMiterLimit(0.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => style.SetFillOpacity(1.1));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => style.SetFontWeight((ChartFontWeight)9));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => style.SetFontStyle((ChartFontStyle)9));
    }

    private static ChartPath CreateTriangleMarker()
    {
        ChartPath marker = new ChartPath();
        marker.MoveTo(0.0, -0.5);
        marker.LineTo(0.5, 0.5);
        marker.LineTo(-0.5, 0.5);
        marker.Close();
        return marker;
    }

    private static void AssertColor(ChartColor color, byte red, byte green, byte blue, byte alpha)
    {
        Assert.That(color.GetR(), Is.EqualTo(red));
        Assert.That(color.GetG(), Is.EqualTo(green));
        Assert.That(color.GetB(), Is.EqualTo(blue));
        Assert.That(color.GetA(), Is.EqualTo(alpha));
    }
}
