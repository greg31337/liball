namespace iso20022;

/// <summary>Represents an ISO text value and its source lexical form.</summary>
public class IsoTextValue
{
    /// <summary>The text value after applicable whitespace processing.</summary>
    public string Value = string.Empty;
    /// <summary>The original lexical form.</summary>
    public string LexicalValue = string.Empty;
}
