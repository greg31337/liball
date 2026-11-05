using System;
using System.Globalization;

namespace time;

/// <summary>Represents a fixed displacement from UTC with one-second precision.</summary>
/// <remarks>Offsets are limited to -18:00 through +18:00. An offset is not a time zone and has no daylight-saving rules.</remarks>
public class Offset : IOffset
{
    int totalSeconds;

    internal Offset(int totalSeconds)
    {
        TimeValidation.RequireOffsetSeconds(totalSeconds, "totalSeconds");

        this.totalSeconds = totalSeconds;
    }

    /// <summary>Gets the zero UTC offset.</summary>
    /// <returns>An offset of zero seconds.</returns>
    internal static Offset Zero()
    {
        return new Offset(0);
    }

    /// <summary>Creates an offset from total signed seconds.</summary>
    /// <param name="seconds">The total offset from UTC.</param>
    /// <returns>The validated offset.</returns>
    internal static Offset FromSeconds(int seconds)
    {
        return new Offset(seconds);
    }

    /// <summary>Creates an offset from total signed minutes.</summary>
    /// <param name="minutes">The total whole minutes from UTC.</param>
    /// <returns>The equivalent offset.</returns>
    internal static Offset FromMinutes(int minutes)
    {
        return new Offset(minutes * TimeConstants.SecondsPerMinute);
    }

    /// <summary>Creates an offset from total signed hours.</summary>
    /// <param name="hours">The total whole hours from UTC.</param>
    /// <returns>The equivalent offset.</returns>
    internal static Offset FromHours(int hours)
    {
        return new Offset(hours * TimeConstants.MinutesPerHour * TimeConstants.SecondsPerMinute);
    }

    /// <summary>Creates an offset from signed hour, minute, and second components.</summary>
    /// <remarks>Zero components are neutral. All nonzero components must have the same sign.</remarks>
    /// <param name="hours">Signed whole hours from -18 through 18.</param>
    /// <param name="minutes">Signed following minutes from -59 through 59.</param>
    /// <param name="seconds">Signed following seconds from -59 through 59.</param>
    /// <returns>The validated combined offset.</returns>
    /// <exception cref="ArgumentException">Nonzero components have different signs.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A component or the combined offset is outside the supported range.</exception>
    internal static Offset FromComponents(int hours, int minutes, int seconds)
    {
        RequireComponentRange(hours, -18, 18, "hours");
        RequireComponentRange(minutes, -TimeConstants.MaxMinute, TimeConstants.MaxMinute, "minutes");
        RequireComponentRange(seconds, -TimeConstants.MaxSecond, TimeConstants.MaxSecond, "seconds");

        int sign = GetComponentSign(hours);
        sign = RequireMatchingSign(sign, minutes, "minutes");
        RequireMatchingSign(sign, seconds, "seconds");

        int total = hours * TimeConstants.MinutesPerHour * TimeConstants.SecondsPerMinute
            + minutes * TimeConstants.SecondsPerMinute
            + seconds;
        if (total < TimeConstants.MinOffsetSeconds || total > TimeConstants.MaxOffsetSeconds)
        {
            throw new ArgumentOutOfRangeException("hours", "The combined offset must be in the range -18:00 through +18:00.");
        }

        return new Offset(total);
    }

    /// <summary>Creates an offset from a built-in time span.</summary>
    /// <param name="timeSpan">A whole-second value in the supported offset range.</param>
    /// <returns>The equivalent offset.</returns>
    /// <exception cref="ArgumentException">The value contains fractional seconds.</exception>
    internal static Offset FromTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.Ticks % TimeConstants.TicksPerSecond != 0L)
        {
            throw new ArgumentException("Offset TimeSpan values must be whole seconds.", "timeSpan");
        }

        long seconds = timeSpan.Ticks / TimeConstants.TicksPerSecond;
        if (seconds < int.MinValue || seconds > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException("timeSpan", "Offset TimeSpan seconds are outside the supported integer range.");
        }

        return new Offset((int)seconds);
    }

    /// <summary>Parses <c>Z</c> or a signed ISO offset.</summary>
    /// <param name="text">Text in <c>Z</c>, <c>+HH:MM</c>, <c>-HH:MM</c>, or second-precision form.</param>
    /// <returns>The parsed offset.</returns>
    /// <exception cref="FormatException">The text is not a supported offset representation.</exception>
    internal static Offset Parse(string text)
    {
        TimeValidation.RequireNotNull(text, "text");

        if (text == "Z")
        {
            return Zero();
        }

        if (text.Length != 6 && text.Length != 9)
        {
            throw new FormatException("Offset text must use Z, HH:mm, or HH:mm:ss with a sign.");
        }

        if (text[0] != '+' && text[0] != '-')
        {
            throw new FormatException("Offset text must start with a sign.");
        }

        if (text[3] != ':' || (text.Length == 9 && text[6] != ':'))
        {
            throw new FormatException("Offset text must use colon separators.");
        }

        int hours = ParseFixedDigits(text, 1, 2);
        int minutes = ParseFixedDigits(text, 4, 2);
        int seconds = 0;
        if (text.Length == 9)
        {
            seconds = ParseFixedDigits(text, 7, 2);
        }

        if (hours > TimeConstants.MaxHour)
        {
            throw new FormatException("Offset hour is outside the supported range.");
        }

        if (minutes > TimeConstants.MaxMinute)
        {
            throw new FormatException("Offset minute is outside the supported range.");
        }

        if (seconds > TimeConstants.MaxSecond)
        {
            throw new FormatException("Offset second is outside the supported range.");
        }

        int total = hours * TimeConstants.MinutesPerHour * TimeConstants.SecondsPerMinute
            + minutes * TimeConstants.SecondsPerMinute
            + seconds;
        if (total > TimeConstants.MaxOffsetSeconds)
        {
            throw new FormatException("Offset is outside the supported range.");
        }

        if (text[0] == '-')
        {
            total = -total;
        }

        return FromSeconds(total);
    }

    /// <summary>Gets the sign of the total offset.</summary>
    /// <returns>-1 for negative, zero for UTC, or 1 for positive.</returns>
    public int GetSign()
    {
        if (totalSeconds < 0)
        {
            return -1;
        }

        if (totalSeconds > 0)
        {
            return 1;
        }

        return 0;
    }

    /// <summary>Gets the unsigned hour component.</summary>
    /// <returns>The absolute whole-hour component.</returns>
    public int GetHours()
    {
        int absoluteSeconds = GetAbsoluteSeconds();
        return absoluteSeconds / (TimeConstants.MinutesPerHour * TimeConstants.SecondsPerMinute);
    }

    /// <summary>Gets the unsigned minute component after whole hours.</summary>
    /// <returns>A value from 0 through 59.</returns>
    public int GetMinutes()
    {
        int absoluteSeconds = GetAbsoluteSeconds();
        int secondsAfterHours = absoluteSeconds % (TimeConstants.MinutesPerHour * TimeConstants.SecondsPerMinute);
        return secondsAfterHours / TimeConstants.SecondsPerMinute;
    }

    /// <summary>Gets the unsigned second component after whole minutes.</summary>
    /// <returns>A value from 0 through 59.</returns>
    public int GetSeconds()
    {
        int absoluteSeconds = GetAbsoluteSeconds();
        return absoluteSeconds % TimeConstants.SecondsPerMinute;
    }

    /// <summary>Gets the complete signed offset in seconds.</summary>
    /// <returns>Total seconds east of UTC; negative values are west of UTC.</returns>
    public int GetTotalSeconds()
    {
        return totalSeconds;
    }

    /// <summary>Gets complete signed minutes using integer truncation toward zero.</summary>
    /// <returns>The total whole minutes from UTC.</returns>
    public int GetTotalMinutes()
    {
        return totalSeconds / TimeConstants.SecondsPerMinute;
    }

    /// <summary>Converts this offset to a built-in time span.</summary>
    /// <returns>A time span containing the same signed seconds.</returns>
    public TimeSpan ToTimeSpan()
    {
        return new TimeSpan((long)totalSeconds * TimeConstants.TicksPerSecond);
    }

    /// <summary>Formats this offset in invariant ISO form.</summary>
    /// <returns><c>Z</c> for zero, otherwise a signed hour-minute string with seconds when needed.</returns>
    public string ToIsoString()
    {
        if (totalSeconds == 0)
        {
            return "Z";
        }

        string sign = "+";
        if (totalSeconds < 0)
        {
            sign = "-";
        }

        string text = sign
            + GetHours().ToString("D2", CultureInfo.InvariantCulture)
            + ":"
            + GetMinutes().ToString("D2", CultureInfo.InvariantCulture);

        if (GetSeconds() != 0)
        {
            text = text
                + ":"
                + GetSeconds().ToString("D2", CultureInfo.InvariantCulture);
        }

        return text;
    }

    /// <summary>Orders offsets by total signed seconds.</summary>
    /// <param name="other">The offset to compare.</param>
    /// <returns>A negative value, zero, or a positive value when this offset is smaller than, equal to, or greater than the other.</returns>
    public int CompareTo(Offset other)
    {
        TimeValidation.RequireNotNull(other, "other");

        return TimeComparison.CompareInt(totalSeconds, other.GetTotalSeconds());
    }

    /// <summary>Tests equality by total signed seconds.</summary>
    /// <param name="other">The offset to compare, or null.</param>
    /// <returns><c>true</c> when both offsets contain the same seconds.</returns>
    public bool EqualsValue(Offset? other)
    {
        if (other == null)
        {
            return false;
        }

        return totalSeconds == other.GetTotalSeconds();
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
                throw new FormatException("Offset components must contain only digits.");
            }

            value = value * 10 + (text[index] - '0');
            index = index + 1;
        }

        return value;
    }

    static int GetComponentSign(int value)
    {
        if (value < 0)
        {
            return -1;
        }

        if (value > 0)
        {
            return 1;
        }

        return 0;
    }

    static int RequireMatchingSign(int sign, int value, string parameterName)
    {
        int valueSign = GetComponentSign(value);
        if (valueSign == 0)
        {
            return sign;
        }

        if (sign != 0 && sign != valueSign)
        {
            throw new ArgumentException("All nonzero offset components must have the same sign.", parameterName);
        }

        return valueSign;
    }

    static void RequireComponentRange(int value, int minimum, int maximum, string parameterName)
    {
        if (value < minimum || value > maximum)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Offset component is outside the supported range.");
        }
    }

    int GetAbsoluteSeconds()
    {
        if (totalSeconds < 0)
        {
            return -totalSeconds;
        }

        return totalSeconds;
    }
}
