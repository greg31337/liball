namespace chart;
/// <summary>Defines a continuous scale whose output is constrained to valid opacity values.</summary>
public interface IChartOpacityScale
{
    /// <summary>Maps a domain value to opacity.</summary>
    /// <param name = "value">The domain value.</param>
    /// <returns>The mapped opacity.</returns>
    double Map(double value);
    /// <summary>Maps opacity back to the configured domain.</summary>
    /// <param name = "value">The opacity.</param>
    /// <returns>The mapped domain value.</returns>
    double Invert(double value);
}
