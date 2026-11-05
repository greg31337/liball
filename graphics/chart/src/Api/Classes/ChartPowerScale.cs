namespace chart;
/// <summary>Represents a continuous power scale.</summary>
public class ChartPowerScale : IChartPowerScale
{
    internal double DomainMinimum;
    internal double DomainMaximum;
    internal double RangeMinimum;
    internal double RangeMaximum;
    internal double Exponent;

    /// <summary>Initializes a power scale.</summary>
    /// <param name = "domainMinimum">The domain minimum.</param>
    /// <param name = "domainMaximum">The domain maximum.</param>
    /// <param name = "rangeMinimum">The range minimum.</param>
    /// <param name = "rangeMaximum">The range maximum.</param>
    /// <param name = "exponent">The positive power exponent.</param>
    public ChartPowerScale(double domainMinimum, double domainMaximum, double rangeMinimum, double rangeMaximum, double exponent)
    {
        ChartLinearScale.ValidateDomain(domainMinimum, domainMaximum);
        ChartLinearScale.ValidateDomain(rangeMinimum, rangeMaximum);
        ChartLinearScale.ValidateFinite(exponent, "exponent");
        if (exponent <= 0.0)
        {
            throw new System.ArgumentOutOfRangeException("exponent");
        }

        DomainMinimum = domainMinimum;
        DomainMaximum = domainMaximum;
        RangeMinimum = rangeMinimum;
        RangeMaximum = rangeMaximum;
        Exponent = exponent;
    }

    /// <summary>Maps a domain value to the configured range.</summary>
    /// <param name = "value">The domain value.</param>
    /// <returns>The mapped range value.</returns>
    public double Map(double value)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        double transformedMinimum = Transform(DomainMinimum, Exponent);
        double transformedMaximum = Transform(DomainMaximum, Exponent);
        double amount = (Transform(value, Exponent) - transformedMinimum) / (transformedMaximum - transformedMinimum);
        return RangeMinimum + amount * (RangeMaximum - RangeMinimum);
    }

    /// <summary>Maps a range value back to the configured domain.</summary>
    /// <param name = "value">The range value.</param>
    /// <returns>The mapped domain value.</returns>
    public double Invert(double value)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        double amount = (value - RangeMinimum) / (RangeMaximum - RangeMinimum);
        double transformedMinimum = Transform(DomainMinimum, Exponent);
        double transformedMaximum = Transform(DomainMaximum, Exponent);
        return Transform(transformedMinimum + amount * (transformedMaximum - transformedMinimum), 1.0 / Exponent);
    }

    internal static double Transform(double value, double exponent)
    {
        if (value < 0.0)
        {
            return -System.Math.Pow(-value, exponent);
        }

        return System.Math.Pow(value, exponent);
    }
}
