using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace token;

/// <summary>
/// Reads strict JSON while rejecting duplicate object member names.
/// </summary>
internal static class JsonReader
{
    /// <summary>
    /// Reads JSON text with a default maximum nesting depth of 64.
    /// </summary>
    /// <param name="text">The JSON text to read.</param>
    /// <returns>A result containing a parsed JSON document, or failure details for invalid input.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>text</c> is null.</exception>
    internal static JsonReadResult Read(string text)
    {
        return Read(text, 64);
    }

    /// <summary>
    /// Reads JSON text with an explicit maximum nesting depth.
    /// </summary>
    /// <param name="text">The JSON text to read.</param>
    /// <param name="maximumDepth">The maximum number of nested objects and arrays.</param>
    /// <returns>A result containing a parsed JSON document, or failure details for invalid input.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>text</c> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>maximumDepth</c> is negative.</exception>
    internal static JsonReadResult Read(string text, int maximumDepth)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (maximumDepth < 0)
        {
            throw new ArgumentOutOfRangeException("maximumDepth");
        }

        JsonValidationState state = new JsonValidationState(text, maximumDepth);
        SkipWhitespace(state);
        if (!ReadValue(state, 0))
        {
            return Failure(state.Error);
        }

        SkipWhitespace(state);
        if (state.Position != text.Length)
        {
            return InvalidJson("Unexpected data follows the JSON value.", state.Position);
        }

        JsonDocumentOptions options = new JsonDocumentOptions();
        options.MaxDepth = maximumDepth == 0 ? 1 : maximumDepth;
        JsonReadResult result = new JsonReadResult();
        result.Document = JsonDocument.Parse(text, options);
        result.Success = true;
        return result;
    }

    /// <summary>
    /// Reads strict UTF-8 JSON bytes with a default maximum nesting depth of 64.
    /// </summary>
    /// <param name="utf8">The UTF-8 JSON bytes to read.</param>
    /// <returns>A result containing a parsed JSON document, or failure details for invalid input.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>utf8</c> is null.</exception>
    internal static JsonReadResult Read(byte[] utf8)
    {
        return Read(utf8, 64);
    }

    /// <summary>
    /// Reads strict UTF-8 JSON bytes with an explicit maximum nesting depth.
    /// </summary>
    /// <param name="utf8">The UTF-8 JSON bytes to read.</param>
    /// <param name="maximumDepth">The maximum number of nested objects and arrays.</param>
    /// <returns>A result containing a parsed JSON document, or failure details for invalid input.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>utf8</c> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>maximumDepth</c> is negative.</exception>
    internal static JsonReadResult Read(byte[] utf8, int maximumDepth)
    {
        if (utf8 == null)
        {
            throw new ArgumentNullException("utf8");
        }

        if (maximumDepth < 0)
        {
            throw new ArgumentOutOfRangeException("maximumDepth");
        }

        TokenTextResult decoded = Utf8Codec.Decode(utf8);
        if (!decoded.Success)
        {
            return Failure(decoded.Error);
        }

        return Read((decoded.Text ?? throw new InvalidOperationException("A required value is null.")), maximumDepth);
    }

    static bool ReadValue(JsonValidationState state, int depth)
    {
        if (state.Position >= state.Text.Length)
        {
            SetInvalidJson(state, "A JSON value is required.", state.Position);
            return false;
        }

        char value = state.Text[state.Position];
        if (value == '{')
        {
            return ReadObject(state, depth + 1);
        }

        if (value == '[')
        {
            return ReadArray(state, depth + 1);
        }

        if (value == '"')
        {
            return ReadString(state, false).Success;
        }

        if (value == 't')
        {
            return ReadLiteral(state, "true");
        }

        if (value == 'f')
        {
            return ReadLiteral(state, "false");
        }

        if (value == 'n')
        {
            return ReadLiteral(state, "null");
        }

        if (value == '-' || (value >= '0' && value <= '9'))
        {
            return ReadNumber(state);
        }

        SetInvalidJson(state, "The JSON value begins with an invalid character.", state.Position);
        return false;
    }

    static bool ReadObject(JsonValidationState state, int depth)
    {
        if (!CheckDepth(state, depth))
        {
            return false;
        }

        state.Position += 1;
        SkipWhitespace(state);
        if (state.Position < state.Text.Length && state.Text[state.Position] == '}')
        {
            state.Position += 1;
            return true;
        }

        HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
        while (state.Position < state.Text.Length)
        {
            if (state.Text[state.Position] != '"')
            {
                SetInvalidJson(state, "A JSON object member name must be a string.", state.Position);
                return false;
            }

            JsonStringResult name = ReadString(state, true);
            if (!name.Success)
            {
                return false;
            }

            if (!names.Add((name.Value ?? throw new InvalidOperationException("A required value is null."))))
            {
                state.Error = new TokenError(
                    "duplicate_json_member",
                    "A JSON object contains a duplicate member name.",
                    name.Start);
                return false;
            }

            SkipWhitespace(state);
            if (state.Position >= state.Text.Length || state.Text[state.Position] != ':')
            {
                SetInvalidJson(state, "A colon is required after a JSON object member name.", state.Position);
                return false;
            }

            state.Position += 1;
            SkipWhitespace(state);
            if (!ReadValue(state, depth))
            {
                return false;
            }

            SkipWhitespace(state);
            if (state.Position >= state.Text.Length)
            {
                SetInvalidJson(state, "The JSON object is not closed.", state.Position);
                return false;
            }

            if (state.Text[state.Position] == '}')
            {
                state.Position += 1;
                return true;
            }

            if (state.Text[state.Position] != ',')
            {
                SetInvalidJson(state, "A comma or closing brace is required in a JSON object.", state.Position);
                return false;
            }

            state.Position += 1;
            SkipWhitespace(state);
        }

        SetInvalidJson(state, "The JSON object is not closed.", state.Position);
        return false;
    }

    static bool ReadArray(JsonValidationState state, int depth)
    {
        if (!CheckDepth(state, depth))
        {
            return false;
        }

        state.Position += 1;
        SkipWhitespace(state);
        if (state.Position < state.Text.Length && state.Text[state.Position] == ']')
        {
            state.Position += 1;
            return true;
        }

        while (state.Position < state.Text.Length)
        {
            if (!ReadValue(state, depth))
            {
                return false;
            }

            SkipWhitespace(state);
            if (state.Position >= state.Text.Length)
            {
                SetInvalidJson(state, "The JSON array is not closed.", state.Position);
                return false;
            }

            if (state.Text[state.Position] == ']')
            {
                state.Position += 1;
                return true;
            }

            if (state.Text[state.Position] != ',')
            {
                SetInvalidJson(state, "A comma or closing bracket is required in a JSON array.", state.Position);
                return false;
            }

            state.Position += 1;
            SkipWhitespace(state);
        }

        SetInvalidJson(state, "The JSON array is not closed.", state.Position);
        return false;
    }

    static JsonStringResult ReadString(JsonValidationState state, bool captureValue)
    {
        JsonStringResult result = new JsonStringResult();
        result.Start = state.Position;
        StringBuilder? builder = captureValue ? new StringBuilder() : null;
        state.Position += 1;

        while (state.Position < state.Text.Length)
        {
            char value = state.Text[state.Position];
            if (value == '"')
            {
                state.Position += 1;
                result.Value = captureValue ? (builder ?? throw new InvalidOperationException("A required value is null.")).ToString() : null;
                result.Success = true;
                return result;
            }

            if (value == '\\')
            {
                if (!ReadEscape(state, builder, captureValue))
                {
                    return result;
                }

                continue;
            }

            if (value < 32)
            {
                SetInvalidJson(state, "A JSON string contains an unescaped control character.", state.Position);
                return result;
            }

            if (char.IsHighSurrogate(value))
            {
                if (state.Position + 1 >= state.Text.Length || !char.IsLowSurrogate(state.Text[state.Position + 1]))
                {
                    SetInvalidJson(state, "A JSON string contains an unpaired UTF-16 surrogate.", state.Position);
                    return result;
                }

                if (captureValue)
                {
                    (builder ?? throw new InvalidOperationException("A required value is null.")).Append(value);
                    builder.Append(state.Text[state.Position + 1]);
                }

                state.Position += 2;
                continue;
            }

            if (char.IsLowSurrogate(value))
            {
                SetInvalidJson(state, "A JSON string contains an unpaired UTF-16 surrogate.", state.Position);
                return result;
            }

            if (captureValue)
            {
                (builder ?? throw new InvalidOperationException("A required value is null.")).Append(value);
            }

            state.Position += 1;
        }

        SetInvalidJson(state, "The JSON string is not closed.", state.Position);
        return result;
    }

    static bool ReadEscape(JsonValidationState state, StringBuilder? builder, bool captureValue)
    {
        int escapePosition = state.Position;
        state.Position += 1;
        if (state.Position >= state.Text.Length)
        {
            SetInvalidJson(state, "A JSON string escape is incomplete.", escapePosition);
            return false;
        }

        char escaped = state.Text[state.Position];
        if (escaped == '"' || escaped == '\\' || escaped == '/')
        {
            if (captureValue)
            {
                (builder ?? throw new InvalidOperationException("A required value is null.")).Append(escaped);
            }

            state.Position += 1;
            return true;
        }

        if (escaped == 'b' || escaped == 'f' || escaped == 'n' || escaped == 'r' || escaped == 't')
        {
            if (captureValue)
            {
                (builder ?? throw new InvalidOperationException("A required value is null.")).Append(DecodeSimpleEscape(escaped));
            }

            state.Position += 1;
            return true;
        }

        if (escaped != 'u')
        {
            SetInvalidJson(state, "A JSON string contains an invalid escape sequence.", state.Position);
            return false;
        }

        int codeUnit = ReadHexCodeUnit(state, state.Position + 1);
        if (codeUnit < 0)
        {
            return false;
        }

        int firstPosition = state.Position + 1;
        state.Position += 5;
        char first = (char)codeUnit;
        if (char.IsHighSurrogate(first))
        {
            if (state.Position + 5 >= state.Text.Length || state.Text[state.Position] != '\\' || state.Text[state.Position + 1] != 'u')
            {
                SetInvalidJson(state, "A JSON Unicode escape contains an unpaired high surrogate.", firstPosition);
                return false;
            }

            int lowCodeUnit = ReadHexCodeUnit(state, state.Position + 2);
            if (lowCodeUnit < 0)
            {
                return false;
            }

            char second = (char)lowCodeUnit;
            if (!char.IsLowSurrogate(second))
            {
                SetInvalidJson(state, "A JSON Unicode escape contains an invalid surrogate pair.", state.Position + 2);
                return false;
            }

            if (captureValue)
            {
                (builder ?? throw new InvalidOperationException("A required value is null.")).Append(first);
                builder.Append(second);
            }

            state.Position += 6;
            return true;
        }

        if (char.IsLowSurrogate(first))
        {
            SetInvalidJson(state, "A JSON Unicode escape contains an unpaired low surrogate.", firstPosition);
            return false;
        }

        if (captureValue)
        {
            (builder ?? throw new InvalidOperationException("A required value is null.")).Append(first);
        }

        return true;
    }

    static int ReadHexCodeUnit(JsonValidationState state, int position)
    {
        if (position + 4 > state.Text.Length)
        {
            SetInvalidJson(state, "A JSON Unicode escape is incomplete.", position);
            return -1;
        }

        int value = 0;
        int index = 0;
        while (index < 4)
        {
            int digit = HexValue(state.Text[position + index]);
            if (digit < 0)
            {
                SetInvalidJson(state, "A JSON Unicode escape contains a non-hexadecimal character.", position + index);
                return -1;
            }

            value = (value << 4) | digit;
            index += 1;
        }

        return value;
    }

    static int HexValue(char value)
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

    static char DecodeSimpleEscape(char value)
    {
        if (value == 'b')
        {
            return '\b';
        }

        if (value == 'f')
        {
            return '\f';
        }

        if (value == 'n')
        {
            return '\n';
        }

        if (value == 'r')
        {
            return '\r';
        }

        return '\t';
    }

    static bool ReadLiteral(JsonValidationState state, string literal)
    {
        if (literal.Length > state.Text.Length - state.Position)
        {
            SetInvalidJson(state, "A JSON literal is incomplete.", state.Position);
            return false;
        }

        int index = 0;
        while (index < literal.Length)
        {
            if (state.Text[state.Position + index] != literal[index])
            {
                SetInvalidJson(state, "A JSON literal is invalid.", state.Position + index);
                return false;
            }

            index += 1;
        }

        state.Position += literal.Length;
        return true;
    }

    static bool ReadNumber(JsonValidationState state)
    {
        int start = state.Position;
        if (state.Text[state.Position] == '-')
        {
            state.Position += 1;
            if (state.Position >= state.Text.Length)
            {
                SetInvalidJson(state, "A JSON number is incomplete.", start);
                return false;
            }
        }

        if (state.Text[state.Position] == '0')
        {
            state.Position += 1;
            if (state.Position < state.Text.Length && IsDigit(state.Text[state.Position]))
            {
                SetInvalidJson(state, "A JSON number must not contain a leading zero.", state.Position);
                return false;
            }
        }
        else
        {
            if (state.Text[state.Position] < '1' || state.Text[state.Position] > '9')
            {
                SetInvalidJson(state, "A JSON number requires an integer digit.", state.Position);
                return false;
            }

            while (state.Position < state.Text.Length && IsDigit(state.Text[state.Position]))
            {
                state.Position += 1;
            }
        }

        if (state.Position < state.Text.Length && state.Text[state.Position] == '.')
        {
            state.Position += 1;
            if (state.Position >= state.Text.Length || !IsDigit(state.Text[state.Position]))
            {
                SetInvalidJson(state, "A JSON number requires a digit after the decimal point.", state.Position);
                return false;
            }

            while (state.Position < state.Text.Length && IsDigit(state.Text[state.Position]))
            {
                state.Position += 1;
            }
        }

        if (state.Position < state.Text.Length && (state.Text[state.Position] == 'e' || state.Text[state.Position] == 'E'))
        {
            state.Position += 1;
            if (state.Position < state.Text.Length && (state.Text[state.Position] == '+' || state.Text[state.Position] == '-'))
            {
                state.Position += 1;
            }

            if (state.Position >= state.Text.Length || !IsDigit(state.Text[state.Position]))
            {
                SetInvalidJson(state, "A JSON number requires an exponent digit.", state.Position);
                return false;
            }

            while (state.Position < state.Text.Length && IsDigit(state.Text[state.Position]))
            {
                state.Position += 1;
            }
        }

        return true;
    }

    static bool IsDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    static bool CheckDepth(JsonValidationState state, int depth)
    {
        if (depth > state.MaximumDepth)
        {
            state.Error = new TokenError(
                "json_depth_exceeded",
                "The JSON value exceeds the configured maximum depth.",
                state.Position);
            return false;
        }

        return true;
    }

    static void SkipWhitespace(JsonValidationState state)
    {
        while (state.Position < state.Text.Length)
        {
            char value = state.Text[state.Position];
            if (value != ' ' && value != '\t' && value != '\r' && value != '\n')
            {
                return;
            }

            state.Position += 1;
        }
    }

    static void SetInvalidJson(JsonValidationState state, string message, int position)
    {
        state.Error = new TokenError("invalid_json", message, position);
    }

    static JsonReadResult InvalidJson(string message, int position)
    {
        return Failure(new TokenError("invalid_json", message, position));
    }

    static JsonReadResult Failure(TokenError? error)
    {
        JsonReadResult result = new JsonReadResult();
        result.Error = error;
        return result;
    }
}
