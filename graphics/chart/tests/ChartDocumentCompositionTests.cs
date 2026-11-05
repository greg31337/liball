namespace chart.Tests;

[TestFixture]
public class ChartDocumentCompositionTests
{
    [Test]
    public void RenderComposition_DrawsDocumentPlotAxesGridSeriesGuidesAndAnnotations()
    {
        IChartDocument document = new ChartDocument(400.0, 300.0);
        document.SetMargins(new ChartPadding(20.0, 20.0, 20.0, 20.0));
        document.SetTitle("Revenue");
        document.SetSubtitle("Quarterly");
        document.SetCaption("Source: sample");
        ChartPlot plot = document.AddPlotById("main");
        plot.SetBackground(ChartColor.FromRgb(245, 246, 247));
        plot.SetCartesianDomains(new ChartRange(0.0, 10.0), new ChartRange(0.0, 10.0));
        ChartAxis bottom = plot.AddBottomAxis(0.0, 10.0, 3);
        ChartAxis left = plot.AddLeftAxis(0.0, 10.0, 3);
        bottom.SetTickLabels(true, false, 0.0);
        bottom.SetTitle("Time");
        bottom.SetGridLines(true, 0.0, ChartColor.FromRgb(220, 220, 220), 1.0);
        left.SetTickLabels(true, false, 0.0);
        left.SetGridLines(true, 0.0, ChartColor.FromRgb(220, 220, 220), 1.0);
        ChartCartesianSeries line = ChartLineAreaChartsOperations.CreateLineSeries("revenue-line", new double[] { 0.0, 10.0 }, new double[] { 2.0, 8.0 }, ChartRenderStyle.CreateStroke(ChartColor.FromRgb(10, 80, 160), 2.0));
        plot.AddSeries(line);
        ChartCartesianSeries annotation = new ChartCartesianSeries("plot-notes", ChartCartesianSeriesKind.Mixed);
        annotation.SetClipToPlotArea(false);
        annotation.AddText("peak-note", "Peak", 10.0, 8.0, ChartRenderStyle.CreateFill(ChartColor.Black()));
        plot.AddAnnotationSeries(annotation);

        ChartLegend legend = new ChartLegend("series-legend");
        ChartStyle legendStyle = new ChartStyle();
        legendStyle.SetFill(ChartColor.FromRgb(10, 80, 160));
        legend.AddSymbolEntry("revenue", "Revenue", ChartMarkerShape.Line, legendStyle);
        document.AddLegend(legend, ChartSide.Right, 90.0, 0.0, 8.0, false);

        ChartLegend colorbar = new ChartLegend("value-colorbar");
        colorbar.AddGradientEntry("scale", "Low to high", new ChartColor[] { ChartColor.FromRgb(0, 0, 255), ChartColor.FromRgb(255, 0, 0) }, new double[] { 0.0, 1.0 });
        document.AddColorbar(colorbar, ChartSide.Bottom, 0.0, 38.0, 6.0, false);
        document.AddCompositionAnnotation("figure-note", "Preliminary", ChartSide.Top, 100.0, 24.0, 4.0, true, ChartRenderStyle.CreateFill(ChartColor.Black()));

        ChartRenderScene scene = document.RenderComposition();

        Assert.That(scene.GetWidth(), Is.EqualTo(400));
        Assert.That(scene.GetHeight(), Is.EqualTo(300));
        Assert.That(FindCommand(scene, "main-background").Kind, Is.EqualTo(ChartRenderCommandKind.Rect));
        Assert.That(FindCommand(scene, "main-axis-bottom-grid-0").Kind, Is.EqualTo(ChartRenderCommandKind.Line));
        Assert.That(FindCommand(scene, "revenue-line-line-segment-0").Kind, Is.EqualTo(ChartRenderCommandKind.Line));
        Assert.That(FindCommand(scene, "axis-bottom-rule").Kind, Is.EqualTo(ChartRenderCommandKind.Line));
        Assert.That(FindCommand(scene, "axis-left-label-0").Kind, Is.EqualTo(ChartRenderCommandKind.Text));
        Assert.That(FindCommand(scene, "peak-note").Text, Is.EqualTo("Peak"));
        Assert.That(FindCommand(scene, "series-legend-revenue-symbol").Kind, Is.EqualTo(ChartRenderCommandKind.Line));
        Assert.That(FindCommand(scene, "value-colorbar-scale-symbol").Style.GetHasFillGradient(), Is.True);
        Assert.That(FindCommand(scene, "figure-note").Text, Is.EqualTo("Preliminary"));
        Assert.That(FindCommand(scene, "title").Text, Is.EqualTo("Revenue"));
        Assert.That(FindCommand(scene, "subtitle").Text, Is.EqualTo("Quarterly"));
        Assert.That(FindCommand(scene, "caption").Text, Is.EqualTo("Source: sample"));
    }

    [Test]
    public void RenderComposition_LaysOutSubplotsInsetsAndOneSharedAxisGuide()
    {
        ChartDocument document = new ChartDocument(420.0, 220.0);
        document.SetMargins(new ChartPadding(10.0, 10.0, 10.0, 10.0));
        ChartGrid grid = new ChartGrid(1, 2);
        grid.SetGaps(0.0, 20.0);
        document.SetGrid(grid);
        ChartPlot left = new ChartPlot("left");
        ChartPlot right = new ChartPlot("right");
        ChartPlot inset = new ChartPlot("detail");
        left.SetGridPosition(0, 0, 1, 1);
        right.SetGridPosition(0, 1, 1, 1);
        inset.SetInset("left", new ChartRect(0.5, 0.1, 0.4, 0.4));
        left.SetSharedAxes("time", "");
        right.SetSharedAxes("time", "");
        left.AddBottomAxis(0.0, 1.0, 2);
        right.AddBottomAxis(0.0, 1.0, 2);
        left.SetCartesianDomains(new ChartRange(0.0, 1.0), new ChartRange(0.0, 1.0));
        right.SetCartesianDomains(new ChartRange(0.0, 1.0), new ChartRange(0.0, 1.0));
        inset.SetCartesianDomains(new ChartRange(0.0, 1.0), new ChartRange(0.0, 1.0));
        left.AddSeries(PointSeries("left-point", 0.5, 0.5));
        right.AddSeries(PointSeries("right-point", 0.5, 0.5));
        inset.SetBackground(ChartColor.White());
        inset.AddSeries(PointSeries("detail-point", 0.5, 0.5));
        document.AddPlot(left);
        document.AddPlot(right);
        document.AddPlot(inset);

        ChartLayoutResult layout = ChartLayoutEngineOperations.Layout(document);
        ChartRenderScene scene = document.RenderComposition();
        ChartRenderCommand sharedRule = FindCommand(scene, "shared-x-time-rule");
        ChartRect leftBounds = layout.ItemById("left").Bounds;
        ChartRect rightBounds = layout.ItemById("right").Bounds;
        ChartRect insetBounds = layout.ItemById("detail").Bounds;

        Assert.That(CountCommands(scene, "shared-x-time-rule"), Is.EqualTo(1));
        Assert.That(sharedRule.X1, Is.EqualTo(leftBounds.Left()).Within(0.0000001));
        Assert.That(sharedRule.X2, Is.EqualTo(rightBounds.Right()).Within(0.0000001));
        Assert.That(FindCommand(scene, "left-point").X1, Is.EqualTo(leftBounds.Center().X).Within(0.0000001));
        Assert.That(FindCommand(scene, "right-point").X1, Is.EqualTo(rightBounds.Center().X).Within(0.0000001));
        Assert.That(FindCommand(scene, "detail-background").X1, Is.EqualTo(insetBounds.X).Within(0.0000001));
        Assert.That(FindCommand(scene, "detail-point").X1, Is.EqualTo(insetBounds.Center().X).Within(0.0000001));
    }

    [Test]
    public void CompositionContent_IsCopiedValidatedAndScaledForDevicePixels()
    {
        ChartDocument document = new ChartDocument(100.0, 80.0);
        document.SetMargins(new ChartPadding(5.0, 5.0, 5.0, 5.0));
        document.SetDevicePixelRatio(2.0);
        ChartPlot plot = document.AddPlotById("main");
        plot.SetCartesianDomains(new ChartRange(0.0, 1.0), new ChartRange(0.0, 1.0));
        ChartCartesianSeries original = PointSeries("point", 0.5, 0.5);
        ChartCartesianSeries stored = plot.AddSeries(original);
        original.SetOrder(9);
        ChartLegend legend = new ChartLegend("legend");
        ChartStyle style = new ChartStyle();
        legend.AddSymbolEntry("item", "Item", ChartMarkerShape.Circle, style);
        document.AddLegend(legend, ChartSide.Right, 30.0, 0.0, 2.0, false);
        ChartDocument copy = document.Copy();
        stored.SetOrder(5);

        ChartRenderScene scene = copy.RenderComposition();

        Assert.That(copy.PlotAt(0).SeriesAt(0).GetOrder(), Is.EqualTo(0));
        Assert.That(scene.GetWidth(), Is.EqualTo(200));
        Assert.That(scene.GetHeight(), Is.EqualTo(160));
        Assert.That(FindCommand(scene, "document-device-scale").Kind, Is.EqualTo(ChartRenderCommandKind.PushTransform));
        Assert.That(copy.LegendAt(0).GetId(), Is.EqualTo("legend"));
        Assert.Throws<System.ArgumentNullException>(() => document.AddLegend(null!, ChartSide.Right, 1.0, 1.0, 0.0, false));
        Assert.Throws<System.ArgumentNullException>(() => document.AddColorbar(null!, ChartSide.Right, 1.0, 1.0, 0.0, false));
        Assert.Throws<System.ArgumentException>(() => document.AddCompositionAnnotation("legend", "duplicate", ChartSide.Top, 1.0, 1.0, 0.0, true, ChartRenderStyle.CreateFill(ChartColor.Black())));
        Assert.Throws<System.ArgumentNullException>(() => plot.AddSeries(null!));
        Assert.Throws<System.ArgumentNullException>(() => plot.AddAnnotationSeries(null!));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => plot.SeriesAt(1));
        Assert.Throws<System.ArgumentException>(() => plot.SetCartesianDomains(new ChartRange(1.0, 1.0), new ChartRange(0.0, 1.0)));
    }

    static ChartCartesianSeries PointSeries(string id, double x, double y)
    {
        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Scatter);
        series.AddMarker(id, ChartMarkerShape.Circle, x, y, 4.0, ChartRenderStyle.CreateFill(ChartColor.Black()));
        return series;
    }

    static ChartRenderCommand FindCommand(ChartRenderScene scene, string id)
    {
        int index = 0;
        while (index < scene.CommandCount)
        {
            ChartRenderCommand command = scene.CommandAt(index);
            if (command.Id == id)
            {
                return command;
            }

            index = index + 1;
        }

        throw new AssertionException("Command was not found: " + id);
    }

    static int CountCommands(ChartRenderScene scene, string id)
    {
        int count = 0;
        int index = 0;
        while (index < scene.CommandCount)
        {
            if (scene.CommandAt(index).Id == id)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }
}
