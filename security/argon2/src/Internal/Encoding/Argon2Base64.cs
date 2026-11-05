using System;

namespace argon2;

internal static class Argon2Base64
{
    const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

    internal static int GetEncodedLength(int byteLength)
    {
        if (byteLength < 0)
        {
            throw new Argon2ParameterException("PHC base64 byte length must not be negative.");
        }

        long encodedLength = (long)(byteLength / 3) * 4L;
        int remainder = byteLength % 3;
        if (remainder != 0)
        {
            encodedLength = encodedLength + remainder + 1L;
        }

        return Argon2ParameterValidator.GetManagedArrayLength(encodedLength);
    }

    internal static int GetDecodedLength(int encodedLength)
    {
        if (encodedLength < 0)
        {
            throw new Argon2ParameterException("PHC base64 character length must not be negative.");
        }

        int decodedLength = GetDecodedLengthWithoutException(encodedLength);
        if (decodedLength < 0)
        {
            throw new Argon2FormatException("PHC base64 has an invalid encoded length.");
        }

        return decodedLength;
    }

    internal static int GetDecodedLengthWithoutException(int encodedLength)
    {
        if (encodedLength < 0 || encodedLength % 4 == 1)
        {
            return -1;
        }

        int decodedLength = (encodedLength / 4) * 3;
        int remainder = encodedLength % 4;
        if (remainder != 0)
        {
            decodedLength = decodedLength + remainder - 1;
        }

        return decodedLength;
    }

    internal static void Encode(
        ReadOnlySpan<byte> source,
        Span<char> destination)
    {
        int encodedLength = GetEncodedLength(source.Length);
        if (destination.Length != encodedLength)
        {
            throw new Argon2ParameterException("PHC base64 encoding destination has an incorrect length.");
        }

        int sourceIndex = 0;
        int destinationIndex = 0;
        while (sourceIndex + 3 <= source.Length)
        {
            byte first = source[sourceIndex];
            byte second = source[sourceIndex + 1];
            byte third = source[sourceIndex + 2];
            destination[destinationIndex] = Alphabet[first >> 2];
            destination[destinationIndex + 1] = Alphabet[((first & 0x03) << 4) | (second >> 4)];
            destination[destinationIndex + 2] = Alphabet[((second & 0x0F) << 2) | (third >> 6)];
            destination[destinationIndex + 3] = Alphabet[third & 0x3F];
            sourceIndex = sourceIndex + 3;
            destinationIndex = destinationIndex + 4;
        }

        int remainder = source.Length - sourceIndex;
        if (remainder == 1)
        {
            byte first = source[sourceIndex];
            destination[destinationIndex] = Alphabet[first >> 2];
            destination[destinationIndex + 1] = Alphabet[(first & 0x03) << 4];
        }
        else if (remainder == 2)
        {
            byte first = source[sourceIndex];
            byte second = source[sourceIndex + 1];
            destination[destinationIndex] = Alphabet[first >> 2];
            destination[destinationIndex + 1] = Alphabet[((first & 0x03) << 4) | (second >> 4)];
            destination[destinationIndex + 2] = Alphabet[(second & 0x0F) << 2];
        }
    }

    internal static void Decode(
        ReadOnlySpan<char> source,
        Span<byte> destination)
    {
        int decodedLength = GetDecodedLength(source.Length);
        if (destination.Length != decodedLength)
        {
            throw new Argon2ParameterException("PHC base64 decoding destination has an incorrect length.");
        }

        if (!DecodeWithoutException(source, destination))
        {
            throw new Argon2FormatException("PHC base64 input is not canonical.");
        }
    }

    internal static bool DecodeWithoutException(
        ReadOnlySpan<char> source,
        Span<byte> destination)
    {
        int decodedLength = GetDecodedLengthWithoutException(source.Length);
        if (decodedLength < 0 || destination.Length != decodedLength)
        {
            return false;
        }

        if (!IsCanonical(source))
        {
            return false;
        }

        int remainder = source.Length % 4;
        int characterIndex = 0;
        int destinationIndex = 0;
        while (characterIndex + 4 <= source.Length)
        {
            int first = GetValue(source[characterIndex]);
            int second = GetValue(source[characterIndex + 1]);
            int third = GetValue(source[characterIndex + 2]);
            int fourth = GetValue(source[characterIndex + 3]);
            destination[destinationIndex] = (byte)((first << 2) | (second >> 4));
            destination[destinationIndex + 1] = (byte)(((second & 0x0F) << 4) | (third >> 2));
            destination[destinationIndex + 2] = (byte)(((third & 0x03) << 6) | fourth);
            characterIndex = characterIndex + 4;
            destinationIndex = destinationIndex + 3;
        }

        if (remainder == 2)
        {
            int first = GetValue(source[characterIndex]);
            int second = GetValue(source[characterIndex + 1]);
            destination[destinationIndex] = (byte)((first << 2) | (second >> 4));
        }
        else if (remainder == 3)
        {
            int first = GetValue(source[characterIndex]);
            int second = GetValue(source[characterIndex + 1]);
            int third = GetValue(source[characterIndex + 2]);
            destination[destinationIndex] = (byte)((first << 2) | (second >> 4));
            destination[destinationIndex + 1] = (byte)(((second & 0x0F) << 4) | (third >> 2));
        }

        return true;
    }

    internal static bool IsCanonical(ReadOnlySpan<char> source)
    {
        if (GetDecodedLengthWithoutException(source.Length) < 0)
        {
            return false;
        }

        int characterIndex = 0;
        while (characterIndex < source.Length)
        {
            if (GetValue(source[characterIndex]) < 0)
            {
                return false;
            }

            characterIndex = characterIndex + 1;
        }

        int remainder = source.Length % 4;
        if (remainder == 2
            && (GetValue(source[source.Length - 1]) & 0x0F) != 0)
        {
            return false;
        }

        if (remainder == 3
            && (GetValue(source[source.Length - 1]) & 0x03) != 0)
        {
            return false;
        }

        return true;
    }

    static int GetValue(char character)
    {
        if (character >= 'A' && character <= 'Z')
        {
            return character - 'A';
        }

        if (character >= 'a' && character <= 'z')
        {
            return character - 'a' + 26;
        }

        if (character >= '0' && character <= '9')
        {
            return character - '0' + 52;
        }

        if (character == '+')
        {
            return 62;
        }

        if (character == '/')
        {
            return 63;
        }

        return -1;
    }
}
