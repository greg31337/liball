namespace currencycodes;

/// <summary>
/// Stores a source date together with the precision supplied by that source.
/// </summary>
public class PartialDate
{
    /// <summary>
    /// Indicates whether any date component is known.
    /// </summary>
    public bool IsKnown = false;

    /// <summary>
    /// Stores the year when <see cref="IsKnown"/> is <c>true</c>.
    /// </summary>
    public int Year = 0;

    /// <summary>
    /// Stores the month when <see cref="Precision"/> is <see cref="PartialDatePrecision.Month"/> or <see cref="PartialDatePrecision.Day"/>.
    /// </summary>
    public int Month = 0;

    /// <summary>
    /// Stores the day when <see cref="Precision"/> is <see cref="PartialDatePrecision.Day"/>.
    /// </summary>
    public int Day = 0;

    /// <summary>
    /// Identifies which date components are known.
    /// </summary>
    public PartialDatePrecision Precision = PartialDatePrecision.Unknown;
}
