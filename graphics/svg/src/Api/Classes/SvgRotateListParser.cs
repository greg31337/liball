using System;

namespace svg;

/// <summary>Parses SVG text rotate number lists.</summary>
public class SvgRotateListParser : ISvgRotateListParser
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgRotateListParser()
    {
    }

    /// <summary>Parses unitless degree rotations separated by commas or whitespace.</summary>
    /// <param name="value">The rotate-list text.</param>
    /// <param name="maxItems">The maximum rotation count.</param>
    /// <param name="maxTokenCharacters">The maximum number token length.</param>
    /// <returns>A rotation list or stable failure.</returns>
    public SvgRotateListParseResult Parse(string? value, int maxItems, int maxTokenCharacters)
    {
        System.Collections.Generic.List<SvgRotateItem> items = new System.Collections.Generic.List<SvgRotateItem>();
        if (value == null || maxItems < 1 || maxTokenCharacters < 1) return new SvgRotateListParser().Result(false, SvgSpecificListParseError.InvalidArgument, SvgListKeyword.Values, items, 0);
        SvgListKeyword keyword = SvgSpecificListRules.ReadWholeKeyword(value);
        if (SvgSpecificListRules.IsCssWide(keyword)) return new SvgRotateListParser().Result(true, SvgSpecificListParseError.None, keyword, items, -1);
        int position = SvgSpecificListRules.SkipWhitespace(value, 0);
        if (position == value.Length) return new SvgRotateListParser().Result(false, SvgSpecificListParseError.ExpectedValue, SvgListKeyword.Values, items, position);
        while (position < value.Length)
        {
            if (items.Count >= maxItems) return new SvgRotateListParser().Result(false, SvgSpecificListParseError.ItemLimitExceeded, SvgListKeyword.Values, items, position);
            int start = position;
            while (position < value.Length && value[position] != ',' && !SvgSpanCharacters.IsCssWhitespace(value[position])) position = position + 1;
            string token = value.Substring(start, position - start);
            SvgNumberParseResult number = SvgNumberParser.ParseNumber(token, maxTokenCharacters);
            if (!number.Success)
            {
                SvgSpecificListParseError error = number.Error == SvgNumberParseError.TokenTooLong ? SvgSpecificListParseError.TokenTooLong : SvgSpecificListParseError.InvalidItem;
                return new SvgRotateListParser().Result(false, error, SvgListKeyword.Values, items, start + number.ErrorOffset);
            }
            items.Add(new SvgRotateItem(new SvgAngle(number.Value, SvgAngleUnit.Number), start, position));
            int before = position;
            position = SvgSpecificListRules.SkipWhitespace(value, position);
            if (position < value.Length && value[position] == ',')
            {
                position = SvgSpecificListRules.SkipWhitespace(value, position + 1);
                if (position == value.Length) return new SvgRotateListParser().Result(false, SvgSpecificListParseError.InvalidSeparator, SvgListKeyword.Values, items, position);
            }
            else if (position < value.Length && position == before) return new SvgRotateListParser().Result(false, SvgSpecificListParseError.InvalidSeparator, SvgListKeyword.Values, items, position);
        }
        return new SvgRotateListParser().Result(true, SvgSpecificListParseError.None, SvgListKeyword.Values, items, -1);
    }
SvgRotateListParseResult Result(bool success, SvgSpecificListParseError error, SvgListKeyword keyword, System.Collections.Generic.List<SvgRotateItem> items, int offset)
    {
        return new SvgRotateListParseResult(success, error, keyword, items, offset);
    }
}
