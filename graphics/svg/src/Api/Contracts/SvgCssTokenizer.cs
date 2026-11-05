namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgCssTokenizer"/>.</summary>
public interface ISvgCssTokenizer
{
    /// <summary>Tokenizes CSS using a validated CSS limit group.</summary>
    /// <param name="value">The decoded CSS source.</param>
    /// <param name="limits">The CSS security limits.</param>
    /// <returns>A complete token stream or a bounded token prefix.</returns>
    SvgCssTokenizeResult TokenizeWithValueAndLimits(string value, SvgCssLimits? limits);
    /// <summary>Tokenizes CSS using explicit token and source-character bounds.</summary>
    /// <param name="value">The decoded CSS source.</param>
    /// <param name="maxTokens">The maximum emitted token count.</param>
    /// <param name="maxCharacters">The maximum source character count.</param>
    /// <returns>A complete token stream or a bounded token prefix.</returns>
    SvgCssTokenizeResult TokenizeWithValueAndMaxTokensAndMaxCharacters(string? value, int maxTokens, long maxCharacters);
}
