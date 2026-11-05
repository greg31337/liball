namespace chart;
/// <summary>Defines an area-preserving radial scale.</summary>
public interface IChartRadialScale
{
    /// <summary>Maps a domain value to a radius.</summary>
    /// <param name = "value">The domain value.</param>
    /// <returns>The mapped radius.</returns>
    double Map(double value);
    /// <summary>Maps a radius back to the configured domain.</summary>
    /// <param name = "value">The radius.</param>
    /// <returns>The mapped domain value.</returns>
    double Invert(double value);
}
