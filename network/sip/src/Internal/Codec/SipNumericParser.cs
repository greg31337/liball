namespace sip;

internal static class SipNumericParser
{
    internal static SipNumericParseResult ParseNonNegativeInt32Text(string? text, int maximumValue)
    {
        if (maximumValue < 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumValue");
        }

        if (text == null || text.Length == 0)
        {
            return new SipNumericParseResult(false, 0, 0, 0);
        }

        int value = 0;
        int index = 0;
        while (index < text.Length)
        {
            int digit = text[index] - '0';
            if (digit < 0 || digit > 9)
            {
                return new SipNumericParseResult(false, 0, index, index);
            }

            if (value > maximumValue / 10 || value == maximumValue / 10 && digit > maximumValue % 10)
            {
                return new SipNumericParseResult(false, 0, index, index);
            }

            value = value * 10 + digit;
            index = index + 1;
        }

        return new SipNumericParseResult(true, value, index, -1);
    }

    internal static SipNumericParseResult ParseNonNegativeInt32Bytes(byte[]? bytes, int maximumValue)
    {
        if (maximumValue < 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumValue");
        }

        if (bytes == null || bytes.Length == 0)
        {
            return new SipNumericParseResult(false, 0, 0, 0);
        }

        int value = 0;
        int index = 0;
        while (index < bytes.Length)
        {
            int digit = bytes[index] - (byte)'0';
            if (digit < 0 || digit > 9)
            {
                return new SipNumericParseResult(false, 0, index, index);
            }

            if (value > maximumValue / 10 || value == maximumValue / 10 && digit > maximumValue % 10)
            {
                return new SipNumericParseResult(false, 0, index, index);
            }

            value = value * 10 + digit;
            index = index + 1;
        }

        return new SipNumericParseResult(true, value, index, -1);
    }
}
