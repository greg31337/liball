using System;

namespace rtp;

internal static class RtpClockMath
{
    internal const ulong NtpHalfRange = 9223372036854775808UL;

    internal static long GetCompactNtpDifference(
        uint laterTimestamp,
        uint earlierTimestamp)
    {
        long difference = (long)laterTimestamp - (long)earlierTimestamp;
        if (difference > 2147483647L)
        {
            difference = difference - 4294967296L;
        }
        else if (difference < -2147483648L)
        {
            difference = difference + 4294967296L;
        }

        return difference;
    }

    internal static long GetCompactNtpDifferenceTicks(
        uint laterTimestamp,
        uint earlierTimestamp)
    {
        long difference = GetCompactNtpDifference(laterTimestamp, earlierTimestamp);
        return (difference * RtpNtpTime.TimeSpanTicksPerSecond) /
            RtcpProtocolConstants.CompactNtpUnitsPerSecond;
    }

    internal static long GetNtpDifferenceTicks(
        ulong laterTimestamp,
        ulong earlierTimestamp)
    {
        ulong magnitude;
        bool negative;
        if (laterTimestamp >= earlierTimestamp)
        {
            magnitude = laterTimestamp - earlierTimestamp;
            negative = magnitude >= NtpHalfRange;
            if (negative)
            {
                magnitude = ulong.MaxValue - magnitude + 1UL;
            }
        }
        else
        {
            magnitude = earlierTimestamp - laterTimestamp;
            negative = magnitude <= NtpHalfRange;
            if (!negative)
            {
                magnitude = ulong.MaxValue - magnitude + 1UL;
            }
        }

        long ticks = ConvertNtpMagnitudeToTicks(magnitude);
        if (negative)
        {
            return -ticks;
        }

        return ticks;
    }

    internal static bool IsUtcClockCorrelationSafe(long utcTicks)
    {
        if (utcTicks < RtpNtpTime.NtpEpochTicks ||
            utcTicks > RtpNtpTime.MaximumUtcTicks)
        {
            throw new RtpConfigurationException(
                "Clock-correlation UTC ticks must be from the NTP epoch through year 9999.");
        }

        DateTime utcTime = new DateTime(utcTicks, DateTimeKind.Utc);
        bool isLastSecondOfMonth =
            utcTime.Day == DateTime.DaysInMonth(utcTime.Year, utcTime.Month) &&
            utcTime.Hour == 23 &&
            utcTime.Minute == 59 &&
            utcTime.Second == 59;
        bool isFirstSecondOfMonth =
            utcTime.Day == 1 &&
            utcTime.Hour == 0 &&
            utcTime.Minute == 0 &&
            utcTime.Second == 0;
        return !isLastSecondOfMonth && !isFirstSecondOfMonth;
    }

    internal static bool IsNtpClockCorrelationSafe(
        ulong ntpTimestamp,
        long eraReferenceUtcTicks)
    {
        long utcTicks = RtpNtpTime.ToUtcTicks(ntpTimestamp, eraReferenceUtcTicks);
        return IsUtcClockCorrelationSafe(utcTicks);
    }

    internal static long ConvertNtpMagnitudeToTicks(ulong magnitude)
    {
        ulong wholeSeconds = magnitude >> 32;
        ulong fraction = magnitude % RtpNtpTime.NtpFractionScale;
        ulong ticks = wholeSeconds * (ulong)RtpNtpTime.TimeSpanTicksPerSecond +
            (fraction * (ulong)RtpNtpTime.TimeSpanTicksPerSecond) /
            RtpNtpTime.NtpFractionScale;
        return (long)ticks;
    }
}
