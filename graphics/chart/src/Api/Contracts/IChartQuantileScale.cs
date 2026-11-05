namespace chart;
/// <summary>Defines a scale that partitions a sample into quantile buckets.</summary>
public interface IChartQuantileScale
{
    /// <summary>Maps a value to its quantile bucket value.</summary>
    /// <param name = "value">The value to classify.</param>
    /// <returns>The configured bucket value.</returns>
    double Map(double value);
    /// <summary>Copies the computed quantile thresholds.</summary>
    /// <returns>A new array containing the thresholds.</returns>
    double[] CopyThresholds();
}
