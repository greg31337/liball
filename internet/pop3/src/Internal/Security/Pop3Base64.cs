namespace pop3;

internal class Pop3Base64
{
    internal const string CancellationMarker = "*";
    internal const string EmptyInitialResponseMarker = "=";

    internal string Encode(byte[] value)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        return System.Convert.ToBase64String(value);
    }

    internal string EncodeInitialResponse(byte[] value)
    {
        string encoded = Encode(value);
        if (encoded.Length == 0)
        {
            return EmptyInitialResponseMarker;
        }
        return encoded;
    }

    internal byte[] Decode(string value)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        if (!IsValid(value))
        {
            throw new System.IO.InvalidDataException("A POP3 SASL payload is not valid canonical Base64.");
        }
        return System.Convert.FromBase64String(value);
    }

    internal byte[] DecodeInitialResponse(string value)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }
        if (value == EmptyInitialResponseMarker)
        {
            return new byte[0];
        }
        if (value.Length == 0)
        {
            throw new System.IO.InvalidDataException("A POP3 SASL initial response cannot be an empty token.");
        }
        return Decode(value);
    }

    internal bool IsCancellationMarker(string value)
    {
        return value == CancellationMarker;
    }

    internal bool IsValid(string value)
    {
        if (value == null)
        {
            return false;
        }
        if (value.Length == 0)
        {
            return true;
        }
        if (value.Length % 4 != 0)
        {
            return false;
        }

        int paddingCount = 0;
        if (value[value.Length - 1] == '=')
        {
            paddingCount = 1;
            if (value.Length > 1 && value[value.Length - 2] == '=')
            {
                paddingCount = 2;
            }
        }

        int dataLength = value.Length - paddingCount;
        int index = 0;
        while (index < dataLength)
        {
            if (!IsAlphabetCharacter(value[index]))
            {
                return false;
            }
            index++;
        }
        while (index < value.Length)
        {
            if (value[index] != '=')
            {
                return false;
            }
            index++;
        }

        if (paddingCount == 1 && (GetAlphabetValue(value[dataLength - 1]) & 3) != 0)
        {
            return false;
        }
        if (paddingCount == 2 && (GetAlphabetValue(value[dataLength - 1]) & 15) != 0)
        {
            return false;
        }
        return true;
    }

    bool IsAlphabetCharacter(char value)
    {
        return value >= 'A' && value <= 'Z' ||
            value >= 'a' && value <= 'z' ||
            value >= '0' && value <= '9' ||
            value == '+' ||
            value == '/';
    }

    int GetAlphabetValue(char value)
    {
        if (value >= 'A' && value <= 'Z')
        {
            return value - 'A';
        }
        if (value >= 'a' && value <= 'z')
        {
            return value - 'a' + 26;
        }
        if (value >= '0' && value <= '9')
        {
            return value - '0' + 52;
        }
        if (value == '+')
        {
            return 62;
        }
        return 63;
    }
}
