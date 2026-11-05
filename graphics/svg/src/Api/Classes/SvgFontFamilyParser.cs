using System;

namespace svg;

/// <summary>Parses quoted and identifier-sequence CSS font-family lists.</summary>
public class SvgFontFamilyParser : ISvgFontFamilyParser
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgFontFamilyParser()
    {
    }

    /// <summary>Parses a comma-separated font-family list.</summary>
    /// <param name="value">The font-family text.</param>
    /// <param name="maxFamilies">The maximum family count.</param>
    /// <param name="maxTokenCharacters">The maximum decoded family length.</param>
    /// <returns>A decoded family list or stable failure.</returns>
    public SvgFontFamilyParseResult Parse(string? value, int maxFamilies, int maxTokenCharacters)
    {
        System.Collections.Generic.List<SvgFontFamily> families = new System.Collections.Generic.List<SvgFontFamily>();
        if (value == null || maxFamilies < 1 || maxTokenCharacters < 1) return new SvgFontFamilyParser().Result(false, SvgSpecificListParseError.InvalidArgument, SvgListKeyword.Values, families, 0);
        SvgListKeyword keyword = SvgSpecificListRules.ReadWholeKeyword(value);
        if (SvgSpecificListRules.IsCssWide(keyword)) return new SvgFontFamilyParser().Result(true, SvgSpecificListParseError.None, keyword, families, -1);
        int position = SvgSpecificListRules.SkipWhitespace(value, 0);
        if (position == value.Length) return new SvgFontFamilyParser().Result(false, SvgSpecificListParseError.ExpectedValue, SvgListKeyword.Values, families, position);
        while (position < value.Length)
        {
            if (families.Count >= maxFamilies) return new SvgFontFamilyParser().Result(false, SvgSpecificListParseError.ItemLimitExceeded, SvgListKeyword.Values, families, position);
            int start = position;
            int end = new SvgFontFamilyParser().FindFamilyEnd(value, start);
            int trimmedEnd = end;
            while (trimmedEnd > start && SvgSpanCharacters.IsCssWhitespace(value[trimmedEnd - 1])) trimmedEnd = trimmedEnd - 1;
            SvgFontFamily? family = new SvgFontFamilyParser().ParseFamily(value, start, trimmedEnd, maxTokenCharacters);
            if (family == null) return new SvgFontFamilyParser().Result(false, SvgSpecificListParseError.InvalidFontFamily, SvgListKeyword.Values, families, start);
            if (family.Name.Length > maxTokenCharacters) return new SvgFontFamilyParser().Result(false, SvgSpecificListParseError.TokenTooLong, SvgListKeyword.Values, families, start + maxTokenCharacters);
            families.Add(family);
            position = SvgSpecificListRules.SkipWhitespace(value, end);
            if (position == value.Length) break;
            if (value[position] != ',') return new SvgFontFamilyParser().Result(false, SvgSpecificListParseError.InvalidSeparator, SvgListKeyword.Values, families, position);
            position = SvgSpecificListRules.SkipWhitespace(value, position + 1);
            if (position == value.Length) return new SvgFontFamilyParser().Result(false, SvgSpecificListParseError.InvalidSeparator, SvgListKeyword.Values, families, position);
        }
        return new SvgFontFamilyParser().Result(true, SvgSpecificListParseError.None, SvgListKeyword.Values, families, -1);
    }
SvgFontFamily? ParseFamily(string value, int start, int end, int maximum)
    {
        if (end <= start) return null;
        char quote = value[start];
        if (quote == '\'' || quote == '"')
        {
            if (end <= start + 1 || value[end - 1] != quote) return null;
            SvgSpanReader reader = new SvgSpanReader(value, new SvgFontFamilyParser().ReaderMaximum(maximum));
            reader.SetPosition(start + 1);
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            while (reader.GetPosition() < end - 1)
            {
                char current = reader.PeekCharacter().Value;
                if (current == '\\')
                {
                    SvgSpanEscapeResult escape = reader.ReadEscape();
                    if (!escape.Success || escape.End > end - 1) return null;
                    builder.Append(escape.Value);
                }
                else
                {
                    if (current == '\n' || current == '\r' || current == '\f') return null;
                    builder.Append(current);
                    reader.ReadCharacter();
                }
                if (builder.Length > maximum) return new SvgFontFamily(builder.ToString(), true, false, start, end);
            }
            return new SvgFontFamily(builder.ToString(), true, false, start, end);
        }
        SvgSpanReader tokenReader = SvgSpanReader.FromMemory(value.AsMemory(start, end - start), new SvgFontFamilyParser().ReaderMaximum(maximum));
        System.Text.StringBuilder name = new System.Text.StringBuilder();
        int identifiers = 0;
        while (!tokenReader.IsEnd())
        {
            if (identifiers > 0)
            {
                if (tokenReader.SkipCssWhitespace() == 0) return null;
                if (tokenReader.IsEnd()) break;
                name.Append(' ');
            }
            if (!new SvgFontFamilyParser().StartsIdentifier(value, start + tokenReader.GetPosition(), end)) return null;
            SvgSpanTokenResult token = tokenReader.ReadToken(IsIdentifierCharacter, true);
            if (!token.Success || token.RawText.Length == 0) return null;
            name.Append(token.GetValue());
            identifiers = identifiers + 1;
            if (name.Length > maximum) return new SvgFontFamily(name.ToString(), false, false, start, end);
        }
        if (name.Length == 0) return null;
        bool generic = identifiers == 1 && new SvgFontFamilyParser().IsGeneric(name.ToString());
        return new SvgFontFamily(name.ToString(), false, generic, start, end);
    }
bool StartsIdentifier(string value, int position, int end)
    {
        char first = value[position];
        if (new SvgFontFamilyParser().IsIdentifierInitial(first)) return true;
        if (first == '\\') return new SvgFontFamilyParser().IsValidEscape(value, position, end);
        if (first != '-' || position + 1 >= end) return false;
        char second = value[position + 1];
        return new SvgFontFamilyParser().IsIdentifierInitial(second) || second == '-' || second == '\\' && new SvgFontFamilyParser().IsValidEscape(value, position + 1, end);
    }
bool IsIdentifierInitial(char value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z' || value == '_' || value >= 128;
    }
bool IsValidEscape(string value, int position, int end)
    {
        if (position + 1 >= end) return false;
        char next = value[position + 1];
        return next != '\n' && next != '\r' && next != '\f';
    }
int ReaderMaximum(int maximum)
    {
        if (maximum > System.Int32.MaxValue / 8) return System.Int32.MaxValue;
        return maximum * 8;
    }
int FindFamilyEnd(string value, int start)
    {
        int position = start;
        char quote = '\0';
        while (position < value.Length)
        {
            char current = value[position];
            if (current == '\\')
            {
                position = SvgSpecificListRules.SkipEscape(value, position);
                continue;
            }
            if (quote != '\0')
            {
                if (current == quote) quote = '\0';
            }
            else if (current == '\'' || current == '"') quote = current;
            else if (current == ',') return position;
            position = position + 1;
        }
        return position;
    }
bool IsIdentifierCharacter(char value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z' || value >= '0' && value <= '9' || value == '-' || value == '_' || value >= 128;
    }
bool IsGeneric(string value)
    {
        string lower = value.ToLowerInvariant();
        return lower == "serif" || lower == "sans-serif" || lower == "cursive" || lower == "fantasy" || lower == "monospace" || lower == "system-ui" || lower == "ui-serif" || lower == "ui-sans-serif" || lower == "ui-monospace" || lower == "ui-rounded" || lower == "math" || lower == "emoji" || lower == "fangsong";
    }
SvgFontFamilyParseResult Result(bool success, SvgSpecificListParseError error, SvgListKeyword keyword, System.Collections.Generic.List<SvgFontFamily> families, int offset)
    {
        return new SvgFontFamilyParseResult(success, error, keyword, families, offset);
    }
}
