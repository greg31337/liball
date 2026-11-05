namespace icc;

internal class SpanCursor
{
    internal System.ReadOnlyMemory<byte> memory;
    internal int position;
    internal long absoluteBaseOffset;

    internal SpanCursor(System.ReadOnlyMemory<byte> memory, long absoluteBaseOffset)
    {
        if (absoluteBaseOffset < 0)
        {
            throw new IccProfileFormatException("A cursor base offset must not be negative.");
        }

        this.memory = memory;
        position = 0;
        this.absoluteBaseOffset = absoluteBaseOffset;
    }

    internal int GetPosition()
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

    internal byte ReadByte()
    {
        EnsureAvailable(1);
        byte value = memory.Span[position];
        position = position + 1;
        return value;
    }

    internal byte[] ReadBytes(int count)
    {
        EnsureAvailable(count);
        int byteCount = CheckedSize.MultiplyCount(count, 1, System.Array.MaxLength);
        byte[] result = new byte[byteCount];
        memory.Span.Slice(position, count).CopyTo(result);
        position = position + count;
        return result;
    }

    internal System.ReadOnlyMemory<byte> ReadSlice(int count)
    {
        EnsureAvailable(count);
        System.ReadOnlyMemory<byte> result = memory.Slice(position, count);
        position = position + count;
        return result;
    }

    internal void SkipBytes(int count)
    {
        EnsureAvailable(count);
        position = position + count;
    }

    internal void AlignPosition(int alignment)
    {
        int aligned = CheckedSize.AlignSize(position, alignment, memory.Length);
        position = aligned;
    }

    internal void EnsureAvailable(int count)
    {
        if (count < 0)
        {
            throw new IccProfileFormatException("A cursor byte count must not be negative.");
        }

        CheckedSize.ValidateRange(position, count, memory.Length);
    }
}
