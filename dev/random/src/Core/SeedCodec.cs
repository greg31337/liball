using System.Buffers.Binary;
using System.Text;

namespace random;

internal enum SeedDomain
{
    UInt64 = 1,
    Int64 = 2,
    Bytes = 3,
    TextUtf8 = 4,
    UInt32Array = 5
}

internal class SeedCodec
{
    internal const int EncodingVersion = 1;
    internal const int HeaderSize = 20;
    internal const ulong Magic = 0x3144454553444E52UL;

    internal static byte[] EncodeUInt64(ulong seed)
    {
        byte[] payload = new byte[8];
        BinaryCodec.WriteUInt64(payload, 0, seed);
        return EncodePayload(SeedDomain.UInt64, payload);
    }

    internal static byte[] EncodeInt64(long seed)
    {
        byte[] payload = new byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(payload, seed);
        return EncodePayload(SeedDomain.Int64, payload);
    }

    internal static byte[] EncodeBytes(byte[] seed)
    {
        Guard.NotNull(seed, "seed");
        return EncodePayload(SeedDomain.Bytes, seed);
    }

    internal static byte[] EncodeText(string seed)
    {
        Guard.NotNull(seed, "seed");
        int byteCount = Utf8ByteCount(seed, "seed");
        byte[] payload = Encoding.UTF8.GetBytes(seed);
        if (payload.Length != byteCount)
        {
            throw RandomArgumentException.Create("seed", "The text seed does not have a canonical UTF-8 representation.");
        }

        return EncodePayload(SeedDomain.TextUtf8, payload);
    }

    internal static byte[] EncodeUInt32Array(uint[] seed)
    {
        Guard.NotNull(seed, "seed");
        if (seed.Length > (int.MaxValue - HeaderSize) / 4)
        {
            throw RandomArgumentException.Create("seed", "The integer seed array is too large to encode.");
        }

        byte[] payload = new byte[seed.Length * 4];
        int index = 0;
        int offset = 0;
        while (index < seed.Length)
        {
            offset = BinaryCodec.WriteUInt32(payload, offset, seed[index]);
            index++;
        }

        return EncodePayload(SeedDomain.UInt32Array, payload);
    }

    internal static byte[] EncodePayload(SeedDomain domain, byte[] payload)
    {
        if (payload.Length > int.MaxValue - HeaderSize)
        {
            throw RandomArgumentException.Create("seed", "The seed payload is too large to encode.");
        }

        byte[] encoded = new byte[HeaderSize + payload.Length];
        int offset = 0;
        offset = BinaryCodec.WriteUInt64(encoded, offset, Magic);
        offset = BinaryCodec.WriteUInt32(encoded, offset, (uint)EncodingVersion);
        offset = BinaryCodec.WriteUInt32(encoded, offset, (uint)domain);
        offset = BinaryCodec.WriteUInt32(encoded, offset, (uint)payload.Length);
        BinaryCodec.WriteBytes(encoded, offset, payload);
        return encoded;
    }

    internal static int Utf8ByteCount(string value)
    {
        return Utf8ByteCount(value, "seed");
    }

    internal static int Utf8ByteCount(string value, string argumentName)
    {
        int byteCount = 0;
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            int required;
            if (current <= 0x007F)
            {
                required = 1;
            }
            else if (current <= 0x07FF)
            {
                required = 2;
            }
            else if (current >= 0xD800 && current <= 0xDBFF)
            {
                if (index + 1 >= value.Length)
                {
                    throw RandomArgumentException.Create(argumentName, "The text contains an unpaired high surrogate.");
                }

                char next = value[index + 1];
                if (next < 0xDC00 || next > 0xDFFF)
                {
                    throw RandomArgumentException.Create(argumentName, "The text contains an unpaired high surrogate.");
                }

                required = 4;
                index++;
            }
            else if (current >= 0xDC00 && current <= 0xDFFF)
            {
                throw RandomArgumentException.Create(argumentName, "The text contains an unpaired low surrogate.");
            }
            else
            {
                required = 3;
            }

            if (byteCount > int.MaxValue - HeaderSize - required)
            {
                throw RandomArgumentException.Create(argumentName, "The text is too large to encode.");
            }

            byteCount += required;
            index++;
        }

        return byteCount;
    }
}
