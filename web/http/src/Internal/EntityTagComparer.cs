namespace http;

internal static class EntityTagComparer
{
    internal static bool CompareStrong(HttpEntityTag left, HttpEntityTag right)
    {
        if (!CanCompare(left, right) || left.IsWeak || right.IsWeak)
        {
            return false;
        }

        return OpaqueTagsEqual(left.OpaqueTag, right.OpaqueTag);
    }

    internal static bool CompareWeak(HttpEntityTag left, HttpEntityTag right)
    {
        if (!CanCompare(left, right))
        {
            return false;
        }

        return OpaqueTagsEqual(left.OpaqueTag, right.OpaqueTag);
    }

    static bool CanCompare(HttpEntityTag left, HttpEntityTag right)
    {
        if (left == null || right == null || left.IsWildcard || right.IsWildcard)
        {
            return false;
        }

        if (left.OpaqueTag == null || right.OpaqueTag == null)
        {
            return false;
        }

        return IsOpaqueTagValid(left.OpaqueTag) && IsOpaqueTagValid(right.OpaqueTag);
    }

    static bool IsOpaqueTagValid(byte[] value)
    {
        int index = 0;
        while (index < value.Length)
        {
            byte character = value[index];
            if (character != 0x21 && (character < 0x23 || character > 0x7e) && character < 0x80)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool OpaqueTagsEqual(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
