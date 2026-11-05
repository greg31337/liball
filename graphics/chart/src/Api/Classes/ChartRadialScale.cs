namespace chart;
/// <summary>Represents an area-preserving radial scale.</summary>
public class ChartRadialScale : IChartRadialScale
{
    internal double DomainMinimum;
    internal double DomainMaximum;
    internal double RadiusMinimum;
    internal double RadiusMaximum;

    /// <summary>Initializes an area-preserving radial scale.</summary>
    /// <param name = "domainMinimum">The domain minimum.</param>
    /// <param name = "domainMaximum">The domain maximum.</param>
    /// <param name = "radiusMinimum">The minimum radius.</param>
    /// <param name = "radiusMaximum">The maximum radius.</param>
    public ChartRadialScale(double domainMinimum, double domainMaximum, double radiusMinimum, double radiusMaximum)
    {
        ChartLinearScale.ValidateDomain(domainMinimum, domainMaximum);
        ChartLinearScale.ValidateDomain(radiusMinimum, radiusMaximum);
        if (radiusMinimum < 0.0 || radiusMaximum < 0.0)
        {
            throw new System.ArgumentOutOfRangeException("radiusMinimum");
        }

        DomainMinimum = domainMinimum;
        DomainMaximum = domainMaximum;
        RadiusMinimum = radiusMinimum;
        RadiusMaximum = radiusMaximum;
    }

    /// <summary>Maps a domain value to a radius.</summary>
    /// <param name = "value">The domain value.</param>
    /// <returns>The mapped radius.</returns>
    public double Map(double value)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        double amount = (value - DomainMinimum) / (DomainMaximum - DomainMinimum);
        double minimumArea = RadiusMinimum * RadiusMinimum;
        double maximumArea = RadiusMaximum * RadiusMaximum;
        return System.Math.Sqrt(minimumArea + amount * (maximumArea - minimumArea));
    }

    /// <summary>Maps a radius back to the configured domain.</summary>
    /// <param name = "value">The radius.</param>
    /// <returns>The mapped domain value.</returns>
    public double Invert(double value)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        if (value < 0.0)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        double minimumArea = RadiusMinimum * RadiusMinimum;
        double maximumArea = RadiusMaximum * RadiusMaximum;
        double amount = (value * value - minimumArea) / (maximumArea - minimumArea);
        return DomainMinimum + amount * (DomainMaximum - DomainMinimum);
    }
}
