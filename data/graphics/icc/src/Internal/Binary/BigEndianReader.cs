namespace icc;

internal class BigEndianReader
{
    internal SpanCursor cursor;

    internal BigEndianReader(System.ReadOnlyMemory<byte> memory, long absoluteBaseOffset)
    {
        cursor = new SpanCursor(memory, absoluteBaseOffset);
    }

    internal byte ReadUInt8()
    {
        return cursor.ReadByte();
    }

    internal sbyte ReadInt8()
    {
        int value = cursor.ReadByte();
        if (value >= 128)
        {
            value = value - 256;
        }

        return (sbyte)value;
    }

    internal ushort ReadUInt16()
    {
        cursor.EnsureAvailable(2);
        uint first = cursor.ReadByte();
        uint second = cursor.ReadByte();
        return (ushort)((first << 8) | second);
    }

    internal short ReadInt16()
    {
        int value = ReadUInt16();
        if (value >= 32768)
        {
            value = value - 65536;
        }

        return (short)value;
    }

    internal uint ReadUInt32()
    {
        cursor.EnsureAvailable(4);
        uint first = cursor.ReadByte();
        uint second = cursor.ReadByte();
        uint third = cursor.ReadByte();
        uint fourth = cursor.ReadByte();
        return (first << 24) | (second << 16) | (third << 8) | fourth;
    }

    internal int ReadInt32()
    {
        uint value = ReadUInt32();
        if (value <= int.MaxValue)
        {
            return (int)value;
        }

        return -1 - (int)(uint.MaxValue - value);
    }

    internal ulong ReadUInt64()
    {
        cursor.EnsureAvailable(8);
        ulong high = ReadUInt32();
        ulong low = ReadUInt32();
        return (high << 32) | low;
    }

    internal long ReadInt64()
    {
        ulong value = ReadUInt64();
        if (value <= long.MaxValue)
        {
            return (long)value;
        }

        return -1 - (long)(ulong.MaxValue - value);
    }

    internal byte[] ReadExactBytes(int count)
    {
        return cursor.ReadBytes(count);
    }

    internal System.ReadOnlyMemory<byte> ReadExactSlice(int count)
    {
        return cursor.ReadSlice(count);
    }

    internal void SkipExactBytes(int count)
    {
        cursor.SkipBytes(count);
    }

    internal void AlignReadPosition(int alignment)
    {
        cursor.AlignPosition(alignment);
    }

    internal int GetReadPosition()
    {
        return cursor.GetPosition();
    }

    internal long GetAbsoluteOffset()
    {
        return cursor.GetAbsoluteOffset();
    }

    internal int GetRemainingCount()
    {
        return cursor.GetRemainingCount();
    }
}
