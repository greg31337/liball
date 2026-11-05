using System;

namespace uuid;

internal class UnixTimeTools
{
    internal const long MinUnixSeconds = -62135596800;
    internal const long MaxUnixSeconds = 253402300799;
    internal const long MinUnixMilliseconds = -62135596800000;
    internal const long MaxUnixMilliseconds = 253402300799999;

    internal static bool IsValidUnixSeconds(long unixSeconds)
    {
        return unixSeconds >= MinUnixSeconds && unixSeconds <= MaxUnixSeconds;
    }

    internal static bool IsValidUnixMilliseconds(long unixMilliseconds)
    {
        return unixMilliseconds >= MinUnixMilliseconds && unixMilliseconds <= MaxUnixMilliseconds;
    }

    internal static IdentifierValidationResult ValidateUnixSeconds(long unixSeconds)
    {
        if (!IsValidUnixSeconds(unixSeconds))
        {
            return IdentifierValidationResult.Failure(IdentifierErrorCode.TimestampOverflow, "unix seconds is outside DateTimeOffset range");
        }

        return IdentifierValidationResult.Success(IdentifierKind.Unknown, unixSeconds.ToString());
    }

    internal static IdentifierValidationResult ValidateUnixMilliseconds(long unixMilliseconds)
    {
        if (!IsValidUnixMilliseconds(unixMilliseconds))
        {
            return IdentifierValidationResult.Failure(IdentifierErrorCode.TimestampOverflow, "unix milliseconds is outside DateTimeOffset range");
        }

        return IdentifierValidationResult.Success(IdentifierKind.Unknown, unixMilliseconds.ToString());
    }

    internal static DateTimeOffset FromUnixSeconds(long unixSeconds)
    {
        ThrowIfInvalidUnixSeconds(unixSeconds);
        return DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
    }

    internal static DateTimeOffset FromUnixMilliseconds(long unixMilliseconds)
    {
        ThrowIfInvalidUnixMilliseconds(unixMilliseconds);
        return DateTimeOffset.FromUnixTimeMilliseconds(unixMilliseconds);
    }

    internal static long ToUnixSeconds(DateTimeOffset time)
    {
        return time.ToUnixTimeSeconds();
    }

    internal static long ToUnixMilliseconds(DateTimeOffset time)
    {
        return time.ToUnixTimeMilliseconds();
    }

    internal static long SecondsToMilliseconds(long unixSeconds)
    {
        ThrowIfInvalidUnixSeconds(unixSeconds);
        return unixSeconds * 1000;
    }

    internal static long MillisecondsToSeconds(long unixMilliseconds)
    {
        ThrowIfInvalidUnixMilliseconds(unixMilliseconds);
        if (unixMilliseconds >= 0)
        {
            return unixMilliseconds / 1000;
        }

        return (unixMilliseconds - 999) / 1000;
    }

    internal static void ThrowIfInvalidUnixSeconds(long unixSeconds)
    {
        if (!IsValidUnixSeconds(unixSeconds))
        {
            throw new ArgumentOutOfRangeException("unixSeconds");
        }
    }

    internal static void ThrowIfInvalidUnixMilliseconds(long unixMilliseconds)
    {
        if (!IsValidUnixMilliseconds(unixMilliseconds))
        {
            throw new ArgumentOutOfRangeException("unixMilliseconds");
        }
    }
}
