using System;

using System.Security.Cryptography;

namespace random;

internal static class SecureUuid
{
    internal const int ByteCount = 16;
    internal const int MaximumCounter = 4095;
    internal const long MaximumUnixMilliseconds = 281474976710655L;

    internal static Guid CreateVersion4(byte[] randomBytes)
    {
        ValidateRandomBytes(randomBytes);
        byte[] bytes = (byte[])randomBytes.Clone();
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x40);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        Guid result = new Guid(bytes, true);
        CryptographicOperations.ZeroMemory(bytes);
        return result;
    }

    internal static Guid CreateVersion7(byte[] randomBytes, long unixMilliseconds, int counter)
    {
        ValidateRandomBytes(randomBytes);
        ValidateUnixMilliseconds(unixMilliseconds);
        if (counter < 0 || counter > MaximumCounter)
        {
            throw RandomArgumentException.Create("counter", "The UUIDv7 counter must fit in 12 bits.");
        }

        byte[] bytes = (byte[])randomBytes.Clone();
        ulong timestamp = (ulong)unixMilliseconds;
        bytes[0] = (byte)((timestamp >> 40) & 0xFFUL);
        bytes[1] = (byte)((timestamp >> 32) & 0xFFUL);
        bytes[2] = (byte)((timestamp >> 24) & 0xFFUL);
        bytes[3] = (byte)((timestamp >> 16) & 0xFFUL);
        bytes[4] = (byte)((timestamp >> 8) & 0xFFUL);
        bytes[5] = (byte)(timestamp & 0xFFUL);
        bytes[6] = (byte)(0x70 | ((counter >> 8) & 0x0F));
        bytes[7] = (byte)(counter & 0xFF);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        Guid result = new Guid(bytes, true);
        CryptographicOperations.ZeroMemory(bytes);
        return result;
    }

    internal static byte[] NetworkBytes(Guid value)
    {
        return value.ToByteArray(true);
    }

    internal static int SeedCounter(byte[] randomBytes)
    {
        ValidateRandomBytes(randomBytes);
        return ((randomBytes[6] & 0x07) << 8) | randomBytes[7];
    }

    internal static void ValidateRandomBytes(byte[] randomBytes)
    {
        Guard.NotNull(randomBytes, "randomBytes");
        if (randomBytes.Length != ByteCount)
        {
            throw RandomArgumentException.Create("randomBytes", "A UUID requires exactly 16 random bytes.");
        }
    }

    internal static void ValidateUnixMilliseconds(long unixMilliseconds)
    {
        if (unixMilliseconds < 0L || unixMilliseconds > MaximumUnixMilliseconds)
        {
            throw RandomArgumentException.Create("unixMilliseconds", "The UUIDv7 timestamp must fit in 48 unsigned bits.");
        }
    }
}
