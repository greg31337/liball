namespace ttf;

internal class FontData
{
    ReadOnlyMemory<byte> memory;

    internal FontData(ReadOnlyMemory<byte> source, bool retain)
    {
        memory = retain ? source : new ReadOnlyMemory<byte>(source.ToArray());
    }

    internal static FontData Create(byte[] source)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        return new FontData(new ReadOnlyMemory<byte>(source), false);
    }

    internal static FontData Create(ReadOnlyMemory<byte> source)
    {
        return new FontData(source, false);
    }

    internal static FontData Create(ReadOnlyMemory<byte> source, bool retain)
    {
        return new FontData(source, retain);
    }

    internal static FontData Create(ReadOnlySpan<byte> source)
    {
        return new FontData(new ReadOnlyMemory<byte>(source.ToArray()), true);
    }

    internal int GetLength()
    {
        return memory.Length;
    }

    internal ReadOnlyMemory<byte> GetMemory()
    {
        return memory;
    }

    internal byte[] CopyBytes()
    {
        return memory.ToArray();
    }

    internal FontDataSlice GetSlice(int offset, int length)
    {
        FontDataRange.Validate(memory.Length, offset, length);
        return new FontDataSlice(memory.Slice(offset, length), offset);
    }
}

internal class FontDataSlice
{
    ReadOnlyMemory<byte> memory;
    long absoluteOffset;

    internal FontDataSlice(ReadOnlyMemory<byte> memory, long absoluteOffset)
    {
        if (absoluteOffset < 0)
        {
            throw new System.ArgumentOutOfRangeException("absoluteOffset");
        }

        if (absoluteOffset > long.MaxValue - memory.Length)
        {
            throw new System.ArgumentOutOfRangeException("absoluteOffset");
        }

        this.memory = memory;
        this.absoluteOffset = absoluteOffset;
    }

    internal int GetLength()
    {
        return memory.Length;
    }

    internal long GetAbsoluteOffset()
    {
        return absoluteOffset;
    }

    internal ReadOnlyMemory<byte> GetMemory()
    {
        return memory;
    }

    internal byte[] CopyBytes()
    {
        return memory.ToArray();
    }

    internal FontDataSlice GetSlice(int offset, int length)
    {
        FontDataRange.Validate(memory.Length, offset, length);
        return new FontDataSlice(
            memory.Slice(offset, length),
            CheckedFontMath.AddOffset(absoluteOffset, offset));
    }
}

internal static class FontDataRange
{
    internal static void Validate(int availableLength, int offset, int length)
    {
        int endOffset = CheckedFontMath.AddOffset(offset, length);

        if (availableLength < 0 || endOffset > availableLength)
        {
            throw FontFormatException.Create("The requested slice is outside the bounded font data.");
        }
    }
}
