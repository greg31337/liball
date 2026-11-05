namespace asn1;

/// <summary>
/// Exposes ASN.1 tokenization through a composed lexer implementation.
/// </summary>
public class AsnLexer : IAsnLexer
{
    internal IAsnLexer Implementation;

    /// <summary>
    /// Initializes a lexer facade over an operation implementation.
    /// </summary>
    /// <param name="implementation">The implementation that performs tokenization.</param>
    public AsnLexer(IAsnLexer implementation)
    {
        this.Implementation = implementation == null ? throw new System.ArgumentNullException("implementation") : implementation;
    }

    /// <summary>
    /// Tokenizes one complete source unit.
    /// </summary>
    /// <param name="source">The source unit.</param>
    /// <returns>The lexer result.</returns>
    public AsnLexerResult TokenizeSource(AsnSourceUnit source)
    {
        return this.Implementation.TokenizeSource(source);
    }

    /// <summary>
    /// Tokenizes a character span under a caller-defined source name.
    /// </summary>
    /// <param name="text">The source character span.</param>
    /// <param name="sourceName">The source name used for token spans.</param>
    /// <returns>The lexer result.</returns>
    public AsnLexerResult TokenizeSpan(System.ReadOnlySpan<char> text, string sourceName)
    {
        return this.Implementation.TokenizeSpan(text, sourceName);
    }

    /// <summary>
    /// Continues tokenization at a UTF-16 source offset.
    /// </summary>
    /// <param name="source">The complete source unit.</param>
    /// <param name="offset">The continuation offset.</param>
    /// <returns>The continued lexer result.</returns>
    public AsnLexerResult ContinueTokenization(AsnSourceUnit source, int offset)
    {
        return this.Implementation.ContinueTokenization(source, offset);
    }
}
