namespace chart.Tests;

[TestFixture]
public class ChartLayoutPublicOperationsTests
{
    [Test]
    public void LayoutRequestFactoryAndAccessors_CoverEveryRequestKindAndSide()
    {
        ChartLayoutRequestFactory factory = new ChartLayoutRequestFactory();
        ChartLayoutRequest legend = factory.CreateLegend("legend", ChartSide.Left, 40.0, 0.0, 5.0);
        ChartLayoutRequest colorbar = factory.CreateColorbar("colorbar", ChartSide.Right, 20.0, 80.0, 6.0);
        ChartLayoutRequest annotation = factory.CreateAnnotation("note", ChartSide.Top, 100.0, 30.0, 7.0, true);

        Assert.That(legend.GetKind(), Is.EqualTo(ChartLayoutItemKind.Legend));
        Assert.That(legend.GetSide(), Is.EqualTo(ChartSide.Left));
        Assert.That(colorbar.GetKind(), Is.EqualTo(ChartLayoutItemKind.Colorbar));
        Assert.That(colorbar.GetSide(), Is.EqualTo(ChartSide.Right));
        Assert.That(annotation.GetKind(), Is.EqualTo(ChartLayoutItemKind.Annotation));
        Assert.That(annotation.GetSide(), Is.EqualTo(ChartSide.Top));
        Assert.That(annotation.GetOverlay(), Is.True);

        ChartLayoutRequest copy = annotation.Copy();
        annotation.SetKind(ChartLayoutItemKind.Legend);
        annotation.SetId("changed");
        annotation.SetSide(ChartSide.Bottom);
        annotation.SetWidth(110.0);
        annotation.SetHeight(35.0);
        annotation.SetSpacing(8.0);
        annotation.SetOverlay(false);

        Assert.That(annotation.GetKind(), Is.EqualTo(ChartLayoutItemKind.Legend));
        Assert.That(annotation.GetId(), Is.EqualTo("changed"));
        Assert.That(annotation.GetSide(), Is.EqualTo(ChartSide.Bottom));
        Assert.That(annotation.GetWidth(), Is.EqualTo(110.0));
        Assert.That(annotation.GetHeight(), Is.EqualTo(35.0));
        Assert.That(annotation.GetSpacing(), Is.EqualTo(8.0));
        Assert.That(annotation.GetOverlay(), Is.False);
        Assert.That(copy.GetKind(), Is.EqualTo(ChartLayoutItemKind.Annotation));
        Assert.That(copy.GetId(), Is.EqualTo("note"));
        Assert.That(copy.GetSide(), Is.EqualTo(ChartSide.Top));
    }

    [Test]
    public void LayoutEngineFacade_ProducesEveryLayoutItemKind()
    {
        ChartDocument document = new ChartDocument(400.0, 300.0);
        document.SetMargins(new ChartPadding(20.0, 20.0, 20.0, 20.0));
        document.SetTitle("Title");
        document.SetSubtitle("Subtitle");
        document.SetCaption("Caption");
        ChartPlot plot = document.AddPlotById("main");
        plot.AddBottomAxis(0.0, 1.0, 2);
        document.AddLegendLayout("legend", ChartSide.Left, 40.0, 0.0, 4.0);
        document.AddColorbarLayout("colorbar", ChartSide.Right, 30.0, 0.0, 4.0);
        document.AddAnnotationLayout("annotation", ChartSide.Top, 50.0, 20.0, 2.0, true);

        ChartLayoutResult result = new ChartLayoutEngine().Layout(document);

        Assert.That(result.ItemById("figure").GetKind(), Is.EqualTo(ChartLayoutItemKind.Figure));
        Assert.That(result.ItemById("content").GetKind(), Is.EqualTo(ChartLayoutItemKind.Content));
        Assert.That(result.ItemById("main").GetKind(), Is.EqualTo(ChartLayoutItemKind.Plot));
        Assert.That(result.ItemById("axis-bottom").GetKind(), Is.EqualTo(ChartLayoutItemKind.Axis));
        Assert.That(result.ItemById("title").GetKind(), Is.EqualTo(ChartLayoutItemKind.Title));
        Assert.That(result.ItemById("subtitle").GetKind(), Is.EqualTo(ChartLayoutItemKind.Subtitle));
        Assert.That(result.ItemById("caption").GetKind(), Is.EqualTo(ChartLayoutItemKind.Caption));
        Assert.That(result.ItemById("legend").GetKind(), Is.EqualTo(ChartLayoutItemKind.Legend));
        Assert.That(result.ItemById("colorbar").GetKind(), Is.EqualTo(ChartLayoutItemKind.Colorbar));
        Assert.That(result.ItemById("annotation").GetKind(), Is.EqualTo(ChartLayoutItemKind.Annotation));
    }

    [Test]
    public void LayoutItemAndResultAccessors_ExposeMutableLayoutState()
    {
        ChartRect figure = new ChartRect(0.0, 0.0, 200.0, 100.0);
        ChartRect content = new ChartRect(10.0, 10.0, 180.0, 80.0);
        ChartRect plot = new ChartRect(20.0, 20.0, 160.0, 60.0);
        ChartLayoutResult result = new ChartLayoutResult(figure, content, plot);
        ChartLayoutItem item = result.AddItem(ChartLayoutItemKind.Plot, "plot", plot, ChartSide.Top);
        ChartRect changed = new ChartRect(25.0, 25.0, 150.0, 50.0);

        item.SetKind(ChartLayoutItemKind.Annotation);
        item.SetId("changed");
        item.SetBounds(changed);
        item.SetSide(ChartSide.Bottom);
        Assert.That(item.GetKind(), Is.EqualTo(ChartLayoutItemKind.Annotation));
        Assert.That(item.GetId(), Is.EqualTo("changed"));
        Assert.That(item.GetBounds(), Is.SameAs(changed));
        Assert.That(item.GetSide(), Is.EqualTo(ChartSide.Bottom));

        ChartLayoutItem itemCopy = item.Copy();
        item.SetId("mutated");
        Assert.That(itemCopy.GetId(), Is.EqualTo("changed"));

        ChartRect newFigure = new ChartRect(0.0, 0.0, 300.0, 200.0);
        ChartRect newContent = new ChartRect(5.0, 5.0, 290.0, 190.0);
        ChartRect newPlot = new ChartRect(15.0, 15.0, 270.0, 170.0);
        ChartLayoutItem replacement = new ChartLayoutItem(ChartLayoutItemKind.Content, "replacement", newContent, ChartSide.Left);
        result.SetFigureRect(newFigure);
        result.SetContentRect(newContent);
        result.SetPlotArea(newPlot);
        result.SetItems(new ChartLayoutItem[] { replacement });
        result.SetItemCount(1);

        Assert.That(result.GetFigureRect(), Is.SameAs(newFigure));
        Assert.That(result.GetContentRect(), Is.SameAs(newContent));
        Assert.That(result.GetPlotArea(), Is.SameAs(newPlot));
        Assert.That(result.GetItems()[0], Is.SameAs(replacement));
        Assert.That(result.GetItemCount(), Is.EqualTo(1));
        Assert.That(result.ItemAt(0), Is.SameAs(replacement));
    }

    [Test]
    public void LayoutResult_DefaultDiagnosticOverlayAddsRetainedCommands()
    {
        ChartLayoutResult result = new ChartLayoutResult(
            new ChartRect(0.0, 0.0, 100.0, 80.0),
            new ChartRect(5.0, 5.0, 90.0, 70.0),
            new ChartRect(10.0, 10.0, 80.0, 60.0));
        result.AddItem(ChartLayoutItemKind.Plot, "main", new ChartRect(10.0, 10.0, 80.0, 60.0), ChartSide.Top);
        ChartRenderScene scene = new ChartRenderScene(100, 80);

        result.AddDiagnosticOverlay(scene);

        Assert.That(scene.GetCommandCount(), Is.EqualTo(2));
        Assert.That(scene.CommandAt(0).GetId(), Is.EqualTo("layout-debug-main-bounds"));
        Assert.That(scene.CommandAt(1).GetId(), Is.EqualTo("layout-debug-main-label"));
        Assert.That(result.ToTraceString(), Does.Contain("Plot id=main"));
        Assert.Throws<System.ArgumentNullException>(() => result.AddDiagnosticOverlay(null!));
    }

    [Test]
    public void GridAccessors_UpdateCellGeometryAndCopyIndependently()
    {
        ChartGrid grid = new ChartGrid(2, 2);
        grid.SetRows(3);
        grid.SetColumns(4);
        grid.SetRowGap(5.0);
        grid.SetColumnGap(10.0);

        Assert.That(grid.GetRows(), Is.EqualTo(3));
        Assert.That(grid.GetColumns(), Is.EqualTo(4));
        Assert.That(grid.GetRowGap(), Is.EqualTo(5.0));
        Assert.That(grid.GetColumnGap(), Is.EqualTo(10.0));
        Assert.That(grid.CellRect(new ChartRect(0.0, 0.0, 100.0, 70.0), 1, 2).GetX(), Is.EqualTo(55.0));

        ChartGrid copy = grid.Copy();
        grid.SetColumnGap(20.0);
        Assert.That(copy.GetColumnGap(), Is.EqualTo(10.0));
    }
}
