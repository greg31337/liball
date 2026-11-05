namespace chart;
/// <summary>
/// Represents a size scale used by the chart model or rendering pipeline.
/// </summary>
public class ChartSizeScale : IChartSizeScale
{
    internal double DomainMinimum;
    internal double DomainMaximum;
    internal double SizeMinimum;
    internal double SizeMaximum;
    internal bool Clamp;
    /// <summary>
    /// Initializes a size scale from the supplied domain minimum, domain maximum, size minimum, and size maximum.
    /// </summary>
    /// <param name = "domainMinimum">The domain minimum.</param>
    /// <param name = "domainMaximum">The domain maximum.</param>
    /// <param name = "sizeMinimum">The size minimum.</param>
    /// <param name = "sizeMaximum">The size maximum.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartSizeScale(double domainMinimum, double domainMaximum, double sizeMinimum, double sizeMaximum)
    {
        ChartLinearScale.ValidateDomain(domainMinimum, domainMaximum);
        ChartLinearScale.ValidateFinite(sizeMinimum, "sizeMinimum");
        ChartLinearScale.ValidateFinite(sizeMaximum, "sizeMaximum");
        if (sizeMinimum < 0.0 || sizeMaximum < 0.0)
        {
            throw new System.ArgumentOutOfRangeException("sizeMinimum");
        }

        DomainMinimum = domainMinimum;
        DomainMaximum = domainMaximum;
        SizeMinimum = sizeMinimum;
        SizeMaximum = sizeMaximum;
        Clamp = false;
    }

    /// <summary>
    /// Maps the configured value through the configured input and output domains.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting double.</returns>
    public double Map(double value)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        double amount = (value - DomainMinimum) / (DomainMaximum - DomainMinimum);
        if (Clamp)
        {
            amount = ChartScaleUtilities.ClampUnit(amount);
        }

        return SizeMinimum + amount * (SizeMaximum - SizeMinimum);
    }

    /// <summary>
    /// Sets the clamp configuration.
    /// </summary>
    /// <param name = "clamp"><c>true</c> to constrain mapped values to the output range.</param>
    public void SetClamp(bool clamp)
    {
        Clamp = clamp;
    }

    /// <summary>Returns the current domain minimum.</summary>
    /// <returns>The current field value.</returns>
    public double GetDomainMinimum()
    {
        return DomainMinimum;
    }

    /// <summary>Replaces the current domain minimum.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetDomainMinimum(double value)
    {
        DomainMinimum = value;
    }

    /// <summary>Returns the current domain maximum.</summary>
    /// <returns>The current field value.</returns>
    public double GetDomainMaximum()
    {
        return DomainMaximum;
    }

    /// <summary>Replaces the current domain maximum.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetDomainMaximum(double value)
    {
        DomainMaximum = value;
    }

    /// <summary>Returns the current size minimum.</summary>
    /// <returns>The current field value.</returns>
    public double GetSizeMinimum()
    {
        return SizeMinimum;
    }

    /// <summary>Replaces the current size minimum.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetSizeMinimum(double value)
    {
        SizeMinimum = value;
    }

    /// <summary>Returns the current size maximum.</summary>
    /// <returns>The current field value.</returns>
    public double GetSizeMaximum()
    {
        return SizeMaximum;
    }

    /// <summary>Replaces the current size maximum.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetSizeMaximum(double value)
    {
        SizeMaximum = value;
    }

    /// <summary>Returns the current clamp.</summary>
    /// <returns>The current field value.</returns>
    public bool GetClamp()
    {
        return Clamp;
    }
}
