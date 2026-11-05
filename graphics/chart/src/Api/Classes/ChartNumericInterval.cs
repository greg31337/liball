namespace chart;
/// <summary>Represents a finite numeric interval with independently inclusive endpoints.</summary>
public class ChartNumericInterval : IChartNumericInterval
{
    internal double Minimum;
    internal double Maximum;
    internal bool MinimumInclusive;
    internal bool MaximumInclusive;

    /// <summary>Initializes a numeric interval from its endpoints and inclusion rules.</summary>
    /// <param name = "minimum">The finite lower endpoint.</param>
    /// <param name = "maximum">The finite upper endpoint.</param>
    /// <param name = "minimumInclusive"><c>true</c> to include the lower endpoint.</param>
    /// <param name = "maximumInclusive"><c>true</c> to include the upper endpoint.</param>
    public ChartNumericInterval(double minimum, double maximum, bool minimumInclusive, bool maximumInclusive)
    {
        ValidateBounds(minimum, maximum);
        Minimum = minimum;
        Maximum = maximum;
        MinimumInclusive = minimumInclusive;
        MaximumInclusive = maximumInclusive;
    }

    /// <summary>Returns the distance between the endpoints.</summary>
    /// <returns>The non-negative interval length.</returns>
    public double Length()
    {
        return Maximum - Minimum;
    }

    /// <summary>Determines whether the interval contains a value.</summary>
    /// <param name = "value">The finite value to test.</param>
    /// <returns><c>true</c> when the value is contained; otherwise, <c>false</c>.</returns>
    public bool Contains(double value)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        bool aboveMinimum = value > Minimum || value == Minimum && MinimumInclusive;
        bool belowMaximum = value < Maximum || value == Maximum && MaximumInclusive;
        return aboveMinimum && belowMaximum;
    }

    /// <summary>Determines whether this interval intersects another interval.</summary>
    /// <param name = "interval">The interval to compare.</param>
    /// <returns><c>true</c> when the intervals share at least one contained value; otherwise, <c>false</c>.</returns>
    public bool Intersects(ChartNumericInterval interval)
    {
        if (interval == null)
        {
            throw new System.ArgumentNullException("interval");
        }

        if (IsEmpty() || interval.IsEmpty())
        {
            return false;
        }

        if (Maximum < interval.Minimum || interval.Maximum < Minimum)
        {
            return false;
        }

        if (Maximum == interval.Minimum)
        {
            return MaximumInclusive && interval.MinimumInclusive;
        }

        if (interval.Maximum == Minimum)
        {
            return interval.MaximumInclusive && MinimumInclusive;
        }

        return true;
    }

    /// <summary>Determines whether the interval contains no values.</summary>
    /// <returns><c>true</c> when the interval is empty; otherwise, <c>false</c>.</returns>
    public bool IsEmpty()
    {
        return Minimum == Maximum && (!MinimumInclusive || !MaximumInclusive);
    }

    /// <summary>Replaces both endpoints and their inclusion rules.</summary>
    /// <param name = "minimum">The finite lower endpoint.</param>
    /// <param name = "maximum">The finite upper endpoint.</param>
    /// <param name = "minimumInclusive"><c>true</c> to include the lower endpoint.</param>
    /// <param name = "maximumInclusive"><c>true</c> to include the upper endpoint.</param>
    public void SetBounds(double minimum, double maximum, bool minimumInclusive, bool maximumInclusive)
    {
        ValidateBounds(minimum, maximum);
        Minimum = minimum;
        Maximum = maximum;
        MinimumInclusive = minimumInclusive;
        MaximumInclusive = maximumInclusive;
    }

    /// <summary>Creates an independent copy.</summary>
    /// <returns>A new interval with the same endpoints and inclusion rules.</returns>
    public ChartNumericInterval Copy()
    {
        return new ChartNumericInterval(Minimum, Maximum, MinimumInclusive, MaximumInclusive);
    }

    /// <summary>Returns the lower endpoint.</summary>
    /// <returns>The current lower endpoint.</returns>
    public double GetMinimum()
    {
        return Minimum;
    }

    /// <summary>Returns the upper endpoint.</summary>
    /// <returns>The current upper endpoint.</returns>
    public double GetMaximum()
    {
        return Maximum;
    }

    /// <summary>Returns whether the lower endpoint is included.</summary>
    /// <returns><c>true</c> when the lower endpoint is included.</returns>
    public bool GetMinimumInclusive()
    {
        return MinimumInclusive;
    }

    /// <summary>Returns whether the upper endpoint is included.</summary>
    /// <returns><c>true</c> when the upper endpoint is included.</returns>
    public bool GetMaximumInclusive()
    {
        return MaximumInclusive;
    }

    internal static void ValidateBounds(double minimum, double maximum)
    {
        ChartLinearScale.ValidateFinite(minimum, "minimum");
        ChartLinearScale.ValidateFinite(maximum, "maximum");
        if (maximum < minimum)
        {
            throw new System.ArgumentException("Interval maximum cannot be less than its minimum.");
        }
    }
}
