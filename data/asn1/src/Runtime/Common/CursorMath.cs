namespace asn1;

internal static class CursorMath
{
    internal static bool IsOffsetValid(int offset, int length)
    {
        return length >= 0 && offset >= 0 && offset <= length;
    }

    internal static bool IsRangeValid(int offset, int count, int length)
    {
        return length >= 0 && offset >= 0 && count >= 0 && offset <= length && count <= length - offset;
    }

    internal static int AdvanceOrNegativeOne(int offset, int count, int length)
    {
        if (!IsRangeValid(offset, count, length))
        {
            return -1;
        }

        return offset + count;
    }

    internal static int RemainingOrNegativeOne(int offset, int length)
    {
        if (!IsOffsetValid(offset, length))
        {
            return -1;
        }

        return length - offset;
    }
}
