using System;
namespace token;

/// <summary>
/// Provides canonical standard base64 helpers for certificate fields and PEM bodies.
/// </summary>
internal static class Base64Codec
{
    const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

    /// <summary>
    /// Encodes bytes as canonical padded base64 text.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>A successful result containing padded base64 text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>bytes</c> is null.</exception>
    internal static TokenTextResult Encode(byte[] bytes)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        return Encode(bytes.AsSpan());
    }

    /// <summary>
    /// Encodes a byte span as canonical padded base64 text.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>A successful result containing padded base64 text.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the encoded output would exceed the maximum string length.</exception>
    internal static TokenTextResult Encode(ReadOnlySpan<byte> bytes)
    {
        TokenTextResult result = new TokenTextResult();
        long requiredLength = ((long)bytes.Length + 2) / 3 * 4;

        if (requiredLength > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException("bytes");
        }

        char[] encoded = new char[(int)requiredLength];
        int sourceIndex = 0;
        int destinationIndex = 0;

        while (sourceIndex + 3 <= bytes.Length)
        {
            int value = bytes[sourceIndex] << 16;
            value |= bytes[sourceIndex + 1] << 8;
            value |= bytes[sourceIndex + 2];

            encoded[destinationIndex] = Alphabet[(value >> 18) & 63];
            encoded[destinationIndex + 1] = Alphabet[(value >> 12) & 63];
            encoded[destinationIndex + 2] = Alphabet[(value >> 6) & 63];
            encoded[destinationIndex + 3] = Alphabet[value & 63];
            sourceIndex += 3;
            destinationIndex += 4;
        }

        if (sourceIndex < bytes.Length)
        {
            int value = bytes[sourceIndex] << 16;
            encoded[destinationIndex] = Alphabet[(value >> 18) & 63];

            if (sourceIndex + 1 < bytes.Length)
            {
                value |= bytes[sourceIndex + 1] << 8;
                encoded[destinationIndex + 1] = Alphabet[(value >> 12) & 63];
                encoded[destinationIndex + 2] = Alphabet[(value >> 6) & 63];
                encoded[destinationIndex + 3] = '=';
            }
            else
            {
                encoded[destinationIndex + 1] = Alphabet[(value >> 12) & 63];
                encoded[destinationIndex + 2] = '=';
                encoded[destinationIndex + 3] = '=';
            }
        }

        result.Text = new string(encoded);
        result.Success = true;
        return result;
    }

    /// <summary>
    /// Decodes canonical base64 text without permitting whitespace.
    /// </summary>
    /// <param name="text">The padded base64 text to decode.</param>
    /// <returns>A result containing decoded bytes, or failure details for invalid input.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>text</c> is null.</exception>
    internal static TokenBytesResult Decode(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        return Decode(text.AsSpan(), false);
    }

    /// <summary>
    /// Decodes base64 text with explicit control over PEM whitespace tolerance.
    /// </summary>
    /// <param name="text">The padded base64 text to decode.</param>
    /// <param name="allowWhitespace"><c>true</c> to ignore spaces, tabs, carriage returns, and line feeds; otherwise, <c>false</c>.</param>
    /// <returns>A result containing decoded bytes, or failure details for invalid input.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>text</c> is null.</exception>
    internal static TokenBytesResult Decode(string text, bool allowWhitespace)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        return Decode(text.AsSpan(), allowWhitespace);
    }

    /// <summary>
    /// Decodes canonical base64 characters without permitting whitespace.
    /// </summary>
    /// <param name="text">The padded base64 characters to decode.</param>
    /// <returns>A result containing decoded bytes, or failure details for invalid input.</returns>
    internal static TokenBytesResult Decode(ReadOnlySpan<char> text)
    {
        return Decode(text, false);
    }

    /// <summary>
    /// Decodes base64 characters with explicit control over PEM whitespace tolerance.
    /// </summary>
    /// <param name="text">The padded base64 characters to decode.</param>
    /// <param name="allowWhitespace"><c>true</c> to ignore spaces, tabs, carriage returns, and line feeds; otherwise, <c>false</c>.</param>
    /// <returns>A result containing decoded bytes, or failure details for invalid input.</returns>
    internal static TokenBytesResult Decode(ReadOnlySpan<char> text, bool allowWhitespace)
    {
        int normalizedLength = 0;
        int index = 0;

        while (index < text.Length)
        {
            if (IsWhitespace(text[index]))
            {
                if (!allowWhitespace)
                {
                    return InvalidDecodeResult("Whitespace is not allowed in strict base64.", index);
                }
            }
            else
            {
                normalizedLength += 1;
            }

            index += 1;
        }

        char[] normalized = new char[normalizedLength];
        int[] positions = new int[normalizedLength];
        int normalizedIndex = 0;
        index = 0;

        while (index < text.Length)
        {
            if (!IsWhitespace(text[index]))
            {
                normalized[normalizedIndex] = text[index];
                positions[normalizedIndex] = index;
                normalizedIndex += 1;
            }

            index += 1;
        }

        int dataLength = normalizedLength;
        int paddingLength = 0;
        while (dataLength > 0 && normalized[dataLength - 1] == '=')
        {
            dataLength -= 1;
            paddingLength += 1;
        }

        if (paddingLength > 2)
        {
            return InvalidDecodeResult("Base64 padding is invalid.", positions[dataLength]);
        }

        index = 0;
        while (index < dataLength)
        {
            if (DecodeValue(normalized[index]) < 0)
            {
                return InvalidDecodeResult("Base64 data contains an invalid character.", positions[index]);
            }

            index += 1;
        }

        if (normalizedLength % 4 != 0)
        {
            return InvalidDecodeResult("Base64 length must be a multiple of four.", text.Length);
        }

        if (paddingLength == 1 && dataLength % 4 != 3)
        {
            return InvalidDecodeResult("Base64 padding does not match the encoded data length.", positions[dataLength]);
        }

        if (paddingLength == 2 && dataLength % 4 != 2)
        {
            return InvalidDecodeResult("Base64 padding does not match the encoded data length.", positions[dataLength]);
        }

        if (paddingLength == 0 && dataLength > 0 && dataLength % 4 != 0)
        {
            return InvalidDecodeResult("Base64 padding is required.", text.Length);
        }

        if (paddingLength == 2 && (DecodeValue(normalized[dataLength - 1]) & 15) != 0)
        {
            return InvalidDecodeResult("Base64 data contains non-zero unused bits.", positions[dataLength - 1]);
        }

        if (paddingLength == 1 && (DecodeValue(normalized[dataLength - 1]) & 3) != 0)
        {
            return InvalidDecodeResult("Base64 data contains non-zero unused bits.", positions[dataLength - 1]);
        }

        byte[] decoded = new byte[(normalizedLength / 4) * 3 - paddingLength];
        int sourceIndex = 0;
        int destinationIndex = 0;

        while (sourceIndex < normalizedLength)
        {
            int value = DecodeValue(normalized[sourceIndex]) << 18;
            value |= DecodeValue(normalized[sourceIndex + 1]) << 12;

            if (sourceIndex + 2 < dataLength)
            {
                value |= DecodeValue(normalized[sourceIndex + 2]) << 6;
            }

            if (sourceIndex + 3 < dataLength)
            {
                value |= DecodeValue(normalized[sourceIndex + 3]);
            }

            decoded[destinationIndex] = (byte)(value >> 16);
            if (destinationIndex + 1 < decoded.Length)
            {
                decoded[destinationIndex + 1] = (byte)((value >> 8) & 255);
            }

            if (destinationIndex + 2 < decoded.Length)
            {
                decoded[destinationIndex + 2] = (byte)(value & 255);
            }

            sourceIndex += 4;
            destinationIndex += 3;
        }

        TokenBytesResult result = new TokenBytesResult();
        result.Bytes = decoded;
        result.Success = true;
        return result;
    }

    static int DecodeValue(char value)
    {
        if (value >= 'A' && value <= 'Z')
        {
            return value - 'A';
        }

        if (value >= 'a' && value <= 'z')
        {
            return value - 'a' + 26;
        }

        if (value >= '0' && value <= '9')
        {
            return value - '0' + 52;
        }

        if (value == '+')
        {
            return 62;
        }

        if (value == '/')
        {
            return 63;
        }

        return -1;
    }

    static bool IsWhitespace(char value)
    {
        return value == ' ' || value == '\t' || value == '\r' || value == '\n';
    }

    static TokenBytesResult InvalidDecodeResult(string message, int position)
    {
        TokenBytesResult result = new TokenBytesResult();
        result.Error = new TokenError("invalid_base64", message, position);
        return result;
    }
}
