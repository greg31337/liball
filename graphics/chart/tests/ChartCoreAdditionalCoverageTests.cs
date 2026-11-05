namespace chart.Tests;

[TestFixture]
public class ChartCoreAdditionalCoverageTests
{
    [Test]
    public void ColorHelpers_RoundClampCopyAndEqualityCoverEdges()
    {
        ChartColor color = ChartColor.FromRgba(1, 2, 3, 4);
        ChartColor copy = color.Copy();
        copy.R = 99;

        Assert.That(color.EqualsColor(new ChartColor(1, 2, 3, 4)), Is.True);
        Assert.That(color.EqualsColor(copy), Is.False);
        Assert.That(ChartColor.RoundToByte(0.49), Is.EqualTo((byte)0));
        Assert.That(ChartColor.RoundToByte(0.5), Is.EqualTo((byte)1));
        Assert.That(ChartColor.RoundToByte(254.6), Is.EqualTo((byte)255));
        Assert.That(ChartColor.ClampToByte(-10.0), Is.EqualTo((byte)0));
        Assert.That(ChartColor.ClampToByte(300.0), Is.EqualTo((byte)255));
        Assert.That(double.IsNaN(ChartColor.ClampDouble(double.NaN, 2.0, 5.0)), Is.True);
        Assert.That(ChartColor.ByteToHex(15), Is.EqualTo("0F"));
        Assert.That(ChartColor.Parse("red").ToHexRgb(), Is.EqualTo("#FF0000"));
    }

    [Test]
    public void GeometryHelpers_TranslateInflateDeflateAndInvalidRanges()
    {
        ChartRect rect = new ChartRect(10.0, 20.0, 30.0, 40.0);
        ChartRect translated = rect.Translate(5.0, -10.0);
        ChartRect inflated = rect.Inflate(2.0, 3.0);
        ChartRect deflated = rect.Deflate(2.0, 3.0);
        ChartSize size = new ChartSize(4.0, 5.0);

        Assert.That(rect.Center().X, Is.EqualTo(25.0));
        Assert.That(rect.Center().Y, Is.EqualTo(40.0));
        AssertRect(translated, 15.0, 10.0, 30.0, 40.0);
        AssertRect(inflated, 8.0, 17.0, 34.0, 46.0);
        AssertRect(deflated, 12.0, 23.0, 26.0, 34.0);
        Assert.That(size.Area(), Is.EqualTo(20.0));
        Assert.That(size.Scale(2.0).Width, Is.EqualTo(8.0));
        Assert.That(new ChartSize(0.0, 5.0).IsEmpty(), Is.True);
        Assert.That(new ChartRange(2.0, 1.0).IsValid(), Is.False);
    }

    [Test]
    public void Scales_HandleReversedRangesAndSingleTickDomains()
    {
        ChartLinearScale reversed = new ChartLinearScale(0.0, 10.0, 100.0, 0.0);
        ChartTick[] oneTick = ChartTickGeneratorOperations.GenerateNumericTicks(2.0, 8.0, 1);
        ChartRange niceSingle = ChartScaleUtilities.NiceDomain(2.0, 8.0, 1);

        Assert.That(reversed.Map(2.5), Is.EqualTo(75.0));
        Assert.That(reversed.Invert(25.0), Is.EqualTo(7.5));
        Assert.That(oneTick.Length, Is.EqualTo(1));
        Assert.That(oneTick[0].Value, Is.EqualTo(2.0));
        Assert.That(niceSingle.Minimum, Is.EqualTo(2.0));
        Assert.That(niceSingle.Maximum, Is.EqualTo(8.0));
        Assert.That(new ChartRange(0.0, 10.0).Normalize(5.0), Is.EqualTo(0.5));
        Assert.That(new ChartRange(10.0, 20.0).Lerp(0.25), Is.EqualTo(12.5));
    }

    [Test]
    public void TickHelpers_CopyTimeTicksAndFormatSingleTickArrays()
    {
        System.DateTime value = new System.DateTime(2026, 8, 4, 12, 30, 0);
        ChartTick tick = ChartTick.CreateTime(value, 3);
        ChartTick copy = tick.Copy();
        ChartTick[] ticks = new ChartTick[] { tick };

        tick.Label = "changed";

        Assert.That(copy.Kind, Is.EqualTo(ChartTickKind.Time));
        Assert.That(copy.DateTimeValue, Is.EqualTo(value));
        Assert.That(copy.Index, Is.EqualTo(3));
        Assert.That(ChartTickFormatterOperations.FormatTicks(ticks), Is.EqualTo(new string[] { "2026-08-04" }));
        Assert.That(ChartTickFormatterOperations.BuildDecimalFormat(0), Is.EqualTo("0"));
        Assert.That(ChartTickFormatterOperations.BuildDecimalFormat(3), Is.EqualTo("0.###"));
    }

    [Test]
    public void Statistics_HandleConstantAndUnsortedValues()
    {
        double[] constant = new double[] { 5.0, 5.0, 5.0 };
        double[] unsorted = new double[] { 10.0, -2.0, 4.0, 8.0 };

        Assert.That(ChartStatisticsOperations.Variance(constant), Is.EqualTo(0.0));
        Assert.That(ChartStatisticsOperations.StandardDeviation(constant), Is.EqualTo(0.0));
        Assert.That(ChartStatisticsOperations.Median(unsorted), Is.EqualTo(6.0));
        Assert.That(ChartStatisticsOperations.Sum(unsorted), Is.EqualTo(20.0));
        Assert.Throws<System.InvalidOperationException>(() => ChartStatisticsOperations.Correlation(constant, new double[] { 1.0, 2.0, 3.0 }));
    }

    [Test]
    public void Binning_HandlesSingleValuesAndBoundaryIndexes()
    {
        ChartHistogram fixedCount = ChartBinnerOperations.BinFixedCount(new double[] { 4.0 }, 2);
        ChartHistogram fixedWidth = ChartBinnerOperations.BinFixedWidth(new double[] { 4.0 }, 3.0);

        Assert.That(fixedCount.Minimum, Is.EqualTo(3.5));
        Assert.That(fixedCount.Maximum, Is.EqualTo(4.5));
        Assert.That(fixedCount.TotalCount(), Is.EqualTo(1));
        Assert.That(fixedWidth.BinCount, Is.EqualTo(1));
        Assert.That(fixedWidth.GetBin(0).Minimum, Is.EqualTo(4.0));
        Assert.That(fixedWidth.GetBin(0).Maximum, Is.EqualTo(7.0));
        Assert.That(ChartBinnerOperations.FindBinIndex(-1.0, 0.0, 10.0, 2.0, 5), Is.EqualTo(0));
        Assert.That(ChartBinnerOperations.FindBinIndex(10.0, 0.0, 10.0, 2.0, 5), Is.EqualTo(4));
        Assert.That(ChartBinnerOperations.NormalizeRuleWidth(double.NaN, 0.0), Is.EqualTo(1.0));
    }

    [Test]
    public void Stacking_CopiesInputMatricesAndExposesDimensions()
    {
        ChartDataMatrix starts = ChartDataMatrix.FromValues(1, 2, new double[] { 0.0, 2.0 });
        ChartDataMatrix ends = ChartDataMatrix.FromValues(1, 2, new double[] { 2.0, 5.0 });
        ChartStackResult result = new ChartStackResult(starts, ends, ChartStackMode.Zero);
        starts.SetValue(0, 0, 99.0);
        ends.SetValue(0, 1, 99.0);

        Assert.That(result.Rows, Is.EqualTo(1));
        Assert.That(result.Columns, Is.EqualTo(2));
        Assert.That(result.GetStart(0, 0), Is.EqualTo(0.0));
        Assert.That(result.GetEnd(0, 1), Is.EqualTo(5.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => result.GetStart(1, 0));
    }

    [Test]
    public void DataValidation_CanMergeTableAndSeriesResults()
    {
        ChartDataTable table = new ChartDataTable();
        table.AddNumberColumn("x", new double[] { 1.0, double.NaN });
        table.AddCategoryColumn("group", new string[] { "A", "" });
        ChartValidationResult combined = new ChartValidationResult();

        combined.AddResult(ChartDataValidatorOperations.ValidateTable(table));
        combined.AddResult(ChartDataValidatorOperations.ValidateNumberSeries("series", new double[] { 1.0 }, new double[] { 1.0, 2.0 }));

        Assert.That(combined.CountCode(ChartDataValidatorOperations.NaNCode), Is.EqualTo(1));
        Assert.That(combined.CountCode(ChartDataValidatorOperations.MissingCategoryCode), Is.EqualTo(1));
        Assert.That(combined.CountCode(ChartDataValidatorOperations.MismatchedLengthCode), Is.EqualTo(1));
        Assert.That(combined.IsValid(), Is.False);
    }

    [Test]
    public void ArrayGrowth_HandlesZeroAndNearMaximumCapacities()
    {
        Assert.That(ChartArrayUtilities.GrowthCapacity(0, 3), Is.EqualTo(3));
        Assert.That(ChartArrayUtilities.GrowthCapacity(4, 5), Is.EqualTo(8));
        Assert.That(ChartArrayUtilities.GrowthCapacity(4, 20), Is.EqualTo(32));
        Assert.That(ChartArrayUtilities.GrowthCapacity(int.MaxValue - 10, int.MaxValue - 5), Is.EqualTo(int.MaxValue - 5));
    }

    static void AssertRect(ChartRect rect, double x, double y, double width, double height)
    {
        Assert.That(rect.X, Is.EqualTo(x).Within(0.0000001));
        Assert.That(rect.Y, Is.EqualTo(y).Within(0.0000001));
        Assert.That(rect.Width, Is.EqualTo(width).Within(0.0000001));
        Assert.That(rect.Height, Is.EqualTo(height).Within(0.0000001));
    }
}
