namespace chart.Tests;

[TestFixture]
public class ChartStackerTests
{
    [Test]
    public void StackNormal_AccumulatesSeriesFromZeroBaseline()
    {
        ChartDataMatrix values = ChartDataMatrix.FromValues(2, 3, new double[] { 1.0, 2.0, 3.0, 2.0, 1.0, 1.0 });

        ChartStackResult stack = ChartStackerOperations.StackNormal(values);

        Assert.That(stack.Mode, Is.EqualTo(ChartStackMode.Zero));
        Assert.That(stack.Rows, Is.EqualTo(2));
        Assert.That(stack.Columns, Is.EqualTo(3));
        AssertStackCell(stack, 0, 0, 0.0, 1.0);
        AssertStackCell(stack, 0, 1, 1.0, 3.0);
        AssertStackCell(stack, 0, 2, 3.0, 6.0);
        AssertStackCell(stack, 1, 0, 0.0, 2.0);
        AssertStackCell(stack, 1, 1, 2.0, 3.0);
        AssertStackCell(stack, 1, 2, 3.0, 4.0);
    }

    [Test]
    public void StackPercent_NormalizesEachRowToOne()
    {
        ChartDataMatrix values = ChartDataMatrix.FromValues(2, 2, new double[] { 1.0, 3.0, 2.0, 2.0 });

        ChartStackResult stack = ChartStackerOperations.StackPercent(values);

        Assert.That(stack.Mode, Is.EqualTo(ChartStackMode.Percent));
        AssertStackCell(stack, 0, 0, 0.0, 0.25);
        AssertStackCell(stack, 0, 1, 0.25, 1.0);
        AssertStackCell(stack, 1, 0, 0.0, 0.5);
        AssertStackCell(stack, 1, 1, 0.5, 1.0);
    }

    [Test]
    public void StackDiverging_SeparatesPositiveAndNegativeBaselines()
    {
        ChartDataMatrix values = ChartDataMatrix.FromValues(1, 4, new double[] { 2.0, -1.0, 3.0, -2.0 });

        ChartStackResult stack = ChartStackerOperations.StackDiverging(values);

        Assert.That(stack.Mode, Is.EqualTo(ChartStackMode.Diverging));
        AssertStackCell(stack, 0, 0, 0.0, 2.0);
        AssertStackCell(stack, 0, 1, 0.0, -1.0);
        AssertStackCell(stack, 0, 2, 2.0, 5.0);
        AssertStackCell(stack, 0, 3, -1.0, -3.0);
    }

    [Test]
    public void StackStreamgraph_CentersRowsAroundZero()
    {
        ChartDataMatrix values = ChartDataMatrix.FromValues(1, 3, new double[] { 1.0, 2.0, 3.0 });

        ChartStackResult stack = ChartStackerOperations.StackStreamgraph(values);

        Assert.That(stack.Mode, Is.EqualTo(ChartStackMode.Streamgraph));
        AssertStackCell(stack, 0, 0, -3.0, -2.0);
        AssertStackCell(stack, 0, 1, -2.0, 0.0);
        AssertStackCell(stack, 0, 2, 0.0, 3.0);
    }

    [Test]
    public void Stack_DispatchesModes()
    {
        ChartDataMatrix values = ChartDataMatrix.FromValues(1, 2, new double[] { 1.0, 2.0 });

        Assert.That(ChartStackerOperations.Stack(values, ChartStackMode.None).Mode, Is.EqualTo(ChartStackMode.None));
        Assert.That(ChartStackerOperations.Stack(values, ChartStackMode.Zero).Mode, Is.EqualTo(ChartStackMode.Zero));
        Assert.That(ChartStackerOperations.Stack(values, ChartStackMode.Percent).Mode, Is.EqualTo(ChartStackMode.Percent));
        Assert.That(ChartStackerOperations.Stack(values, ChartStackMode.Diverging).Mode, Is.EqualTo(ChartStackMode.Diverging));
        Assert.That(ChartStackerOperations.Stack(values, ChartStackMode.Streamgraph).Mode, Is.EqualTo(ChartStackMode.Streamgraph));
    }

    [Test]
    public void StackNone_UsesZeroStartsAndRawValueEnds()
    {
        ChartDataMatrix values = ChartDataMatrix.FromValues(1, 2, new double[] { -1.0, 2.0 });

        ChartStackResult stack = ChartStackerOperations.StackNone(values);

        AssertStackCell(stack, 0, 0, 0.0, -1.0);
        AssertStackCell(stack, 0, 1, 0.0, 2.0);
    }

    [Test]
    public void ChartStackResult_CopiesInputMatrices()
    {
        ChartDataMatrix starts = ChartDataMatrix.FromValues(2, 2, new double[] { 0.0, 1.0, -2.0, 3.0 });
        ChartDataMatrix ends = ChartDataMatrix.FromValues(2, 2, new double[] { 4.0, 5.0, -1.0, 7.0 });

        ChartStackResult stack = new ChartStackResult(starts, ends, ChartStackMode.Diverging);

        starts.SetValue(0, 1, 100.0);
        ends.SetValue(1, 0, 100.0);

        Assert.That(stack.Mode, Is.EqualTo(ChartStackMode.Diverging));
        Assert.That(stack.Rows, Is.EqualTo(2));
        Assert.That(stack.Columns, Is.EqualTo(2));
        AssertStackCell(stack, 0, 0, 0.0, 4.0);
        AssertStackCell(stack, 0, 1, 1.0, 5.0);
        AssertStackCell(stack, 1, 0, -2.0, -1.0);
        AssertStackCell(stack, 1, 1, 3.0, 7.0);
    }

    [Test]
    public void RowTotal_SumsRequestedRow()
    {
        ChartDataMatrix values = ChartDataMatrix.FromValues(3, 3, new double[] { 1.0, 2.0, 3.0, -2.0, 5.0, 7.0, 4.0, -1.0, 2.0 });

        Assert.That(ChartStackerOperations.RowTotal(values, 0), Is.EqualTo(6.0));
        Assert.That(ChartStackerOperations.RowTotal(values, 1), Is.EqualTo(10.0));
        Assert.That(ChartStackerOperations.RowTotal(values, 2), Is.EqualTo(5.0));
    }

    [Test]
    public void Stacker_ValidatesInputs()
    {
        ChartDataMatrix values = ChartDataMatrix.FromValues(1, 2, new double[] { 1.0, 2.0 });
        ChartDataMatrix zeroPercent = ChartDataMatrix.FromValues(1, 2, new double[] { 1.0, -1.0 });
        ChartDataMatrix invalid = new ChartDataMatrix(1, 1);
        invalid.Values = new double[] { double.NaN };

        Assert.Throws<System.ArgumentNullException>(() => ChartStackerOperations.StackNormal(null!));
        Assert.Throws<System.ArgumentException>(() => ChartStackerOperations.Stack(values, (ChartStackMode)999));
        Assert.Throws<System.ArgumentException>(() => ChartStackerOperations.StackPercent(zeroPercent));
        Assert.Throws<System.ArgumentException>(() => ChartStackerOperations.StackNormal(invalid));
        Assert.Throws<System.ArgumentException>(() => new ChartStackResult(new ChartDataMatrix(1, 1), new ChartDataMatrix(1, 2), ChartStackMode.Zero));
    }

    static void AssertStackCell(ChartStackResult stack, int row, int column, double start, double end)
    {
        Assert.That(stack.GetStart(row, column), Is.EqualTo(start).Within(0.0000001));
        Assert.That(stack.GetEnd(row, column), Is.EqualTo(end).Within(0.0000001));
    }
}
