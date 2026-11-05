namespace chart;

/// <summary>
/// Provides operations for statistics.
/// </summary>
internal static class ChartStatisticsOperations
{
    /// <summary>
    /// Returns the smallest finite value in the input array.
    /// </summary>
    /// <param name="values">The input values to process.</param>
    /// <returns>The resulting double.</returns>
    public static double Min(double[] values)
    {
        ValidateFiniteValues(values);

        double minimum = values[0];
        int index = 1;
        while (index < values.Length)
        {
            if (values[index] < minimum)
            {
                minimum = values[index];
            }

            index = index + 1;
        }

        return minimum;
    }

    /// <summary>
    /// Returns the largest finite value in the input array.
    /// </summary>
    /// <param name="values">The input values to process.</param>
    /// <returns>The resulting double.</returns>
    public static double Max(double[] values)
    {
        ValidateFiniteValues(values);

        double maximum = values[0];
        int index = 1;
        while (index < values.Length)
        {
            if (values[index] > maximum)
            {
                maximum = values[index];
            }

            index = index + 1;
        }

        return maximum;
    }

    /// <summary>
    /// Sums all input values using compensated summation to reduce floating-point error.
    /// </summary>
    /// <param name="values">The input values to process.</param>
    /// <returns>The resulting double.</returns>
    public static double Sum(double[] values)
    {
        ValidateFiniteValues(values);
        return CompensatedFiniteSum(values);
    }

    /// <summary>
    /// Returns the arithmetic mean of all input values.
    /// </summary>
    /// <param name="values">The input values to process.</param>
    /// <returns>The resulting double.</returns>
    public static double Mean(double[] values)
    {
        ValidateFiniteValues(values);
        return CompensatedFiniteSum(values) / values.Length;
    }

    /// <summary>
    /// Sorts a copy of the input and returns its middle value or the mean of its two middle values.
    /// </summary>
    /// <param name="values">The input values to process.</param>
    /// <returns>The resulting double.</returns>
    public static double Median(double[] values)
    {
        return Quantile(values, 0.5);
    }

    /// <summary>
    /// Returns the population variance, dividing the squared deviations by the value count.
    /// </summary>
    /// <param name="values">The input values to process.</param>
    /// <returns>The resulting double.</returns>
    public static double Variance(double[] values)
    {
        return PopulationVariance(values);
    }

    /// <summary>
    /// Returns the square root of the population variance.
    /// </summary>
    /// <param name="values">The input values to process.</param>
    /// <returns>The resulting double.</returns>
    public static double StandardDeviation(double[] values)
    {
        return System.Math.Sqrt(Variance(values));
    }

    /// <summary>
    /// Returns the population covariance of two equal-length value arrays.
    /// </summary>
    /// <param name="xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name="yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <returns>The resulting double.</returns>
    public static double Covariance(double[] xValues, double[] yValues)
    {
        return PopulationCovariance(xValues, yValues);
    }

    /// <summary>
    /// Returns the Pearson correlation coefficient for two equal-length value arrays.
    /// </summary>
    /// <param name="xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name="yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <returns>The resulting double.</returns>
    /// <exception cref="System.InvalidOperationException">Thrown when the current object state cannot perform this operation.</exception>
    public static double Correlation(double[] xValues, double[] yValues)
    {
        ValidateFinitePairs(xValues, yValues);
        double xVariance = PopulationVariance(xValues);
        double yVariance = PopulationVariance(yValues);

        if (xVariance == 0.0 || yVariance == 0.0)
        {
            throw new System.InvalidOperationException("Correlation is undefined for zero-variance data.");
        }

        return PopulationCovariance(xValues, yValues) / System.Math.Sqrt(xVariance * yVariance);
    }

    /// <summary>Returns the R-7 quantile for a probability from zero through one.</summary>
    /// <param name="values">The finite input values.</param>
    /// <param name="probability">The quantile probability from zero through one.</param>
    /// <returns>The interpolated quantile.</returns>
    public static double Quantile(double[] values, double probability)
    {
        ValidateFiniteValues(values);
        if (double.IsNaN(probability) || double.IsInfinity(probability) || probability < 0.0 || probability > 1.0)
        {
            throw new System.ArgumentOutOfRangeException("probability");
        }

        double[] sorted = ChartDataMatrix.CopyDoubleArray(values);
        System.Array.Sort(sorted);
        double position = (sorted.Length - 1) * probability;
        int lowerIndex = (int)System.Math.Floor(position);
        int upperIndex = (int)System.Math.Ceiling(position);
        if (lowerIndex == upperIndex)
        {
            return sorted[lowerIndex];
        }

        double amount = position - lowerIndex;
        return sorted[lowerIndex] + (sorted[upperIndex] - sorted[lowerIndex]) * amount;
    }

    /// <summary>Returns the R-7 percentile for a percentage from zero through 100.</summary>
    /// <param name="values">The finite input values.</param>
    /// <param name="percentile">The percentile from zero through 100.</param>
    /// <returns>The interpolated percentile.</returns>
    public static double Percentile(double[] values, double percentile)
    {
        if (double.IsNaN(percentile) || double.IsInfinity(percentile) || percentile < 0.0 || percentile > 100.0)
        {
            throw new System.ArgumentOutOfRangeException("percentile");
        }

        return Quantile(values, percentile / 100.0);
    }

    /// <summary>Returns the first quartile, median, and third quartile in that order.</summary>
    /// <param name="values">The finite input values.</param>
    /// <returns>A three-element quartile array.</returns>
    public static double[] Quartiles(double[] values)
    {
        double[] quartiles = new double[3];
        quartiles[0] = Quantile(values, 0.25);
        quartiles[1] = Quantile(values, 0.5);
        quartiles[2] = Quantile(values, 0.75);
        return quartiles;
    }

    /// <summary>Returns the difference between the third and first quartiles.</summary>
    /// <param name="values">The finite input values.</param>
    /// <returns>The interquartile range.</returns>
    public static double InterquartileRange(double[] values)
    {
        double[] quartiles = Quartiles(values);
        return quartiles[2] - quartiles[0];
    }

    /// <summary>Returns the median absolute deviation from the sample median.</summary>
    /// <param name="values">The finite input values.</param>
    /// <returns>The unscaled median absolute deviation.</returns>
    public static double MedianAbsoluteDeviation(double[] values)
    {
        ValidateFiniteValues(values);
        double median = Quantile(values, 0.5);
        double[] deviations = new double[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            deviations[index] = System.Math.Abs(values[index] - median);
            index = index + 1;
        }

        return Quantile(deviations, 0.5);
    }

    /// <summary>Returns population variance using a stable one-pass update.</summary>
    /// <param name="values">The finite input values.</param>
    /// <returns>The population variance.</returns>
    public static double PopulationVariance(double[] values)
    {
        ValidateFiniteValues(values);
        return SumSquaredDeviations(values) / values.Length;
    }

    /// <summary>Returns sample variance using Bessel's correction.</summary>
    /// <param name="values">The finite input values.</param>
    /// <returns>The sample variance.</returns>
    public static double SampleVariance(double[] values)
    {
        ValidateSampleValues(values);
        return SumSquaredDeviations(values) / (values.Length - 1);
    }

    /// <summary>Returns the square root of population variance.</summary>
    /// <param name="values">The finite input values.</param>
    /// <returns>The population standard deviation.</returns>
    public static double PopulationStandardDeviation(double[] values)
    {
        return System.Math.Sqrt(PopulationVariance(values));
    }

    /// <summary>Returns the square root of sample variance.</summary>
    /// <param name="values">The finite input values.</param>
    /// <returns>The sample standard deviation.</returns>
    public static double SampleStandardDeviation(double[] values)
    {
        return System.Math.Sqrt(SampleVariance(values));
    }

    /// <summary>Returns population covariance using stable online means.</summary>
    /// <param name="xValues">The finite horizontal values.</param>
    /// <param name="yValues">The finite vertical values.</param>
    /// <returns>The population covariance.</returns>
    public static double PopulationCovariance(double[] xValues, double[] yValues)
    {
        ValidateFinitePairs(xValues, yValues);
        return SumCrossDeviations(xValues, yValues) / xValues.Length;
    }

    /// <summary>Returns sample covariance using Bessel's correction.</summary>
    /// <param name="xValues">The finite horizontal values.</param>
    /// <param name="yValues">The finite vertical values.</param>
    /// <returns>The sample covariance.</returns>
    public static double SampleCovariance(double[] xValues, double[] yValues)
    {
        ValidateFinitePairs(xValues, yValues);
        if (xValues.Length < 2)
        {
            throw new System.ArgumentException("Sample covariance requires at least two pairs.");
        }

        return SumCrossDeviations(xValues, yValues) / (xValues.Length - 1);
    }

    /// <summary>Linearly interpolates between two finite endpoints.</summary>
    /// <param name="start">The finite starting value.</param>
    /// <param name="end">The finite ending value.</param>
    /// <param name="amount">The finite interpolation amount; values outside zero through one extrapolate.</param>
    /// <returns>The interpolated value.</returns>
    public static double Interpolate(double start, double end, double amount)
    {
        ChartLinearScale.ValidateFinite(start, "start");
        ChartLinearScale.ValidateFinite(end, "end");
        ChartLinearScale.ValidateFinite(amount, "amount");
        return start + (end - start) * amount;
    }

    /// <summary>Returns the interpolation amount of a finite value between two distinct endpoints.</summary>
    /// <param name="start">The finite starting value.</param>
    /// <param name="end">The finite ending value.</param>
    /// <param name="value">The finite value to invert.</param>
    /// <returns>The inverse interpolation amount.</returns>
    public static double InverseInterpolate(double start, double end, double value)
    {
        ChartLinearScale.ValidateFinite(start, "start");
        ChartLinearScale.ValidateFinite(end, "end");
        ChartLinearScale.ValidateFinite(value, "value");
        if (start == end)
        {
            throw new System.ArgumentException("Interpolation endpoints must be distinct.");
        }

        return (value - start) / (end - start);
    }

    /// <summary>Rounds a positive value to a power-of-ten multiple of one, two, five, or ten.</summary>
    /// <param name="value">The positive finite value.</param>
    /// <param name="round"><c>true</c> to choose the nearest nice value; <c>false</c> to choose a ceiling.</param>
    /// <returns>The nice number.</returns>
    public static double NiceNumber(double value, bool round)
    {
        ChartRenderStyle.ValidatePositive(value, "value");
        double exponent = System.Math.Floor(System.Math.Log10(value));
        double power = System.Math.Pow(10.0, exponent);
        double fraction = value / power;
        double niceFraction = 10.0;
        if (round)
        {
            if (fraction < 1.5)
            {
                niceFraction = 1.0;
            }
            else if (fraction < 3.0)
            {
                niceFraction = 2.0;
            }
            else if (fraction < 7.0)
            {
                niceFraction = 5.0;
            }
        }
        else if (fraction <= 1.0)
        {
            niceFraction = 1.0;
        }
        else if (fraction <= 2.0)
        {
            niceFraction = 2.0;
        }
        else if (fraction <= 5.0)
        {
            niceFraction = 5.0;
        }

        return niceFraction * power;
    }

    /// <summary>Returns the minimum using explicit not-a-number and infinity policies.</summary>
    /// <param name="values">The input values.</param>
    /// <param name="nanPolicy">The not-a-number policy.</param>
    /// <param name="infinityPolicy">The infinity policy.</param>
    /// <returns>The policy-aware minimum.</returns>
    public static double MinWithPolicy(double[] values, ChartNaNPolicy nanPolicy, ChartInfinityPolicy infinityPolicy)
    {
        ValidatePolicyInputs(values, nanPolicy, infinityPolicy);
        if (nanPolicy == ChartNaNPolicy.Propagate && ContainsNaN(values))
        {
            return double.NaN;
        }

        double[] prepared = PrepareValues(values, nanPolicy, infinityPolicy);
        double minimum = prepared[0];
        int index = 1;
        while (index < prepared.Length)
        {
            if (prepared[index] < minimum)
            {
                minimum = prepared[index];
            }

            index = index + 1;
        }

        return minimum;
    }

    /// <summary>Returns the maximum using explicit not-a-number and infinity policies.</summary>
    /// <param name="values">The input values.</param>
    /// <param name="nanPolicy">The not-a-number policy.</param>
    /// <param name="infinityPolicy">The infinity policy.</param>
    /// <returns>The policy-aware maximum.</returns>
    public static double MaxWithPolicy(double[] values, ChartNaNPolicy nanPolicy, ChartInfinityPolicy infinityPolicy)
    {
        ValidatePolicyInputs(values, nanPolicy, infinityPolicy);
        if (nanPolicy == ChartNaNPolicy.Propagate && ContainsNaN(values))
        {
            return double.NaN;
        }

        double[] prepared = PrepareValues(values, nanPolicy, infinityPolicy);
        double maximum = prepared[0];
        int index = 1;
        while (index < prepared.Length)
        {
            if (prepared[index] > maximum)
            {
                maximum = prepared[index];
            }

            index = index + 1;
        }

        return maximum;
    }

    /// <summary>Returns the sum using explicit not-a-number and infinity policies.</summary>
    /// <param name="values">The input values.</param>
    /// <param name="nanPolicy">The not-a-number policy.</param>
    /// <param name="infinityPolicy">The infinity policy.</param>
    /// <returns>The policy-aware sum.</returns>
    public static double SumWithPolicy(double[] values, ChartNaNPolicy nanPolicy, ChartInfinityPolicy infinityPolicy)
    {
        ValidatePolicyInputs(values, nanPolicy, infinityPolicy);
        if (nanPolicy == ChartNaNPolicy.Propagate && ContainsNaN(values))
        {
            return double.NaN;
        }

        double[] prepared = PrepareValues(values, nanPolicy, infinityPolicy);
        if (ContainsInfinity(prepared))
        {
            double total = 0.0;
            int index = 0;
            while (index < prepared.Length)
            {
                total = total + prepared[index];
                index = index + 1;
            }

            return total;
        }

        return CompensatedFiniteSum(prepared);
    }

    /// <summary>Returns the arithmetic mean using explicit not-a-number and infinity policies.</summary>
    /// <param name="values">The input values.</param>
    /// <param name="nanPolicy">The not-a-number policy.</param>
    /// <param name="infinityPolicy">The infinity policy.</param>
    /// <returns>The policy-aware mean.</returns>
    public static double MeanWithPolicy(double[] values, ChartNaNPolicy nanPolicy, ChartInfinityPolicy infinityPolicy)
    {
        ValidatePolicyInputs(values, nanPolicy, infinityPolicy);
        if (nanPolicy == ChartNaNPolicy.Propagate && ContainsNaN(values))
        {
            return double.NaN;
        }

        double[] prepared = PrepareValues(values, nanPolicy, infinityPolicy);
        return SumWithPolicy(prepared, ChartNaNPolicy.Reject, infinityPolicy) / prepared.Length;
    }

    internal static double CompensatedFiniteSum(double[] values)
    {
        double sum = 0.0;
        double compensation = 0.0;
        int index = 0;
        while (index < values.Length)
        {
            double value = values[index];
            double next = sum + value;
            if (System.Math.Abs(sum) >= System.Math.Abs(value))
            {
                double correction = (sum - next) + value;
                compensation = compensation + correction;
            }
            else
            {
                double correction = (value - next) + sum;
                compensation = compensation + correction;
            }

            sum = next;
            index = index + 1;
        }

        return sum + compensation;
    }

    internal static double SumSquaredDeviations(double[] values)
    {
        double mean = 0.0;
        double sum = 0.0;
        int count = 0;
        int index = 0;
        while (index < values.Length)
        {
            count = count + 1;
            double delta = values[index] - mean;
            mean = mean + delta / count;
            double adjusted = values[index] - mean;
            sum = sum + delta * adjusted;
            index = index + 1;
        }

        return sum;
    }

    internal static double SumCrossDeviations(double[] xValues, double[] yValues)
    {
        double xMean = 0.0;
        double yMean = 0.0;
        double sum = 0.0;
        int count = 0;
        int index = 0;
        while (index < xValues.Length)
        {
            count = count + 1;
            double xDelta = xValues[index] - xMean;
            xMean = xMean + xDelta / count;
            double yDelta = yValues[index] - yMean;
            yMean = yMean + yDelta / count;
            sum = sum + xDelta * (yValues[index] - yMean);
            index = index + 1;
        }

        return sum;
    }

    internal static double[] PrepareValues(double[] values, ChartNaNPolicy nanPolicy, ChartInfinityPolicy infinityPolicy)
    {
        ValidatePolicyInputs(values, nanPolicy, infinityPolicy);
        double[] buffer = new double[values.Length];
        int count = 0;
        int index = 0;
        while (index < values.Length)
        {
            double value = values[index];
            if (double.IsNaN(value))
            {
                if (nanPolicy == ChartNaNPolicy.Reject)
                {
                    throw new System.ArgumentException("Values cannot contain not-a-number.");
                }
            }
            else if (double.IsInfinity(value))
            {
                if (infinityPolicy == ChartInfinityPolicy.Reject)
                {
                    throw new System.ArgumentException("Values cannot contain infinity.");
                }

                if (infinityPolicy == ChartInfinityPolicy.Allow)
                {
                    buffer[count] = value;
                    count = count + 1;
                }
            }
            else
            {
                buffer[count] = value;
                count = count + 1;
            }

            index = index + 1;
        }

        if (count == 0)
        {
            throw new System.ArgumentException("Policies excluded every input value.");
        }

        double[] result = new double[count];
        System.Array.Copy(buffer, result, count);
        return result;
    }

    internal static bool ContainsNaN(double[] values)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }

        int index = 0;
        while (index < values.Length)
        {
            if (double.IsNaN(values[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool ContainsInfinity(double[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (double.IsInfinity(values[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static void ValidatePolicyInputs(double[] values, ChartNaNPolicy nanPolicy, ChartInfinityPolicy infinityPolicy)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }

        if (values.Length == 0)
        {
            throw new System.ArgumentException("Values cannot be empty.");
        }

        int nanValue = (int)nanPolicy;
        if (nanValue < 0 || nanValue > 2)
        {
            throw new System.ArgumentOutOfRangeException("nanPolicy");
        }

        int infinityValue = (int)infinityPolicy;
        if (infinityValue < 0 || infinityValue > 2)
        {
            throw new System.ArgumentOutOfRangeException("infinityPolicy");
        }
    }

    internal static void ValidateSampleValues(double[] values)
    {
        ValidateFiniteValues(values);
        if (values.Length < 2)
        {
            throw new System.ArgumentException("Sample dispersion requires at least two values.");
        }
    }

    internal static void ValidateFinitePairs(double[] xValues, double[] yValues)
    {
        ValidateFiniteValues(xValues);
        ValidateFiniteValues(yValues);
        if (xValues.Length != yValues.Length)
        {
            throw new System.ArgumentException("Value arrays must have the same length.");
        }
    }

    internal static void ValidateFiniteValues(double[] values)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }

        if (values.Length == 0)
        {
            throw new System.ArgumentException("Values cannot be empty.");
        }

        int index = 0;
        while (index < values.Length)
        {
            if (double.IsNaN(values[index]) || double.IsInfinity(values[index]))
            {
                throw new System.ArgumentException("Values must be finite.");
            }

            index = index + 1;
        }
    }
}
