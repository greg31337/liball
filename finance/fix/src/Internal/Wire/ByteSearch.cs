using System;

namespace fix;

internal static class ByteSearch
{
    internal static int IndexOfByte(ReadOnlySpan<byte> source, byte value)
    {
        for (int index = 0; index < source.Length; index++)
        {
            if (source[index] == value)
            {
                return index;
            }
        }
        return -1;
    }

    internal static int LastIndexOfByte(ReadOnlySpan<byte> source, byte value)
    {
        int index = source.Length - 1;
        while (index >= 0)
        {
            if (source[index] == value)
            {
                return index;
            }
            index--;
        }
        return -1;
    }

    internal static int IndexOfSequence(ReadOnlySpan<byte> source, ReadOnlySpan<byte> value)
    {
        if (value.Length == 0)
        {
            return 0;
        }
        if (value.Length > source.Length)
        {
            return -1;
        }

        int lastStart = source.Length - value.Length;
        for (int start = 0; start <= lastStart; start++)
        {
            bool matches = true;
            for (int offset = 0; offset < value.Length; offset++)
            {
                if (source[start + offset] != value[offset])
                {
                    matches = false;
                    break;
                }
            }
            if (matches)
            {
                return start;
            }
        }
        return -1;
    }
}
