namespace chart;
/// <summary>Defines a continuous scale for coordinated universal time values.</summary>
public interface IChartUtcTimeScale
{
    /// <summary>Maps a UTC date and time to the configured range.</summary>
    /// <param name = "value">The UTC date and time.</param>
    /// <returns>The mapped range value.</returns>
    double Map(System.DateTime value);
    /// <summary>Maps a range value back to a UTC date and time.</summary>
    /// <param name = "value">The range value.</param>
    /// <returns>The mapped UTC date and time.</returns>
    System.DateTime Invert(double value);
}
