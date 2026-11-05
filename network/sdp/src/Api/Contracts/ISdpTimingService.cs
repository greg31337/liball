using System;

namespace sdp;

/// <summary>Converts SDP NTP time values to and from exactly representable .NET instants.</summary>
public interface ISdpTimingService
{
    /// <summary>Converts unsigned decimal NTP seconds to a UTC .NET instant.</summary>
    /// <param name="ntpValue">The lossless unsigned decimal count of seconds since the NTP epoch.</param>
    /// <returns>A result that reports invalid, overflowing, or unrepresentable values without throwing.</returns>
    SdpNtpConversionResult ConvertNtpToDateTimeOffset(string ntpValue);

    /// <summary>Converts a .NET instant to unsigned decimal whole NTP seconds.</summary>
    /// <param name="dateTimeValue">The instant to convert. Its offset is normalized to UTC.</param>
    /// <returns>A result that reports instants before the NTP epoch or with fractional seconds without throwing.</returns>
    SdpNtpConversionResult ConvertDateTimeOffsetToNtp(DateTimeOffset dateTimeValue);

    /// <summary>Normalizes an SDP typed-time value to signed seconds.</summary>
    /// <param name="value">The typed-time value to normalize.</param>
    /// <returns>A result that reports invalid units and arithmetic overflow without throwing.</returns>
    SdpTypedTimeNormalizationResult NormalizeTypedTime(SdpTypedTime value);

    /// <summary>Calculates active timing intervals intersecting a bounded UTC window.</summary>
    /// <param name="description">The session description whose timing periods are evaluated.</param>
    /// <param name="windowStart">The inclusive start of the requested window.</param>
    /// <param name="windowStop">The exclusive end of the requested window.</param>
    /// <param name="resultLimit">The positive maximum number of occurrence intervals to retain.</param>
    /// <returns>A chronological bounded result that reports malformed or unrepresentable timing data.</returns>
    SdpTimingOccurrenceResult CalculateOccurrences(SdpSessionDescription description, DateTimeOffset windowStart, DateTimeOffset windowStop, int resultLimit);
}
