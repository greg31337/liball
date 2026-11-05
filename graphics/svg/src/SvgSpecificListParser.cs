using System;

namespace svg;

/// <summary>Provides shared SVG-specific list parsing rules.</summary>
internal static class SvgSpecificListRules
{
    /// <summary>Reads a complete recognized keyword or returns ordinary values.</summary>
    /// <param name="value">The source text.</param>
    /// <returns>The recognized keyword state.</returns>
    public static SvgListKeyword ReadWholeKeyword(string? value)
    {
        if (value == null) return SvgListKeyword.Values;
        int start = Start(value);
        int end = value.Length;
        while (end > start && SvgSpanCharacters.IsCssWhitespace(value[end - 1])) end = end - 1;
        string text = value.Substring(start, end - start);
        if (text.IndexOf('\\') >= 0)
        {
            SvgSpanReader reader = new SvgSpanReader(text, text.Length);
            SvgSpanTokenResult token = reader.ReadToken(IsKeywordCharacter, true);
            if (!token.Success || !reader.IsEnd()) return SvgListKeyword.Values;
            text = token.GetValue();
        }
        text = text.ToLowerInvariant();
        if (text == "none") return SvgListKeyword.None;
        if (text == "normal") return SvgListKeyword.Normal;
        if (text == "inherit") return SvgListKeyword.Inherit;
        if (text == "initial") return SvgListKeyword.Initial;
        if (text == "unset") return SvgListKeyword.Unset;
        if (text == "revert") return SvgListKeyword.Revert;
        if (text == "revert-layer") return SvgListKeyword.RevertLayer;
        return SvgListKeyword.Values;
    }

    static bool IsKeywordCharacter(char value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z' || value >= '0' && value <= '9' || value == '-' || value == '_';
    }

    /// <summary>Tests for a CSS-wide keyword.</summary>
    /// <param name="keyword">The keyword state.</param>
    /// <returns><c>true</c> for a CSS-wide keyword.</returns>
    public static bool IsCssWide(SvgListKeyword keyword)
    {
        return keyword == SvgListKeyword.Inherit || keyword == SvgListKeyword.Initial || keyword == SvgListKeyword.Unset || keyword == SvgListKeyword.Revert || keyword == SvgListKeyword.RevertLayer;
    }

    /// <summary>Returns canonical keyword text.</summary>
    /// <param name="keyword">The keyword.</param>
    /// <returns>The lowercase keyword or an empty string.</returns>
    public static string KeywordText(SvgListKeyword keyword)
    {
        if (keyword == SvgListKeyword.None) return "none";
        if (keyword == SvgListKeyword.Normal) return "normal";
        if (keyword == SvgListKeyword.Inherit) return "inherit";
        if (keyword == SvgListKeyword.Initial) return "initial";
        if (keyword == SvgListKeyword.Unset) return "unset";
        if (keyword == SvgListKeyword.Revert) return "revert";
        if (keyword == SvgListKeyword.RevertLayer) return "revert-layer";
        return "";
    }

    /// <summary>Returns the first non-whitespace offset.</summary>
    /// <param name="value">The source text.</param>
    /// <returns>The first non-whitespace offset.</returns>
    public static int Start(string value)
    {
        return SkipWhitespace(value, 0);
    }

    /// <summary>Skips CSS whitespace.</summary>
    /// <param name="value">The source text.</param>
    /// <param name="position">The initial offset.</param>
    /// <returns>The next non-whitespace offset.</returns>
    public static int SkipWhitespace(string value, int position)
    {
        while (position < value.Length && SvgSpanCharacters.IsCssWhitespace(value[position])) position = position + 1;
        return position;
    }

    /// <summary>Skips one CSS escape for delimiter scanning.</summary>
    /// <param name="value">The source text.</param>
    /// <param name="position">The backslash offset.</param>
    /// <returns>The offset after the escape.</returns>
    public static int SkipEscape(string value, int position)
    {
        SvgSpanReader reader = new SvgSpanReader(value, value.Length + 1);
        reader.SetPosition(position);
        SvgSpanEscapeResult escape = reader.ReadEscape();
        return escape.Success ? escape.End : position + 1;
    }
}
