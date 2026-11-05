using System;
using System.Text;

namespace token;

/// <summary>
/// Provides strict UTF-8 encoding and decoding helpers.
/// </summary>
internal static class Utf8Codec
{
    /// <summary>
    /// Encodes text as UTF-8 after rejecting unpaired UTF-16 surrogates.
    /// </summary>
    /// <param name="text">The text to encode.</param>
    /// <returns>A result containing UTF-8 bytes, or failure details for invalid text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>text</c> is null.</exception>
    internal static TokenBytesResult Encode(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        return Encode(text.AsSpan());
    }

    /// <summary>
    /// Encodes a character span as UTF-8 after rejecting unpaired UTF-16 surrogates.
    /// </summary>
    /// <param name="text">The characters to encode.</param>
    /// <returns>A result containing UTF-8 bytes, or failure details for invalid text.</returns>
    internal static TokenBytesResult Encode(ReadOnlySpan<char> text)
    {
        TokenBytesResult result = new TokenBytesResult();
        int invalidPosition = FindInvalidUtf16Position(text);

        if (invalidPosition >= 0)
        {
            result.Error = new TokenError(
                "invalid_utf16",
                "The text contains an unpaired UTF-16 surrogate.",
                invalidPosition);
            return result;
        }

        result.Bytes = new byte[Encoding.UTF8.GetByteCount(text)];
        Encoding.UTF8.GetBytes(text, result.Bytes.AsSpan());
        result.Success = true;
        return result;
    }

    /// <summary>
    /// Decodes UTF-8 bytes after rejecting malformed, overlong, surrogate, and out-of-range sequences.
    /// </summary>
    /// <param name="bytes">The UTF-8 bytes to decode.</param>
    /// <returns>A result containing decoded text, or failure details for invalid UTF-8.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>bytes</c> is null.</exception>
    internal static TokenTextResult Decode(byte[] bytes)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        return Decode(bytes.AsSpan());
    }

    /// <summary>
    /// Decodes a UTF-8 byte span after rejecting malformed, overlong, surrogate, and out-of-range sequences.
    /// </summary>
    /// <param name="bytes">The UTF-8 bytes to decode.</param>
    /// <returns>A result containing decoded text, or failure details for invalid UTF-8.</returns>
    internal static TokenTextResult Decode(ReadOnlySpan<byte> bytes)
    {
        TokenTextResult result = new TokenTextResult();
        int invalidPosition = FindInvalidUtf8Position(bytes);

        if (invalidPosition >= 0)
        {
            result.Error = new TokenError(
                "invalid_utf8",
                "The input contains an invalid UTF-8 byte sequence.",
                invalidPosition);
            return result;
        }

        result.Text = Encoding.UTF8.GetString(bytes);
        result.Success = true;
        return result;
    }

    static int FindInvalidUtf16Position(ReadOnlySpan<char> text)
    {
        int index = 0;

        while (index < text.Length)
        {
            char value = text[index];

            if (char.IsHighSurrogate(value))
            {
                if (index + 1 >= text.Length || !char.IsLowSurrogate(text[index + 1]))
                {
                    return index;
                }

                index += 2;
                continue;
            }

            if (char.IsLowSurrogate(value))
            {
                return index;
            }

            index += 1;
        }

        return -1;
    }

    static int FindInvalidUtf8Position(ReadOnlySpan<byte> bytes)
    {
        int index = 0;

        while (index < bytes.Length)
        {
            byte first = bytes[index];

            if (first <= 127)
            {
                index += 1;
                continue;
            }

            if (first >= 194 && first <= 223)
            {
                if (index + 1 >= bytes.Length)
                {
                    return index;
                }

                if (!IsContinuation(bytes[index + 1]))
                {
                    return index + 1;
                }

                index += 2;
                continue;
            }

            if (first >= 224 && first <= 239)
            {
                if (index + 1 >= bytes.Length)
                {
                    return index;
                }

                byte second = bytes[index + 1];
                if (first == 224 && (second < 160 || second > 191))
                {
                    return index + 1;
                }

                if (first == 237 && (second < 128 || second > 159))
                {
                    return index + 1;
                }

                if (first != 224 && first != 237 && !IsContinuation(second))
                {
                    return index + 1;
                }

                if (index + 2 >= bytes.Length)
                {
                    return index;
                }

                if (!IsContinuation(bytes[index + 2]))
                {
                    return index + 2;
                }

                index += 3;
                continue;
            }

            if (first >= 240 && first <= 244)
            {
                if (index + 1 >= bytes.Length)
                {
                    return index;
                }

                byte second = bytes[index + 1];
                if (first == 240 && (second < 144 || second > 191))
                {
                    return index + 1;
                }

                if (first == 244 && (second < 128 || second > 143))
                {
                    return index + 1;
                }

                if (first != 240 && first != 244 && !IsContinuation(second))
                {
                    return index + 1;
                }

                if (index + 2 >= bytes.Length)
                {
                    return index;
                }

                if (!IsContinuation(bytes[index + 2]))
                {
                    return index + 2;
                }

                if (index + 3 >= bytes.Length)
                {
                    return index;
                }

                if (!IsContinuation(bytes[index + 3]))
                {
                    return index + 3;
                }

                index += 4;
                continue;
            }

            return index;
        }

        return -1;
    }

    static bool IsContinuation(byte value)
    {
        return value >= 128 && value <= 191;
    }
}
