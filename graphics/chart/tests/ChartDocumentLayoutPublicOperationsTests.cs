namespace chart.Tests;

[TestFixture]
public class ChartDocumentLayoutPublicOperationsTests
{
    [Test]
    public void DocumentAccessors_ExposeFigureTextThemeAndAccessibilityState()
    {
        ChartDocument document = new ChartDocument(320.0, 240.0);
        ChartPadding margins = new ChartPadding(10.0, 20.0, 30.0, 40.0);
        ChartTheme theme = new ChartThemeFactory().Dark();
        ChartAccessibilityMetadata accessibility = new ChartAccessibilityMetadata();
        ChartConfiguration configuration = new ChartConfiguration();
        accessibility.SetTitleDescription("Accessible", "Description");
        configuration.SetDirection(ChartDirection.Reversed);

        document.SetMargins(margins);
        document.SetTheme(theme);
        document.SetTitle("Title");
        document.SetSubtitle("Subtitle");
        document.SetCaption("Caption");
        document.SetAccessibility(accessibility);
        document.SetConfiguration(configuration);

        Assert.That(document.GetMargins().GetLeft(), Is.EqualTo(10.0));
        Assert.That(document.GetMargins(), Is.Not.SameAs(margins));
        Assert.That(document.GetTheme().GetName(), Is.EqualTo(theme.GetName()));
        Assert.That(document.GetTheme(), Is.Not.SameAs(theme));
        Assert.That(document.GetTitle().GetText(), Is.EqualTo("Title"));
        Assert.That(document.GetSubtitle().GetText(), Is.EqualTo("Subtitle"));
        Assert.That(document.GetCaption().GetText(), Is.EqualTo("Caption"));
        Assert.That(document.GetAccessibility().GetTitle(), Is.EqualTo("Accessible"));
        Assert.That(document.GetAccessibility().GetDescription(), Is.EqualTo("Description"));
        Assert.That(document.GetConfiguration().GetDirection(), Is.EqualTo(ChartDirection.Reversed));
    }

    [Test]
    public void DocumentCollectionAccessors_ReplacePlotsAndLayoutRequests()
    {
        ChartDocument document = new ChartDocument(300.0, 200.0);
        ChartPlot plot = new ChartPlot("plot");
        ChartLayoutRequest request = new ChartLayoutRequest(ChartLayoutItemKind.Legend, "legend", ChartSide.Right, 40.0, 0.0, 5.0, false);
        ChartPlot[] plots = new ChartPlot[] { plot };
        ChartLayoutRequest[] requests = new ChartLayoutRequest[] { request };

        document.SetPlots(plots);
        document.SetPlotCount(1);
        document.SetLayoutRequests(requests);
        document.SetLayoutRequestCount(1);

        Assert.That(document.GetPlots(), Is.SameAs(plots));
        Assert.That(document.GetPlotCount(), Is.EqualTo(1));
        Assert.That(document.PlotAt(0), Is.SameAs(plot));
        Assert.That(document.GetLayoutRequests(), Is.SameAs(requests));
        Assert.That(document.GetLayoutRequestCount(), Is.EqualTo(1));
        Assert.That(document.LayoutRequestAt(0), Is.SameAs(request));
    }

    [Test]
    public void DocumentGuideAccessors_ReturnStoredLegendAndColorbarCopies()
    {
        ChartDocument document = new ChartDocument(300.0, 200.0);
        ChartLegend legend = new ChartLegend("legend");
        ChartLegend colorbar = new ChartLegend("colorbar");
        legend.SetTitle("Series");
        colorbar.SetTitle("Values");

        ChartLegend storedLegend = document.AddLegend(legend, ChartSide.Left, 40.0, 0.0, 4.0, false);
        ChartLegend storedColorbar = document.AddColorbar(colorbar, ChartSide.Right, 30.0, 0.0, 4.0, true);
        legend.SetTitle("Changed");
        colorbar.SetTitle("Changed");

        Assert.That(document.LegendAt(0), Is.SameAs(storedLegend));
        Assert.That(document.LegendAt(0).GetTitle(), Is.EqualTo("Series"));
        Assert.That(document.ColorbarAt(0), Is.SameAs(storedColorbar));
        Assert.That(document.ColorbarAt(0).GetTitle(), Is.EqualTo("Values"));
        Assert.That(document.LayoutRequestAt(0).GetOverlay(), Is.False);
        Assert.That(document.LayoutRequestAt(1).GetOverlay(), Is.True);
        Assert.Throws<System.ArgumentOutOfRangeException>(() => document.ColorbarAt(1));
    }

    [Test]
    public void PlotAccessors_ExposeAxesGridSpansInsetsSharingAndTernaryRegion()
    {
        ChartPlot plot = new ChartPlot("plot");
        ChartAxis top = plot.AddTopAxis(0.0, 10.0, 3);
        ChartAxis right = plot.AddRightAxis(0.0, 10.0, 3);
        ChartAxis[] axes = new ChartAxis[] { top, right };
        ChartRect inset = new ChartRect(0.1, 0.2, 0.3, 0.4);

        plot.SetAxes(axes);
        plot.SetAxisCount(2);
        plot.SetHasGridPosition(true);
        plot.SetGridRow(1);
        plot.SetGridColumn(2);
        plot.SetRowSpan(3);
        plot.SetColumnSpan(4);
        plot.SetIsInset(true);
        plot.SetInsetParentId("parent");
        plot.SetInsetBounds(inset);
        plot.SetShareXAxisGroup("shared-x");
        plot.SetShareYAxisGroup("shared-y");
        plot.SetTernaryRegion(new ChartPoint(0.0, 1.0), new ChartPoint(1.0, 1.0), new ChartPoint(0.5, 0.0));

        Assert.That(plot.GetAxes(), Is.SameAs(axes));
        Assert.That(plot.GetAxisCount(), Is.EqualTo(2));
        Assert.That(plot.GetHasGridPosition(), Is.True);
        Assert.That(plot.GetGridRow(), Is.EqualTo(1));
        Assert.That(plot.GetGridColumn(), Is.EqualTo(2));
        Assert.That(plot.GetRowSpan(), Is.EqualTo(3));
        Assert.That(plot.GetColumnSpan(), Is.EqualTo(4));
        Assert.That(plot.GetIsInset(), Is.True);
        Assert.That(plot.GetInsetParentId(), Is.EqualTo("parent"));
        Assert.That(plot.GetInsetBounds(), Is.SameAs(inset));
        Assert.That(plot.GetShareXAxisGroup(), Is.EqualTo("shared-x"));
        Assert.That(plot.GetShareYAxisGroup(), Is.EqualTo("shared-y"));
        Assert.That(plot.GetTernaryFirstVertex().GetY(), Is.EqualTo(1.0));
        Assert.That(plot.GetTernarySecondVertex().GetX(), Is.EqualTo(1.0));
        Assert.That(plot.GetTernaryThirdVertex().GetX(), Is.EqualTo(0.5));
        Assert.That(top.GetPlacement(), Is.EqualTo(ChartAxisPlacement.Top));
        Assert.That(right.GetPlacement(), Is.EqualTo(ChartAxisPlacement.Right));
    }

    [Test]
    public void PlotAnnotationAccessor_ReturnsCopiedStoredSeries()
    {
        ChartPlot plot = new ChartPlot("plot");
        ChartCartesianSeries annotation = new ChartCartesianSeries("annotation", ChartCartesianSeriesKind.Mixed);
        annotation.AddText("label", "Note", 1.0, 2.0, ChartRenderStyle.CreateFill(ChartColor.Black()));

        ChartCartesianSeries stored = plot.AddAnnotationSeries(annotation);
        annotation.SetOrder(5);

        Assert.That(plot.AnnotationSeriesAt(0), Is.SameAs(stored));
        Assert.That(plot.AnnotationSeriesAt(0).GetOrder(), Is.EqualTo(0));
        Assert.That(plot.AnnotationSeriesAt(0).CommandAt(0).GetText(), Is.EqualTo("Note"));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => plot.AnnotationSeriesAt(1));
    }
}
