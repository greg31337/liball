namespace chart;
/// <summary>Provides the public chart statistics operations.</summary>
public class ChartStatistics : IChartStatistics
{
    /// <summary>Initializes a stateless chart statistics facade.</summary>
    public ChartStatistics()
    {
    }

    /// <summary>
    /// Returns the smallest finite value in the input array.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The resulting double.</returns>
    public double Min(double[] values)
    {
        return ChartStatisticsOperations.Min(values);
    }

    /// <summary>
    /// Returns the largest finite value in the input array.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The resulting double.</returns>
    public double Max(double[] values)
    {
        return ChartStatisticsOperations.Max(values);
    }

    /// <summary>
    /// Sums all input values using compensated summation to reduce floating-point error.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The resulting double.</returns>
    public double Sum(double[] values)
    {
        ChartStatisticsOperations.ValidateFiniteValues(values);
        return ChartStatisticsOperations.CompensatedFiniteSum(values);
    }

    /// <summary>
    /// Returns the arithmetic mean of all input values.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The resulting double.</returns>
    public double Mean(double[] values)
    {
        return ChartStatisticsOperations.Mean(values);
    }

    /// <summary>
    /// Sorts a copy of the input and returns its middle value or the mean of its two middle values.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The resulting double.</returns>
    public double Median(double[] values)
    {
        return ChartStatisticsOperations.Median(values);
    }

    /// <summary>
    /// Returns the population variance, dividing the squared deviations by the value count.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The resulting double.</returns>
    public double Variance(double[] values)
    {
        return ChartStatisticsOperations.Variance(values);
    }

    /// <summary>
    /// Returns the square root of the population variance.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The resulting double.</returns>
    public double StandardDeviation(double[] values)
    {
        return ChartStatisticsOperations.StandardDeviation(values);
    }

    /// <summary>
    /// Returns the population covariance of two equal-length value arrays.
    /// </summary>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <returns>The resulting double.</returns>
    public double Covariance(double[] xValues, double[] yValues)
    {
        return ChartStatisticsOperations.Covariance(xValues, yValues);
    }

    /// <summary>
    /// Returns the Pearson correlation coefficient for two equal-length value arrays.
    /// </summary>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <returns>The resulting double.</returns>
    /// <exception cref = "System.InvalidOperationException">Thrown when the current object state cannot perform this operation.</exception>
    public double Correlation(double[] xValues, double[] yValues)
    {
        return ChartStatisticsOperations.Correlation(xValues, yValues);
    }

    /// <summary>Returns the R-7 quantile for a probability from zero through one.</summary>
    /// <param name = "values">The finite input values.</param>
    /// <param name = "probability">The quantile probability from zero through one.</param>
    /// <returns>The interpolated quantile.</returns>
    public double Quantile(double[] values, double probability)
    {
        return ChartStatisticsOperations.Quantile(values, probability);
    }

    /// <summary>Returns the R-7 percentile for a percentage from zero through 100.</summary>
    /// <param name = "values">The finite input values.</param>
    /// <param name = "percentile">The percentile from zero through 100.</param>
    /// <returns>The interpolated percentile.</returns>
    public double Percentile(double[] values, double percentile)
    {
        return ChartStatisticsOperations.Percentile(values, percentile);
    }

    /// <summary>Returns the first quartile, median, and third quartile in that order.</summary>
    /// <param name = "values">The finite input values.</param>
    /// <returns>A three-element quartile array.</returns>
    public double[] Quartiles(double[] values)
    {
        return ChartStatisticsOperations.Quartiles(values);
    }

    /// <summary>Returns the difference between the third and first quartiles.</summary>
    /// <param name = "values">The finite input values.</param>
    /// <returns>The interquartile range.</returns>
    public double InterquartileRange(double[] values)
    {
        return ChartStatisticsOperations.InterquartileRange(values);
    }

    /// <summary>Returns the median absolute deviation from the sample median.</summary>
    /// <param name = "values">The finite input values.</param>
    /// <returns>The unscaled median absolute deviation.</returns>
    public double MedianAbsoluteDeviation(double[] values)
    {
        return ChartStatisticsOperations.MedianAbsoluteDeviation(values);
    }

    /// <summary>Returns population variance using a stable one-pass update.</summary>
    /// <param name = "values">The finite input values.</param>
    /// <returns>The population variance.</returns>
    public double PopulationVariance(double[] values)
    {
        return ChartStatisticsOperations.PopulationVariance(values);
    }

    /// <summary>Returns sample variance using Bessel's correction.</summary>
    /// <param name = "values">The finite input values.</param>
    /// <returns>The sample variance.</returns>
    public double SampleVariance(double[] values)
    {
        return ChartStatisticsOperations.SampleVariance(values);
    }

    /// <summary>Returns the square root of population variance.</summary>
    /// <param name = "values">The finite input values.</param>
    /// <returns>The population standard deviation.</returns>
    public double PopulationStandardDeviation(double[] values)
    {
        return ChartStatisticsOperations.PopulationStandardDeviation(values);
    }

    /// <summary>Returns the square root of sample variance.</summary>
    /// <param name = "values">The finite input values.</param>
    /// <returns>The sample standard deviation.</returns>
    public double SampleStandardDeviation(double[] values)
    {
        return ChartStatisticsOperations.SampleStandardDeviation(values);
    }

    /// <summary>Returns population covariance using stable online means.</summary>
    /// <param name = "xValues">The finite horizontal values.</param>
    /// <param name = "yValues">The finite vertical values.</param>
    /// <returns>The population covariance.</returns>
    public double PopulationCovariance(double[] xValues, double[] yValues)
    {
        return ChartStatisticsOperations.PopulationCovariance(xValues, yValues);
    }

    /// <summary>Returns sample covariance using Bessel's correction.</summary>
    /// <param name = "xValues">The finite horizontal values.</param>
    /// <param name = "yValues">The finite vertical values.</param>
    /// <returns>The sample covariance.</returns>
    public double SampleCovariance(double[] xValues, double[] yValues)
    {
        return ChartStatisticsOperations.SampleCovariance(xValues, yValues);
    }

    /// <summary>Linearly interpolates between two finite endpoints.</summary>
    /// <param name = "start">The finite starting value.</param>
    /// <param name = "end">The finite ending value.</param>
    /// <param name = "amount">The finite interpolation amount; values outside zero through one extrapolate.</param>
    /// <returns>The interpolated value.</returns>
    public double Interpolate(double start, double end, double amount)
    {
        return ChartStatisticsOperations.Interpolate(start, end, amount);
    }

    /// <summary>Returns the interpolation amount of a finite value between two distinct endpoints.</summary>
    /// <param name = "start">The finite starting value.</param>
    /// <param name = "end">The finite ending value.</param>
    /// <param name = "value">The finite value to invert.</param>
    /// <returns>The inverse interpolation amount.</returns>
    public double InverseInterpolate(double start, double end, double value)
    {
        return ChartStatisticsOperations.InverseInterpolate(start, end, value);
    }

    /// <summary>Rounds a positive value to a power-of-ten multiple of one, two, five, or ten.</summary>
    /// <param name = "value">The positive finite value.</param>
    /// <param name = "round"><c>true</c> to choose the nearest nice value; <c>false</c> to choose a ceiling.</param>
    /// <returns>The nice number.</returns>
    public double NiceNumber(double value, bool round)
    {
        return ChartStatisticsOperations.NiceNumber(value, round);
    }

    /// <summary>Returns the minimum using explicit not-a-number and infinity policies.</summary>
    /// <param name = "values">The input values.</param>
    /// <param name = "nanPolicy">The not-a-number policy.</param>
    /// <param name = "infinityPolicy">The infinity policy.</param>
    /// <returns>The policy-aware minimum.</returns>
    public double MinWithPolicy(double[] values, ChartNaNPolicy nanPolicy, ChartInfinityPolicy infinityPolicy)
    {
        return ChartStatisticsOperations.MinWithPolicy(values, nanPolicy, infinityPolicy);
    }

    /// <summary>Returns the maximum using explicit not-a-number and infinity policies.</summary>
    /// <param name = "values">The input values.</param>
    /// <param name = "nanPolicy">The not-a-number policy.</param>
    /// <param name = "infinityPolicy">The infinity policy.</param>
    /// <returns>The policy-aware maximum.</returns>
    public double MaxWithPolicy(double[] values, ChartNaNPolicy nanPolicy, ChartInfinityPolicy infinityPolicy)
    {
        return ChartStatisticsOperations.MaxWithPolicy(values, nanPolicy, infinityPolicy);
    }

    /// <summary>Returns the sum using explicit not-a-number and infinity policies.</summary>
    /// <param name = "values">The input values.</param>
    /// <param name = "nanPolicy">The not-a-number policy.</param>
    /// <param name = "infinityPolicy">The infinity policy.</param>
    /// <returns>The policy-aware sum.</returns>
    public double SumWithPolicy(double[] values, ChartNaNPolicy nanPolicy, ChartInfinityPolicy infinityPolicy)
    {
        return ChartStatisticsOperations.SumWithPolicy(values, nanPolicy, infinityPolicy);
    }

    /// <summary>Returns the arithmetic mean using explicit not-a-number and infinity policies.</summary>
    /// <param name = "values">The input values.</param>
    /// <param name = "nanPolicy">The not-a-number policy.</param>
    /// <param name = "infinityPolicy">The infinity policy.</param>
    /// <returns>The policy-aware mean.</returns>
    public double MeanWithPolicy(double[] values, ChartNaNPolicy nanPolicy, ChartInfinityPolicy infinityPolicy)
    {
        return ChartStatisticsOperations.MeanWithPolicy(values, nanPolicy, infinityPolicy);
    }
}
