using rtp;

namespace rtp.Tests.Timing;

public class RtpNtpTimeTests
{
    [Test]
    public void KnownUtcInstantsConvertToFullNtpFixedPointValues()
    {
        long ntpEpoch = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
        long unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
        long unixEpochAndHalf = unixEpoch + TimeSpan.TicksPerSecond / 2;

        ulong atNtpEpoch = RtpNtpTime.FromUtcTicks(ntpEpoch);
        ulong atUnixEpoch = RtpNtpTime.FromUtcTicks(unixEpoch);
        ulong atUnixEpochAndHalf = RtpNtpTime.FromUtcTicks(unixEpochAndHalf);

        Assert.Multiple(() =>
        {
            Assert.That(atNtpEpoch, Is.Zero);
            Assert.That(atUnixEpoch,
                Is.EqualTo(RtcpProtocolConstants.NtpToUnixEpochSeconds << 32));
            Assert.That(atUnixEpochAndHalf,
                Is.EqualTo((RtcpProtocolConstants.NtpToUnixEpochSeconds << 32) |
                2147483648UL));
        });
    }

    [Test]
    public void FullNtpConversionResolvesTheEraNearestToTheReference()
    {
        long beforeEraWrap =
            new DateTime(2036, 2, 7, 6, 28, 15, DateTimeKind.Utc).Ticks;
        long afterEraWrap = beforeEraWrap + TimeSpan.TicksPerSecond;
        long laterEraTime =
            new DateTime(2200, 6, 1, 12, 34, 56, 789, DateTimeKind.Utc).Ticks;

        ulong beforeTimestamp = RtpNtpTime.FromUtcTicks(beforeEraWrap);
        ulong afterTimestamp = RtpNtpTime.FromUtcTicks(afterEraWrap);
        ulong laterTimestamp = RtpNtpTime.FromUtcTicks(laterEraTime);
        long decodedBefore = RtpNtpTime.ToUtcTicks(beforeTimestamp, beforeEraWrap);
        long decodedAfter = RtpNtpTime.ToUtcTicks(afterTimestamp, afterEraWrap);
        long decodedLater = RtpNtpTime.ToUtcTicks(laterTimestamp, laterEraTime);

        Assert.Multiple(() =>
        {
            Assert.That(beforeTimestamp >> 32, Is.EqualTo(uint.MaxValue));
            Assert.That(afterTimestamp, Is.Zero);
            Assert.That(decodedBefore, Is.EqualTo(beforeEraWrap));
            Assert.That(decodedAfter, Is.EqualTo(afterEraWrap));
            Assert.That(decodedLater, Is.InRange(laterEraTime - 1L, laterEraTime));
        });
    }

    [Test]
    public void CompactNtpConversionUsesTheMiddleBitsAndExpandsAroundAReference()
    {
        ulong reference = 0x123456789ABCDEF0UL;

        uint compact = RtpNtpTime.ToCompactNtp(reference);
        ulong expanded = RtpNtpTime.ExpandCompactNtp(compact, reference);
        ulong expandedAcrossWrap = RtpNtpTime.ExpandCompactNtp(
            0U,
            0xFFFFFFFFFFFF0000UL);
        ulong expandedBackwardAcrossWrap = RtpNtpTime.ExpandCompactNtp(
            uint.MaxValue,
            0UL);

        Assert.Multiple(() =>
        {
            Assert.That(compact, Is.EqualTo(0x56789ABCU));
            Assert.That(expanded, Is.EqualTo(0x123456789ABC0000UL));
            Assert.That(expandedAcrossWrap, Is.Zero);
            Assert.That(expandedBackwardAcrossWrap,
                Is.EqualTo(0xFFFFFFFFFFFF0000UL));
        });
    }

    [Test]
    public void DelayConversionUsesSixteenPointSixteenUnitsAndSaturates()
    {
        long fiveAndQuarterSeconds = 5L * TimeSpan.TicksPerSecond +
            TimeSpan.TicksPerSecond / 4;

        uint compactDelay = RtpNtpTime.ElapsedTicksToCompactDelay(
            fiveAndQuarterSeconds);
        long restoredTicks = RtpNtpTime.CompactDelayToElapsedTicks(compactDelay);
        uint saturated = RtpNtpTime.ElapsedTicksToCompactDelay(
            65536L * TimeSpan.TicksPerSecond);

        Assert.Multiple(() =>
        {
            Assert.That(compactDelay, Is.EqualTo(0x00054000U));
            Assert.That(restoredTicks, Is.EqualTo(fiveAndQuarterSeconds));
            Assert.That(saturated, Is.EqualTo(uint.MaxValue));
            Assert.Throws<RtpConfigurationException>(() =>
                RtpNtpTime.ElapsedTicksToCompactDelay(-1L));
        });
    }

    [Test]
    public void CompactAndFullNtpDifferencesAreWrapSafe()
    {
        long compactForward = RtpClockMath.GetCompactNtpDifference(
            0x00000010U,
            0xFFFFFFF0U);
        long compactBackward = RtpClockMath.GetCompactNtpDifference(
            0xFFFFFFF0U,
            0x00000010U);
        long compactTicks = RtpClockMath.GetCompactNtpDifferenceTicks(
            0x00000010U,
            0xFFFFFFF0U);
        long fullForwardTicks = RtpClockMath.GetNtpDifferenceTicks(
            0UL,
            0xFFFFFFFF00000000UL);
        long fullBackwardTicks = RtpClockMath.GetNtpDifferenceTicks(
            0xFFFFFFFF00000000UL,
            0UL);

        Assert.Multiple(() =>
        {
            Assert.That(compactForward, Is.EqualTo(32L));
            Assert.That(compactBackward, Is.EqualTo(-32L));
            Assert.That(compactTicks, Is.EqualTo(4882L));
            Assert.That(fullForwardTicks, Is.EqualTo(TimeSpan.TicksPerSecond));
            Assert.That(fullBackwardTicks, Is.EqualTo(-TimeSpan.TicksPerSecond));
        });
    }

    [Test]
    public void MonthBoundaryLeapSecondAmbiguityIsExcludedFromCorrelation()
    {
        long beforeWindow =
            new DateTime(2024, 1, 31, 23, 59, 58, 500, DateTimeKind.Utc).Ticks;
        long repeatedLastSecond =
            new DateTime(2024, 1, 31, 23, 59, 59, 500, DateTimeKind.Utc).Ticks;
        long repeatedFirstSecond =
            new DateTime(2024, 2, 1, 0, 0, 0, 500, DateTimeKind.Utc).Ticks;
        long afterWindow =
            new DateTime(2024, 2, 1, 0, 0, 1, DateTimeKind.Utc).Ticks;
        ulong repeatedFirstNtp = RtpNtpTime.FromUtcTicks(repeatedFirstSecond);

        Assert.Multiple(() =>
        {
            Assert.That(RtpClockMath.IsUtcClockCorrelationSafe(beforeWindow), Is.True);
            Assert.That(
                RtpClockMath.IsUtcClockCorrelationSafe(repeatedLastSecond),
                Is.False);
            Assert.That(
                RtpClockMath.IsUtcClockCorrelationSafe(repeatedFirstSecond),
                Is.False);
            Assert.That(RtpClockMath.IsUtcClockCorrelationSafe(afterWindow), Is.True);
            Assert.That(RtpClockMath.IsNtpClockCorrelationSafe(
                repeatedFirstNtp,
                repeatedFirstSecond), Is.False);
        });
    }

    [Test]
    public void NtpConversionRejectsUtcValuesOutsideItsSupportedRange()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<RtpConfigurationException>(() =>
                RtpNtpTime.FromUtcTicks(RtpNtpTime.NtpEpochTicks - 1L));
            Assert.Throws<RtpConfigurationException>(() =>
                RtpNtpTime.ToUtcTicks(0UL, RtpNtpTime.NtpEpochTicks - 1L));
            Assert.Throws<RtpConfigurationException>(() =>
                RtpClockMath.IsUtcClockCorrelationSafe(
                    RtpNtpTime.NtpEpochTicks - 1L));
        });
    }
}
