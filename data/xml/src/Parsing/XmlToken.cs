namespace xml;

internal class XmlToken
{
    internal XmlTokenKind Kind;
    internal string? Name;
    internal string? Value;
    internal string Lexeme;
    internal int StartUtf16Offset;
    internal int Utf16Length;
    internal int ScalarValue;
    internal bool RestrictedCharacter;
    internal char Quote;
    internal bool HasNameValueSeparator;
    internal XmlDeclarationInfo? Declaration;

    internal XmlToken(XmlTokenKind kind, string? name, string? value, string lexeme, int startUtf16Offset, int utf16Length, int scalarValue, bool restrictedCharacter, char quote, bool hasNameValueSeparator, XmlDeclarationInfo? declaration)
    {
        Kind = kind;
        Name = name;
        Value = value;
        Lexeme = lexeme;
        StartUtf16Offset = startUtf16Offset;
        Utf16Length = utf16Length;
        ScalarValue = scalarValue;
        RestrictedCharacter = restrictedCharacter;
        Quote = quote;
        HasNameValueSeparator = hasNameValueSeparator;
        Declaration = declaration;
    }
}
