namespace chart;
/// <summary>
/// Represents a time scale used by the chart model or rendering pipeline.
/// </summary>
public class ChartTimeScale : IChartTimeScale
{
    internal System.DateTime DomainStart;
    internal System.DateTime DomainEnd;
    internal double RangeMinimum;
    internal double RangeMaximum;
    /// <summary>
    /// Initializes a time scale from the supplied domain start, domain end, range minimum, and range maximum.
    /// </summary>
    /// <param name = "domainStart">The domain start.</param>
    /// <param name = "domainEnd">The domain end.</param>
    /// <param name = "rangeMinimum">The range minimum.</param>
    /// <param name = "rangeMaximum">The range maximum.</param>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    public ChartTimeScale(System.DateTime domainStart, System.DateTime domainEnd, double rangeMinimum, double rangeMaximum)
    {
        if (domainStart.Ticks == domainEnd.Ticks)
        {
            throw new System.ArgumentException("Time scale domain cannot be zero length.");
        }

        ChartLinearScale.ValidateFinite(rangeMinimum, "rangeMinimum");
        ChartLinearScale.ValidateFinite(rangeMaximum, "rangeMaximum");
        DomainStart = domainStart;
        DomainEnd = domainEnd;
        RangeMinimum = rangeMinimum;
        RangeMaximum = rangeMaximum;
    }

    /// <summary>
    /// Maps the configured value through the configured input and output domains.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting double.</returns>
    public double Map(System.DateTime value)
    {
        double amount = (value.Ticks - DomainStart.Ticks) / (double)(DomainEnd.Ticks - DomainStart.Ticks);
        return RangeMinimum + amount * (RangeMaximum - RangeMinimum);
    }

    /// <summary>
    /// Applies the invert operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting date time.</returns>
    public System.DateTime Invert(double value)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        double amount = (value - RangeMinimum) / (RangeMaximum - RangeMinimum);
        long ticks = DomainStart.Ticks + (long)System.Math.Round(amount * (DomainEnd.Ticks - DomainStart.Ticks));
        return new System.DateTime(ticks);
    }

    /// <summary>Returns the current domain start.</summary>
    /// <returns>The current field value.</returns>
    public System.DateTime GetDomainStart()
    {
        return DomainStart;
    }

    /// <summary>Replaces the current domain start.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetDomainStart(System.DateTime value)
    {
        DomainStart = value;
    }

    /// <summary>Returns the current domain end.</summary>
    /// <returns>The current field value.</returns>
    public System.DateTime GetDomainEnd()
    {
        return DomainEnd;
    }

    /// <summary>Replaces the current domain end.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetDomainEnd(System.DateTime value)
    {
        DomainEnd = value;
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
}
