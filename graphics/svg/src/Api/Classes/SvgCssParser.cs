namespace svg;

/// <summary>Parses bounded CSS stylesheets, declaration lists, and style attributes.</summary>
public class SvgCssParser : ISvgCssParser
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgCssParser()
    {
    }

    /// <summary>Parses an anonymous author-origin stylesheet.</summary>
    /// <param name="value">The complete CSS source.</param>
    /// <param name="limits">The CSS security limits.</param>
    /// <returns>The parsed stylesheet and recovery diagnostics.</returns>
    public SvgCssParseResult ParseStyleSheetWithValueAndLimits(string value, SvgCssLimits limits)
    {
        return new SvgCssParser().ParseStyleSheetWithValueAndLimitsAndOriginAndSource(value, limits, SvgStyleOrigin.Author, new SvgSourceUri(""));
    }

    /// <summary>Parses a stylesheet with explicit origin and source identity.</summary>
    /// <param name="value">The complete CSS source.</param>
    /// <param name="limits">The CSS security limits.</param>
    /// <param name="origin">The cascade origin.</param>
    /// <param name="source">The source resource.</param>
    /// <returns>The parsed stylesheet and recovery diagnostics.</returns>
    public SvgCssParseResult ParseStyleSheetWithValueAndLimitsAndOriginAndSource(string value, SvgCssLimits limits, SvgStyleOrigin origin, SvgSourceUri source)
    {
        if (!new SvgCssParser().ValidArguments(value, limits, source) || origin < SvgStyleOrigin.UserAgent || origin > SvgStyleOrigin.Transition)
        {
            return new SvgCssParser().InvalidResult(SvgCssParseMode.StyleSheet);
        }
        SvgCssParserState state = new SvgCssParser().CreateState(value, limits, source, SvgCssParseMode.StyleSheet);
        if (state == null) return new SvgCssParser().InvalidResult(SvgCssParseMode.StyleSheet);
        return state.ParseStyleSheet(origin);
    }

    /// <summary>Parses an anonymous standalone declaration list.</summary>
    /// <param name="value">The declaration-list source.</param>
    /// <param name="limits">The CSS security limits.</param>
    /// <returns>The parsed declarations and recovery diagnostics.</returns>
    public SvgCssParseResult ParseDeclarationListWithValueAndLimits(string value, SvgCssLimits limits)
    {
        return new SvgCssParser().ParseDeclarationListWithValueAndLimitsAndSource(value, limits, new SvgSourceUri(""));
    }

    /// <summary>Parses a standalone declaration list with source identity.</summary>
    /// <param name="value">The declaration-list source.</param>
    /// <param name="limits">The CSS security limits.</param>
    /// <param name="source">The source resource.</param>
    /// <returns>The parsed declarations and recovery diagnostics.</returns>
    public SvgCssParseResult ParseDeclarationListWithValueAndLimitsAndSource(string value, SvgCssLimits limits, SvgSourceUri source)
    {
        if (!new SvgCssParser().ValidArguments(value, limits, source)) return new SvgCssParser().InvalidResult(SvgCssParseMode.DeclarationList);
        SvgCssParserState state = new SvgCssParser().CreateState(value, limits, source, SvgCssParseMode.DeclarationList);
        if (state == null) return new SvgCssParser().InvalidResult(SvgCssParseMode.DeclarationList);
        return state.ParseDeclarations();
    }

    /// <summary>Parses an anonymous style attribute.</summary>
    /// <param name="value">The style attribute source.</param>
    /// <param name="limits">The CSS security limits.</param>
    /// <returns>The parsed declarations and recovery diagnostics.</returns>
    public SvgCssParseResult ParseStyleAttributeWithValueAndLimits(string value, SvgCssLimits limits)
    {
        return new SvgCssParser().ParseStyleAttributeWithValueAndLimitsAndSource(value, limits, new SvgSourceUri(""));
    }

    /// <summary>Parses a style attribute with source identity.</summary>
    /// <param name="value">The style attribute source.</param>
    /// <param name="limits">The CSS security limits.</param>
    /// <param name="source">The source resource.</param>
    /// <returns>The parsed declarations and recovery diagnostics.</returns>
    public SvgCssParseResult ParseStyleAttributeWithValueAndLimitsAndSource(string value, SvgCssLimits limits, SvgSourceUri source)
    {
        if (!new SvgCssParser().ValidArguments(value, limits, source)) return new SvgCssParser().InvalidResult(SvgCssParseMode.StyleAttribute);
        SvgCssParserState state = new SvgCssParser().CreateState(value, limits, source, SvgCssParseMode.StyleAttribute);
        if (state == null) return new SvgCssParser().InvalidResult(SvgCssParseMode.StyleAttribute);
        return state.ParseDeclarations();
    }

    /// <summary>Parses an anonymous selector list.</summary>
    /// <param name="value">The selector source.</param>
    /// <param name="limits">The CSS security limits.</param>
    /// <returns>The parsed selector list or located failure.</returns>
    public SvgSelectorParseResult ParseSelectorWithValueAndLimits(string value, SvgCssLimits limits)
    {
        return new SvgSelectorParser().ParseWithValueAndLimits(value, limits);
    }

    /// <summary>Parses a selector list with source identity.</summary>
    /// <param name="value">The selector source.</param>
    /// <param name="limits">The CSS security limits.</param>
    /// <param name="source">The source resource.</param>
    /// <returns>The parsed selector list or located failure.</returns>
    public SvgSelectorParseResult ParseSelectorWithValueAndLimitsAndSource(string value, SvgCssLimits limits, SvgSourceUri source)
    {
        return new SvgSelectorParser().ParseWithValueAndLimitsAndSource(value, limits, source);
    }
SvgCssParserState CreateState(string value, SvgCssLimits limits, SvgSourceUri? source, SvgCssParseMode mode)
    {
        SvgCssTokenizeResult tokenized = new SvgCssTokenizer().TokenizeWithValueAndMaxTokensAndMaxCharacters(value, limits.MaxTokens, limits.MaxCharacters);
        return new SvgCssParserState(value, tokenized, limits, source, mode);
    }
bool ValidArguments(string? value, SvgCssLimits? limits, SvgSourceUri? source)
    {
        if (value == null || limits == null || source == null) return false;
        return limits.MaxCharacters > 0 && limits.MaxTokens > 0 && limits.MaxRules > 0 && limits.MaxDeclarations > 0 && limits.MaxBlockDepth > 0;
    }
SvgCssParseResult InvalidResult(SvgCssParseMode mode)
    {
        return new SvgCssParseResult(false, mode, SvgCssParseError.InvalidArgument, null, new System.Collections.Generic.List<SvgCssDeclaration>(), new System.Collections.Generic.List<SvgCssUnknownSyntax>(), new System.Collections.Generic.List<SvgDiagnostic>(), 0, "Parser arguments are invalid.");
    }
}
