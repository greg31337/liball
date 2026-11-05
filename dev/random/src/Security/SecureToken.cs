using System;
using System.Collections.Generic;

namespace random;

internal static class SecureToken
{
    internal const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    internal static int HexLength(int byteCount)
    {
        Guard.Count(byteCount, "byteCount");
        return EncodedLength((long)byteCount * 2L, "byteCount");
    }

    internal static int Base32Length(int byteCount)
    {
        Guard.Count(byteCount, "byteCount");
        long length = ((long)byteCount * 8L + 4L) / 5L;
        return EncodedLength(length, "byteCount");
    }

    internal static int Base64Length(int byteCount)
    {
        Guard.Count(byteCount, "byteCount");
        long length = (((long)byteCount + 2L) / 3L) * 4L;
        return EncodedLength(length, "byteCount");
    }

    internal static int Base64UrlLength(int byteCount)
    {
        Guard.Count(byteCount, "byteCount");
        long length = ((long)byteCount * 8L + 5L) / 6L;
        return EncodedLength(length, "byteCount");
    }

    internal static long ByteEntropyBits(int byteCount)
    {
        Guard.Count(byteCount, "byteCount");
        return (long)byteCount * 8L;
    }

    internal static double StringEntropyBits(string alphabet, int length)
    {
        int alphabetLength = ValidateAlphabet(alphabet);
        Guard.Count(length, "length");
        return (double)length * Math.Log2((double)alphabetLength);
    }

    internal static int ValidateAlphabet(string alphabet)
    {
        Guard.NotNull(alphabet, "alphabet");
        if (alphabet.Length < 2)
        {
            throw RandomArgumentException.Create("alphabet", "The alphabet must contain at least two distinct characters.");
        }

        HashSet<char> characters = new HashSet<char>();
        int index = 0;
        while (index < alphabet.Length)
        {
            char character = alphabet[index];
            if (char.IsSurrogate(character))
            {
                throw RandomArgumentException.Create("alphabet", "The alphabet must not contain UTF-16 surrogate code units.");
            }

            if (!characters.Add(character))
            {
                throw RandomArgumentException.Create("alphabet", "The alphabet must not contain duplicate characters.");
            }

            index++;
        }

        return alphabet.Length;
    }

    internal static string EncodeHex(byte[] bytes)
    {
        Guard.NotNull(bytes, "bytes");
        return Convert.ToHexString(bytes);
    }

    internal static string EncodeBase32(byte[] bytes)
    {
        Guard.NotNull(bytes, "bytes");
        char[] result = new char[Base32Length(bytes.Length)];
        int inputIndex = 0;
        int outputIndex = 0;
        int bitBuffer = 0;
        int bufferedBitCount = 0;
        while (inputIndex < bytes.Length)
        {
            bitBuffer = (bitBuffer << 8) | bytes[inputIndex];
            bufferedBitCount += 8;
            inputIndex++;
            while (bufferedBitCount >= 5)
            {
                bufferedBitCount -= 5;
                int alphabetIndex = (bitBuffer >> bufferedBitCount) & 31;
                result[outputIndex] = Base32Alphabet[alphabetIndex];
                outputIndex++;
            }
        }

        if (bufferedBitCount != 0)
        {
            int alphabetIndex = (bitBuffer << (5 - bufferedBitCount)) & 31;
            result[outputIndex] = Base32Alphabet[alphabetIndex];
        }

        string encoded = new string(result);
        Array.Fill(result, '\0');
        return encoded;
    }

    internal static string EncodeBase64(byte[] bytes)
    {
        Guard.NotNull(bytes, "bytes");
        return Convert.ToBase64String(bytes);
    }

    internal static string EncodeBase64Url(byte[] bytes)
    {
        Guard.NotNull(bytes, "bytes");
        string base64 = Convert.ToBase64String(bytes);
        int length = base64.Length;
        while (length != 0 && base64[length - 1] == '=')
        {
            length--;
        }

        char[] result = new char[length];
        int index = 0;
        while (index < length)
        {
            char character = base64[index];
            if (character == '+')
            {
                character = '-';
            }
            else if (character == '/')
            {
                character = '_';
            }

            result[index] = character;
            index++;
        }

        string encoded = new string(result);
        Array.Fill(result, '\0');
        return encoded;
    }

    internal static int EncodedLength(long length, string argumentName)
    {
        if (length > int.MaxValue)
        {
            throw RandomArgumentException.Create(argumentName, "The encoded token would exceed the maximum string length.");
        }

        return (int)length;
    }
}
