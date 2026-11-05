namespace asn1;

internal class XerXmlCharacters
{
    internal bool IsXmlCharacter(int value)
    {
        return value == 9 || value == 10 || value == 13
            || value >= 32 && value <= 55295
            || value >= 57344 && value <= 65533
            || value >= 65536 && value <= 1114111;
    }

    internal bool IsNameStart(int value)
    {
        return value == 58 || value == 95
            || value >= 65 && value <= 90
            || value >= 97 && value <= 122
            || value >= 192 && value <= 214
            || value >= 216 && value <= 246
            || value >= 248 && value <= 767
            || value >= 880 && value <= 893
            || value >= 895 && value <= 8191
            || value >= 8204 && value <= 8205
            || value >= 8304 && value <= 8591
            || value >= 11264 && value <= 12271
            || value >= 12289 && value <= 55295
            || value >= 63744 && value <= 64975
            || value >= 65008 && value <= 65533
            || value >= 65536 && value <= 983039;
    }

    internal bool IsNameCharacter(int value)
    {
        return this.IsNameStart(value)
            || value == 45 || value == 46
            || value >= 48 && value <= 57
            || value == 183
            || value >= 768 && value <= 879
            || value >= 8255 && value <= 8256;
    }

    internal bool IsValidName(string value)
    {
        if (value == null || value.Length == 0)
        {
            return false;
        }

        int index = 0;
        XerXmlScalar first = this.ReadScalar(value, index);
        if (!first.Success || !this.IsNameStart(first.Value))
        {
            return false;
        }

        index = index + first.CharacterCount;
        while (index < value.Length)
        {
            XerXmlScalar current = this.ReadScalar(value, index);
            if (!current.Success || !this.IsNameCharacter(current.Value))
            {
                return false;
            }

            index = index + current.CharacterCount;
        }

        return true;
    }

    internal bool IsValidNcName(string value)
    {
        if (!this.IsValidName(value))
        {
            return false;
        }

        return value.IndexOf(':') < 0;
    }

    internal bool IsValidText(string value)
    {
        if (value == null)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            XerXmlScalar current = this.ReadScalar(value, index);
            if (!current.Success || !this.IsXmlCharacter(current.Value))
            {
                return false;
            }

            index = index + current.CharacterCount;
        }

        return true;
    }

    internal XerXmlScalar ReadScalar(string value, int offset)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        if (offset < 0 || offset >= value.Length)
        {
            return new XerXmlScalar();
        }

        int first = value[offset];
        if (first < 55296 || first > 57343)
        {
            return new XerXmlScalar
            {
                Success = true,
                Value = first,
                CharacterCount = 1
            };
        }

        if (first > 56319 || offset + 1 >= value.Length)
        {
            return new XerXmlScalar();
        }

        int second = value[offset + 1];
        if (second < 56320 || second > 57343)
        {
            return new XerXmlScalar();
        }

        return new XerXmlScalar
        {
            Success = true,
            Value = 65536 + ((first - 55296) << 10) + second - 56320,
            CharacterCount = 2
        };
    }
}
