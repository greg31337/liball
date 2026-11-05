namespace chart;
/// <summary>Represents a scale that partitions a sample into quantile buckets.</summary>
public class ChartQuantileScale : IChartQuantileScale
{
    internal double[] Thresholds;
    internal double[] RangeValues;

    /// <summary>Initializes a quantile scale from a sample and bucket values.</summary>
    /// <param name = "domainValues">The finite sample used to compute thresholds.</param>
    /// <param name = "rangeValues">The values returned for successive quantile buckets.</param>
    public ChartQuantileScale(double[] domainValues, double[] rangeValues)
    {
        ChartStatisticsOperations.ValidateFiniteValues(domainValues);
        ChartStatisticsOperations.ValidateFiniteValues(rangeValues);
        RangeValues = ChartDataMatrix.CopyDoubleArray(rangeValues);
        Thresholds = new double[rangeValues.Length - 1];
        int index = 0;
        while (index < Thresholds.Length)
        {
            Thresholds[index] = ChartStatisticsOperations.Quantile(domainValues, (index + 1.0) / rangeValues.Length);
            index = index + 1;
        }
    }

    /// <summary>Maps a value to its quantile bucket value.</summary>
    /// <param name = "value">The value to classify.</param>
    /// <returns>The configured bucket value.</returns>
    public double Map(double value)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        int index = 0;
        while (index < Thresholds.Length && value >= Thresholds[index])
        {
            index = index + 1;
        }

        return RangeValues[index];
    }

    /// <summary>Copies the computed quantile thresholds.</summary>
    /// <returns>A new array containing the thresholds.</returns>
    public double[] CopyThresholds()
    {
        return ChartDataMatrix.CopyDoubleArray(Thresholds);
    }
}
