namespace icc;

internal class BigEndianWriter
{
    internal System.Memory<byte> memory;
    internal int position;
    internal long absoluteBaseOffset;

    internal BigEndianWriter(System.Memory<byte> memory, long absoluteBaseOffset)
    {
        if (absoluteBaseOffset < 0)
        {
            throw new IccProfileWriteException("A writer base offset must not be negative.");
        }

        this.memory = memory;
        position = 0;
        this.absoluteBaseOffset = absoluteBaseOffset;
    }

    internal void WriteUInt8(byte value)
    {
        EnsureAvailable(1);
        memory.Span[position] = value;
        position = position + 1;
    }

    internal void WriteInt8(sbyte value)
    {
        int encoded = value;
        if (encoded < 0)
        {
            encoded = encoded + 256;
        }

        WriteUInt8((byte)encoded);
    }

    internal void WriteUInt16(ushort value)
    {
        EnsureAvailable(2);
        memory.Span[position] = (byte)((value >> 8) & 255);
        memory.Span[position + 1] = (byte)(value & 255);
        position = position + 2;
    }

    internal void WriteInt16(short value)
    {
        int encoded = value;
        if (encoded < 0)
        {
            encoded = encoded + 65536;
        }

        WriteUInt16((ushort)encoded);
    }

    internal void WriteUInt32(uint value)
    {
        EnsureAvailable(4);
        memory.Span[position] = (byte)((value >> 24) & 255);
        memory.Span[position + 1] = (byte)((value >> 16) & 255);
        memory.Span[position + 2] = (byte)((value >> 8) & 255);
        memory.Span[position + 3] = (byte)(value & 255);
        position = position + 4;
    }

    internal void WriteInt32(int value)
    {
        uint encoded;
        if (value >= 0)
        {
            encoded = (uint)value;
        }
        else
        {
            encoded = uint.MaxValue - (uint)(-1 - value);
        }

        WriteUInt32(encoded);
    }

    internal void WriteUInt64(ulong value)
    {
        EnsureAvailable(8);
        WriteUInt32((uint)(value >> 32));
        WriteUInt32((uint)(value & uint.MaxValue));
    }

    internal void WriteInt64(long value)
    {
        ulong encoded;
        if (value >= 0)
        {
            encoded = (ulong)value;
        }
        else
        {
            encoded = ulong.MaxValue - (ulong)(-1 - value);
        }

        WriteUInt64(encoded);
    }

    internal void WriteExactBytes(byte[] values)
    {
        if (values == null)
        {
            throw new IccProfileWriteException("Bytes to write cannot be null.");
        }

        EnsureAvailable(values.Length);
        System.ReadOnlySpan<byte> source = new System.ReadOnlySpan<byte>(values);
        source.CopyTo(memory.Span.Slice(position, values.Length));
        position = position + values.Length;
    }

    internal void WriteZeroBytes(int count)
    {
        EnsureAvailable(count);
        int end = position + count;
        while (position < end)
        {
            memory.Span[position] = 0;
            position = position + 1;
        }
    }

    internal void SkipWriteBytes(int count)
    {
        WriteZeroBytes(count);
    }

    internal void AlignWritePosition(int alignment)
    {
        int aligned = CheckedSize.AlignSize(position, alignment, memory.Length);
        WriteZeroBytes(aligned - position);
    }

    internal int GetWritePosition()
    {
        return position;
    }

    internal long GetAbsoluteOffset()
    {
        return CheckedSize.AddOffsets(absoluteBaseOffset, position, long.MaxValue);
    }

    internal int GetRemainingCount()
    {
        return memory.Length - position;
    }

    internal System.ReadOnlyMemory<byte> GetWrittenMemory()
    {
        return memory.Slice(0, position);
    }

    internal void EnsureAvailable(int count)
    {
        if (count < 0)
        {
            throw new IccProfileWriteException("A write byte count must not be negative.");
        }

        if (count > memory.Length - position)
        {
            throw new IccProfileWriteException("The destination does not contain enough bytes for the requested write.");
        }
    }
}
