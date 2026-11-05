using System.Threading;
using rtp;

namespace rtp.Tests.Timing;

public class SystemEnvironmentServiceTests
{
    [Test]
    public void SystemClockProvidesMonotonicElapsedAndCurrentUtcTime()
    {
        IRtpClock clock = new SystemRtpClock();
        long firstElapsed = clock.GetMonotonicTimeTicks();
        Thread.SpinWait(100000);
        long secondElapsed = clock.GetMonotonicTimeTicks();
        long utcBefore = DateTime.UtcNow.Ticks;
        long clockUtc = clock.GetUtcTimeTicks();
        long utcAfter = DateTime.UtcNow.Ticks;

        Assert.Multiple(() =>
        {
            Assert.That(firstElapsed, Is.GreaterThanOrEqualTo(0L));
            Assert.That(secondElapsed, Is.GreaterThan(firstElapsed));
            Assert.That(clockUtc, Is.GreaterThanOrEqualTo(utcBefore));
            Assert.That(clockUtc, Is.LessThanOrEqualTo(utcAfter));
        });
    }

    [Test]
    public void SystemClockNtpTimestampMatchesTheUtcSamplingWindow()
    {
        IRtpClock clock = new SystemRtpClock();
        long utcBefore = DateTime.UtcNow.Ticks;
        ulong ntpTimestamp = clock.GetNtpTimestamp();
        long utcAfter = DateTime.UtcNow.Ticks;
        ulong seconds = ntpTimestamp >> 32;
        ulong fraction = ntpTimestamp & 4294967295UL;
        long decodedTicks = SystemRtpClock.NtpEpochTicks +
            (long)seconds * SystemRtpClock.TimeSpanTicksPerSecond +
            (long)((fraction * (ulong)SystemRtpClock.TimeSpanTicksPerSecond) /
            SystemRtpClock.NtpFractionScale);

        Assert.Multiple(() =>
        {
            Assert.That(decodedTicks, Is.GreaterThanOrEqualTo(utcBefore - 1L));
            Assert.That(decodedTicks, Is.LessThanOrEqualTo(utcAfter));
            Assert.That(fraction, Is.LessThan(SystemRtpClock.NtpFractionScale));
        });
    }

    [Test]
    public void SecureRandomSourceProducesDiverseFullWidthIdentifiers()
    {
        IRtpRandomSource random = new SecureRtpRandomSource();
        HashSet<uint> sourceIdentifiers = new HashSet<uint>();
        HashSet<ushort> sequenceNumbers = new HashSet<ushort>();
        HashSet<uint> timestamps = new HashSet<uint>();
        bool sourceHighBit = false;
        bool sourceLowBit = false;
        bool timestampHighBit = false;
        bool timestampLowBit = false;
        int sampleIndex = 0;
        while (sampleIndex < 512)
        {
            uint sourceIdentifier = random.CreateSynchronizationSource();
            ushort sequenceNumber = random.CreateSequenceNumber();
            uint timestamp = random.CreateTimestamp();
            sourceIdentifiers.Add(sourceIdentifier);
            sequenceNumbers.Add(sequenceNumber);
            timestamps.Add(timestamp);
            if ((sourceIdentifier & 0x80000000U) == 0)
            {
                sourceLowBit = true;
            }
            else
            {
                sourceHighBit = true;
            }

            if ((timestamp & 0x80000000U) == 0)
            {
                timestampLowBit = true;
            }
            else
            {
                timestampHighBit = true;
            }

            sampleIndex = sampleIndex + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(sourceIdentifiers.Count, Is.GreaterThan(500));
            Assert.That(sequenceNumbers.Count, Is.GreaterThan(480));
            Assert.That(timestamps.Count, Is.GreaterThan(500));
            Assert.That(sourceHighBit, Is.True);
            Assert.That(sourceLowBit, Is.True);
            Assert.That(timestampHighBit, Is.True);
            Assert.That(timestampLowBit, Is.True);
        });
    }

    [Test]
    public void SecureRandomSourceUnitValuesStayWithinTheHalfOpenInterval()
    {
        IRtpRandomSource random = new SecureRtpRandomSource();
        HashSet<double> values = new HashSet<double>();
        double minimum = 1.0;
        double maximum = 0.0;
        int sampleIndex = 0;
        while (sampleIndex < 512)
        {
            double value = random.CreateUnitIntervalValue();
            Assert.That(value, Is.GreaterThanOrEqualTo(0.0));
            Assert.That(value, Is.LessThan(1.0));
            values.Add(value);
            if (value < minimum)
            {
                minimum = value;
            }

            if (value > maximum)
            {
                maximum = value;
            }

            sampleIndex = sampleIndex + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(values.Count, Is.GreaterThan(500));
            Assert.That(minimum, Is.LessThan(0.25));
            Assert.That(maximum, Is.GreaterThan(0.75));
        });
    }
}
