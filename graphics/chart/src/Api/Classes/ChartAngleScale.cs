namespace chart;
/// <summary>Represents a continuous angular scale whose output is measured in radians.</summary>
public class ChartAngleScale : IChartAngleScale
{
    internal ChartLinearScale Scale;

    /// <summary>Initializes an angular scale.</summary>
    /// <param name = "domainMinimum">The domain minimum.</param>
    /// <param name = "domainMaximum">The domain maximum.</param>
    /// <param name = "angleMinimum">The minimum angle in radians.</param>
    /// <param name = "angleMaximum">The maximum angle in radians.</param>
    public ChartAngleScale(double domainMinimum, double domainMaximum, double angleMinimum, double angleMaximum)
    {
        Scale = new ChartLinearScale(domainMinimum, domainMaximum, angleMinimum, angleMaximum);
    }

    /// <summary>Maps a domain value to an angle in radians.</summary>
    /// <param name = "value">The domain value.</param>
    /// <returns>The mapped angle.</returns>
    public double Map(double value)
    {
        return Scale.Map(value);
    }

    /// <summary>Maps an angle in radians back to the configured domain.</summary>
    /// <param name = "value">The angle in radians.</param>
    /// <returns>The mapped domain value.</returns>
    public double Invert(double value)
    {
        return Scale.Invert(value);
    }
}
