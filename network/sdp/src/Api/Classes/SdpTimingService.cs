using System;

namespace sdp;

/// <summary>Converts SDP NTP time values to and from exactly representable .NET instants.</summary>
public class SdpTimingService : ISdpTimingService
{
    /// <summary>Initializes an SDP timing service.</summary>
    public SdpTimingService()
    {
    }

    /// <summary>Converts unsigned decimal NTP seconds to a UTC .NET instant.</summary>
    /// <param name="ntpValue">The lossless unsigned decimal count of seconds since the NTP epoch.</param>
    /// <returns>A result that reports invalid, overflowing, or unrepresentable values without throwing.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the NTP value is null.</exception>
    public SdpNtpConversionResult ConvertNtpToDateTimeOffset(string ntpValue)
    {
        if (ntpValue == null)
        {
            throw new ArgumentNullException("ntpValue");
        }
        return SdpTimingServiceCore.ConvertNtpToDateTimeOffset(ntpValue);
    }

    /// <summary>Converts a .NET instant to unsigned decimal whole NTP seconds.</summary>
    /// <param name="dateTimeValue">The instant to convert. Its offset is normalized to UTC.</param>
    /// <returns>A result that reports instants before the NTP epoch or with fractional seconds without throwing.</returns>
    public SdpNtpConversionResult ConvertDateTimeOffsetToNtp(DateTimeOffset dateTimeValue)
    {
        return SdpTimingServiceCore.ConvertDateTimeOffsetToNtp(dateTimeValue);
    }

    /// <summary>Normalizes an SDP typed-time value to signed seconds.</summary>
    /// <param name="value">The typed-time value to normalize.</param>
    /// <returns>A result that reports invalid units and arithmetic overflow without throwing.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the typed-time value is null.</exception>
    public SdpTypedTimeNormalizationResult NormalizeTypedTime(SdpTypedTime value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }
        return SdpTimingServiceCore.NormalizeTypedTime(value);
    }

    /// <summary>Calculates active timing intervals intersecting a bounded UTC window.</summary>
    /// <param name="description">The session description whose timing periods are evaluated.</param>
    /// <param name="windowStart">The inclusive start of the requested window.</param>
    /// <param name="windowStop">The exclusive end of the requested window.</param>
    /// <param name="resultLimit">The positive maximum number of occurrence intervals to retain.</param>
    /// <returns>A chronological bounded result that reports malformed or unrepresentable timing data.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the session description is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the window stop precedes the window start.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the result limit is not positive.</exception>
    public SdpTimingOccurrenceResult CalculateOccurrences(SdpSessionDescription description, DateTimeOffset windowStart, DateTimeOffset windowStop, int resultLimit)
    {
        if (description == null)
        {
            throw new ArgumentNullException("description");
        }
        if (windowStop < windowStart)
        {
            throw new ArgumentException("The occurrence window stop must not precede its start.", "windowStop");
        }
        if (resultLimit <= 0)
        {
            throw new ArgumentOutOfRangeException("resultLimit");
        }
        return SdpTimingServiceCore.CalculateOccurrences(description, windowStart, windowStop, resultLimit);
    }
}
