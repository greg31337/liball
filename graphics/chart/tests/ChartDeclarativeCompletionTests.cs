namespace chart.Tests;

[TestFixture]
public class ChartDeclarativeCompletionTests
{
    [Test]
    public void DeclarativeDispatcher_CreatesExtendedCartesianMarks()
    {
        ChartDeclarativeCharts charts = new ChartDeclarativeCharts();
        ChartDataTable table = MarkTable();
        ChartVisualEncoding encoding = FullEncoding();
        ChartRenderStyle fill = ChartRenderStyle.CreateFill(ChartColor.FromRgb(20, 80, 160));
        ChartRenderStyle stroke = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);

        ChartCartesianSeries point = charts.CreateSeries("point", ChartDeclarativeMark.Point, table, encoding, fill);
        ChartCartesianSeries circle = charts.CreateSeries("circle", ChartDeclarativeMark.Circle, table, encoding, fill);
        ChartCartesianSeries square = charts.CreateSeries("square", ChartDeclarativeMark.Square, table, encoding, fill);
        ChartCartesianSeries rect = charts.CreateSeries("rect", ChartDeclarativeMark.Rect, table, encoding, fill);
        ChartCartesianSeries rule = charts.CreateSeries("rule", ChartDeclarativeMark.Rule, table, encoding, stroke);
        ChartCartesianSeries text = charts.CreateSeries("text", ChartDeclarativeMark.Text, table, encoding, fill);
        ChartCartesianSeries tick = charts.CreateSeries("tick", ChartDeclarativeMark.Tick, table, encoding, stroke);
        ChartCartesianSeries trail = charts.CreateSeries("trail", ChartDeclarativeMark.Trail, table, encoding, stroke);
        ChartCartesianSeries step = charts.CreateSeries("step", ChartDeclarativeMark.StepLine, table, encoding, stroke);
        ChartCartesianSeries spline = charts.CreateSeries("spline", ChartDeclarativeMark.SplineLine, table, encoding, stroke);
        ChartCartesianSeries rangeArea = charts.CreateSeries("range-area", ChartDeclarativeMark.RangeArea, table, encoding, fill);
        ChartCartesianSeries lollipop = charts.CreateSeries("lollipop", ChartDeclarativeMark.Lollipop, table, encoding, fill);
        ChartCartesianSeries stem = charts.CreateSeries("stem", ChartDeclarativeMark.Stem, table, encoding, stroke);

        Assert.That(point.CommandAt(0).GetMarkerShape(), Is.EqualTo(ChartMarkerShape.Diamond));
        Assert.That(circle.CommandAt(0).GetMarkerShape(), Is.EqualTo(ChartMarkerShape.Circle));
        Assert.That(square.CommandAt(0).GetMarkerShape(), Is.EqualTo(ChartMarkerShape.Square));
        Assert.That(rect.GetCommandCount(), Is.EqualTo(3));
        Assert.That(rect.CommandAt(0).GetKind(), Is.EqualTo(ChartRenderCommandKind.Polygon));
        Assert.That(rule.GetCommandCount(), Is.EqualTo(3));
        Assert.That(text.CommandAt(0).GetText(), Is.EqualTo("one"));
        Assert.That(tick.GetCommandCount(), Is.EqualTo(3));
        Assert.That(trail.GetCommandCount(), Is.EqualTo(2));
        Assert.That(trail.CommandAt(0).GetStyle().GetStrokeWidth(), Is.EqualTo(2.0));
        Assert.That(step.CommandAt(0).GetPoints().Length, Is.GreaterThan(3));
        Assert.That(spline.CommandAt(0).GetPoints().Length, Is.GreaterThan(3));
        Assert.That(rangeArea.CommandAt(0).GetPoints().Length, Is.EqualTo(6));
        Assert.That(lollipop.GetCommandCount(), Is.EqualTo(6));
        Assert.That(stem.GetCommandCount(), Is.EqualTo(3));
    }

    [Test]
    public void OrderChannel_ReordersRowsAndPreservesConditionalSelectionState()
    {
        ChartDataTable table = new ChartDataTable();
        table.AddNumberColumn("x", new double[] { 0.0, 1.0, 2.0 });
        table.AddNumberColumn("y", new double[] { 10.0, 11.0, 12.0 });
        table.AddNumberColumn("order", new double[] { 3.0, 1.0, 2.0 });
        ChartSelection selection = new ChartSelection("ordered", "shared");
        selection.SetSelectedRows(new int[] { 0 });
        ChartVisualEncoding encoding = new ChartVisualEncoding();
        encoding.SetX("x", ChartDataKind.Number);
        encoding.SetY("y", ChartDataKind.Number);
        encoding.SetOrder("order", ChartDataKind.Number);
        encoding.SetConstantColor(ChartEncodingChannel.FillColor, ChartColor.FromRgb(100, 100, 100));
        encoding.SetSelection(selection);
        ChartConditionalEncoding condition = new ChartConditionalEncoding(ChartEncodingChannel.FillColor);
        ChartEncodingBindingFactory bindings = new ChartEncodingBindingFactory();
        condition.SetUnselectedBinding(bindings.CreateColorConstant(ChartEncodingChannel.FillColor, ChartColor.FromRgb(100, 100, 100)));
        condition.SetSelectedBinding(bindings.CreateColorConstant(ChartEncodingChannel.FillColor, ChartColor.FromRgb(240, 20, 20)));
        encoding.SetConditionalEncoding(condition);

        ChartCartesianSeries series = new ChartDeclarativeCharts().CreateSeries("ordered", ChartDeclarativeMark.Point, table, encoding, ChartRenderStyle.CreateFill(ChartColor.Black()));

        Assert.That(series.CommandAt(0).GetX1(), Is.EqualTo(1.0));
        Assert.That(series.CommandAt(1).GetX1(), Is.EqualTo(2.0));
        Assert.That(series.CommandAt(2).GetX1(), Is.EqualTo(0.0));
        Assert.That(series.CommandAt(0).GetStyle().GetFillColor().GetR(), Is.EqualTo(100));
        Assert.That(series.CommandAt(2).GetStyle().GetFillColor().GetR(), Is.EqualTo(240));
    }

    [Test]
    public void FacetGrouping_RemapConditionalRowsToEachSubset()
    {
        ChartDataTable table = new ChartDataTable();
        table.AddNumberColumn("x", new double[] { 0.0, 1.0, 2.0 });
        table.AddNumberColumn("y", new double[] { 3.0, 4.0, 5.0 });
        table.AddCategoryColumn("facet", new string?[] { "A", "B", "A" });
        ChartSelection selection = new ChartSelection("faceted", "shared");
        selection.SetSelectedRows(new int[] { 2 });
        ChartVisualEncoding encoding = new ChartVisualEncoding();
        encoding.SetX("x", ChartDataKind.Number);
        encoding.SetY("y", ChartDataKind.Number);
        encoding.SetWrap("facet", ChartDataKind.Category);
        encoding.SetConstantColor(ChartEncodingChannel.FillColor, ChartColor.FromRgb(80, 80, 80));
        encoding.SetSelection(selection);
        ChartConditionalEncoding condition = new ChartConditionalEncoding(ChartEncodingChannel.FillColor);
        ChartEncodingBindingFactory bindings = new ChartEncodingBindingFactory();
        condition.SetSelectedBinding(bindings.CreateColorConstant(ChartEncodingChannel.FillColor, ChartColor.FromRgb(230, 10, 10)));
        encoding.SetConditionalEncoding(condition);

        ChartCartesianSeries[] facets = new ChartDeclarativeCharts().CreateWrapFacetSeries("facet", ChartDeclarativeMark.Point, table, encoding, ChartRenderStyle.CreateFill(ChartColor.Black()));

        Assert.That(facets[0].GetCommandCount(), Is.EqualTo(2));
        Assert.That(facets[0].CommandAt(0).GetStyle().GetFillColor().GetR(), Is.EqualTo(80));
        Assert.That(facets[0].CommandAt(1).GetStyle().GetFillColor().GetR(), Is.EqualTo(230));
    }

    [Test]
    public void DeclarativeView_GroupsLayersAcrossRowAndColumnFacetsWithSharedGuide()
    {
        ChartDataTable table = FacetTable();
        ChartVisualEncoding encoding = new ChartVisualEncoding();
        encoding.SetX("x", ChartDataKind.Number);
        encoding.SetY("y", ChartDataKind.Number);
        encoding.SetDetail("series", ChartDataKind.Category);
        encoding.SetRow("row", ChartDataKind.Category);
        encoding.SetColumn("column", ChartDataKind.Category);
        ChartDeclarativeLayer points = new ChartDeclarativeLayer("points", ChartDeclarativeMark.Point, table, encoding, ChartRenderStyle.CreateFill(ChartColor.Black()));
        ChartDeclarativeLayer ticks = new ChartDeclarativeLayer("ticks", ChartDeclarativeMark.Tick, table, encoding, ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0));

        ChartDeclarativeView view = new ChartDeclarativeCharts().CreateView(new ChartDeclarativeLayer[] { points, ticks });

        Assert.That(view.Series.Length, Is.EqualTo(16));
        Assert.That(view.LayerIndices[0], Is.EqualTo(0));
        Assert.That(view.LayerIndices[8], Is.EqualTo(1));
        Assert.That(view.RowFacetKeys, Does.Contain("R1"));
        Assert.That(view.RowFacetKeys, Does.Contain("R2"));
        Assert.That(view.ColumnFacetKeys, Does.Contain("C1"));
        Assert.That(view.ColumnFacetKeys, Does.Contain("C2"));
        Assert.That(view.GuideLabels, Is.EqualTo(new string[] { "A", "B" }));
        Assert.That(view.GuideColors.Length, Is.EqualTo(2));
        Assert.That(view.Series[0].CommandAt(0).GetStyle().GetFillColor().EqualsColor(view.GuideColors[0]), Is.True);
        Assert.That(view.Series[1].CommandAt(0).GetStyle().GetFillColor().EqualsColor(view.GuideColors[1]), Is.True);
    }

    [Test]
    public void DeclarativeView_StacksOrderedDetailGroups()
    {
        ChartDataTable table = new ChartDataTable();
        table.AddNumberColumn("x", new double[] { 1.0, 0.0, 1.0, 0.0 });
        table.AddNumberColumn("value", new double[] { 2.0, 1.0, 4.0, 3.0 });
        table.AddNumberColumn("order", new double[] { 1.0, 0.0, 1.0, 0.0 });
        table.AddCategoryColumn("series", new string?[] { "A", "A", "B", "B" });
        ChartVisualEncoding encoding = new ChartVisualEncoding();
        encoding.SetX("x", ChartDataKind.Number);
        encoding.SetY("value", ChartDataKind.Number);
        encoding.SetOrder("order", ChartDataKind.Number);
        encoding.SetDetail("series", ChartDataKind.Category);
        encoding.SetConstantNumber(ChartEncodingChannel.Size, 0.5);
        ChartDeclarativeLayer layer = new ChartDeclarativeLayer("stacked", ChartDeclarativeMark.VerticalBar, table, encoding, ChartRenderStyle.CreateFill(ChartColor.Black()));
        layer.StackMode = ChartStackMode.Zero;

        ChartDeclarativeView view = new ChartDeclarativeCharts().CreateView(new ChartDeclarativeLayer[] { layer });

        Assert.That(view.Series.Length, Is.EqualTo(2));
        Assert.That(view.Series[0].GetId(), Is.EqualTo("stacked-group-0"));
        Assert.That(view.Series[1].GetId(), Is.EqualTo("stacked-group-1"));
        ChartPoint[] firstB = view.Series[1].CommandAt(0).GetPoints();
        ChartPoint[] secondB = view.Series[1].CommandAt(1).GetPoints();
        Assert.That(firstB[0].GetY(), Is.EqualTo(1.0));
        Assert.That(firstB[1].GetY(), Is.EqualTo(4.0));
        Assert.That(secondB[0].GetY(), Is.EqualTo(2.0));
        Assert.That(secondB[1].GetY(), Is.EqualTo(6.0));
    }

    [Test]
    public void DeclarativeView_ValidatesLayersAndStackCompatibility()
    {
        ChartDeclarativeCharts charts = new ChartDeclarativeCharts();
        ChartDataTable table = FacetTable();
        ChartVisualEncoding encoding = new ChartVisualEncoding();
        encoding.SetX("x", ChartDataKind.Number);
        encoding.SetY("y", ChartDataKind.Number);
        encoding.SetDetail("series", ChartDataKind.Category);
        ChartDeclarativeLayer layer = new ChartDeclarativeLayer("points", ChartDeclarativeMark.Point, table, encoding, ChartRenderStyle.CreateFill(ChartColor.Black()));
        layer.StackMode = ChartStackMode.Zero;

        Assert.Throws<System.ArgumentNullException>(() => charts.CreateView(null!));
        Assert.Throws<System.ArgumentException>(() => charts.CreateView(new ChartDeclarativeLayer[0]));
        Assert.Throws<System.ArgumentException>(() => charts.CreateView(new ChartDeclarativeLayer[] { null! }));
        Assert.Throws<System.ArgumentException>(() => charts.CreateView(new ChartDeclarativeLayer[] { layer }));
    }

    static ChartDataTable MarkTable()
    {
        ChartDataTable table = new ChartDataTable();
        table.AddNumberColumn("x", new double[] { 0.0, 1.0, 2.0 });
        table.AddNumberColumn("y", new double[] { 2.0, 4.0, 3.0 });
        table.AddNumberColumn("x2", new double[] { 0.5, 1.5, 2.5 });
        table.AddNumberColumn("y2", new double[] { 1.0, 2.0, 1.5 });
        table.AddNumberColumn("size", new double[] { 2.0, 4.0, 6.0 });
        table.AddTextColumn("text", new string?[] { "one", "two", "three" });
        return table;
    }

    static ChartVisualEncoding FullEncoding()
    {
        ChartVisualEncoding encoding = new ChartVisualEncoding();
        encoding.SetX("x", ChartDataKind.Number);
        encoding.SetY("y", ChartDataKind.Number);
        encoding.SetX2("x2", ChartDataKind.Number);
        encoding.SetY2("y2", ChartDataKind.Number);
        encoding.SetText("text", ChartDataKind.Text);
        encoding.SetConstantNumber(ChartEncodingChannel.Size, 2.0);
        encoding.SetConstantShape(ChartEncodingChannel.Shape, ChartMarkerShape.Diamond);
        return encoding;
    }

    static ChartDataTable FacetTable()
    {
        ChartDataTable table = new ChartDataTable();
        table.AddNumberColumn("x", new double[] { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 });
        table.AddNumberColumn("y", new double[] { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0 });
        table.AddCategoryColumn("series", new string?[] { "A", "B", "A", "B", "A", "B", "A", "B" });
        table.AddCategoryColumn("row", new string?[] { "R1", "R1", "R1", "R1", "R2", "R2", "R2", "R2" });
        table.AddCategoryColumn("column", new string?[] { "C1", "C1", "C2", "C2", "C1", "C1", "C2", "C2" });
        return table;
    }
}
