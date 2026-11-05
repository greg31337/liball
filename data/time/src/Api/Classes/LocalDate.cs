using System;
using System.Globalization;

namespace time;

/// <summary>Represents a Gregorian calendar date without a time, offset, or time zone.</summary>
/// <remarks>Years 1 through 9999 are supported. Month and year arithmetic constrains an invalid result day to the last day of the target month.</remarks>
public class LocalDate : ILocalDate
{
    int year;
    int month;
    int day;

    internal LocalDate(int year, int month, int day)
    {
        TimeValidation.RequireDay(year, month, day, "day");

        this.year = year;
        this.month = month;
        this.day = day;
    }

    /// <summary>Creates a validated Gregorian date.</summary>
    /// <param name="year">The year from 1 through 9999.</param><param name="month">The month from 1 through 12.</param><param name="day">The valid day within the month.</param>
    /// <returns>The requested local date.</returns>
    internal static LocalDate Create(int year, int month, int day)
    {
        return new LocalDate(year, month, day);
    }

    /// <summary>Creates a local date from a built-in date-only value.</summary><param name="date">The value to copy.</param><returns>The equivalent local date.</returns>
    internal static LocalDate FromDateOnly(DateOnly date)
    {
        return new LocalDate(date.Year, date.Month, date.Day);
    }

    /// <summary>Creates a local date from the calendar fields of a built-in date-time.</summary>
    /// <remarks>The time and kind are ignored; no time-zone conversion occurs.</remarks>
    /// <param name="dateTime">The value whose year, month, and day are copied.</param><returns>The equivalent local date.</returns>
    internal static LocalDate FromDateTime(DateTime dateTime)
    {
        return new LocalDate(dateTime.Year, dateTime.Month, dateTime.Day);
    }

    /// <summary>Creates a date from its zero-based day number since 0001-01-01.</summary><param name="dayNumber">A supported .NET day number.</param><returns>The corresponding date.</returns>
    internal static LocalDate FromDayNumber(int dayNumber)
    {
        TimeValidation.RequireDayNumber(dayNumber, "dayNumber");

        return new LocalDate(
            CalendarMath.GetYearFromDayNumber(dayNumber),
            CalendarMath.GetMonthFromDayNumber(dayNumber),
            CalendarMath.GetDayFromDayNumber(dayNumber));
    }

    /// <summary>Parses invariant ISO date text.</summary><param name="text">Text in <c>yyyy-MM-dd</c> form.</param><returns>The parsed date.</returns>
    /// <exception cref="FormatException">The text is not a valid supported date.</exception>
    internal static LocalDate Parse(string text)
    {
        TimeValidation.RequireNotNull(text, "text");

        if (text.Length != 10)
        {
            throw new FormatException("Local date text must use YYYY-MM-DD format.");
        }

        if (text[4] != '-' || text[7] != '-')
        {
            throw new FormatException("Local date text must use YYYY-MM-DD format.");
        }

        int parsedYear = ParseFixedDigits(text, 0, 4);
        int parsedMonth = ParseFixedDigits(text, 5, 2);
        int parsedDay = ParseFixedDigits(text, 8, 2);

        if (parsedYear < TimeConstants.MinYear || parsedYear > TimeConstants.MaxYear)
        {
            throw new FormatException("Local date year is outside the supported range.");
        }

        if (parsedMonth < TimeConstants.MinMonth || parsedMonth > TimeConstants.MaxMonth)
        {
            throw new FormatException("Local date month is outside the supported range.");
        }

        int daysInMonth = CalendarMath.GetDaysInMonth(parsedYear, parsedMonth);
        if (parsedDay < TimeConstants.MinDay || parsedDay > daysInMonth)
        {
            throw new FormatException("Local date day is not valid for the parsed year and month.");
        }

        return new LocalDate(parsedYear, parsedMonth, parsedDay);
    }

    /// <summary>Parses invariant ISO basic date text.</summary><param name="text">Text in <c>yyyyMMdd</c> form.</param><returns>The parsed date.</returns>
    /// <exception cref="FormatException">The text is not a valid supported date.</exception>
    internal static LocalDate ParseIsoBasic(string text)
    {
        TimeValidation.RequireNotNull(text, "text");

        if (text.Length != 8)
        {
            throw new FormatException("Local date text must use YYYYMMDD format.");
        }

        int parsedYear = ParseFixedDigits(text, 0, 4);
        int parsedMonth = ParseFixedDigits(text, 4, 2);
        int parsedDay = ParseFixedDigits(text, 6, 2);

        if (parsedYear < TimeConstants.MinYear || parsedYear > TimeConstants.MaxYear)
        {
            throw new FormatException("Local date year is outside the supported range.");
        }

        if (parsedMonth < TimeConstants.MinMonth || parsedMonth > TimeConstants.MaxMonth)
        {
            throw new FormatException("Local date month is outside the supported range.");
        }

        int daysInMonth = CalendarMath.GetDaysInMonth(parsedYear, parsedMonth);
        if (parsedDay < TimeConstants.MinDay || parsedDay > daysInMonth)
        {
            throw new FormatException("Local date day is not valid for the parsed year and month.");
        }

        return new LocalDate(parsedYear, parsedMonth, parsedDay);
    }

    /// <summary>Gets the Gregorian year.</summary><returns>A value from 1 through 9999.</returns>
    public int GetYear()
    {
        return year;
    }

    /// <summary>Gets the month number.</summary><returns>A value from 1 through 12.</returns>
    public int GetMonth()
    {
        return month;
    }

    /// <summary>Gets the day of the month.</summary><returns>A valid day in the current month.</returns>
    public int GetDay()
    {
        return day;
    }

    /// <summary>Gets the ISO weekday number.</summary><returns>Monday as 1 through Sunday as 7.</returns>
    public int GetDayOfWeek()
    {
        return CalendarMath.GetDayOfWeek(year, month, day);
    }

    /// <summary>Gets the one-based day within the year.</summary><returns>A value from 1 through 365 or 366.</returns>
    public int GetDayOfYear()
    {
        return CalendarMath.GetDayOfYear(year, month, day);
    }

    /// <summary>Gets the zero-based number of days since 0001-01-01.</summary><returns>The day number compatible with <see cref="DateOnly"/>.</returns>
    public int GetDayNumber()
    {
        return CalendarMath.GetDayNumber(year, month, day);
    }

    /// <summary>Gets the length of this date's month.</summary><returns>28 through 31 days.</returns>
    public int GetDaysInMonth()
    {
        return CalendarMath.GetDaysInMonth(year, month);
    }

    /// <summary>Gets the length of this date's year.</summary><returns>365 or 366 days.</returns>
    public int GetDaysInYear()
    {
        return CalendarMath.GetDaysInYear(year);
    }

    /// <summary>Reports whether this date's Gregorian year contains February 29.</summary><returns><c>true</c> for a leap year.</returns>
    public bool IsLeapYear()
    {
        return CalendarMath.IsLeapYear(year);
    }

    /// <summary>Orders dates by calendar day.</summary><param name="other">The date to compare.</param><returns>A negative value, zero, or a positive value when this date is earlier than, equal to, or later than the other.</returns>
    public int CompareTo(LocalDate other)
    {
        TimeValidation.RequireNotNull(other, "other");

        int yearComparison = TimeComparison.CompareInt(year, other.GetYear());
        if (yearComparison != 0)
        {
            return yearComparison;
        }

        int monthComparison = TimeComparison.CompareInt(month, other.GetMonth());
        if (monthComparison != 0)
        {
            return monthComparison;
        }

        return TimeComparison.CompareInt(day, other.GetDay());
    }

    /// <summary>Tests equality of year, month, and day.</summary><param name="other">The date to compare, or null.</param><returns><c>true</c> when both dates identify the same calendar day.</returns>
    public bool EqualsValue(LocalDate? other)
    {
        if (other == null)
        {
            return false;
        }

        return year == other.GetYear()
            && month == other.GetMonth()
            && day == other.GetDay();
    }

    /// <summary>Converts this value to the built-in date-only type.</summary><returns>A date with the same calendar fields.</returns>
    public DateOnly ToDateOnly()
    {
        return new DateOnly(year, month, day);
    }

    /// <summary>Formats this date in invariant ISO form.</summary><returns>Text in <c>yyyy-MM-dd</c> form.</returns>
    public string ToIsoString()
    {
        return year.ToString("D4", CultureInfo.InvariantCulture)
            + "-"
            + month.ToString("D2", CultureInfo.InvariantCulture)
            + "-"
            + day.ToString("D2", CultureInfo.InvariantCulture);
    }

    /// <summary>Formats this date in invariant ISO basic form.</summary><returns>Text in <c>yyyyMMdd</c> form.</returns>
    public string ToIsoBasicString()
    {
        return year.ToString("D4", CultureInfo.InvariantCulture)
            + month.ToString("D2", CultureInfo.InvariantCulture)
            + day.ToString("D2", CultureInfo.InvariantCulture);
    }

    /// <summary>Adds calendar days.</summary><param name="days">The signed number of days.</param><returns>The resulting date.</returns>
    public LocalDate AddDays(long days)
    {
        long dayNumber = GetDayNumber() + days;
        if (dayNumber < TimeConstants.MinDayNumber || dayNumber > TimeConstants.MaxDayNumber)
        {
            throw new ArgumentOutOfRangeException("days", "Date arithmetic moved outside the supported local date range.");
        }

        return FromDayNumber((int)dayNumber);
    }

    /// <summary>Subtracts calendar days.</summary><param name="days">The signed number of days.</param><returns>The resulting date.</returns>
    public LocalDate SubtractDays(long days)
    {
        return AddDays(-days);
    }

    /// <summary>Adds seven-day calendar weeks.</summary><param name="weeks">The signed number of weeks.</param><returns>The resulting date.</returns>
    public LocalDate AddWeeks(long weeks)
    {
        return AddDays(weeks * TimeConstants.DaysPerWeek);
    }

    /// <summary>Subtracts seven-day calendar weeks.</summary><param name="weeks">The signed number of weeks.</param><returns>The resulting date.</returns>
    public LocalDate SubtractWeeks(long weeks)
    {
        return AddWeeks(-weeks);
    }

    /// <summary>Adds Gregorian months, constraining the day at month end.</summary><param name="months">The signed number of months.</param><returns>The resulting date.</returns>
    public LocalDate AddMonths(long months)
    {
        long currentMonthIndex = (long)(year - 1) * TimeConstants.MonthsPerYear + month - 1L;
        long newMonthIndex = currentMonthIndex + months;
        long maxMonthIndex = (long)TimeConstants.MaxYear * TimeConstants.MonthsPerYear - 1L;

        if (newMonthIndex < 0L || newMonthIndex > maxMonthIndex)
        {
            throw new ArgumentOutOfRangeException("months", "Date arithmetic moved outside the supported local date range.");
        }

        int newYear = (int)(newMonthIndex / TimeConstants.MonthsPerYear) + 1;
        int newMonth = (int)(newMonthIndex % TimeConstants.MonthsPerYear) + 1;
        int newDay = ConstrainDayToMonth(newYear, newMonth, day);

        return new LocalDate(newYear, newMonth, newDay);
    }

    /// <summary>Subtracts Gregorian months, constraining the day at month end.</summary><param name="months">The signed number of months.</param><returns>The resulting date.</returns>
    public LocalDate SubtractMonths(long months)
    {
        return AddMonths(-months);
    }

    /// <summary>Adds Gregorian years, constraining February 29 to February 28 when needed.</summary><param name="years">The signed number of years.</param><returns>The resulting date.</returns>
    public LocalDate AddYears(long years)
    {
        long newYearValue = (long)year + years;
        if (newYearValue < TimeConstants.MinYear || newYearValue > TimeConstants.MaxYear)
        {
            throw new ArgumentOutOfRangeException("years", "Date arithmetic moved outside the supported local date range.");
        }

        int newYear = (int)newYearValue;
        int newDay = ConstrainDayToMonth(newYear, month, day);

        return new LocalDate(newYear, month, newDay);
    }

    /// <summary>Subtracts Gregorian years, constraining February 29 to February 28 when needed.</summary><param name="years">The signed number of years.</param><returns>The resulting date.</returns>
    public LocalDate SubtractYears(long years)
    {
        return AddYears(-years);
    }

    /// <summary>Adds the date components of a calendar period.</summary>
    /// <remarks>Years, then months, then weeks and days are applied. Nonzero time components are rejected because this type has no time of day.</remarks>
    /// <param name="period">The calendar amount to add.</param><returns>The resulting date.</returns>
    public LocalDate AddPeriod(Period period)
    {
        TimeValidation.RequireNotNull(period, "period");
        RequireDateOnlyPeriod(period);

        LocalDate result = AddYears(period.GetYears());
        result = result.AddMonths(period.GetMonths());
        result = result.AddWeeks(period.GetWeeks());
        result = result.AddDays(period.GetDays());

        return result;
    }

    /// <summary>Subtracts the date components of a calendar period.</summary><param name="period">The calendar amount to subtract.</param><returns>The resulting date.</returns>
    public LocalDate SubtractPeriod(Period period)
    {
        TimeValidation.RequireNotNull(period, "period");

        return AddPeriod(period.Negate());
    }

    /// <summary>Gets the first day of this date's month.</summary><returns>A date whose day is 1.</returns>
    public LocalDate StartOfMonth()
    {
        return new LocalDate(year, month, TimeConstants.MinDay);
    }

    /// <summary>Gets the final day of this date's month.</summary><returns>The last valid date in the month.</returns>
    public LocalDate EndOfMonth()
    {
        return new LocalDate(year, month, CalendarMath.GetDaysInMonth(year, month));
    }

    /// <summary>Gets January 1 in this date's year.</summary><returns>The first date of the year.</returns>
    public LocalDate StartOfYear()
    {
        return new LocalDate(year, TimeConstants.MinMonth, TimeConstants.MinDay);
    }

    /// <summary>Gets December 31 in this date's year.</summary><returns>The final date of the year.</returns>
    public LocalDate EndOfYear()
    {
        return new LocalDate(year, TimeConstants.MaxMonth, CalendarMath.GetDaysInMonth(year, TimeConstants.MaxMonth));
    }

    /// <summary>Finds the next occurrence of an ISO weekday, never returning this date.</summary><param name="dayOfWeek">Monday as 1 through Sunday as 7.</param><returns>A date one through seven days later.</returns>
    public LocalDate Next(int dayOfWeek)
    {
        TimeValidation.RequireIsoDayOfWeek(dayOfWeek, "dayOfWeek");

        int delta = dayOfWeek - GetDayOfWeek();
        if (delta <= 0)
        {
            delta = delta + TimeConstants.DaysPerWeek;
        }

        return AddDays(delta);
    }

    /// <summary>Finds the previous occurrence of an ISO weekday, never returning this date.</summary><param name="dayOfWeek">Monday as 1 through Sunday as 7.</param><returns>A date one through seven days earlier.</returns>
    public LocalDate Previous(int dayOfWeek)
    {
        TimeValidation.RequireIsoDayOfWeek(dayOfWeek, "dayOfWeek");

        int delta = GetDayOfWeek() - dayOfWeek;
        if (delta <= 0)
        {
            delta = delta + TimeConstants.DaysPerWeek;
        }

        return AddDays(-delta);
    }

    /// <summary>Finds this date when its weekday matches, otherwise the next match.</summary><param name="dayOfWeek">Monday as 1 through Sunday as 7.</param><returns>This date or a date up to six days later.</returns>
    public LocalDate SameOrNext(int dayOfWeek)
    {
        TimeValidation.RequireIsoDayOfWeek(dayOfWeek, "dayOfWeek");

        int delta = dayOfWeek - GetDayOfWeek();
        if (delta < 0)
        {
            delta = delta + TimeConstants.DaysPerWeek;
        }

        return AddDays(delta);
    }

    /// <summary>Finds this date when its weekday matches, otherwise the previous match.</summary><param name="dayOfWeek">Monday as 1 through Sunday as 7.</param><returns>This date or a date up to six days earlier.</returns>
    public LocalDate SameOrPrevious(int dayOfWeek)
    {
        TimeValidation.RequireIsoDayOfWeek(dayOfWeek, "dayOfWeek");

        int delta = GetDayOfWeek() - dayOfWeek;
        if (delta < 0)
        {
            delta = delta + TimeConstants.DaysPerWeek;
        }

        return AddDays(-delta);
    }

    static int ConstrainDayToMonth(int year, int month, int day)
    {
        int daysInMonth = CalendarMath.GetDaysInMonth(year, month);
        if (day > daysInMonth)
        {
            return daysInMonth;
        }

        return day;
    }

    static void RequireDateOnlyPeriod(Period period)
    {
        if (period.GetHours() != 0L
            || period.GetMinutes() != 0L
            || period.GetSeconds() != 0L
            || period.GetMilliseconds() != 0L
            || period.GetMicroseconds() != 0L
            || period.GetTicks() != 0L)
        {
            throw new ArgumentException("LocalDate period arithmetic does not support time fields.", "period");
        }
    }

    static int ParseFixedDigits(string text, int start, int length)
    {
        int index = start;
        int end = start + length;
        int value = 0;

        while (index < end)
        {
            if (text[index] < '0' || text[index] > '9')
            {
                throw new FormatException("Local date components must contain only digits.");
            }

            value = value * 10 + (text[index] - '0');
            index = index + 1;
        }

        return value;
    }
}
