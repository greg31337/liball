namespace chart.Tests;

[TestFixture]
public class ChartMissingValuesTests
{
    [Test]
    public void ResolvePoints_AppliesEveryPolicyWithRowAlignment()
    {
        ChartMissingValues missing = new ChartMissingValues();
        double[] x = new double[] { 0.0, 1.0, double.NaN, 3.0, 4.0 };
        double[] y = new double[] { 1.0, double.NaN, 3.0, 4.0, 5.0 };

        ChartPoint[] skip = missing.ResolvePoints(x, y, ChartMissingValuePolicy.Skip);
        ChartPoint[] gap = missing.ResolvePoints(x, y, ChartMissingValuePolicy.Gap);
        ChartPoint[] zero = missing.ResolvePoints(x, y, ChartMissingValuePolicy.Zero);
        ChartPoint[] interpolate = missing.ResolvePoints(x, y, ChartMissingValuePolicy.Interpolate);
        ChartPoint[] carry = missing.ResolvePoints(x, y, ChartMissingValuePolicy.CarryForward);

        Assert.That(skip.Length, Is.EqualTo(3));
        AssertPoint(skip[0], 0.0, 1.0);
        AssertPoint(skip[1], 3.0, 4.0);
        Assert.That(gap.Length, Is.EqualTo(5));
        Assert.That(gap[1].GetX(), Is.NaN);
        Assert.That(gap[2].GetY(), Is.NaN);
        AssertPoint(zero[1], 1.0, 0.0);
        AssertPoint(zero[2], 0.0, 3.0);
        AssertPoint(interpolate[1], 1.0, 2.0);
        AssertPoint(interpolate[2], 2.0, 3.0);
        AssertPoint(carry[1], 1.0, 1.0);
        AssertPoint(carry[2], 1.0, 3.0);
    }

    [Test]
    public void ResolveTable_ProcessesNumericColumnsAndKeepsRelatedRows()
    {
        ChartDataTable table = new ChartDataTable();
        table.AddNumberColumn("x", new double[] { 0.0, 1.0, 2.0, 3.0 });
        table.AddNumberColumn("y", new double[] { 1.0, double.NaN, double.PositiveInfinity, 4.0 });
        table.AddTextColumn("label", new string[] { "A", "B", "C", "D" });
        table.AddCategoryColumn("group", new string[] { "first", "first", "second", "second" });
        ChartMissingValues missing = new ChartMissingValues();

        ChartDataTable skip = missing.ResolveTable(table, ChartMissingValuePolicy.Skip);
        ChartDataTable gap = missing.ResolveTable(table, ChartMissingValuePolicy.Gap);
        ChartDataTable zero = missing.ResolveTable(table, ChartMissingValuePolicy.Zero);
        ChartDataTable interpolate = missing.ResolveTable(table, ChartMissingValuePolicy.Interpolate);
        ChartDataTable carry = missing.ResolveTable(table, ChartMissingValuePolicy.CarryForward);

        Assert.That(skip.GetRowCount(), Is.EqualTo(2));
        Assert.That(skip.GetColumnCount(), Is.EqualTo(4));
        Assert.That(skip.GetColumn("x").CopyNumberValues(), Is.EqualTo(new double[] { 0.0, 3.0 }));
        Assert.That(skip.GetColumn("label").CopyTextValues(), Is.EqualTo(new string[] { "A", "D" }));
        Assert.That(gap.GetNumber("y", 1), Is.NaN);
        Assert.That(double.IsPositiveInfinity(gap.GetNumber("y", 2)), Is.True);
        Assert.That(zero.GetColumn("y").CopyNumberValues(), Is.EqualTo(new double[] { 1.0, 0.0, 0.0, 4.0 }));
        Assert.That(interpolate.GetColumn("y").CopyNumberValues(), Is.EqualTo(new double[] { 1.0, 2.0, 3.0, 4.0 }));
        Assert.That(carry.GetColumn("y").CopyNumberValues(), Is.EqualTo(new double[] { 1.0, 1.0, 1.0, 4.0 }));
        Assert.That(table.GetNumber("y", 1), Is.NaN);
    }

    [Test]
    public void ResolveDomainAndTransform_UseResolvedFiniteValues()
    {
        ChartMissingValues missing = new ChartMissingValues();
        ChartRange gapDomain = missing.ResolveDomain(new double[] { 2.0, double.NaN, 5.0 }, ChartMissingValuePolicy.Gap);
        ChartRange zeroDomain = missing.ResolveDomain(new double[] { 2.0, double.NaN, 5.0 }, ChartMissingValuePolicy.Zero);
        ChartCartesianTransform transform = new ChartCartesianTransform(
            new ChartRect(10.0, 20.0, 100.0, 200.0),
            new ChartRange(0.0, 10.0),
            new ChartRange(0.0, 10.0));

        ChartPoint[] gap = missing.TransformPoints(transform, new double[] { 0.0, double.NaN, 10.0 }, new double[] { 0.0, 5.0, 10.0 }, ChartMissingValuePolicy.Gap);
        ChartPoint[] zero = missing.TransformPoints(transform, new double[] { 0.0, double.NaN, 10.0 }, new double[] { 0.0, 5.0, 10.0 }, ChartMissingValuePolicy.Zero);

        Assert.That(gapDomain.GetMinimum(), Is.EqualTo(2.0));
        Assert.That(gapDomain.GetMaximum(), Is.EqualTo(5.0));
        Assert.That(zeroDomain.GetMinimum(), Is.EqualTo(0.0));
        Assert.That(zeroDomain.GetMaximum(), Is.EqualTo(5.0));
        AssertPoint(gap[0], 10.0, 220.0);
        Assert.That(gap[1].GetX(), Is.NaN);
        AssertPoint(gap[2], 110.0, 20.0);
        AssertPoint(zero[1], 10.0, 120.0);
    }

    [Test]
    public void PolicyAwareBuilders_SegmentGapsAndSkipIncompleteMarks()
    {
        ChartMissingValues missing = new ChartMissingValues();
        double[] x = new double[] { 0.0, 1.0, 2.0, 3.0, 4.0, 5.0 };
        double[] y = new double[] { 1.0, 2.0, double.NaN, 4.0, 5.0, 6.0 };
        ChartRenderStyle stroke = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);
        ChartRenderStyle fill = ChartRenderStyle.CreateFill(ChartColor.Black());

        ChartCartesianSeries gapLine = missing.CreateLineSeries("gap", x, y, ChartMissingValuePolicy.Gap, stroke);
        ChartCartesianSeries skipLine = missing.CreateLineSeries("skip", x, y, ChartMissingValuePolicy.Skip, stroke);
        ChartCartesianSeries scatter = missing.CreateScatterSeries("scatter", x, y, ChartMissingValuePolicy.Gap, ChartMarkerShape.Circle, 5.0, fill);
        ChartCartesianSeries bars = missing.CreateVerticalBarSeries("bars", x, y, ChartMissingValuePolicy.Gap, 0.5, 0.0, fill);

        Assert.That(gapLine.GetCommandCount(), Is.EqualTo(2));
        Assert.That(gapLine.CommandAt(0).GetPoints().Length, Is.EqualTo(2));
        Assert.That(gapLine.CommandAt(1).GetPoints().Length, Is.EqualTo(3));
        Assert.That(skipLine.GetCommandCount(), Is.EqualTo(1));
        Assert.That(skipLine.CommandAt(0).GetPoints().Length, Is.EqualTo(5));
        Assert.That(scatter.GetCommandCount(), Is.EqualTo(5));
        Assert.That(bars.GetCommandCount(), Is.EqualTo(5));
        Assert.That(missing.CreateScatterSeries("empty", new double[] { double.NaN }, new double[] { 1.0 }, ChartMissingValuePolicy.Skip, ChartMarkerShape.Circle, 5.0, fill).GetCommandCount(), Is.EqualTo(0));
    }

    [Test]
    public void Configuration_AppliesPolicyToConfiguredLineAndAreaBuilders()
    {
        ChartConfiguration configuration = new ChartConfiguration();
        configuration.SetMissingValuePolicy(ChartMissingValuePolicy.Gap);
        double[] x = new double[] { 0.0, 1.0, 2.0, 3.0, 4.0 };
        double[] y = new double[] { 1.0, 2.0, double.NaN, 4.0, 5.0 };
        ChartRenderStyle lineStyle = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);
        ChartRenderStyle areaStyle = ChartRenderStyle.CreateFill(ChartColor.Black());

        configuration.SetSeriesKind(ChartSeriesKind.StepLine);
        ChartCartesianSeries line = configuration.CreateCartesianSeries("step", x, y, 0.0, 1.0, lineStyle);
        configuration.SetSeriesKind(ChartSeriesKind.Area);
        ChartCartesianSeries area = configuration.CreateCartesianSeries("area", x, y, 0.0, 1.0, areaStyle);

        Assert.That(line.GetCommandCount(), Is.EqualTo(2));
        Assert.That(line.CommandAt(0).GetPoints().Length, Is.EqualTo(4));
        Assert.That(area.GetCommandCount(), Is.EqualTo(2));
        Assert.That(area.CommandAt(0).GetPoints().Length, Is.EqualTo(4));
    }

    [Test]
    public void MissingValueOperations_ValidateInputs()
    {
        ChartMissingValues missing = new ChartMissingValues();
        Assert.Throws<System.ArgumentNullException>(() => missing.ResolveValues(null!, ChartMissingValuePolicy.Skip));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => missing.ResolveValues(new double[] { 1.0 }, (ChartMissingValuePolicy)99));
        Assert.Throws<System.ArgumentException>(() => missing.ResolvePoints(new double[] { 1.0 }, new double[] { 1.0, 2.0 }, ChartMissingValuePolicy.Skip));
        Assert.Throws<System.ArgumentException>(() => missing.ResolveDomain(new double[] { double.NaN }, ChartMissingValuePolicy.Gap));
        Assert.Throws<System.ArgumentNullException>(() => missing.TransformPoints(null!, new double[] { 1.0 }, new double[] { 1.0 }, ChartMissingValuePolicy.Zero));
        Assert.Throws<System.ArgumentNullException>(() => missing.ResolveTable(null!, ChartMissingValuePolicy.Zero));
    }

    static void AssertPoint(ChartPoint point, double x, double y)
    {
        Assert.That(point.GetX(), Is.EqualTo(x).Within(0.0000001));
        Assert.That(point.GetY(), Is.EqualTo(y).Within(0.0000001));
    }
}
