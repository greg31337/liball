namespace svg;

/// <summary>Parses the non-pseudo selector grammar over CSS lexical tokens.</summary>
public class SvgSelectorParser : ISvgSelectorParser
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgSelectorParser()
    {
    }

    /// <summary>Parses an anonymous selector list.</summary>
    /// <param name="value">The selector source.</param>
    /// <param name="limits">The CSS security limits.</param>
    /// <returns>The parsed selector list or located failure.</returns>
    public SvgSelectorParseResult ParseWithValueAndLimits(string value, SvgCssLimits limits)
    {
        return new SvgSelectorParser().ParseWithValueAndLimitsAndSource(value, limits, new SvgSourceUri(""));
    }

    /// <summary>Parses a selector list with source identity.</summary>
    /// <param name="value">The selector source.</param>
    /// <param name="limits">The CSS security limits.</param>
    /// <param name="source">The source resource.</param>
    /// <returns>The parsed selector list or located failure.</returns>
    public SvgSelectorParseResult ParseWithValueAndLimitsAndSource(string? value, SvgCssLimits? limits, SvgSourceUri? source)
    {
        if (value == null || limits == null || source == null || limits.MaxCharacters < 1 || limits.MaxSelectorTokens < 1 || limits.MaxBlockDepth < 1) return new SvgSelectorParser().InvalidResult(source);
        SvgCssTokenizeResult tokenized = new SvgCssTokenizer().TokenizeWithValueAndMaxTokensAndMaxCharacters(value, limits.MaxSelectorTokens, limits.MaxCharacters);
        if (!tokenized.Success)
        {
            SvgSelectorParseError error = tokenized.Error == SvgCssTokenizeError.CharacterLimitExceeded ? SvgSelectorParseError.CharacterLimitExceeded : tokenized.Error == SvgCssTokenizeError.TokenLimitExceeded ? SvgSelectorParseError.TokenLimitExceeded : SvgSelectorParseError.InvalidArgument;
            return new SvgSelectorParser().FailureBeforeParse(value, source, tokenized.Tokens, error, tokenized.ErrorOffset, tokenized.Message);
        }
        SvgSelectorParserState state = new SvgSelectorParserState(value, tokenized.Tokens, 0, tokenized.Tokens.Count, source, limits, 0, false, false);
        return state.Parse();
    }

    /// <summary>Parses a selector token subrange belonging to a larger CSS source.</summary>
    /// <param name="value">The complete CSS source.</param>
    /// <param name="tokens">The complete CSS token stream.</param>
    /// <param name="start">The first selector token index.</param>
    /// <param name="end">The exclusive selector token index.</param>
    /// <param name="limits">The CSS security limits.</param>
    /// <param name="source">The source resource.</param>
    /// <returns>The parsed selector list or located failure.</returns>
    internal SvgSelectorParseResult ParseTokens(string? value, System.Collections.Generic.List<SvgCssToken>? tokens, int start, int end, SvgCssLimits? limits, SvgSourceUri? source)
    {
        if (value == null || tokens == null || limits == null || source == null || start < 0 || end < start || end > tokens.Count || limits.MaxSelectorTokens < 1 || limits.MaxBlockDepth < 1) return new SvgSelectorParser().InvalidResult(source);
        if (end - start > limits.MaxSelectorTokens)
        {
            int offset = tokens[start + limits.MaxSelectorTokens].StartOffset;
            return new SvgSelectorParser().FailureBeforeParse(value, source, new System.Collections.Generic.List<SvgCssToken>(), SvgSelectorParseError.TokenLimitExceeded, offset, "The configured selector token count was exceeded.");
        }
        SvgSelectorParserState state = new SvgSelectorParserState(value, tokens, start, end, source, limits, 0, false, false);
        return state.Parse();
    }
SvgSelectorParseResult InvalidResult(SvgSourceUri? source)
    {
        SvgSourceUri actual = source == null ? new SvgSourceUri("") : source;
        SvgSourceSpan span = new SvgSourceSpan(actual, 0, 1, 1, 0, 1, 1);
        SvgDiagnostic diagnostic = new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidArgument, "Selector parser arguments are invalid.", span);
        System.Collections.Generic.List<SvgDiagnostic> diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        diagnostics.Add(diagnostic);
        SvgSelector selector = new SvgSelector(new System.Collections.Generic.List<SvgComplexSelector>(), "", span, false, diagnostics);
        return new SvgSelectorParseResult(false, SvgSelectorParseError.InvalidArgument, selector, 0, diagnostic.Message);
    }
SvgSelectorParseResult FailureBeforeParse(string value, SvgSourceUri source, System.Collections.Generic.List<SvgCssToken> tokens, SvgSelectorParseError error, int offset, string message)
    {
        SvgCssLocation location = SvgCssTokenizerState.GetLocation(value, offset);
        SvgSourceSpan span = new SvgSourceSpan(source, offset, location.Line, location.Column, offset, location.Line, location.Column);
        SvgDiagnosticCode code = error == SvgSelectorParseError.CharacterLimitExceeded || error == SvgSelectorParseError.TokenLimitExceeded ? SvgDiagnosticCode.InputLimitExceeded : SvgDiagnosticCode.InvalidCss;
        System.Collections.Generic.List<SvgDiagnostic> diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, code, message, span));
        SvgSelector selector = new SvgSelector(new System.Collections.Generic.List<SvgComplexSelector>(), "", span, false, diagnostics);
        return new SvgSelectorParseResult(false, error, selector, offset, message);
    }
}
