namespace chart.Tests;

[TestFixture]
public class ChartBinnerTests
{
    [Test]
    public void BinFixedCount_CreatesEqualWidthBinsAndCountsValues()
    {
        ChartHistogram histogram = ChartBinnerOperations.BinFixedCount(new double[] { 0.0, 1.0, 2.0, 3.0, 4.0, 5.0 }, 3);

        Assert.That(histogram.Strategy, Is.EqualTo(ChartHistogramBinStrategy.FixedCount));
        Assert.That(histogram.BinCount, Is.EqualTo(3));
        Assert.That(histogram.ValueCount, Is.EqualTo(6));
        Assert.That(histogram.Minimum, Is.EqualTo(0.0));
        Assert.That(histogram.Maximum, Is.EqualTo(5.0));
        Assert.That(histogram.BinWidth, Is.EqualTo(5.0 / 3.0).Within(0.0000001));
        Assert.That(histogram.GetBin(0).Minimum, Is.EqualTo(0.0).Within(0.0000001));
        Assert.That(histogram.GetBin(0).Maximum, Is.EqualTo(5.0 / 3.0).Within(0.0000001));
        Assert.That(histogram.GetBin(0).Count, Is.EqualTo(2));
        Assert.That(histogram.GetBin(1).Count, Is.EqualTo(2));
        Assert.That(histogram.GetBin(2).Count, Is.EqualTo(2));
        Assert.That(histogram.TotalCount(), Is.EqualTo(6));
    }

    [Test]
    public void BinFixedWidth_ExtendsMaximumToWholeBins()
    {
        ChartHistogram histogram = ChartBinnerOperations.BinFixedWidth(new double[] { 0.0, 0.5, 1.2, 2.8, 3.0 }, 1.0);

        Assert.That(histogram.Strategy, Is.EqualTo(ChartHistogramBinStrategy.FixedWidth));
        Assert.That(histogram.BinCount, Is.EqualTo(3));
        Assert.That(histogram.Minimum, Is.EqualTo(0.0));
        Assert.That(histogram.Maximum, Is.EqualTo(3.0));
        Assert.That(histogram.GetBin(0).Count, Is.EqualTo(2));
        Assert.That(histogram.GetBin(1).Count, Is.EqualTo(1));
        Assert.That(histogram.GetBin(2).Count, Is.EqualTo(2));
    }

    [Test]
    public void AutomaticRules_ComputeExpectedCountsAndWidths()
    {
        double[] values = new double[] { 0.0, 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0 };

        ChartHistogram sturges = ChartBinnerOperations.BinSturges(values);
        ChartHistogram scott = ChartBinnerOperations.BinScott(values);
        ChartHistogram freedmanDiaconis = ChartBinnerOperations.BinFreedmanDiaconis(values);

        Assert.That(ChartBinnerOperations.CalculateSturgesBinCount(values.Length), Is.EqualTo(4));
        Assert.That(sturges.BinCount, Is.EqualTo(4));
        Assert.That(sturges.GetBin(0).Count, Is.EqualTo(2));
        Assert.That(sturges.GetBin(3).Count, Is.EqualTo(2));

        Assert.That(ChartBinnerOperations.CalculateScottBinWidth(values), Is.EqualTo(4.0097535).Within(0.000001));
        Assert.That(scott.Strategy, Is.EqualTo(ChartHistogramBinStrategy.Scott));
        Assert.That(scott.BinCount, Is.EqualTo(2));

        Assert.That(ChartBinnerOperations.Percentile(values, 0.25), Is.EqualTo(1.75).Within(0.0000001));
        Assert.That(ChartBinnerOperations.Percentile(values, 0.75), Is.EqualTo(5.25).Within(0.0000001));
        Assert.That(ChartBinnerOperations.CalculateFreedmanDiaconisBinWidth(values), Is.EqualTo(3.5).Within(0.0000001));
        Assert.That(freedmanDiaconis.Strategy, Is.EqualTo(ChartHistogramBinStrategy.FreedmanDiaconis));
        Assert.That(freedmanDiaconis.BinCount, Is.EqualTo(2));
    }

    [Test]
    public void Bin_DispatchesByStrategy()
    {
        double[] values = new double[] { 0.0, 1.0, 2.0, 3.0 };

        Assert.That(ChartBinnerOperations.Bin(values, ChartHistogramBinStrategy.FixedCount, 2, 1.0).Strategy, Is.EqualTo(ChartHistogramBinStrategy.FixedCount));
        Assert.That(ChartBinnerOperations.Bin(values, ChartHistogramBinStrategy.FixedWidth, 2, 1.0).Strategy, Is.EqualTo(ChartHistogramBinStrategy.FixedWidth));
        Assert.That(ChartBinnerOperations.Bin(values, ChartHistogramBinStrategy.Sturges, 2, 1.0).Strategy, Is.EqualTo(ChartHistogramBinStrategy.Sturges));
        Assert.That(ChartBinnerOperations.Bin(values, ChartHistogramBinStrategy.Scott, 2, 1.0).Strategy, Is.EqualTo(ChartHistogramBinStrategy.Scott));
        Assert.That(ChartBinnerOperations.Bin(values, ChartHistogramBinStrategy.FreedmanDiaconis, 2, 1.0).Strategy, Is.EqualTo(ChartHistogramBinStrategy.FreedmanDiaconis));
    }

    [Test]
    public void Histogram_CopyBinsReturnsIndependentBins()
    {
        ChartHistogram histogram = ChartBinnerOperations.BinFixedCount(new double[] { 1.0, 2.0, 3.0 }, 2);

        ChartHistogramBin[] copy = histogram.CopyBins();
        copy[0].Count = 99;

        Assert.That(histogram.GetBin(0).Count, Is.Not.EqualTo(99));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => histogram.GetBin(2));
    }

    [Test]
    public void Binner_ValidatesInputs()
    {
        Assert.Throws<System.ArgumentNullException>(() => ChartBinnerOperations.BinFixedCount(null!, 2));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartBinnerOperations.BinFixedCount(new double[] { 1.0 }, 0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartBinnerOperations.BinFixedWidth(new double[] { 1.0 }, 0.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartBinnerOperations.BinFixedWidth(new double[] { 0.0 - double.MaxValue, double.MaxValue }, 1.0));
        Assert.Throws<System.ArgumentException>(() => ChartBinnerOperations.BinFixedWidth(new double[] { double.NaN }, 1.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartBinnerOperations.CalculateSturgesBinCount(0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartBinnerOperations.Percentile(new double[] { 1.0 }, 1.5));
        Assert.Throws<System.ArgumentException>(() => new ChartHistogramBin(1.0, 1.0));
        Assert.Throws<System.ArgumentException>(() => new ChartHistogram(new ChartHistogramBin[0], 0.0, 1.0, 1.0, 0, ChartHistogramBinStrategy.FixedCount));
    }
}
