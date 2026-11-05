namespace svg;

/// <summary>Parses bounded Media Queries Level 4 type and feature syntax.</summary>
public class SvgMediaQueryParser : ISvgMediaQueryParser
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgMediaQueryParser()
    {
    }

    /// <summary>Parses an anonymous media query list.</summary>
    /// <param name="value">The complete media query list.</param>
    /// <param name="limits">The CSS token and character limits.</param>
    /// <returns>The parsed list and located diagnostics.</returns>
    public SvgMediaQueryParseResult ParseWithValueAndLimits(string value, SvgCssLimits limits)
    {
        return new SvgMediaQueryParser().ParseWithValueAndLimitsAndSource(value, limits, new SvgSourceUri(""));
    }

    /// <summary>Parses a media query list with source identity.</summary>
    /// <param name="value">The complete media query list.</param>
    /// <param name="limits">The CSS token and character limits.</param>
    /// <param name="source">The source resource.</param>
    /// <returns>The parsed list and located diagnostics.</returns>
    public SvgMediaQueryParseResult ParseWithValueAndLimitsAndSource(string value, SvgCssLimits limits, SvgSourceUri source)
    {
        return new SvgMediaQueryParser().ParseLocated(value, limits, source, 0, 1, 1);
    }

    internal SvgMediaQueryParseResult ParseLocated(string? value, SvgCssLimits? limits, SvgSourceUri? source, int baseOffset, int baseLine, int baseColumn)
    {
        if (value == null || limits == null || source == null || baseOffset < 0 || baseLine < 1 || baseColumn < 1)
        {
            return new SvgMediaQueryParseResult(false, null, new System.Collections.Generic.List<SvgDiagnostic>(), 0);
        }
        SvgCssTokenizeResult tokenized = new SvgCssTokenizer().TokenizeWithValueAndMaxTokensAndMaxCharacters(value, limits.MaxTokens, limits.MaxCharacters);
        if (!tokenized.Success)
        {
            SvgSourceSpan span = new SvgMediaQueryParser().ErrorSpan(source, tokenized, baseOffset, baseLine, baseColumn);
            System.Collections.Generic.List<SvgDiagnostic> failures = new System.Collections.Generic.List<SvgDiagnostic>();
            failures.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InputLimitExceeded, tokenized.Message, span));
            return new SvgMediaQueryParseResult(false, null, failures, tokenized.ErrorOffset);
        }
        SvgMediaQueryParserState state = new SvgMediaQueryParserState(value, new SvgMediaQueryParser().Significant(tokenized.Tokens), source, baseOffset, baseLine, baseColumn, limits.MaxRules, limits.MaxDeclarations);
        return state.Parse();
    }
System.Collections.Generic.List<SvgCssToken> Significant(System.Collections.Generic.List<SvgCssToken> tokens)
    {
        System.Collections.Generic.List<SvgCssToken> result = new System.Collections.Generic.List<SvgCssToken>();
        int index = 0;
        while (index < tokens.Count)
        {
            SvgCssToken token = tokens[index];
            if (token.Kind != SvgCssTokenKind.Whitespace && token.Kind != SvgCssTokenKind.Comment) result.Add(token);
            index = index + 1;
        }
        return result;
    }
SvgSourceSpan ErrorSpan(SvgSourceUri source, SvgCssTokenizeResult tokenized, int baseOffset, int baseLine, int baseColumn)
    {
        if (tokenized.ErrorLine < 1 || tokenized.ErrorColumn < 1)
        {
            return new SvgSourceSpan(source, baseOffset, baseLine, baseColumn, baseOffset, baseLine, baseColumn);
        }
        int line = baseLine + tokenized.ErrorLine - 1;
        int column = tokenized.ErrorLine <= 1 ? baseColumn + tokenized.ErrorColumn - 1 : tokenized.ErrorColumn;
        int offset = baseOffset + tokenized.ErrorOffset;
        return new SvgSourceSpan(source, offset, line, column, offset, line, column);
    }
}
