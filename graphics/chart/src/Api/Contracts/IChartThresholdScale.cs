namespace chart;
/// <summary>Defines a scale that classifies values using explicit thresholds.</summary>
public interface IChartThresholdScale
{
    /// <summary>Maps a value to its threshold bucket value.</summary>
    /// <param name = "value">The value to classify.</param>
    /// <returns>The configured bucket value.</returns>
    double Map(double value);
}
