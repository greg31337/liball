using System;
using System.Security.Cryptography;
using System.Text;

namespace uuid;

internal class UuidGenerator
{
    internal static UuidValue NewV4()
    {
        return NewV4(RandomTools.SecureSource);
    }

    internal static UuidValue NewV4(IRandomSource source)
    {
        byte[] bytes = RandomTools.NewBytes(16, source);
        ApplyVersionAndVariant(bytes, 4);
        return new UuidValue(bytes);
    }

    internal static UuidValue NewV7()
    {
        return NewV7At(DateTimeOffset.UtcNow);
    }

    internal static UuidValue NewV7At(DateTimeOffset time)
    {
        long unixMilliseconds = UnixTimeTools.ToUnixMilliseconds(time);
        return NewV7AtUnixMilliseconds(unixMilliseconds);
    }

    internal static UuidValue NewV7AtUnixMilliseconds(long unixMilliseconds)
    {
        return NewV7AtUnixMilliseconds(unixMilliseconds, RandomTools.SecureSource);
    }

    internal static UuidValue NewV7AtUnixMilliseconds(long unixMilliseconds, IRandomSource source)
    {
        if (unixMilliseconds < 0 || unixMilliseconds > BigEndianTools.MaxUInt48)
        {
            throw new ArgumentOutOfRangeException("unixMilliseconds");
        }

        byte[] bytes = RandomTools.NewBytes(16, source);
        BigEndianTools.WriteUInt48(bytes, 0, unixMilliseconds);
        ApplyVersionAndVariant(bytes, 7);
        return new UuidValue(bytes);
    }

    internal static UuidValue FromNameV3(UuidValue? namespaceId, string? name)
    {
        if (namespaceId == null)
        {
            throw new ArgumentNullException("namespaceId");
        }

        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        byte[] namespaceBytes = namespaceId.ToBytes();
        byte[] nameBytes = Encoding.UTF8.GetBytes(name);
        byte[] input = new byte[namespaceBytes.Length + nameBytes.Length];
        int index = 0;
        while (index < namespaceBytes.Length)
        {
            input[index] = namespaceBytes[index];
            index = index + 1;
        }

        int nameIndex = 0;
        while (nameIndex < nameBytes.Length)
        {
            input[index] = nameBytes[nameIndex];
            index = index + 1;
            nameIndex = nameIndex + 1;
        }

        byte[] hash = MD5.HashData(input);
        byte[] bytes = new byte[16];
        index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = hash[index];
            index = index + 1;
        }

        ApplyVersionAndVariant(bytes, 3);
        return new UuidValue(bytes);
    }

    internal static UuidValue FromNameV5(UuidValue? namespaceId, string? name)
    {
        if (namespaceId == null)
        {
            throw new ArgumentNullException("namespaceId");
        }

        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        byte[] namespaceBytes = namespaceId.ToBytes();
        byte[] nameBytes = Encoding.UTF8.GetBytes(name);
        byte[] input = new byte[namespaceBytes.Length + nameBytes.Length];
        int index = 0;
        while (index < namespaceBytes.Length)
        {
            input[index] = namespaceBytes[index];
            index = index + 1;
        }

        int nameIndex = 0;
        while (nameIndex < nameBytes.Length)
        {
            input[index] = nameBytes[nameIndex];
            index = index + 1;
            nameIndex = nameIndex + 1;
        }

        byte[] hash = SHA1.HashData(input);
        byte[] bytes = new byte[16];
        index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = hash[index];
            index = index + 1;
        }

        ApplyVersionAndVariant(bytes, 5);
        return new UuidValue(bytes);
    }

    internal static bool IncrementV7RandomTail(byte[]? bytes)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        if (bytes.Length != 16)
        {
            throw new ArgumentException("UUID byte array must be 16 bytes", "bytes");
        }

        int index = 15;
        while (index >= 9)
        {
            if (bytes[index] != 255)
            {
                bytes[index] = (byte)(bytes[index] + 1);
                return true;
            }

            bytes[index] = 0;
            index = index - 1;
        }

        int variantPayload = bytes[8] & 63;
        if (variantPayload != 63)
        {
            bytes[8] = (byte)((bytes[8] & 192) | (variantPayload + 1));
            return true;
        }

        bytes[8] = (byte)(bytes[8] & 192);
        if (bytes[7] != 255)
        {
            bytes[7] = (byte)(bytes[7] + 1);
            return true;
        }

        bytes[7] = 0;
        int versionPayload = bytes[6] & 15;
        if (versionPayload != 15)
        {
            bytes[6] = (byte)((bytes[6] & 240) | (versionPayload + 1));
            return true;
        }

        bytes[6] = (byte)(bytes[6] & 240);
        return false;
    }

    internal static void ApplyVersionAndVariant(byte[]? bytes, int version)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        if (bytes.Length != 16)
        {
            throw new ArgumentException("UUID byte array must be 16 bytes", "bytes");
        }

        if (version < 0 || version > 15)
        {
            throw new ArgumentOutOfRangeException("version");
        }

        bytes[6] = (byte)((bytes[6] & 15) | (version << 4));
        bytes[8] = (byte)((bytes[8] & 63) | 128);
    }
}
