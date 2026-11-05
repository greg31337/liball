namespace iso20022;

/// <summary>Represents an XSD duration without reducing calendar units to elapsed seconds.</summary>
public class IsoDurationValue
{
    /// <summary>Whether the duration is negative.</summary>
    public bool IsNegative;
    /// <summary>The year component.</summary>
    public int Years;
    /// <summary>Whether the lexical form included the year component.</summary>
    public bool HasYears;
    /// <summary>The month component.</summary>
    public int Months;
    /// <summary>Whether the lexical form included the month component.</summary>
    public bool HasMonths;
    /// <summary>The day component.</summary>
    public int Days;
    /// <summary>Whether the lexical form included the day component.</summary>
    public bool HasDays;
    /// <summary>The hour component.</summary>
    public int Hours;
    /// <summary>Whether the lexical form included the hour component.</summary>
    public bool HasHours;
    /// <summary>The minute component.</summary>
    public int Minutes;
    /// <summary>Whether the lexical form included the minute component.</summary>
    public bool HasMinutes;
    /// <summary>The integral second component.</summary>
    public int Seconds;
    /// <summary>Whether the lexical form included the second component.</summary>
    public bool HasSeconds;
    /// <summary>The fractional-second digits without the decimal separator.</summary>
    public string FractionalSecondDigits = string.Empty;
    /// <summary>The original lexical form.</summary>
    public string LexicalValue = string.Empty;
}
