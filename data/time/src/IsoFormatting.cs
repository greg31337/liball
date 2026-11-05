using System;
using System.Globalization;

namespace time;

internal static class IsoFormatting
{
    internal static string FormatFixedDigits(int value, int digits)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException("value", "Fixed digit value must not be negative.");
        }

        if (digits <= 0)
        {
            throw new ArgumentOutOfRangeException("digits", "Fixed digit count must be positive.");
        }

        return value.ToString("D" + digits.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
    }

    internal static string FormatDate(int year, int month, int day)
    {
        TimeValidation.RequireDay(year, month, day, "day");

        return FormatFixedDigits(year, 4) + "-" + FormatFixedDigits(month, 2) + "-" + FormatFixedDigits(day, 2);
    }

    internal static string FormatTime(int hour, int minute, int second, long fractionalTicks)
    {
        TimeValidation.RequireTime(hour, minute, second, 0, 0, 0);

        if (fractionalTicks < 0L || fractionalTicks >= TimeConstants.TicksPerSecond)
        {
            throw new ArgumentOutOfRangeException("fractionalTicks", "Fractional second ticks must be within one second.");
        }

        return FormatFixedDigits(hour, 2)
            + ":"
            + FormatFixedDigits(minute, 2)
            + ":"
            + FormatFixedDigits(second, 2)
            + FormatFractionalSecond(fractionalTicks);
    }

    internal static string FormatFractionalSecond(long fractionalTicks)
    {
        if (fractionalTicks < 0L || fractionalTicks >= TimeConstants.TicksPerSecond)
        {
            throw new ArgumentOutOfRangeException("fractionalTicks", "Fractional second ticks must be within one second.");
        }

        if (fractionalTicks == 0L)
        {
            return "";
        }

        return "." + fractionalTicks.ToString("D7", CultureInfo.InvariantCulture).TrimEnd('0');
    }

    internal static string FormatOffsetSeconds(int totalSeconds)
    {
        TimeValidation.RequireOffsetSeconds(totalSeconds, "totalSeconds");

        if (totalSeconds == 0)
        {
            return "Z";
        }

        string sign = "+";
        int absoluteSeconds = totalSeconds;
        if (totalSeconds < 0)
        {
            sign = "-";
            absoluteSeconds = -totalSeconds;
        }

        int hours = absoluteSeconds / (TimeConstants.MinutesPerHour * TimeConstants.SecondsPerMinute);
        int secondsAfterHours = absoluteSeconds % (TimeConstants.MinutesPerHour * TimeConstants.SecondsPerMinute);
        int minutes = secondsAfterHours / TimeConstants.SecondsPerMinute;
        int seconds = secondsAfterHours % TimeConstants.SecondsPerMinute;
        string result = sign + FormatFixedDigits(hours, 2) + ":" + FormatFixedDigits(minutes, 2);

        if (seconds != 0)
        {
            result = result + ":" + FormatFixedDigits(seconds, 2);
        }

        return result;
    }
}
