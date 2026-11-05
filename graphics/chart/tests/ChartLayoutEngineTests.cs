namespace chart.Tests;

[TestFixture]
public class ChartLayoutEngineTests
{
    [Test]
    public void Layout_UsesDocumentContentAsDefaultPlotArea()
    {
        ChartDocument document = new ChartDocument(400.0, 300.0);

        document.SetMargins(new ChartPadding(20.0, 30.0, 40.0, 50.0));
        ChartLayoutResult result = ChartLayoutEngineOperations.Layout(document);
        ChartLayoutItem plot = result.ItemById("plot");

        Assert.That(result.FigureRect.Width, Is.EqualTo(400.0));
        Assert.That(result.FigureRect.Height, Is.EqualTo(300.0));
        Assert.That(result.ContentRect.X, Is.EqualTo(20.0));
        Assert.That(result.ContentRect.Y, Is.EqualTo(30.0));
        Assert.That(result.ContentRect.Width, Is.EqualTo(340.0));
        Assert.That(result.ContentRect.Height, Is.EqualTo(220.0));
        Assert.That(result.PlotArea.X, Is.EqualTo(20.0));
        Assert.That(result.PlotArea.Y, Is.EqualTo(30.0));
        Assert.That(plot.Kind, Is.EqualTo(ChartLayoutItemKind.Plot));
        Assert.That(plot.Bounds.Width, Is.EqualTo(340.0));
    }

    [Test]
    public void Layout_ReservesTextDockedItemsPlotsAndAxes()
    {
        ChartDocument document = new ChartDocument(400.0, 300.0);
        ChartPlot plot = new ChartPlot("main");
        ChartAxis axis = ChartAxis.CreateBottom(
            ChartTickGeneratorOperations.GenerateExplicitTicks(new double[] { 0.0, 10.0 }),
            new ChartRange(0.0, 10.0),
            new ChartRect(0.0, 0.0, 100.0, 100.0));

        document.SetMargins(new ChartPadding(20.0, 20.0, 20.0, 20.0));
        document.SetTitle("Title");
        document.Title.SetStyle(ChartColor.Black(), "Arial", 20);
        document.Title.SetSpacing(5.0);
        document.SetSubtitle("Subtitle");
        document.SetCaption("Caption");
        document.AddLegendLayout("legend", ChartSide.Right, 60.0, 0.0, 10.0);
        document.AddColorbarLayout("colorbar", ChartSide.Bottom, 0.0, 30.0, 5.0);
        document.AddAnnotationLayout("note", ChartSide.Top, 50.0, 40.0, 0.0, true);
        axis.SetTickLabels(true, false, 0.0);
        axis.SetTitle("X");
        plot.SetMargins(new ChartPadding(5.0, 5.0, 5.0, 5.0));
        plot.AddAxis(axis);
        document.AddPlot(plot);

        ChartLayoutResult result = ChartLayoutEngineOperations.Layout(document);

        AssertRect(result.ItemById("title").Bounds, 20.0, 20.0, 360.0, 20.0);
        AssertRect(result.ItemById("subtitle").Bounds, 20.0, 45.0, 360.0, 12.0);
        AssertRect(result.ItemById("caption").Bounds, 20.0, 268.0, 360.0, 12.0);
        AssertRect(result.ItemById("legend").Bounds, 320.0, 65.0, 60.0, 195.0);
        AssertRect(result.ItemById("colorbar").Bounds, 20.0, 230.0, 290.0, 30.0);
        AssertRect(result.ItemById("note").Bounds, 20.0, 65.0, 50.0, 40.0);
        AssertRect(result.PlotArea, 20.0, 65.0, 290.0, 160.0);
        AssertRect(result.ItemById("main").Bounds, 25.0, 70.0, 280.0, 150.0);
        AssertRect(result.ItemById("axis-bottom").Bounds, 25.0, 220.0, 280.0, 42.0);
    }

    [Test]
    public void Layout_ReservesTitleSubtitleAndCaptionWithCustomSpacing()
    {
        ChartDocument document = new ChartDocument(320.0, 220.0);

        document.SetMargins(new ChartPadding(10.0, 15.0, 20.0, 25.0));
        document.SetTitle("Revenue");
        document.Title.SetStyle(ChartColor.Black(), "Arial", 24);
        document.Title.SetSpacing(6.0);
        document.SetSubtitle("Quarterly");
        document.Subtitle.SetStyle(ChartColor.Black(), "Arial", 14);
        document.Subtitle.SetSpacing(4.0);
        document.SetCaption("Source");
        document.Caption.SetStyle(ChartColor.Black(), "Arial", 10);
        document.Caption.SetSpacing(3.0);

        ChartLayoutResult result = ChartLayoutEngineOperations.Layout(document);

        AssertRect(result.ItemById("title").Bounds, 10.0, 15.0, 290.0, 24.0);
        AssertRect(result.ItemById("subtitle").Bounds, 10.0, 45.0, 290.0, 14.0);
        AssertRect(result.ItemById("caption").Bounds, 10.0, 185.0, 290.0, 10.0);
        AssertRect(result.PlotArea, 10.0, 63.0, 290.0, 119.0);
    }

    [Test]
    public void Layout_ReservesAxesOnEverySideWithThicknessFromLabelsAndTitles()
    {
        ChartDocument document = new ChartDocument(300.0, 200.0);
        ChartPlot plot = new ChartPlot("main");
        ChartTick[] ticks = ChartTickGeneratorOperations.GenerateExplicitTicks(new double[] { 0.0, 1.0 });
        ChartRange domain = new ChartRange(0.0, 1.0);
        ChartRect area = new ChartRect(0.0, 0.0, 100.0, 100.0);
        ChartAxis bottom = ChartAxis.CreateBottom(ticks, domain, area);
        ChartAxis top = ChartAxis.CreateTop(ticks, domain, area);
        ChartAxis left = ChartAxis.CreateLeft(ticks, domain, area);
        ChartAxis right = ChartAxis.CreateRight(ticks, domain, area);

        document.SetMargins(new ChartPadding(20.0, 20.0, 20.0, 20.0));
        bottom.SetTickLabels(true, false, 0.0);
        bottom.SetTitle("Bottom");
        top.SetTickLabels(true, false, 0.0);
        left.SetTitle("Left");
        plot.AddAxis(bottom);
        plot.AddAxis(top);
        plot.AddAxis(left);
        plot.AddAxis(right);
        document.AddPlot(plot);

        ChartLayoutResult result = ChartLayoutEngineOperations.Layout(document);

        AssertRect(result.ItemById("axis-bottom").Bounds, 20.0, 180.0, 260.0, 42.0);
        AssertRect(result.ItemById("axis-top").Bounds, 20.0, -8.0, 260.0, 28.0);
        AssertRect(result.ItemById("axis-left").Bounds, -22.0, 20.0, 42.0, 160.0);
        AssertRect(result.ItemById("axis-right").Bounds, 280.0, 20.0, 6.0, 160.0);
        Assert.That(result.ItemById("axis-bottom").Side, Is.EqualTo(ChartSide.Bottom));
        Assert.That(result.ItemById("axis-top").Side, Is.EqualTo(ChartSide.Top));
        Assert.That(result.ItemById("axis-left").Side, Is.EqualTo(ChartSide.Left));
        Assert.That(result.ItemById("axis-right").Side, Is.EqualTo(ChartSide.Right));
    }

    [Test]
    public void Layout_ReservesLegendsAndColorbarsOnAllSides()
    {
        ChartDocument document = new ChartDocument(300.0, 220.0);

        document.SetMargins(new ChartPadding(10.0, 10.0, 10.0, 10.0));
        document.AddLegendLayout("legend-left", ChartSide.Left, 30.0, 0.0, 5.0);
        document.AddLegendLayout("legend-right", ChartSide.Right, 40.0, 0.0, 6.0);
        document.AddColorbarLayout("colorbar-top", ChartSide.Top, 0.0, 20.0, 4.0);
        document.AddColorbarLayout("colorbar-bottom", ChartSide.Bottom, 0.0, 25.0, 5.0);

        ChartLayoutResult result = ChartLayoutEngineOperations.Layout(document);

        AssertRect(result.ItemById("legend-left").Bounds, 10.0, 10.0, 30.0, 200.0);
        AssertRect(result.ItemById("legend-right").Bounds, 250.0, 10.0, 40.0, 200.0);
        AssertRect(result.ItemById("colorbar-top").Bounds, 45.0, 10.0, 199.0, 20.0);
        AssertRect(result.ItemById("colorbar-bottom").Bounds, 45.0, 185.0, 199.0, 25.0);
        AssertRect(result.PlotArea, 45.0, 34.0, 199.0, 146.0);
    }

    [Test]
    public void Layout_UsesGridCellsForMultiplePlots()
    {
        ChartDocument document = new ChartDocument(420.0, 220.0);
        ChartGrid grid = new ChartGrid(1, 2);

        document.SetMargins(new ChartPadding(10.0, 10.0, 10.0, 10.0));
        grid.SetGaps(0.0, 20.0);
        document.SetGrid(grid);
        document.AddPlot(new ChartPlot("left"));
        document.AddPlot(new ChartPlot("right"));

        ChartLayoutResult result = ChartLayoutEngineOperations.Layout(document);

        AssertRect(result.ItemById("left").Bounds, 10.0, 10.0, 190.0, 200.0);
        AssertRect(result.ItemById("right").Bounds, 220.0, 10.0, 190.0, 200.0);
    }

    [Test]
    public void Layout_UsesExplicitSpansAndInsetPlots()
    {
        ChartDocument document = new ChartDocument(420.0, 320.0);
        ChartGrid grid = new ChartGrid(2, 3);
        ChartPlot main = new ChartPlot("main");
        ChartPlot detail = new ChartPlot("detail");

        document.SetMargins(new ChartPadding(10.0, 10.0, 10.0, 10.0));
        grid.SetGaps(10.0, 20.0);
        document.SetGrid(grid);
        main.SetGridPosition(0, 0, 1, 2);
        detail.SetInset("main", new ChartRect(0.5, 0.5, 0.25, 0.4));
        document.AddPlot(main);
        document.AddPlot(detail);

        ChartLayoutResult result = ChartLayoutEngineOperations.Layout(document);

        AssertRect(result.ItemById("main").Bounds, 10.0, 10.0, 260.0, 145.0);
        AssertRect(result.ItemById("detail").Bounds, 140.0, 82.5, 65.0, 58.0);
    }

    [Test]
    public void CreateSmallMultiples_AddsGridPositionedPlots()
    {
        ChartDocument document = new ChartDocument(420.0, 220.0);

        document.SetMargins(new ChartPadding(10.0, 10.0, 10.0, 10.0));
        document.CreateSmallMultiples("small", 2, 2);
        ChartLayoutResult result = ChartLayoutEngineOperations.Layout(document);

        Assert.That(document.Grid.Rows, Is.EqualTo(2));
        Assert.That(document.Grid.Columns, Is.EqualTo(2));
        Assert.That(document.PlotCount, Is.EqualTo(4));
        AssertRect(result.ItemById("small-0").Bounds, 10.0, 10.0, 200.0, 100.0);
        AssertRect(result.ItemById("small-1").Bounds, 210.0, 10.0, 200.0, 100.0);
        AssertRect(result.ItemById("small-2").Bounds, 10.0, 110.0, 200.0, 100.0);
        AssertRect(result.ItemById("small-3").Bounds, 210.0, 110.0, 200.0, 100.0);
    }

    [Test]
    public void Layout_SubplotsFacetsAndInsetsUseStableGridMath()
    {
        ChartDocument document = new ChartDocument(620.0, 420.0);
        ChartPlot inset = new ChartPlot("facet-4-inset");

        document.SetMargins(new ChartPadding(10.0, 10.0, 10.0, 10.0));
        document.CreateSmallMultiples("facet", 2, 3);
        document.Grid.SetGaps(20.0, 30.0);
        inset.SetInset("facet-4", new ChartRect(0.25, 0.2, 0.5, 0.4));
        document.AddPlot(inset);

        ChartLayoutResult result = ChartLayoutEngineOperations.Layout(document);

        AssertRect(result.ItemById("facet-0").Bounds, 10.0, 10.0, 180.0, 190.0);
        AssertRect(result.ItemById("facet-2").Bounds, 430.0, 10.0, 180.0, 190.0);
        AssertRect(result.ItemById("facet-4").Bounds, 220.0, 220.0, 180.0, 190.0);
        AssertRect(result.ItemById("facet-4-inset").Bounds, 265.0, 258.0, 90.0, 76.0);
    }

    [Test]
    public void Layout_DistinguishesIndependentAndSharedAxes()
    {
        ChartDocument independent = CreateTwoPlotAxisDocument(false);
        ChartDocument shared = CreateTwoPlotAxisDocument(true);

        ChartLayoutResult independentResult = ChartLayoutEngineOperations.Layout(independent);
        ChartLayoutResult sharedResult = ChartLayoutEngineOperations.Layout(shared);

        Assert.That(independentResult.ItemIndex("left-axis-bottom"), Is.GreaterThanOrEqualTo(0));
        Assert.That(independentResult.ItemIndex("right-axis-bottom"), Is.GreaterThanOrEqualTo(0));
        Assert.That(sharedResult.ItemIndex("shared-x-time"), Is.GreaterThanOrEqualTo(0));
        Assert.That(sharedResult.ItemIndex("left-axis-bottom"), Is.EqualTo(-1));
        Assert.That(sharedResult.ItemIndex("right-axis-bottom"), Is.EqualTo(-1));
    }

    [Test]
    public void LayoutRequests_CopyIntoDocument()
    {
        ChartDocument document = new ChartDocument(300.0, 200.0);
        ChartLayoutRequest request = ChartLayoutRequest.CreateLegend("legend", ChartSide.Left, 40.0, 0.0, 5.0);

        ChartLayoutRequest added = document.AddLayoutRequest(request);
        request.Id = "changed";
        ChartDocument copy = document.Copy();
        added.Width = 80.0;

        Assert.That(document.LayoutRequestCount, Is.EqualTo(1));
        Assert.That(document.LayoutRequestAt(0).Id, Is.EqualTo("legend"));
        Assert.That(copy.LayoutRequestAt(0).Width, Is.EqualTo(40.0));
    }

    [Test]
    public void LayoutEngine_ValidatesInputs()
    {
        ChartDocument document = new ChartDocument(100.0, 100.0);
        ChartLayoutResult result = new ChartLayoutResult(
            new ChartRect(0.0, 0.0, 100.0, 100.0),
            new ChartRect(10.0, 10.0, 80.0, 80.0),
            new ChartRect(10.0, 10.0, 80.0, 80.0));

        Assert.Throws<System.ArgumentNullException>(() => ChartLayoutEngineOperations.Layout(null!));
        Assert.Throws<System.ArgumentException>(() => new ChartLayoutRequest(ChartLayoutItemKind.Plot, "plot", ChartSide.Top, 1.0, 1.0, 0.0, false));
        Assert.Throws<System.ArgumentNullException>(() => ChartLayoutRequest.CreateLegend(null!, ChartSide.Left, 1.0, 1.0, 0.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartLayoutRequest.CreateLegend("legend", ChartSide.Left, -1.0, 1.0, 0.0));
        Assert.Throws<System.ArgumentNullException>(() => document.AddLayoutRequest(null!));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => document.LayoutRequestAt(0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => result.ItemAt(0));
        Assert.Throws<System.ArgumentException>(() => result.ItemById("missing"));
        Assert.Throws<System.ArgumentNullException>(() => result.ItemIndex(null!));
        Assert.Throws<System.ArgumentException>(() => ChartLayoutEngineOperations.ValidateRemaining(new ChartRect(0.0, 0.0, 0.0, 1.0)));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartPlot("p").SetGridPosition(-1, 0, 1, 1));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartPlot("p").SetGridPosition(0, 0, 0, 1));
        Assert.Throws<System.ArgumentNullException>(() => new ChartPlot("p").SetInset(null!, new ChartRect(0.0, 0.0, 1.0, 1.0)));
        Assert.Throws<System.ArgumentNullException>(() => new ChartPlot("p").SetInset("parent", null!));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartPlot("p").SetInset("parent", new ChartRect(0.8, 0.0, 0.3, 1.0)));
        Assert.Throws<System.ArgumentNullException>(() => new ChartPlot("p").SetSharedAxes(null!, ""));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartLayoutEngineOperations.SpanCell(new ChartGrid(1, 1), new ChartRect(0.0, 0.0, 10.0, 10.0), 0, 0, 2, 1));
        Assert.Throws<System.ArgumentException>(() => ChartLayoutEngineOperations.InsetPlotBounds(result, new ChartPlot("missing")));
        Assert.Throws<System.ArgumentException>(() => document.CreateSmallMultiples("", 1, 1));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => document.CreateSmallMultiples("small", 0, 1));
    }

    static ChartDocument CreateTwoPlotAxisDocument(bool sharedAxes)
    {
        ChartDocument document = new ChartDocument(420.0, 220.0);
        ChartGrid grid = new ChartGrid(1, 2);
        ChartPlot left = new ChartPlot("left");
        ChartPlot right = new ChartPlot("right");
        ChartAxis leftAxis = ChartAxis.CreateBottom(
            ChartTickGeneratorOperations.GenerateExplicitTicks(new double[] { 0.0, 1.0 }),
            new ChartRange(0.0, 1.0),
            new ChartRect(0.0, 0.0, 1.0, 1.0));
        ChartAxis rightAxis = ChartAxis.CreateBottom(
            ChartTickGeneratorOperations.GenerateExplicitTicks(new double[] { 0.0, 1.0 }),
            new ChartRange(0.0, 1.0),
            new ChartRect(0.0, 0.0, 1.0, 1.0));

        document.SetMargins(new ChartPadding(10.0, 10.0, 10.0, 10.0));
        grid.SetGaps(0.0, 20.0);
        document.SetGrid(grid);
        left.SetGridPosition(0, 0, 1, 1);
        right.SetGridPosition(0, 1, 1, 1);
        if (sharedAxes)
        {
            left.SetSharedAxes("time", "");
            right.SetSharedAxes("time", "");
        }

        left.AddAxis(leftAxis);
        right.AddAxis(rightAxis);
        document.AddPlot(left);
        document.AddPlot(right);
        return document;
    }

    static void AssertRect(ChartRect rect, double x, double y, double width, double height)
    {
        Assert.That(rect.X, Is.EqualTo(x).Within(0.0000001));
        Assert.That(rect.Y, Is.EqualTo(y).Within(0.0000001));
        Assert.That(rect.Width, Is.EqualTo(width).Within(0.0000001));
        Assert.That(rect.Height, Is.EqualTo(height).Within(0.0000001));
    }
}
