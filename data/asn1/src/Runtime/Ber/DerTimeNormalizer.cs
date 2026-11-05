namespace asn1;

internal class DerTimeNormalizer
{
    internal DerTimeNormalizationResult NormalizeTime(
        AsnTypeKind kind,
        string text)
    {
        if (kind == AsnTypeKind.UtcTime)
        {
            return this.NormalizeUtcTime(text);
        }

        if (kind == AsnTypeKind.GeneralizedTime)
        {
            return this.NormalizeGeneralizedTime(text);
        }

        if (kind == AsnTypeKind.Duration)
        {
            return this.CreateSuccess(this.NormalizeDuration(text));
        }

        if (kind == AsnTypeKind.Time)
        {
            if (text.StartsWith("P", System.StringComparison.Ordinal)
                || text.StartsWith("-P", System.StringComparison.Ordinal))
            {
                return this.CreateSuccess(this.NormalizeDuration(text));
            }

            return this.CreateSuccess(this.NormalizeTimeDifference(text));
        }

        if (kind == AsnTypeKind.TimeOfDay
            || kind == AsnTypeKind.DateTime)
        {
            return this.CreateSuccess(this.NormalizeTimeDifference(text));
        }

        return this.CreateSuccess(text);
    }

    internal DerTimeNormalizationResult NormalizeUtcTime(string text)
    {
        DerTimeZoneResult zone = this.ParseRequiredZone(text, 10);
        if (!zone.Success)
        {
            return this.CreateFailure(
                "DER UTCTime requires an explicit UTC designator or difference.");
        }

        int shortYear = this.ParseNumber(zone.Body, 0, 2);
        int year = shortYear >= 50 ? 1900 + shortYear : 2000 + shortYear;
        int month = this.ParseNumber(zone.Body, 2, 2);
        int day = this.ParseNumber(zone.Body, 4, 2);
        int hour = this.ParseNumber(zone.Body, 6, 2);
        int minute = this.ParseNumber(zone.Body, 8, 2);
        int second = zone.Body.Length == 12
            ? this.ParseNumber(zone.Body, 10, 2)
            : 0;
        DerTimeNormalizationResult shifted = this.ShiftWholeSecondTime(
            year,
            month,
            day,
            hour,
            minute,
            second,
            zone.OffsetMinutes,
            second == 60,
            true);
        if (!shifted.Success)
        {
            return shifted;
        }

        return this.CreateSuccess(shifted.Text.Substring(2));
    }

    internal DerTimeNormalizationResult NormalizeGeneralizedTime(string text)
    {
        DerTimeZoneResult zone = this.ParseRequiredZone(text, 10);
        if (!zone.Success)
        {
            return this.CreateFailure(
                "DER GeneralizedTime requires an explicit UTC designator or difference.");
        }

        string integral = zone.Body;
        string fraction = "";
        int separator = integral.IndexOf('.');
        if (separator < 0)
        {
            separator = integral.IndexOf(',');
        }

        if (separator >= 0)
        {
            fraction = integral.Substring(separator + 1);
            integral = integral.Substring(0, separator);
        }

        int year = this.ParseNumber(integral, 0, 4);
        int month = this.ParseNumber(integral, 4, 2);
        int day = this.ParseNumber(integral, 6, 2);
        int hour = this.ParseNumber(integral, 8, 2);
        int minute = integral.Length >= 12
            ? this.ParseNumber(integral, 10, 2)
            : 0;
        int second = integral.Length == 14
            ? this.ParseNumber(integral, 12, 2)
            : 0;

        System.Numerics.BigInteger remainder = System.Numerics.BigInteger.Zero;
        System.Numerics.BigInteger scale = System.Numerics.BigInteger.One;
        int additionalSeconds = 0;
        if (fraction.Length > 0)
        {
            scale = System.Numerics.BigInteger.Pow(10, fraction.Length);
            int multiplier = integral.Length == 10
                ? 3600
                : integral.Length == 12 ? 60 : 1;
            System.Numerics.BigInteger product = System.Numerics.BigInteger.Parse(
                fraction,
                System.Globalization.CultureInfo.InvariantCulture)
                * new System.Numerics.BigInteger(multiplier);
            additionalSeconds = (int)(product / scale);
            remainder = product % scale;
        }

        DerTimeNormalizationResult shifted = this.ShiftFractionalTime(
            year,
            month,
            day,
            hour,
            minute,
            second,
            additionalSeconds,
            zone.OffsetMinutes,
            remainder,
            scale,
            fraction.Length);
        return shifted;
    }

    internal DerTimeNormalizationResult ShiftWholeSecondTime(
        int year,
        int month,
        int day,
        int hour,
        int minute,
        int second,
        int offsetMinutes,
        bool leapSecond,
        bool utcTime)
    {
        int dayShift = 0;
        int normalizedHour;
        int normalizedMinute;
        int normalizedSecond = second;
        if (leapSecond)
        {
            int totalMinutes = hour * 60 + minute - offsetMinutes;
            while (totalMinutes < 0)
            {
                totalMinutes = totalMinutes + 1440;
                dayShift = dayShift - 1;
            }

            while (totalMinutes >= 1440)
            {
                totalMinutes = totalMinutes - 1440;
                dayShift = dayShift + 1;
            }

            normalizedHour = totalMinutes / 60;
            normalizedMinute = totalMinutes % 60;
        }
        else
        {
            int totalSeconds = hour * 3600 + minute * 60 + second
                - offsetMinutes * 60;
            while (totalSeconds < 0)
            {
                totalSeconds = totalSeconds + 86400;
                dayShift = dayShift - 1;
            }

            while (totalSeconds >= 86400)
            {
                totalSeconds = totalSeconds - 86400;
                dayShift = dayShift + 1;
            }

            normalizedHour = totalSeconds / 3600;
            normalizedMinute = totalSeconds % 3600 / 60;
            normalizedSecond = totalSeconds % 60;
        }

        DerCalendarResult date = this.ShiftDate(year, month, day, dayShift);
        if (!date.Success
            || utcTime && (date.Year < 1950 || date.Year > 2049))
        {
            return this.CreateFailure(
                "The time value cannot be represented in DER after conversion to UTC.");
        }

        return this.CreateSuccess(
            this.FormatNumber(date.Year, 4)
                + this.FormatNumber(date.Month, 2)
                + this.FormatNumber(date.Day, 2)
                + this.FormatNumber(normalizedHour, 2)
                + this.FormatNumber(normalizedMinute, 2)
                + this.FormatNumber(normalizedSecond, 2)
                + "Z");
    }

    internal DerTimeNormalizationResult ShiftFractionalTime(
        int year,
        int month,
        int day,
        int hour,
        int minute,
        int second,
        int additionalSeconds,
        int offsetMinutes,
        System.Numerics.BigInteger remainder,
        System.Numerics.BigInteger scale,
        int fractionalDigits)
    {
        DerTimeNormalizationResult shifted = this.ShiftWholeSecondTime(
            year,
            month,
            day,
            hour,
            minute,
            second + additionalSeconds,
            offsetMinutes,
            second == 60 && additionalSeconds == 0,
            false);
        if (!shifted.Success)
        {
            return shifted;
        }

        string baseText = shifted.Text.Substring(0, shifted.Text.Length - 1);
        string fraction = this.FormatFraction(
            remainder,
            scale,
            fractionalDigits);
        return this.CreateSuccess(
            fraction.Length == 0
                ? baseText + "Z"
                : baseText + "." + fraction + "Z");
    }

    internal string FormatFraction(
        System.Numerics.BigInteger remainder,
        System.Numerics.BigInteger scale,
        int digits)
    {
        if (remainder == System.Numerics.BigInteger.Zero
            || scale == System.Numerics.BigInteger.One)
        {
            return "";
        }

        string text = remainder.ToString(
            System.Globalization.CultureInfo.InvariantCulture);
        while (text.Length < digits)
        {
            text = "0" + text;
        }

        while (text.EndsWith("0", System.StringComparison.Ordinal))
        {
            text = text.Substring(0, text.Length - 1);
        }

        return text;
    }

    internal DerTimeZoneResult ParseRequiredZone(string text, int minimumBody)
    {
        if (text.EndsWith("Z", System.StringComparison.Ordinal))
        {
            return new DerTimeZoneResult
            {
                Success = true,
                Body = text.Substring(0, text.Length - 1)
            };
        }

        int plus = text.LastIndexOf('+');
        int minus = text.LastIndexOf('-');
        int start = plus > minus ? plus : minus;
        if (start < minimumBody)
        {
            return new DerTimeZoneResult();
        }

        string difference = text.Substring(start + 1).Replace(":", "");
        if (difference.Length != 4)
        {
            return new DerTimeZoneResult();
        }

        int hours = this.ParseNumber(difference, 0, 2);
        int minutes = this.ParseNumber(difference, 2, 2);
        int offset = hours * 60 + minutes;
        return new DerTimeZoneResult
        {
            Success = true,
            Body = text.Substring(0, start),
            OffsetMinutes = text[start] == '-' ? -offset : offset
        };
    }

    internal DerCalendarResult ShiftDate(
        int year,
        int month,
        int day,
        int shift)
    {
        while (shift > 0)
        {
            day = day + 1;
            if (day > this.GetMonthLength(year, month))
            {
                day = 1;
                month = month + 1;
                if (month > 12)
                {
                    month = 1;
                    year = year + 1;
                }
            }

            shift = shift - 1;
        }

        while (shift < 0)
        {
            day = day - 1;
            if (day == 0)
            {
                month = month - 1;
                if (month == 0)
                {
                    month = 12;
                    year = year - 1;
                }

                day = this.GetMonthLength(year, month);
            }

            shift = shift + 1;
        }

        return new DerCalendarResult
        {
            Success = year >= 0 && year <= 9999,
            Year = year,
            Month = month,
            Day = day
        };
    }

    internal int GetMonthLength(int year, int month)
    {
        if (month == 4 || month == 6 || month == 9 || month == 11)
        {
            return 30;
        }

        if (month == 2)
        {
            bool leap = year % 4 == 0
                && (year % 100 != 0 || year % 400 == 0);
            return leap ? 29 : 28;
        }

        return 31;
    }

    internal string NormalizeTimeDifference(string text)
    {
        string normalized = text.Replace(',', '.');
        int plus = normalized.LastIndexOf('+');
        int minus = normalized.LastIndexOf('-');
        int start = plus > minus ? plus : minus;
        if (start >= 8)
        {
            string difference = normalized.Substring(start + 1)
                .Replace(":", "");
            if (difference.Length == 4
                && difference.Substring(2, 2) == "00")
            {
                return normalized.Substring(0, start + 1)
                    + difference.Substring(0, 2);
            }
        }

        return normalized;
    }

    internal string NormalizeDuration(string text)
    {
        string normalized = text.Replace(',', '.');
        int position = normalized.StartsWith("-", System.StringComparison.Ordinal)
            ? 2
            : 1;
        System.Collections.Generic.List<string> components =
            new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<bool> timeComponents =
            new System.Collections.Generic.List<bool>();
        System.Collections.Generic.List<bool> zeroComponents =
            new System.Collections.Generic.List<bool>();
        bool inTime = false;
        while (position < normalized.Length)
        {
            if (normalized[position] == 'T')
            {
                inTime = true;
                position = position + 1;
                continue;
            }

            int start = position;
            bool zero = true;
            while (position < normalized.Length
                && (normalized[position] >= '0'
                    && normalized[position] <= '9'
                    || normalized[position] == '.'))
            {
                if (normalized[position] >= '1'
                    && normalized[position] <= '9')
                {
                    zero = false;
                }

                position = position + 1;
            }

            position = position + 1;
            components.Add(normalized.Substring(start, position - start));
            timeComponents.Add(inTime);
            zeroComponents.Add(zero);
        }

        string result = normalized.StartsWith("-", System.StringComparison.Ordinal)
            ? "-P"
            : "P";
        bool wroteTimeMarker = false;
        int index = 0;
        while (index < components.Count)
        {
            if (!zeroComponents[index] || index == components.Count - 1)
            {
                if (timeComponents[index] && !wroteTimeMarker)
                {
                    result = result + "T";
                    wroteTimeMarker = true;
                }

                result = result + components[index];
            }

            index = index + 1;
        }

        return result;
    }

    internal int ParseNumber(string text, int start, int count)
    {
        return int.Parse(
            text.Substring(start, count),
            System.Globalization.CultureInfo.InvariantCulture);
    }

    internal string FormatNumber(int value, int digits)
    {
        return value.ToString(
            "D" + digits.ToString(
                System.Globalization.CultureInfo.InvariantCulture),
            System.Globalization.CultureInfo.InvariantCulture);
    }

    internal DerTimeNormalizationResult CreateSuccess(string text)
    {
        return new DerTimeNormalizationResult
        {
            Success = true,
            Text = text
        };
    }

    internal DerTimeNormalizationResult CreateFailure(string message)
    {
        return new DerTimeNormalizationResult
        {
            ErrorMessage = message
        };
    }
}
