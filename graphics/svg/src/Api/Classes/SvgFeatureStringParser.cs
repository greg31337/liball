namespace svg;

/// <summary>Parses whitespace-separated SVG feature strings.</summary>
public class SvgFeatureStringParser : ISvgFeatureStringParser
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgFeatureStringParser()
    {
    }

    /// <summary>Parses a nonempty required-features string list.</summary>
    /// <param name="value">The feature string list.</param>
    /// <param name="maxItems">The maximum feature count.</param>
    /// <param name="maxTokenCharacters">The maximum feature string length.</param>
    /// <returns>A preserved source-mapped list or stable failure.</returns>
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
            while (position < value.Length && !SvgSpanCharacters.IsCssWhitespace(value[position])) position = position + 1;
            int end = position;
            if (end - start > maxTokenCharacters) return new SvgTextTokenListParseResult(false, SvgIriParseError.TokenTooLong, items, start + maxTokenCharacters);
            int invalid = SvgIriLexical.ValidateIri(value, start, end);
            if (invalid >= 0) return new SvgTextTokenListParseResult(false, SvgIriParseError.InvalidFeatureString, items, invalid);
            string token = value.Substring(start, end - start);
            items.Add(new SvgTextToken(token, token, start, end));
            int next = SvgIriLexical.SkipWhitespace(value, position);
            if (next == position && next < value.Length) return new SvgTextTokenListParseResult(false, SvgIriParseError.InvalidSeparator, items, next);
            position = next;
        }
        return new SvgTextTokenListParseResult(true, SvgIriParseError.None, items, -1);
    }
}
