namespace iso20022;

/// <summary>Represents an XSD date with explicit timezone and lexical state.</summary>
public class IsoDateValue
{
    /// <summary>The signed Gregorian year.</summary>
    public int Year;
    /// <summary>The Gregorian month.</summary>
    public int Month;
    /// <summary>The Gregorian day.</summary>
    public int Day;
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
