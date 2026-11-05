using System;
using System.Globalization;

namespace iso20022;

internal static class DurationLexical
{
    internal static IsoDurationValue? ParseDuration(ReadOnlySpan<char> text)
    {
        if (text.Length < 2)
        {
            return null;
        }

        int index = 0;
        bool negative = false;
        if (text[index] == '-')
        {
            negative = true;
            index++;
        }

        if (index >= text.Length || text[index] != 'P')
        {
            return null;
        }

        index++;
        if (index == text.Length)
        {
            return null;
        }

        IsoDurationValue result = new IsoDurationValue();
        result.IsNegative = negative;
        bool inTime = false;
        bool hasComponent = false;
        bool hasTimeComponent = false;
        int lastDateOrder = 0;
        int lastTimeOrder = 0;
        while (index < text.Length)
        {
            if (text[index] == 'T')
            {
                if (inTime || index == text.Length - 1)
                {
                    return null;
                }

                inTime = true;
                index++;
                continue;
            }

            int numberStart = index;
            while (index < text.Length && text[index] >= '0' && text[index] <= '9')
            {
                index++;
            }

            if (numberStart == index)
            {
                return null;
            }

            ReadOnlySpan<char> numberText = text.Slice(numberStart, index - numberStart);
            IntegerLexicalResult number = IntegerLexical.ParseInt32(numberText);
            if (!number.Success)
            {
                return null;
            }

            string fractionalSecondDigits = string.Empty;
            if (index < text.Length && text[index] == '.')
            {
                index++;
                int fractionalStart = index;
                while (index < text.Length && text[index] >= '0' && text[index] <= '9')
                {
                    index++;
                }

                if (fractionalStart == index)
                {
                    return null;
                }

                fractionalSecondDigits = text.Slice(fractionalStart, index - fractionalStart).ToString();
            }

            if (index == text.Length)
            {
                return null;
            }

            char designator = text[index];
            index++;
            if (!inTime)
            {
                if (fractionalSecondDigits.Length != 0)
                {
                    return null;
                }

                if (designator == 'Y' && lastDateOrder < 1)
                {
                    result.Years = number.Value;
                    result.HasYears = true;
                    lastDateOrder = 1;
                }
                else if (designator == 'M' && lastDateOrder < 2)
                {
                    result.Months = number.Value;
                    result.HasMonths = true;
                    lastDateOrder = 2;
                }
                else if (designator == 'D' && lastDateOrder < 3)
                {
                    result.Days = number.Value;
                    result.HasDays = true;
                    lastDateOrder = 3;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                if (designator == 'H' && lastTimeOrder < 1 && fractionalSecondDigits.Length == 0)
                {
                    result.Hours = number.Value;
                    result.HasHours = true;
                    lastTimeOrder = 1;
                }
                else if (designator == 'M' && lastTimeOrder < 2 && fractionalSecondDigits.Length == 0)
                {
                    result.Minutes = number.Value;
                    result.HasMinutes = true;
                    lastTimeOrder = 2;
                }
                else if (designator == 'S' && lastTimeOrder < 3)
                {
                    result.Seconds = number.Value;
                    result.HasSeconds = true;
                    result.FractionalSecondDigits = fractionalSecondDigits;
                    lastTimeOrder = 3;
                }
                else
                {
                    return null;
                }

                hasTimeComponent = true;
            }

            hasComponent = true;
        }

        if (!hasComponent || (inTime && !hasTimeComponent))
        {
            return null;
        }

        result.LexicalValue = text.ToString();
        return result;
    }

    internal static string? FormatDuration(IsoDurationValue value)
    {
        if (value == null || value.Years < 0 || value.Months < 0 || value.Days < 0 || value.Hours < 0 || value.Minutes < 0 || value.Seconds < 0)
        {
            return null;
        }

        if (value.FractionalSecondDigits.Length > 0 && !DateTimeLexicalHelpers.IsAsciiDigits(value.FractionalSecondDigits.AsSpan()))
        {
            return null;
        }

        bool includeYears = value.HasYears || value.Years != 0;
        bool includeMonths = value.HasMonths || value.Months != 0;
        bool includeDays = value.HasDays || value.Days != 0;
        bool includeHours = value.HasHours || value.Hours != 0;
        bool includeMinutes = value.HasMinutes || value.Minutes != 0;
        bool includeSeconds = value.HasSeconds || value.Seconds != 0 || value.FractionalSecondDigits.Length != 0;
        string result = value.IsNegative ? "-P" : "P";
        if (includeYears)
        {
            result = result + value.Years.ToString(CultureInfo.InvariantCulture) + "Y";
        }

        if (includeMonths)
        {
            result = result + value.Months.ToString(CultureInfo.InvariantCulture) + "M";
        }

        if (includeDays)
        {
            result = result + value.Days.ToString(CultureInfo.InvariantCulture) + "D";
        }

        if (includeHours || includeMinutes || includeSeconds)
        {
            result = result + "T";
            if (includeHours)
            {
                result = result + value.Hours.ToString(CultureInfo.InvariantCulture) + "H";
            }

            if (includeMinutes)
            {
                result = result + value.Minutes.ToString(CultureInfo.InvariantCulture) + "M";
            }

            if (includeSeconds)
            {
                result = result + value.Seconds.ToString(CultureInfo.InvariantCulture);
                if (value.FractionalSecondDigits.Length > 0)
                {
                    result = result + "." + value.FractionalSecondDigits;
                }

                result = result + "S";
            }
        }

        if (!includeYears && !includeMonths && !includeDays && !includeHours && !includeMinutes && !includeSeconds)
        {
            result = result + "T0S";
        }

        return result;
    }
}
