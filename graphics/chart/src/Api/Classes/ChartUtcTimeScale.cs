namespace chart;
/// <summary>Represents a continuous scale for coordinated universal time values.</summary>
public class ChartUtcTimeScale : IChartUtcTimeScale
{
    internal System.DateTime DomainStart;
    internal System.DateTime DomainEnd;
    internal double RangeMinimum;
    internal double RangeMaximum;

    /// <summary>Initializes a UTC-time scale.</summary>
    /// <param name = "domainStart">The UTC domain start.</param>
    /// <param name = "domainEnd">The UTC domain end.</param>
    /// <param name = "rangeMinimum">The range minimum.</param>
    /// <param name = "rangeMaximum">The range maximum.</param>
    public ChartUtcTimeScale(System.DateTime domainStart, System.DateTime domainEnd, double rangeMinimum, double rangeMaximum)
    {
        ValidateUtc(domainStart, "domainStart");
        ValidateUtc(domainEnd, "domainEnd");
        if (domainStart.Ticks == domainEnd.Ticks)
        {
            throw new System.ArgumentException("UTC-time scale domain cannot be zero length.");
        }

        ChartLinearScale.ValidateDomain(rangeMinimum, rangeMaximum);
        DomainStart = domainStart;
        DomainEnd = domainEnd;
        RangeMinimum = rangeMinimum;
        RangeMaximum = rangeMaximum;
    }

    /// <summary>Maps a UTC date and time to the configured range.</summary>
    /// <param name = "value">The UTC date and time.</param>
    /// <returns>The mapped range value.</returns>
    public double Map(System.DateTime value)
    {
        ValidateUtc(value, "value");
        double amount = (value.Ticks - DomainStart.Ticks) / (double)(DomainEnd.Ticks - DomainStart.Ticks);
        return RangeMinimum + amount * (RangeMaximum - RangeMinimum);
    }

    /// <summary>Maps a range value back to a UTC date and time.</summary>
    /// <param name = "value">The range value.</param>
    /// <returns>The mapped UTC date and time.</returns>
    public System.DateTime Invert(double value)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        double amount = (value - RangeMinimum) / (RangeMaximum - RangeMinimum);
        long ticks = DomainStart.Ticks + (long)System.Math.Round(amount * (DomainEnd.Ticks - DomainStart.Ticks));
        return new System.DateTime(ticks, System.DateTimeKind.Utc);
    }

    internal static void ValidateUtc(System.DateTime value, string name)
    {
        if (value.Kind != System.DateTimeKind.Utc)
        {
            throw new System.ArgumentException("UTC-time scale values must have UTC kind.", name);
        }
    }
}
