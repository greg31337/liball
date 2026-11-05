namespace chart;
/// <summary>
/// Represents a color scale used by the chart model or rendering pipeline.
/// </summary>
public class ChartColorScale : IChartColorScale
{
    internal double DomainMinimum;
    internal double DomainMaximum;
    internal ChartColor StartColor;
    internal ChartColor EndColor;
    internal bool Clamp;
    /// <summary>
    /// Initializes a color scale from the supplied domain minimum, domain maximum, start color, and end color.
    /// </summary>
    /// <param name = "domainMinimum">The domain minimum.</param>
    /// <param name = "domainMaximum">The domain maximum.</param>
    /// <param name = "startColor">The start color.</param>
    /// <param name = "endColor">The end color.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartColorScale(double domainMinimum, double domainMaximum, ChartColor startColor, ChartColor endColor)
    {
        ChartLinearScale.ValidateDomain(domainMinimum, domainMaximum);
        if (startColor == null)
        {
            throw new System.ArgumentNullException("startColor");
        }

        if (endColor == null)
        {
            throw new System.ArgumentNullException("endColor");
        }

        DomainMinimum = domainMinimum;
        DomainMaximum = domainMaximum;
        StartColor = startColor.Copy();
        EndColor = endColor.Copy();
        Clamp = false;
    }

    /// <summary>
    /// Maps the configured value through the configured input and output domains.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting color.</returns>
    public ChartColor Map(double value)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        double amount = (value - DomainMinimum) / (DomainMaximum - DomainMinimum);
        if (Clamp)
        {
            amount = ChartScaleUtilities.ClampUnit(amount);
        }

        return ChartColor.Lerp(StartColor, EndColor, amount);
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

    /// <summary>Returns the current start color.</summary>
    /// <returns>The current field value.</returns>
    public ChartColor GetStartColor()
    {
        return StartColor;
    }

    /// <summary>Replaces the current start color.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetStartColor(ChartColor value)
    {
        StartColor = value;
    }

    /// <summary>Returns the current end color.</summary>
    /// <returns>The current field value.</returns>
    public ChartColor GetEndColor()
    {
        return EndColor;
    }

    /// <summary>Replaces the current end color.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetEndColor(ChartColor value)
    {
        EndColor = value;
    }

    /// <summary>Returns the current clamp.</summary>
    /// <returns>The current field value.</returns>
    public bool GetClamp()
    {
        return Clamp;
    }
}
