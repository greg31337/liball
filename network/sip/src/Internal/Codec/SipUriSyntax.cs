namespace sip;

internal static class SipUriSyntax
{
    internal static bool IsScheme(string? value)
    {
        if (value == null || value.Length == 0 || !IsAsciiAlpha(value[0]))
        {
            return false;
        }

        int index = 1;
        while (index < value.Length)
        {
            int character = value[index];
            if (!IsAsciiAlpha(character) && !IsAsciiDigit(character)
                && character != '+' && character != '-' && character != '.')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsHostname(string? value)
    {
        if (value == null || value.Length == 0)
        {
            return false;
        }

        bool numeric = true;
        int numericIndex = 0;
        while (numericIndex < value.Length)
        {
            int numericCharacter = value[numericIndex];
            if (!IsAsciiDigit(numericCharacter) && numericCharacter != '.')
            {
                numeric = false;
                break;
            }

            numericIndex = numericIndex + 1;
        }

        if (numeric)
        {
            return IsIpv4Address(value);
        }

        int length = value.Length;
        if (value[length - 1] == '.')
        {
            length = length - 1;
        }

        if (length == 0)
        {
            return false;
        }

        int labelStart = 0;
        int index = 0;
        while (index <= length)
        {
            if (index == length || value[index] == '.')
            {
                int labelLength = index - labelStart;
                if (labelLength == 0 || labelLength > 63)
                {
                    return false;
                }

                if (!IsAsciiAlphaNumeric(value[labelStart]) || !IsAsciiAlphaNumeric(value[index - 1]))
                {
                    return false;
                }

                int labelIndex = labelStart;
                while (labelIndex < index)
                {
                    int character = value[labelIndex];
                    if (!IsAsciiAlphaNumeric(character) && character != '-')
                    {
                        return false;
                    }

                    labelIndex = labelIndex + 1;
                }

                labelStart = index + 1;
            }

            index = index + 1;
        }

        int topLabelStart = value.LastIndexOf('.', length - 1) + 1;
        return IsAsciiAlpha(value[topLabelStart]);
    }

    internal static bool IsIpv4Address(string? value)
    {
        if (value == null || value.Length == 0)
        {
            return false;
        }

        int partCount = 0;
        int partValue = 0;
        int partDigits = 0;
        int index = 0;
        while (index <= value.Length)
        {
            if (index == value.Length || value[index] == '.')
            {
                if (partDigits == 0 || partValue > 255)
                {
                    return false;
                }

                partCount = partCount + 1;
                partValue = 0;
                partDigits = 0;
            }
            else
            {
                if (!IsAsciiDigit(value[index]) || partDigits == 3)
                {
                    return false;
                }

                partValue = partValue * 10 + value[index] - '0';
                partDigits = partDigits + 1;
            }

            index = index + 1;
        }

        return partCount == 4;
    }

    internal static bool IsIpv6Address(string? value)
    {
        if (value == null || value.Length < 2)
        {
            return false;
        }

        int compression = value.IndexOf("::", System.StringComparison.Ordinal);
        if (compression >= 0 && value.IndexOf("::", compression + 2, System.StringComparison.Ordinal) >= 0)
        {
            return false;
        }

        if (compression < 0)
        {
            int fullCount = CountIpv6Groups(value, true);
            return fullCount == 8;
        }

        string left = value.Substring(0, compression);
        string right = value.Substring(compression + 2);
        int leftCount = CountIpv6Groups(left, right.Length == 0);
        int rightCount = CountIpv6Groups(right, true);
        return leftCount >= 0 && rightCount >= 0 && leftCount + rightCount < 8;
    }

    internal static bool IsTelephoneSubscriber(string? value)
    {
        if (value == null || value.Length == 0)
        {
            return false;
        }

        bool hasDialCharacter = false;
        bool hasDecimalDigit = false;
        bool global = value[0] == '+';
        int index = 0;
        while (index < value.Length)
        {
            int character = value[index];
            if (character == '+')
            {
                if (index != 0)
                {
                    return false;
                }
            }
            else if (IsAsciiDigit(character) || character >= 'A' && character <= 'F'
                || character >= 'a' && character <= 'f' || character == '*' || character == '#')
            {
                hasDialCharacter = true;
                if (IsAsciiDigit(character)) hasDecimalDigit = true;
            }
            else if (character != '-' && character != '.' && character != '(' && character != ')')
            {
                return false;
            }

            index = index + 1;
        }

        return hasDialCharacter && (!global || hasDecimalDigit);
    }

    internal static bool IsTelephoneParameterName(string? value)
    {
        if (value == null || value.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (!IsAsciiAlphaNumeric(value[index]) && value[index] != '-')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsTelephoneExtension(string? value)
    {
        if (value == null || value.Length == 0)
        {
            return false;
        }

        int index = 0;
        bool hasDigit = false;
        while (index < value.Length)
        {
            int character = value[index];
            if (IsAsciiDigit(character))
            {
                hasDigit = true;
            }
            else if (character != '-' && character != '.' && character != '(' && character != ')')
            {
                return false;
            }

            index = index + 1;
        }

        return hasDigit;
    }

    internal static bool HasValidPercentEscapes(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '%')
            {
                if (index + 2 >= value.Length || !IsHexDigit(value[index + 1]) || !IsHexDigit(value[index + 2]))
                {
                    return false;
                }

                index = index + 3;
            }
            else
            {
                index = index + 1;
            }
        }

        return true;
    }

    internal static bool IsAbsoluteUriCharacter(int value)
    {
        return IsUnreserved(value) || value == ';' || value == '/' || value == '?' || value == ':'
            || value == '@' || value == '&' || value == '=' || value == '+' || value == '$'
            || value == ',' || value == '[' || value == ']';
    }

    internal static bool IsVisibleAsciiString(string? value)
    {
        if (value == null)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < 33 || value[index] > 126)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsAsciiAlpha(int value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z';
    }

    internal static bool IsAsciiDigit(int value)
    {
        return value >= '0' && value <= '9';
    }

    internal static bool IsAsciiAlphaNumeric(int value)
    {
        return IsAsciiAlpha(value) || IsAsciiDigit(value);
    }

    internal static bool IsHexDigit(int value)
    {
        return IsAsciiDigit(value) || value >= 'A' && value <= 'F' || value >= 'a' && value <= 'f';
    }

    internal static int GetHexValue(int value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }

        if (value >= 'A' && value <= 'F')
        {
            return value - 'A' + 10;
        }

        if (value >= 'a' && value <= 'f')
        {
            return value - 'a' + 10;
        }

        return -1;
    }

    internal static bool IsUnreserved(int value)
    {
        return IsAsciiAlphaNumeric(value) || value == '-' || value == '_' || value == '.'
            || value == '!' || value == '~' || value == '*' || value == '\'' || value == '(' || value == ')';
    }

    static int CountIpv6Groups(string value, bool allowIpv4AtEnd)
    {
        if (value.Length == 0)
        {
            return 0;
        }

        int groupCount = 0;
        int groupStart = 0;
        int index = 0;
        while (index <= value.Length)
        {
            if (index == value.Length || value[index] == ':')
            {
                int groupLength = index - groupStart;
                if (groupLength == 0)
                {
                    return -1;
                }

                string group = value.Substring(groupStart, groupLength);
                if (group.IndexOf('.') >= 0)
                {
                    if (!allowIpv4AtEnd || index != value.Length || !IsIpv4Address(group))
                    {
                        return -1;
                    }

                    groupCount = groupCount + 2;
                }
                else
                {
                    if (groupLength > 4)
                    {
                        return -1;
                    }

                    int hexIndex = 0;
                    while (hexIndex < group.Length)
                    {
                        if (!IsHexDigit(group[hexIndex]))
                        {
                            return -1;
                        }

                        hexIndex = hexIndex + 1;
                    }

                    groupCount = groupCount + 1;
                }

                groupStart = index + 1;
            }

            index = index + 1;
        }

        return groupCount;
    }
}
