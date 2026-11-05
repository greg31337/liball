using System.Numerics;

namespace odf;

internal static class FormulaDateTimeFunctions
{
    internal static OdfFormulaResult Evaluate(
        string name,
        OdfFormulaResult[] arguments,
        FormulaFunctionContext context)
    {
        OdfSpreadsheetSettings settings = context.SpreadsheetSettings;
        if (name == OdfFormulaNames.DATE)
        {
            return CreateDate(arguments, settings);
        }
        if (name == OdfFormulaNames.DATEVALUE)
        {
            return DateValue(arguments[0], settings);
        }
        if (name == OdfFormulaNames.DATEDIF)
        {
            return DateDifference(arguments, settings);
        }
        if (name == OdfFormulaNames.DAYS)
        {
            return DaysDifference(arguments, settings);
        }
        if (name == OdfFormulaNames.DAYS360)
        {
            return Days360(arguments, settings);
        }
        if (name == OdfFormulaNames.EASTERSUNDAY)
        {
            return EasterSunday(arguments, context);
        }
        if (name == OdfFormulaNames.EDATE)
        {
            return AddMonths(arguments, settings, false);
        }
        if (name == OdfFormulaNames.EOMONTH)
        {
            return AddMonths(arguments, settings, true);
        }
        if (name == OdfFormulaNames.HOUR)
        {
            return TimePart(arguments[0], 0);
        }
        if (name == OdfFormulaNames.MINUTE)
        {
            return TimePart(arguments[0], 1);
        }
        if (name == OdfFormulaNames.SECOND)
        {
            return TimePart(arguments[0], 2);
        }
        if (name == OdfFormulaNames.ISOWEEKNUM)
        {
            return WeekNumber(arguments, settings, true);
        }
        if (name == OdfFormulaNames.MONTH)
        {
            return ExtractDatePart(arguments[0], settings, 1);
        }
        if (name == OdfFormulaNames.NETWORKDAYS)
        {
            return NetworkDays(arguments, settings);
        }
        if (name == OdfFormulaNames.NOW)
        {
            return CurrentDateTime(context, false);
        }
        if (name == OdfFormulaNames.TIME)
        {
            return CreateTime(arguments);
        }
        if (name == OdfFormulaNames.TIMEVALUE)
        {
            return TimeValue(arguments[0], settings);
        }
        if (name == OdfFormulaNames.TODAY)
        {
            return CurrentDateTime(context, true);
        }
        if (name == OdfFormulaNames.WEEKDAY)
        {
            return Weekday(arguments, settings);
        }
        if (name == OdfFormulaNames.WEEKNUM)
        {
            return WeekNumber(arguments, settings, false);
        }
        if (name == OdfFormulaNames.WORKDAY)
        {
            return Workday(arguments, settings);
        }
        if (name == OdfFormulaNames.YEAR)
        {
            return ExtractDatePart(arguments[0], settings, 2);
        }
        if (name == OdfFormulaNames.YEARFRAC)
        {
            return YearFraction(arguments, settings);
        }
        return ExtractDatePart(arguments[0], settings, 0);
    }

    static OdfFormulaResult CreateDate(
        OdfFormulaResult[] arguments,
        OdfSpreadsheetSettings settings)
    {
        OdfFormulaResult yearValue = FormulaValueModel.ConvertToNumber(
            arguments[0]);
        OdfFormulaResult monthValue = FormulaValueModel.ConvertToNumber(
            arguments[1]);
        OdfFormulaResult dayValue = FormulaValueModel.ConvertToNumber(
            arguments[2]);
        if (yearValue.Error != OdfFormulaError.None)
        {
            return yearValue;
        }
        if (monthValue.Error != OdfFormulaError.None)
        {
            return monthValue;
        }
        if (dayValue.Error != OdfFormulaError.None)
        {
            return dayValue;
        }
        if (!IsSafeInteger(yearValue.NumericValue)
            || !IsSafeInteger(monthValue.NumericValue)
            || !IsSafeInteger(dayValue.NumericValue))
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        BigInteger year = new BigInteger(
            global::System.Math.Truncate(yearValue.NumericValue));
        int month = (int)global::System.Math.Truncate(
            monthValue.NumericValue);
        int day = (int)global::System.Math.Truncate(dayValue.NumericValue);
        return NormalizedDate(year, month, day, settings);
    }

    static OdfFormulaResult NormalizedDate(
        BigInteger year,
        int month,
        int day,
        OdfSpreadsheetSettings settings)
    {
        BigInteger totalMonths = year * 12 + month - 1;
        BigInteger normalizedYear = FloorDivideByTwelve(totalMonths);
        int normalizedMonth = (int)(totalMonths - normalizedYear * 12) + 1;
        if (normalizedYear == 0)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        OdfTemporalValue first = new OdfTemporalValue();
        first.Year = normalizedYear;
        first.Month = normalizedMonth;
        first.Day = 1;
        first.HasDate = true;
        first.IsValid = true;
        BigInteger target = FormulaTemporalModel.DayNumber(first) + day - 1;
        return ResultFromDayNumber(target, settings);
    }

    static OdfFormulaResult DateValue(
        OdfFormulaResult value,
        OdfSpreadsheetSettings settings)
    {
        OdfFormulaResult text = FormulaValueModel.ConvertToText(value);
        if (text.Error != OdfFormulaError.None)
        {
            return text;
        }
        OdfFormulaResult parsed = FormulaTemporalModel.CreateTemporal(
            text.StringValue,
            settings);
        if (parsed.Error != OdfFormulaError.None
            || (parsed.Kind != OdfValueKind.Date
                && parsed.Kind != OdfValueKind.DateTime))
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        double serial = global::System.Math.Truncate(parsed.NumericValue);
        return DateResult(serial, settings);
    }

    static OdfFormulaResult DateDifference(
        OdfFormulaResult[] arguments,
        OdfSpreadsheetSettings settings)
    {
        OdfTemporalValue? start = DateParts(arguments[0], settings);
        OdfTemporalValue? end = DateParts(arguments[1], settings);
        OdfFormulaResult format = FormulaValueModel.ConvertToText(arguments[2]);
        if (start == null || end == null
            || format.Error != OdfFormulaError.None)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        BigInteger startDay = FormulaTemporalModel.DayNumber(start);
        BigInteger endDay = FormulaTemporalModel.DayNumber(end);
        if (endDay < startDay)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        string unit = format.StringValue.ToLowerInvariant();
        if (unit == "d")
        {
            return FormulaValueModel.CreateNumber((double)(endDay - startDay));
        }
        BigInteger months = (end.Year - start.Year) * 12
            + end.Month - start.Month;
        if (end.Day < start.Day)
        {
            months = months - 1;
        }
        if (unit == "m")
        {
            return FormulaValueModel.CreateNumber((double)months);
        }
        if (unit == "y")
        {
            return FormulaValueModel.CreateNumber((double)(months / 12));
        }
        if (unit == "ym")
        {
            return FormulaValueModel.CreateNumber((double)(months % 12));
        }
        if (unit == "md")
        {
            return FormulaValueModel.CreateNumber(
                DifferenceIgnoringMonths(start, end));
        }
        if (unit == "yd")
        {
            return FormulaValueModel.CreateNumber(
                DifferenceIgnoringYears(start, end));
        }
        return FormulaValueModel.CreateError(OdfFormulaError.Value);
    }

    static double DifferenceIgnoringMonths(
        OdfTemporalValue start,
        OdfTemporalValue end)
    {
        if (end.Day >= start.Day)
        {
            return end.Day - start.Day;
        }
        BigInteger previousYear = end.Month == 1
            ? end.Year - 1
            : end.Year;
        int previousMonth = end.Month == 1 ? 12 : end.Month - 1;
        int startDay = start.Day;
        int days = DaysInMonth(previousYear, previousMonth);
        if (startDay > days)
        {
            startDay = days;
        }
        return days - startDay + end.Day;
    }

    static double DifferenceIgnoringYears(
        OdfTemporalValue start,
        OdfTemporalValue end)
    {
        OdfTemporalValue anniversary = CreateValidDate(
            end.Year,
            start.Month,
            start.Day);
        BigInteger anniversaryDay = FormulaTemporalModel.DayNumber(anniversary);
        BigInteger endDay = FormulaTemporalModel.DayNumber(end);
        if (anniversaryDay > endDay)
        {
            anniversary = CreateValidDate(
                end.Year - 1,
                start.Month,
                start.Day);
            anniversaryDay = FormulaTemporalModel.DayNumber(anniversary);
        }
        return (double)(endDay - anniversaryDay);
    }

    static OdfFormulaResult DaysDifference(
        OdfFormulaResult[] arguments,
        OdfSpreadsheetSettings settings)
    {
        OdfFormulaResult end = DateSerial(arguments[0], settings);
        OdfFormulaResult start = DateSerial(arguments[1], settings);
        if (end.Error != OdfFormulaError.None)
        {
            return end;
        }
        if (start.Error != OdfFormulaError.None)
        {
            return start;
        }
        return FormulaValueModel.CreateNumber(
            end.NumericValue - start.NumericValue);
    }

    static OdfFormulaResult Days360(
        OdfFormulaResult[] arguments,
        OdfSpreadsheetSettings settings)
    {
        OdfTemporalValue? start = DateParts(arguments[0], settings);
        OdfTemporalValue? end = DateParts(arguments[1], settings);
        if (start == null || end == null)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        bool european = false;
        if (arguments.Length > 2)
        {
            OdfFormulaResult method = FormulaValueModel.ConvertToBoolean(
                arguments[2]);
            if (method.Error != OdfFormulaError.None)
            {
                return method;
            }
            european = method.BooleanValue;
        }
        return FormulaValueModel.CreateNumber(
            ThirtyDayDifference(start, end, european));
    }

    static double ThirtyDayDifference(
        OdfTemporalValue start,
        OdfTemporalValue end,
        bool european)
    {
        int sign = 1;
        OdfTemporalValue left = start;
        OdfTemporalValue right = end;
        if (european
            && FormulaTemporalModel.DayNumber(left)
                > FormulaTemporalModel.DayNumber(right))
        {
            left = end;
            right = start;
            sign = -1;
        }
        int startDay = left.Day;
        int endDay = right.Day;
        if (startDay == 31
            || (!european
                && left.Month == 2
                && startDay == DaysInMonth(left.Year, left.Month)))
        {
            startDay = 30;
        }
        if (endDay == 31 && (european || startDay == 30))
        {
            endDay = 30;
        }
        BigInteger value = (right.Year - left.Year) * 360
            + (right.Month - left.Month) * 30
            + endDay - startDay;
        return sign * (double)value;
    }

    static OdfFormulaResult EasterSunday(
        OdfFormulaResult[] arguments,
        FormulaFunctionContext context)
    {
        int year;
        if (arguments.Length > 0)
        {
            OdfFormulaResult number = FormulaValueModel.ConvertToNumber(
                arguments[0]);
            if (number.Error != OdfFormulaError.None
                || !IsSafeInteger(number.NumericValue))
            {
                return FormulaValueModel.CreateError(OdfFormulaError.Value);
            }
            year = (int)global::System.Math.Truncate(number.NumericValue);
        }
        else
        {
            if (context.CurrentDateTime == null)
            {
                return FormulaValueModel.CreateError(
                    OdfFormulaError.NotAvailable);
            }
            OdfTemporalValue? current = DateParts(
                context.CurrentDateTime,
                context.SpreadsheetSettings);
            if (current == null || current.Year > int.MaxValue)
            {
                return FormulaValueModel.CreateError(OdfFormulaError.Value);
            }
            year = (int)current.Year;
            OdfFormulaResult thisYear = EasterForYear(
                year,
                context.SpreadsheetSettings);
            if (thisYear.Error == OdfFormulaError.None
                && thisYear.NumericValue
                    >= global::System.Math.Truncate(
                        context.CurrentDateTime.NumericValue))
            {
                return thisYear;
            }
            year = year + 1;
        }
        return EasterForYear(year, context.SpreadsheetSettings);
    }

    static OdfFormulaResult EasterForYear(
        int year,
        OdfSpreadsheetSettings settings)
    {
        if (year < 1583 || year > 9956)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        int a1 = year / 100;
        int a2 = year - 19 * (year / 19);
        int a3 = (a1 - 17) / 25;
        int a4 = ModuloInt(
            a1 - a1 / 4 - (a1 - a3) / 3 + 19 * a2 + 15,
            30);
        int a5 = a4 - (a4 / 28)
            * (1 - (a4 / 28) * (29 / (a4 + 1)) * ((21 - a2) / 11));
        int a6 = ModuloInt(
            year + year / 4 + a5 + 2 - a1 + a1 / 4,
            7);
        int a7 = a5 - a6;
        int month = 3 + (a7 + 40) / 44;
        int day = a7 + 28 - 31 * (month / 4);
        return NormalizedDate(year, month, day, settings);
    }

    static OdfFormulaResult AddMonths(
        OdfFormulaResult[] arguments,
        OdfSpreadsheetSettings settings,
        bool endOfMonth)
    {
        OdfTemporalValue? start = DateParts(arguments[0], settings);
        OdfFormulaResult add = FormulaValueModel.ConvertToNumber(arguments[1]);
        if (start == null || add.Error != OdfFormulaError.None
            || !IsSafeInteger(add.NumericValue))
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        int months = (int)global::System.Math.Truncate(add.NumericValue);
        BigInteger total = start.Year * 12 + start.Month - 1 + months;
        BigInteger year = FloorDivideByTwelve(total);
        int month = (int)(total - year * 12) + 1;
        int day = endOfMonth
            ? DaysInMonth(year, month)
            : start.Day;
        int maximum = DaysInMonth(year, month);
        if (day > maximum)
        {
            day = maximum;
        }
        return NormalizedDate(year, month, day, settings);
    }

    static OdfFormulaResult TimePart(OdfFormulaResult value, int part)
    {
        OdfFormulaResult number = FormulaValueModel.ConvertToNumber(value);
        if (number.Error != OdfFormulaError.None)
        {
            return number;
        }
        double totalSeconds = number.NumericValue * 86400;
        if (part == 2)
        {
            double rounded = global::System.Math.Round(
                totalSeconds,
                0,
                global::System.MidpointRounding.AwayFromZero);
            return FormulaValueModel.CreateNumber(ModuloDouble(rounded, 60));
        }
        double fraction = ModuloDouble(number.NumericValue, 1);
        if (part == 0)
        {
            return FormulaValueModel.CreateNumber(
                global::System.Math.Floor(fraction * 24));
        }
        return FormulaValueModel.CreateNumber(
            global::System.Math.Floor(fraction * 1440) % 60);
    }

    static OdfFormulaResult CreateTime(OdfFormulaResult[] arguments)
    {
        OdfFormulaResult hours = FormulaValueModel.ConvertToNumber(arguments[0]);
        OdfFormulaResult minutes = FormulaValueModel.ConvertToNumber(arguments[1]);
        OdfFormulaResult seconds = FormulaValueModel.ConvertToNumber(arguments[2]);
        if (hours.Error != OdfFormulaError.None)
        {
            return hours;
        }
        if (minutes.Error != OdfFormulaError.None)
        {
            return minutes;
        }
        if (seconds.Error != OdfFormulaError.None)
        {
            return seconds;
        }
        OdfFormulaResult result = FormulaValueModel.CreateNumber(
            (hours.NumericValue * 3600
                + minutes.NumericValue * 60
                + seconds.NumericValue) / 86400);
        if (result.Error == OdfFormulaError.None)
        {
            result.Kind = OdfValueKind.Time;
        }
        return result;
    }

    static OdfFormulaResult TimeValue(
        OdfFormulaResult value,
        OdfSpreadsheetSettings settings)
    {
        OdfFormulaResult text = FormulaValueModel.ConvertToText(value);
        if (text.Error != OdfFormulaError.None)
        {
            return text;
        }
        OdfFormulaResult parsed = FormulaTemporalModel.CreateTemporal(
            text.StringValue,
            settings);
        if (parsed.Error != OdfFormulaError.None
            || (parsed.Kind != OdfValueKind.Time
                && parsed.Kind != OdfValueKind.DateTime))
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        OdfFormulaResult result = FormulaValueModel.CreateNumber(
            ModuloDouble(parsed.NumericValue, 1));
        result.Kind = OdfValueKind.Time;
        return result;
    }

    static OdfFormulaResult CurrentDateTime(
        FormulaFunctionContext context,
        bool dateOnly)
    {
        if (context.CurrentDateTime == null
            || context.CurrentDateTime.Error != OdfFormulaError.None)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.NotAvailable);
        }
        if (!dateOnly)
        {
            return context.CurrentDateTime;
        }
        return DateResult(
            global::System.Math.Truncate(
                context.CurrentDateTime.NumericValue),
            context.SpreadsheetSettings);
    }

    static OdfFormulaResult ExtractDatePart(
        OdfFormulaResult value,
        OdfSpreadsheetSettings settings,
        int part)
    {
        OdfTemporalValue? date = DateParts(value, settings);
        if (date == null)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        if (part == 0)
        {
            return FormulaValueModel.CreateNumber(date.Day);
        }
        if (part == 1)
        {
            return FormulaValueModel.CreateNumber(date.Month);
        }
        return FormulaValueModel.CreateNumber((double)date.Year);
    }

    static OdfFormulaResult Weekday(
        OdfFormulaResult[] arguments,
        OdfSpreadsheetSettings settings)
    {
        OdfTemporalValue? date = DateParts(arguments[0], settings);
        if (date == null)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        int type = 1;
        if (arguments.Length > 1)
        {
            OdfFormulaResult mode = FormulaValueModel.ConvertToNumber(
                arguments[1]);
            if (mode.Error != OdfFormulaError.None
                || !IsSafeInteger(mode.NumericValue))
            {
                return FormulaValueModel.CreateError(OdfFormulaError.Value);
            }
            type = (int)global::System.Math.Truncate(mode.NumericValue);
        }
        int monday = MondayIndex(FormulaTemporalModel.DayNumber(date));
        if (type == 3)
        {
            return FormulaValueModel.CreateNumber(monday);
        }
        int start = WeekStartIndex(type);
        if (start < 0)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        return FormulaValueModel.CreateNumber(
            ModuloInt(monday - start, 7) + 1);
    }

    static OdfFormulaResult WeekNumber(
        OdfFormulaResult[] arguments,
        OdfSpreadsheetSettings settings,
        bool forceIso)
    {
        OdfTemporalValue? date = DateParts(arguments[0], settings);
        if (date == null)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        int mode = forceIso ? 21 : 1;
        if (!forceIso && arguments.Length > 1)
        {
            OdfFormulaResult modeValue = FormulaValueModel.ConvertToNumber(
                arguments[1]);
            if (modeValue.Error != OdfFormulaError.None
                || !IsSafeInteger(modeValue.NumericValue))
            {
                return FormulaValueModel.CreateError(OdfFormulaError.Value);
            }
            mode = (int)global::System.Math.Truncate(modeValue.NumericValue);
        }
        if (mode == 21 || mode == 150)
        {
            return FormulaValueModel.CreateNumber(IsoWeekNumber(date));
        }
        int start = WeekStartIndex(mode);
        if (start < 0 || mode == 3)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        OdfTemporalValue january = CreateValidDate(date.Year, 1, 1);
        BigInteger day = FormulaTemporalModel.DayNumber(date);
        BigInteger first = FormulaTemporalModel.DayNumber(january);
        int offset = ModuloInt(MondayIndex(first) - start, 7);
        return FormulaValueModel.CreateNumber(
            (double)((day - first + offset) / 7 + 1));
    }

    static int IsoWeekNumber(OdfTemporalValue date)
    {
        BigInteger day = FormulaTemporalModel.DayNumber(date);
        int weekday = MondayIndex(day) + 1;
        BigInteger thursday = day + 4 - weekday;
        OdfTemporalValue isoDate = FormulaTemporalModel.DateFromDayNumber(
            thursday);
        OdfTemporalValue januaryFourth = CreateValidDate(
            isoDate.Year,
            1,
            4);
        BigInteger januaryDay = FormulaTemporalModel.DayNumber(januaryFourth);
        BigInteger firstMonday = januaryDay - MondayIndex(januaryDay);
        return (int)((thursday - firstMonday) / 7 + 1);
    }

    static OdfFormulaResult NetworkDays(
        OdfFormulaResult[] arguments,
        OdfSpreadsheetSettings settings)
    {
        OdfTemporalValue? first = DateParts(arguments[0], settings);
        OdfTemporalValue? second = DateParts(arguments[1], settings);
        if (first == null || second == null
            || !ValidWorkweek(arguments))
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        BigInteger start = FormulaTemporalModel.DayNumber(first);
        BigInteger end = FormulaTemporalModel.DayNumber(second);
        int sign = 1;
        if (start > end)
        {
            BigInteger swap = start;
            start = end;
            end = swap;
            sign = -1;
        }
        int count = 0;
        BigInteger day = start;
        while (day <= end)
        {
            if (IsWorkday(day, arguments)
                && !IsHoliday(day, arguments, settings))
            {
                count = count + 1;
            }
            day = day + 1;
        }
        return FormulaValueModel.CreateNumber(sign * count);
    }

    static OdfFormulaResult Workday(
        OdfFormulaResult[] arguments,
        OdfSpreadsheetSettings settings)
    {
        OdfTemporalValue? date = DateParts(arguments[0], settings);
        OdfFormulaResult offsetValue = FormulaValueModel.ConvertToNumber(
            arguments[1]);
        if (date == null
            || offsetValue.Error != OdfFormulaError.None
            || !IsSafeInteger(offsetValue.NumericValue)
            || !ValidWorkweek(arguments))
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        int offset = (int)global::System.Math.Truncate(
            offsetValue.NumericValue);
        BigInteger day = FormulaTemporalModel.DayNumber(date);
        if (offset == 0)
        {
            return ResultFromDayNumber(day, settings);
        }
        if (!HasWorkingDay(arguments))
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        int direction = offset < 0 ? -1 : 1;
        int remaining = global::System.Math.Abs(offset);
        while (remaining > 0)
        {
            day = day + direction;
            if (IsWorkday(day, arguments)
                && !IsHoliday(day, arguments, settings))
            {
                remaining = remaining - 1;
            }
        }
        return ResultFromDayNumber(day, settings);
    }

    static bool ValidWorkweek(OdfFormulaResult[] arguments)
    {
        if (arguments.Length < 4
            || arguments[3].Kind == OdfValueKind.Empty)
        {
            return true;
        }
        OdfFormulaResult value = arguments[3];
        if ((value.Kind != OdfValueKind.Array
                && value.Kind != OdfValueKind.Reference)
            || value.Values.Length != 7)
        {
            return false;
        }
        int index = 0;
        while (index < value.Values.Length)
        {
            OdfFormulaResult number = FormulaValueModel.ConvertToNumber(
                value.Values[index]);
            if (number.Error != OdfFormulaError.None)
            {
                return false;
            }
            index = index + 1;
        }
        return (value.Kind == OdfValueKind.Array
                || value.Kind == OdfValueKind.Reference)
            && value.Values.Length == 7;
    }

    static bool HasWorkingDay(OdfFormulaResult[] arguments)
    {
        if (arguments.Length < 4
            || arguments[3].Kind == OdfValueKind.Empty)
        {
            return true;
        }
        int index = 0;
        while (index < arguments[3].Values.Length)
        {
            OdfFormulaResult number = FormulaValueModel.ConvertToNumber(
                arguments[3].Values[index]);
            if (number.Error == OdfFormulaError.None
                && number.NumericValue == 0)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool IsWorkday(
        BigInteger day,
        OdfFormulaResult[] arguments)
    {
        int monday = MondayIndex(day);
        int sundayIndex = (monday + 1) % 7;
        if (arguments.Length < 4
            || arguments[3].Kind == OdfValueKind.Empty)
        {
            return sundayIndex != 0 && sundayIndex != 6;
        }
        OdfFormulaResult number = FormulaValueModel.ConvertToNumber(
            arguments[3].Values[sundayIndex]);
        return number.Error == OdfFormulaError.None
            && number.NumericValue == 0;
    }

    static bool IsHoliday(
        BigInteger day,
        OdfFormulaResult[] arguments,
        OdfSpreadsheetSettings settings)
    {
        if (arguments.Length < 3
            || arguments[2].Kind == OdfValueKind.Empty)
        {
            return false;
        }
        OdfFormulaResult holidays = arguments[2];
        if (holidays.Kind != OdfValueKind.Array
            && holidays.Kind != OdfValueKind.Reference)
        {
            OdfTemporalValue? single = DateParts(holidays, settings);
            return single != null
                && FormulaTemporalModel.DayNumber(single) == day;
        }
        int index = 0;
        while (index < holidays.Values.Length)
        {
            OdfTemporalValue? holiday = DateParts(
                holidays.Values[index],
                settings);
            if (holiday != null
                && FormulaTemporalModel.DayNumber(holiday) == day)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static OdfFormulaResult YearFraction(
        OdfFormulaResult[] arguments,
        OdfSpreadsheetSettings settings)
    {
        OdfTemporalValue? start = DateParts(arguments[0], settings);
        OdfTemporalValue? end = DateParts(arguments[1], settings);
        if (start == null || end == null)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        int basis = 0;
        if (arguments.Length > 2)
        {
            OdfFormulaResult basisValue = FormulaValueModel.ConvertToNumber(
                arguments[2]);
            if (basisValue.Error != OdfFormulaError.None
                || !IsSafeInteger(basisValue.NumericValue))
            {
                return FormulaValueModel.CreateError(OdfFormulaError.Value);
            }
            basis = (int)global::System.Math.Truncate(basisValue.NumericValue);
        }
        if (basis < 0 || basis > 4)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        BigInteger startDay = FormulaTemporalModel.DayNumber(start);
        BigInteger endDay = FormulaTemporalModel.DayNumber(end);
        int sign = 1;
        if (startDay > endDay)
        {
            OdfTemporalValue swap = start;
            start = end;
            end = swap;
            BigInteger swapDay = startDay;
            startDay = endDay;
            endDay = swapDay;
            sign = -1;
        }
        if (basis == 0 || basis == 4)
        {
            return FormulaValueModel.CreateNumber(
                sign * ThirtyDayDifference(start, end, basis == 4) / 360);
        }
        double actual = (double)(endDay - startDay);
        if (basis == 2)
        {
            return FormulaValueModel.CreateNumber(sign * actual / 360);
        }
        if (basis == 3)
        {
            return FormulaValueModel.CreateNumber(sign * actual / 365);
        }
        return FormulaValueModel.CreateNumber(
            sign * ActualActualFraction(start, end));
    }

    static double ActualActualFraction(
        OdfTemporalValue start,
        OdfTemporalValue end)
    {
        BigInteger startDay = FormulaTemporalModel.DayNumber(start);
        BigInteger endDay = FormulaTemporalModel.DayNumber(end);
        if (start.Year == end.Year)
        {
            return (double)(endDay - startDay)
                / DaysInYear(start.Year);
        }
        OdfTemporalValue nextYear = CreateValidDate(start.Year + 1, 1, 1);
        double result = (double)(FormulaTemporalModel.DayNumber(nextYear)
                - startDay)
            / DaysInYear(start.Year);
        BigInteger year = start.Year + 1;
        while (year < end.Year)
        {
            result = result + 1;
            year = year + 1;
        }
        OdfTemporalValue endYear = CreateValidDate(end.Year, 1, 1);
        result = result + (double)(endDay
                - FormulaTemporalModel.DayNumber(endYear))
            / DaysInYear(end.Year);
        return result;
    }

    static OdfFormulaResult DateSerial(
        OdfFormulaResult value,
        OdfSpreadsheetSettings settings)
    {
        if (value.Kind == OdfValueKind.String)
        {
            return DateValue(value, settings);
        }
        return FormulaValueModel.ConvertToNumber(value);
    }

    static OdfTemporalValue? DateParts(
        OdfFormulaResult value,
        OdfSpreadsheetSettings settings)
    {
        OdfFormulaResult serial = DateSerial(value, settings);
        if (serial.Error != OdfFormulaError.None
            || !double.IsFinite(serial.NumericValue))
        {
            return null;
        }
        OdfTemporalValue nullDate = FormulaTemporalModel.ParseNullDate(
            settings);
        if (!nullDate.IsValid)
        {
            return null;
        }
        BigInteger day = FormulaTemporalModel.DayNumber(nullDate)
            + new BigInteger(global::System.Math.Truncate(
                serial.NumericValue));
        return FormulaTemporalModel.DateFromDayNumber(day);
    }

    static OdfFormulaResult ResultFromDayNumber(
        BigInteger day,
        OdfSpreadsheetSettings settings)
    {
        OdfTemporalValue nullDate = FormulaTemporalModel.ParseNullDate(
            settings);
        if (!nullDate.IsValid)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        double serial = (double)(day - FormulaTemporalModel.DayNumber(nullDate));
        return DateResult(serial, settings);
    }

    static OdfFormulaResult DateResult(
        double serial,
        OdfSpreadsheetSettings settings)
    {
        return FormulaTemporalModel.TemporalFromSerial(
            serial,
            OdfValueKind.Date,
            settings,
            0,
            false);
    }

    static OdfTemporalValue CreateValidDate(
        BigInteger year,
        int month,
        int requestedDay)
    {
        OdfTemporalValue result = new OdfTemporalValue();
        result.Year = year;
        result.Month = month;
        int maximum = DaysInMonth(year, month);
        result.Day = requestedDay > maximum ? maximum : requestedDay;
        result.HasDate = true;
        result.IsValid = true;
        return result;
    }

    static int DaysInMonth(BigInteger year, int month)
    {
        if (month == 12)
        {
            return 31;
        }
        return FormulaTemporalModel.MonthStartForYear(year, month + 1)
            - FormulaTemporalModel.MonthStartForYear(year, month);
    }

    static int DaysInYear(BigInteger year)
    {
        return FormulaTemporalModel.IsLeapYear(year) ? 366 : 365;
    }

    static int MondayIndex(BigInteger day)
    {
        return (int)FormulaTemporalModel.Modulo(day, 7);
    }

    static int WeekStartIndex(int mode)
    {
        if (mode == 1 || mode == 17)
        {
            return 6;
        }
        if (mode == 2 || mode == 11)
        {
            return 0;
        }
        if (mode >= 12 && mode <= 16)
        {
            return mode - 11;
        }
        return -1;
    }

    static BigInteger FloorDivideByTwelve(BigInteger value)
    {
        BigInteger result = value / 12;
        if (value % 12 < 0)
        {
            result = result - 1;
        }
        return result;
    }

    static int ModuloInt(int value, int divisor)
    {
        int result = value % divisor;
        return result < 0 ? result + divisor : result;
    }

    static double ModuloDouble(double value, double divisor)
    {
        double result = value % divisor;
        return result < 0 ? result + divisor : result;
    }

    static bool IsSafeInteger(double value)
    {
        return double.IsFinite(value)
            && value > int.MinValue
            && value <= int.MaxValue;
    }
}
