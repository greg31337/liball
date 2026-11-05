namespace svg;

/// <summary>Provides shared lexical operations for IRI-related microsyntax.</summary>
internal static class SvgIriLexical
{
    /// <summary>Skips CSS whitespace.</summary>
    /// <param name="value">The source text.</param>
    /// <param name="position">The initial offset.</param>
    /// <returns>The first non-whitespace offset.</returns>
    public static int SkipWhitespace(string value, int position)
    {
        while (position < value.Length && SvgSpanCharacters.IsCssWhitespace(value[position])) position = position + 1;
        return position;
    }

    /// <summary>Trims trailing CSS whitespace.</summary>
    /// <param name="value">The source text.</param>
    /// <param name="minimum">The minimum returned offset.</param>
    /// <returns>The exclusive trimmed ending offset.</returns>
    public static int TrimEnd(string value, int minimum)
    {
        int end = value.Length;
        while (end > minimum && SvgSpanCharacters.IsCssWhitespace(value[end - 1])) end = end - 1;
        return end;
    }

    /// <summary>Finds the closing parenthesis matching an opening parenthesis.</summary>
    /// <param name="value">The source text.</param>
    /// <param name="open">The opening parenthesis offset.</param>
    /// <returns>The matching close offset, or <c>-1</c>.</returns>
    public static int FindMatchingClose(string value, int open)
    {
        int depth = 1;
        int position = open + 1;
        char quote = '\0';
        while (position < value.Length)
        {
            char current = value[position];
            if (current == '\\')
            {
                SvgSpanReader reader = new SvgSpanReader(value, value.Length + 1);
                reader.SetPosition(position);
                SvgSpanEscapeResult escape = reader.ReadEscape();
                if (!escape.Success) return -1;
                position = escape.End;
                continue;
            }
            if (quote != '\0')
            {
                if (current == quote) quote = '\0';
                position = position + 1;
                continue;
            }
            if (current == '\'' || current == '"') quote = current;
            else if (current == '(') depth = depth + 1;
            else if (current == ')')
            {
                depth = depth - 1;
                if (depth == 0) return position;
            }
            position = position + 1;
        }
        return -1;
    }

    /// <summary>Validates a preserved IRI slice.</summary>
    /// <param name="value">The source text.</param>
    /// <param name="start">The starting offset.</param>
    /// <param name="end">The exclusive ending offset.</param>
    /// <returns>The invalid offset, or <c>-1</c>.</returns>
    public static int ValidateIri(string value, int start, int end)
    {
        int position = start;
        bool fragment = false;
        while (position < end)
        {
            char current = value[position];
            if (current <= 32 || current == 127 || current == '<' || current == '>' || current == '"' || current == '\\' || current == '^' || current == '`' || current == '{' || current == '}') return position;
            if (current == '%')
            {
                if (position + 2 >= end || SvgSpanCharacters.HexValue(value[position + 1]) < 0 || SvgSpanCharacters.HexValue(value[position + 2]) < 0) return position;
                position = position + 3;
                continue;
            }
            if (current == '#')
            {
                if (fragment) return position;
                fragment = true;
            }
            if (char.IsHighSurrogate(current))
            {
                if (position + 1 >= end || !char.IsLowSurrogate(value[position + 1])) return position;
                position = position + 2;
                continue;
            }
            if (char.IsLowSurrogate(current)) return position;
            position = position + 1;
        }
        return -1;
    }

    /// <summary>Validates decoded CSS URL text before later resolution.</summary>
    /// <param name="value">The decoded URL.</param>
    /// <returns>The invalid offset, or <c>-1</c>.</returns>
    public static int ValidateDecodedUrl(string value)
    {
        int position = 0;
        bool fragment = false;
        while (position < value.Length)
        {
            char current = value[position];
            if (current == '\0' || current == '\n' || current == '\r' || current == '\f' || current == 127) return position;
            if (current == '%')
            {
                if (position + 2 >= value.Length || SvgSpanCharacters.HexValue(value[position + 1]) < 0 || SvgSpanCharacters.HexValue(value[position + 2]) < 0) return position;
                position = position + 3;
                continue;
            }
            if (current == '#')
            {
                if (fragment) return position;
                fragment = true;
            }
            if (char.IsHighSurrogate(current))
            {
                if (position + 1 >= value.Length || !char.IsLowSurrogate(value[position + 1])) return position;
                position = position + 2;
                continue;
            }
            if (char.IsLowSurrogate(current)) return position;
            position = position + 1;
        }
        return -1;
    }

    /// <summary>Decodes the content of a CSS URL function.</summary>
    /// <param name="content">The raw function content.</param>
    /// <param name="sourceOffset">The content source offset.</param>
    /// <param name="maximum">The maximum decoded length.</param>
    /// <returns>Decoded URL text or stable failure.</returns>
    public static SvgDecodedTextResult DecodeUrlContent(string content, int sourceOffset, int maximum)
    {
        int start = SkipWhitespace(content, 0);
        int end = TrimEnd(content, start);
        if (start == end) return new SvgDecodedTextResult(false, SvgIriParseError.ExpectedValue, "", sourceOffset + start, 0);
        char quote = content[start];
        bool quoted = quote == '\'' || quote == '"';
        int position = quoted ? start + 1 : start;
        int limit = quoted ? end - 1 : end;
        if (quoted && (end <= start + 1 || content[end - 1] != quote)) return new SvgDecodedTextResult(false, SvgIriParseError.ExpectedClose, "", sourceOffset + end, 0);
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        while (position < limit)
        {
            char current = content[position];
            if (current == '\\')
            {
                SvgSpanReader reader = new SvgSpanReader(content, content.Length + 1);
                reader.SetPosition(position);
                SvgSpanEscapeResult escape = reader.ReadEscape();
                if (!escape.Success || escape.End > limit) return new SvgDecodedTextResult(false, SvgIriParseError.InvalidEscape, "", sourceOffset + position, 0);
                builder.Append(escape.Value);
                position = escape.End;
            }
            else
            {
                if (current == '\n' || current == '\r' || current == '\f' || !quoted && (SvgSpanCharacters.IsCssWhitespace(current) || current == '\'' || current == '"' || current == '(' || current == ')')) return new SvgDecodedTextResult(false, SvgIriParseError.InvalidCharacter, "", sourceOffset + position, 0);
                builder.Append(current);
                position = position + 1;
            }
            if (builder.Length > maximum) return new SvgDecodedTextResult(false, SvgIriParseError.TokenTooLong, "", sourceOffset + position - 1, 0);
        }
        if (builder.Length == 0) return new SvgDecodedTextResult(false, SvgIriParseError.ExpectedValue, "", sourceOffset + start, 0);
        return new SvgDecodedTextResult(true, SvgIriParseError.None, builder.ToString(), -1, end);
    }

    /// <summary>Tests an RFC media type token.</summary>
    /// <param name="value">The token text.</param>
    /// <returns><c>true</c> for a nonempty token.</returns>
    public static bool IsMimeToken(string value)
    {
        if (value == null || value.Length == 0) return false;
        int index = 0;
        while (index < value.Length)
        {
            if (!IsMimeTokenCharacter(value[index])) return false;
            index = index + 1;
        }
        return true;
    }

    /// <summary>Tests one RFC media type token character.</summary>
    /// <param name="value">The character.</param>
    /// <returns><c>true</c> for an allowed token character.</returns>
    public static bool IsMimeTokenCharacter(char value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z' || value >= '0' && value <= '9' || value == '!' || value == '#' || value == '$' || value == '%' || value == '&' || value == '\'' || value == '*' || value == '+' || value == '-' || value == '.' || value == '^' || value == '_' || value == '`' || value == '|' || value == '~';
    }

    /// <summary>Reads a MIME token or quoted-string value.</summary>
    /// <param name="value">The complete source.</param>
    /// <param name="position">The value starting offset.</param>
    /// <param name="end">The exclusive source end.</param>
    /// <param name="maximum">The maximum value length.</param>
    /// <returns>A decoded parameter value.</returns>
    public static SvgDecodedTextResult ReadMimeValue(string value, int position, int end, int maximum)
    {
        if (position >= end) return new SvgDecodedTextResult(false, SvgIriParseError.InvalidMediaType, "", position, position);
        if (value[position] != '"')
        {
            int start = position;
            while (position < end && IsMimeTokenCharacter(value[position])) position = position + 1;
            if (position == start) return new SvgDecodedTextResult(false, SvgIriParseError.InvalidMediaType, "", position, position);
            if (position - start > maximum) return new SvgDecodedTextResult(false, SvgIriParseError.TokenTooLong, "", start + maximum, position);
            return new SvgDecodedTextResult(true, SvgIriParseError.None, value.Substring(start, position - start), -1, position);
        }
        int quoteStart = position;
        position = position + 1;
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        while (position < end && value[position] != '"')
        {
            char current = value[position];
            if (current < 32 && current != '\t' || current == 127) return new SvgDecodedTextResult(false, SvgIriParseError.InvalidMediaType, "", position, position);
            if (current == '\\')
            {
                position = position + 1;
                if (position >= end) return new SvgDecodedTextResult(false, SvgIriParseError.ExpectedClose, "", position, position);
                current = value[position];
            }
            builder.Append(current);
            position = position + 1;
            if (builder.Length > maximum) return new SvgDecodedTextResult(false, SvgIriParseError.TokenTooLong, "", position - 1, position);
        }
        if (position >= end || value[position] != '"') return new SvgDecodedTextResult(false, SvgIriParseError.ExpectedClose, "", quoteStart, position);
        return new SvgDecodedTextResult(true, SvgIriParseError.None, builder.ToString(), -1, position + 1);
    }

    /// <summary>Validates a simplified complete BCP 47 language tag.</summary>
    /// <param name="value">The language tag.</param>
    /// <returns>The invalid offset, or <c>-1</c>.</returns>
    public static int ValidateLanguage(string value)
    {
        int position = 0;
        int subtagStart = 0;
        int subtag = 0;
        while (position <= value.Length)
        {
            if (position == value.Length || value[position] == '-')
            {
                int length = position - subtagStart;
                if (length < 1 || length > 8) return subtagStart;
                int check = subtagStart;
                while (check < position)
                {
                    char current = value[check];
                    bool valid = current >= 'A' && current <= 'Z' || current >= 'a' && current <= 'z' || subtag > 0 && current >= '0' && current <= '9';
                    if (!valid) return check;
                    check = check + 1;
                }
                subtag = subtag + 1;
                subtagStart = position + 1;
            }
            position = position + 1;
        }
        return -1;
    }
}

/// <summary>Contains decoded text and its next source offset.</summary>
internal class SvgDecodedTextResult
{
    /// <summary>Whether decoding succeeded.</summary>
    public bool Success;
    /// <summary>The stable failure kind.</summary>
    public SvgIriParseError Error;
    /// <summary>The decoded text.</summary>
    public string Value;
    /// <summary>The exact failure offset.</summary>
    public int ErrorOffset;
    /// <summary>The next unread offset.</summary>
    public int EndOffset;

    /// <summary>Creates a decoded text result.</summary>
    /// <param name="success">Whether decoding succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="value">The decoded text.</param>
    /// <param name="errorOffset">The exact failure offset.</param>
    /// <param name="endOffset">The next unread offset.</param>
    public SvgDecodedTextResult(bool success, SvgIriParseError error, string value, int errorOffset, int endOffset)
    {
        Success = success;
        Error = error;
        Value = value;
        ErrorOffset = errorOffset;
        EndOffset = endOffset;
    }
}
