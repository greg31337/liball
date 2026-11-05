namespace svg;

/// <summary>Tokenizes CSS Syntax Level 3 input while retaining exact source slices.</summary>
public class SvgCssTokenizer : ISvgCssTokenizer
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgCssTokenizer()
    {
    }

    /// <summary>Tokenizes CSS using a validated CSS limit group.</summary>
    /// <param name="value">The decoded CSS source.</param>
    /// <param name="limits">The CSS security limits.</param>
    /// <returns>A complete token stream or a bounded token prefix.</returns>
    public SvgCssTokenizeResult TokenizeWithValueAndLimits(string value, SvgCssLimits? limits)
    {
        if (limits == null)
        {
            return new SvgCssTokenizer().InvalidResult();
        }
        return new SvgCssTokenizer().TokenizeWithValueAndMaxTokensAndMaxCharacters(value, limits.MaxTokens, limits.MaxCharacters);
    }

    /// <summary>Tokenizes CSS using explicit token and source-character bounds.</summary>
    /// <param name="value">The decoded CSS source.</param>
    /// <param name="maxTokens">The maximum emitted token count.</param>
    /// <param name="maxCharacters">The maximum source character count.</param>
    /// <returns>A complete token stream or a bounded token prefix.</returns>
    public SvgCssTokenizeResult TokenizeWithValueAndMaxTokensAndMaxCharacters(string? value, int maxTokens, long maxCharacters)
    {
        if (value == null || maxTokens < 1 || maxCharacters < 1)
        {
            return new SvgCssTokenizer().InvalidResult();
        }
        if (value.Length > maxCharacters)
        {
            int offset = maxCharacters > int.MaxValue ? value.Length : (int)maxCharacters;
            SvgCssLocation location = SvgCssTokenizerState.GetLocation(value, offset);
            return new SvgCssTokenizeResult(false, SvgCssTokenizeError.CharacterLimitExceeded, new System.Collections.Generic.List<SvgCssToken>(), 0, offset, location.Line, location.Column, "The configured CSS character count was exceeded.");
        }
        SvgCssTokenizerState state = new SvgCssTokenizerState(value, maxTokens);
        return state.Tokenize();
    }
SvgCssTokenizeResult InvalidResult()
    {
        return new SvgCssTokenizeResult(false, SvgCssTokenizeError.InvalidArgument, new System.Collections.Generic.List<SvgCssToken>(), 0, 0, 0, 0, "Tokenizer arguments are invalid.");
    }
}
