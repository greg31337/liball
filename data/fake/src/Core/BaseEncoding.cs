using System;

namespace fake;

internal static class BaseEncoding
{
    const string Base16Alphabet = "0123456789ABCDEF";
    const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
    const string Base64Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
    const string Base58Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

    internal static int Base16Length(int byteCount, int maximumOutputSize, string parameterName)
    {
        Guard.NonNegative(byteCount, parameterName);
        return Guard.OutputSize((long)byteCount * 2L, maximumOutputSize, parameterName);
    }

    internal static int Base32Length(int byteCount, int maximumOutputSize, string parameterName)
    {
        Guard.NonNegative(byteCount, parameterName);
        long length = ((long)byteCount + 4L) / 5L * 8L;
        return Guard.OutputSize(length, maximumOutputSize, parameterName);
    }

    internal static int Base64Length(int byteCount, int maximumOutputSize, string parameterName)
    {
        Guard.NonNegative(byteCount, parameterName);
        long length = ((long)byteCount + 2L) / 3L * 4L;
        return Guard.OutputSize(length, maximumOutputSize, parameterName);
    }

    internal static string Base16(ReadOnlySpan<byte> source, int maximumOutputSize)
    {
        int length = Base16Length(source.Length, maximumOutputSize, "source");
        char[] result = new char[length];
        WriteBase16(source, new Span<char>(result), maximumOutputSize);
        return new string(result);
    }

    internal static string Base32(ReadOnlySpan<byte> source, int maximumOutputSize)
    {
        int length = Base32Length(source.Length, maximumOutputSize, "source");
        char[] result = new char[length];
        WriteBase32(source, new Span<char>(result), maximumOutputSize);
        return new string(result);
    }

    internal static string Base64(ReadOnlySpan<byte> source, int maximumOutputSize)
    {
        int length = Base64Length(source.Length, maximumOutputSize, "source");
        char[] result = new char[length];
        WriteBase64(source, new Span<char>(result), maximumOutputSize);
        return new string(result);
    }

    internal static int WriteBase16(ReadOnlySpan<byte> source, Span<char> destination, int maximumOutputSize)
    {
        int length = Base16Length(source.Length, maximumOutputSize, "source");
        RequireDestination(destination, length);
        int sourceIndex = 0;
        int destinationIndex = 0;
        while (sourceIndex < source.Length)
        {
            byte value = source[sourceIndex];
            destination[destinationIndex] = Base16Alphabet[value >> 4];
            destination[destinationIndex + 1] = Base16Alphabet[value & 15];
            sourceIndex = sourceIndex + 1;
            destinationIndex = destinationIndex + 2;
        }
        return length;
    }

    internal static int WriteBase32(ReadOnlySpan<byte> source, Span<char> destination, int maximumOutputSize)
    {
        int length = Base32Length(source.Length, maximumOutputSize, "source");
        RequireDestination(destination, length);
        int sourceIndex = 0;
        int destinationIndex = 0;
        int accumulator = 0;
        int availableBits = 0;
        while (sourceIndex < source.Length)
        {
            accumulator = (accumulator << 8) | source[sourceIndex];
            availableBits = availableBits + 8;
            sourceIndex = sourceIndex + 1;
            while (availableBits >= 5)
            {
                availableBits = availableBits - 5;
                destination[destinationIndex] = Base32Alphabet[(accumulator >> availableBits) & 31];
                destinationIndex = destinationIndex + 1;
            }
        }
        if (availableBits > 0)
        {
            destination[destinationIndex] = Base32Alphabet[(accumulator << (5 - availableBits)) & 31];
            destinationIndex = destinationIndex + 1;
        }
        while (destinationIndex < length)
        {
            destination[destinationIndex] = '=';
            destinationIndex = destinationIndex + 1;
        }
        return length;
    }

    internal static int WriteBase64(ReadOnlySpan<byte> source, Span<char> destination, int maximumOutputSize)
    {
        int length = Base64Length(source.Length, maximumOutputSize, "source");
        RequireDestination(destination, length);
        int sourceIndex = 0;
        int destinationIndex = 0;
        while (sourceIndex + 2 < source.Length)
        {
            int first = source[sourceIndex];
            int second = source[sourceIndex + 1];
            int third = source[sourceIndex + 2];
            destination[destinationIndex] = Base64Alphabet[first >> 2];
            destination[destinationIndex + 1] = Base64Alphabet[((first & 3) << 4) | (second >> 4)];
            destination[destinationIndex + 2] = Base64Alphabet[((second & 15) << 2) | (third >> 6)];
            destination[destinationIndex + 3] = Base64Alphabet[third & 63];
            sourceIndex = sourceIndex + 3;
            destinationIndex = destinationIndex + 4;
        }
        int remaining = source.Length - sourceIndex;
        if (remaining == 1)
        {
            int first = source[sourceIndex];
            destination[destinationIndex] = Base64Alphabet[first >> 2];
            destination[destinationIndex + 1] = Base64Alphabet[(first & 3) << 4];
            destination[destinationIndex + 2] = '=';
            destination[destinationIndex + 3] = '=';
        }
        else if (remaining == 2)
        {
            int first = source[sourceIndex];
            int second = source[sourceIndex + 1];
            destination[destinationIndex] = Base64Alphabet[first >> 2];
            destination[destinationIndex + 1] = Base64Alphabet[((first & 3) << 4) | (second >> 4)];
            destination[destinationIndex + 2] = Base64Alphabet[(second & 15) << 2];
            destination[destinationIndex + 3] = '=';
        }
        return length;
    }

    internal static string GenerateBase58(FakeRandom random, int count, int maximumOutputSize)
    {
        int length = Guard.OutputSize(count, maximumOutputSize, "count");
        char[] result = new char[length];
        WriteBase58(random, new Span<char>(result), maximumOutputSize);
        return new string(result);
    }

    internal static int WriteBase58(FakeRandom random, Span<char> destination, int maximumOutputSize)
    {
        Guard.OutputSize(destination.Length, maximumOutputSize, "destination");
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = Base58Alphabet[random.IntFromMaximumExclusive(58)];
            index = index + 1;
        }
        return destination.Length;
    }

    static void RequireDestination(Span<char> destination, int requiredLength)
    {
        if (destination.Length < requiredLength)
        {
            throw new ArgumentException($"destination must contain at least {requiredLength} characters.", "destination");
        }
    }
}
