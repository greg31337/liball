using System;
namespace token;

/// <summary>
/// Provides base64url helpers.
/// </summary>
internal static class Base64Url
{
    const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";

    /// <summary>
    /// Encodes bytes as unpadded base64url text.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>A successful result containing unpadded base64url text.</returns>
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
    /// Encodes a byte span as unpadded base64url text.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>A successful result containing unpadded base64url text.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the encoded output would exceed the maximum string length.</exception>
    internal static TokenTextResult Encode(ReadOnlySpan<byte> bytes)
    {
        TokenTextResult result = new TokenTextResult();
        int completeGroups = bytes.Length / 3;
        int remainder = bytes.Length % 3;
        long requiredLength = (long)completeGroups * 4;

        if (remainder > 0)
        {
            requiredLength += remainder + 1;
        }

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

        if (remainder == 1)
        {
            int value = bytes[sourceIndex] << 16;
            encoded[destinationIndex] = Alphabet[(value >> 18) & 63];
            encoded[destinationIndex + 1] = Alphabet[(value >> 12) & 63];
        }
        else if (remainder == 2)
        {
            int value = bytes[sourceIndex] << 16;
            value |= bytes[sourceIndex + 1] << 8;
            encoded[destinationIndex] = Alphabet[(value >> 18) & 63];
            encoded[destinationIndex + 1] = Alphabet[(value >> 12) & 63];
            encoded[destinationIndex + 2] = Alphabet[(value >> 6) & 63];
        }

        result.Text = new string(encoded);
        result.Success = true;
        return result;
    }

    /// <summary>
    /// Decodes canonical unpadded base64url text.
    /// </summary>
    /// <param name="text">The unpadded base64url text to decode.</param>
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
    /// Decodes base64url text with explicit control over compatibility padding.
    /// </summary>
    /// <param name="text">The base64url text to decode.</param>
    /// <param name="allowPadding"><c>true</c> to accept valid trailing padding; otherwise, <c>false</c>.</param>
    /// <returns>A result containing decoded bytes, or failure details for invalid input.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>text</c> is null.</exception>
    internal static TokenBytesResult Decode(string text, bool allowPadding)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        return Decode(text.AsSpan(), allowPadding);
    }

    /// <summary>
    /// Decodes canonical unpadded base64url characters.
    /// </summary>
    /// <param name="text">The unpadded base64url characters to decode.</param>
    /// <returns>A result containing decoded bytes, or failure details for invalid input.</returns>
    internal static TokenBytesResult Decode(ReadOnlySpan<char> text)
    {
        return Decode(text, false);
    }

    /// <summary>
    /// Decodes base64url characters with explicit control over compatibility padding.
    /// </summary>
    /// <param name="text">The base64url characters to decode.</param>
    /// <param name="allowPadding"><c>true</c> to accept valid trailing padding; otherwise, <c>false</c>.</param>
    /// <returns>A result containing decoded bytes, or failure details for invalid input.</returns>
    internal static TokenBytesResult Decode(ReadOnlySpan<char> text, bool allowPadding)
    {
        TokenBytesResult result = new TokenBytesResult();
        int dataLength = text.Length;
        int paddingLength = 0;
        int index;

        while (dataLength > 0 && text[dataLength - 1] == '=')
        {
            dataLength -= 1;
            paddingLength += 1;
        }

        if (paddingLength > 0 && !allowPadding)
        {
            return InvalidDecodeResult("Padding is not allowed in strict base64url.", dataLength);
        }

        if (paddingLength > 2)
        {
            return InvalidDecodeResult("Base64url padding is invalid.", dataLength);
        }

        if (paddingLength > 0)
        {
            if (text.Length % 4 != 0)
            {
                return InvalidDecodeResult("Padded base64url length must be a multiple of four.", text.Length);
            }

            if (paddingLength == 1 && dataLength % 4 != 3)
            {
                return InvalidDecodeResult("Base64url padding does not match the encoded data length.", dataLength);
            }

            if (paddingLength == 2 && dataLength % 4 != 2)
            {
                return InvalidDecodeResult("Base64url padding does not match the encoded data length.", dataLength);
            }
        }

        if (dataLength % 4 == 1)
        {
            return InvalidDecodeResult("Base64url data has an invalid length.", dataLength);
        }

        index = 0;
        while (index < dataLength)
        {
            if (DecodeValue(text[index]) < 0)
            {
                return InvalidDecodeResult("Base64url data contains an invalid character.", index);
            }

            index += 1;
        }

        if (dataLength % 4 == 2 && (DecodeValue(text[dataLength - 1]) & 15) != 0)
        {
            return InvalidDecodeResult("Base64url data contains non-zero unused bits.", dataLength - 1);
        }

        if (dataLength % 4 == 3 && (DecodeValue(text[dataLength - 1]) & 3) != 0)
        {
            return InvalidDecodeResult("Base64url data contains non-zero unused bits.", dataLength - 1);
        }

        int outputLength = (dataLength / 4) * 3;
        int remainder = dataLength % 4;
        if (remainder > 0)
        {
            outputLength += remainder - 1;
        }

        byte[] decoded = new byte[outputLength];
        int sourceIndex = 0;
        int destinationIndex = 0;

        while (sourceIndex + 4 <= dataLength)
        {
            int value = DecodeValue(text[sourceIndex]) << 18;
            value |= DecodeValue(text[sourceIndex + 1]) << 12;
            value |= DecodeValue(text[sourceIndex + 2]) << 6;
            value |= DecodeValue(text[sourceIndex + 3]);

            decoded[destinationIndex] = (byte)(value >> 16);
            decoded[destinationIndex + 1] = (byte)((value >> 8) & 255);
            decoded[destinationIndex + 2] = (byte)(value & 255);
            sourceIndex += 4;
            destinationIndex += 3;
        }

        if (remainder == 2)
        {
            int value = DecodeValue(text[sourceIndex]) << 6;
            value |= DecodeValue(text[sourceIndex + 1]);
            decoded[destinationIndex] = (byte)(value >> 4);
        }
        else if (remainder == 3)
        {
            int value = DecodeValue(text[sourceIndex]) << 12;
            value |= DecodeValue(text[sourceIndex + 1]) << 6;
            value |= DecodeValue(text[sourceIndex + 2]);
            decoded[destinationIndex] = (byte)(value >> 10);
            decoded[destinationIndex + 1] = (byte)((value >> 2) & 255);
        }

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

        if (value == '-')
        {
            return 62;
        }

        if (value == '_')
        {
            return 63;
        }

        return -1;
    }

    static TokenBytesResult InvalidDecodeResult(string message, int position)
    {
        TokenBytesResult result = new TokenBytesResult();
        result.Error = new TokenError("invalid_base64url", message, position);
        return result;
    }
}
