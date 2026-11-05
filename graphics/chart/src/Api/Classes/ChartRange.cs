namespace chart;
/// <summary>
/// Represents a range used by the chart model or rendering pipeline.
/// </summary>
public class ChartRange : IChartRange
{
    internal double Minimum;
    internal double Maximum;
    /// <summary>
    /// Initializes a range from the supplied minimum and maximum.
    /// </summary>
    /// <param name = "minimum">The inclusive lower end of the numeric domain.</param>
    /// <param name = "maximum">The inclusive upper end of the numeric domain.</param>
    public ChartRange(double minimum, double maximum)
    {
        Minimum = minimum;
        Maximum = maximum;
    }

    /// <summary>
    /// Applies the length operation to the supplied data and returns the computed result.
    /// </summary>
    /// <returns>The resulting double.</returns>
    public double Length()
    {
        return Maximum - Minimum;
    }

    /// <summary>
    /// Determines whether this value is valid.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public bool IsValid()
    {
        return !double.IsNaN(Minimum) && !double.IsNaN(Maximum) && !double.IsInfinity(Minimum) && !double.IsInfinity(Maximum) && Maximum >= Minimum;
    }

    /// <summary>
    /// Determines whether this value contains the configured value.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public bool Contains(double value)
    {
        return value >= Minimum && value <= Maximum;
    }

    /// <summary>
    /// Clamps the configured value using the supplied data and configuration.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting double.</returns>
    public double Clamp(double value)
    {
        if (value < Minimum)
        {
            return Minimum;
        }

        if (value > Maximum)
        {
            return Maximum;
        }

        return value;
    }

    /// <summary>
    /// Normalizes the configured value using the supplied data and configuration.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting double.</returns>
    public double Normalize(double value)
    {
        double length = Length();
        if (length == 0.0)
        {
            return 0.0;
        }

        return (value - Minimum) / length;
    }

    /// <summary>
    /// Applies the lerp operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "amount">The amount.</param>
    /// <returns>The resulting double.</returns>
    public double Lerp(double amount)
    {
        return Minimum + Length() * amount;
    }

    /// <summary>
    /// Applies the pad operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "amount">The amount.</param>
    /// <returns>The resulting range.</returns>
    public ChartRange Pad(double amount)
    {
        return new ChartRange(Minimum - amount, Maximum + amount);
    }

    /// <summary>
    /// Expands this value to include the configured value using the supplied data and configuration.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting range.</returns>
    public ChartRange Include(double value)
    {
        double minimum = Minimum;
        double maximum = Maximum;
        if (value < minimum)
        {
            minimum = value;
        }

        if (value > maximum)
        {
            maximum = value;
        }

        return new ChartRange(minimum, maximum);
    }

    /// <summary>Returns the current minimum.</summary>
    /// <returns>The current field value.</returns>
    public double GetMinimum()
    {
        return Minimum;
    }

    /// <summary>Replaces the current minimum.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetMinimum(double value)
    {
        Minimum = value;
    }

    /// <summary>Returns the current maximum.</summary>
    /// <returns>The current field value.</returns>
    public double GetMaximum()
    {
        return Maximum;
    }

    /// <summary>Replaces the current maximum.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetMaximum(double value)
    {
        Maximum = value;
    }
}
