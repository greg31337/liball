namespace geo;

/// <summary>Stores the classification of a parsing failure.</summary>
public class ParseError
{
    /// <summary>The parsing failure classification.</summary>
    public ParseErrorKind Kind;

    /// <summary>The actionable parsing failure message.</summary>
    public string Message = "";

    /// <summary>The zero-based character or byte offset of the failure.</summary>
    public long Offset = -1;

    /// <summary>The JSON path associated with the failure, or an empty string.</summary>
    public string JsonPath = "";

    /// <summary>The geometry component path associated with the failure, or an empty string.</summary>
    public string ComponentPath = "";
}
