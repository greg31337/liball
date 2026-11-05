using System;

namespace uuid;

internal class CrockfordBase32
{
    internal const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    internal static int DecodeValue(char character)
    {
        char normalized = character;
        if (normalized >= 'a' && normalized <= 'z')
        {
            normalized = (char)(normalized - 'a' + 'A');
        }

        int index = 0;
        while (index < Alphabet.Length)
        {
            if (Alphabet[index] == normalized)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static bool IsCrockfordBase32(char character)
    {
        return DecodeValue(character) >= 0;
    }

    internal static string EncodeBytes(byte[]? bytes, int encodedLength)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        int paddingBits = ValidateEncodingShape(bytes.Length, encodedLength);
        char[] characters = new char[encodedLength];
        int charIndex = 0;
        while (charIndex < encodedLength)
        {
            int value = 0;
            int bitIndex = 0;
            while (bitIndex < 5)
            {
                long encodedBit = ((long)charIndex * 5) + bitIndex;
                long sourceBit = encodedBit - paddingBits;
                if (sourceBit >= 0)
                {
                    int byteIndex = (int)(sourceBit / 8);
                    int bitInByte = 7 - (int)(sourceBit % 8);
                    if ((bytes[byteIndex] & (1 << bitInByte)) != 0)
                    {
                        value = value | (1 << (4 - bitIndex));
                    }
                }

                bitIndex = bitIndex + 1;
            }

            characters[charIndex] = Alphabet[value];
            charIndex = charIndex + 1;
        }

        return new string(characters);
    }

    internal static byte[] DecodeBytes(string? text, int byteLength)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        int paddingBits = ValidateEncodingShape(byteLength, text.Length);
        byte[] bytes = new byte[byteLength];
        int charIndex = 0;
        while (charIndex < text.Length)
        {
            int value = DecodeValue(text[charIndex]);
            if (value < 0)
            {
                throw new ArgumentException("Crockford Base32 text contains an invalid character", "text");
            }

            int bitIndex = 0;
            while (bitIndex < 5)
            {
                long encodedBit = ((long)charIndex * 5) + bitIndex;
                long sourceBit = encodedBit - paddingBits;
                int mask = 1 << (4 - bitIndex);
                if (sourceBit < 0)
                {
                    if ((value & mask) != 0)
                    {
                        throw new ArgumentException("Crockford Base32 text overflows target byte length", "text");
                    }
                }
                else if ((value & mask) != 0)
                {
                    int byteIndex = (int)(sourceBit / 8);
                    int bitInByte = 7 - (int)(sourceBit % 8);
                    bytes[byteIndex] = (byte)(bytes[byteIndex] | (1 << bitInByte));
                }

                bitIndex = bitIndex + 1;
            }

            charIndex = charIndex + 1;
        }

        return bytes;
    }

    internal static string EncodeUlidBytes(byte[] bytes)
    {
        byte[] copy = ByteArrayTools.CopyLength(bytes, 16);
        return EncodeBytes(copy, 26);
    }

    internal static byte[] DecodeUlidBytes(string? text)
    {
        return DecodeBytes(RequireLength(text, 26), 16);
    }

    internal static string EncodeTimestamp(long unixMilliseconds)
    {
        BigEndianTools.ValidateRange(unixMilliseconds, BigEndianTools.MaxUInt48);
        byte[] bytes = new byte[6];
        BigEndianTools.WriteUInt48(bytes, 0, unixMilliseconds);
        return EncodeBytes(bytes, 10);
    }

    internal static long DecodeTimestamp(string text)
    {
        byte[] bytes = DecodeBytes(RequireLength(text, 10), 6);
        return BigEndianTools.ReadUInt48(bytes, 0);
    }

    internal static string EncodeRandomness(byte[] bytes)
    {
        byte[] copy = ByteArrayTools.CopyLength(bytes, 10);
        return EncodeBytes(copy, 16);
    }

    internal static byte[] DecodeRandomness(string text)
    {
        return DecodeBytes(RequireLength(text, 16), 10);
    }

    internal static IdentifierValidationResult ValidateUlidText(string? text)
    {
        if (text == null)
        {
            return IdentifierValidationResult.Failure(IdentifierErrorCode.NullInput, "ULID text is null");
        }

        if (text.Length != 26)
        {
            return IdentifierValidationResult.Failure(IdentifierErrorCode.InvalidLength, "expected 26 Crockford Base32 characters but found " + text.Length.ToString());
        }

        int index = 0;
        while (index < text.Length)
        {
            int value = DecodeValue(text[index]);
            if (value < 0)
            {
                return IdentifierValidationResult.Failure(IdentifierErrorCode.InvalidCharacter, "invalid Crockford Base32 character at index " + index.ToString());
            }

            if (index == 0 && value > 7)
            {
                return IdentifierValidationResult.Failure(IdentifierErrorCode.ValueOverflow, "ULID text overflows 128 bits");
            }

            index = index + 1;
        }

        byte[] bytes = DecodeUlidBytes(text);
        return IdentifierValidationResult.Success(IdentifierKind.Ulid, EncodeUlidBytes(bytes));
    }

    internal static int ValidateEncodingShape(int byteLength, int encodedLength)
    {
        if (byteLength < 0)
        {
            throw new ArgumentOutOfRangeException("byteLength");
        }

        if (encodedLength < 0)
        {
            throw new ArgumentOutOfRangeException("encodedLength");
        }

        long paddingBits = ((long)encodedLength * 5) - ((long)byteLength * 8);
        if (paddingBits < 0 || paddingBits > 4)
        {
            throw new ArgumentException("encoded length cannot represent byte length", "encodedLength");
        }

        return (int)paddingBits;
    }

    internal static string RequireLength(string? text, int length)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (text.Length != length)
        {
            throw new ArgumentException("Crockford Base32 text length does not match expected length", "text");
        }

        return text;
    }
}
