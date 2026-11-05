namespace chart.Tests;

[TestFixture]
public class ChartStreamingDataTests
{
    [Test]
    public void DataTable_AppendUpdateAndInvalidatePreserveTypedColumns()
    {
        System.DateTime firstDate = new System.DateTime(2026, 1, 1);
        System.DateTime secondDate = new System.DateTime(2026, 1, 2);
        System.DateTime thirdDate = new System.DateTime(2026, 1, 3);
        ChartDataTable table = CreateStreamingTable(new double[] { 1.0, 2.0 }, new string?[] { "one", "two" }, new System.DateTime[] { firstDate, secondDate }, new string?[] { "A", "B" });
        ChartDataColumn originalNumberColumn = table.GetColumn("number");
        int initialVersion = table.GetVersion();
        ChartDataTable appended = CreateStreamingTable(new double[] { 3.0 }, new string?[] { null }, new System.DateTime[] { thirdDate }, new string?[] { "A" });

        table.AppendRows(appended);

        Assert.That(table.GetRowCount(), Is.EqualTo(3));
        Assert.That(table.GetColumn("number"), Is.SameAs(originalNumberColumn));
        Assert.That(table.GetColumn("number").CopyNumberValues(), Is.EqualTo(new double[] { 1.0, 2.0, 3.0 }));
        Assert.That(table.GetColumn("text").CopyTextValues(), Is.EqualTo(new string?[] { "one", "two", null }));
        Assert.That(table.GetColumn("date").CopyDateTimeValues(), Is.EqualTo(new System.DateTime[] { firstDate, secondDate, thirdDate }));
        Assert.That(table.GetColumn("category").CopyCategoryOrder(), Is.EqualTo(new string?[] { "A", "B" }));
        Assert.That(table.GetVersion(), Is.GreaterThan(initialVersion));

        ChartDataTable replacement = CreateStreamingTable(new double[] { 20.0 }, new string?[] { "updated" }, new System.DateTime[] { thirdDate }, new string?[] { "C" });
        int appendVersion = table.GetVersion();
        table.UpdateRows(1, replacement);

        Assert.That(table.GetColumn("number").CopyNumberValues(), Is.EqualTo(new double[] { 1.0, 20.0, 3.0 }));
        Assert.That(table.GetText("text", 1), Is.EqualTo("updated"));
        Assert.That(table.GetDateTime("date", 1), Is.EqualTo(thirdDate));
        Assert.That(table.GetColumn("category").CopyCategoryOrder(), Is.EqualTo(new string?[] { "A", "C" }));
        Assert.That(table.GetVersion(), Is.GreaterThan(appendVersion));

        int updateVersion = table.GetVersion();
        table.Invalidate();
        Assert.That(table.GetVersion(), Is.Not.EqualTo(updateVersion));
    }

    [Test]
    public void DataTable_StreamingMutationsValidateSchemaAndBoundsAtomically()
    {
        ChartDataTable table = new ChartDataTable();
        table.AddNumberColumn("value", new double[] { 1.0, 2.0 });
        ChartDataTable wrongName = new ChartDataTable();
        wrongName.AddNumberColumn("other", new double[] { 3.0 });
        ChartDataTable tooMany = new ChartDataTable();
        tooMany.AddNumberColumn("value", new double[] { 3.0, 4.0 });
        int version = table.GetVersion();

        Assert.Throws<System.ArgumentException>(() => table.AppendRows(wrongName));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => table.UpdateRows(1, tooMany));
        Assert.Throws<System.ArgumentNullException>(() => table.AppendRows(null!));
        Assert.That(table.GetColumn("value").CopyNumberValues(), Is.EqualTo(new double[] { 1.0, 2.0 }));
        Assert.That(table.GetVersion(), Is.EqualTo(version));

        ChartDataTable matrixTable = new ChartDataTable();
        matrixTable.AddMatrixColumn("matrix", new ChartDataMatrix(1, 1));
        ChartDataTable matrixRows = new ChartDataTable();
        matrixRows.AddMatrixColumn("matrix", new ChartDataMatrix(1, 1));
        Assert.Throws<System.InvalidOperationException>(() => matrixTable.AppendRows(matrixRows));
    }

    [Test]
    public void SeriesData_AppendUpdateAndInvalidateAllPresentChannels()
    {
        ChartSeriesData data = new ChartSeriesData(2);
        data.SetXValues(new double[] { 0.0, 1.0 });
        data.SetYValues(new double[] { 10.0, 20.0 });
        data.SetLabelValues(new string?[] { "a", "b" });
        data.SetGroupValues(new string?[] { "north", "south" });
        ChartSeriesData appended = new ChartSeriesData(1);
        appended.SetXValues(new double[] { 2.0 });
        appended.SetYValues(new double[] { 30.0 });
        appended.SetLabelValues(new string?[] { null });
        appended.SetGroupValues(new string?[] { "north" });
        int initialVersion = data.GetVersion();

        data.AppendData(appended);

        Assert.That(data.GetRowCount(), Is.EqualTo(3));
        Assert.That(data.CopyXValues(), Is.EqualTo(new double[] { 0.0, 1.0, 2.0 }));
        Assert.That(data.CopyYValues(), Is.EqualTo(new double[] { 10.0, 20.0, 30.0 }));
        Assert.That(data.CopyLabelValues(), Is.EqualTo(new string?[] { "a", "b", null }));
        Assert.That(data.GetVersion(), Is.GreaterThan(initialVersion));

        ChartSeriesData replacement = new ChartSeriesData(1);
        replacement.SetXValues(new double[] { 1.5 });
        replacement.SetYValues(new double[] { 25.0 });
        replacement.SetLabelValues(new string?[] { "updated" });
        replacement.SetGroupValues(new string?[] { "west" });
        data.UpdateData(1, replacement);

        Assert.That(data.CopyXValues(), Is.EqualTo(new double[] { 0.0, 1.5, 2.0 }));
        Assert.That(data.CopyYValues(), Is.EqualTo(new double[] { 10.0, 25.0, 30.0 }));
        Assert.That(data.CopyGroupValues(), Is.EqualTo(new string?[] { "north", "west", "north" }));
        int updateVersion = data.GetVersion();
        data.Invalidate();
        Assert.That(data.GetVersion(), Is.Not.EqualTo(updateVersion));

        ChartSeriesData wrongSchema = new ChartSeriesData(1);
        wrongSchema.SetXValues(new double[] { 3.0 });
        Assert.Throws<System.ArgumentException>(() => data.AppendData(wrongSchema));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => data.UpdateData(3, replacement));
    }

    [Test]
    public void DocumentRender_RefreshesStreamingLineAfterAppendUpdateAndInvalidation()
    {
        ChartDocument document = new ChartDocument(100.0, 100.0);
        ChartCartesianTransform transform = new ChartCartesianTransform(new ChartRect(0.0, 0.0, 100.0, 100.0), new ChartRange(0.0, 10.0), new ChartRange(0.0, 10.0));
        ChartCartesianRenderer renderer = new ChartCartesianRenderer(transform);
        ChartSeriesData data = CreateLineData(new double[] { 0.0, 5.0 }, new double[] { 0.0, 5.0 });
        renderer.AddStreamingLineSeries("live", data, ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0));

        ChartRenderScene first = document.Render(renderer);
        AssertLineEnd(first.CommandAt(first.GetCommandCount() - 1), 50.0, 50.0);

        data.AppendData(CreateLineData(new double[] { 10.0 }, new double[] { 10.0 }));
        ChartRenderScene appended = document.Render(renderer);
        Assert.That(appended.GetCommandCount(), Is.EqualTo(first.GetCommandCount() + 1));
        AssertLineEnd(appended.CommandAt(appended.GetCommandCount() - 1), 100.0, 0.0);

        data.UpdateData(2, CreateLineData(new double[] { 10.0 }, new double[] { 0.0 }));
        ChartRenderScene updated = document.Render(renderer);
        AssertLineEnd(updated.CommandAt(updated.GetCommandCount() - 1), 100.0, 100.0);

        ChartCartesianSeries beforeInvalidation = renderer.SeriesAt(0);
        data.Invalidate();
        document.Render(renderer);
        Assert.That(renderer.SeriesAt(0), Is.Not.SameAs(beforeInvalidation));
    }

    [Test]
    public void DocumentRender_RefreshesStreamingScatterMarkers()
    {
        ChartDocument document = new ChartDocument(100.0, 100.0);
        ChartCartesianTransform transform = new ChartCartesianTransform(new ChartRect(0.0, 0.0, 100.0, 100.0), new ChartRange(0.0, 10.0), new ChartRange(0.0, 10.0));
        ChartCartesianRenderer renderer = new ChartCartesianRenderer(transform);
        ChartSeriesData data = CreateLineData(new double[] { 2.0 }, new double[] { 3.0 });
        renderer.AddStreamingScatterSeries("points", data, ChartMarkerShape.Square, 6.0, ChartRenderStyle.CreateFill(ChartColor.Black()));

        ChartRenderScene first = document.Render(renderer);
        data.AppendData(CreateLineData(new double[] { 8.0 }, new double[] { 9.0 }));
        ChartRenderScene second = document.Render(renderer);

        Assert.That(second.GetCommandCount(), Is.EqualTo(first.GetCommandCount() + 1));
        ChartRenderCommand marker = second.CommandAt(second.GetCommandCount() - 1);
        Assert.That(marker.GetKind(), Is.EqualTo(ChartRenderCommandKind.Marker));
        Assert.That(marker.GetX1(), Is.EqualTo(80.0).Within(0.0000001));
        Assert.That(marker.GetY1(), Is.EqualTo(10.0).Within(0.0000001));
        Assert.Throws<System.ArgumentNullException>(() => renderer.AddStreamingLineSeries("bad", null!, ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0)));
    }

    internal static ChartDataTable CreateStreamingTable(double[] numbers, string?[] text, System.DateTime[] dates, string?[] categories)
    {
        ChartDataTable table = new ChartDataTable();
        table.AddNumberColumn("number", numbers);
        table.AddTextColumn("text", text);
        table.AddDateTimeColumn("date", dates);
        table.AddCategoryColumn("category", categories);
        return table;
    }

    internal static ChartSeriesData CreateLineData(double[] xValues, double[] yValues)
    {
        ChartSeriesData data = new ChartSeriesData(xValues.Length);
        data.SetXValues(xValues);
        data.SetYValues(yValues);
        return data;
    }

    internal static void AssertLineEnd(ChartRenderCommand command, double x, double y)
    {
        Assert.That(command.GetKind(), Is.EqualTo(ChartRenderCommandKind.Line));
        Assert.That(command.GetX2(), Is.EqualTo(x).Within(0.0000001));
        Assert.That(command.GetY2(), Is.EqualTo(y).Within(0.0000001));
    }
}
