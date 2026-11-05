namespace svg;

internal class SvgComputedConversion
{
    internal bool Success;
    internal SvgComputedValue? Value;

    internal SvgComputedConversion(bool success, SvgComputedValue? value)
    {
        Success = success;
        Value = value;
    }
}

internal static class SvgComputedCursorParser
{
    internal static SvgComputedCursor? Parse(string? text, int maxUrls, int maxTokenCharacters)
    {
        if (text == null || maxUrls < 1 || maxTokenCharacters < 1) return null;
        System.Collections.Generic.List<string>? parts = Split(text);
        if (parts == null || parts.Count == 0) return null;
        string keyword = parts[parts.Count - 1].Trim().ToLowerInvariant();
        if (!CursorKeyword(keyword)) return null;
        System.Collections.Generic.List<SvgIriReference> urls = new System.Collections.Generic.List<SvgIriReference>();
        int index = 0;
        while (index < parts.Count - 1)
        {
            if (urls.Count >= maxUrls) return null;
            SvgCursorUrlResult parsed = ParseUrlPart(parts[index], maxTokenCharacters);
            if (!parsed.Success) return null;
            urls.Add((parsed.Reference ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
            index = index + 1;
        }
        return new SvgComputedCursor(urls, keyword, text);
    }

    static SvgCursorUrlResult ParseUrlPart(string part, int maxTokenCharacters)
    {
        string trimmed = part.Trim();
        int close = MatchingClose(trimmed);
        if (close < 0) return new SvgCursorUrlResult(false, null);
        SvgIriParseResult iri = new SvgIriParser().ParseUrl(trimmed.Substring(0, close + 1), maxTokenCharacters);
        if (!iri.Success) return new SvgCursorUrlResult(false, null);
        string tail = trimmed.Substring(close + 1).Trim();
        if (tail.Length != 0)
        {
            string[]? numbers = tail.Split((char[]?)null, System.StringSplitOptions.RemoveEmptyEntries);
            if (numbers.Length != 2) return new SvgCursorUrlResult(false, null);
            SvgNumberParseResult x = SvgNumberParser.ParseCssNumber(numbers[0], maxTokenCharacters);
            SvgNumberParseResult y = SvgNumberParser.ParseCssNumber(numbers[1], maxTokenCharacters);
            if (!x.Success || !y.Success) return new SvgCursorUrlResult(false, null);
        }
        return new SvgCursorUrlResult(true, iri.Reference);
    }

    static int MatchingClose(string text)
    {
        int open = text.IndexOf('(');
        if (open < 1 || !string.Equals(text.Substring(0, open).Trim(), "url", System.StringComparison.OrdinalIgnoreCase)) return -1;
        char quote = '\0';
        int index = open + 1;
        while (index < text.Length)
        {
            char value = text[index];
            if (quote != '\0')
            {
                if (value == '\\') index = index + 1;
                else if (value == quote) quote = '\0';
            }
            else if (value == '\'' || value == '"') quote = value;
            else if (value == ')') return index;
            index = index + 1;
        }
        return -1;
    }

    static System.Collections.Generic.List<string>? Split(string text)
    {
        System.Collections.Generic.List<string> result = new System.Collections.Generic.List<string>();
        int start = 0;
        int depth = 0;
        char quote = '\0';
        int index = 0;
        while (index < text.Length)
        {
            char value = text[index];
            if (quote != '\0')
            {
                if (value == '\\') index = index + 1;
                else if (value == quote) quote = '\0';
            }
            else if (value == '\'' || value == '"') quote = value;
            else if (value == '(') depth = depth + 1;
            else if (value == ')')
            {
                depth = depth - 1;
                if (depth < 0) return null;
            }
            else if (value == ',' && depth == 0)
            {
                if (index == start) return null;
                result.Add(text.Substring(start, index - start));
                start = index + 1;
            }
            index = index + 1;
        }
        if (quote != '\0' || depth != 0 || start >= text.Length) return null;
        result.Add(text.Substring(start));
        return result;
    }

    static bool CursorKeyword(string value)
    {
        switch (value)
        {
            case "auto":
            case "default":
            case "none":
            case "context-menu":
            case "help":
            case "pointer":
            case "progress":
            case "wait":
            case "cell":
            case "crosshair":
            case "text":
            case "vertical-text":
            case "alias":
            case "copy":
            case "move":
            case "no-drop":
            case "not-allowed":
            case "grab":
            case "grabbing":
            case "e-resize":
            case "n-resize":
            case "ne-resize":
            case "nw-resize":
            case "s-resize":
            case "se-resize":
            case "sw-resize":
            case "w-resize":
            case "ew-resize":
            case "ns-resize":
            case "nesw-resize":
            case "nwse-resize":
            case "col-resize":
            case "row-resize":
            case "all-scroll":
            case "zoom-in":
            case "zoom-out":
                return true;
            default:
                return false;
        }
    }
}

internal class SvgCursorUrlResult
{
    internal bool Success;
    internal SvgIriReference? Reference;

    internal SvgCursorUrlResult(bool success, SvgIriReference? reference)
    {
        Success = success;
        Reference = reference;
    }
}
