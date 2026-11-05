using System;
namespace token;

/// <summary>
/// Provides hexadecimal encoding and decoding helpers.
/// </summary>
internal static class HexCodec
{
    const string LowercaseAlphabet = "0123456789abcdef";

    /// <summary>
    /// Encodes bytes as lowercase hexadecimal text.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>A successful result containing lowercase hexadecimal text.</returns>
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
    /// Encodes a byte span as lowercase hexadecimal text.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>A successful result containing lowercase hexadecimal text.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the encoded output would exceed the maximum string length.</exception>
    internal static TokenTextResult Encode(ReadOnlySpan<byte> bytes)
    {
        TokenTextResult result = new TokenTextResult();
        long requiredLength = (long)bytes.Length * 2;

        if (requiredLength > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException("bytes");
        }

        char[] encoded = new char[(int)requiredLength];
        int index = 0;

        while (index < bytes.Length)
        {
            byte value = bytes[index];
            encoded[index * 2] = LowercaseAlphabet[value >> 4];
            encoded[index * 2 + 1] = LowercaseAlphabet[value & 15];
            index += 1;
        }

        result.Text = new string(encoded);
        result.Success = true;
        return result;
    }

    /// <summary>
    /// Decodes strict hexadecimal text using either letter case.
    /// </summary>
    /// <param name="text">The hexadecimal text to decode.</param>
    /// <returns>A result containing decoded bytes, or failure details for invalid input.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>text</c> is null.</exception>
    internal static TokenBytesResult Decode(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        return Decode(text.AsSpan());
    }

    /// <summary>
    /// Decodes a strict hexadecimal character span using either letter case.
    /// </summary>
    /// <param name="text">The hexadecimal characters to decode.</param>
    /// <returns>A result containing decoded bytes, or failure details for invalid input.</returns>
    internal static TokenBytesResult Decode(ReadOnlySpan<char> text)
    {
        TokenBytesResult result = new TokenBytesResult();

        if (text.Length % 2 != 0)
        {
            result.Error = new TokenError(
                "invalid_hex",
                "Hexadecimal text must contain an even number of characters.",
                text.Length);
            return result;
        }

        byte[] decoded = new byte[text.Length / 2];
        int sourceIndex = 0;

        while (sourceIndex < text.Length)
        {
            int high = DecodeValue(text[sourceIndex]);
            if (high < 0)
            {
                result.Error = new TokenError(
                    "invalid_hex",
                    "Hexadecimal text contains an invalid character.",
                    sourceIndex);
                return result;
            }

            int low = DecodeValue(text[sourceIndex + 1]);
            if (low < 0)
            {
                result.Error = new TokenError(
                    "invalid_hex",
                    "Hexadecimal text contains an invalid character.",
                    sourceIndex + 1);
                return result;
            }

            decoded[sourceIndex / 2] = (byte)((high << 4) | low);
            sourceIndex += 2;
        }

        result.Bytes = decoded;
        result.Success = true;
        return result;
    }

    static int DecodeValue(char value)
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
}
