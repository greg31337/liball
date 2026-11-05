using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace jmap;

internal class JmapJsonReader
{
    internal long MaximumDocumentBytes;
    internal int MaximumDepth;

    internal JmapJsonReader(long maximumDocumentBytes, int maximumDepth)
    {
        if (maximumDocumentBytes < 1L || maximumDocumentBytes > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException("maximumDocumentBytes");
        }

        if (maximumDepth < 1)
        {
            throw new ArgumentOutOfRangeException("maximumDepth");
        }

        MaximumDocumentBytes = maximumDocumentBytes;
        MaximumDepth = maximumDepth;
    }

    internal JsonElement ReadUtf8Span(ReadOnlySpan<byte> utf8)
    {
        if (utf8.Length > MaximumDocumentBytes)
        {
            throw new JmapProtocolException("The JSON document exceeds the configured byte limit.", null, default(JsonElement));
        }

        byte[] copy = utf8.ToArray();
        return ReadUtf8Memory(copy);
    }

    internal JsonElement ReadUtf8Memory(ReadOnlyMemory<byte> utf8)
    {
        if (utf8.Length > MaximumDocumentBytes)
        {
            throw new JmapProtocolException("The JSON document exceeds the configured byte limit.", null, default(JsonElement));
        }

        ValidateUtf8Encoding(utf8.Span);
        ValidateTokens(utf8.Span);

        JsonDocumentOptions options = new JsonDocumentOptions();
        options.AllowTrailingCommas = false;
        options.CommentHandling = JsonCommentHandling.Disallow;
        options.MaxDepth = MaximumDepth;

        using JsonDocument document = JsonDocument.Parse(utf8, options);
        return document.RootElement.Clone();
    }

    internal JsonElement ReadUtf8Stream(Stream stream, CancellationToken cancellationToken)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        byte[] buffer = new byte[8192];
        long total = 0L;

        using MemoryStream documentBytes = new MemoryStream();
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            long remaining = MaximumDocumentBytes - total;
            int requested = buffer.Length;
            if (remaining < requested)
            {
                requested = (int)remaining + 1;
            }

            int read = stream.Read(buffer, 0, requested);
            if (read == 0)
            {
                break;
            }

            total = total + read;
            if (total > MaximumDocumentBytes)
            {
                throw new JmapProtocolException("The JSON document exceeds the configured byte limit.", null, default(JsonElement));
            }

            documentBytes.Write(buffer, 0, read);
        }

        return ReadUtf8Memory(documentBytes.ToArray());
    }

    internal JsonElement CloneUnknownValue(JsonElement value)
    {
        return value.Clone();
    }

    internal void ValidateTokens(ReadOnlySpan<byte> utf8)
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.AllowTrailingCommas = false;
        options.CommentHandling = JsonCommentHandling.Disallow;
        options.MaxDepth = MaximumDepth;

        Utf8JsonReader reader = new Utf8JsonReader(utf8, options);
        List<HashSet<string>?> objectMembers = new List<HashSet<string>?>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                objectMembers.Add(new HashSet<string>(StringComparer.Ordinal));
                continue;
            }

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                objectMembers.Add(null);
                continue;
            }

            if (reader.TokenType == JsonTokenType.EndObject || reader.TokenType == JsonTokenType.EndArray)
            {
                objectMembers.RemoveAt(objectMembers.Count - 1);
                continue;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                ValidateEscapedUnicode(reader.ValueSpan);
                string? propertyName = reader.GetString();
                if (propertyName == null)
                {
                    throw new JmapProtocolException("A JSON object member name is invalid.", null, default(JsonElement));
                }

                ValidateUnicodeString(propertyName);

                HashSet<string>? names = objectMembers[objectMembers.Count - 1];
                if (names == null || !names.Add(propertyName))
                {
                    throw new JmapProtocolException("A JSON object contains a duplicate member name.", propertyName, default(JsonElement));
                }

                continue;
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                ValidateEscapedUnicode(reader.ValueSpan);
                string? value = reader.GetString();
                if (value == null)
                {
                    throw new JmapProtocolException("A JSON string value is invalid.", null, default(JsonElement));
                }

                ValidateUnicodeString(value);
                continue;
            }

            if (reader.TokenType == JsonTokenType.Number)
            {
                ValidateSafeInteger(reader.ValueSpan);
            }
        }
    }

    internal void ValidateUtf8Encoding(ReadOnlySpan<byte> utf8)
    {
        int index = 0;
        while (index < utf8.Length)
        {
            byte first = utf8[index];
            int codePoint;
            int length;

            if (first <= 127)
            {
                codePoint = first;
                length = 1;
            }
            else if (first >= 194 && first <= 223)
            {
                length = 2;
                if (index + length > utf8.Length || (utf8[index + 1] & 192) != 128)
                {
                    throw new JmapProtocolException("The JSON document is not valid UTF-8.", null, default(JsonElement));
                }

                codePoint = ((first & 31) << 6) | (utf8[index + 1] & 63);
            }
            else if (first >= 224 && first <= 239)
            {
                length = 3;
                if (index + length > utf8.Length || (utf8[index + 1] & 192) != 128 || (utf8[index + 2] & 192) != 128)
                {
                    throw new JmapProtocolException("The JSON document is not valid UTF-8.", null, default(JsonElement));
                }

                byte second = utf8[index + 1];
                if ((first == 224 && second < 160) || (first == 237 && second > 159))
                {
                    throw new JmapProtocolException("The JSON document is not valid UTF-8.", null, default(JsonElement));
                }

                codePoint = ((first & 15) << 12) | ((second & 63) << 6) | (utf8[index + 2] & 63);
            }
            else if (first >= 240 && first <= 244)
            {
                length = 4;
                if (index + length > utf8.Length || (utf8[index + 1] & 192) != 128 || (utf8[index + 2] & 192) != 128 || (utf8[index + 3] & 192) != 128)
                {
                    throw new JmapProtocolException("The JSON document is not valid UTF-8.", null, default(JsonElement));
                }

                byte second = utf8[index + 1];
                if ((first == 240 && second < 144) || (first == 244 && second > 143))
                {
                    throw new JmapProtocolException("The JSON document is not valid UTF-8.", null, default(JsonElement));
                }

                codePoint = ((first & 7) << 18) | ((second & 63) << 12) | ((utf8[index + 2] & 63) << 6) | (utf8[index + 3] & 63);
            }
            else
            {
                throw new JmapProtocolException("The JSON document is not valid UTF-8.", null, default(JsonElement));
            }

            ValidateUnicodeCodePoint(codePoint);
            index = index + length;
        }
    }

    internal void ValidateEscapedUnicode(ReadOnlySpan<byte> token)
    {
        int index = 0;
        while (index < token.Length)
        {
            if (token[index] != 92)
            {
                index = index + 1;
                continue;
            }

            if (index + 1 >= token.Length || token[index + 1] != 117)
            {
                index = index + 2;
                continue;
            }

            int codePoint = DecodeUnicodeEscape(token, index + 2);
            if (codePoint >= 55296 && codePoint <= 56319)
            {
                if (index + 12 > token.Length || token[index + 6] != 92 || token[index + 7] != 117)
                {
                    throw new JmapProtocolException("A JSON string contains an unpaired Unicode surrogate.", null, default(JsonElement));
                }

                int low = DecodeUnicodeEscape(token, index + 8);
                if (low < 56320 || low > 57343)
                {
                    throw new JmapProtocolException("A JSON string contains an unpaired Unicode surrogate.", null, default(JsonElement));
                }

                codePoint = 65536 + ((codePoint - 55296) * 1024) + (low - 56320);
                index = index + 12;
            }
            else
            {
                if (codePoint >= 56320 && codePoint <= 57343)
                {
                    throw new JmapProtocolException("A JSON string contains an unpaired Unicode surrogate.", null, default(JsonElement));
                }

                index = index + 6;
            }

            ValidateUnicodeCodePoint(codePoint);
        }
    }

    internal int DecodeUnicodeEscape(ReadOnlySpan<byte> token, int offset)
    {
        int value = 0;
        int index = offset;
        while (index < offset + 4)
        {
            byte digit = token[index];
            int decoded;
            if (digit >= 48 && digit <= 57)
            {
                decoded = digit - 48;
            }
            else if (digit >= 65 && digit <= 70)
            {
                decoded = digit - 55;
            }
            else if (digit >= 97 && digit <= 102)
            {
                decoded = digit - 87;
            }
            else
            {
                throw new JmapProtocolException("A JSON string contains an invalid Unicode escape.", null, default(JsonElement));
            }

            value = (value * 16) + decoded;
            index = index + 1;
        }

        return value;
    }

    internal void ValidateSafeInteger(ReadOnlySpan<byte> token)
    {
        int index = 0;
        if (token.Length > 0 && token[0] == 45)
        {
            index = 1;
        }

        byte[] digits = new byte[token.Length];
        int digitCount = 0;
        int fractionDigits = 0;
        bool inFraction = false;
        int exponentIndex = token.Length;

        while (index < token.Length)
        {
            byte current = token[index];
            if (current == 69 || current == 101)
            {
                exponentIndex = index + 1;
                break;
            }

            if (current == 46)
            {
                inFraction = true;
            }
            else
            {
                digits[digitCount] = current;
                digitCount = digitCount + 1;
                if (inFraction)
                {
                    fractionDigits = fractionDigits + 1;
                }
            }

            index = index + 1;
        }

        bool negativeExponent = false;
        if (exponentIndex < token.Length && (token[exponentIndex] == 43 || token[exponentIndex] == 45))
        {
            negativeExponent = token[exponentIndex] == 45;
            exponentIndex = exponentIndex + 1;
        }

        long exponent = 0L;
        long exponentLimit = token.Length + 32L;
        while (exponentIndex < token.Length)
        {
            if (exponent < exponentLimit)
            {
                exponent = (exponent * 10L) + token[exponentIndex] - 48L;
                if (exponent > exponentLimit)
                {
                    exponent = exponentLimit;
                }
            }

            exponentIndex = exponentIndex + 1;
        }

        if (negativeExponent)
        {
            exponent = 0L - exponent;
        }

        int firstSignificant = 0;
        while (firstSignificant < digitCount && digits[firstSignificant] == 48)
        {
            firstSignificant = firstSignificant + 1;
        }

        if (firstSignificant == digitCount)
        {
            return;
        }

        long scale = exponent - fractionDigits;
        int significantCount = digitCount - firstSignificant;
        int retainedCount = significantCount;

        if (scale < 0L)
        {
            long discardedCount = 0L - scale;
            if (discardedCount > significantCount)
            {
                return;
            }

            int discardedIndex = digitCount - (int)discardedCount;
            while (discardedIndex < digitCount)
            {
                if (digits[discardedIndex] != 48)
                {
                    return;
                }

                discardedIndex = discardedIndex + 1;
            }

            retainedCount = significantCount - (int)discardedCount;
            scale = 0L;
        }

        long integerDigits = retainedCount + scale;
        if (integerDigits < 16L)
        {
            return;
        }

        if (integerDigits > 16L)
        {
            throw new JmapProtocolException("A JSON integer is outside the interoperable safe range.", null, default(JsonElement));
        }

        byte[] maximum = new byte[] { 57, 48, 48, 55, 49, 57, 57, 50, 53, 52, 55, 52, 48, 57, 57, 49 };
        int comparedIndex = 0;
        while (comparedIndex < maximum.Length)
        {
            byte actual = 48;
            if (comparedIndex < retainedCount)
            {
                actual = digits[firstSignificant + comparedIndex];
            }

            byte allowed = maximum[comparedIndex];
            if (actual < allowed)
            {
                return;
            }

            if (actual > allowed)
            {
                throw new JmapProtocolException("A JSON integer is outside the interoperable safe range.", null, default(JsonElement));
            }

            comparedIndex = comparedIndex + 1;
        }
    }

    internal void ValidateUnicodeString(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            int codePoint = value[index];
            if (codePoint >= 55296 && codePoint <= 56319)
            {
                if (index + 1 >= value.Length)
                {
                    throw new JmapProtocolException("A JSON string contains an unpaired Unicode surrogate.", null, default(JsonElement));
                }

                int low = value[index + 1];
                if (low < 56320 || low > 57343)
                {
                    throw new JmapProtocolException("A JSON string contains an unpaired Unicode surrogate.", null, default(JsonElement));
                }

                codePoint = 65536 + ((codePoint - 55296) * 1024) + (low - 56320);
                index = index + 2;
            }
            else
            {
                if (codePoint >= 56320 && codePoint <= 57343)
                {
                    throw new JmapProtocolException("A JSON string contains an unpaired Unicode surrogate.", null, default(JsonElement));
                }

                index = index + 1;
            }

            ValidateUnicodeCodePoint(codePoint);
        }
    }

    internal void ValidateUnicodeCodePoint(int codePoint)
    {
        if ((codePoint >= 64976 && codePoint <= 65007) || (codePoint & 65535) == 65534 || (codePoint & 65535) == 65535)
        {
            throw new JmapProtocolException("A JSON string contains a Unicode noncharacter.", null, default(JsonElement));
        }
    }
}
