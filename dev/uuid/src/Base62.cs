using System;

namespace uuid;

internal class Base62
{
    internal const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    internal static int DecodeValue(char character)
    {
        int index = 0;
        while (index < Alphabet.Length)
        {
            if (Alphabet[index] == character)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static bool IsBase62(char character)
    {
        return DecodeValue(character) >= 0;
    }

    internal static string EncodeBytes(byte[]? bytes, int encodedLength)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        if (encodedLength < 0)
        {
            throw new ArgumentOutOfRangeException("encodedLength");
        }

        byte[] working = ByteArrayTools.Copy(bytes);
        char[] characters = new char[encodedLength];
        int charIndex = encodedLength - 1;
        while (charIndex >= 0)
        {
            int remainder = DivideBy62(working);
            characters[charIndex] = Alphabet[remainder];
            charIndex = charIndex - 1;
        }

        if (HasNonZeroByte(working))
        {
            throw new ArgumentException("encoded length is too short for byte value", "encodedLength");
        }

        return new string(characters);
    }

    internal static byte[] DecodeBytes(string? text, int byteLength)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (byteLength < 0)
        {
            throw new ArgumentOutOfRangeException("byteLength");
        }

        byte[] bytes = new byte[byteLength];
        int textIndex = 0;
        while (textIndex < text.Length)
        {
            int value = DecodeValue(text[textIndex]);
            if (value < 0)
            {
                throw new ArgumentException("Base62 text contains an invalid character", "text");
            }

            int carry = value;
            int byteIndex = bytes.Length - 1;
            while (byteIndex >= 0)
            {
                int total = (bytes[byteIndex] * 62) + carry;
                bytes[byteIndex] = (byte)(total & 255);
                carry = total >> 8;
                byteIndex = byteIndex - 1;
            }

            if (carry != 0)
            {
                throw new ArgumentException("Base62 text overflows target byte length", "text");
            }

            textIndex = textIndex + 1;
        }

        return bytes;
    }

    internal static string EncodeKsuidBytes(byte[] bytes)
    {
        byte[] copy = ByteArrayTools.CopyLength(bytes, 20);
        return EncodeBytes(copy, 27);
    }

    internal static byte[] DecodeKsuidBytes(string text)
    {
        return DecodeBytes(RequireLength(text, 27), 20);
    }

    internal static IdentifierValidationResult ValidateKsuidText(string? text)
    {
        if (text == null)
        {
            return IdentifierValidationResult.Failure(IdentifierErrorCode.NullInput, "KSUID text is null");
        }

        if (text.Length != 27)
        {
            return IdentifierValidationResult.Failure(IdentifierErrorCode.InvalidLength, "expected 27 Base62 characters but found " + text.Length.ToString());
        }

        byte[] bytes = new byte[20];
        int textIndex = 0;
        while (textIndex < text.Length)
        {
            int value = DecodeValue(text[textIndex]);
            if (value < 0)
            {
                return IdentifierValidationResult.Failure(IdentifierErrorCode.InvalidCharacter, "invalid Base62 character at index " + textIndex.ToString());
            }

            int carry = value;
            int byteIndex = bytes.Length - 1;
            while (byteIndex >= 0)
            {
                int total = (bytes[byteIndex] * 62) + carry;
                bytes[byteIndex] = (byte)(total & 255);
                carry = total >> 8;
                byteIndex = byteIndex - 1;
            }

            if (carry != 0)
            {
                return IdentifierValidationResult.Failure(IdentifierErrorCode.ValueOverflow, "KSUID text overflows 160 bits");
            }

            textIndex = textIndex + 1;
        }

        return IdentifierValidationResult.Success(IdentifierKind.Ksuid, EncodeKsuidBytes(bytes));
    }

    internal static string RequireLength(string? text, int length)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (text.Length != length)
        {
            throw new ArgumentException("Base62 text length does not match expected length", "text");
        }

        return text;
    }

    internal static int DivideBy62(byte[] bytes)
    {
        int remainder = 0;
        int index = 0;
        while (index < bytes.Length)
        {
            int accumulator = (remainder << 8) + bytes[index];
            bytes[index] = (byte)(accumulator / 62);
            remainder = accumulator % 62;
            index = index + 1;
        }

        return remainder;
    }

    internal static bool HasNonZeroByte(byte[] bytes)
    {
        int index = 0;
        while (index < bytes.Length)
        {
            if (bytes[index] != 0)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
