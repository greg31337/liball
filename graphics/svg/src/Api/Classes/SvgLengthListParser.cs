using System;

namespace svg;

/// <summary>Parses stroke dash arrays and SVG text positioning length lists.</summary>
public class SvgLengthListParser : ISvgLengthListParser
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgLengthListParser()
    {
    }

    /// <summary>Parses <c>stroke-dasharray</c>, including <c>none</c> and CSS-wide keywords.</summary>
    /// <param name="value">The dash-array text.</param>
    /// <param name="maxItems">The maximum dash count.</param>
    /// <param name="maxTokenCharacters">The maximum typed-length token length.</param>
    /// <returns>A typed dash list or stable failure.</returns>
    public SvgLengthListParseResult ParseDashArray(string value, int maxItems, int maxTokenCharacters)
    {
        return new SvgLengthListParser().ParseLengths(value, maxItems, maxTokenCharacters, true, true);
    }

    /// <summary>Parses an <c>x</c>, <c>y</c>, <c>dx</c>, or <c>dy</c> text positioning list.</summary>
    /// <param name="value">The positioning list text.</param>
    /// <param name="maxItems">The maximum position count.</param>
    /// <param name="maxTokenCharacters">The maximum typed-length token length.</param>
    /// <returns>A typed positioning list or stable failure.</returns>
    public SvgLengthListParseResult ParseTextPosition(string value, int maxItems, int maxTokenCharacters)
    {
        return new SvgLengthListParser().ParseLengths(value, maxItems, maxTokenCharacters, false, false);
    }

    /// <summary>Parses an SVG text <c>x</c> list.</summary>
    /// <param name="value">The list text.</param>
    /// <param name="maxItems">The maximum item count.</param>
    /// <param name="maxTokenCharacters">The maximum token length.</param>
    /// <returns>A typed position list.</returns>
    public SvgLengthListParseResult ParseX(string value, int maxItems, int maxTokenCharacters)
    {
        return new SvgLengthListParser().ParseTextPosition(value, maxItems, maxTokenCharacters);
    }

    /// <summary>Parses an SVG text <c>y</c> list.</summary>
    /// <param name="value">The list text.</param>
    /// <param name="maxItems">The maximum item count.</param>
    /// <param name="maxTokenCharacters">The maximum token length.</param>
    /// <returns>A typed position list.</returns>
    public SvgLengthListParseResult ParseY(string value, int maxItems, int maxTokenCharacters)
    {
        return new SvgLengthListParser().ParseTextPosition(value, maxItems, maxTokenCharacters);
    }

    /// <summary>Parses an SVG text <c>dx</c> list.</summary>
    /// <param name="value">The list text.</param>
    /// <param name="maxItems">The maximum item count.</param>
    /// <param name="maxTokenCharacters">The maximum token length.</param>
    /// <returns>A typed position list.</returns>
    public SvgLengthListParseResult ParseDx(string value, int maxItems, int maxTokenCharacters)
    {
        return new SvgLengthListParser().ParseTextPosition(value, maxItems, maxTokenCharacters);
    }

    /// <summary>Parses an SVG text <c>dy</c> list.</summary>
    /// <param name="value">The list text.</param>
    /// <param name="maxItems">The maximum item count.</param>
    /// <param name="maxTokenCharacters">The maximum token length.</param>
    /// <returns>A typed position list.</returns>
    public SvgLengthListParseResult ParseDy(string value, int maxItems, int maxTokenCharacters)
    {
        return new SvgLengthListParser().ParseTextPosition(value, maxItems, maxTokenCharacters);
    }
SvgLengthListParseResult ParseLengths(string? value, int maximum, int tokenMaximum, bool allowNone, bool nonnegative)
    {
        if (value == null || maximum < 1 || tokenMaximum < 1) return new SvgLengthListParser().Failure(SvgSpecificListParseError.InvalidArgument, 0, null);
        SvgListKeyword keyword = SvgSpecificListRules.ReadWholeKeyword(value);
        if (keyword != SvgListKeyword.Values)
        {
            if (SvgSpecificListRules.IsCssWide(keyword) || keyword == SvgListKeyword.None && allowNone) return new SvgLengthListParser().Success(new SvgLengthListValue(keyword, new System.Collections.Generic.List<SvgLengthListItem>(), value));
            return new SvgLengthListParser().Failure(SvgSpecificListParseError.InvalidKeyword, SvgSpecificListRules.Start(value), null);
        }
        int position = SvgSpecificListRules.SkipWhitespace(value, 0);
        if (position == value.Length) return new SvgLengthListParser().Failure(SvgSpecificListParseError.ExpectedValue, position, null);
        System.Collections.Generic.List<SvgLengthListItem> items = new System.Collections.Generic.List<SvgLengthListItem>();
        bool allZero = true;
        while (position < value.Length)
        {
            if (items.Count >= maximum) return new SvgLengthListParser().PrefixFailure(SvgSpecificListParseError.ItemLimitExceeded, position, items, value);
            int start = position;
            while (position < value.Length && value[position] != ',' && !SvgSpanCharacters.IsCssWhitespace(value[position])) position = position + 1;
            int end = position;
            if (end == start) return new SvgLengthListParser().PrefixFailure(SvgSpecificListParseError.InvalidItem, start, items, value);
            string token = value.Substring(start, end - start);
            SvgLengthParseResult parsed = SvgLengthParser.ParseLength(token, true, true, tokenMaximum);
            if (!parsed.Success)
            {
                SvgSpecificListParseError error = parsed.Error == SvgLengthParseError.TokenTooLong || parsed.NumberError == SvgNumberParseError.TokenTooLong ? SvgSpecificListParseError.TokenTooLong : SvgSpecificListParseError.InvalidItem;
                return new SvgLengthListParser().PrefixFailure(error, start + parsed.ErrorOffset, items, value);
            }
            if (nonnegative && (parsed.Length ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Value < 0) return new SvgLengthListParser().PrefixFailure(SvgSpecificListParseError.NegativeDash, start, items, value);
            if ((parsed.Length ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Value != 0) allZero = false;
            items.Add(new SvgLengthListItem(parsed.Length, start, end));
            int before = position;
            position = SvgSpecificListRules.SkipWhitespace(value, position);
            if (position < value.Length && value[position] == ',')
            {
                position = SvgSpecificListRules.SkipWhitespace(value, position + 1);
                if (position == value.Length) return new SvgLengthListParser().PrefixFailure(SvgSpecificListParseError.InvalidSeparator, position, items, value);
            }
            else if (position < value.Length && position == before)
            {
                return new SvgLengthListParser().PrefixFailure(SvgSpecificListParseError.InvalidSeparator, position, items, value);
            }
        }
        if (allowNone && allZero) return new SvgLengthListParser().Success(new SvgLengthListValue(SvgListKeyword.None, new System.Collections.Generic.List<SvgLengthListItem>(), value));
        return new SvgLengthListParser().Success(new SvgLengthListValue(SvgListKeyword.Values, items, value));
    }
SvgLengthListParseResult PrefixFailure(SvgSpecificListParseError error, int offset, System.Collections.Generic.List<SvgLengthListItem> items, string source)
    {
        SvgLengthListValue? value = items.Count == 0 ? null : new SvgLengthListValue(SvgListKeyword.Values, items, source);
        return new SvgLengthListParser().Failure(error, offset, value);
    }
SvgLengthListParseResult Success(SvgLengthListValue value)
    {
        return new SvgLengthListParseResult(true, SvgSpecificListParseError.None, value, -1);
    }
SvgLengthListParseResult Failure(SvgSpecificListParseError error, int offset, SvgLengthListValue? value)
    {
        return new SvgLengthListParseResult(false, error, value, offset);
    }
}
