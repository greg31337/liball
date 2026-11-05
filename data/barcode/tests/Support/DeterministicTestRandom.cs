namespace barcode.Tests;

internal class DeterministicTestRandom
{
    const uint ZeroSeedReplacement = 0x6d2b79f5U;
    uint State;

    internal DeterministicTestRandom(int seed)
    {
        State = (uint)seed;
        if (State == 0U)
        {
            State = ZeroSeedReplacement;
        }
    }

    internal uint NextUInt32()
    {
        uint value = State;
        value = value ^ (value << 13);
        value = value ^ (value >> 17);
        value = value ^ (value << 5);
        State = value;
        return value;
    }

    internal int NextInteger(int exclusiveMaximum)
    {
        if (exclusiveMaximum <= 0)
        {
            throw new global::System.ArgumentOutOfRangeException(
                nameof(exclusiveMaximum));
        }

        return (int)(NextUInt32() % (uint)exclusiveMaximum);
    }

    internal int NextIntegerRange(
        int inclusiveMinimum,
        int exclusiveMaximum)
    {
        if (exclusiveMaximum <= inclusiveMinimum)
        {
            throw new global::System.ArgumentOutOfRangeException(
                nameof(exclusiveMaximum));
        }

        return inclusiveMinimum +
            NextInteger(exclusiveMaximum - inclusiveMinimum);
    }

    internal byte NextByte()
    {
        return (byte)(NextUInt32() & 255U);
    }

    internal void FillBytes(byte[] target)
    {
        int index = 0;
        while (index < target.Length)
        {
            target[index] = NextByte();
            index = index + 1;
        }
    }

    internal int[] SelectDistinctIndices(int length, int count)
    {
        if (length < 0 || count < 0 || count > length)
        {
            throw new global::System.ArgumentOutOfRangeException(
                nameof(count));
        }

        int[] pool = new int[length];
        int index = 0;
        while (index < length)
        {
            pool[index] = index;
            index = index + 1;
        }

        index = 0;
        while (index < count)
        {
            int selected = index + NextInteger(length - index);
            int swap = pool[index];
            pool[index] = pool[selected];
            pool[selected] = swap;
            index = index + 1;
        }

        int[] result = new int[count];
        global::System.Array.Copy(pool, result, count);
        global::System.Array.Sort(result);
        return result;
    }

    internal void ShuffleIntegers(int[] values)
    {
        int index = values.Length - 1;
        while (index > 0)
        {
            int selected = NextInteger(index + 1);
            int swap = values[index];
            values[index] = values[selected];
            values[selected] = swap;
            index = index - 1;
        }
    }
}
