namespace chart;
/// <summary>Represents a continuous square-root scale.</summary>
public class ChartSquareRootScale : IChartSquareRootScale
{
    internal ChartPowerScale Scale;

    /// <summary>Initializes a square-root scale.</summary>
    /// <param name = "domainMinimum">The domain minimum.</param>
    /// <param name = "domainMaximum">The domain maximum.</param>
    /// <param name = "rangeMinimum">The range minimum.</param>
    /// <param name = "rangeMaximum">The range maximum.</param>
    public ChartSquareRootScale(double domainMinimum, double domainMaximum, double rangeMinimum, double rangeMaximum)
    {
        Scale = new ChartPowerScale(domainMinimum, domainMaximum, rangeMinimum, rangeMaximum, 0.5);
    }

    /// <summary>Maps a domain value to the configured range.</summary>
    /// <param name = "value">The domain value.</param>
    /// <returns>The mapped range value.</returns>
    public double Map(double value)
    {
        return Scale.Map(value);
    }

    /// <summary>Maps a range value back to the configured domain.</summary>
    /// <param name = "value">The range value.</param>
    /// <returns>The mapped domain value.</returns>
    public double Invert(double value)
    {
        return Scale.Invert(value);
    }
}
