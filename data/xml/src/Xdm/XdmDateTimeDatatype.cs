using System;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace xml;

internal class XdmDateTimeDatatype
{
    internal const int MaximumYearDigits = 10000;
    internal XdmTypeSystem TypeSystem;

    internal XdmDateTimeDatatype()
    {
        TypeSystem = new XdmTypeSystem();
    }

    internal XdmAtomicValue ParseXdmDateTime(string lexicalValue, XdmAtomicType targetType)
    {
        XdmNumericParseResult result = TryParseXdmDateTime(lexicalValue, targetType);
        return new XdmNumericDatatype().RequireXdmNumericParseResult(result);
    }

    internal XdmNumericParseResult TryParseXdmDateTime(string lexicalValue, XdmAtomicType targetType)
    {
        Guard.NotNull(lexicalValue, "lexicalValue");
        RequireXdmDateTimeType(targetType);
        string normalized = new XdmStringDatatype().CollapseXdmStringWhitespace(lexicalValue);
        XdmDateTimeParserState state = new XdmDateTimeParserState(normalized);
        XdmDateTimeStorage storage = new XdmDateTimeStorage();
        if (targetType == XdmAtomicType.DateTime || targetType == XdmAtomicType.DateTimeStamp)
        {
            ReadXdmDateFields(state, storage);
            ExpectXdmDateTimeCharacter(state, 'T');
            ReadXdmTimeFields(state, storage);
        }
        else if (targetType == XdmAtomicType.Date)
        {
            ReadXdmDateFields(state, storage);
        }
        else if (targetType == XdmAtomicType.Time)
        {
            ReadXdmTimeFields(state, storage);
        }
        else if (targetType == XdmAtomicType.GYearMonth)
        {
            storage.Year = ReadXdmDateTimeYear(state);
            ExpectXdmDateTimeCharacter(state, '-');
            storage.Month = ReadXdmDateTimeTwoDigits(state);
        }
        else if (targetType == XdmAtomicType.GYear)
        {
            storage.Year = ReadXdmDateTimeYear(state);
        }
        else if (targetType == XdmAtomicType.GMonthDay)
        {
            ExpectXdmDateTimeCharacter(state, '-');
            ExpectXdmDateTimeCharacter(state, '-');
            storage.Month = ReadXdmDateTimeTwoDigits(state);
            ExpectXdmDateTimeCharacter(state, '-');
            storage.Day = ReadXdmDateTimeTwoDigits(state);
        }
        else if (targetType == XdmAtomicType.GMonth)
        {
            ExpectXdmDateTimeCharacter(state, '-');
            ExpectXdmDateTimeCharacter(state, '-');
            storage.Month = ReadXdmDateTimeTwoDigits(state);
        }
        else
        {
            ExpectXdmDateTimeCharacter(state, '-');
            ExpectXdmDateTimeCharacter(state, '-');
            ExpectXdmDateTimeCharacter(state, '-');
            storage.Day = ReadXdmDateTimeTwoDigits(state);
        }

        ReadXdmDateTimeTimezone(state, storage);
        if (targetType == XdmAtomicType.DateTimeStamp && !storage.HasTimezone)
        {
            state.Valid = false;
        }

        if (state.Overflow)
        {
            return OverflowXdmDateTimeLexical(targetType);
        }

        if (state.Index != normalized.Length || !state.Valid || !IsValidXdmDateTimeStorage(storage, targetType))
        {
            return InvalidXdmDateTimeLexical(targetType);
        }

        if (storage.Hour == 24)
        {
            storage.Hour = 0;
            if (targetType == XdmAtomicType.DateTime || targetType == XdmAtomicType.DateTimeStamp)
            {
                XdmCalendarDate following = AddXdmCalendarDays(new XdmCalendarDate(storage.Year, storage.Month, storage.Day), BigInteger.One);
                storage.Year = following.Year;
                storage.Month = following.Month;
                storage.Day = following.Day;
            }
        }

        return new XdmNumericParseResult(true, CreateXdmDateTimeValue(storage, targetType, lexicalValue), null, null);
    }

    internal void ReadXdmDateFields(XdmDateTimeParserState state, XdmDateTimeStorage storage)
    {
        storage.Year = ReadXdmDateTimeYear(state);
        ExpectXdmDateTimeCharacter(state, '-');
        storage.Month = ReadXdmDateTimeTwoDigits(state);
        ExpectXdmDateTimeCharacter(state, '-');
        storage.Day = ReadXdmDateTimeTwoDigits(state);
    }

    internal void ReadXdmTimeFields(XdmDateTimeParserState state, XdmDateTimeStorage storage)
    {
        storage.Hour = ReadXdmDateTimeTwoDigits(state);
        ExpectXdmDateTimeCharacter(state, ':');
        storage.Minute = ReadXdmDateTimeTwoDigits(state);
        ExpectXdmDateTimeCharacter(state, ':');
        string? seconds = ReadXdmDateTimeDigits(state, 2);
        if (seconds == null)
        {
            state.Valid = false;
            return;
        }

        StringBuilder secondLexical = new StringBuilder(seconds);
        if (state.Index < state.Source.Length && state.Source[state.Index] == '.')
        {
            secondLexical.Append('.');
            state.Index = state.Index + 1;
            int fractionStart = state.Index;
            while (state.Index < state.Source.Length && state.Source[state.Index] >= '0' && state.Source[state.Index] <= '9')
            {
                secondLexical.Append(state.Source[state.Index]);
                state.Index = state.Index + 1;
            }

            if (state.Index == fractionStart || state.Index - fractionStart > MaximumYearDigits)
            {
                if (state.Index - fractionStart > MaximumYearDigits)
                {
                    state.Overflow = true;
                }

                state.Valid = false;
                return;
            }
        }

        XdmDecimalStorage parsed = new XdmDurationDatatype().ParseXdmDurationSecondComponent(secondLexical.ToString());
        storage.SecondCoefficient = parsed.Coefficient;
        storage.SecondScale = parsed.Scale;
    }

    internal BigInteger ReadXdmDateTimeYear(XdmDateTimeParserState state)
    {
        bool negative = false;
        if (state.Index < state.Source.Length && state.Source[state.Index] == '-')
        {
            negative = true;
            state.Index = state.Index + 1;
        }

        int start = state.Index;
        while (state.Index < state.Source.Length && state.Source[state.Index] >= '0' && state.Source[state.Index] <= '9')
        {
            state.Index = state.Index + 1;
        }

        int length = state.Index - start;
        if (length > MaximumYearDigits)
        {
            state.Overflow = true;
            state.Valid = false;
            return BigInteger.Zero;
        }

        if (length < 4 || length > 4 && state.Source[start] == '0')
        {
            state.Valid = false;
            return BigInteger.Zero;
        }

        BigInteger year = BigInteger.Parse(state.Source.Substring(start, length), CultureInfo.InvariantCulture);
        if (negative)
        {
            if (year == BigInteger.Zero)
            {
                state.Valid = false;
            }

            year = -year;
        }

        return year;
    }

    internal int ReadXdmDateTimeTwoDigits(XdmDateTimeParserState state)
    {
        string? digits = ReadXdmDateTimeDigits(state, 2);
        if (digits == null)
        {
            return 0;
        }

        return int.Parse(digits, CultureInfo.InvariantCulture);
    }

    internal string? ReadXdmDateTimeDigits(XdmDateTimeParserState state, int count)
    {
        if (state.Index + count > state.Source.Length)
        {
            state.Valid = false;
            return null;
        }

        int index = 0;
        while (index < count)
        {
            char character = state.Source[state.Index + index];
            if (character < '0' || character > '9')
            {
                state.Valid = false;
                return null;
            }

            index = index + 1;
        }

        string result = state.Source.Substring(state.Index, count);
        state.Index = state.Index + count;
        return result;
    }

    internal void ExpectXdmDateTimeCharacter(XdmDateTimeParserState state, char expected)
    {
        if (state.Index >= state.Source.Length || state.Source[state.Index] != expected)
        {
            state.Valid = false;
            return;
        }

        state.Index = state.Index + 1;
    }

    internal void ReadXdmDateTimeTimezone(XdmDateTimeParserState state, XdmDateTimeStorage storage)
    {
        if (!state.Valid || state.Index >= state.Source.Length)
        {
            return;
        }

        if (state.Source[state.Index] == 'Z')
        {
            storage.HasTimezone = true;
            storage.TimezoneMinutes = 0;
            state.Index = state.Index + 1;
            return;
        }

        char sign = state.Source[state.Index];
        if (sign != '+' && sign != '-')
        {
            return;
        }

        state.Index = state.Index + 1;
        int hours = ReadXdmDateTimeTwoDigits(state);
        ExpectXdmDateTimeCharacter(state, ':');
        int minutes = ReadXdmDateTimeTwoDigits(state);
        if (!state.Valid || minutes > 59 || hours > 14 || hours == 14 && minutes != 0)
        {
            state.Valid = false;
            return;
        }

        storage.HasTimezone = true;
        storage.TimezoneMinutes = (hours * 60 + minutes) * (sign == '-' ? -1 : 1);
    }

    internal bool IsValidXdmDateTimeStorage(XdmDateTimeStorage storage, XdmAtomicType type)
    {
        if (TypeHasXdmDate(type) && (storage.Month < 1 || storage.Month > 12 || storage.Day < 1 || storage.Day > DaysInXdmCalendarMonth(storage.Year, storage.Month)))
        {
            return false;
        }

        if (type == XdmAtomicType.GYearMonth && (storage.Month < 1 || storage.Month > 12))
        {
            return false;
        }

        if (type == XdmAtomicType.GMonthDay && (storage.Month < 1 || storage.Month > 12 || storage.Day < 1 || storage.Day > DaysInXdmCalendarMonth(new BigInteger(2000), storage.Month)))
        {
            return false;
        }

        if (type == XdmAtomicType.GMonth && (storage.Month < 1 || storage.Month > 12))
        {
            return false;
        }

        if (type == XdmAtomicType.GDay && (storage.Day < 1 || storage.Day > 31))
        {
            return false;
        }

        if (TypeHasXdmTime(type))
        {
            BigInteger secondLimit = new XdmNumericDatatype().PowerOfTen(storage.SecondScale) * 60;
            bool endOfDay = storage.Hour == 24 && storage.Minute == 0 && storage.SecondCoefficient == BigInteger.Zero;
            if (storage.Hour < 0 || storage.Hour > 23 && !endOfDay || storage.Minute < 0 || storage.Minute > 59 || storage.SecondCoefficient < BigInteger.Zero || storage.SecondCoefficient >= secondLimit)
            {
                return false;
            }
        }

        return !storage.HasTimezone || storage.TimezoneMinutes >= -840 && storage.TimezoneMinutes <= 840;
    }

    internal bool TypeHasXdmDate(XdmAtomicType type)
    {
        return type == XdmAtomicType.DateTime || type == XdmAtomicType.DateTimeStamp || type == XdmAtomicType.Date;
    }

    internal bool TypeHasXdmTime(XdmAtomicType type)
    {
        return type == XdmAtomicType.DateTime || type == XdmAtomicType.DateTimeStamp || type == XdmAtomicType.Time;
    }

    internal XdmAtomicValue CreateXdmDateTimeValue(XdmDateTimeStorage source, XdmAtomicType type, string? lexicalValue)
    {
        RequireXdmDateTimeType(type);
        XdmDateTimeStorage storage = CopyXdmDateTimeStorage(source);
        if (!IsValidXdmDateTimeStorage(storage, type))
        {
            throw new ArgumentException("The date/time components are outside the value space.", "source");
        }

        CheckXdmDateTimeMagnitude(storage.Year);
        string canonical = CanonicalizeXdmDateTime(storage, type);
        XdmAtomicValue value = new XdmAtomicValue();
        value.Type = type;
        value.LexicalValue = lexicalValue == null ? canonical : lexicalValue;
        value.CanonicalValue = canonical;
        value.StringValue = canonical;
        value.ExtendedValue = storage;
        value.IsImmutable = true;
        return value;
    }

    internal XdmDateTimeStorage CopyXdmDateTimeStorage(XdmDateTimeStorage source)
    {
        XdmDateTimeStorage result = new XdmDateTimeStorage();
        result.Year = source.Year;
        result.Month = source.Month;
        result.Day = source.Day;
        result.Hour = source.Hour;
        result.Minute = source.Minute;
        result.SecondCoefficient = source.SecondCoefficient;
        result.SecondScale = source.SecondScale;
        result.HasTimezone = source.HasTimezone;
        result.TimezoneMinutes = source.TimezoneMinutes;
        return result;
    }

    internal string CanonicalizeXdmDateTime(XdmDateTimeStorage storage, XdmAtomicType type)
    {
        StringBuilder result = new StringBuilder();
        if (type == XdmAtomicType.DateTime || type == XdmAtomicType.DateTimeStamp || type == XdmAtomicType.Date || type == XdmAtomicType.GYearMonth || type == XdmAtomicType.GYear)
        {
            result.Append(CanonicalizeXdmDateTimeYear(storage.Year));
        }

        if (type == XdmAtomicType.DateTime || type == XdmAtomicType.DateTimeStamp || type == XdmAtomicType.Date)
        {
            result.Append('-');
            AppendXdmDateTimeTwoDigits(result, storage.Month);
            result.Append('-');
            AppendXdmDateTimeTwoDigits(result, storage.Day);
        }
        else if (type == XdmAtomicType.GYearMonth)
        {
            result.Append('-');
            AppendXdmDateTimeTwoDigits(result, storage.Month);
        }
        else if (type == XdmAtomicType.GMonthDay)
        {
            result.Append("--");
            AppendXdmDateTimeTwoDigits(result, storage.Month);
            result.Append('-');
            AppendXdmDateTimeTwoDigits(result, storage.Day);
        }
        else if (type == XdmAtomicType.GMonth)
        {
            result.Append("--");
            AppendXdmDateTimeTwoDigits(result, storage.Month);
        }
        else if (type == XdmAtomicType.GDay)
        {
            result.Append("---");
            AppendXdmDateTimeTwoDigits(result, storage.Day);
        }

        if (type == XdmAtomicType.DateTime || type == XdmAtomicType.DateTimeStamp)
        {
            result.Append('T');
        }

        if (TypeHasXdmTime(type))
        {
            AppendXdmDateTimeTwoDigits(result, storage.Hour);
            result.Append(':');
            AppendXdmDateTimeTwoDigits(result, storage.Minute);
            result.Append(':');
            AppendXdmDateTimeSecond(result, storage);
        }

        if (storage.HasTimezone)
        {
            AppendXdmDateTimeTimezone(result, storage.TimezoneMinutes);
        }

        return result.ToString();
    }

    internal string CanonicalizeXdmDateTimeYear(BigInteger year)
    {
        bool negative = year < BigInteger.Zero;
        string digits = BigInteger.Abs(year).ToString(CultureInfo.InvariantCulture);
        while (digits.Length < 4)
        {
            digits = "0" + digits;
        }

        return negative ? "-" + digits : digits;
    }

    internal void AppendXdmDateTimeTwoDigits(StringBuilder target, int value)
    {
        if (value < 10)
        {
            target.Append('0');
        }

        target.Append(value.ToString(CultureInfo.InvariantCulture));
    }

    internal void AppendXdmDateTimeSecond(StringBuilder target, XdmDateTimeStorage storage)
    {
        BigInteger factor = new XdmNumericDatatype().PowerOfTen(storage.SecondScale);
        BigInteger whole = storage.SecondCoefficient / factor;
        BigInteger fraction = storage.SecondCoefficient % factor;
        if (whole < 10)
        {
            target.Append('0');
        }

        target.Append(whole.ToString(CultureInfo.InvariantCulture));
        if (fraction != BigInteger.Zero)
        {
            string digits = fraction.ToString(CultureInfo.InvariantCulture);
            while (digits.Length < storage.SecondScale)
            {
                digits = "0" + digits;
            }

            int end = digits.Length;
            while (end > 0 && digits[end - 1] == '0')
            {
                end = end - 1;
            }

            target.Append('.');
            target.Append(digits.Substring(0, end));
        }
    }

    internal void AppendXdmDateTimeTimezone(StringBuilder target, int minutes)
    {
        if (minutes == 0)
        {
            target.Append('Z');
            return;
        }

        int absolute = Math.Abs(minutes);
        target.Append(minutes < 0 ? '-' : '+');
        AppendXdmDateTimeTwoDigits(target, absolute / 60);
        target.Append(':');
        AppendXdmDateTimeTwoDigits(target, absolute % 60);
    }

    internal XdmDateTimeStorage ReadXdmDateTimeStorage(XdmAtomicValue value)
    {
        RequireXdmDateTimeValue(value);
        object? extended = value.ExtendedValue;
        if (extended != null)
        {
            return CopyXdmDateTimeStorage((XdmDateTimeStorage)extended);
        }

        string? representation = value.CanonicalValue == null ? value.StringValue : value.CanonicalValue;
        if (representation == null)
        {
            representation = value.LexicalValue;
        }

        if (representation == null)
        {
            throw new ArgumentException("The date/time value has no representation.", "value");
        }

        XdmAtomicValue parsed = ParseXdmDateTime(representation, value.Type);
        object? parsedStorage = parsed.ExtendedValue;
        if (parsedStorage == null)
        {
            throw new InvalidOperationException("Parsed date/time storage is missing.");
        }

        return CopyXdmDateTimeStorage((XdmDateTimeStorage)parsedStorage);
    }

    internal bool IsXdmLeapYear(BigInteger year)
    {
        return year % 400 == BigInteger.Zero || year % 4 == BigInteger.Zero && year % 100 != BigInteger.Zero;
    }

    internal int DaysInXdmCalendarMonth(BigInteger year, int month)
    {
        if (month == 2)
        {
            return IsXdmLeapYear(year) ? 29 : 28;
        }

        return month == 4 || month == 6 || month == 9 || month == 11 ? 30 : 31;
    }

    internal BigInteger FloorDivideXdmCalendar(BigInteger dividend, BigInteger divisor)
    {
        BigInteger quotient = dividend / divisor;
        BigInteger remainder = dividend % divisor;
        if (remainder != BigInteger.Zero && dividend < BigInteger.Zero)
        {
            quotient = quotient - BigInteger.One;
        }

        return quotient;
    }

    internal BigInteger XdmCalendarDayNumber(BigInteger year, int month, int day)
    {
        BigInteger adjustedYear = month <= 2 ? year - BigInteger.One : year;
        BigInteger era = FloorDivideXdmCalendar(adjustedYear, new BigInteger(400));
        BigInteger yearOfEra = adjustedYear - era * 400;
        int adjustedMonth = month > 2 ? month - 3 : month + 9;
        BigInteger dayOfYear = (153 * adjustedMonth + 2) / 5 + day - 1;
        BigInteger dayOfEra = yearOfEra * 365 + yearOfEra / 4 - yearOfEra / 100 + dayOfYear;
        return era * 146097 + dayOfEra;
    }

    internal XdmCalendarDate XdmCalendarDateFromDayNumber(BigInteger dayNumber)
    {
        BigInteger era = FloorDivideXdmCalendar(dayNumber, new BigInteger(146097));
        BigInteger dayOfEra = dayNumber - era * 146097;
        BigInteger yearOfEra = (dayOfEra - dayOfEra / 1460 + dayOfEra / 36524 - dayOfEra / 146096) / 365;
        BigInteger year = yearOfEra + era * 400;
        BigInteger dayOfYear = dayOfEra - (365 * yearOfEra + yearOfEra / 4 - yearOfEra / 100);
        BigInteger monthPrime = (5 * dayOfYear + 2) / 153;
        int monthPrimeValue = (int)monthPrime;
        int day = (int)(dayOfYear - (153 * monthPrime + 2) / 5 + 1);
        int month = monthPrimeValue < 10 ? monthPrimeValue + 3 : monthPrimeValue - 9;
        if (month <= 2)
        {
            year = year + BigInteger.One;
        }

        return new XdmCalendarDate(year, month, day);
    }

    internal XdmCalendarDate AddXdmCalendarDays(XdmCalendarDate date, BigInteger days)
    {
        return XdmCalendarDateFromDayNumber(XdmCalendarDayNumber(date.Year, date.Month, date.Day) + days);
    }

    internal int CompareXdmDateTime(XdmAtomicValue left, XdmAtomicValue right, int implicitTimezoneMinutes)
    {
        RequireComparableXdmDateTimeValues(left, right);
        ValidateXdmTimezoneMinutes(implicitTimezoneMinutes);
        XdmDecimalStorage leftTimeline = XdmDateTimeTimelineSeconds(ReadXdmDateTimeStorage(left), left.Type, implicitTimezoneMinutes);
        XdmDecimalStorage rightTimeline = XdmDateTimeTimelineSeconds(ReadXdmDateTimeStorage(right), right.Type, implicitTimezoneMinutes);
        return CompareXdmDateTimeSeconds(leftTimeline, rightTimeline);
    }

    internal bool EqualXdmDateTime(XdmAtomicValue left, XdmAtomicValue right, int implicitTimezoneMinutes)
    {
        if (left.Type == XdmAtomicType.GYearMonth || left.Type == XdmAtomicType.GYear || left.Type == XdmAtomicType.GMonthDay || left.Type == XdmAtomicType.GMonth || left.Type == XdmAtomicType.GDay)
        {
            return EqualXdmGregorianValue(left, right, implicitTimezoneMinutes);
        }

        return CompareXdmDateTime(left, right, implicitTimezoneMinutes) == 0;
    }

    internal bool EqualXdmGregorianValue(XdmAtomicValue left, XdmAtomicValue right, int implicitTimezoneMinutes)
    {
        ValidateXdmTimezoneMinutes(implicitTimezoneMinutes);
        RequireXdmDateTimeValue(left);
        RequireXdmDateTimeValue(right);
        if (left.Type != right.Type)
        {
            return false;
        }

        XdmDateTimeStorage leftStorage = ReadXdmDateTimeStorage(left);
        XdmDateTimeStorage rightStorage = ReadXdmDateTimeStorage(right);
        XdmDecimalStorage leftTimeline = XdmGregorianTimelineSeconds(leftStorage, left.Type, implicitTimezoneMinutes);
        XdmDecimalStorage rightTimeline = XdmGregorianTimelineSeconds(rightStorage, right.Type, implicitTimezoneMinutes);
        return CompareXdmDateTimeSeconds(leftTimeline, rightTimeline) == 0;
    }

    internal XdmDecimalStorage XdmGregorianTimelineSeconds(XdmDateTimeStorage storage, XdmAtomicType type, int implicitTimezoneMinutes)
    {
        BigInteger year = storage.Year;
        int month = storage.Month;
        int day = storage.Day;
        if (type == XdmAtomicType.GYear)
        {
            month = 1;
            day = 1;
        }
        else if (type == XdmAtomicType.GYearMonth)
        {
            day = 1;
        }
        else if (type == XdmAtomicType.GMonthDay)
        {
            year = new BigInteger(1972);
        }
        else if (type == XdmAtomicType.GMonth)
        {
            year = new BigInteger(1972);
            day = 1;
        }
        else
        {
            year = new BigInteger(1972);
            month = 12;
        }

        int timezone = storage.HasTimezone ? storage.TimezoneMinutes : implicitTimezoneMinutes;
        BigInteger timeline = XdmCalendarDayNumber(year, month, day) * 86400 - timezone * 60;
        return new XdmDecimalStorage(timeline, 0);
    }

    internal XdmDecimalStorage XdmDateTimeTimelineSeconds(XdmDateTimeStorage storage, XdmAtomicType type, int implicitTimezoneMinutes)
    {
        int timezone = storage.HasTimezone ? storage.TimezoneMinutes : implicitTimezoneMinutes;
        BigInteger day;
        if (type == XdmAtomicType.Time)
        {
            day = XdmCalendarDayNumber(new BigInteger(1972), 12, 31);
        }
        else
        {
            day = XdmCalendarDayNumber(storage.Year, storage.Month, storage.Day);
        }

        BigInteger factor = new XdmNumericDatatype().PowerOfTen(storage.SecondScale);
        BigInteger whole = day * 86400 + storage.Hour * 3600 + storage.Minute * 60 - timezone * 60;
        return new XdmDecimalStorage(whole * factor + storage.SecondCoefficient, storage.SecondScale);
    }

    internal int CompareXdmDateTimeSeconds(XdmDecimalStorage left, XdmDecimalStorage right)
    {
        int scale = left.Scale > right.Scale ? left.Scale : right.Scale;
        XdmNumericDatatype numeric = new XdmNumericDatatype();
        BigInteger leftCoefficient = numeric.ScaleXdmDecimalCoefficient(left, scale);
        BigInteger rightCoefficient = numeric.ScaleXdmDecimalCoefficient(right, scale);
        return leftCoefficient.CompareTo(rightCoefficient);
    }

    internal XdmAtomicValue AdjustXdmDateTimeTimezone(XdmAtomicValue value, bool hasTargetTimezone, int targetTimezoneMinutes)
    {
        RequireXdmDateTimeValue(value);
        if (value.Type != XdmAtomicType.DateTime && value.Type != XdmAtomicType.DateTimeStamp && value.Type != XdmAtomicType.Date && value.Type != XdmAtomicType.Time)
        {
            throw new ArgumentException("Timezone adjustment is defined only for dateTime, date, and time values.", "value");
        }

        if (hasTargetTimezone)
        {
            ValidateXdmTimezoneMinutes(targetTimezoneMinutes);
        }

        if (value.Type == XdmAtomicType.DateTimeStamp && !hasTargetTimezone)
        {
            throw new ArgumentException("An xs:dateTimeStamp must retain a timezone.", "hasTargetTimezone");
        }

        XdmDateTimeStorage storage = ReadXdmDateTimeStorage(value);
        if (!hasTargetTimezone)
        {
            storage.HasTimezone = false;
            storage.TimezoneMinutes = 0;
            return CreateXdmDateTimeValue(storage, value.Type, null);
        }

        if (!storage.HasTimezone)
        {
            storage.HasTimezone = true;
            storage.TimezoneMinutes = targetTimezoneMinutes;
            return CreateXdmDateTimeValue(storage, value.Type, null);
        }

        int differenceMinutes = targetTimezoneMinutes - storage.TimezoneMinutes;
        storage = AddXdmMinutesToDateTimeStorage(storage, value.Type, differenceMinutes);
        storage.HasTimezone = true;
        storage.TimezoneMinutes = targetTimezoneMinutes;
        return CreateXdmDateTimeValue(storage, value.Type, null);
    }

    internal XdmAtomicValue ApplyXdmImplicitTimezone(XdmAtomicValue value, int implicitTimezoneMinutes)
    {
        ValidateXdmTimezoneMinutes(implicitTimezoneMinutes);
        XdmDateTimeStorage storage = ReadXdmDateTimeStorage(value);
        if (!storage.HasTimezone)
        {
            storage.HasTimezone = true;
            storage.TimezoneMinutes = implicitTimezoneMinutes;
        }

        return CreateXdmDateTimeValue(storage, value.Type, null);
    }

    internal XdmDateTimeStorage AddXdmMinutesToDateTimeStorage(XdmDateTimeStorage storage, XdmAtomicType type, int minutes)
    {
        XdmDurationStorage duration = new XdmDurationStorage(BigInteger.Zero, new BigInteger(minutes * 60), 0);
        return AddXdmDayTimeDurationStorage(storage, type, duration);
    }

    internal XdmAtomicValue YearFromXdmDateTime(XdmAtomicValue value)
    {
        XdmDateTimeStorage storage = ReadXdmDateTimeStorage(value);
        return new XdmNumericDatatype().CreateXdmIntegerValue(storage.Year, XdmAtomicType.Integer, null);
    }

    internal XdmAtomicValue MonthFromXdmDateTime(XdmAtomicValue value)
    {
        return CreateXdmDateTimeIntegerComponent(ReadXdmDateTimeStorage(value).Month);
    }

    internal XdmAtomicValue DayFromXdmDateTime(XdmAtomicValue value)
    {
        return CreateXdmDateTimeIntegerComponent(ReadXdmDateTimeStorage(value).Day);
    }

    internal XdmAtomicValue HoursFromXdmDateTime(XdmAtomicValue value)
    {
        return CreateXdmDateTimeIntegerComponent(ReadXdmDateTimeStorage(value).Hour);
    }

    internal XdmAtomicValue MinutesFromXdmDateTime(XdmAtomicValue value)
    {
        return CreateXdmDateTimeIntegerComponent(ReadXdmDateTimeStorage(value).Minute);
    }

    internal XdmAtomicValue SecondsFromXdmDateTime(XdmAtomicValue value)
    {
        XdmDateTimeStorage storage = ReadXdmDateTimeStorage(value);
        return new XdmNumericDatatype().CreateXdmDecimalValue(new XdmDecimalStorage(storage.SecondCoefficient, storage.SecondScale), null);
    }

    internal XdmAtomicValue? TimezoneFromXdmDateTime(XdmAtomicValue value)
    {
        XdmDateTimeStorage storage = ReadXdmDateTimeStorage(value);
        if (!storage.HasTimezone)
        {
            return null;
        }

        return new XdmDurationDatatype().CreateXdmDurationValue(new XdmDurationStorage(BigInteger.Zero, new BigInteger(storage.TimezoneMinutes * 60), 0), XdmAtomicType.DayTimeDuration, null);
    }

    internal XdmAtomicValue CreateXdmDateTimeIntegerComponent(int component)
    {
        return new XdmNumericDatatype().CreateXdmIntegerValue(new BigInteger(component), XdmAtomicType.Integer, null);
    }

    internal XdmAtomicValue SubtractXdmDateTimeValues(XdmAtomicValue left, XdmAtomicValue right, int implicitTimezoneMinutes)
    {
        RequireComparableXdmDateTimeValues(left, right);
        ValidateXdmTimezoneMinutes(implicitTimezoneMinutes);
        XdmDecimalStorage leftTimeline = XdmDateTimeTimelineSeconds(ReadXdmDateTimeStorage(left), left.Type, implicitTimezoneMinutes);
        XdmDecimalStorage rightTimeline = XdmDateTimeTimelineSeconds(ReadXdmDateTimeStorage(right), right.Type, implicitTimezoneMinutes);
        int scale = leftTimeline.Scale > rightTimeline.Scale ? leftTimeline.Scale : rightTimeline.Scale;
        XdmNumericDatatype numeric = new XdmNumericDatatype();
        BigInteger leftCoefficient = numeric.ScaleXdmDecimalCoefficient(leftTimeline, scale);
        BigInteger rightCoefficient = numeric.ScaleXdmDecimalCoefficient(rightTimeline, scale);
        return new XdmDurationDatatype().CreateXdmDurationValue(new XdmDurationStorage(BigInteger.Zero, leftCoefficient - rightCoefficient, scale), XdmAtomicType.DayTimeDuration, null);
    }

    internal XdmAtomicValue AddXdmDurationToDateTime(XdmAtomicValue value, XdmAtomicValue duration)
    {
        RequireXdmDateTimeValue(value);
        if (value.Type != XdmAtomicType.DateTime && value.Type != XdmAtomicType.DateTimeStamp && value.Type != XdmAtomicType.Date && value.Type != XdmAtomicType.Time)
        {
            throw new ArgumentException("Duration arithmetic is not defined for this Gregorian type.", "value");
        }

        XdmDateTimeStorage storage = ReadXdmDateTimeStorage(value);
        XdmDurationStorage durationStorage = new XdmDurationDatatype().ReadXdmDurationStorage(duration);
        if (duration.Type == XdmAtomicType.YearMonthDuration)
        {
            if (value.Type == XdmAtomicType.Time)
            {
                throw new ArgumentException("A year-month duration cannot be added to a time.", "duration");
            }

            storage = AddXdmYearMonthDurationStorage(storage, durationStorage);
        }
        else if (duration.Type == XdmAtomicType.DayTimeDuration)
        {
            storage = AddXdmDayTimeDurationStorage(storage, value.Type, durationStorage);
        }
        else
        {
            throw new ArgumentException("Date/time arithmetic requires an ordered duration subtype.", "duration");
        }

        return CreateXdmDateTimeValue(storage, value.Type, null);
    }

    internal XdmAtomicValue SubtractXdmDurationFromDateTime(XdmAtomicValue value, XdmAtomicValue duration)
    {
        XdmAtomicValue negative = new XdmDurationDatatype().NegateXdmDuration(duration);
        return AddXdmDurationToDateTime(value, negative);
    }

    internal XdmDateTimeStorage AddXdmYearMonthDurationStorage(XdmDateTimeStorage storage, XdmDurationStorage duration)
    {
        BigInteger monthIndex = storage.Year * 12 + storage.Month - 1 + duration.Months;
        BigInteger year = FloorDivideXdmCalendar(monthIndex, new BigInteger(12));
        int month = (int)(monthIndex - year * 12) + 1;
        CheckXdmDateTimeMagnitude(year);
        storage.Year = year;
        storage.Month = month;
        int maximumDay = DaysInXdmCalendarMonth(year, month);
        if (storage.Day > maximumDay)
        {
            storage.Day = maximumDay;
        }

        return storage;
    }

    internal void RequireComparableXdmDateTimeValues(XdmAtomicValue left, XdmAtomicValue right)
    {
        RequireXdmDateTimeValue(left);
        RequireXdmDateTimeValue(right);
        bool dateTimes = (left.Type == XdmAtomicType.DateTime || left.Type == XdmAtomicType.DateTimeStamp) && (right.Type == XdmAtomicType.DateTime || right.Type == XdmAtomicType.DateTimeStamp);
        if (!dateTimes && left.Type != right.Type)
        {
            throw new ArgumentException("Date/time comparison requires compatible operand types.", "right");
        }

        if (!dateTimes && left.Type != XdmAtomicType.Date && left.Type != XdmAtomicType.Time)
        {
            throw new ArgumentException("Ordering is defined only for dateTime, date, and time values.", "left");
        }
    }

    internal void ValidateXdmTimezoneMinutes(int timezoneMinutes)
    {
        if (timezoneMinutes < -840 || timezoneMinutes > 840)
        {
            throw CreateXdmDateTimeError(XPathErrorCodes.FODT0003, "A timezone offset must be between -14:00 and +14:00.");
        }
    }

    internal void CheckXdmDateTimeMagnitude(BigInteger year)
    {
        if (BigInteger.Abs(year).ToString(CultureInfo.InvariantCulture).Length > MaximumYearDigits)
        {
            throw CreateXdmDateTimeError(XPathErrorCodes.FODT0001, "The date/time year exceeds the supported range.");
        }
    }

    internal XdmNumericParseResult InvalidXdmDateTimeLexical(XdmAtomicType type)
    {
        string? qualifiedName = TypeSystem.GetXdmAtomicTypeDefinition(type).Name.QualifiedName;
        string typeName = qualifiedName == null ? type.ToString() : qualifiedName;
        return new XdmNumericParseResult(false, null, XPathErrorCodes.FORG0001, "The lexical value is not valid for " + typeName + ".");
    }

    internal XdmNumericParseResult OverflowXdmDateTimeLexical(XdmAtomicType type)
    {
        string? qualifiedName = TypeSystem.GetXdmAtomicTypeDefinition(type).Name.QualifiedName;
        string typeName = qualifiedName == null ? type.ToString() : qualifiedName;
        return new XdmNumericParseResult(false, null, XPathErrorCodes.FOCA0002, "The lexical value exceeds the supported range of " + typeName + ".");
    }

    internal XPathDynamicException CreateXdmDateTimeError(string code, string message)
    {
        return new XdmNumericDatatype().CreateXdmNumericError(code, message);
    }

    internal void RequireXdmDateTimeType(XdmAtomicType type)
    {
        if (type < XdmAtomicType.DateTime || type > XdmAtomicType.GDay)
        {
            throw new ArgumentException("The requested type is not a date/time type.", "type");
        }
    }

    internal void RequireXdmDateTimeValue(XdmAtomicValue value)
    {
        Guard.NotNull(value, "value");
        RequireXdmDateTimeType(value.Type);
    }
    internal XdmDateTimeStorage AddXdmDayTimeDurationStorage(XdmDateTimeStorage storage, XdmAtomicType type, XdmDurationStorage duration)
    {
        int scale = storage.SecondScale > duration.SecondsScale ? storage.SecondScale : duration.SecondsScale;
        XdmNumericDatatype numeric = new XdmNumericDatatype();
        BigInteger factor = numeric.PowerOfTen(scale);
        BigInteger localSeconds = (storage.Hour * 3600 + storage.Minute * 60) * factor + numeric.ScaleXdmDecimalCoefficient(new XdmDecimalStorage(storage.SecondCoefficient, storage.SecondScale), scale);
        BigInteger addedSeconds = numeric.ScaleXdmDecimalCoefficient(new XdmDecimalStorage(duration.SecondsCoefficient, duration.SecondsScale), scale);
        BigInteger total = localSeconds + addedSeconds;
        BigInteger dayUnit = factor * 86400;
        BigInteger dayAdjustment = FloorDivideXdmCalendar(total, dayUnit);
        BigInteger remainder = total - dayAdjustment * dayUnit;
        BigInteger whole = remainder / factor;
        storage.Hour = (int)(whole / 3600);
        storage.Minute = (int)(whole % 3600 / 60);
        storage.SecondCoefficient = (whole % 60) * factor + remainder % factor;
        storage.SecondScale = scale;
        if (type != XdmAtomicType.Time)
        {
            XdmCalendarDate date = AddXdmCalendarDays(new XdmCalendarDate(storage.Year, storage.Month, storage.Day), dayAdjustment);
            storage.Year = date.Year;
            storage.Month = date.Month;
            storage.Day = date.Day;
            CheckXdmDateTimeMagnitude(storage.Year);
        }

        if (type == XdmAtomicType.Date)
        {
            storage.Hour = 0;
            storage.Minute = 0;
            storage.SecondCoefficient = BigInteger.Zero;
            storage.SecondScale = 0;
        }

        return storage;
    }
}
