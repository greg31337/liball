using System.Text;

namespace email;

internal class EmailHeaderUtf8
{
    internal static int GetInvalidIndex(byte[] value)
    {
        int index = 0;
        while (index < value.Length)
        {
            byte first = value[index];
            if (first < 128)
            {
                index++;
                continue;
            }

            if (first >= 194 && first <= 223)
            {
                if (index + 1 >= value.Length || !IsContinuation(value[index + 1]))
                {
                    return index;
                }

                index += 2;
                continue;
            }

            if (first >= 224 && first <= 239)
            {
                if (index + 2 >= value.Length || !IsContinuation(value[index + 1]) || !IsContinuation(value[index + 2]))
                {
                    return index;
                }

                if (first == 224 && value[index + 1] < 160 || first == 237 && value[index + 1] >= 160)
                {
                    return index;
                }

                index += 3;
                continue;
            }

            if (first >= 240 && first <= 244)
            {
                if (index + 3 >= value.Length || !IsContinuation(value[index + 1]) || !IsContinuation(value[index + 2]) || !IsContinuation(value[index + 3]))
                {
                    return index;
                }

                if (first == 240 && value[index + 1] < 144 || first == 244 && value[index + 1] >= 144)
                {
                    return index;
                }

                index += 4;
                continue;
            }

            return index;
        }

        return -1;
    }

    internal static bool ContainsNonAscii(byte[] value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] >= 128)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal static string GetString(byte[] value)
    {
        return Encoding.UTF8.GetString(value);
    }

    static bool IsContinuation(byte value)
    {
        return value >= 128 && value <= 191;
    }
}
