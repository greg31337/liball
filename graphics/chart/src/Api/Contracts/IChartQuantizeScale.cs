namespace chart;
/// <summary>Defines a scale that partitions a continuous domain into equal-width buckets.</summary>
public interface IChartQuantizeScale
{
    /// <summary>Maps a value to its equal-width bucket value.</summary>
    /// <param name = "value">The value to classify.</param>
    /// <returns>The configured bucket value.</returns>
    double Map(double value);
    /// <summary>Returns the domain extent for a bucket.</summary>
    /// <param name = "index">The zero-based bucket index.</param>
    /// <returns>The bucket domain extent.</returns>
    ChartRange InvertExtent(int index);
}
