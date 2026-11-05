namespace yaml;

/// <summary>
/// Parses the library's simple YAML path notation.
/// </summary>
internal static class YamlPathParser
{
    /// <summary>
    /// Parses root, dotted mapping-key, and bracketed sequence-index path syntax without throwing for malformed paths.
    /// </summary>
    /// <param name="text">The path text to parse.</param>
    /// <returns>The resulting YAML path.</returns>
    internal static YamlPath Parse(string? text)
    {
        YamlPath path = new YamlPath();
        path.Source = text == null ? "" : text;
        string source = path.Source;
        if (source == "" || source == "$")
        {
            return path;
        }

        int index = 0;
        bool afterDot = false;
        if (source[index] == '$')
        {
            index = index + 1;
            if (index < source.Length && source[index] == '.')
            {
                index = index + 1;
                afterDot = true;
            }
            else if (index < source.Length && source[index] != '[')
            {
                path.MarkInvalid("Expected dot or sequence index after root marker.");
                return path;
            }
        }

        bool expectSegment = true;
        while (index < source.Length)
        {
            char current = source[index];
            if (current == '.')
            {
                if (expectSegment)
                {
                    path.MarkInvalid("Expected mapping key before dot.");
                    return path;
                }

                expectSegment = true;
                afterDot = true;
                index = index + 1;
                continue;
            }

            if (current == '[')
            {
                if (afterDot)
                {
                    path.MarkInvalid("Expected mapping key after dot.");
                    return path;
                }

                index = ParseIndex(path, source, index);
                if (!path.Valid)
                {
                    return path;
                }

                expectSegment = false;
                afterDot = false;
                continue;
            }

            if (!expectSegment)
            {
                path.MarkInvalid("Expected dot or sequence index between path segments.");
                return path;
            }

            if (!IsKeyChar(current))
            {
                path.MarkInvalid("Unexpected character in mapping key.");
                return path;
            }

            index = ParseKey(path, source, index);
            if (!path.Valid)
            {
                return path;
            }

            expectSegment = false;
            afterDot = false;
        }

        if (expectSegment)
        {
            path.MarkInvalid("Expected path segment.");
        }

        return path;
    }

    internal static int ParseKey(YamlPath path, string source, int start)
    {
        int index = start;
        while (index < source.Length && IsKeyChar(source[index]))
        {
            index = index + 1;
        }

        if (index == start)
        {
            path.MarkInvalid("Expected mapping key.");
            return index;
        }

        path.AddKey(source.Substring(start, index - start));
        return index;
    }

    internal static int ParseIndex(YamlPath path, string source, int start)
    {
        int index = start + 1;
        if (index >= source.Length)
        {
            path.MarkInvalid("Expected sequence index.");
            return index;
        }

        if (!IsDigit(source[index]))
        {
            path.MarkInvalid("Expected sequence index digit.");
            return index;
        }

        int value = 0;
        while (index < source.Length && IsDigit(source[index]))
        {
            int digit = source[index] - '0';
            if (value > (2147483647 - digit) / 10)
            {
                path.MarkInvalid("Sequence index is too large.");
                return index;
            }

            value = value * 10 + digit;
            index = index + 1;
        }

        if (index >= source.Length || source[index] != ']')
        {
            path.MarkInvalid("Expected closing bracket for sequence index.");
            return index;
        }

        path.AddIndex(value);
        return index + 1;
    }

    internal static bool IsKeyChar(char value)
    {
        return (value >= 'a' && value <= 'z')
            || (value >= 'A' && value <= 'Z')
            || (value >= '0' && value <= '9')
            || value == '_'
            || value == '-';
    }

    internal static bool IsDigit(char value)
    {
        return value >= '0' && value <= '9';
    }
}
