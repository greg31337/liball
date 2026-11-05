namespace asn1;

internal class TimeValueValidator
{
    internal string ErrorMessage = "";

    internal TimeValidationResult ValidateTime(AsnTimeValue? value)
    {
        if (value == null || value.Text.Length == 0)
        {
            return this.CreateTimeFailure("A useful or time value cannot be empty.");
        }

        bool valid = false;
        switch (value.Kind)
        {
            case AsnTimeKind.UtcTime:
                valid = this.ValidateUtcTimeText(value.Text);
                break;
            case AsnTimeKind.GeneralizedTime:
                valid = this.ValidateGeneralizedTimeText(value.Text);
                break;
            case AsnTimeKind.Date:
                valid = this.ValidateDateText(value.Text);
                break;
            case AsnTimeKind.TimeOfDay:
                valid = this.ValidateTimeOfDayText(value.Text);
                break;
            case AsnTimeKind.DateTime:
                valid = this.ValidateDateTimeText(value.Text);
                break;
            case AsnTimeKind.Duration:
                valid = this.ValidateDurationText(value.Text);
                break;
            case AsnTimeKind.Time:
                valid = this.ValidateFlexibleTimeText(value.Text);
                break;
            default:
                this.ErrorMessage = "The useful or time value category is not recognized.";
                break;
        }

        if (valid && !value.IsValid())
        {
            valid = false;
            this.ErrorMessage = "The useful or time value metadata is inconsistent with its lexical value.";
        }

        return valid
            ? new TimeValidationResult { Success = true }
            : this.CreateTimeFailure(this.ErrorMessage.Length == 0
                ? "The useful or time value is not valid."
                : this.ErrorMessage);
    }

    internal bool ValidateUtcTimeText(string text)
    {
        TimeZoneParseResult zone = this.SeparateTimeZone(text, 10);
        if (!zone.Success
            || (zone.Body.Length != 10 && zone.Body.Length != 12)
            || !this.AreDecimalDigits(zone.Body, 0, zone.Body.Length))
        {
            this.ErrorMessage = "UTCTime requires YYMMDDhhmm[ss] with an optional valid UTC designator or difference.";
            return false;
        }

        int shortYear = this.ParseSmallNumber(zone.Body, 0, 2);
        int year = shortYear >= 50 ? 1900 + shortYear : 2000 + shortYear;
        int month = this.ParseSmallNumber(zone.Body, 2, 2);
        int day = this.ParseSmallNumber(zone.Body, 4, 2);
        int hour = this.ParseSmallNumber(zone.Body, 6, 2);
        int minute = this.ParseSmallNumber(zone.Body, 8, 2);
        int second = zone.Body.Length == 12
            ? this.ParseSmallNumber(zone.Body, 10, 2)
            : 0;
        return this.ValidateCalendarAndClock(
            year,
            month,
            day,
            hour,
            minute,
            second,
            zone.Body.Length == 12);
    }

    internal bool ValidateGeneralizedTimeText(string text)
    {
        TimeZoneParseResult zone = this.SeparateTimeZone(text, 10);
        if (!zone.Success)
        {
            this.ErrorMessage = "GeneralizedTime contains an invalid UTC designator or difference.";
            return false;
        }

        string integral = zone.Body;
        int fraction = this.FindFractionSeparator(integral);
        if (fraction >= 0)
        {
            if (fraction + 1 >= integral.Length
                || !this.AreDecimalDigits(
                    integral,
                    fraction + 1,
                    integral.Length - fraction - 1))
            {
                this.ErrorMessage = "GeneralizedTime has an invalid fractional part.";
                return false;
            }

            integral = integral.Substring(0, fraction);
        }

        if ((integral.Length != 10
                && integral.Length != 12
                && integral.Length != 14)
            || !this.AreDecimalDigits(integral, 0, integral.Length))
        {
            this.ErrorMessage = "GeneralizedTime requires YYYYMMDDHH[MM[SS]][.fraction].";
            return false;
        }

        int year = this.ParseSmallNumber(integral, 0, 4);
        int month = this.ParseSmallNumber(integral, 4, 2);
        int day = this.ParseSmallNumber(integral, 6, 2);
        int hour = this.ParseSmallNumber(integral, 8, 2);
        int minute = integral.Length >= 12
            ? this.ParseSmallNumber(integral, 10, 2)
            : 0;
        int second = integral.Length == 14
            ? this.ParseSmallNumber(integral, 12, 2)
            : 0;
        return this.ValidateCalendarAndClock(
            year,
            month,
            day,
            hour,
            minute,
            second,
            integral.Length == 14);
    }

    internal bool ValidateDateText(string text)
    {
        if (text.Length != 10
            || text[4] != '-'
            || text[7] != '-'
            || !this.AreDecimalDigits(text, 0, 4)
            || !this.AreDecimalDigits(text, 5, 2)
            || !this.AreDecimalDigits(text, 8, 2))
        {
            this.ErrorMessage = "DATE requires YYYY-MM-DD notation.";
            return false;
        }

        int year = this.ParseSmallNumber(text, 0, 4);
        int month = this.ParseSmallNumber(text, 5, 2);
        int day = this.ParseSmallNumber(text, 8, 2);
        if (!this.ValidateCalendarDate(year, month, day))
        {
            this.ErrorMessage = "DATE contains a day that does not exist in its calendar month.";
            return false;
        }

        return true;
    }

    internal bool ValidateTimeOfDayText(string text)
    {
        TimeZoneParseResult zone = this.SeparateTimeZone(text, 8);
        if (!zone.Success)
        {
            this.ErrorMessage = "TIME-OF-DAY contains an invalid UTC designator or difference.";
            return false;
        }

        string integral = zone.Body;
        int fraction = this.FindFractionSeparator(integral);
        if (fraction >= 0)
        {
            if (fraction + 1 >= integral.Length
                || !this.AreDecimalDigits(
                    integral,
                    fraction + 1,
                    integral.Length - fraction - 1))
            {
                this.ErrorMessage = "TIME-OF-DAY has an invalid fractional part.";
                return false;
            }

            integral = integral.Substring(0, fraction);
        }

        if (integral.Length != 8
            || integral[2] != ':'
            || integral[5] != ':'
            || !this.AreDecimalDigits(integral, 0, 2)
            || !this.AreDecimalDigits(integral, 3, 2)
            || !this.AreDecimalDigits(integral, 6, 2))
        {
            this.ErrorMessage = "TIME-OF-DAY requires hh:mm:ss[.fraction] notation.";
            return false;
        }

        int hour = this.ParseSmallNumber(integral, 0, 2);
        int minute = this.ParseSmallNumber(integral, 3, 2);
        int second = this.ParseSmallNumber(integral, 6, 2);
        if (!this.ValidateClockTime(hour, minute, second, true))
        {
            this.ErrorMessage = "TIME-OF-DAY contains an out-of-range clock component.";
            return false;
        }

        return true;
    }

    internal bool ValidateDateTimeText(string text)
    {
        if (text.Length <= 11 || text[10] != 'T')
        {
            this.ErrorMessage = "DATE-TIME requires a date and time separated by 'T'.";
            return false;
        }

        if (!this.ValidateDateText(text.Substring(0, 10)))
        {
            return false;
        }

        if (!this.ValidateTimeOfDayText(text.Substring(11)))
        {
            return false;
        }

        return true;
    }

    internal bool ValidateDurationText(string text)
    {
        int index = 0;
        if (text.Length > 0 && text[0] == '-')
        {
            index = 1;
        }

        if (index >= text.Length || text[index] != 'P')
        {
            this.ErrorMessage = "DURATION requires ISO 8601 P notation.";
            return false;
        }

        index = index + 1;
        bool inTime = false;
        bool sawComponent = false;
        bool sawTimeComponent = false;
        bool sawWeek = false;
        int lastOrder = 0;
        while (index < text.Length)
        {
            if (text[index] == 'T')
            {
                if (inTime)
                {
                    this.ErrorMessage = "DURATION contains more than one time separator.";
                    return false;
                }

                inTime = true;
                index = index + 1;
                continue;
            }

            int numberStart = index;
            while (index < text.Length && this.IsDecimalDigit(text[index]))
            {
                index = index + 1;
            }

            if (numberStart == index)
            {
                this.ErrorMessage = "DURATION components require a numeric value.";
                return false;
            }

            bool fractional = false;
            if (index < text.Length && (text[index] == '.' || text[index] == ','))
            {
                fractional = true;
                index = index + 1;
                int fractionStart = index;
                while (index < text.Length && this.IsDecimalDigit(text[index]))
                {
                    index = index + 1;
                }

                if (fractionStart == index)
                {
                    this.ErrorMessage = "DURATION has an empty fractional component.";
                    return false;
                }
            }

            if (index >= text.Length)
            {
                this.ErrorMessage = "DURATION components require a unit designator.";
                return false;
            }

            char designator = text[index];
            index = index + 1;
            int order = this.GetDurationComponentOrder(designator, inTime);
            if (order == 0 || order <= lastOrder)
            {
                this.ErrorMessage = "DURATION components are duplicated or out of order.";
                return false;
            }

            if (designator == 'W')
            {
                sawWeek = true;
            }

            if (sawWeek && (sawComponent || index < text.Length))
            {
                this.ErrorMessage = "A week DURATION cannot be combined with other components.";
                return false;
            }

            if (fractional && index != text.Length)
            {
                this.ErrorMessage = "Only the final DURATION component can have a fraction.";
                return false;
            }

            lastOrder = order;
            sawComponent = true;
            if (inTime)
            {
                sawTimeComponent = true;
            }
        }

        if (!sawComponent || (inTime && !sawTimeComponent))
        {
            this.ErrorMessage = "DURATION requires at least one component after each designator.";
            return false;
        }

        return true;
    }

    internal bool ValidateFlexibleTimeText(string text)
    {
        if (text.StartsWith("P", System.StringComparison.Ordinal)
            || text.StartsWith("-P", System.StringComparison.Ordinal))
        {
            return this.ValidateDurationText(text);
        }

        if (text.Length > 10 && text[10] == 'T')
        {
            return this.ValidateDateTimeText(text);
        }

        if (text.Length == 10 && text[4] == '-' && text[7] == '-')
        {
            return this.ValidateDateText(text);
        }

        return this.ValidateTimeOfDayText(text);
    }

    internal TimeZoneParseResult SeparateTimeZone(string text, int minimumBodyLength)
    {
        if (text.EndsWith("Z", System.StringComparison.Ordinal))
        {
            return text.Length - 1 < minimumBodyLength
                ? new TimeZoneParseResult()
                : new TimeZoneParseResult
                {
                    Success = true,
                    Body = text.Substring(0, text.Length - 1)
                };
        }

        int plus = text.LastIndexOf('+');
        int minus = text.LastIndexOf('-');
        int zoneStart = plus > minus ? plus : minus;
        if (zoneStart < minimumBodyLength)
        {
            return new TimeZoneParseResult
            {
                Success = true,
                Body = text
            };
        }

        string zone = text.Substring(zoneStart);
        if (!this.ValidateTimeZoneText(zone))
        {
            return new TimeZoneParseResult();
        }

        return new TimeZoneParseResult
        {
            Success = true,
            Body = text.Substring(0, zoneStart)
        };
    }

    internal bool ValidateTimeZoneText(string text)
    {
        if (text.Length != 5 && text.Length != 6)
        {
            return false;
        }

        if (text[0] != '+' && text[0] != '-')
        {
            return false;
        }

        int minuteStart = text.Length == 5 ? 3 : 4;
        if (text.Length == 6 && text[3] != ':')
        {
            return false;
        }

        if (!this.AreDecimalDigits(text, 1, 2)
            || !this.AreDecimalDigits(text, minuteStart, 2))
        {
            return false;
        }

        int hours = this.ParseSmallNumber(text, 1, 2);
        int minutes = this.ParseSmallNumber(text, minuteStart, 2);
        return hours <= 14 && minutes <= 59 && (hours != 14 || minutes == 0);
    }

    internal bool ValidateCalendarAndClock(
        int year,
        int month,
        int day,
        int hour,
        int minute,
        int second,
        bool hasSecond)
    {
        if (!this.ValidateCalendarDate(year, month, day))
        {
            this.ErrorMessage = "The time value contains a day that does not exist in its calendar month.";
            return false;
        }

        if (hour > 23 || !this.ValidateClockTime(hour, minute, second, hasSecond))
        {
            this.ErrorMessage = "The time value contains an out-of-range clock component.";
            return false;
        }

        return true;
    }

    internal bool ValidateCalendarDate(int year, int month, int day)
    {
        if (month < 1 || month > 12 || day < 1)
        {
            return false;
        }

        int maximum = 31;
        if (month == 4 || month == 6 || month == 9 || month == 11)
        {
            maximum = 30;
        }
        else if (month == 2)
        {
            bool leap = year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);
            maximum = leap ? 29 : 28;
        }

        return day <= maximum;
    }

    internal bool ValidateClockTime(
        int hour,
        int minute,
        int second,
        bool hasSecond)
    {
        if (minute > 59 || (hasSecond && second > 60))
        {
            return false;
        }

        if (hour < 24)
        {
            return true;
        }

        return hour == 24 && minute == 0 && second == 0;
    }

    internal int FindFractionSeparator(string text)
    {
        int dot = text.IndexOf('.');
        int comma = text.IndexOf(',');
        if (dot < 0)
        {
            return comma;
        }

        return comma < 0 || dot < comma ? dot : comma;
    }

    internal int GetDurationComponentOrder(char designator, bool inTime)
    {
        if (!inTime)
        {
            switch (designator)
            {
                case 'Y':
                    return 1;
                case 'M':
                    return 2;
                case 'W':
                    return 3;
                case 'D':
                    return 4;
                default:
                    return 0;
            }
        }

        switch (designator)
        {
            case 'H':
                return 5;
            case 'M':
                return 6;
            case 'S':
                return 7;
            default:
                return 0;
        }
    }

    internal bool AreDecimalDigits(string text, int start, int count)
    {
        if (start < 0 || count < 0 || start + count > text.Length)
        {
            return false;
        }

        int index = start;
        while (index < start + count)
        {
            if (!this.IsDecimalDigit(text[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool IsDecimalDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    internal int ParseSmallNumber(string text, int start, int count)
    {
        return int.Parse(
            text.Substring(start, count),
            System.Globalization.CultureInfo.InvariantCulture);
    }

    internal TimeValidationResult CreateTimeFailure(string message)
    {
        return new TimeValidationResult
        {
            ErrorMessage = message
        };
    }
}
