namespace chart.Tests;

[TestFixture]
public class ChartDataOperationsCompletionTests
{
    [Test]
    public void SortIndicesByCustomOrder_UsesExplicitOrderAndStableFallback()
    {
        ChartDataOperations operations = new ChartDataOperations();
        ChartDataTable table = new ChartDataTable();
        ChartDataColumn categories = table.AddCategoryColumn("priority", new string?[] { "medium", "low", "unknown", "high", "low", null });

        int[] indices = operations.SortIndicesByCustomOrder(categories, new string?[] { "high", "medium", "low" });

        Assert.That(indices, Is.EqualTo(new int[] { 3, 0, 1, 4, 2, 5 }));
        Assert.Throws<System.ArgumentException>(() => operations.SortIndicesByCustomOrder(categories, new string?[] { "low", "low" }));
        Assert.Throws<System.ArgumentNullException>(() => operations.SortIndicesByCustomOrder(categories, null!));
        Assert.Throws<System.InvalidOperationException>(() => operations.SortIndicesByCustomOrder(table.AddNumberColumn("number", new double[] { 1, 2, 3, 4, 5, 6 }), new string?[] { "1" }));
    }

    [Test]
    public void JoinTablesByKey_ProducesStableManyToManyInnerAndLeftJoins()
    {
        ChartDataOperations operations = new ChartDataOperations();
        ChartDataTable left = CreateLeftTable();
        ChartDataTable right = CreateRightTable();

        ChartDataTable inner = operations.JoinTablesByKey(left, right, "key", "key", ChartJoinKind.Inner);
        ChartDataTable leftJoin = operations.JoinTablesByKey(left, right, "key", "key", ChartJoinKind.Left);

        Assert.That(inner.GetRowCount(), Is.EqualTo(4));
        Assert.That(inner.GetColumnCount(), Is.EqualTo(6));
        Assert.That(inner.GetColumn("value").CopyNumberValues(), Is.EqualTo(new double[] { 2, 2, 3, 3 }));
        Assert.That(inner.GetColumn("right.value").CopyNumberValues(), Is.EqualTo(new double[] { 20, 21, 20, 21 }));
        Assert.That(inner.GetColumn("note").CopyTextValues(), Is.EqualTo(new string?[] { "RB1", "RB2", "RB1", "RB2" }));

        Assert.That(leftJoin.GetRowCount(), Is.EqualTo(7));
        Assert.That(leftJoin.GetColumn("key").CopyCategoryValues(), Is.EqualTo(new string?[] { "A", "B", "B", "B", "B", "C", null }));
        Assert.That(leftJoin.GetNumber("right.value", 0), Is.NaN);
        Assert.That(leftJoin.GetNumber("right.value", 5), Is.NaN);
        Assert.That(leftJoin.GetNumber("right.value", 6), Is.NaN);
        Assert.That(leftJoin.GetText("note", 0), Is.Null);
        Assert.That(left.GetRowCount(), Is.EqualTo(5));
        Assert.That(right.GetRowCount(), Is.EqualTo(4));
    }

    [Test]
    public void JoinTablesByKey_ProducesFullAndRightJoinsWithUnmatchedRows()
    {
        ChartDataOperations operations = new ChartDataOperations();
        ChartDataTable left = CreateLeftTable();
        ChartDataTable right = CreateRightTable();

        ChartDataTable full = operations.JoinTablesByKey(left, right, "key", "key", ChartJoinKind.Full);
        ChartDataTable rightJoin = operations.JoinTablesByKey(left, right, "key", "key", ChartJoinKind.Right);

        Assert.That(full.GetRowCount(), Is.EqualTo(9));
        Assert.That(full.GetNumber("value", 7), Is.NaN);
        Assert.That(full.GetCategory("right.key", 7), Is.EqualTo("D"));
        Assert.That(full.GetNumber("value", 8), Is.NaN);
        Assert.That(full.GetCategory("right.key", 8), Is.Null);

        Assert.That(rightJoin.GetRowCount(), Is.EqualTo(6));
        Assert.That(rightJoin.GetColumn("value").CopyNumberValues()[0..4], Is.EqualTo(new double[] { 2, 3, 2, 3 }));
        Assert.That(rightJoin.GetColumn("right.value").CopyNumberValues(), Is.EqualTo(new double[] { 20, 20, 21, 21, 40, 50 }));
        Assert.That(rightJoin.GetNumber("value", 4), Is.NaN);
        Assert.That(rightJoin.GetNumber("value", 5), Is.NaN);
    }

    [Test]
    public void JoinTablesByKey_ValidatesKindsAndUnsupportedColumns()
    {
        ChartDataOperations operations = new ChartDataOperations();
        ChartDataTable left = new ChartDataTable();
        left.AddNumberColumn("key", new double[] { 1.0 });
        ChartDataTable right = new ChartDataTable();
        right.AddTextColumn("key", new string?[] { "1" });
        ChartDataTable withMatrix = new ChartDataTable();
        withMatrix.AddNumberColumn("key", new double[] { 1.0 });
        withMatrix.AddMatrixColumn("matrix", new ChartDataMatrix(1, 1));

        Assert.Throws<System.ArgumentException>(() => operations.JoinTablesByKey(left, right, "key", "key", ChartJoinKind.Inner));
        Assert.Throws<System.ArgumentException>(() => operations.JoinTablesByKey(left, left, "key", "key", (ChartJoinKind)99));
        Assert.Throws<System.InvalidOperationException>(() => operations.JoinTablesByKey(left, withMatrix, "key", "key", ChartJoinKind.Left));
        Assert.Throws<System.ArgumentNullException>(() => operations.JoinTablesByKey(null!, right, "key", "key", ChartJoinKind.Inner));
    }

    [Test]
    public void CalculatedColumns_UseExplicitRowCalculationsAndUpdateTheTable()
    {
        ChartDataOperations operations = new ChartDataOperations();
        ChartDataTable table = new ChartDataTable();
        table.AddNumberColumn("x", new double[] { 2.0, 4.0, 6.0 });
        table.AddTextColumn("label", new string?[] { "A", null, "C" });

        ChartDataColumn doubled = operations.AddCalculatedNumberColumn(table, "doubled", new DoubleValueCalculation());
        ChartDataColumn display = operations.AddCalculatedTextColumn(table, "display", new DisplayValueCalculation());

        Assert.That(doubled.CopyNumberValues(), Is.EqualTo(new double[] { 4.0, 8.0, 12.0 }));
        Assert.That(display.CopyTextValues(), Is.EqualTo(new string?[] { "A:4", null, "C:12" }));
        Assert.That(table.GetColumnCount(), Is.EqualTo(4));
        Assert.That(table.GetNumber("doubled", 1), Is.EqualTo(8.0));
        Assert.Throws<System.ArgumentException>(() => operations.AddCalculatedNumberColumn(table, "x", new DoubleValueCalculation()));
        Assert.Throws<System.ArgumentNullException>(() => operations.AddCalculatedTextColumn(table, "other", null!));
    }

    [Test]
    public void AggregatePercentile_CompletesTheAggregationSet()
    {
        ChartDataOperations operations = new ChartDataOperations();
        ChartDataTable table = new ChartDataTable();
        ChartDataColumn values = table.AddNumberColumn("value", new double[] { 1.0, 2.0, 3.0, 4.0 });
        int[] indices = new int[] { 0, 1, 2, 3 };

        Assert.That(operations.AggregateNumber(values, indices, ChartAggregationKind.Percentile), Is.EqualTo(2.5));
        Assert.That(operations.AggregatePercentile(values, indices, 25.0), Is.EqualTo(1.75));
        Assert.That(operations.AggregatePercentile(values, indices, 75.0), Is.EqualTo(3.25));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => operations.AggregatePercentile(values, indices, 101.0));
        Assert.Throws<System.ArgumentException>(() => operations.AggregatePercentile(values, new int[0], 50.0));
    }

    internal static ChartDataTable CreateLeftTable()
    {
        ChartDataTable table = new ChartDataTable();
        table.AddCategoryColumn("key", new string?[] { "A", "B", "B", "C", null });
        table.AddNumberColumn("value", new double[] { 1, 2, 3, 4, 5 });
        table.AddTextColumn("label", new string?[] { "LA", "LB1", "LB2", "LC", "LM" });
        return table;
    }

    internal static ChartDataTable CreateRightTable()
    {
        ChartDataTable table = new ChartDataTable();
        table.AddCategoryColumn("key", new string?[] { "B", "B", "D", null });
        table.AddNumberColumn("value", new double[] { 20, 21, 40, 50 });
        table.AddTextColumn("note", new string?[] { "RB1", "RB2", "RD", "RM" });
        return table;
    }
}

internal class DoubleValueCalculation : IChartNumberCalculation
{
    public double Calculate(ChartDataTable table, int row)
    {
        return table.GetNumber("x", row) * 2.0;
    }
}

internal class DisplayValueCalculation : IChartTextCalculation
{
    public string? Calculate(ChartDataTable table, int row)
    {
        string? label = table.GetText("label", row);
        if (label == null)
        {
            return null;
        }

        return label + ":" + table.GetNumber("doubled", row).ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
