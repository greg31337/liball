namespace chart;
/// <summary>Defines a continuous angular scale.</summary>
public interface IChartAngleScale
{
    /// <summary>Maps a domain value to an angle in radians.</summary>
    /// <param name = "value">The domain value.</param>
    /// <returns>The mapped angle.</returns>
    double Map(double value);
    /// <summary>Maps an angle in radians back to the configured domain.</summary>
    /// <param name = "value">The angle in radians.</param>
    /// <returns>The mapped domain value.</returns>
    double Invert(double value);
}
