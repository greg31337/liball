namespace pop3;

internal class Pop3Utf8Validator
{
    internal bool IsValidUtf8(System.ReadOnlySpan<byte> value)
    {
        int index = 0;
        while (index < value.Length)
        {
            byte first = value[index];
            if (first <= 127)
            {
                index++;
            }
            else if (first >= 194 && first <= 223)
            {
                if (index + 1 >= value.Length || !IsContinuation(value[index + 1]))
                {
                    return false;
                }
                index += 2;
            }
            else if (first >= 224 && first <= 239)
            {
                if (index + 2 >= value.Length ||
                    !IsContinuation(value[index + 1]) ||
                    !IsContinuation(value[index + 2]))
                {
                    return false;
                }
                if (first == 224 && value[index + 1] < 160)
                {
                    return false;
                }
                if (first == 237 && value[index + 1] > 159)
                {
                    return false;
                }
                index += 3;
            }
            else if (first >= 240 && first <= 244)
            {
                if (index + 3 >= value.Length ||
                    !IsContinuation(value[index + 1]) ||
                    !IsContinuation(value[index + 2]) ||
                    !IsContinuation(value[index + 3]))
                {
                    return false;
                }
                if (first == 240 && value[index + 1] < 144)
                {
                    return false;
                }
                if (first == 244 && value[index + 1] > 143)
                {
                    return false;
                }
                index += 4;
            }
            else
            {
                return false;
            }
        }
        return true;
    }

    bool IsContinuation(byte value)
    {
        return value >= 128 && value <= 191;
    }
}
