namespace icc;

/// <summary>
/// Represents the six unsigned components of an ICC dateTimeNumber.
/// </summary>
public class IccDateTime
{
    /// <summary>
    /// Gets or sets the year component.
    /// </summary>
    public int Year;

    /// <summary>
    /// Gets or sets the month component.
    /// </summary>
    public int Month;

    /// <summary>
    /// Gets or sets the day component.
    /// </summary>
    public int Day;

    /// <summary>
    /// Gets or sets the hour component.
    /// </summary>
    public int Hour;

    /// <summary>
    /// Gets or sets the minute component.
    /// </summary>
    public int Minute;

    /// <summary>
    /// Gets or sets the second component.
    /// </summary>
    public int Second;

    /// <summary>
    /// Initializes an unspecified ICC date and time.
    /// </summary>
    public IccDateTime()
    {
        Year = 0;
        Month = 0;
        Day = 0;
        Hour = 0;
        Minute = 0;
        Second = 0;
    }
}
