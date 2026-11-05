namespace iso20022;

/// <summary>Represents a string-valued code and its code-set identity.</summary>
public class IsoCodeValue
{
    /// <summary>The code value.</summary>
    public string Value = string.Empty;
    /// <summary>The code-set name or identifier.</summary>
    public string CodeSetName = string.Empty;
    /// <summary>The original lexical form.</summary>
    public string LexicalValue = string.Empty;
}
