namespace iso20022;

/// <summary>Represents an ISO decimal while retaining lexical scale and signed-zero state.</summary>
public class IsoDecimalValue
{
    /// <summary>The numeric value.</summary>
    public decimal Value;
    /// <summary>The number of fractional digits in the lexical form.</summary>
    public int Scale;
    /// <summary>Whether the lexical form represented negative zero.</summary>
    public bool IsNegativeZero;
    /// <summary>The original lexical form.</summary>
    public string LexicalValue = string.Empty;
}
