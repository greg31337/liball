namespace json;

/// <summary>Classifies the grammatical form of a JSON number lexeme.</summary>
public enum JsonNumberKind
{
    /// <summary>The lexeme has neither a fraction nor an exponent.</summary>
    Integer = 0,

    /// <summary>The lexeme has a fraction but no exponent.</summary>
    Fraction = 1,

    /// <summary>The lexeme has an exponent but no fraction.</summary>
    Exponent = 2,

    /// <summary>The lexeme has both a fraction and an exponent.</summary>
    FractionWithExponent = 3
}
