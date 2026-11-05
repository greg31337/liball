namespace asn1;

internal class ByteAccumulator
{
    internal byte[] Buffer;
    internal int Count;

    internal ByteAccumulator(int initialCapacity)
    {
        if (initialCapacity < 0)
        {
            throw new System.ArgumentOutOfRangeException("initialCapacity");
        }

        this.Buffer = new byte[initialCapacity];
    }

    internal void AppendByte(byte value)
    {
        this.EnsureAdditionalCapacity(1);
        this.Buffer[this.Count] = value;
        this.Count = this.Count + 1;
    }

    internal void AppendBytes(byte[] values)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }

        this.AppendSegment(values, 0, values.Length);
    }

    internal void AppendSegment(byte[] values, int offset, int count)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }

        if (!CursorMath.IsRangeValid(offset, count, values.Length))
        {
            throw new System.ArgumentOutOfRangeException("offset");
        }

        this.EnsureAdditionalCapacity(count);
        System.Buffer.BlockCopy(values, offset, this.Buffer, this.Count, count);
        this.Count = this.Count + count;
    }

    internal byte[] ToArray()
    {
        byte[] result = new byte[this.Count];
        System.Buffer.BlockCopy(this.Buffer, 0, result, 0, this.Count);
        return result;
    }

    internal void Clear()
    {
        this.Count = 0;
    }

    internal void EnsureAdditionalCapacity(int additionalCount)
    {
        if (additionalCount < 0 || additionalCount > int.MaxValue - this.Count)
        {
            throw new System.ArgumentOutOfRangeException("additionalCount");
        }

        int requiredCapacity = this.Count + additionalCount;
        if (requiredCapacity <= this.Buffer.Length)
        {
            return;
        }

        int newCapacity = this.Buffer.Length == 0 ? 4 : this.Buffer.Length;
        while (newCapacity < requiredCapacity)
        {
            if (newCapacity > int.MaxValue / 2)
            {
                newCapacity = requiredCapacity;
            }
            else
            {
                newCapacity = newCapacity * 2;
            }
        }

        byte[] replacement = new byte[newCapacity];
        System.Buffer.BlockCopy(this.Buffer, 0, replacement, 0, this.Count);
        this.Buffer = replacement;
    }
}
