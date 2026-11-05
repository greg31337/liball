namespace cookies;

internal static class CookieDateParser
{
    internal static CookieDateParseResult ParseCookieDate(global::System.ReadOnlySpan<char> input)
    {
        bool foundTime = false;
        bool foundDay = false;
        bool foundMonth = false;
        bool foundYear = false;
        int hour = 0;
        int minute = 0;
        int second = 0;
        int day = 0;
        int month = 0;
        int year = 0;
        int offset = 0;
        while (offset < input.Length)
        {
            CookieDateTokenResult tokenResult = CookieDateTokenizer.ReadNextDateToken(input, offset);
            if (!tokenResult.HasToken)
            {
                break;
            }

            global::System.ReadOnlySpan<char> token = input.Slice(tokenResult.Start, tokenResult.Length);
            bool recognized = false;
            if (!foundTime)
            {
                CookieDateComponentResult timeResult = MatchTime(token);
                if (timeResult.Success)
                {
                    foundTime = true;
                    hour = timeResult.First;
                    minute = timeResult.Second;
                    second = timeResult.Third;
                    recognized = true;
                }
            }

            if (!recognized && !foundDay)
            {
                CookieDateComponentResult dayResult = MatchDayOfMonth(token);
                if (dayResult.Success)
                {
                    foundDay = true;
                    day = dayResult.First;
                    recognized = true;
                }
            }

            if (!recognized && !foundMonth)
            {
                CookieDateComponentResult monthResult = MatchMonth(token);
                if (monthResult.Success)
                {
                    foundMonth = true;
                    month = monthResult.First;
                    recognized = true;
                }
            }

            if (!recognized && !foundYear)
            {
                CookieDateComponentResult yearResult = MatchYear(token);
                if (yearResult.Success)
                {
                    foundYear = true;
                    year = yearResult.First;
                }
            }

            offset = tokenResult.NextOffset;
        }

        CookieDateParseResult result = new CookieDateParseResult();
        if (!foundTime || !foundDay || !foundMonth || !foundYear)
        {
            return result;
        }

        if (year >= 70 && year <= 99)
        {
            year = year + 1900;
        }
        else if (year >= 0 && year <= 69)
        {
            year = year + 2000;
        }

        if (year < 1601 || year > 9999 || day < 1 || day > DaysInMonth(year, month) ||
            hour > 23 || minute > 59 || second > 59)
        {
            return result;
        }

        result.Value = new global::System.DateTimeOffset(
            year,
            month,
            day,
            hour,
            minute,
            second,
            global::System.TimeSpan.Zero);
        result.Success = true;
        return result;
    }

    static CookieDateComponentResult MatchTime(global::System.ReadOnlySpan<char> token)
    {
        CookieDateComponentResult result = new CookieDateComponentResult();
        CookieDateFieldResult hour = ReadTimeField(token, 0);
        if (!hour.Success || hour.NextOffset >= token.Length || token[hour.NextOffset] != ':')
        {
            return result;
        }

        CookieDateFieldResult minute = ReadTimeField(token, hour.NextOffset + 1);
        if (!minute.Success || minute.NextOffset >= token.Length || token[minute.NextOffset] != ':')
        {
            return result;
        }

        CookieDateFieldResult second = ReadTimeField(token, minute.NextOffset + 1);
        if (!second.Success || !HasValidNonDigitSuffix(token, second.NextOffset))
        {
            return result;
        }

        result.Success = true;
        result.First = hour.Value;
        result.Second = minute.Value;
        result.Third = second.Value;
        return result;
    }

    static CookieDateComponentResult MatchDayOfMonth(global::System.ReadOnlySpan<char> token)
    {
        CookieDateComponentResult result = new CookieDateComponentResult();
        CookieDateFieldResult number = ReadLeadingNumber(token, 1, 2);
        if (!number.Success || !HasValidNonDigitSuffix(token, number.NextOffset))
        {
            return result;
        }

        result.Success = true;
        result.First = number.Value;
        return result;
    }

    static CookieDateComponentResult MatchMonth(global::System.ReadOnlySpan<char> token)
    {
        CookieDateComponentResult result = new CookieDateComponentResult();
        if (token.Length < 3 || !HasOctetSuffix(token, 3))
        {
            return result;
        }

        char first = ToAsciiLower(token[0]);
        char second = ToAsciiLower(token[1]);
        char third = ToAsciiLower(token[2]);
        int month = 0;
        if (first == 'j' && second == 'a' && third == 'n')
        {
            month = 1;
        }
        else if (first == 'f' && second == 'e' && third == 'b')
        {
            month = 2;
        }
        else if (first == 'm' && second == 'a' && third == 'r')
        {
            month = 3;
        }
        else if (first == 'a' && second == 'p' && third == 'r')
        {
            month = 4;
        }
        else if (first == 'm' && second == 'a' && third == 'y')
        {
            month = 5;
        }
        else if (first == 'j' && second == 'u' && third == 'n')
        {
            month = 6;
        }
        else if (first == 'j' && second == 'u' && third == 'l')
        {
            month = 7;
        }
        else if (first == 'a' && second == 'u' && third == 'g')
        {
            month = 8;
        }
        else if (first == 's' && second == 'e' && third == 'p')
        {
            month = 9;
        }
        else if (first == 'o' && second == 'c' && third == 't')
        {
            month = 10;
        }
        else if (first == 'n' && second == 'o' && third == 'v')
        {
            month = 11;
        }
        else if (first == 'd' && second == 'e' && third == 'c')
        {
            month = 12;
        }

        if (month == 0)
        {
            return result;
        }

        result.Success = true;
        result.First = month;
        return result;
    }

    static CookieDateComponentResult MatchYear(global::System.ReadOnlySpan<char> token)
    {
        CookieDateComponentResult result = new CookieDateComponentResult();
        CookieDateFieldResult number = ReadLeadingNumber(token, 2, 4);
        if (!number.Success || !HasValidNonDigitSuffix(token, number.NextOffset))
        {
            return result;
        }

        result.Success = true;
        result.First = number.Value;
        return result;
    }

    static CookieDateFieldResult ReadTimeField(global::System.ReadOnlySpan<char> token, int start)
    {
        return ReadLeadingNumberAtOffset(token, start, 1, 2);
    }

    static CookieDateFieldResult ReadLeadingNumber(global::System.ReadOnlySpan<char> token, int minimumDigits, int maximumDigits)
    {
        return ReadLeadingNumberAtOffset(token, 0, minimumDigits, maximumDigits);
    }

    static CookieDateFieldResult ReadLeadingNumberAtOffset(
        global::System.ReadOnlySpan<char> token,
        int start,
        int minimumDigits,
        int maximumDigits)
    {
        CookieDateFieldResult result = new CookieDateFieldResult();
        int index = start;
        int value = 0;
        int digits = 0;
        while (index < token.Length && digits < maximumDigits && CookieCharacterRules.IsAsciiDigit(token[index]))
        {
            value = value * 10 + token[index] - '0';
            digits = digits + 1;
            index = index + 1;
        }

        if (digits < minimumDigits)
        {
            return result;
        }

        result.Success = true;
        result.Value = value;
        result.NextOffset = index;
        return result;
    }

    static bool HasValidNonDigitSuffix(global::System.ReadOnlySpan<char> token, int start)
    {
        if (start == token.Length)
        {
            return true;
        }

        if (CookieCharacterRules.IsAsciiDigit(token[start]))
        {
            return false;
        }

        return HasOctetSuffix(token, start);
    }

    static bool HasOctetSuffix(global::System.ReadOnlySpan<char> token, int start)
    {
        int index = start;
        while (index < token.Length)
        {
            if (token[index] > (char)0xff)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static char ToAsciiLower(char character)
    {
        if (character >= 'A' && character <= 'Z')
        {
            return (char)(character + ('a' - 'A'));
        }

        return character;
    }

    static int DaysInMonth(int year, int month)
    {
        if (month == 2)
        {
            bool leapYear = year % 400 == 0 || year % 4 == 0 && year % 100 != 0;
            return leapYear ? 29 : 28;
        }

        if (month == 4 || month == 6 || month == 9 || month == 11)
        {
            return 30;
        }

        if (month >= 1 && month <= 12)
        {
            return 31;
        }

        return 0;
    }
}
