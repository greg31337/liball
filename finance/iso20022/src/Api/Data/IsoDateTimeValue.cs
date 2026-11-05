namespace iso20022;

/// <summary>Represents an XSD date-time with separately addressable date and time state.</summary>
public class IsoDateTimeValue
{
    /// <summary>The date component.</summary>
    public IsoDateValue Date = new IsoDateValue();
    /// <summary>The time component.</summary>
    public IsoTimeValue Time = new IsoTimeValue();
    /// <summary>The lexical representation mode.</summary>
    public DateTimeLexicalMode LexicalMode;
    /// <summary>The original lexical form.</summary>
    public string LexicalValue = string.Empty;
}
