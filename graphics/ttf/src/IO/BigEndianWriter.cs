namespace ttf;

internal class BigEndianWriter
{
    byte[] buffer;
    int length;
    int maximumLength;

    internal BigEndianWriter(int initialCapacity, int maximumLength)
    {
        if (initialCapacity < 0)
        {
            throw new System.ArgumentOutOfRangeException("initialCapacity");
        }

        if (maximumLength < 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumLength");
        }

        if (initialCapacity > maximumLength)
        {
            throw new System.ArgumentException("The initial capacity exceeds the maximum length.", "initialCapacity");
        }

        buffer = new byte[initialCapacity];
        length = 0;
        this.maximumLength = maximumLength;
    }

    internal static BigEndianWriter Create()
    {
        return new BigEndianWriter(256, 536870912);
    }

    internal static BigEndianWriter Create(int maximumLength)
    {
        if (maximumLength < 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumLength");
        }

        int initialCapacity = maximumLength < 256 ? maximumLength : 256;
        return new BigEndianWriter(initialCapacity, maximumLength);
    }

    internal static BigEndianWriter Create(
        int initialCapacity,
        int maximumLength)
    {
        return new BigEndianWriter(initialCapacity, maximumLength);
    }

    internal int GetLength()
    {
        return length;
    }

    internal int GetCapacity()
    {
        return buffer.Length;
    }

    internal int GetMaximumLength()
    {
        return maximumLength;
    }

    internal byte[] CopyBytes()
    {
        byte[] result = new byte[length];
        buffer.AsSpan(0, length).CopyTo(result);
        return result;
    }

    internal void WriteUInt8(byte value)
    {
        WriteUnsigned(value, 1);
    }

    internal void WriteInt8(sbyte value)
    {
        int encoded = value < 0 ? value + 256 : value;
        WriteUnsigned((byte)encoded, 1);
    }

    internal void WriteUInt16(ushort value)
    {
        WriteUnsigned(value, 2);
    }

    internal void WriteInt16(short value)
    {
        int encoded = value < 0 ? value + 65536 : value;
        WriteUnsigned((ushort)encoded, 2);
    }

    internal void WriteUInt24(uint value)
    {
        if (value > 0x00FFFFFFU)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        WriteUnsigned(value, 3);
    }

    internal void WriteInt24(int value)
    {
        if (value < -8388608 || value > 8388607)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        uint encoded = CheckedFontMath.ReinterpretInt32AsUInt32(value);
        WriteUnsigned(encoded & 0x00FFFFFFU, 3);
    }

    internal void WriteUInt32(uint value)
    {
        WriteUnsigned(value, 4);
    }

    internal void WriteInt32(int value)
    {
        long encoded = value < 0 ? (long)value + 4294967296L : value;
        WriteUnsigned((uint)encoded, 4);
    }

    internal void WriteUInt64(ulong value)
    {
        WriteUnsigned(value, 8);
    }

    internal void WriteInt64(long value)
    {
        ulong encoded = value < 0
            ? 0x8000000000000000UL + (ulong)(value + long.MaxValue + 1L)
            : (ulong)value;
        WriteUnsigned(encoded, 8);
    }

    internal void WriteFixed16Dot16(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        double scaled = System.Math.Round(value * 65536.0, System.MidpointRounding.AwayFromZero);

        if (scaled < int.MinValue || scaled > int.MaxValue)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        WriteInt32((int)scaled);
    }

    internal void WriteF2Dot14(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        double scaled = System.Math.Round(value * 16384.0, System.MidpointRounding.AwayFromZero);

        if (scaled < short.MinValue || scaled > short.MaxValue)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        WriteInt16((short)scaled);
    }

    internal void WriteTag(int value)
    {
        WriteInt32(value);
    }

    internal void WriteOffset16(int value)
    {
        if (value < 0 || value > ushort.MaxValue)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        WriteUInt16((ushort)value);
    }

    internal void WriteOffset24(int value)
    {
        if (value < 0 || value > 16777215)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        WriteUInt24((uint)value);
    }

    internal void WriteOffset32(long value)
    {
        if (value < 0 || value > uint.MaxValue)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        WriteUInt32((uint)value);
    }

    internal void WriteLongDateTimeSeconds(long value)
    {
        WriteInt64(value);
    }

    internal void WriteLongDateTime(System.DateTime value)
    {
        if (value.Kind != System.DateTimeKind.Utc)
        {
            throw new System.ArgumentException("A LONGDATETIME value must use the UTC kind.", "value");
        }

        long ticksSinceEpoch = value.Ticks - 600527520000000000L;

        if (ticksSinceEpoch % 10000000L != 0)
        {
            throw new System.ArgumentException("A LONGDATETIME value must have whole-second precision.", "value");
        }

        WriteLongDateTimeSeconds(ticksSinceEpoch / 10000000L);
    }

    internal void WriteBytes(byte[]? value)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        WriteSpan(value.AsSpan());
    }

    internal void WriteMemory(ReadOnlyMemory<byte> value)
    {
        WriteSpan(value.Span);
    }

    internal void WriteSpan(ReadOnlySpan<byte> value)
    {
        EnsureCapacity(value.Length);
        value.CopyTo(buffer.AsSpan(length, value.Length));
        length += value.Length;
    }

    internal int WritePaddingToFourBytes()
    {
        int paddingLength = (4 - length % 4) % 4;
        EnsureCapacity(paddingLength);

        int index = 0;

        while (index < paddingLength)
        {
            buffer[length + index] = 0;
            index += 1;
        }

        length += paddingLength;
        return paddingLength;
    }

    void WriteUnsigned(ulong value, int byteCount)
    {
        EnsureCapacity(byteCount);

        int index = 0;

        while (index < byteCount)
        {
            int shift = (byteCount - index - 1) * 8;
            buffer[length + index] = (byte)((value >> shift) & 0xFFUL);
            index += 1;
        }

        length += byteCount;
    }

    void EnsureCapacity(int additionalLength)
    {
        if (additionalLength < 0)
        {
            throw new System.ArgumentOutOfRangeException("additionalLength");
        }

        if (additionalLength > maximumLength - length)
        {
            throw FontLimitException.Create("The deterministic font output buffer exceeded its maximum length.");
        }

        int requiredLength = length + additionalLength;

        if (requiredLength <= buffer.Length)
        {
            return;
        }

        int nextCapacity = buffer.Length;

        if (nextCapacity == 0)
        {
            nextCapacity = maximumLength < 256 ? maximumLength : 256;
        }

        while (nextCapacity < requiredLength)
        {
            if (nextCapacity > maximumLength - nextCapacity)
            {
                nextCapacity = maximumLength;
            }
            else
            {
                nextCapacity += nextCapacity;
            }
        }

        byte[] nextBuffer = new byte[nextCapacity];
        buffer.AsSpan(0, length).CopyTo(nextBuffer);
        buffer = nextBuffer;
    }
}
