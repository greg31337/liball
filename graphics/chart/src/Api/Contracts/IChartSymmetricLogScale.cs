namespace chart;
/// <summary>Defines a continuous scale that is logarithmic away from zero and linear near zero.</summary>
public interface IChartSymmetricLogScale
{
    /// <summary>Maps a domain value to the configured range.</summary>
    /// <param name = "value">The domain value.</param>
    /// <returns>The mapped range value.</returns>
    double Map(double value);
    /// <summary>Maps a range value back to the configured domain.</summary>
    /// <param name = "value">The range value.</param>
    /// <returns>The mapped domain value.</returns>
    double Invert(double value);
}
