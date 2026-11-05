using System;

namespace uuid;

internal class HexTools
{
    internal const string LowerAlphabet = "0123456789abcdef";
    internal const string UpperAlphabet = "0123456789ABCDEF";

    internal static int HexValue(char character)
    {
        if (character >= '0' && character <= '9')
        {
            return character - '0';
        }

        if (character >= 'a' && character <= 'f')
        {
            return character - 'a' + 10;
        }

        if (character >= 'A' && character <= 'F')
        {
            return character - 'A' + 10;
        }

        return -1;
    }

    internal static bool IsHex(char character)
    {
        return HexValue(character) >= 0;
    }

    internal static byte[] ParseHex(string? text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if ((text.Length % 2) != 0)
        {
            throw new ArgumentException("hex text length must be even", "text");
        }

        return ParseFixedLength(text, text.Length / 2);
    }

    internal static byte[] ParseFixedLength(string? text, int byteLength)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (byteLength < 0)
        {
            throw new ArgumentOutOfRangeException("byteLength");
        }

        long expectedLength = (long)byteLength * 2;
        if (text.Length != expectedLength)
        {
            throw new ArgumentException("hex text length does not match expected byte length", "text");
        }

        byte[] bytes = new byte[byteLength];
        int byteIndex = 0;
        int textIndex = 0;
        while (byteIndex < byteLength)
        {
            int high = HexValue(text[textIndex]);
            int low = HexValue(text[textIndex + 1]);
            if (high < 0 || low < 0)
            {
                throw new ArgumentException("hex text contains an invalid character", "text");
            }

            bytes[byteIndex] = (byte)((high << 4) | low);
            byteIndex = byteIndex + 1;
            textIndex = textIndex + 2;
        }

        return bytes;
    }

    internal static byte[] ParseUuidCompact(string text)
    {
        return ParseFixedLength(text, 16);
    }

    internal static byte[] ParseObjectId(string text)
    {
        return ParseFixedLength(text, 12);
    }

    internal static string FormatLower(byte[]? bytes)
    {
        return Format(bytes, false);
    }

    internal static string FormatUpper(byte[]? bytes)
    {
        return Format(bytes, true);
    }

    internal static string FormatUuidCompactLower(byte[] bytes)
    {
        byte[] copy = ByteArrayTools.CopyLength(bytes, 16);
        return FormatLower(copy);
    }

    internal static string FormatUuidCompactUpper(byte[] bytes)
    {
        byte[] copy = ByteArrayTools.CopyLength(bytes, 16);
        return FormatUpper(copy);
    }

    internal static string FormatObjectIdLower(byte[] bytes)
    {
        byte[] copy = ByteArrayTools.CopyLength(bytes, 12);
        return FormatLower(copy);
    }

    internal static string FormatObjectIdUpper(byte[] bytes)
    {
        byte[] copy = ByteArrayTools.CopyLength(bytes, 12);
        return FormatUpper(copy);
    }

    internal static IdentifierValidationResult ValidateFixedHex(string? text, int byteLength, IdentifierKind kind)
    {
        if (byteLength < 0)
        {
            return IdentifierValidationResult.Failure(IdentifierErrorCode.InvalidLength, "expected byte length cannot be negative");
        }

        if (text == null)
        {
            return IdentifierValidationResult.Failure(IdentifierErrorCode.NullInput, "hex text is null");
        }

        long expectedLength = (long)byteLength * 2;
        if (text.Length != expectedLength)
        {
            return IdentifierValidationResult.Failure(IdentifierErrorCode.InvalidLength, "expected " + expectedLength.ToString() + " hex characters but found " + text.Length.ToString());
        }

        int index = 0;
        while (index < text.Length)
        {
            if (!IsHex(text[index]))
            {
                return IdentifierValidationResult.Failure(IdentifierErrorCode.InvalidCharacter, "invalid hex character at index " + index.ToString());
            }

            index = index + 1;
        }

        byte[] bytes = ParseFixedLength(text, byteLength);
        return IdentifierValidationResult.Success(kind, FormatLower(bytes));
    }

    internal static string Format(byte[]? bytes, bool upper)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        string alphabet = LowerAlphabet;
        if (upper)
        {
            alphabet = UpperAlphabet;
        }

        if (bytes.Length > int.MaxValue / 2)
        {
            throw new ArgumentException("byte array is too long to format as hexadecimal text", "bytes");
        }

        char[] characters = new char[bytes.Length * 2];
        int byteIndex = 0;
        int textIndex = 0;
        while (byteIndex < bytes.Length)
        {
            int value = bytes[byteIndex];
            characters[textIndex] = alphabet[value >> 4];
            characters[textIndex + 1] = alphabet[value & 15];
            byteIndex = byteIndex + 1;
            textIndex = textIndex + 2;
        }

        return new string(characters);
    }
}
