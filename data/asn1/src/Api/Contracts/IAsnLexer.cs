namespace asn1;

/// <summary>
/// Tokenizes ASN.1 source text and supports explicit continuation offsets.
/// </summary>
public interface IAsnLexer
{
    /// <summary>
    /// Tokenizes one complete source unit.
    /// </summary>
    /// <param name="source">The source unit.</param>
    /// <returns>The lexer result.</returns>
    public AsnLexerResult TokenizeSource(AsnSourceUnit source);

    /// <summary>
    /// Tokenizes a character span under a caller-defined source name.
    /// </summary>
    /// <param name="text">The source character span.</param>
    /// <param name="sourceName">The source name used for token spans.</param>
    /// <returns>The lexer result.</returns>
    public AsnLexerResult TokenizeSpan(System.ReadOnlySpan<char> text, string sourceName);

    /// <summary>
    /// Continues tokenization at a UTF-16 source offset.
    /// </summary>
    /// <param name="source">The complete source unit.</param>
    /// <param name="offset">The continuation offset.</param>
    /// <returns>The continued lexer result.</returns>
    public AsnLexerResult ContinueTokenization(AsnSourceUnit source, int offset);
}
