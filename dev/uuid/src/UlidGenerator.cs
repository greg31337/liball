using System;

namespace uuid;

internal class UlidGenerator
{
    internal static UlidValue New()
    {
        return NewAt(DateTimeOffset.UtcNow);
    }

    internal static UlidValue NewAt(DateTimeOffset time)
    {
        long unixMilliseconds = UnixTimeTools.ToUnixMilliseconds(time);
        return NewAtUnixMilliseconds(unixMilliseconds);
    }

    internal static UlidValue NewAtUnixMilliseconds(long unixMilliseconds)
    {
        return NewAtUnixMilliseconds(unixMilliseconds, RandomTools.SecureSource);
    }

    internal static UlidValue NewAtUnixMilliseconds(long unixMilliseconds, IRandomSource source)
    {
        if (unixMilliseconds < 0 || unixMilliseconds > BigEndianTools.MaxUInt48)
        {
            throw new ArgumentOutOfRangeException("unixMilliseconds");
        }

        byte[] bytes = new byte[16];
        BigEndianTools.WriteUInt48(bytes, 0, unixMilliseconds);
        byte[] randomness = RandomTools.NewBytes(10, source);
        int index = 0;
        while (index < randomness.Length)
        {
            bytes[index + 6] = randomness[index];
            index = index + 1;
        }

        return new UlidValue(bytes);
    }

    internal static bool IncrementRandomness(byte[]? bytes)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        if (bytes.Length != 16)
        {
            throw new ArgumentException("ULID byte array must be 16 bytes", "bytes");
        }

        int index = 15;
        while (index >= 6)
        {
            if (bytes[index] != 255)
            {
                bytes[index] = (byte)(bytes[index] + 1);
                return true;
            }

            bytes[index] = 0;
            index = index - 1;
        }

        return false;
    }
}
