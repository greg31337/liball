using System;

namespace svg;

/// <summary>Parses the SVG <c>paint-order</c> component list.</summary>
public class SvgPaintOrderParser : ISvgPaintOrderParser
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgPaintOrderParser()
    {
    }

    /// <summary>Parses <c>normal</c>, CSS-wide keywords, or unique fill/stroke/markers components.</summary>
    /// <param name="value">The paint-order text.</param>
    /// <returns>A paint-order value or stable failure.</returns>
    public SvgPaintOrderParseResult Parse(string? value)
    {
        System.Collections.Generic.List<SvgPaintOrderComponent> items = new System.Collections.Generic.List<SvgPaintOrderComponent>();
        if (value == null) return new SvgPaintOrderParser().Result(false, SvgSpecificListParseError.InvalidArgument, SvgListKeyword.Values, items, 0);
        SvgListKeyword keyword = SvgSpecificListRules.ReadWholeKeyword(value);
        if (keyword == SvgListKeyword.Normal || SvgSpecificListRules.IsCssWide(keyword)) return new SvgPaintOrderParser().Result(true, SvgSpecificListParseError.None, keyword, items, -1);
        int position = SvgSpecificListRules.SkipWhitespace(value, 0);
        if (position == value.Length) return new SvgPaintOrderParser().Result(false, SvgSpecificListParseError.ExpectedValue, SvgListKeyword.Values, items, position);
        while (position < value.Length)
        {
            int start = position;
            while (position < value.Length && !SvgSpanCharacters.IsCssWhitespace(value[position])) position = position + 1;
            string name = value.Substring(start, position - start).ToLowerInvariant();
            SvgPaintOrderComponent component;
            if (name == "fill") component = SvgPaintOrderComponent.Fill;
            else if (name == "stroke") component = SvgPaintOrderComponent.Stroke;
            else if (name == "markers") component = SvgPaintOrderComponent.Markers;
            else return new SvgPaintOrderParser().Result(false, SvgSpecificListParseError.InvalidKeyword, SvgListKeyword.Values, items, start);
            if (items.Contains(component)) return new SvgPaintOrderParser().Result(false, SvgSpecificListParseError.DuplicateItem, SvgListKeyword.Values, items, start);
            items.Add(component);
            position = SvgSpecificListRules.SkipWhitespace(value, position);
        }
        return new SvgPaintOrderParser().Result(true, SvgSpecificListParseError.None, SvgListKeyword.Values, items, -1);
    }
SvgPaintOrderParseResult Result(bool success, SvgSpecificListParseError error, SvgListKeyword keyword, System.Collections.Generic.List<SvgPaintOrderComponent> items, int offset)
    {
        return new SvgPaintOrderParseResult(success, error, keyword, items, offset);
    }
}
