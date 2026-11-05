namespace json;

/// <summary>Selects how decoded UTF-16 surrogate code units are handled.</summary>
public enum JsonUnicodePolicy
{
    /// <summary>Allows escaped lone surrogates for syntax-compatible processing.</summary>
    SyntaxCompatible = 0,

    /// <summary>Requires every decoded string to contain only Unicode scalar values.</summary>
    UnicodeScalar = 1
}
