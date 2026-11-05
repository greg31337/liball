namespace rtp;

internal static class RtpNtpTime
{
    internal const long NtpEpochTicks = 599266080000000000L;
    internal const long TimeSpanTicksPerSecond = 10000000L;
    internal const long MaximumUtcTicks = 3155378975999999999L;
    internal const ulong NtpFractionScale = 4294967296UL;
    internal const long NtpEraSeconds = 4294967296L;
    internal const long NtpHalfEraSeconds = 2147483648L;
    internal const ulong CompactNtpModulus = 4294967296UL;

    internal static ulong FromUtcTicks(long utcTicks)
    {
        if (utcTicks < NtpEpochTicks || utcTicks > MaximumUtcTicks)
        {
            throw new RtpConfigurationException(
                "UTC ticks must identify a time from the NTP epoch through year 9999.");
        }

        long elapsedTicks = utcTicks - NtpEpochTicks;
        long wholeSeconds = elapsedTicks / TimeSpanTicksPerSecond;
        long remainingTicks = elapsedTicks % TimeSpanTicksPerSecond;
        ulong eraSeconds = (ulong)(wholeSeconds % NtpEraSeconds);
        ulong fraction =
            ((ulong)remainingTicks * NtpFractionScale) /
            (ulong)TimeSpanTicksPerSecond;
        return (eraSeconds << 32) | fraction;
    }

    internal static long ToUtcTicks(ulong ntpTimestamp, long eraReferenceUtcTicks)
    {
        if (eraReferenceUtcTicks < NtpEpochTicks ||
            eraReferenceUtcTicks > MaximumUtcTicks)
        {
            throw new RtpConfigurationException(
                "The NTP era reference must be from the NTP epoch through year 9999.");
        }

        long referenceSeconds =
            (eraReferenceUtcTicks - NtpEpochTicks) / TimeSpanTicksPerSecond;
        long referenceEra = referenceSeconds / NtpEraSeconds;
        long timestampSeconds = (long)(ntpTimestamp >> 32);
        long candidateSeconds = referenceEra * NtpEraSeconds + timestampSeconds;
        long candidateDifference = candidateSeconds - referenceSeconds;
        if (candidateDifference > NtpHalfEraSeconds && referenceEra > 0)
        {
            candidateSeconds = candidateSeconds - NtpEraSeconds;
        }
        else if (candidateDifference < -NtpHalfEraSeconds)
        {
            candidateSeconds = candidateSeconds + NtpEraSeconds;
        }

        ulong fraction = ntpTimestamp % NtpFractionScale;
        long fractionTicks = (long)((fraction *
            (ulong)TimeSpanTicksPerSecond) / NtpFractionScale);
        long maximumCandidateSeconds =
            (MaximumUtcTicks - NtpEpochTicks - fractionTicks) /
            TimeSpanTicksPerSecond;
        if (candidateSeconds < 0 || candidateSeconds > maximumCandidateSeconds)
        {
            throw new RtpConfigurationException(
                "The NTP timestamp cannot be represented in the selected UTC era.");
        }

        return NtpEpochTicks +
            candidateSeconds * TimeSpanTicksPerSecond + fractionTicks;
    }

    internal static uint ToCompactNtp(ulong ntpTimestamp)
    {
        return (uint)((ntpTimestamp >> 16) % CompactNtpModulus);
    }

    internal static ulong ExpandCompactNtp(
        uint compactTimestamp,
        ulong referenceNtpTimestamp)
    {
        uint referenceCompactTimestamp = ToCompactNtp(referenceNtpTimestamp);
        long compactDifference = RtpClockMath.GetCompactNtpDifference(
            compactTimestamp,
            referenceCompactTimestamp);
        ulong referenceAtCompactResolution =
            referenceNtpTimestamp - (referenceNtpTimestamp % 65536UL);
        ulong differenceMagnitude;
        if (compactDifference >= 0)
        {
            differenceMagnitude = (ulong)compactDifference * 65536UL;
            if (referenceAtCompactResolution <=
                ulong.MaxValue - differenceMagnitude)
            {
                return referenceAtCompactResolution + differenceMagnitude;
            }

            return differenceMagnitude -
                (ulong.MaxValue - referenceAtCompactResolution) - 1UL;
        }

        differenceMagnitude = (ulong)(-compactDifference) * 65536UL;
        if (referenceAtCompactResolution >= differenceMagnitude)
        {
            return referenceAtCompactResolution - differenceMagnitude;
        }

        return ulong.MaxValue -
            (differenceMagnitude - referenceAtCompactResolution) + 1UL;
    }

    internal static uint ElapsedTicksToCompactDelay(long elapsedTicks)
    {
        if (elapsedTicks < 0)
        {
            throw new RtpConfigurationException(
                "An NTP delay cannot be created from negative elapsed time.");
        }

        long wholeSeconds = elapsedTicks / TimeSpanTicksPerSecond;
        if (wholeSeconds >= 65536L)
        {
            return uint.MaxValue;
        }

        long remainingTicks = elapsedTicks % TimeSpanTicksPerSecond;
        long delayUnits =
            wholeSeconds * RtcpProtocolConstants.CompactNtpUnitsPerSecond +
            (remainingTicks * RtcpProtocolConstants.CompactNtpUnitsPerSecond) /
            TimeSpanTicksPerSecond;
        return (uint)delayUnits;
    }

    internal static long CompactDelayToElapsedTicks(uint compactDelay)
    {
        long wholeSeconds = compactDelay /
            (uint)RtcpProtocolConstants.CompactNtpUnitsPerSecond;
        long fractionalUnits = compactDelay %
            (uint)RtcpProtocolConstants.CompactNtpUnitsPerSecond;
        return wholeSeconds * TimeSpanTicksPerSecond +
            (fractionalUnits * TimeSpanTicksPerSecond) /
            RtcpProtocolConstants.CompactNtpUnitsPerSecond;
    }
}
