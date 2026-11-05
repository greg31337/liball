namespace ttf;

internal class BigEndianReader
{
    FontDataSlice data;
    int position;

    internal BigEndianReader(FontDataSlice data)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        this.data = data;
        position = 0;
    }

    internal int GetPosition()
    {
        return position;
    }

    internal long GetAbsolutePosition()
    {
        return data.GetAbsoluteOffset() + position;
    }

    internal int GetRemainingLength()
    {
        return data.GetLength() - position;
    }

    internal byte ReadUInt8()
    {
        return (byte)ReadUnsigned(1);
    }

    internal sbyte ReadInt8()
    {
        int value = (int)ReadUnsigned(1);
        return (sbyte)(value < 128 ? value : value - 256);
    }

    internal ushort ReadUInt16()
    {
        return (ushort)ReadUnsigned(2);
    }

    internal short ReadInt16()
    {
        int value = (int)ReadUnsigned(2);
        return (short)(value < 32768 ? value : value - 65536);
    }

    internal uint ReadUInt24()
    {
        return (uint)ReadUnsigned(3);
    }

    internal int ReadInt24()
    {
        uint value = (uint)ReadUnsigned(3);

        if ((value & 0x00800000U) != 0)
        {
            return (int)((long)value - 16777216L);
        }

        return (int)value;
    }

    internal uint ReadUInt32()
    {
        return (uint)ReadUnsigned(4);
    }

    internal int ReadInt32()
    {
        ulong value = ReadUnsigned(4);

        if (value <= int.MaxValue)
        {
            return (int)value;
        }

        return (int)((long)value - 4294967296L);
    }

    internal ulong ReadUInt64()
    {
        return ReadUnsigned(8);
    }

    internal long ReadInt64()
    {
        ulong value = ReadUnsigned(8);

        if (value <= long.MaxValue)
        {
            return (long)value;
        }

        return long.MinValue + (long)(value - 0x8000000000000000UL);
    }

    internal double ReadFixed16Dot16()
    {
        return ReadInt32() / 65536.0;
    }

    internal double ReadF2Dot14()
    {
        return ReadInt16() / 16384.0;
    }

    internal int ReadTag()
    {
        return ReadInt32();
    }

    internal int ReadOffset16()
    {
        return ReadUInt16();
    }

    internal int ReadOffset24()
    {
        return (int)ReadUInt24();
    }

    internal long ReadOffset32()
    {
        return ReadUInt32();
    }

    internal long ReadLongDateTimeSeconds()
    {
        return ReadInt64();
    }

    internal System.DateTime ReadLongDateTime()
    {
        long seconds = ReadLongDateTimeSeconds();

        if (seconds < -60052752000L || seconds > 255485145599L)
        {
            throw FontFormatException.Create("The LONGDATETIME value is outside the supported UTC date range.");
        }

        long ticks = 600527520000000000L + seconds * 10000000L;
        return new System.DateTime(ticks, System.DateTimeKind.Utc);
    }

    ulong ReadUnsigned(int byteCount)
    {
        EnsureAvailable(byteCount);

        ReadOnlySpan<byte> span = data.GetMemory().Span;
        ulong value = 0;
        int index = 0;

        while (index < byteCount)
        {
            value = (value << 8) | span[position + index];
            index += 1;
        }

        position += byteCount;
        return value;
    }

    void EnsureAvailable(int byteCount)
    {
        if (byteCount < 0 || byteCount > data.GetLength() - position)
        {
            throw FontFormatException.Create("The bounded font data is truncated.");
        }
    }
}
