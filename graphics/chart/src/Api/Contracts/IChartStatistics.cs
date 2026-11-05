namespace chart;
/// <summary>Defines substitutable chart statistics operations.</summary>
public interface IChartStatistics
{
    /// <summary>
    /// Returns the smallest finite value in the input array.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The resulting double.</returns>
    double Min(double[] values);
    /// <summary>
    /// Returns the largest finite value in the input array.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The resulting double.</returns>
    double Max(double[] values);
    /// <summary>
    /// Sums all input values using compensated summation to reduce floating-point error.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The resulting double.</returns>
    double Sum(double[] values);
    /// <summary>
    /// Returns the arithmetic mean of all input values.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The resulting double.</returns>
    double Mean(double[] values);
    /// <summary>
    /// Sorts a copy of the input and returns its middle value or the mean of its two middle values.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The resulting double.</returns>
    double Median(double[] values);
    /// <summary>
    /// Returns the population variance, dividing the squared deviations by the value count.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The resulting double.</returns>
    double Variance(double[] values);
    /// <summary>
    /// Returns the square root of the population variance.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The resulting double.</returns>
    double StandardDeviation(double[] values);
    /// <summary>
    /// Returns the population covariance of two equal-length value arrays.
    /// </summary>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <returns>The resulting double.</returns>
    double Covariance(double[] xValues, double[] yValues);
    /// <summary>
    /// Returns the Pearson correlation coefficient for two equal-length value arrays.
    /// </summary>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <returns>The resulting double.</returns>
    /// <exception cref = "System.InvalidOperationException">Thrown when the current object state cannot perform this operation.</exception>
    double Correlation(double[] xValues, double[] yValues);
    /// <summary>Returns the R-7 quantile for a probability from zero through one.</summary>
    /// <param name = "values">The finite input values.</param>
    /// <param name = "probability">The quantile probability from zero through one.</param>
    /// <returns>The interpolated quantile.</returns>
    double Quantile(double[] values, double probability);
    /// <summary>Returns the R-7 percentile for a percentage from zero through 100.</summary>
    /// <param name = "values">The finite input values.</param>
    /// <param name = "percentile">The percentile from zero through 100.</param>
    /// <returns>The interpolated percentile.</returns>
    double Percentile(double[] values, double percentile);
    /// <summary>Returns the first quartile, median, and third quartile in that order.</summary>
    /// <param name = "values">The finite input values.</param>
    /// <returns>A three-element quartile array.</returns>
    double[] Quartiles(double[] values);
    /// <summary>Returns the difference between the third and first quartiles.</summary>
    /// <param name = "values">The finite input values.</param>
    /// <returns>The interquartile range.</returns>
    double InterquartileRange(double[] values);
    /// <summary>Returns the median absolute deviation from the sample median.</summary>
    /// <param name = "values">The finite input values.</param>
    /// <returns>The unscaled median absolute deviation.</returns>
    double MedianAbsoluteDeviation(double[] values);
    /// <summary>Returns population variance using a stable one-pass update.</summary>
    /// <param name = "values">The finite input values.</param>
    /// <returns>The population variance.</returns>
    double PopulationVariance(double[] values);
    /// <summary>Returns sample variance using Bessel's correction.</summary>
    /// <param name = "values">The finite input values.</param>
    /// <returns>The sample variance.</returns>
    double SampleVariance(double[] values);
    /// <summary>Returns the square root of population variance.</summary>
    /// <param name = "values">The finite input values.</param>
    /// <returns>The population standard deviation.</returns>
    double PopulationStandardDeviation(double[] values);
    /// <summary>Returns the square root of sample variance.</summary>
    /// <param name = "values">The finite input values.</param>
    /// <returns>The sample standard deviation.</returns>
    double SampleStandardDeviation(double[] values);
    /// <summary>Returns population covariance using stable online means.</summary>
    /// <param name = "xValues">The finite horizontal values.</param>
    /// <param name = "yValues">The finite vertical values.</param>
    /// <returns>The population covariance.</returns>
    double PopulationCovariance(double[] xValues, double[] yValues);
    /// <summary>Returns sample covariance using Bessel's correction.</summary>
    /// <param name = "xValues">The finite horizontal values.</param>
    /// <param name = "yValues">The finite vertical values.</param>
    /// <returns>The sample covariance.</returns>
    double SampleCovariance(double[] xValues, double[] yValues);
    /// <summary>Linearly interpolates between two finite endpoints.</summary>
    /// <param name = "start">The finite starting value.</param>
    /// <param name = "end">The finite ending value.</param>
    /// <param name = "amount">The finite interpolation amount; values outside zero through one extrapolate.</param>
    /// <returns>The interpolated value.</returns>
    double Interpolate(double start, double end, double amount);
    /// <summary>Returns the interpolation amount of a finite value between two distinct endpoints.</summary>
    /// <param name = "start">The finite starting value.</param>
    /// <param name = "end">The finite ending value.</param>
    /// <param name = "value">The finite value to invert.</param>
    /// <returns>The inverse interpolation amount.</returns>
    double InverseInterpolate(double start, double end, double value);
    /// <summary>Rounds a positive value to a power-of-ten multiple of one, two, five, or ten.</summary>
    /// <param name = "value">The positive finite value.</param>
    /// <param name = "round"><c>true</c> to choose the nearest nice value; <c>false</c> to choose a ceiling.</param>
    /// <returns>The nice number.</returns>
    double NiceNumber(double value, bool round);
    /// <summary>Returns the minimum using explicit not-a-number and infinity policies.</summary>
    /// <param name = "values">The input values.</param>
    /// <param name = "nanPolicy">The not-a-number policy.</param>
    /// <param name = "infinityPolicy">The infinity policy.</param>
    /// <returns>The policy-aware minimum.</returns>
    double MinWithPolicy(double[] values, ChartNaNPolicy nanPolicy, ChartInfinityPolicy infinityPolicy);
    /// <summary>Returns the maximum using explicit not-a-number and infinity policies.</summary>
    /// <param name = "values">The input values.</param>
    /// <param name = "nanPolicy">The not-a-number policy.</param>
    /// <param name = "infinityPolicy">The infinity policy.</param>
    /// <returns>The policy-aware maximum.</returns>
    double MaxWithPolicy(double[] values, ChartNaNPolicy nanPolicy, ChartInfinityPolicy infinityPolicy);
    /// <summary>Returns the sum using explicit not-a-number and infinity policies.</summary>
    /// <param name = "values">The input values.</param>
    /// <param name = "nanPolicy">The not-a-number policy.</param>
    /// <param name = "infinityPolicy">The infinity policy.</param>
    /// <returns>The policy-aware sum.</returns>
    double SumWithPolicy(double[] values, ChartNaNPolicy nanPolicy, ChartInfinityPolicy infinityPolicy);
    /// <summary>Returns the arithmetic mean using explicit not-a-number and infinity policies.</summary>
    /// <param name = "values">The input values.</param>
    /// <param name = "nanPolicy">The not-a-number policy.</param>
    /// <param name = "infinityPolicy">The infinity policy.</param>
    /// <returns>The policy-aware mean.</returns>
    double MeanWithPolicy(double[] values, ChartNaNPolicy nanPolicy, ChartInfinityPolicy infinityPolicy);
}
