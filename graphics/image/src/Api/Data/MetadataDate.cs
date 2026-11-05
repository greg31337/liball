namespace image;

/// <summary>Stores a parsed metadata date without inventing missing precision or a missing time zone.</summary>
public class MetadataDate
{
    /// <summary>Indicates whether the source text matched a supported date form and contained valid components.</summary>
    public bool IsValid;
    /// <summary>The original caller-supplied text, including its formatting and whitespace.</summary>
    public string OriginalText;
    /// <summary>The most precise date or time component present in the source.</summary>
    public MetadataDatePrecision Precision;
    /// <summary>The four-digit year, or zero when unavailable.</summary>
    public int Year;
    /// <summary>The month from 1 through 12, or zero when unavailable.</summary>
    public int Month;
    /// <summary>The day of the month, or zero when unavailable.</summary>
    public int Day;
    /// <summary>The hour from 0 through 23, or zero when unavailable.</summary>
    public int Hour;
    /// <summary>The minute from 0 through 59, or zero when unavailable.</summary>
    public int Minute;
    /// <summary>The second from 0 through 60, where 60 represents a source leap second.</summary>
    public int Second;
    /// <summary>The fractional-second digits exactly as written after the decimal separator.</summary>
    public string FractionalSecondText;
    /// <summary>Indicates that the source included an explicit UTC designator or numeric time-zone offset.</summary>
    public bool HasTimeZone;
    /// <summary>The signed time-zone offset east of UTC in minutes when <see cref="HasTimeZone"/> is true.</summary>
    public int TimeZoneOffsetMinutes;

    /// <summary>Creates an invalid result with empty source text and unknown precision.</summary>
    public MetadataDate()
    {
        IsValid = false;
        OriginalText = "";
        Precision = MetadataDatePrecision.Unknown;
        Year = 0;
        Month = 0;
        Day = 0;
        Hour = 0;
        Minute = 0;
        Second = 0;
        FractionalSecondText = "";
        HasTimeZone = false;
        TimeZoneOffsetMinutes = 0;
    }
}
