namespace time;

internal static class TimeConstants
{
    internal const long TicksPerMicrosecond = 10L;
    internal const long TicksPerMillisecond = 10000L;
    internal const long TicksPerSecond = 10000000L;
    internal const long TicksPerMinute = 600000000L;
    internal const long TicksPerHour = 36000000000L;
    internal const long TicksPerDay = 864000000000L;
    internal const long TicksPerWeek = 6048000000000L;
    internal const long MinTicksSinceMidnight = 0L;
    internal const long MaxTicksSinceMidnight = 863999999999L;
    internal const long MinUtcTicks = 0L;
    internal const long MaxUtcTicks = 3155378975999999999L;
    internal const long UnixEpochUtcTicks = 621355968000000000L;

    internal const int MicrosecondsPerMillisecond = 1000;
    internal const int MillisecondsPerSecond = 1000;
    internal const int SecondsPerMinute = 60;
    internal const int MinutesPerHour = 60;
    internal const int HoursPerDay = 24;
    internal const int DaysPerWeek = 7;
    internal const int MonthsPerYear = 12;
    internal const int DaysPerCommonYear = 365;
    internal const int DaysPerLeapYear = 366;

    internal const int IsoMonday = 1;
    internal const int IsoTuesday = 2;
    internal const int IsoWednesday = 3;
    internal const int IsoThursday = 4;
    internal const int IsoFriday = 5;
    internal const int IsoSaturday = 6;
    internal const int IsoSunday = 7;

    internal const int MinYear = 1;
    internal const int MaxYear = 9999;
    internal const int MinMonth = 1;
    internal const int MaxMonth = 12;
    internal const int MinDay = 1;
    internal const int MaxDay = 31;
    internal const int MinDayNumber = 0;
    internal const int MaxDayNumber = 3652058;

    internal const int MinHour = 0;
    internal const int MaxHour = 23;
    internal const int MinMinute = 0;
    internal const int MaxMinute = 59;
    internal const int MinSecond = 0;
    internal const int MaxSecond = 59;
    internal const int MinMillisecond = 0;
    internal const int MaxMillisecond = 999;
    internal const int MinMicrosecond = 0;
    internal const int MaxMicrosecond = 999;
    internal const int MinTickRemainder = 0;
    internal const int MaxTickRemainder = 9;

    internal const int MinOffsetSeconds = -64800;
    internal const int MaxOffsetSeconds = 64800;
}
