namespace whois;

internal class ByteAccumulator
{
    byte[] buffer;
    int count;
    bool overflowed;

    internal ByteAccumulator(int maximum)
    {
        if (maximum <= 0)
        {
            throw new System.ArgumentOutOfRangeException("maximum");
        }
        buffer = new byte[maximum];
        count = 0;
        overflowed = false;
    }

    internal bool Append(System.ReadOnlySpan<byte> bytes)
    {
        if (overflowed)
        {
            return false;
        }

        int available = buffer.Length - count;
        int copyCount = bytes.Length;
        if (copyCount > available)
        {
            copyCount = available;
            overflowed = true;
        }
        if (copyCount > 0)
        {
            bytes.Slice(0, copyCount).CopyTo(System.MemoryExtensions.AsSpan(buffer, count, copyCount));
            count = count + copyCount;
        }
        return !overflowed;
    }

    internal int GetCount()
    {
        return count;
    }

    internal bool HasOverflowed()
    {
        return overflowed;
    }

    internal byte[] GetBytes()
    {
        byte[] result = new byte[count];
        System.MemoryExtensions.AsSpan(buffer, 0, count).CopyTo(System.MemoryExtensions.AsSpan(result));
        return result;
    }
}
