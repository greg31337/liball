namespace asn1;

internal class TextAccumulator
{
    internal char[] Buffer;
    internal int Count;

    internal TextAccumulator(int initialCapacity)
    {
        if (initialCapacity < 0)
        {
            throw new System.ArgumentOutOfRangeException("initialCapacity");
        }

        this.Buffer = new char[initialCapacity];
    }

    internal void AppendCharacter(char value)
    {
        this.EnsureAdditionalCapacity(1);
        this.Buffer[this.Count] = value;
        this.Count = this.Count + 1;
    }

    internal void AppendText(string value)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        this.AppendTextSegment(value, 0, value.Length);
    }

    internal void AppendTextSegment(string value, int offset, int count)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        if (!CursorMath.IsRangeValid(offset, count, value.Length))
        {
            throw new System.ArgumentOutOfRangeException("offset");
        }

        this.EnsureAdditionalCapacity(count);
        value.CopyTo(offset, this.Buffer, this.Count, count);
        this.Count = this.Count + count;
    }

    internal string ToText()
    {
        return new string(this.Buffer, 0, this.Count);
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

        char[] replacement = new char[newCapacity];
        System.Array.Copy(this.Buffer, 0, replacement, 0, this.Count);
        this.Buffer = replacement;
    }
}
