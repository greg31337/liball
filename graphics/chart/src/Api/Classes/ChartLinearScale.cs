namespace chart;
/// <summary>
/// Represents a linear scale used by the chart model or rendering pipeline.
/// </summary>
public class ChartLinearScale : IChartLinearScale
{
    internal double DomainMinimum;
    internal double DomainMaximum;
    internal double RangeMinimum;
    internal double RangeMaximum;
    internal bool Clamp;
    /// <summary>
    /// Initializes a linear scale from the supplied domain minimum, domain maximum, range minimum, and range maximum.
    /// </summary>
    /// <param name = "domainMinimum">The domain minimum.</param>
    /// <param name = "domainMaximum">The domain maximum.</param>
    /// <param name = "rangeMinimum">The range minimum.</param>
    /// <param name = "rangeMaximum">The range maximum.</param>
    public ChartLinearScale(double domainMinimum, double domainMaximum, double rangeMinimum, double rangeMaximum)
    {
        ValidateDomain(domainMinimum, domainMaximum);
        ValidateFinite(rangeMinimum, "rangeMinimum");
        ValidateFinite(rangeMaximum, "rangeMaximum");
        DomainMinimum = domainMinimum;
        DomainMaximum = domainMaximum;
        RangeMinimum = rangeMinimum;
        RangeMaximum = rangeMaximum;
        Clamp = false;
    }

    /// <summary>
    /// Maps the configured value through the configured input and output domains.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting double.</returns>
    public double Map(double value)
    {
        ValidateFinite(value, "value");
        double amount = (value - DomainMinimum) / (DomainMaximum - DomainMinimum);
        if (Clamp)
        {
            amount = ChartScaleUtilities.ClampUnit(amount);
        }

        return RangeMinimum + amount * (RangeMaximum - RangeMinimum);
    }

    /// <summary>
    /// Applies the invert operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting double.</returns>
    public double Invert(double value)
    {
        ValidateFinite(value, "value");
        double amount = (value - RangeMinimum) / (RangeMaximum - RangeMinimum);
        if (Clamp)
        {
            amount = ChartScaleUtilities.ClampUnit(amount);
        }

        return DomainMinimum + amount * (DomainMaximum - DomainMinimum);
    }

    /// <summary>
    /// Sets the domain configuration.
    /// </summary>
    /// <param name = "minimum">The inclusive lower end of the numeric domain.</param>
    /// <param name = "maximum">The inclusive upper end of the numeric domain.</param>
    public void SetDomain(double minimum, double maximum)
    {
        ValidateDomain(minimum, maximum);
        DomainMinimum = minimum;
        DomainMaximum = maximum;
    }

    /// <summary>
    /// Sets the range configuration.
    /// </summary>
    /// <param name = "minimum">The inclusive lower end of the numeric domain.</param>
    /// <param name = "maximum">The inclusive upper end of the numeric domain.</param>
    public void SetRange(double minimum, double maximum)
    {
        ValidateFinite(minimum, "minimum");
        ValidateFinite(maximum, "maximum");
        RangeMinimum = minimum;
        RangeMaximum = maximum;
    }

    /// <summary>
    /// Sets the clamp configuration.
    /// </summary>
    /// <param name = "clamp"><c>true</c> to constrain mapped values to the output range.</param>
    public void SetClamp(bool clamp)
    {
        Clamp = clamp;
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartLinearScale Copy()
    {
        ChartLinearScale scale = new ChartLinearScale(DomainMinimum, DomainMaximum, RangeMinimum, RangeMaximum);
        scale.Clamp = Clamp;
        return scale;
    }

    /// <summary>
    /// Creates a copy with the specified domain.
    /// </summary>
    /// <param name = "minimum">The inclusive lower end of the numeric domain.</param>
    /// <param name = "maximum">The inclusive upper end of the numeric domain.</param>
    /// <returns>The resulting linear scale.</returns>
    public ChartLinearScale WithDomain(double minimum, double maximum)
    {
        ChartLinearScale scale = Copy();
        scale.SetDomain(minimum, maximum);
        return scale;
    }

    /// <summary>
    /// Creates a copy with the specified range.
    /// </summary>
    /// <param name = "minimum">The inclusive lower end of the numeric domain.</param>
    /// <param name = "maximum">The inclusive upper end of the numeric domain.</param>
    /// <returns>The resulting linear scale.</returns>
    public ChartLinearScale WithRange(double minimum, double maximum)
    {
        ChartLinearScale scale = Copy();
        scale.SetRange(minimum, maximum);
        return scale;
    }

    /// <summary>
    /// Creates a copy with the specified clamp.
    /// </summary>
    /// <param name = "clamp"><c>true</c> to constrain mapped values to the output range.</param>
    /// <returns>The resulting linear scale.</returns>
    public ChartLinearScale WithClamp(bool clamp)
    {
        ChartLinearScale scale = Copy();
        scale.Clamp = clamp;
        return scale;
    }

    /// <summary>
    /// Creates a reversed copy of the range.
    /// </summary>
    /// <returns>The resulting linear scale.</returns>
    public ChartLinearScale ReverseRange()
    {
        ChartLinearScale scale = Copy();
        scale.SetRange(RangeMaximum, RangeMinimum);
        return scale;
    }

    /// <summary>
    /// Expands this value to include zero using the supplied data and configuration.
    /// </summary>
    /// <returns>The resulting linear scale.</returns>
    public ChartLinearScale IncludeZero()
    {
        ChartRange range = ChartScaleUtilities.IncludeZero(DomainMinimum, DomainMaximum);
        return WithDomain(range.Minimum, range.Maximum);
    }

    /// <summary>
    /// Applies the pad domain operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "fraction">The fraction.</param>
    /// <returns>The resulting linear scale.</returns>
    public ChartLinearScale PadDomain(double fraction)
    {
        ChartRange range = ChartScaleUtilities.PadRange(DomainMinimum, DomainMaximum, fraction);
        return WithDomain(range.Minimum, range.Maximum);
    }

    /// <summary>
    /// Applies the nice domain operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "tickCount">The requested number of tick entries.</param>
    /// <returns>The resulting linear scale.</returns>
    public ChartLinearScale NiceDomain(int tickCount)
    {
        ChartRange range = ChartScaleUtilities.NiceDomain(DomainMinimum, DomainMaximum, tickCount);
        return WithDomain(range.Minimum, range.Maximum);
    }

    internal static void ValidateDomain(double minimum, double maximum)
    {
        ValidateFinite(minimum, "minimum");
        ValidateFinite(maximum, "maximum");
        if (minimum == maximum)
        {
            throw new System.ArgumentException("Scale domain cannot be zero length.");
        }
    }

    internal static void ValidateFinite(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new System.ArgumentOutOfRangeException(name);
        }
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

    /// <summary>Returns the current range minimum.</summary>
    /// <returns>The current field value.</returns>
    public double GetRangeMinimum()
    {
        return RangeMinimum;
    }

    /// <summary>Replaces the current range minimum.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetRangeMinimum(double value)
    {
        RangeMinimum = value;
    }

    /// <summary>Returns the current range maximum.</summary>
    /// <returns>The current field value.</returns>
    public double GetRangeMaximum()
    {
        return RangeMaximum;
    }

    /// <summary>Replaces the current range maximum.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetRangeMaximum(double value)
    {
        RangeMaximum = value;
    }

    /// <summary>Returns the current clamp.</summary>
    /// <returns>The current field value.</returns>
    public bool GetClamp()
    {
        return Clamp;
    }
}
