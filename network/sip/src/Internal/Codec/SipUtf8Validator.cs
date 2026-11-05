namespace sip;

internal static class SipUtf8Validator
{
    internal static bool IsValidUtf8(byte[] bytes)
    {
        return GetInvalidUtf8Offset(bytes) < 0;
    }

    internal static int GetInvalidUtf8Offset(byte[] bytes)
    {
        int index = 0;
        while (index < bytes.Length)
        {
            int first = bytes[index];
            if (first <= 127)
            {
                index = index + 1;
                continue;
            }

            if (first >= 194 && first <= 223)
            {
                if (!HasContinuationBytes(bytes, index, 1))
                {
                    return index;
                }

                index = index + 2;
                continue;
            }

            if (first >= 224 && first <= 239)
            {
                if (!HasContinuationBytes(bytes, index, 2))
                {
                    return index;
                }

                int second = bytes[index + 1];
                if (first == 224 && second < 160 || first == 237 && second > 159)
                {
                    return index;
                }

                index = index + 3;
                continue;
            }

            if (first >= 240 && first <= 244)
            {
                if (!HasContinuationBytes(bytes, index, 3))
                {
                    return index;
                }

                int second = bytes[index + 1];
                if (first == 240 && second < 144 || first == 244 && second > 143)
                {
                    return index;
                }

                index = index + 4;
                continue;
            }

            return index;
        }

        return -1;
    }

    static bool HasContinuationBytes(byte[] bytes, int index, int count)
    {
        if (count > bytes.Length - index - 1)
        {
            return false;
        }

        int continuationIndex = 1;
        while (continuationIndex <= count)
        {
            int value = bytes[index + continuationIndex];
            if (value < 128 || value > 191)
            {
                return false;
            }

            continuationIndex = continuationIndex + 1;
        }

        return true;
    }
}
