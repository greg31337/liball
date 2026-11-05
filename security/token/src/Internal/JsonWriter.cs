using System;
using System.Text;

namespace token;

/// <summary>
/// Writes deterministic JSON used by JWK thumbprint calculations.
/// </summary>
internal static class JsonWriter
{
    const string HexAlphabet = "0123456789abcdef";

    /// <summary>
    /// Writes a compact JSON object whose string members are ordered by Unicode scalar value.
    /// </summary>
    /// <param name="names">The member names.</param>
    /// <param name="values">The string values corresponding to the member names.</param>
    /// <returns>A result containing deterministic JSON, or failure details for invalid input.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>names</c> or <c>values</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the arrays have different lengths.</exception>
    internal static TokenTextResult WriteStringObject(string[] names, string[] values)
    {
        ValidateArrays(names, values);

        int[] order = new int[names.Length];
        int index = 0;
        while (index < order.Length)
        {
            if (names[index] == null)
            {
                return InvalidMember("A JSON member name must not be null.", index);
            }

            if (values[index] == null)
            {
                return InvalidMember("A JSON string value must not be null.", index);
            }

            int invalidNamePosition = FindInvalidUtf16Position(names[index]);
            if (invalidNamePosition >= 0)
            {
                return InvalidString("A JSON member name contains invalid UTF-16.", invalidNamePosition);
            }

            int invalidValuePosition = FindInvalidUtf16Position(values[index]);
            if (invalidValuePosition >= 0)
            {
                return InvalidString("A JSON string value contains invalid UTF-16.", invalidValuePosition);
            }

            order[index] = index;
            index += 1;
        }

        SortMemberIndexes(names, order);
        index = 1;
        while (index < order.Length)
        {
            if (CompareByUnicodeScalar(names[order[index - 1]], names[order[index]]) == 0)
            {
                TokenTextResult duplicateResult = new TokenTextResult();
                duplicateResult.Error = new TokenError(
                    "duplicate_json_member",
                    "A deterministic JSON object must not contain duplicate member names.",
                    order[index]);
                return duplicateResult;
            }

            index += 1;
        }

        StringBuilder builder = new StringBuilder();
        builder.Append('{');
        index = 0;
        while (index < order.Length)
        {
            if (index > 0)
            {
                builder.Append(',');
            }

            int memberIndex = order[index];
            AppendString(builder, names[memberIndex]);
            builder.Append(':');
            AppendString(builder, values[memberIndex]);
            index += 1;
        }

        builder.Append('}');
        TokenTextResult result = new TokenTextResult();
        result.Text = builder.ToString();
        result.Success = true;
        return result;
    }

    /// <summary>
    /// Writes a compact deterministic JSON object as UTF-8 bytes ready for hashing.
    /// </summary>
    /// <param name="names">The member names.</param>
    /// <param name="values">The string values corresponding to the member names.</param>
    /// <returns>A result containing deterministic UTF-8 JSON, or failure details for invalid input.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>names</c> or <c>values</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the arrays have different lengths.</exception>
    internal static TokenBytesResult WriteStringObjectUtf8(string[] names, string[] values)
    {
        TokenTextResult textResult = WriteStringObject(names, values);
        if (!textResult.Success)
        {
            TokenBytesResult failure = new TokenBytesResult();
            failure.Error = textResult.Error;
            return failure;
        }

        return Utf8Codec.Encode((textResult.Text ?? throw new InvalidOperationException("A required value is null.")));
    }

    internal static TokenTextResult WriteStringValue(string? value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        int invalidPosition = FindInvalidUtf16Position(value);
        if (invalidPosition >= 0)
        {
            return InvalidString("A JSON string value contains invalid UTF-16.", invalidPosition);
        }

        StringBuilder builder = new StringBuilder();
        AppendString(builder, value);
        TokenTextResult result = new TokenTextResult();
        result.Text = builder.ToString();
        result.Success = true;
        return result;
    }

    static void ValidateArrays(string[] names, string[] values)
    {
        if (names == null)
        {
            throw new ArgumentNullException("names");
        }

        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        if (names.Length != values.Length)
        {
            throw new ArgumentException("Member name and value arrays must have the same length.", "values");
        }
    }

    static void SortMemberIndexes(string[] names, int[] order)
    {
        int index = 1;
        while (index < order.Length)
        {
            int current = order[index];
            int insertion = index;
            while (insertion > 0 && CompareByUnicodeScalar(names[order[insertion - 1]], names[current]) > 0)
            {
                order[insertion] = order[insertion - 1];
                insertion -= 1;
            }

            order[insertion] = current;
            index += 1;
        }
    }

    static int CompareByUnicodeScalar(string left, string right)
    {
        int leftPosition = 0;
        int rightPosition = 0;
        while (leftPosition < left.Length && rightPosition < right.Length)
        {
            int leftScalar = GetScalar(left, leftPosition);
            int rightScalar = GetScalar(right, rightPosition);
            if (leftScalar < rightScalar)
            {
                return -1;
            }

            if (leftScalar > rightScalar)
            {
                return 1;
            }

            leftPosition += char.IsHighSurrogate(left[leftPosition]) ? 2 : 1;
            rightPosition += char.IsHighSurrogate(right[rightPosition]) ? 2 : 1;
        }

        if (leftPosition < left.Length)
        {
            return 1;
        }

        if (rightPosition < right.Length)
        {
            return -1;
        }

        return 0;
    }

    static int GetScalar(string text, int position)
    {
        char first = text[position];
        if (!char.IsHighSurrogate(first))
        {
            return first;
        }

        char second = text[position + 1];
        return 65536 + ((first - 55296) << 10) + second - 56320;
    }

    static int FindInvalidUtf16Position(string text)
    {
        int position = 0;
        while (position < text.Length)
        {
            char value = text[position];
            if (char.IsHighSurrogate(value))
            {
                if (position + 1 >= text.Length || !char.IsLowSurrogate(text[position + 1]))
                {
                    return position;
                }

                position += 2;
                continue;
            }

            if (char.IsLowSurrogate(value))
            {
                return position;
            }

            position += 1;
        }

        return -1;
    }

    static void AppendString(StringBuilder builder, string value)
    {
        builder.Append('"');
        int position = 0;
        while (position < value.Length)
        {
            char character = value[position];
            if (character == '"')
            {
                builder.Append("\\\"");
            }
            else if (character == '\\')
            {
                builder.Append("\\\\");
            }
            else if (character == '\b')
            {
                builder.Append("\\b");
            }
            else if (character == '\t')
            {
                builder.Append("\\t");
            }
            else if (character == '\n')
            {
                builder.Append("\\n");
            }
            else if (character == '\f')
            {
                builder.Append("\\f");
            }
            else if (character == '\r')
            {
                builder.Append("\\r");
            }
            else if (character < 32)
            {
                builder.Append("\\u00");
                builder.Append(HexAlphabet[(character >> 4) & 15]);
                builder.Append(HexAlphabet[character & 15]);
            }
            else
            {
                builder.Append(character);
            }

            position += 1;
        }

        builder.Append('"');
    }

    static TokenTextResult InvalidMember(string message, int memberIndex)
    {
        TokenTextResult result = new TokenTextResult();
        result.Error = new TokenError("invalid_json_member", message, memberIndex);
        return result;
    }

    static TokenTextResult InvalidString(string message, int position)
    {
        TokenTextResult result = new TokenTextResult();
        result.Error = new TokenError("invalid_json_string", message, position);
        return result;
    }
}
