namespace chart.Tests;

[TestFixture]
public class ChartDeclarativeChartsTests
{
    [Test]
    public void DeclarativeLineAreaAndCreateSeries_DelegateToDirectSeries()
    {
        ChartDataTable table = CreateTable();
        ChartVisualEncoding lineEncoding = new ChartVisualEncoding();
        ChartVisualEncoding areaEncoding = new ChartVisualEncoding();
        ChartRenderStyle lineStyle = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);
        ChartRenderStyle areaStyle = ChartRenderStyle.CreateFill(ChartColor.FromRgb(200, 220, 240));

        lineEncoding.SetX("x", ChartDataKind.Number);
        lineEncoding.SetY("y", ChartDataKind.Number);
        areaEncoding.SetX("x", ChartDataKind.Number);
        areaEncoding.SetY("high", ChartDataKind.Number);
        areaEncoding.SetY2("low", ChartDataKind.Number);

        ChartCartesianSeries line = ChartDeclarativeChartsOperations.CreateLineSeries("line", table, lineEncoding, lineStyle);
        ChartCartesianSeries viaMark = ChartDeclarativeChartsOperations.CreateSeries("line-mark", ChartDeclarativeMark.Line, table, lineEncoding, lineStyle);
        ChartCartesianSeries area = ChartDeclarativeChartsOperations.CreateAreaSeries("area", table, areaEncoding, areaStyle);

        Assert.That(line.Kind, Is.EqualTo(ChartCartesianSeriesKind.Line));
        Assert.That(line.CommandCount, Is.EqualTo(1));
        AssertPoint(line.CommandAt(0).Points[0], 0.0, 2.0);
        AssertPoint(line.CommandAt(0).Points[2], 2.0, 5.0);
        Assert.That(viaMark.Kind, Is.EqualTo(ChartCartesianSeriesKind.Line));
        Assert.That(area.Kind, Is.EqualTo(ChartCartesianSeriesKind.Area));
        Assert.That(area.CommandAt(0).Points.Length, Is.EqualTo(6));
        AssertPoint(area.CommandAt(0).Points[0], 0.0, 3.0);
        AssertPoint(area.CommandAt(0).Points[3], 2.0, 2.5);
    }

    [Test]
    public void DeclarativeScatterBubbleBarsAndRangeBars_CreateExpectedCommands()
    {
        ChartDataTable table = CreateTable();
        ChartVisualEncoding scatterEncoding = XYEncoding();
        scatterEncoding.SetConstantNumber(ChartEncodingChannel.Size, 7.0);
        scatterEncoding.SetConstantShape(ChartEncodingChannel.Shape, ChartMarkerShape.Diamond);
        ChartVisualEncoding bubbleEncoding = XYEncoding();
        bubbleEncoding.SetSize("size", ChartDataKind.Number);
        ChartVisualEncoding barEncoding = XYEncoding();
        barEncoding.SetConstantNumber(ChartEncodingChannel.Size, 0.5);
        barEncoding.SetConstantNumber(ChartEncodingChannel.Y2, -1.0);
        barEncoding.SetConstantNumber(ChartEncodingChannel.X2, -1.0);
        ChartVisualEncoding rangeEncoding = new ChartVisualEncoding();
        rangeEncoding.SetX("x", ChartDataKind.Number);
        rangeEncoding.SetY("low", ChartDataKind.Number);
        rangeEncoding.SetY2("high", ChartDataKind.Number);
        rangeEncoding.SetConstantNumber(ChartEncodingChannel.Size, 0.4);
        ChartRenderStyle style = ChartRenderStyle.CreateFill(ChartColor.Black());

        ChartCartesianSeries scatter = ChartDeclarativeChartsOperations.CreateScatterSeries("scatter", table, scatterEncoding, style);
        ChartCartesianSeries bubble = ChartDeclarativeChartsOperations.CreateBubbleSeries("bubble", table, bubbleEncoding, 2.0, 8.0, style);
        ChartCartesianSeries verticalBar = ChartDeclarativeChartsOperations.CreateVerticalBarSeries("bar", table, barEncoding, style);
        ChartCartesianSeries horizontalBar = ChartDeclarativeChartsOperations.CreateHorizontalBarSeries("hbar", table, barEncoding, style);
        ChartCartesianSeries range = ChartDeclarativeChartsOperations.CreateRangeBarSeries("range", table, rangeEncoding, style);

        Assert.That(scatter.CommandAt(0).MarkerShape, Is.EqualTo(ChartMarkerShape.Diamond));
        Assert.That(scatter.CommandAt(0).MarkerSize, Is.EqualTo(7.0).Within(0.0000001));
        Assert.That(bubble.CommandCount, Is.EqualTo(3));
        Assert.That(bubble.CommandAt(0).MarkerSize, Is.EqualTo(2.0).Within(0.0000001));
        Assert.That(bubble.CommandAt(2).MarkerSize, Is.EqualTo(8.0).Within(0.0000001));
        AssertPoint(verticalBar.CommandAt(0).Points[0], -0.25, -1.0);
        AssertPoint(verticalBar.CommandAt(0).Points[1], -0.25, 2.0);
        AssertPoint(horizontalBar.CommandAt(0).Points[0], -1.0, 1.75);
        AssertPoint(horizontalBar.CommandAt(0).Points[1], 0.0, 1.75);
        AssertPoint(range.CommandAt(0).Points[0], -0.2, 1.0);
        AssertPoint(range.CommandAt(0).Points[1], -0.2, 3.0);
    }

    [Test]
    public void DeclarativeHelpers_ResolveConstantsAndFallbacks()
    {
        ChartDataTable table = CreateTable();
        ChartVisualEncoding encoding = new ChartVisualEncoding();
        encoding.SetX("x", ChartDataKind.Number);
        encoding.SetConstantNumber(ChartEncodingChannel.Y, 4.0);

        double[] yValues = ChartDeclarativeChartsOperations.NumberValues(table, encoding, ChartEncodingChannel.Y);
        double[] y2Values = ChartDeclarativeChartsOperations.NumberValuesOrConstant(table, encoding, ChartEncodingChannel.Y2, 2.0);
        double markerSize = ChartDeclarativeChartsOperations.NumberConstantOrDefault(encoding, ChartEncodingChannel.Size, 6.0);
        ChartMarkerShape shape = ChartDeclarativeChartsOperations.ShapeConstantOrDefault(encoding, ChartEncodingChannel.Shape, ChartMarkerShape.Circle);

        Assert.That(yValues, Is.EqualTo(new double[] { 4.0, 4.0, 4.0 }));
        Assert.That(y2Values, Is.EqualTo(new double[] { 2.0, 2.0, 2.0 }));
        Assert.That(markerSize, Is.EqualTo(6.0));
        Assert.That(shape, Is.EqualTo(ChartMarkerShape.Circle));
    }

    [Test]
    public void DeclarativeCharts_ValidateInputs()
    {
        ChartDataTable table = CreateTable();
        ChartVisualEncoding encoding = XYEncoding();
        ChartRenderStyle style = ChartRenderStyle.CreateFill(ChartColor.Black());
        ChartVisualEncoding missingY = new ChartVisualEncoding();
        missingY.SetX("x", ChartDataKind.Number);
        ChartVisualEncoding wrongKind = new ChartVisualEncoding();
        wrongKind.SetX("label", ChartDataKind.Text);
        wrongKind.SetY("y", ChartDataKind.Number);
        ChartVisualEncoding sizeField = XYEncoding();
        sizeField.SetSize("size", ChartDataKind.Number);
        ChartVisualEncoding shapeField = XYEncoding();
        shapeField.SetShape("label", ChartDataKind.Text);
        ChartDataTable empty = new ChartDataTable();

        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartDeclarativeChartsOperations.CreateSeries("bad", (ChartDeclarativeMark)99, table, encoding, style));
        Assert.Throws<System.ArgumentNullException>(() => ChartDeclarativeChartsOperations.CreateLineSeries("line", null!, encoding, style));
        Assert.Throws<System.ArgumentNullException>(() => ChartDeclarativeChartsOperations.CreateLineSeries("line", table, null!, style));
        Assert.Throws<System.ArgumentNullException>(() => ChartDeclarativeChartsOperations.CreateLineSeries("line", table, encoding, null!));
        Assert.Throws<System.ArgumentException>(() => ChartDeclarativeChartsOperations.CreateLineSeries("line", empty, encoding, style));
        Assert.Throws<System.ArgumentException>(() => ChartDeclarativeChartsOperations.CreateLineSeries("line", table, missingY, style));
        Assert.Throws<System.ArgumentException>(() => ChartDeclarativeChartsOperations.CreateLineSeries("line", table, wrongKind, style));
        Assert.Throws<System.ArgumentException>(() => ChartDeclarativeChartsOperations.CreateScatterSeries("scatter", table, sizeField, style));
        Assert.Throws<System.ArgumentException>(() => ChartDeclarativeChartsOperations.CreateScatterSeries("scatter", table, shapeField, style));
        Assert.Throws<System.ArgumentException>(() => ChartDeclarativeChartsOperations.CreateBubbleSeries("bubble", table, encoding, 2.0, 8.0, style));
        Assert.Throws<System.ArgumentException>(() => ChartDeclarativeChartsOperations.CreateRangeBarSeries("range", table, encoding, style));
        Assert.Throws<System.ArgumentNullException>(() => ChartDeclarativeChartsOperations.RequireChannel(null!, ChartEncodingChannel.X));
        Assert.Throws<System.ArgumentException>(() => ChartDeclarativeChartsOperations.RequireChannel(missingY, ChartEncodingChannel.Y));
    }

    static ChartDataTable CreateTable()
    {
        ChartDataTable table = new ChartDataTable();
        table.AddNumberColumn("x", new double[] { 0.0, 1.0, 2.0 });
        table.AddNumberColumn("y", new double[] { 2.0, 4.0, 5.0 });
        table.AddNumberColumn("low", new double[] { 1.0, 2.0, 2.5 });
        table.AddNumberColumn("high", new double[] { 3.0, 5.0, 6.0 });
        table.AddNumberColumn("size", new double[] { 1.0, 4.0, 9.0 });
        table.AddTextColumn("label", new string[] { "A", "B", "C" });
        return table;
    }

    static ChartVisualEncoding XYEncoding()
    {
        ChartVisualEncoding encoding = new ChartVisualEncoding();
        encoding.SetX("x", ChartDataKind.Number);
        encoding.SetY("y", ChartDataKind.Number);
        return encoding;
    }

    static void AssertPoint(ChartPoint point, double x, double y)
    {
        Assert.That(point.X, Is.EqualTo(x).Within(0.0000001));
        Assert.That(point.Y, Is.EqualTo(y).Within(0.0000001));
    }
}
