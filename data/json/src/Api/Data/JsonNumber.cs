namespace json;

/// <summary>Contains an exact JSON number lexeme and its grammatical classification.</summary>
public class JsonNumber
{
    /// <summary>The validated original decimal lexeme.</summary>
    public string? Lexeme;

    /// <summary>The grammatical form of the number lexeme.</summary>
    public JsonNumberKind Kind;

    /// <summary>Indicates whether the original lexeme begins with a minus sign.</summary>
    public bool IsNegative;

    /// <summary>Indicates whether the original lexeme contains a fraction.</summary>
    public bool HasFraction;

    /// <summary>Indicates whether the original lexeme contains an exponent.</summary>
    public bool HasExponent;

    /// <summary>Indicates whether the original lexeme represents negative zero.</summary>
    public bool IsNegativeZero;
}
