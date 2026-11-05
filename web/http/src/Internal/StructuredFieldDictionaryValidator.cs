namespace http;

internal class StructuredFieldDictionaryValidator
{
    const int MaximumTopMembers = 1024;
    const int MaximumInnerMembers = 256;
    const int MaximumParameters = 256;

    byte[] input;
    int position;

    internal StructuredFieldDictionaryValidator(byte[] value)
    {
        input = value;
    }

    internal bool IsValidDictionary()
    {
        SkipLeadingSpaces();
        if (position == input.Length)
        {
            return true;
        }

        int members = 0;
        while (position < input.Length)
        {
            members = members + 1;
            if (members > MaximumTopMembers || !ReadKey())
            {
                return false;
            }

            if (position < input.Length && input[position] == 0x3d)
            {
                position = position + 1;
                if (!ReadItemOrInnerList())
                {
                    return false;
                }
            }
            else if (!ReadParameters())
            {
                return false;
            }

            SkipOptionalWhitespace();
            if (position == input.Length)
            {
                return true;
            }

            if (input[position] != 0x2c)
            {
                return false;
            }

            position = position + 1;
            SkipOptionalWhitespace();
            if (position == input.Length)
            {
                return false;
            }
        }

        return true;
    }

    bool ReadItemOrInnerList()
    {
        if (position < input.Length && input[position] == 0x28)
        {
            return ReadInnerList();
        }

        return ReadItem();
    }

    bool ReadInnerList()
    {
        position = position + 1;
        int members = 0;
        while (position < input.Length)
        {
            SkipLeadingSpaces();
            if (position < input.Length && input[position] == 0x29)
            {
                position = position + 1;
                return ReadParameters();
            }

            members = members + 1;
            if (members > MaximumInnerMembers || !ReadItem())
            {
                return false;
            }

            if (position >= input.Length ||
                input[position] != 0x20 && input[position] != 0x29)
            {
                return false;
            }
        }

        return false;
    }

    bool ReadItem()
    {
        if (!ReadBareItem())
        {
            return false;
        }

        return ReadParameters();
    }

    bool ReadBareItem()
    {
        if (position >= input.Length)
        {
            return false;
        }

        byte first = input[position];
        if (first == 0x2d || IsDigit(first))
        {
            return ReadNumber();
        }

        if (first == 0x22)
        {
            return ReadString();
        }

        if (IsAlpha(first) || first == 0x2a)
        {
            return ReadToken();
        }

        if (first == 0x3a)
        {
            return ReadByteSequence();
        }

        if (first == 0x3f)
        {
            return ReadBoolean();
        }

        if (first == 0x40)
        {
            return ReadDate();
        }

        if (first == 0x25)
        {
            return ReadDisplayString();
        }

        return false;
    }

    bool ReadNumber()
    {
        if (input[position] == 0x2d)
        {
            position = position + 1;
        }

        int integerStart = position;
        while (position < input.Length && IsDigit(input[position]))
        {
            position = position + 1;
            if (position - integerStart > 15)
            {
                return false;
            }
        }

        int integerDigits = position - integerStart;
        if (integerDigits == 0)
        {
            return false;
        }

        if (position >= input.Length || input[position] != 0x2e)
        {
            return true;
        }

        if (integerDigits > 12)
        {
            return false;
        }

        position = position + 1;
        int fractionStart = position;
        while (position < input.Length && IsDigit(input[position]))
        {
            position = position + 1;
            if (position - fractionStart > 3)
            {
                return false;
            }
        }

        return position - fractionStart > 0;
    }

    bool ReadString()
    {
        position = position + 1;
        while (position < input.Length)
        {
            byte character = input[position];
            position = position + 1;
            if (character == 0x5c)
            {
                if (position >= input.Length ||
                    input[position] != 0x22 && input[position] != 0x5c)
                {
                    return false;
                }

                position = position + 1;
            }
            else if (character == 0x22)
            {
                return true;
            }
            else if (character < 0x20 || character > 0x7e)
            {
                return false;
            }
        }

        return false;
    }

    bool ReadToken()
    {
        position = position + 1;
        while (position < input.Length && IsTokenCharacter(input[position]))
        {
            position = position + 1;
        }

        return true;
    }

    bool ReadByteSequence()
    {
        position = position + 1;
        int contentStart = position;
        while (position < input.Length && input[position] != 0x3a)
        {
            if (!IsBase64Character(input[position]))
            {
                return false;
            }

            position = position + 1;
        }

        if (position >= input.Length)
        {
            return false;
        }

        int contentEnd = position;
        position = position + 1;
        return IsValidBase64(contentStart, contentEnd);
    }

    bool ReadBoolean()
    {
        position = position + 1;
        if (position >= input.Length ||
            input[position] != 0x30 && input[position] != 0x31)
        {
            return false;
        }

        position = position + 1;
        return true;
    }

    bool ReadDate()
    {
        position = position + 1;
        if (position < input.Length && input[position] == 0x2d)
        {
            position = position + 1;
        }

        int start = position;
        while (position < input.Length && IsDigit(input[position]))
        {
            position = position + 1;
            if (position - start > 15)
            {
                return false;
            }
        }

        return position - start > 0;
    }

    bool ReadDisplayString()
    {
        if (position + 1 >= input.Length || input[position + 1] != 0x22)
        {
            return false;
        }

        position = position + 2;
        byte[] decoded = new byte[input.Length - position];
        int decodedLength = 0;
        while (position < input.Length)
        {
            byte character = input[position];
            position = position + 1;
            if (character < 0x20 || character > 0x7e)
            {
                return false;
            }

            if (character == 0x25)
            {
                if (position + 1 >= input.Length ||
                    !IsLowerHex(input[position]) ||
                    !IsLowerHex(input[position + 1]))
                {
                    return false;
                }

                decoded[decodedLength] = (byte)(
                    GetHexValue(input[position]) * 16 +
                    GetHexValue(input[position + 1]));
                decodedLength = decodedLength + 1;
                position = position + 2;
            }
            else if (character == 0x22)
            {
                return IsValidUtf8(decoded, decodedLength);
            }
            else
            {
                decoded[decodedLength] = character;
                decodedLength = decodedLength + 1;
            }
        }

        return false;
    }

    bool ReadParameters()
    {
        int parameters = 0;
        while (position < input.Length && input[position] == 0x3b)
        {
            parameters = parameters + 1;
            if (parameters > MaximumParameters)
            {
                return false;
            }

            position = position + 1;
            SkipLeadingSpaces();
            if (!ReadKey())
            {
                return false;
            }

            if (position < input.Length && input[position] == 0x3d)
            {
                position = position + 1;
                if (!ReadBareItem())
                {
                    return false;
                }
            }
        }

        return true;
    }

    bool ReadKey()
    {
        if (position >= input.Length || !IsKeyFirst(input[position]))
        {
            return false;
        }

        position = position + 1;
        while (position < input.Length && IsKeyCharacter(input[position]))
        {
            position = position + 1;
        }

        return true;
    }

    bool IsValidBase64(int start, int end)
    {
        int paddingStart = end;
        int index = start;
        while (index < end)
        {
            if (input[index] == 0x3d)
            {
                paddingStart = index;
                break;
            }

            index = index + 1;
        }

        index = paddingStart;
        while (index < end)
        {
            if (input[index] != 0x3d)
            {
                return false;
            }

            index = index + 1;
        }

        int coreLength = paddingStart - start;
        int suppliedPadding = end - paddingStart;
        int remainder = coreLength % 4;
        if (remainder == 1)
        {
            return false;
        }

        int requiredPadding = remainder == 0 ? 0 : 4 - remainder;
        return suppliedPadding <= requiredPadding;
    }

    void SkipLeadingSpaces()
    {
        while (position < input.Length && input[position] == 0x20)
        {
            position = position + 1;
        }
    }

    void SkipOptionalWhitespace()
    {
        while (position < input.Length &&
            (input[position] == 0x20 || input[position] == 0x09))
        {
            position = position + 1;
        }
    }

    static bool IsAlpha(byte value)
    {
        return value >= 0x41 && value <= 0x5a ||
            value >= 0x61 && value <= 0x7a;
    }

    static bool IsDigit(byte value)
    {
        return value >= 0x30 && value <= 0x39;
    }

    static bool IsKeyFirst(byte value)
    {
        return value >= 0x61 && value <= 0x7a || value == 0x2a;
    }

    static bool IsKeyCharacter(byte value)
    {
        return IsKeyFirst(value) || IsDigit(value) || value == 0x5f ||
            value == 0x2d || value == 0x2e;
    }

    static bool IsTokenCharacter(byte value)
    {
        return HttpGrammar.IsTokenCharacter(value) || value == 0x3a ||
            value == 0x2f;
    }

    static bool IsBase64Character(byte value)
    {
        return IsAlpha(value) || IsDigit(value) || value == 0x2b ||
            value == 0x2f || value == 0x3d;
    }

    static bool IsLowerHex(byte value)
    {
        return IsDigit(value) || value >= 0x61 && value <= 0x66;
    }

    static int GetHexValue(byte value)
    {
        if (IsDigit(value))
        {
            return value - 0x30;
        }

        return value - 0x61 + 10;
    }

    static bool IsValidUtf8(byte[] value, int length)
    {
        int index = 0;
        while (index < length)
        {
            byte first = value[index];
            if (first <= 0x7f)
            {
                index = index + 1;
                continue;
            }

            if (first >= 0xc2 && first <= 0xdf)
            {
                if (index + 1 >= length ||
                    !IsContinuation(value[index + 1]))
                {
                    return false;
                }

                index = index + 2;
                continue;
            }

            if (first >= 0xe0 && first <= 0xef)
            {
                if (index + 2 >= length ||
                    !IsContinuation(value[index + 1]) ||
                    !IsContinuation(value[index + 2]))
                {
                    return false;
                }

                if (first == 0xe0 && value[index + 1] < 0xa0 ||
                    first == 0xed && value[index + 1] > 0x9f)
                {
                    return false;
                }

                index = index + 3;
                continue;
            }

            if (first >= 0xf0 && first <= 0xf4)
            {
                if (index + 3 >= length ||
                    !IsContinuation(value[index + 1]) ||
                    !IsContinuation(value[index + 2]) ||
                    !IsContinuation(value[index + 3]))
                {
                    return false;
                }

                if (first == 0xf0 && value[index + 1] < 0x90 ||
                    first == 0xf4 && value[index + 1] > 0x8f)
                {
                    return false;
                }

                index = index + 4;
                continue;
            }

            return false;
        }

        return true;
    }

    static bool IsContinuation(byte value)
    {
        return value >= 0x80 && value <= 0xbf;
    }
}
