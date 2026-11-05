namespace svg;

/// <summary>Parses comma-separated BCP 47 language tags.</summary>
public class SvgLanguageListParser : ISvgLanguageListParser
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgLanguageListParser()
    {
    }

    /// <summary>Parses a nonempty comma-separated system-language list.</summary>
    /// <param name="value">The language list text.</param>
    /// <param name="maxItems">The maximum language count.</param>
    /// <param name="maxTokenCharacters">The maximum tag length.</param>
    /// <returns>A normalized source-mapped list or stable failure.</returns>
    public SvgTextTokenListParseResult Parse(string value, int maxItems, int maxTokenCharacters)
    {
        System.Collections.Generic.List<SvgTextToken> items = new System.Collections.Generic.List<SvgTextToken>();
        if (value == null || maxItems < 1 || maxTokenCharacters < 1) return new SvgTextTokenListParseResult(false, SvgIriParseError.InvalidArgument, items, 0);
        int position = SvgIriLexical.SkipWhitespace(value, 0);
        if (position == value.Length) return new SvgTextTokenListParseResult(false, SvgIriParseError.ExpectedValue, items, position);
        while (position < value.Length)
        {
            if (items.Count >= maxItems) return new SvgTextTokenListParseResult(false, SvgIriParseError.ItemLimitExceeded, items, position);
            int start = position;
            while (position < value.Length && value[position] != ',' && !SvgSpanCharacters.IsCssWhitespace(value[position])) position = position + 1;
            int end = position;
            if (end == start) return new SvgTextTokenListParseResult(false, SvgIriParseError.InvalidLanguageTag, items, start);
            if (end - start > maxTokenCharacters) return new SvgTextTokenListParseResult(false, SvgIriParseError.TokenTooLong, items, start + maxTokenCharacters);
            string token = value.Substring(start, end - start);
            int invalid = SvgIriLexical.ValidateLanguage(token);
            if (invalid >= 0) return new SvgTextTokenListParseResult(false, SvgIriParseError.InvalidLanguageTag, items, start + invalid);
            items.Add(new SvgTextToken(token.ToLowerInvariant(), token, start, end));
            position = SvgIriLexical.SkipWhitespace(value, position);
            if (position == value.Length) return new SvgTextTokenListParseResult(true, SvgIriParseError.None, items, -1);
            if (value[position] != ',') return new SvgTextTokenListParseResult(false, SvgIriParseError.InvalidSeparator, items, position);
            position = SvgIriLexical.SkipWhitespace(value, position + 1);
            if (position == value.Length) return new SvgTextTokenListParseResult(false, SvgIriParseError.InvalidSeparator, items, position);
        }
        return new SvgTextTokenListParseResult(true, SvgIriParseError.None, items, -1);
    }
}
