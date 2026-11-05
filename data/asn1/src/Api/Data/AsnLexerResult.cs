namespace asn1;

/// <summary>
/// Contains the tokens, diagnostics, and progress produced by lexing one source unit.
/// </summary>
public class AsnLexerResult
{
    /// <summary>
    /// Contains the emitted tokens in source order.
    /// </summary>
    public AsnToken[] Tokens = [];

    /// <summary>
    /// Contains lexical diagnostics.
    /// </summary>
    public AsnDiagnostic[] Diagnostics = [];

    /// <summary>
    /// Contains the number of UTF-16 code units consumed from the source text.
    /// </summary>
    public int ConsumedCharacterCount;

    /// <summary>
    /// Indicates whether the lexer reached the end of the supplied source unit.
    /// </summary>
    public bool Complete;
}
