namespace xml;

internal class DtdToken
{
    internal DtdTokenKind Kind;
    internal string? Name;
    internal string? Value;
    internal string Lexeme;
    internal int StartUtf16Offset;
    internal int Utf16Length;
    internal char Quote;
    internal bool HasNameValueSeparator;

    internal DtdToken(DtdTokenKind kind, string? name, string? value, string lexeme, int startUtf16Offset, int utf16Length, char quote, bool hasNameValueSeparator)
    {
        Kind = kind;
        Name = name;
        Value = value;
        Lexeme = lexeme;
        StartUtf16Offset = startUtf16Offset;
        Utf16Length = utf16Length;
        Quote = quote;
        HasNameValueSeparator = hasNameValueSeparator;
    }
}
