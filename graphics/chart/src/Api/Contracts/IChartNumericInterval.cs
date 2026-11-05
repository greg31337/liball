namespace chart;
/// <summary>Defines a finite numeric interval with independently inclusive endpoints.</summary>
public interface IChartNumericInterval
{
    /// <summary>Returns the distance between the endpoints.</summary>
    /// <returns>The non-negative interval length.</returns>
    double Length();
    /// <summary>Determines whether the interval contains a value.</summary>
    /// <param name = "value">The finite value to test.</param>
    /// <returns><c>true</c> when the value is contained; otherwise, <c>false</c>.</returns>
    bool Contains(double value);
    /// <summary>Determines whether this interval intersects another interval.</summary>
    /// <param name = "interval">The interval to compare.</param>
    /// <returns><c>true</c> when the intervals share at least one contained value; otherwise, <c>false</c>.</returns>
    bool Intersects(ChartNumericInterval interval);
    /// <summary>Determines whether the interval contains no values.</summary>
    /// <returns><c>true</c> when the interval is empty; otherwise, <c>false</c>.</returns>
    bool IsEmpty();
    /// <summary>Replaces both endpoints and their inclusion rules.</summary>
    /// <param name = "minimum">The finite lower endpoint.</param>
    /// <param name = "maximum">The finite upper endpoint.</param>
    /// <param name = "minimumInclusive"><c>true</c> to include the lower endpoint.</param>
    /// <param name = "maximumInclusive"><c>true</c> to include the upper endpoint.</param>
    void SetBounds(double minimum, double maximum, bool minimumInclusive, bool maximumInclusive);
    /// <summary>Creates an independent copy.</summary>
    /// <returns>A new interval with the same endpoints and inclusion rules.</returns>
    ChartNumericInterval Copy();
    /// <summary>Returns the lower endpoint.</summary>
    /// <returns>The current lower endpoint.</returns>
    double GetMinimum();
    /// <summary>Returns the upper endpoint.</summary>
    /// <returns>The current upper endpoint.</returns>
    double GetMaximum();
    /// <summary>Returns whether the lower endpoint is included.</summary>
    /// <returns><c>true</c> when the lower endpoint is included.</returns>
    bool GetMinimumInclusive();
    /// <summary>Returns whether the upper endpoint is included.</summary>
    /// <returns><c>true</c> when the upper endpoint is included.</returns>
    bool GetMaximumInclusive();
}
