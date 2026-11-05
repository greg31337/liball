namespace asn1;

/// <summary>
/// Parses ASN.1 tokens or source units with recoverable results.
/// </summary>
public interface IAsnParser
{
    /// <summary>
    /// Parses a token span.
    /// </summary>
    /// <param name="tokens">The token span.</param>
    /// <returns>The parser result.</returns>
    public AsnParserResult ParseTokens(System.ReadOnlySpan<AsnToken> tokens);

    /// <summary>
    /// Parses one source unit.
    /// </summary>
    /// <param name="source">The source unit.</param>
    /// <returns>The parser result.</returns>
    public AsnParserResult ParseSource(AsnSourceUnit source);

    /// <summary>
    /// Continues parsing at a token offset.
    /// </summary>
    /// <param name="tokens">The complete token span.</param>
    /// <param name="tokenOffset">The continuation token offset.</param>
    /// <returns>The continued parser result.</returns>
    public AsnParserResult ContinueParsing(System.ReadOnlySpan<AsnToken> tokens, int tokenOffset);
}
