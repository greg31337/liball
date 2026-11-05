namespace chart.Tests;

[TestFixture]
public class ChartStatisticsTests
{
    [Test]
    public void SummaryHelpers_ComputeMinMaxSumMeanAndMedian()
    {
        double[] even = new double[] { 4.0, 1.0, 3.0, 2.0 };
        double[] odd = new double[] { 9.0, 1.0, 5.0 };

        Assert.That(ChartStatisticsOperations.Min(even), Is.EqualTo(1.0));
        Assert.That(ChartStatisticsOperations.Max(even), Is.EqualTo(4.0));
        Assert.That(ChartStatisticsOperations.Sum(even), Is.EqualTo(10.0));
        Assert.That(ChartStatisticsOperations.Mean(even), Is.EqualTo(2.5));
        Assert.That(ChartStatisticsOperations.Median(even), Is.EqualTo(2.5));
        Assert.That(ChartStatisticsOperations.Median(odd), Is.EqualTo(5.0));
        Assert.That(even, Is.EqualTo(new double[] { 4.0, 1.0, 3.0, 2.0 }));
    }

    [Test]
    public void VarianceAndStandardDeviation_UsePopulationFormula()
    {
        double[] values = new double[] { 1.0, 2.0, 3.0, 4.0 };

        Assert.That(ChartStatisticsOperations.Variance(values), Is.EqualTo(1.25).Within(0.0000001));
        Assert.That(ChartStatisticsOperations.StandardDeviation(values), Is.EqualTo(System.Math.Sqrt(1.25)).Within(0.0000001));
    }

    [Test]
    public void CovarianceAndCorrelation_ComputePairedPopulationStatistics()
    {
        double[] x = new double[] { 1.0, 2.0, 3.0 };
        double[] y = new double[] { 2.0, 4.0, 6.0 };
        double[] inverse = new double[] { 6.0, 4.0, 2.0 };

        Assert.That(ChartStatisticsOperations.Covariance(x, y), Is.EqualTo(4.0 / 3.0).Within(0.0000001));
        Assert.That(ChartStatisticsOperations.Correlation(x, y), Is.EqualTo(1.0).Within(0.0000001));
        Assert.That(ChartStatisticsOperations.Correlation(x, inverse), Is.EqualTo(-1.0).Within(0.0000001));
    }

    [Test]
    public void SummaryHelpers_ValidateInputs()
    {
        Assert.Throws<System.ArgumentNullException>(() => ChartStatisticsOperations.Min(null!));
        Assert.Throws<System.ArgumentException>(() => ChartStatisticsOperations.Max(new double[0]));
        Assert.Throws<System.ArgumentException>(() => ChartStatisticsOperations.Sum(new double[] { 1.0, double.NaN }));
        Assert.Throws<System.ArgumentException>(() => ChartStatisticsOperations.Mean(new double[] { double.PositiveInfinity }));
        Assert.Throws<System.ArgumentException>(() => ChartStatisticsOperations.Covariance(new double[] { 1.0 }, new double[] { 1.0, 2.0 }));
        Assert.Throws<System.InvalidOperationException>(() => ChartStatisticsOperations.Correlation(new double[] { 1.0, 1.0 }, new double[] { 2.0, 3.0 }));
    }
}
