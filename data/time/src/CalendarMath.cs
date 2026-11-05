using System;

namespace time;

internal static class CalendarMath
{
    internal static bool IsLeapYear(int year)
    {
        TimeValidation.RequireYear(year, "year");

        if (year % 4 != 0)
        {
            return false;
        }

        if (year % 100 != 0)
        {
            return true;
        }

        return year % 400 == 0;
    }

    internal static int GetDaysInMonth(int year, int month)
    {
        TimeValidation.RequireYear(year, "year");
        TimeValidation.RequireMonth(month, "month");

        switch (month)
        {
            case 1:
                return 31;
            case 2:
                if (IsLeapYear(year))
                {
                    return 29;
                }

                return 28;
            case 3:
                return 31;
            case 4:
                return 30;
            case 5:
                return 31;
            case 6:
                return 30;
            case 7:
                return 31;
            case 8:
                return 31;
            case 9:
                return 30;
            case 10:
                return 31;
            case 11:
                return 30;
            default:
                return 31;
        }
    }

    internal static int GetDaysInYear(int year)
    {
        if (IsLeapYear(year))
        {
            return TimeConstants.DaysPerLeapYear;
        }

        return TimeConstants.DaysPerCommonYear;
    }

    internal static int GetDayOfYear(int year, int month, int day)
    {
        TimeValidation.RequireDay(year, month, day, "day");

        int dayOfYear = day;
        int currentMonth = 1;
        while (currentMonth < month)
        {
            dayOfYear = dayOfYear + GetDaysInMonth(year, currentMonth);
            currentMonth = currentMonth + 1;
        }

        return dayOfYear;
    }

    internal static int GetStartDayNumberOfYear(int year)
    {
        TimeValidation.RequireYear(year, "year");

        int previousYears = year - 1;
        return previousYears * TimeConstants.DaysPerCommonYear
            + previousYears / 4
            - previousYears / 100
            + previousYears / 400;
    }

    internal static int GetDayNumber(int year, int month, int day)
    {
        TimeValidation.RequireDay(year, month, day, "day");

        return GetStartDayNumberOfYear(year) + GetDayOfYear(year, month, day) - 1;
    }

    internal static int GetDayOfWeek(int year, int month, int day)
    {
        int dayNumber = GetDayNumber(year, month, day);
        return dayNumber % TimeConstants.DaysPerWeek + TimeConstants.IsoMonday;
    }

    internal static int GetYearFromDayNumber(int dayNumber)
    {
        TimeValidation.RequireDayNumber(dayNumber, "dayNumber");

        int low = TimeConstants.MinYear;
        int high = TimeConstants.MaxYear;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            int start = GetStartDayNumberOfYear(middle);
            int nextStart = TimeConstants.MaxDayNumber + 1;
            if (middle < TimeConstants.MaxYear)
            {
                nextStart = GetStartDayNumberOfYear(middle + 1);
            }

            if (dayNumber < start)
            {
                high = middle - 1;
            }
            else if (dayNumber >= nextStart)
            {
                low = middle + 1;
            }
            else
            {
                return middle;
            }
        }

        throw new InvalidOperationException("Day number could not be resolved to a year.");
    }

    internal static int GetMonthFromDayNumber(int dayNumber)
    {
        int year = GetYearFromDayNumber(dayNumber);
        int dayOfYear = dayNumber - GetStartDayNumberOfYear(year) + 1;
        int month = TimeConstants.MinMonth;

        while (month < TimeConstants.MaxMonth)
        {
            int daysInMonth = GetDaysInMonth(year, month);
            if (dayOfYear <= daysInMonth)
            {
                return month;
            }

            dayOfYear = dayOfYear - daysInMonth;
            month = month + 1;
        }

        return TimeConstants.MaxMonth;
    }

    internal static int GetDayFromDayNumber(int dayNumber)
    {
        int year = GetYearFromDayNumber(dayNumber);
        int dayOfYear = dayNumber - GetStartDayNumberOfYear(year) + 1;
        int month = TimeConstants.MinMonth;

        while (month < TimeConstants.MaxMonth)
        {
            int daysInMonth = GetDaysInMonth(year, month);
            if (dayOfYear <= daysInMonth)
            {
                return dayOfYear;
            }

            dayOfYear = dayOfYear - daysInMonth;
            month = month + 1;
        }

        return dayOfYear;
    }
}
