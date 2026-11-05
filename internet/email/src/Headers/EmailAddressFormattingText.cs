namespace email;

internal class EmailAddressFormattingText
{
    internal static bool IsValidUnicode(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (char.IsHighSurrogate(character))
            {
                if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]))
                {
                    return false;
                }

                index += 2;
                continue;
            }

            if (char.IsLowSurrogate(character))
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal static bool ContainsNonAscii(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 127)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal static bool ContainsInjectionOrControl(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < 32 || character == 127)
            {
                return true;
            }

            index++;
        }

        return false;
    }
}
