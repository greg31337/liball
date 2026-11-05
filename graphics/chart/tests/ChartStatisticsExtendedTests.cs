namespace chart.Tests;

[TestFixture]
public class ChartStatisticsExtendedTests
{
    [Test]
    public void Sum_UsesCompensationForCancellationSensitiveInputs()
    {
        IChartStatistics statistics = new ChartStatistics();
        double[] values = new double[] { 10000000000000000.0, 1.0, -10000000000000000.0 };

        Assert.That(statistics.Sum(values), Is.EqualTo(1.0));
        Assert.That(statistics.Mean(values), Is.EqualTo(1.0 / 3.0).Within(0.000000000000001));
    }

    [Test]
    public void QuantilesPercentilesAndQuartiles_UseR7Interpolation()
    {
        ChartStatistics statistics = new ChartStatistics();
        double[] values = new double[] { 8.0, 1.0, 6.0, 3.0, 5.0, 2.0, 7.0, 4.0 };

        Assert.That(statistics.Quantile(values, 0.0), Is.EqualTo(1.0));
        Assert.That(statistics.Quantile(values, 0.25), Is.EqualTo(2.75));
        Assert.That(statistics.Quantile(values, 0.5), Is.EqualTo(4.5));
        Assert.That(statistics.Percentile(values, 75.0), Is.EqualTo(6.25));
        Assert.That(statistics.Quantile(values, 1.0), Is.EqualTo(8.0));
        Assert.That(statistics.Quartiles(values), Is.EqualTo(new double[] { 2.75, 4.5, 6.25 }));
        Assert.That(values, Is.EqualTo(new double[] { 8.0, 1.0, 6.0, 3.0, 5.0, 2.0, 7.0, 4.0 }));
    }

    [Test]
    public void InterquartileRangeAndMedianAbsoluteDeviation_AreRobustStatistics()
    {
        ChartStatistics statistics = new ChartStatistics();
        double[] values = new double[] { 1.0, 2.0, 3.0, 4.0, 100.0 };

        Assert.That(statistics.InterquartileRange(values), Is.EqualTo(2.0));
        Assert.That(statistics.MedianAbsoluteDeviation(values), Is.EqualTo(1.0));
    }

    [Test]
    public void PopulationAndSampleDispersion_UseTheirDocumentedDivisors()
    {
        ChartStatistics statistics = new ChartStatistics();
        double[] values = new double[] { 1.0, 2.0, 3.0, 4.0 };

        Assert.That(statistics.Variance(values), Is.EqualTo(1.25).Within(0.0000001));
        Assert.That(statistics.PopulationVariance(values), Is.EqualTo(1.25).Within(0.0000001));
        Assert.That(statistics.SampleVariance(values), Is.EqualTo(5.0 / 3.0).Within(0.0000001));
        Assert.That(statistics.PopulationStandardDeviation(values), Is.EqualTo(System.Math.Sqrt(1.25)).Within(0.0000001));
        Assert.That(statistics.SampleStandardDeviation(values), Is.EqualTo(System.Math.Sqrt(5.0 / 3.0)).Within(0.0000001));
        Assert.Throws<System.ArgumentException>(() => statistics.SampleVariance(new double[] { 1.0 }));
    }

    [Test]
    public void PopulationSampleCovarianceAndCorrelation_UseStablePairedUpdates()
    {
        ChartStatistics statistics = new ChartStatistics();
        double[] x = new double[] { 1000000000001.0, 1000000000002.0, 1000000000003.0 };
        double[] y = new double[] { 2.0, 4.0, 6.0 };

        Assert.That(statistics.Covariance(x, y), Is.EqualTo(4.0 / 3.0).Within(0.0000001));
        Assert.That(statistics.PopulationCovariance(x, y), Is.EqualTo(4.0 / 3.0).Within(0.0000001));
        Assert.That(statistics.SampleCovariance(x, y), Is.EqualTo(2.0).Within(0.0000001));
        Assert.That(statistics.Correlation(x, y), Is.EqualTo(1.0).Within(0.0000001));
        Assert.Throws<System.ArgumentException>(() => statistics.SampleCovariance(new double[] { 1.0 }, new double[] { 2.0 }));
    }

    [Test]
    public void InterpolationAndNiceNumbers_HandleMappingAndTickSizing()
    {
        ChartStatistics statistics = new ChartStatistics();

        Assert.That(statistics.Interpolate(10.0, 20.0, 0.25), Is.EqualTo(12.5));
        Assert.That(statistics.Interpolate(10.0, 20.0, 1.5), Is.EqualTo(25.0));
        Assert.That(statistics.InverseInterpolate(10.0, 20.0, 12.5), Is.EqualTo(0.25));
        Assert.That(statistics.NiceNumber(1.6, true), Is.EqualTo(2.0));
        Assert.That(statistics.NiceNumber(4.2, true), Is.EqualTo(5.0));
        Assert.That(statistics.NiceNumber(5.1, false), Is.EqualTo(10.0));
        Assert.That(statistics.NiceNumber(0.032, false), Is.EqualTo(0.05).Within(0.0000001));
        Assert.Throws<System.ArgumentException>(() => statistics.InverseInterpolate(1.0, 1.0, 1.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => statistics.NiceNumber(0.0, true));
    }

    [Test]
    public void PolicyAwareAggregates_RejectSkipPropagateAndAllowExplicitly()
    {
        ChartStatistics statistics = new ChartStatistics();
        double[] mixed = new double[] { 1.0, double.NaN, 3.0, double.PositiveInfinity };

        Assert.Throws<System.ArgumentException>(() => statistics.MinWithPolicy(mixed, ChartNaNPolicy.Reject, ChartInfinityPolicy.Allow));
        Assert.Throws<System.ArgumentException>(() => statistics.MaxWithPolicy(mixed, ChartNaNPolicy.Skip, ChartInfinityPolicy.Reject));
        Assert.That(statistics.MinWithPolicy(mixed, ChartNaNPolicy.Skip, ChartInfinityPolicy.Allow), Is.EqualTo(1.0));
        Assert.That(statistics.MaxWithPolicy(mixed, ChartNaNPolicy.Skip, ChartInfinityPolicy.Allow), Is.EqualTo(double.PositiveInfinity));
        Assert.That(statistics.MeanWithPolicy(mixed, ChartNaNPolicy.Skip, ChartInfinityPolicy.Skip), Is.EqualTo(2.0));
        Assert.That(double.IsNaN(statistics.SumWithPolicy(mixed, ChartNaNPolicy.Propagate, ChartInfinityPolicy.Allow)), Is.True);
        Assert.That(statistics.SumWithPolicy(mixed, ChartNaNPolicy.Skip, ChartInfinityPolicy.Allow), Is.EqualTo(double.PositiveInfinity));
    }

    [Test]
    public void ExtendedStatistics_ValidateProbabilitiesPoliciesAndFiniteInputs()
    {
        ChartStatistics statistics = new ChartStatistics();

        Assert.Throws<System.ArgumentOutOfRangeException>(() => statistics.Quantile(new double[] { 1.0 }, -0.1));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => statistics.Percentile(new double[] { 1.0 }, 101.0));
        Assert.Throws<System.ArgumentException>(() => statistics.Quantile(new double[] { 1.0, double.NaN }, 0.5));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => statistics.Interpolate(0.0, 1.0, double.NaN));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => statistics.MinWithPolicy(new double[] { 1.0 }, (ChartNaNPolicy)99, ChartInfinityPolicy.Reject));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => statistics.MaxWithPolicy(new double[] { 1.0 }, ChartNaNPolicy.Reject, (ChartInfinityPolicy)99));
        Assert.Throws<System.ArgumentException>(() => statistics.MeanWithPolicy(new double[] { double.NaN }, ChartNaNPolicy.Skip, ChartInfinityPolicy.Skip));
    }
}
