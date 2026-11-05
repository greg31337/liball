namespace cookies;

internal static class DecimalSecondsParser
{
    internal static DecimalSecondsParseResult ParseMaxAgeSeconds(global::System.ReadOnlySpan<char> input)
    {
        DecimalSecondsParseResult result = new DecimalSecondsParseResult();
        if (input.Length == 0)
        {
            return result;
        }

        int index = 0;
        bool negative = false;
        if (input[0] == '-')
        {
            negative = true;
            index = 1;
        }

        if (index == input.Length)
        {
            return result;
        }

        long limit = negative ? long.MinValue : -long.MaxValue;
        long multiplicationLimit = limit / 10;
        long parsed = 0;
        bool saturated = false;
        while (index < input.Length)
        {
            char character = input[index];
            if (!CookieCharacterRules.IsAsciiDigit(character))
            {
                return new DecimalSecondsParseResult();
            }

            int digit = character - '0';
            if (!saturated)
            {
                if (parsed < multiplicationLimit)
                {
                    saturated = true;
                }
                else
                {
                    long multiplied = parsed * 10;
                    if (multiplied < limit + digit)
                    {
                        saturated = true;
                    }
                    else
                    {
                        parsed = multiplied - digit;
                    }
                }
            }

            index = index + 1;
        }

        result.Success = true;
        result.IsNegative = negative;
        result.Saturated = saturated;
        if (saturated)
        {
            result.Value = negative ? long.MinValue : long.MaxValue;
        }
        else
        {
            result.Value = negative ? parsed : -parsed;
        }

        return result;
    }

    internal static NumericLimitParseResult ParseNumericLimit(global::System.ReadOnlySpan<char> input)
    {
        NumericLimitParseResult result = new NumericLimitParseResult();
        if (input.Length == 0)
        {
            return result;
        }

        int index = 0;
        int parsed = 0;
        bool saturated = false;
        while (index < input.Length)
        {
            char character = input[index];
            if (!CookieCharacterRules.IsAsciiDigit(character))
            {
                return new NumericLimitParseResult();
            }

            int digit = character - '0';
            if (!saturated)
            {
                int multiplicationLimit = (int.MaxValue - digit) / 10;
                if (parsed > multiplicationLimit)
                {
                    saturated = true;
                }
                else
                {
                    parsed = parsed * 10 + digit;
                }
            }

            index = index + 1;
        }

        result.Success = true;
        result.Saturated = saturated;
        result.Value = saturated ? int.MaxValue : parsed;
        return result;
    }

    internal static DateTimeSaturationResult AddSecondsSaturated(global::System.DateTimeOffset origin, long seconds)
    {
        DateTimeSaturationResult result = new DateTimeSaturationResult();
        global::System.DateTimeOffset utcOrigin = origin.ToUniversalTime();
        long originTicks = utcOrigin.Ticks;
        long ticksPerSecond = global::System.TimeSpan.TicksPerSecond;
        if (seconds > 0)
        {
            long availableSeconds = (global::System.DateTimeOffset.MaxValue.Ticks - originTicks) / ticksPerSecond;
            if (seconds > availableSeconds)
            {
                result.Saturated = true;
                result.Value = global::System.DateTimeOffset.MaxValue;
                return result;
            }
        }
        else if (seconds < 0)
        {
            long availableSeconds = (originTicks - global::System.DateTimeOffset.MinValue.Ticks) / ticksPerSecond;
            long minimumSeconds = -availableSeconds;
            if (seconds < minimumSeconds)
            {
                result.Saturated = true;
                result.Value = global::System.DateTimeOffset.MinValue;
                return result;
            }
        }

        long resultTicks = originTicks + seconds * ticksPerSecond;
        result.Value = new global::System.DateTimeOffset(resultTicks, global::System.TimeSpan.Zero);
        return result;
    }
}
