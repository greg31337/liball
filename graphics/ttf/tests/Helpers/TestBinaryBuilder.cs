using System.Buffers.Binary;

namespace ttf.Tests;

internal sealed class TestBinaryBuilder
{
    private readonly List<byte> bytes = new List<byte>();

    internal int GetLength()
    {
        return bytes.Count;
    }

    internal TestBinaryBuilder WriteUInt8(byte value)
    {
        bytes.Add(value);
        return this;
    }

    internal TestBinaryBuilder WriteInt8(sbyte value)
    {
        bytes.Add((byte)value);
        return this;
    }

    internal TestBinaryBuilder WriteUInt16(ushort value)
    {
        bytes.Add((byte)(value >> 8));
        bytes.Add((byte)value);
        return this;
    }

    internal TestBinaryBuilder WriteInt16(short value)
    {
        return WriteUInt16((ushort)value);
    }

    internal TestBinaryBuilder WriteUInt24(uint value)
    {
        if (value > 0xFFFFFFU)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        bytes.Add((byte)(value >> 16));
        bytes.Add((byte)(value >> 8));
        bytes.Add((byte)value);
        return this;
    }

    internal TestBinaryBuilder WriteInt24(int value)
    {
        if (value < -8388608 || value > 8388607)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        return WriteUInt24((uint)value & 0xFFFFFFU);
    }

    internal TestBinaryBuilder WriteUInt32(uint value)
    {
        bytes.Add((byte)(value >> 24));
        bytes.Add((byte)(value >> 16));
        bytes.Add((byte)(value >> 8));
        bytes.Add((byte)value);
        return this;
    }

    internal TestBinaryBuilder WriteInt32(int value)
    {
        return WriteUInt32((uint)value);
    }

    internal TestBinaryBuilder WriteUInt64(ulong value)
    {
        bytes.Add((byte)(value >> 56));
        bytes.Add((byte)(value >> 48));
        bytes.Add((byte)(value >> 40));
        bytes.Add((byte)(value >> 32));
        bytes.Add((byte)(value >> 24));
        bytes.Add((byte)(value >> 16));
        bytes.Add((byte)(value >> 8));
        bytes.Add((byte)value);
        return this;
    }

    internal TestBinaryBuilder WriteInt64(long value)
    {
        return WriteUInt64((ulong)value);
    }

    internal TestBinaryBuilder WriteTag(string value)
    {
        return WriteUInt32(TestSfntEncoding.EncodeTag(value));
    }

    internal TestBinaryBuilder WriteBytes(byte[] value)
    {
        ArgumentNullException.ThrowIfNull(value);
        bytes.AddRange(value);
        return this;
    }

    internal TestBinaryBuilder WriteZeros(int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        for (int index = 0; index < count; index += 1)
        {
            bytes.Add(0);
        }

        return this;
    }

    internal TestBinaryBuilder PadToFourBytes()
    {
        int padding = (4 - bytes.Count % 4) % 4;
        return WriteZeros(padding);
    }

    internal TestBinaryBuilder PatchUInt16(int offset, ushort value)
    {
        ValidatePatch(offset, 2);
        byte[] encoded = new byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(encoded, value);
        bytes[offset] = encoded[0];
        bytes[offset + 1] = encoded[1];
        return this;
    }

    internal TestBinaryBuilder PatchUInt32(int offset, uint value)
    {
        ValidatePatch(offset, 4);
        byte[] encoded = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(encoded, value);

        for (int index = 0; index < encoded.Length; index += 1)
        {
            bytes[offset + index] = encoded[index];
        }

        return this;
    }

    internal byte[] CopyBytes()
    {
        return bytes.ToArray();
    }

    private void ValidatePatch(int offset, int length)
    {
        if (offset < 0 || offset > bytes.Count - length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }
    }
}
