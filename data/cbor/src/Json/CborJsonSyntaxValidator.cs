namespace cbor;

internal class CborJsonSyntaxValidator
{
    internal string Text;
    internal int MaxDepth;
    internal int Offset;
    internal string? ErrorCode;
    internal string? ErrorMessage;
    internal int ErrorOffset = -1;
    internal string? ErrorPath;

    internal CborJsonSyntaxValidator(string text, int maxDepth)
    {
        Text = text;
        MaxDepth = maxDepth;
    }

    internal bool ValidateDocument()
    {
        SkipWhitespace();
        if (Offset >= Text.Length)
        {
            SetFailure(
                "CBOR_JSON_UNEXPECTED_END",
                "One JSON value is required.",
                Offset,
                "$"
            );
            return false;
        }
        if (!ParseValue(0, "$"))
        {
            return false;
        }
        SkipWhitespace();
        if (Offset != Text.Length)
        {
            SetFailure(
                "CBOR_JSON_TRAILING_DATA",
                "Characters remain after the complete JSON value.",
                Offset,
                "$"
            );
            return false;
        }
        return true;
    }

    internal bool ParseValue(int depth, string path)
    {
        if (depth > MaxDepth)
        {
            SetFailure(
                "CBOR_JSON_MAX_NESTING_DEPTH",
                "The JSON value exceeds the configured nesting limit.",
                Offset,
                path
            );
            return false;
        }
        SkipWhitespace();
        if (Offset >= Text.Length)
        {
            SetFailure(
                "CBOR_JSON_UNEXPECTED_END",
                "The JSON value is incomplete.",
                Offset,
                path
            );
            return false;
        }
        char character = Text[Offset];
        if (character == '{')
        {
            return ParseObject(depth, path);
        }
        if (character == '[')
        {
            return ParseArray(depth, path);
        }
        if (character == '"')
        {
            return ParseString(path);
        }
        if (character == 't')
        {
            return ParseLiteral("true", path);
        }
        if (character == 'f')
        {
            return ParseLiteral("false", path);
        }
        if (character == 'n')
        {
            return ParseLiteral("null", path);
        }
        if (character == '-' || IsDigit(character))
        {
            return ParseNumber(path);
        }
        SetFailure(
            "CBOR_JSON_TOKEN_INVALID",
            "The character cannot begin a JSON value.",
            Offset,
            path
        );
        return false;
    }

    internal bool ParseObject(int depth, string path)
    {
        Offset++;
        SkipWhitespace();
        if (Offset < Text.Length && Text[Offset] == '}')
        {
            Offset++;
            return true;
        }
        int propertyIndex = 0;
        while (Offset < Text.Length)
        {
            if (Text[Offset] != '"')
            {
                SetFailure(
                    "CBOR_JSON_PROPERTY_NAME_EXPECTED",
                    "A JSON object property name is required.",
                    Offset,
                    path
                );
                return false;
            }
            if (!ParseString($"{path}.property[{propertyIndex}]"))
            {
                return false;
            }
            SkipWhitespace();
            if (Offset >= Text.Length || Text[Offset] != ':')
            {
                SetFailure(
                    "CBOR_JSON_COLON_EXPECTED",
                    "A colon must follow a JSON property name.",
                    Offset,
                    path
                );
                return false;
            }
            Offset++;
            if (!ParseValue(depth + 1, $"{path}.property[{propertyIndex}]"))
            {
                return false;
            }
            SkipWhitespace();
            if (Offset < Text.Length && Text[Offset] == '}')
            {
                Offset++;
                return true;
            }
            if (Offset >= Text.Length || Text[Offset] != ',')
            {
                SetFailure(
                    "CBOR_JSON_OBJECT_SEPARATOR_EXPECTED",
                    "A comma or closing brace is required in the JSON object.",
                    Offset,
                    path
                );
                return false;
            }
            Offset++;
            SkipWhitespace();
            propertyIndex++;
        }
        SetFailure(
            "CBOR_JSON_UNEXPECTED_END",
            "The JSON object is incomplete.",
            Offset,
            path
        );
        return false;
    }

    internal bool ParseArray(int depth, string path)
    {
        Offset++;
        SkipWhitespace();
        if (Offset < Text.Length && Text[Offset] == ']')
        {
            Offset++;
            return true;
        }
        int itemIndex = 0;
        while (Offset < Text.Length)
        {
            if (!ParseValue(depth + 1, $"{path}[{itemIndex}]"))
            {
                return false;
            }
            SkipWhitespace();
            if (Offset < Text.Length && Text[Offset] == ']')
            {
                Offset++;
                return true;
            }
            if (Offset >= Text.Length || Text[Offset] != ',')
            {
                SetFailure(
                    "CBOR_JSON_ARRAY_SEPARATOR_EXPECTED",
                    "A comma or closing bracket is required in the JSON array.",
                    Offset,
                    path
                );
                return false;
            }
            Offset++;
            SkipWhitespace();
            itemIndex++;
        }
        SetFailure(
            "CBOR_JSON_UNEXPECTED_END",
            "The JSON array is incomplete.",
            Offset,
            path
        );
        return false;
    }

    internal bool ParseString(string path)
    {
        Offset++;
        while (Offset < Text.Length)
        {
            char character = Text[Offset];
            if (character == '"')
            {
                Offset++;
                return true;
            }
            if (character < ' ')
            {
                SetFailure(
                    "CBOR_JSON_STRING_CHARACTER_INVALID",
                    "An unescaped control character is not permitted in a JSON string.",
                    Offset,
                    path
                );
                return false;
            }
            if (character == '\\')
            {
                if (!ParseEscape(path))
                {
                    return false;
                }
                continue;
            }
            if (character >= '\ud800' && character <= '\udbff')
            {
                if (Offset + 1 >= Text.Length
                    || Text[Offset + 1] < '\udc00'
                    || Text[Offset + 1] > '\udfff')
                {
                    SetFailure(
                        "CBOR_JSON_STRING_UNICODE_INVALID",
                        "A high UTF-16 surrogate must be followed by a low surrogate.",
                        Offset,
                        path
                    );
                    return false;
                }
                Offset = Offset + 2;
                continue;
            }
            if (character >= '\udc00' && character <= '\udfff')
            {
                SetFailure(
                    "CBOR_JSON_STRING_UNICODE_INVALID",
                    "A low UTF-16 surrogate must follow a high surrogate.",
                    Offset,
                    path
                );
                return false;
            }
            Offset++;
        }
        SetFailure(
            "CBOR_JSON_UNEXPECTED_END",
            "The JSON string is incomplete.",
            Offset,
            path
        );
        return false;
    }

    internal bool ParseEscape(string path)
    {
        int escapeOffset = Offset;
        Offset++;
        if (Offset >= Text.Length)
        {
            SetFailure(
                "CBOR_JSON_UNEXPECTED_END",
                "The JSON string escape is incomplete.",
                Offset,
                path
            );
            return false;
        }
        char escape = Text[Offset];
        if (escape == '"' || escape == '\\' || escape == '/'
            || escape == 'b' || escape == 'f' || escape == 'n'
            || escape == 'r' || escape == 't')
        {
            Offset++;
            return true;
        }
        if (escape != 'u')
        {
            SetFailure(
                "CBOR_JSON_STRING_ESCAPE_INVALID",
                "The JSON string escape is invalid.",
                escapeOffset,
                path
            );
            return false;
        }
        int scalar = ReadHexQuad(Offset + 1);
        if (scalar < 0)
        {
            SetFailure(
                "CBOR_JSON_STRING_ESCAPE_INVALID",
                "A Unicode escape requires four hexadecimal digits.",
                escapeOffset,
                path
            );
            return false;
        }
        Offset = Offset + 5;
        if (scalar >= 0xd800 && scalar <= 0xdbff)
        {
            if (Offset + 6 > Text.Length
                || Text[Offset] != '\\'
                || Text[Offset + 1] != 'u')
            {
                SetFailure(
                    "CBOR_JSON_STRING_UNICODE_INVALID",
                    "An escaped high surrogate must be followed by an escaped low surrogate.",
                    escapeOffset,
                    path
                );
                return false;
            }
            int low = ReadHexQuad(Offset + 2);
            if (low < 0xdc00 || low > 0xdfff)
            {
                SetFailure(
                    "CBOR_JSON_STRING_UNICODE_INVALID",
                    "An escaped high surrogate must be followed by an escaped low surrogate.",
                    escapeOffset,
                    path
                );
                return false;
            }
            Offset = Offset + 6;
            return true;
        }
        if (scalar >= 0xdc00 && scalar <= 0xdfff)
        {
            SetFailure(
                "CBOR_JSON_STRING_UNICODE_INVALID",
                "An escaped low surrogate must follow an escaped high surrogate.",
                escapeOffset,
                path
            );
            return false;
        }
        return true;
    }

    internal int ReadHexQuad(int start)
    {
        if (start + 4 > Text.Length)
        {
            return -1;
        }
        int value = 0;
        int index = 0;
        while (index < 4)
        {
            int digit = HexValue(Text[start + index]);
            if (digit < 0)
            {
                return -1;
            }
            value = value * 16 + digit;
            index++;
        }
        return value;
    }

    internal bool ParseLiteral(string literal, string path)
    {
        int index = 0;
        while (index < literal.Length)
        {
            if (Offset + index >= Text.Length || Text[Offset + index] != literal[index])
            {
                SetFailure(
                    "CBOR_JSON_LITERAL_INVALID",
                    "The JSON literal is invalid.",
                    Offset,
                    path
                );
                return false;
            }
            index++;
        }
        Offset = Offset + literal.Length;
        return true;
    }

    internal bool ParseNumber(string path)
    {
        int start = Offset;
        if (Text[Offset] == '-')
        {
            Offset++;
            if (Offset >= Text.Length)
            {
                SetFailure(
                    "CBOR_JSON_NUMBER_INVALID",
                    "A digit must follow the JSON number sign.",
                    start,
                    path
                );
                return false;
            }
        }
        if (Text[Offset] == '0')
        {
            Offset++;
            if (Offset < Text.Length && IsDigit(Text[Offset]))
            {
                SetFailure(
                    "CBOR_JSON_NUMBER_INVALID",
                    "Leading zeroes are not permitted in a JSON number.",
                    start,
                    path
                );
                return false;
            }
        }
        else if (Text[Offset] >= '1' && Text[Offset] <= '9')
        {
            while (Offset < Text.Length && IsDigit(Text[Offset]))
            {
                Offset++;
            }
        }
        else
        {
            SetFailure(
                "CBOR_JSON_NUMBER_INVALID",
                "The JSON number integer part is invalid.",
                start,
                path
            );
            return false;
        }
        if (Offset < Text.Length && Text[Offset] == '.')
        {
            Offset++;
            if (Offset >= Text.Length || !IsDigit(Text[Offset]))
            {
                SetFailure(
                    "CBOR_JSON_NUMBER_INVALID",
                    "A JSON number fraction requires at least one digit.",
                    start,
                    path
                );
                return false;
            }
            while (Offset < Text.Length && IsDigit(Text[Offset]))
            {
                Offset++;
            }
        }
        if (Offset < Text.Length && (Text[Offset] == 'e' || Text[Offset] == 'E'))
        {
            Offset++;
            if (Offset < Text.Length && (Text[Offset] == '+' || Text[Offset] == '-'))
            {
                Offset++;
            }
            if (Offset >= Text.Length || !IsDigit(Text[Offset]))
            {
                SetFailure(
                    "CBOR_JSON_NUMBER_INVALID",
                    "A JSON number exponent requires at least one digit.",
                    start,
                    path
                );
                return false;
            }
            while (Offset < Text.Length && IsDigit(Text[Offset]))
            {
                Offset++;
            }
        }
        return true;
    }

    internal void SkipWhitespace()
    {
        while (Offset < Text.Length)
        {
            char character = Text[Offset];
            if (character != ' ' && character != '\t'
                && character != '\r' && character != '\n')
            {
                return;
            }
            Offset++;
        }
    }

    internal bool IsDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    internal int HexValue(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }
        if (value >= 'a' && value <= 'f')
        {
            return value - 'a' + 10;
        }
        if (value >= 'A' && value <= 'F')
        {
            return value - 'A' + 10;
        }
        return -1;
    }

    internal void SetFailure(string code, string message, int offset, string path)
    {
        if (ErrorCode == null)
        {
            ErrorCode = code;
            ErrorMessage = message;
            ErrorOffset = offset;
            ErrorPath = path;
        }
    }
}
