namespace chart;
/// <summary>Represents a continuous scale whose output is constrained to valid opacity values.</summary>
public class ChartOpacityScale : IChartOpacityScale
{
    internal double DomainMinimum;
    internal double DomainMaximum;
    internal double OpacityMinimum;
    internal double OpacityMaximum;

    /// <summary>Initializes an opacity scale.</summary>
    /// <param name = "domainMinimum">The domain minimum.</param>
    /// <param name = "domainMaximum">The domain maximum.</param>
    /// <param name = "opacityMinimum">The minimum opacity from zero through one.</param>
    /// <param name = "opacityMaximum">The maximum opacity from zero through one.</param>
    public ChartOpacityScale(double domainMinimum, double domainMaximum, double opacityMinimum, double opacityMaximum)
    {
        ChartLinearScale.ValidateDomain(domainMinimum, domainMaximum);
        ChartLinearScale.ValidateDomain(opacityMinimum, opacityMaximum);
        ValidateOpacity(opacityMinimum, "opacityMinimum");
        ValidateOpacity(opacityMaximum, "opacityMaximum");
        DomainMinimum = domainMinimum;
        DomainMaximum = domainMaximum;
        OpacityMinimum = opacityMinimum;
        OpacityMaximum = opacityMaximum;
    }

    /// <summary>Maps a domain value to opacity.</summary>
    /// <param name = "value">The domain value.</param>
    /// <returns>The mapped opacity.</returns>
    public double Map(double value)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        double amount = (value - DomainMinimum) / (DomainMaximum - DomainMinimum);
        amount = ChartScaleUtilities.ClampUnit(amount);
        return OpacityMinimum + amount * (OpacityMaximum - OpacityMinimum);
    }

    /// <summary>Maps opacity back to the configured domain.</summary>
    /// <param name = "value">The opacity.</param>
    /// <returns>The mapped domain value.</returns>
    public double Invert(double value)
    {
        ValidateOpacity(value, "value");
        double amount = (value - OpacityMinimum) / (OpacityMaximum - OpacityMinimum);
        amount = ChartScaleUtilities.ClampUnit(amount);
        return DomainMinimum + amount * (DomainMaximum - DomainMinimum);
    }

    internal static void ValidateOpacity(double value, string name)
    {
        ChartLinearScale.ValidateFinite(value, name);
        if (value < 0.0 || value > 1.0)
        {
            throw new System.ArgumentOutOfRangeException(name);
        }
    }
}
