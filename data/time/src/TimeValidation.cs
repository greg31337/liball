using System;

namespace time;

internal static class TimeValidation
{
    internal static void RequireNotNull(object? value, string parameterName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName);
        }
    }

    internal static void RequireYear(int year, string parameterName)
    {
        if (year < TimeConstants.MinYear || year > TimeConstants.MaxYear)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Year must be in range 1 through 9999.");
        }
    }

    internal static void RequireMonth(int month, string parameterName)
    {
        if (month < TimeConstants.MinMonth || month > TimeConstants.MaxMonth)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Month must be in range 1 through 12.");
        }
    }

    internal static void RequireDay(int year, int month, int day, string parameterName)
    {
        RequireYear(year, "year");
        RequireMonth(month, "month");

        int daysInMonth = CalendarMath.GetDaysInMonth(year, month);
        if (day < TimeConstants.MinDay || day > daysInMonth)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Day must be valid for the supplied year and month.");
        }
    }

    internal static void RequireDayNumber(int dayNumber, string parameterName)
    {
        if (dayNumber < TimeConstants.MinDayNumber || dayNumber > TimeConstants.MaxDayNumber)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Day number must be in the supported local date range.");
        }
    }

    internal static void RequireIsoDayOfWeek(int dayOfWeek, string parameterName)
    {
        if (dayOfWeek < TimeConstants.IsoMonday || dayOfWeek > TimeConstants.IsoSunday)
        {
            throw new ArgumentOutOfRangeException(parameterName, "ISO day of week must be in range 1 through 7.");
        }
    }

    internal static void RequireTime(int hour, int minute, int second, int millisecond, int microsecond, int tickRemainder)
    {
        RequireHour(hour, "hour");
        RequireMinute(minute, "minute");
        RequireSecond(second, "second");
        RequireMillisecond(millisecond, "millisecond");
        RequireMicrosecond(microsecond, "microsecond");
        RequireTickRemainder(tickRemainder, "tickRemainder");
    }

    internal static void RequireTicksSinceMidnight(long ticksSinceMidnight, string parameterName)
    {
        if (ticksSinceMidnight < TimeConstants.MinTicksSinceMidnight || ticksSinceMidnight > TimeConstants.MaxTicksSinceMidnight)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Ticks since midnight must be in range 0 through one tick before midnight.");
        }
    }

    internal static void RequireOffsetSeconds(int offsetSeconds, string parameterName)
    {
        if (offsetSeconds < TimeConstants.MinOffsetSeconds || offsetSeconds > TimeConstants.MaxOffsetSeconds)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Offset seconds must be in range -18:00 through +18:00.");
        }
    }

    internal static void RequireHour(int hour, string parameterName)
    {
        if (hour < TimeConstants.MinHour || hour > TimeConstants.MaxHour)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Hour must be in range 0 through 23.");
        }
    }

    internal static void RequireMinute(int minute, string parameterName)
    {
        if (minute < TimeConstants.MinMinute || minute > TimeConstants.MaxMinute)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Minute must be in range 0 through 59.");
        }
    }

    internal static void RequireSecond(int second, string parameterName)
    {
        if (second < TimeConstants.MinSecond || second > TimeConstants.MaxSecond)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Second must be in range 0 through 59.");
        }
    }

    internal static void RequireMillisecond(int millisecond, string parameterName)
    {
        if (millisecond < TimeConstants.MinMillisecond || millisecond > TimeConstants.MaxMillisecond)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Millisecond must be in range 0 through 999.");
        }
    }

    internal static void RequireMicrosecond(int microsecond, string parameterName)
    {
        if (microsecond < TimeConstants.MinMicrosecond || microsecond > TimeConstants.MaxMicrosecond)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Microsecond must be in range 0 through 999.");
        }
    }

    internal static void RequireTickRemainder(int tickRemainder, string parameterName)
    {
        if (tickRemainder < TimeConstants.MinTickRemainder || tickRemainder > TimeConstants.MaxTickRemainder)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Tick remainder must be in range 0 through 9.");
        }
    }
}
