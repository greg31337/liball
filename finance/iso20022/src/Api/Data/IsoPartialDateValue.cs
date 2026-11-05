namespace iso20022;

/// <summary>Represents an XSD Gregorian partial-date value.</summary>
public class IsoPartialDateValue
{
    /// <summary>The partial-date form.</summary>
    public PartialDateKind Kind;
    /// <summary>The year component when present.</summary>
    public int Year;
    /// <summary>The month component when present.</summary>
    public int Month;
    /// <summary>The day component when present.</summary>
    public int Day;
    /// <summary>Whether the source included a timezone.</summary>
    public bool HasTimezone;
    /// <summary>The timezone offset in minutes.</summary>
    public int TimezoneOffsetMinutes;
    /// <summary>Whether the timezone used the UTC <c>Z</c> form.</summary>
    public bool IsUtc;
    /// <summary>Whether a zero timezone offset used the negative <c>-00:00</c> form.</summary>
    public bool IsNegativeTimezoneZero;
    /// <summary>The original lexical form.</summary>
    public string LexicalValue = string.Empty;
}
