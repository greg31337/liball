namespace chart;
/// <summary>Represents a continuous scale with explicit domain and range midpoints.</summary>
public class ChartDivergingScale : IChartDivergingScale
{
    internal double DomainMinimum;
    internal double DomainMidpoint;
    internal double DomainMaximum;
    internal double RangeMinimum;
    internal double RangeMidpoint;
    internal double RangeMaximum;

    /// <summary>Initializes a diverging scale.</summary>
    /// <param name = "domainMinimum">The domain minimum.</param>
    /// <param name = "domainMidpoint">The domain midpoint.</param>
    /// <param name = "domainMaximum">The domain maximum.</param>
    /// <param name = "rangeMinimum">The range minimum.</param>
    /// <param name = "rangeMidpoint">The range midpoint.</param>
    /// <param name = "rangeMaximum">The range maximum.</param>
    public ChartDivergingScale(double domainMinimum, double domainMidpoint, double domainMaximum, double rangeMinimum, double rangeMidpoint, double rangeMaximum)
    {
        ValidateOrder(domainMinimum, domainMidpoint, domainMaximum, "domain");
        ValidateMonotonic(rangeMinimum, rangeMidpoint, rangeMaximum, "range");
        DomainMinimum = domainMinimum;
        DomainMidpoint = domainMidpoint;
        DomainMaximum = domainMaximum;
        RangeMinimum = rangeMinimum;
        RangeMidpoint = rangeMidpoint;
        RangeMaximum = rangeMaximum;
    }

    /// <summary>Maps a domain value to the configured range.</summary>
    /// <param name = "value">The domain value.</param>
    /// <returns>The mapped range value.</returns>
    public double Map(double value)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        if (value <= DomainMidpoint)
        {
            return RangeMinimum + (value - DomainMinimum) / (DomainMidpoint - DomainMinimum) * (RangeMidpoint - RangeMinimum);
        }

        return RangeMidpoint + (value - DomainMidpoint) / (DomainMaximum - DomainMidpoint) * (RangeMaximum - RangeMidpoint);
    }

    /// <summary>Maps a range value back to the configured domain.</summary>
    /// <param name = "value">The range value.</param>
    /// <returns>The mapped domain value.</returns>
    public double Invert(double value)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        if (RangeMinimum < RangeMidpoint && value <= RangeMidpoint)
        {
            return DomainMinimum + (value - RangeMinimum) / (RangeMidpoint - RangeMinimum) * (DomainMidpoint - DomainMinimum);
        }

        if (RangeMinimum > RangeMidpoint && value >= RangeMidpoint)
        {
            return DomainMinimum + (value - RangeMinimum) / (RangeMidpoint - RangeMinimum) * (DomainMidpoint - DomainMinimum);
        }

        return DomainMidpoint + (value - RangeMidpoint) / (RangeMaximum - RangeMidpoint) * (DomainMaximum - DomainMidpoint);
    }

    internal static void ValidateOrder(double minimum, double midpoint, double maximum, string name)
    {
        ChartLinearScale.ValidateFinite(minimum, name);
        ChartLinearScale.ValidateFinite(midpoint, name);
        ChartLinearScale.ValidateFinite(maximum, name);
        if (minimum >= midpoint || midpoint >= maximum)
        {
            throw new System.ArgumentException("Diverging scale values must be strictly increasing.", name);
        }
    }

    internal static void ValidateMonotonic(double first, double midpoint, double last, string name)
    {
        ChartLinearScale.ValidateFinite(first, name);
        ChartLinearScale.ValidateFinite(midpoint, name);
        ChartLinearScale.ValidateFinite(last, name);
        bool increasing = first < midpoint && midpoint < last;
        bool decreasing = first > midpoint && midpoint > last;
        if (!increasing && !decreasing)
        {
            throw new System.ArgumentException("Diverging scale range values must be strictly monotonic.", name);
        }
    }
}
