namespace image;

/// <summary>Identifies the most precise component retained from a parsed metadata date.</summary>
public enum MetadataDatePrecision
{
    /// <summary>No valid date component was decoded.</summary>
    Unknown = 0,
    /// <summary>Only the year was present.</summary>
    Year = 1,
    /// <summary>The year and month were present.</summary>
    Month = 2,
    /// <summary>A complete calendar date was present.</summary>
    Day = 3,
    /// <summary>The date and hour were present.</summary>
    Hour = 4,
    /// <summary>The date, hour, and minute were present.</summary>
    Minute = 5,
    /// <summary>The date and whole-second time were present.</summary>
    Second = 6,
    /// <summary>A fractional-second component was present.</summary>
    FractionalSecond = 7
}
