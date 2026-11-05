using System;

namespace uuid;

internal class KsuidTimestamp
{
    internal static bool IsValidAdjustedSeconds(long adjustedSeconds)
    {
        return adjustedSeconds >= 0 && adjustedSeconds <= KsuidLimits.MaxAdjustedSeconds;
    }

    internal static bool IsValidUnixSeconds(long unixSeconds)
    {
        return unixSeconds >= KsuidLimits.EpochUnixSeconds && unixSeconds <= KsuidLimits.MaxUnixSeconds;
    }

    internal static IdentifierValidationResult ValidateAdjustedSeconds(long adjustedSeconds)
    {
        if (!IsValidAdjustedSeconds(adjustedSeconds))
        {
            return IdentifierValidationResult.Failure(IdentifierErrorCode.TimestampOverflow, "KSUID adjusted seconds is outside 32-bit range");
        }

        return IdentifierValidationResult.Success(IdentifierKind.Ksuid, adjustedSeconds.ToString());
    }

    internal static IdentifierValidationResult ValidateUnixSeconds(long unixSeconds)
    {
        if (!IsValidUnixSeconds(unixSeconds))
        {
            return IdentifierValidationResult.Failure(IdentifierErrorCode.TimestampOverflow, "KSUID unix seconds is outside adjusted epoch range");
        }

        return IdentifierValidationResult.Success(IdentifierKind.Ksuid, unixSeconds.ToString());
    }

    internal static long ToAdjustedSeconds(long unixSeconds)
    {
        ThrowIfInvalidUnixSeconds(unixSeconds);
        return unixSeconds - KsuidLimits.EpochUnixSeconds;
    }

    internal static long ToUnixSeconds(long adjustedSeconds)
    {
        ThrowIfInvalidAdjustedSeconds(adjustedSeconds);
        return KsuidLimits.EpochUnixSeconds + adjustedSeconds;
    }

    internal static void ThrowIfInvalidAdjustedSeconds(long adjustedSeconds)
    {
        if (!IsValidAdjustedSeconds(adjustedSeconds))
        {
            throw new ArgumentOutOfRangeException("adjustedSeconds");
        }
    }

    internal static void ThrowIfInvalidUnixSeconds(long unixSeconds)
    {
        if (!IsValidUnixSeconds(unixSeconds))
        {
            throw new ArgumentOutOfRangeException("unixSeconds");
        }
    }
}
