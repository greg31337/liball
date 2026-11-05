namespace chart.Tests;

[TestFixture]
public class ChartDataOperationsTests
{
    [Test]
    public void GroupByCategory_ReturnsStableGroupsWithSourceIndices()
    {
        ChartDataColumn groups = ChartDataColumn.CreateCategory("group", new string[] { "B", "A", "B", "C", "A" });

        ChartDataGroup[] result = ChartDataOperationsOperations.GroupByCategory(groups);

        Assert.That(result.Length, Is.EqualTo(3));
        Assert.That(result[0].Key, Is.EqualTo("B"));
        Assert.That(result[0].CopyIndices(), Is.EqualTo(new int[] { 0, 2 }));
        Assert.That(result[1].Key, Is.EqualTo("A"));
        Assert.That(result[1].CopyIndices(), Is.EqualTo(new int[] { 1, 4 }));
        Assert.That(result[2].Key, Is.EqualTo("C"));
        Assert.That(result[2].CopyIndices(), Is.EqualTo(new int[] { 3 }));
    }

    [Test]
    public void SortHelpers_ReturnStableIndicesForNumbersAndText()
    {
        ChartDataColumn numbers = ChartDataColumn.CreateNumber("value", new double[] { 3.0, 1.0, 2.0, 1.0 });
        ChartDataColumn text = ChartDataColumn.CreateText("label", new string[] { "b", "a", "c" });

        Assert.That(ChartDataOperationsOperations.SortIndicesByNumber(numbers, true), Is.EqualTo(new int[] { 1, 3, 2, 0 }));
        Assert.That(ChartDataOperationsOperations.SortIndicesByNumber(numbers, false), Is.EqualTo(new int[] { 0, 2, 1, 3 }));
        Assert.That(ChartDataOperationsOperations.SortIndicesByText(text, true), Is.EqualTo(new int[] { 1, 0, 2 }));
        Assert.That(ChartDataOperationsOperations.SortIndicesByText(text, false), Is.EqualTo(new int[] { 2, 0, 1 }));
    }

    [Test]
    public void FilterHelpers_ReturnMatchingRowIndices()
    {
        ChartDataColumn numbers = ChartDataColumn.CreateNumber("value", new double[] { 1.0, 2.5, 4.0, 6.0 });
        ChartDataColumn groups = ChartDataColumn.CreateCategory("group", new string?[] { "A", "", "B", "C", null });
        ChartDataColumn labels = ChartDataColumn.CreateText("label", new string[] { "alpha", "beta", "alphabet" });

        Assert.That(ChartDataOperationsOperations.FilterNumberRange(numbers, 2.0, 4.0), Is.EqualTo(new int[] { 1, 2 }));
        Assert.That(ChartDataOperationsOperations.FilterCategoryIn(groups, new string[] { "A", "C" }), Is.EqualTo(new int[] { 0, 3 }));
        Assert.That(ChartDataOperationsOperations.FilterMissingCategories(groups), Is.EqualTo(new int[] { 1, 4 }));
        Assert.That(ChartDataOperationsOperations.FilterTextContains(labels, "alpha"), Is.EqualTo(new int[] { 0, 2 }));
    }

    [Test]
    public void AggregateNumber_ComputesSelectedAggregations()
    {
        ChartDataColumn values = ChartDataColumn.CreateNumber("value", new double[] { 1.0, 2.0, 3.0, 4.0 });
        int[] indices = new int[] { 0, 2, 3 };

        Assert.That(ChartDataOperationsOperations.AggregateNumber(values, indices, ChartAggregationKind.Count), Is.EqualTo(3.0));
        Assert.That(ChartDataOperationsOperations.AggregateNumber(values, indices, ChartAggregationKind.Sum), Is.EqualTo(8.0));
        Assert.That(ChartDataOperationsOperations.AggregateNumber(values, indices, ChartAggregationKind.Mean), Is.EqualTo(8.0 / 3.0).Within(0.0000001));
        Assert.That(ChartDataOperationsOperations.AggregateNumber(values, indices, ChartAggregationKind.Median), Is.EqualTo(3.0));
        Assert.That(ChartDataOperationsOperations.AggregateNumber(values, indices, ChartAggregationKind.Min), Is.EqualTo(1.0));
        Assert.That(ChartDataOperationsOperations.AggregateNumber(values, indices, ChartAggregationKind.Max), Is.EqualTo(4.0));
        Assert.That(ChartDataOperationsOperations.AggregateNumber(values, indices, ChartAggregationKind.First), Is.EqualTo(1.0));
        Assert.That(ChartDataOperationsOperations.AggregateNumber(values, indices, ChartAggregationKind.Last), Is.EqualTo(4.0));
        Assert.That(ChartDataOperationsOperations.AggregateNumber(values, indices, ChartAggregationKind.Variance), Is.EqualTo(14.0 / 9.0).Within(0.0000001));
        Assert.That(ChartDataOperationsOperations.AggregateNumber(values, indices, ChartAggregationKind.StandardDeviation), Is.EqualTo(System.Math.Sqrt(14.0 / 9.0)).Within(0.0000001));
    }

    [Test]
    public void PivotCategoryNumber_CreatesStableMatrixOfAggregates()
    {
        ChartDataColumn rows = ChartDataColumn.CreateCategory("row", new string[] { "R1", "R1", "R2", "R2", "R2" });
        ChartDataColumn columns = ChartDataColumn.CreateCategory("column", new string[] { "C1", "C2", "C1", "C2", "C2" });
        ChartDataColumn values = ChartDataColumn.CreateNumber("value", new double[] { 1.0, 2.0, 3.0, 4.0, 6.0 });

        ChartPivotTable pivot = ChartDataOperationsOperations.PivotCategoryNumber(rows, columns, values, ChartAggregationKind.Mean);

        Assert.That(pivot.CopyRowKeys(), Is.EqualTo(new string[] { "R1", "R2" }));
        Assert.That(pivot.CopyColumnKeys(), Is.EqualTo(new string[] { "C1", "C2" }));
        Assert.That(pivot.GetValue(0, 0), Is.EqualTo(1.0));
        Assert.That(pivot.GetValue(0, 1), Is.EqualTo(2.0));
        Assert.That(pivot.GetValue(1, 0), Is.EqualTo(3.0));
        Assert.That(pivot.GetValue(1, 1), Is.EqualTo(5.0));
    }

    [Test]
    public void PivotCategoryNumber_HandlesSparseValidDataAndPreservesObservedKeyOrder()
    {
        ChartDataColumn rows = ChartDataColumn.CreateCategory("region", new string[] { "East", "West", "East", "North", "West", "North", "East" });
        ChartDataColumn columns = ChartDataColumn.CreateCategory("quarter", new string[] { "Q2", "Q1", "Q2", "Q1", "Q3", "Q3", "Q3" });
        ChartDataColumn values = ChartDataColumn.CreateNumber("revenue", new double[] { 4.0, 10.0, 6.0, 8.0, -2.0, 12.0, 3.0 });

        ChartPivotTable pivot = ChartDataOperationsOperations.PivotCategoryNumber(rows, columns, values, ChartAggregationKind.Sum);

        Assert.That(pivot.CopyRowKeys(), Is.EqualTo(new string[] { "East", "West", "North" }));
        Assert.That(pivot.CopyColumnKeys(), Is.EqualTo(new string[] { "Q2", "Q1", "Q3" }));
        Assert.That(pivot.GetValue(0, 0), Is.EqualTo(10.0));
        Assert.That(pivot.GetValue(0, 1), Is.NaN);
        Assert.That(pivot.GetValue(0, 2), Is.EqualTo(3.0));
        Assert.That(pivot.GetValue(1, 0), Is.NaN);
        Assert.That(pivot.GetValue(1, 1), Is.EqualTo(10.0));
        Assert.That(pivot.GetValue(1, 2), Is.EqualTo(-2.0));
        Assert.That(pivot.GetValue(2, 0), Is.NaN);
        Assert.That(pivot.GetValue(2, 1), Is.EqualTo(8.0));
        Assert.That(pivot.GetValue(2, 2), Is.EqualTo(12.0));
    }

    [Test]
    public void AggregateNumber_UsesProvidedSelectionOrderForFirstAndLast()
    {
        ChartDataColumn values = ChartDataColumn.CreateNumber("value", new double[] { 100.0, -5.0, 7.5, 7.5, 42.0 });
        int[] indices = new int[] { 4, 1, 3 };

        Assert.That(ChartDataOperationsOperations.AggregateNumber(values, indices, ChartAggregationKind.Count), Is.EqualTo(3.0));
        Assert.That(ChartDataOperationsOperations.AggregateNumber(values, indices, ChartAggregationKind.Sum), Is.EqualTo(44.5));
        Assert.That(ChartDataOperationsOperations.AggregateNumber(values, indices, ChartAggregationKind.Median), Is.EqualTo(7.5));
        Assert.That(ChartDataOperationsOperations.AggregateNumber(values, indices, ChartAggregationKind.Min), Is.EqualTo(-5.0));
        Assert.That(ChartDataOperationsOperations.AggregateNumber(values, indices, ChartAggregationKind.Max), Is.EqualTo(42.0));
        Assert.That(ChartDataOperationsOperations.AggregateNumber(values, indices, ChartAggregationKind.First), Is.EqualTo(42.0));
        Assert.That(ChartDataOperationsOperations.AggregateNumber(values, indices, ChartAggregationKind.Last), Is.EqualTo(7.5));
    }

    [Test]
    public void FilterAndSortHelpers_HandleDuplicateCategoriesAndMissingValues()
    {
        ChartDataColumn groups = ChartDataColumn.CreateCategory("group", new string?[] { "B", "", "A", null, "B", "A" });
        ChartDataColumn labels = ChartDataColumn.CreateText("label", new string[] { "alpha-10", "beta-20", "alpha-20", "gamma", "alpha-30" });

        Assert.That(ChartDataOperationsOperations.SortIndicesByText(groups, true), Is.EqualTo(new int[] { 3, 1, 2, 5, 0, 4 }));
        Assert.That(ChartDataOperationsOperations.FilterCategoryIn(groups, new string?[] { "A", null, "A" }), Is.EqualTo(new int[] { 2, 3, 5 }));
        Assert.That(ChartDataOperationsOperations.FilterMissingCategories(groups), Is.EqualTo(new int[] { 1, 3 }));
        Assert.That(ChartDataOperationsOperations.FilterTextContains(labels, "alpha-"), Is.EqualTo(new int[] { 0, 2, 4 }));
    }

    [Test]
    public void DataOperations_ValidateInputs()
    {
        ChartDataColumn number = ChartDataColumn.CreateNumber("value", new double[] { 1.0 });
        ChartDataColumn category = ChartDataColumn.CreateCategory("group", new string[] { "A" });
        ChartDataColumn text = ChartDataColumn.CreateText("label", new string[] { "A" });

        Assert.Throws<System.InvalidOperationException>(() => ChartDataOperationsOperations.GroupByCategory(number));
        Assert.Throws<System.ArgumentException>(() => ChartDataOperationsOperations.SortIndicesByNumber(ChartDataColumn.CreateNumber("bad", new double[] { double.NaN }), true));
        Assert.Throws<System.InvalidOperationException>(() => ChartDataOperationsOperations.SortIndicesByText(number, true));
        Assert.Throws<System.ArgumentException>(() => ChartDataOperationsOperations.FilterNumberRange(number, 2.0, 1.0));
        Assert.Throws<System.ArgumentNullException>(() => ChartDataOperationsOperations.FilterCategoryIn(category, null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartDataOperationsOperations.FilterTextContains(text, null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartDataOperationsOperations.AggregateNumber(number, null!, ChartAggregationKind.Sum));
        Assert.Throws<System.ArgumentException>(() => ChartDataOperationsOperations.AggregateNumber(number, new int[0], ChartAggregationKind.Sum));
        Assert.That(ChartDataOperationsOperations.AggregateNumber(number, new int[] { 0 }, ChartAggregationKind.Percentile), Is.EqualTo(1.0));
        Assert.Throws<System.ArgumentException>(() => ChartDataOperationsOperations.PivotCategoryNumber(category, ChartDataColumn.CreateCategory("short", new string[0]), number, ChartAggregationKind.Sum));
        Assert.Throws<System.ArgumentException>(() => new ChartPivotTable(new string[] { "R" }, new string[] { "C" }, new ChartDataMatrix(2, 1)));
    }

    [Test]
    public void DataOperations_RejectInvalidNumericSelectionsAndRanges()
    {
        ChartDataColumn values = ChartDataColumn.CreateNumber("value", new double[] { 1.0, 2.0, 3.0 });
        ChartDataColumn withNaN = ChartDataColumn.CreateNumber("nan", new double[] { 1.0, double.NaN });
        ChartDataColumn withInfinity = ChartDataColumn.CreateNumber("infinity", new double[] { 1.0, double.PositiveInfinity });

        Assert.Throws<System.ArgumentException>(() => ChartDataOperationsOperations.SortIndicesByNumber(withInfinity, true));
        Assert.Throws<System.ArgumentException>(() => ChartDataOperationsOperations.FilterNumberRange(values, double.NaN, 3.0));
        Assert.Throws<System.ArgumentException>(() => ChartDataOperationsOperations.FilterNumberRange(values, 0.0, double.PositiveInfinity));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartDataOperationsOperations.AggregateNumber(values, new int[] { 0, 3 }, ChartAggregationKind.Sum));
        Assert.Throws<System.ArgumentException>(() => ChartDataOperationsOperations.AggregateNumber(withNaN, new int[] { 0, 1 }, ChartAggregationKind.Sum));
    }

    [Test]
    public void PivotCategoryNumber_RejectsInvalidColumnKindsLengthsAndAggregations()
    {
        ChartDataColumn categories = ChartDataColumn.CreateCategory("category", new string[] { "A", "B" });
        ChartDataColumn otherCategories = ChartDataColumn.CreateCategory("other", new string[] { "Q1", "Q2" });
        ChartDataColumn shortCategories = ChartDataColumn.CreateCategory("short", new string[] { "Q1" });
        ChartDataColumn values = ChartDataColumn.CreateNumber("value", new double[] { 1.0, 2.0 });
        ChartDataColumn shortValues = ChartDataColumn.CreateNumber("shortValue", new double[] { 1.0 });
        ChartDataColumn text = ChartDataColumn.CreateText("text", new string[] { "A", "B" });

        Assert.Throws<System.InvalidOperationException>(() => ChartDataOperationsOperations.PivotCategoryNumber(text, otherCategories, values, ChartAggregationKind.Sum));
        Assert.Throws<System.InvalidOperationException>(() => ChartDataOperationsOperations.PivotCategoryNumber(categories, text, values, ChartAggregationKind.Sum));
        Assert.Throws<System.InvalidOperationException>(() => ChartDataOperationsOperations.PivotCategoryNumber(categories, otherCategories, text, ChartAggregationKind.Sum));
        Assert.Throws<System.ArgumentException>(() => ChartDataOperationsOperations.PivotCategoryNumber(categories, shortCategories, values, ChartAggregationKind.Sum));
        Assert.Throws<System.ArgumentException>(() => ChartDataOperationsOperations.PivotCategoryNumber(categories, otherCategories, shortValues, ChartAggregationKind.Sum));
        ChartPivotTable percentile = ChartDataOperationsOperations.PivotCategoryNumber(categories, otherCategories, values, ChartAggregationKind.Percentile);
        Assert.That(percentile.GetValue(0, 0), Is.EqualTo(1.0));
        Assert.That(percentile.GetValue(1, 1), Is.EqualTo(2.0));
    }
}
