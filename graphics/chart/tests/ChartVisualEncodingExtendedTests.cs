namespace chart.Tests;

[TestFixture]
public class ChartVisualEncodingExtendedTests
{
    [Test]
    public void VisualEncoding_BindsEveryExtendedChannel()
    {
        ChartVisualEncoding encoding = new ChartVisualEncoding();

        encoding.SetRadius("radius", ChartDataKind.Number);
        encoding.SetAngle("angle", ChartDataKind.Number);
        encoding.SetLatitude("latitude", ChartDataKind.Number);
        encoding.SetLongitude("longitude", ChartDataKind.Number);
        encoding.SetStrokeColor("stroke", ChartDataKind.Category);
        encoding.SetFillColor("fill", ChartDataKind.Category);
        encoding.SetStrokeWidth("width", ChartDataKind.Number);
        encoding.SetDash("dash", ChartDataKind.Number);
        encoding.SetValueLabel("label", ChartDataKind.Text);
        encoding.SetZIndex("z", ChartDataKind.Number);
        encoding.SetWrap("facet", ChartDataKind.Category);

        Assert.That(encoding.GetBindingCount(), Is.EqualTo(11));
        Assert.That(encoding.BindingFor(ChartEncodingChannel.Radius).GetFieldName(), Is.EqualTo("radius"));
        Assert.That(encoding.BindingFor(ChartEncodingChannel.Wrap).GetFieldName(), Is.EqualTo("facet"));
        Assert.That(ChartEncodingBinding.ChannelName(ChartEncodingChannel.StrokeColor), Is.EqualTo("strokeColor"));
        Assert.That(ChartEncodingBinding.ChannelName(ChartEncodingChannel.ValueLabel), Is.EqualTo("valueLabel"));
        Assert.That(ChartEncodingBinding.ChannelName(ChartEncodingChannel.ZIndex), Is.EqualTo("zIndex"));
        Assert.That(ChartEncodingBinding.ChannelName(ChartEncodingChannel.Wrap), Is.EqualTo("wrap"));
    }

    [Test]
    public void DeclarativeScatter_ResolvesPolarAndGeographicPositions()
    {
        ChartDeclarativeCharts charts = new ChartDeclarativeCharts();
        ChartDataTable table = new ChartDataTable();
        table.AddNumberColumn("radius", new double[] { 1.0, 2.0 });
        table.AddNumberColumn("angle", new double[] { 0.0, System.Math.PI / 2.0 });
        table.AddNumberColumn("latitude", new double[] { 10.0, 20.0 });
        table.AddNumberColumn("longitude", new double[] { 30.0, 40.0 });
        ChartVisualEncoding polar = new ChartVisualEncoding();
        polar.SetRadius("radius", ChartDataKind.Number);
        polar.SetAngle("angle", ChartDataKind.Number);
        ChartVisualEncoding geographic = new ChartVisualEncoding();
        geographic.SetLatitude("latitude", ChartDataKind.Number);
        geographic.SetLongitude("longitude", ChartDataKind.Number);
        ChartRenderStyle style = ChartRenderStyle.CreateFill(ChartColor.Black());

        ChartCartesianSeries polarSeries = charts.CreateScatterSeries("polar", table, polar, style);
        ChartCartesianSeries geographicSeries = charts.CreateScatterSeries("geo", table, geographic, style);

        Assert.That(polarSeries.CommandAt(0).GetX1(), Is.EqualTo(1.0).Within(0.0000001));
        Assert.That(polarSeries.CommandAt(0).GetY1(), Is.EqualTo(0.0).Within(0.0000001));
        Assert.That(polarSeries.CommandAt(1).GetX1(), Is.EqualTo(0.0).Within(0.0000001));
        Assert.That(polarSeries.CommandAt(1).GetY1(), Is.EqualTo(2.0).Within(0.0000001));
        Assert.That(geographicSeries.CommandAt(0).GetX1(), Is.EqualTo(30.0));
        Assert.That(geographicSeries.CommandAt(0).GetY1(), Is.EqualTo(10.0));
        Assert.That(geographicSeries.CommandAt(1).GetX1(), Is.EqualTo(40.0));
        Assert.That(geographicSeries.CommandAt(1).GetY1(), Is.EqualTo(20.0));
    }

    [Test]
    public void DeclarativeMarks_ApplyPerRowPaintWidthOpacityAndValueLabels()
    {
        ChartDeclarativeCharts charts = new ChartDeclarativeCharts();
        ChartDataTable table = new ChartDataTable();
        table.AddNumberColumn("x", new double[] { 1.0, 2.0 });
        table.AddNumberColumn("y", new double[] { 3.0, 4.0 });
        table.AddCategoryColumn("fill", new string?[] { "A", "B" });
        table.AddCategoryColumn("stroke", new string?[] { "cold", "hot" });
        table.AddNumberColumn("width", new double[] { 1.5, 3.5 });
        table.AddNumberColumn("opacity", new double[] { 0.25, 0.75 });
        table.AddTextColumn("label", new string?[] { "first", "second" });
        ChartVisualEncoding encoding = new ChartVisualEncoding();
        encoding.SetX("x", ChartDataKind.Number);
        encoding.SetY("y", ChartDataKind.Number);
        encoding.SetFillColor("fill", ChartDataKind.Category);
        encoding.SetStrokeColor("stroke", ChartDataKind.Category);
        encoding.SetStrokeWidth("width", ChartDataKind.Number);
        encoding.SetOpacity("opacity", ChartDataKind.Number);
        encoding.SetValueLabel("label", ChartDataKind.Text);
        ChartRenderStyle style = ChartRenderStyle.CreateFillStroke(ChartColor.White(), ChartColor.Black(), 1.0);

        ChartCartesianSeries series = charts.CreateScatterSeries("styled", table, encoding, style);

        Assert.That(series.GetCommandCount(), Is.EqualTo(4));
        ChartRenderStyle first = series.CommandAt(0).GetStyle();
        ChartRenderStyle second = series.CommandAt(1).GetStyle();
        Assert.That(first.GetFillColor().GetR(), Is.Not.EqualTo(second.GetFillColor().GetR()));
        Assert.That(first.GetStrokeColor().GetR(), Is.Not.EqualTo(second.GetStrokeColor().GetR()));
        Assert.That(first.GetStrokeWidth(), Is.EqualTo(1.5));
        Assert.That(second.GetStrokeWidth(), Is.EqualTo(3.5));
        Assert.That(first.GetOpacity(), Is.EqualTo(0.25));
        Assert.That(second.GetOpacity(), Is.EqualTo(0.75));
        Assert.That(series.CommandAt(2).GetKind(), Is.EqualTo(ChartRenderCommandKind.Text));
        Assert.That(series.CommandAt(2).GetText(), Is.EqualTo("first"));
        Assert.That(series.CommandAt(3).GetText(), Is.EqualTo("second"));
    }

    [Test]
    public void DashAndZIndex_ChangeRetainedGeometryAndRenderingOrder()
    {
        ChartDeclarativeCharts charts = new ChartDeclarativeCharts();
        ChartDataTable table = new ChartDataTable();
        table.AddNumberColumn("x", new double[] { 0.0, 10.0 });
        table.AddNumberColumn("y", new double[] { 0.0, 0.0 });
        ChartVisualEncoding highEncoding = new ChartVisualEncoding();
        highEncoding.SetX("x", ChartDataKind.Number);
        highEncoding.SetY("y", ChartDataKind.Number);
        highEncoding.SetConstantNumber(ChartEncodingChannel.Dash, 2.0);
        highEncoding.SetConstantNumber(ChartEncodingChannel.StrokeWidth, 3.0);
        highEncoding.SetConstantColor(ChartEncodingChannel.StrokeColor, ChartColor.FromRgb(200, 10, 10));
        highEncoding.SetConstantNumber(ChartEncodingChannel.ZIndex, 5.0);
        ChartVisualEncoding lowEncoding = new ChartVisualEncoding();
        lowEncoding.SetX("x", ChartDataKind.Number);
        lowEncoding.SetY("y", ChartDataKind.Number);
        lowEncoding.SetConstantNumber(ChartEncodingChannel.ZIndex, 0.0);
        ChartRenderStyle style = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);

        ChartCartesianSeries high = charts.CreateLineSeries("high", table, highEncoding, style);
        ChartCartesianSeries low = charts.CreateLineSeries("low", table, lowEncoding, style);

        Assert.That(high.GetCommandCount(), Is.EqualTo(3));
        Assert.That(high.CommandAt(0).GetX1(), Is.EqualTo(0.0));
        Assert.That(high.CommandAt(0).GetX2(), Is.EqualTo(2.0));
        Assert.That(high.CommandAt(1).GetX1(), Is.EqualTo(4.0));
        Assert.That(high.CommandAt(2).GetX2(), Is.EqualTo(10.0));
        Assert.That(high.CommandAt(0).GetStyle().GetStrokeWidth(), Is.EqualTo(3.0));
        Assert.That(high.GetOrder(), Is.EqualTo(5));

        ChartCartesianRenderer renderer = new ChartCartesianRenderer(new ChartCartesianTransform(new ChartRect(0.0, 0.0, 100.0, 100.0), new ChartRange(0.0, 10.0), new ChartRange(-1.0, 1.0)));
        renderer.AddSeries(high);
        renderer.AddSeries(low);
        ChartRenderScene scene = renderer.Render(100, 100);
        Assert.That(scene.CommandAt(0).GetId(), Is.EqualTo("low-line-segment-0"));
    }

    [Test]
    public void WrapFacetChannel_SplitsRowsInStableFirstObservedOrder()
    {
        ChartDeclarativeCharts charts = new ChartDeclarativeCharts();
        ChartDataTable table = new ChartDataTable();
        table.AddNumberColumn("x", new double[] { 0.0, 1.0, 2.0 });
        table.AddNumberColumn("y", new double[] { 10.0, 20.0, 30.0 });
        table.AddCategoryColumn("facet", new string?[] { "A", "B", "A" });
        ChartVisualEncoding encoding = new ChartVisualEncoding();
        encoding.SetX("x", ChartDataKind.Number);
        encoding.SetY("y", ChartDataKind.Number);
        encoding.SetWrap("facet", ChartDataKind.Category);

        ChartCartesianSeries[] facets = charts.CreateWrapFacetSeries("points", ChartDeclarativeMark.Scatter, table, encoding, ChartRenderStyle.CreateFill(ChartColor.Black()));

        Assert.That(facets.Length, Is.EqualTo(2));
        Assert.That(facets[0].GetId(), Is.EqualTo("points-wrap-0"));
        Assert.That(facets[0].GetCommandCount(), Is.EqualTo(2));
        Assert.That(facets[0].CommandAt(0).GetX1(), Is.EqualTo(0.0));
        Assert.That(facets[0].CommandAt(1).GetX1(), Is.EqualTo(2.0));
        Assert.That(facets[1].GetCommandCount(), Is.EqualTo(1));
        Assert.That(facets[1].CommandAt(0).GetX1(), Is.EqualTo(1.0));
    }

    [Test]
    public void ExtendedChannels_ValidateUnsupportedValues()
    {
        ChartDeclarativeCharts charts = new ChartDeclarativeCharts();
        ChartDataTable polarTable = new ChartDataTable();
        polarTable.AddNumberColumn("radius", new double[] { -1.0 });
        polarTable.AddNumberColumn("angle", new double[] { 0.0 });
        ChartVisualEncoding polar = new ChartVisualEncoding();
        polar.SetRadius("radius", ChartDataKind.Number);
        polar.SetAngle("angle", ChartDataKind.Number);
        Assert.Throws<System.ArgumentOutOfRangeException>(() => charts.CreateScatterSeries("bad", polarTable, polar, ChartRenderStyle.CreateFill(ChartColor.Black())));

        ChartDataTable lineTable = new ChartDataTable();
        lineTable.AddNumberColumn("x", new double[] { 0.0, 1.0 });
        lineTable.AddNumberColumn("y", new double[] { 0.0, 1.0 });
        ChartVisualEncoding dash = new ChartVisualEncoding();
        dash.SetX("x", ChartDataKind.Number);
        dash.SetY("y", ChartDataKind.Number);
        dash.SetConstantNumber(ChartEncodingChannel.Dash, 0.0);
        Assert.Throws<System.ArgumentOutOfRangeException>(() => charts.CreateLineSeries("bad-dash", lineTable, dash, ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0)));

        ChartVisualEncoding wrap = new ChartVisualEncoding();
        wrap.SetX("x", ChartDataKind.Number);
        wrap.SetY("y", ChartDataKind.Number);
        wrap.SetConstantText(ChartEncodingChannel.Wrap, "same");
        Assert.Throws<System.ArgumentException>(() => charts.CreateWrapFacetSeries("bad-wrap", ChartDeclarativeMark.Scatter, lineTable, wrap, ChartRenderStyle.CreateFill(ChartColor.Black())));
    }
}
