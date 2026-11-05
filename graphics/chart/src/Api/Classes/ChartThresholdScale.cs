namespace chart;
/// <summary>Represents a scale that classifies values using explicit thresholds.</summary>
public class ChartThresholdScale : IChartThresholdScale
{
    internal double[] Thresholds;
    internal double[] RangeValues;

    /// <summary>Initializes an explicit threshold scale.</summary>
    /// <param name = "thresholds">The strictly increasing thresholds.</param>
    /// <param name = "rangeValues">The bucket values, one more than the threshold count.</param>
    public ChartThresholdScale(double[] thresholds, double[] rangeValues)
    {
        ChartStatisticsOperations.ValidateFiniteValues(thresholds);
        ChartStatisticsOperations.ValidateFiniteValues(rangeValues);
        if (rangeValues.Length != thresholds.Length + 1)
        {
            throw new System.ArgumentException("Threshold scales require one more range value than threshold values.");
        }

        int index = 1;
        while (index < thresholds.Length)
        {
            if (thresholds[index] <= thresholds[index - 1])
            {
                throw new System.ArgumentException("Thresholds must be strictly increasing.");
            }

            index = index + 1;
        }

        Thresholds = ChartDataMatrix.CopyDoubleArray(thresholds);
        RangeValues = ChartDataMatrix.CopyDoubleArray(rangeValues);
    }

    /// <summary>Maps a value to its threshold bucket value.</summary>
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
}
