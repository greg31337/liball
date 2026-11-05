namespace svg;

/// <summary>Parses bounded CSS supports conditions with declaration and selector features.</summary>
public class SvgSupportsQueryParser : ISvgSupportsQueryParser
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgSupportsQueryParser()
    {
    }

    /// <summary>Parses an anonymous supports condition.</summary>
    /// <param name="value">The complete condition text.</param>
    /// <param name="limits">The CSS parsing limits.</param>
    /// <returns>The parsed condition and diagnostics.</returns>
    public SvgSupportsParseResult ParseWithValueAndLimits(string value, SvgCssLimits limits)
    {
        return new SvgSupportsQueryParser().ParseWithValueAndLimitsAndSource(value, limits, new SvgSourceUri(""));
    }

    /// <summary>Parses a supports condition with source identity.</summary>
    /// <param name="value">The complete condition text.</param>
    /// <param name="limits">The CSS parsing limits.</param>
    /// <param name="source">The source resource.</param>
    /// <returns>The parsed condition and diagnostics.</returns>
    public SvgSupportsParseResult ParseWithValueAndLimitsAndSource(string value, SvgCssLimits limits, SvgSourceUri source)
    {
        return new SvgSupportsQueryParser().ParseLocated(value, limits, source, 0, 1, 1);
    }

    internal SvgSupportsParseResult ParseLocated(string? value, SvgCssLimits? limits, SvgSourceUri? source, int baseOffset, int baseLine, int baseColumn)
    {
        if (value == null || limits == null || source == null || baseOffset < 0 || baseLine < 1 || baseColumn < 1)
        {
            return new SvgSupportsParseResult(false, null, new System.Collections.Generic.List<SvgDiagnostic>(), 0);
        }
        SvgCssTokenizeResult tokenized = new SvgCssTokenizer().TokenizeWithValueAndMaxTokensAndMaxCharacters(value, limits.MaxTokens, limits.MaxCharacters);
        if (!tokenized.Success)
        {
            SvgSourceSpan span = new SvgSupportsQueryParser().ErrorSpan(source, tokenized, baseOffset, baseLine, baseColumn);
            System.Collections.Generic.List<SvgDiagnostic> failures = new System.Collections.Generic.List<SvgDiagnostic>();
            failures.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InputLimitExceeded, tokenized.Message, span));
            return new SvgSupportsParseResult(false, null, failures, tokenized.ErrorOffset);
        }
        System.Collections.Generic.List<SvgSupportsToken> significant = new SvgSupportsQueryParser().Significant(tokenized.Tokens);
        SvgSupportsParserState state = new SvgSupportsParserState(value, tokenized.Tokens, significant, source, limits, baseOffset, baseLine, baseColumn);
        return state.Parse();
    }
System.Collections.Generic.List<SvgSupportsToken> Significant(System.Collections.Generic.List<SvgCssToken> tokens)
    {
        System.Collections.Generic.List<SvgSupportsToken> result = new System.Collections.Generic.List<SvgSupportsToken>();
        int index = 0;
        while (index < tokens.Count)
        {
            SvgCssToken token = tokens[index];
            if (token.Kind != SvgCssTokenKind.Whitespace && token.Kind != SvgCssTokenKind.Comment) result.Add(new SvgSupportsToken(token, index));
            index = index + 1;
        }
        return result;
    }
SvgSourceSpan ErrorSpan(SvgSourceUri source, SvgCssTokenizeResult tokenized, int baseOffset, int baseLine, int baseColumn)
    {
        if (tokenized.ErrorLine < 1 || tokenized.ErrorColumn < 1) return new SvgSourceSpan(source, baseOffset, baseLine, baseColumn, baseOffset, baseLine, baseColumn);
        int line = baseLine + tokenized.ErrorLine - 1;
        int column = tokenized.ErrorLine == 1 ? baseColumn + tokenized.ErrorColumn - 1 : tokenized.ErrorColumn;
        int offset = baseOffset + tokenized.ErrorOffset;
        return new SvgSourceSpan(source, offset, line, column, offset, line, column);
    }
}
