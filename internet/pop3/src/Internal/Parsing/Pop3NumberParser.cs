namespace pop3;

internal class Pop3NumberParseResult
{
    internal bool Success;
    internal long Value;
}

internal class Pop3NumberParser
{
    internal Pop3NumberParseResult ParseSignedDecimal(
        System.ReadOnlySpan<byte> text,
        long minimum,
        long maximum)
    {
        if (minimum > maximum)
        {
            throw new System.ArgumentOutOfRangeException("minimum");
        }

        Pop3NumberParseResult parsed = new Pop3NumberParseResult();
        if (text.Length == 0)
        {
            return parsed;
        }

        bool negative = false;
        int index = 0;
        if (text[0] == 45 || text[0] == 43)
        {
            negative = text[0] == 45;
            index = 1;
            if (index == text.Length)
            {
                return parsed;
            }
        }

        long limit = negative ? long.MinValue : -long.MaxValue;
        long multiplicationLimit = limit / 10;
        long value = 0;
        while (index < text.Length)
        {
            byte character = text[index];
            if (character < 48 || character > 57)
            {
                return parsed;
            }
            int digit = character - 48;
            if (value < multiplicationLimit)
            {
                return parsed;
            }
            value *= 10;
            if (value < limit + digit)
            {
                return parsed;
            }
            value -= digit;
            index++;
        }

        long finalValue = negative ? value : -value;
        if (finalValue < minimum || finalValue > maximum)
        {
            return parsed;
        }
        parsed.Success = true;
        parsed.Value = finalValue;
        return parsed;
    }
}
