namespace currencycodes;

/// <summary>
/// Identifies how precisely a source date is known.
/// </summary>
public enum PartialDatePrecision
{
    /// <summary>
    /// No date component is known.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Only the year is known.
    /// </summary>
    Year = 1,

    /// <summary>
    /// The year and month are known.
    /// </summary>
    Month = 2,

    /// <summary>
    /// The year, month, and day are known.
    /// </summary>
    Day = 3
}
