using System;

namespace uuid;

internal class RandomTools
{
    internal static readonly IRandomSource SecureSource = new CryptographicRandomSource();

    internal static void FillBytes(byte[]? bytes)
    {
        FillBytes(bytes, SecureSource);
    }

    internal static void FillBytes(byte[]? bytes, IRandomSource? source)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        source.FillBytes(bytes);
    }

    internal static byte[] NewBytes(int length)
    {
        return NewBytes(length, SecureSource);
    }

    internal static byte[] NewBytes(int length, IRandomSource? source)
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException("length");
        }

        byte[] bytes = new byte[length];
        FillBytes(bytes, source);
        return bytes;
    }

    internal static int RandomIndex(int alphabetLength)
    {
        return RandomIndex(alphabetLength, SecureSource);
    }

    internal static int RandomIndex(int alphabetLength, IRandomSource? source)
    {
        int limit = UnbiasedByteLimit(alphabetLength);
        byte[] randomByte = new byte[1];

        while (true)
        {
            FillBytes(randomByte, source);
            int value = randomByte[0];
            if (value < limit)
            {
                return value % alphabetLength;
            }
        }
    }

    internal static int UnbiasedByteLimit(int alphabetLength)
    {
        ValidateAlphabetLength(alphabetLength);
        return 256 - (256 % alphabetLength);
    }

    internal static void ValidateAlphabetLength(int alphabetLength)
    {
        if (alphabetLength < 2 || alphabetLength > 256)
        {
            throw new ArgumentOutOfRangeException("alphabetLength");
        }
    }
}
