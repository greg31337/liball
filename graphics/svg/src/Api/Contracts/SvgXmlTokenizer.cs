namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgXmlTokenizer"/>.</summary>
public interface ISvgXmlTokenizer
{
    /// <summary>Tokenizes one complete decoded XML source.</summary>
    /// <param name="value">The decoded XML characters.</param>
    /// <param name="maxTokens">The maximum combined top-level, attribute, and nested-reference token count.</param>
    /// <param name="maxTokenCharacters">The maximum raw token character count.</param>
    /// <returns>A complete token stream or valid prefix and located failure.</returns>
    SvgXmlTokenizeResult Tokenize(string? value, int maxTokens, int maxTokenCharacters);
}
