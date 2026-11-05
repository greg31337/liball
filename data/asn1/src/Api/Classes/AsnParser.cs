namespace asn1;

/// <summary>
/// Exposes ASN.1 parsing through a composed parser implementation.
/// </summary>
public class AsnParser : IAsnParser
{
    internal IAsnParser Implementation;

    /// <summary>
    /// Initializes a parser facade over an operation implementation.
    /// </summary>
    /// <param name="implementation">The implementation that performs parsing.</param>
    public AsnParser(IAsnParser implementation)
    {
        this.Implementation = implementation == null ? throw new System.ArgumentNullException("implementation") : implementation;
    }

    /// <summary>
    /// Parses a token span.
    /// </summary>
    /// <param name="tokens">The token span.</param>
    /// <returns>The parser result.</returns>
    public AsnParserResult ParseTokens(System.ReadOnlySpan<AsnToken> tokens)
    {
        return this.Implementation.ParseTokens(tokens);
    }

    /// <summary>
    /// Parses one source unit.
    /// </summary>
    /// <param name="source">The source unit.</param>
    /// <returns>The parser result.</returns>
    public AsnParserResult ParseSource(AsnSourceUnit source)
    {
        return this.Implementation.ParseSource(source);
    }

    /// <summary>
    /// Continues parsing at a token offset.
    /// </summary>
    /// <param name="tokens">The complete token span.</param>
    /// <param name="tokenOffset">The continuation token offset.</param>
    /// <returns>The continued parser result.</returns>
    public AsnParserResult ContinueParsing(System.ReadOnlySpan<AsnToken> tokens, int tokenOffset)
    {
        return this.Implementation.ContinueParsing(tokens, tokenOffset);
    }
}
