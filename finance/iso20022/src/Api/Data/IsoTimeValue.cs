namespace iso20022;

/// <summary>Represents an XSD time with exact fractional-second and timezone state.</summary>
public class IsoTimeValue
{
    /// <summary>The hour component.</summary>
    public int Hour;
    /// <summary>The minute component.</summary>
    public int Minute;
    /// <summary>The integral second component.</summary>
    public int Second;
    /// <summary>The fractional-second digits without the decimal separator.</summary>
    public string FractionalSecondDigits = string.Empty;
    /// <summary>Whether the source included a timezone.</summary>
    public bool HasTimezone;
    /// <summary>The timezone offset in minutes.</summary>
    public int TimezoneOffsetMinutes;
    /// <summary>Whether the timezone used the UTC <c>Z</c> form.</summary>
    public bool IsUtc;
    /// <summary>Whether a zero timezone offset used the negative <c>-00:00</c> form.</summary>
    public bool IsNegativeTimezoneZero;
    /// <summary>The lexical representation mode.</summary>
    public DateTimeLexicalMode LexicalMode;
    /// <summary>The original lexical form.</summary>
    public string LexicalValue = string.Empty;
}
