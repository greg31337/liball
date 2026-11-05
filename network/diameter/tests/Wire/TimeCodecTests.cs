using System;

namespace diameter.Tests;

public class TimeCodecTests
{
    [Test]
    public void TimeEncodingUsesNtpEpochAndNetworkByteOrder()
    {
        DateTimeOffset ntpEpoch = new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset unixEpoch = DateTimeOffset.UnixEpoch;
        byte[] ntpBytes = new byte[4];
        byte[] unixBytes = new byte[4];

        DiameterValueResult ntpResult = ValueCodecs.EncodeTime(ntpBytes, ntpEpoch);
        DiameterValueResult unixResult = ValueCodecs.EncodeTime(unixBytes, unixEpoch);

        Assert.Multiple(() =>
        {
            AssertSuccess(ntpResult, 0, 4);
            AssertSuccess(unixResult, 0, 4);
            Assert.That(ntpBytes, Is.EqualTo(new byte[] { 0, 0, 0, 0 }));
            Assert.That(unixBytes, Is.EqualTo(new byte[] { 131, 170, 126, 128 }));
            Assert.That(ntpResult.Time, Is.EqualTo(ntpEpoch));
            Assert.That(unixResult.Time, Is.EqualTo(unixEpoch));
        });
    }

    [Test]
    public void TimeCodecCrossesThe2036EraBoundary()
    {
        DateTimeOffset boundary = new DateTimeOffset(2036, 2, 7, 6, 28, 16, TimeSpan.Zero);
        DateTimeOffset before = boundary.AddSeconds(-1);
        DateTimeOffset after = boundary.AddSeconds(1);
        byte[] beforeBytes = new byte[4];
        byte[] boundaryBytes = new byte[4];
        byte[] afterBytes = new byte[4];

        ValueCodecs.EncodeTime(beforeBytes, before);
        ValueCodecs.EncodeTime(boundaryBytes, boundary);
        ValueCodecs.EncodeTime(afterBytes, after);

        DiameterValueResult decodedBefore = ValueCodecs.DecodeTime(beforeBytes, boundary);
        DiameterValueResult decodedBoundary = ValueCodecs.DecodeTime(boundaryBytes, boundary);
        DiameterValueResult decodedAfter = ValueCodecs.DecodeTime(afterBytes, boundary);

        Assert.Multiple(() =>
        {
            Assert.That(beforeBytes, Is.EqualTo(new byte[] { 255, 255, 255, 255 }));
            Assert.That(boundaryBytes, Is.EqualTo(new byte[] { 0, 0, 0, 0 }));
            Assert.That(afterBytes, Is.EqualTo(new byte[] { 0, 0, 0, 1 }));
            Assert.That(decodedBefore.Time, Is.EqualTo(before));
            Assert.That(decodedBoundary.Time, Is.EqualTo(boundary));
            Assert.That(decodedAfter.Time, Is.EqualTo(after));
            Assert.That(decodedBefore.Unsigned32, Is.EqualTo(uint.MaxValue));
            Assert.That(decodedBoundary.Unsigned32, Is.Zero);
            Assert.That(decodedAfter.Unsigned32, Is.EqualTo(1U));
        });
    }

    [Test]
    public void TimeDecodingUsesTheReferenceToSelectTheCorrectEra()
    {
        byte[] zeroSeconds = new byte[] { 0, 0, 0, 0 };
        DateTimeOffset eraZeroReference = new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset eraOneReference = new DateTimeOffset(2036, 2, 7, 6, 28, 16, TimeSpan.Zero);

        DiameterValueResult eraZero = ValueCodecs.DecodeTime(zeroSeconds, eraZeroReference);
        DiameterValueResult eraOne = ValueCodecs.DecodeTime(zeroSeconds, eraOneReference);

        Assert.Multiple(() =>
        {
            AssertSuccess(eraZero, 4, 0);
            AssertSuccess(eraOne, 4, 0);
            Assert.That(eraZero.Time, Is.EqualTo(eraZeroReference));
            Assert.That(eraOne.Time, Is.EqualTo(eraOneReference));
        });
    }

    [Test]
    public void ExactHalfEraTieDeterministicallySelectsTheEarlierInstant()
    {
        byte[] halfEra = new byte[] { 128, 0, 0, 0 };
        DateTimeOffset eraOneBoundary = new DateTimeOffset(2036, 2, 7, 6, 28, 16, TimeSpan.Zero);
        long expectedUnixSeconds = DiameterProtocolConstants.NtpEraSeconds / 2L -
            DiameterProtocolConstants.NtpUnixEpochOffsetSeconds;
        DateTimeOffset expected = DateTimeOffset.FromUnixTimeSeconds(expectedUnixSeconds);

        DiameterValueResult result = ValueCodecs.DecodeTime(halfEra, eraOneBoundary);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(result.Time, Is.EqualTo(expected));
            Assert.That(result.Time, Is.LessThan(eraOneBoundary));
        });
    }

    [Test]
    public void TimeCodecSupportsLaterErasWhenReferenceIsNearby()
    {
        DateTimeOffset value = new DateTimeOffset(2104, 2, 7, 6, 28, 17, TimeSpan.Zero);
        byte[] destination = new byte[4];

        ValueCodecs.EncodeTime(destination, value);
        DiameterValueResult result = ValueCodecs.DecodeTime(destination, value);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(result.Time, Is.EqualTo(value));
            Assert.That(result.ConsumedCount, Is.EqualTo(4));
        });
    }

    [Test]
    public void TimeEncodingNormalizesOffsetsAndTruncatesSubseconds()
    {
        DateTimeOffset value = new DateTimeOffset(
            2026,
            8,
            28,
            12,
            0,
            0,
            750,
            TimeSpan.FromHours(2));
        DateTimeOffset expected = DateTimeOffset.FromUnixTimeSeconds(value.ToUnixTimeSeconds());
        byte[] destination = new byte[4];

        DiameterValueResult encoding = ValueCodecs.EncodeTime(destination, value);
        DiameterValueResult decoding = ValueCodecs.DecodeTime(destination, expected);

        Assert.Multiple(() =>
        {
            Assert.That(encoding.Time, Is.EqualTo(expected));
            Assert.That(decoding.Time, Is.EqualTo(expected));
            Assert.That(decoding.Time.Offset, Is.EqualTo(TimeSpan.Zero));
        });
    }

    [Test]
    public void TimeCodecRejectsInvalidDatesLengthsAndShortDestinations()
    {
        DateTimeOffset beforeEpoch = new DateTimeOffset(1899, 12, 31, 23, 59, 59, TimeSpan.Zero);
        byte[] shortDestination = new byte[] { 170, 170, 170 };
        DiameterValueResult earlyEncoding = ValueCodecs.EncodeTime(new byte[4], beforeEpoch);
        DiameterValueResult shortEncoding = ValueCodecs.EncodeTime(
            shortDestination,
            DateTimeOffset.UnixEpoch);
        DiameterValueResult shortDecoding = ValueCodecs.DecodeTime(
            new byte[3],
            DateTimeOffset.UnixEpoch);
        DiameterValueResult longDecoding = ValueCodecs.DecodeTime(
            new byte[5],
            DateTimeOffset.UnixEpoch);
        DiameterValueResult earlyReference = ValueCodecs.DecodeTime(
            new byte[4],
            beforeEpoch);
        DiameterValueResult unfoldedBeforeEpoch = ValueCodecs.DecodeTime(
            new byte[] { 255, 255, 255, 255 },
            new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeSpan.Zero));

        Assert.Multiple(() =>
        {
            AssertFailure(earlyEncoding, DiameterCodecStatus.Invalid, 4, 0, "time-before-ntp-epoch");
            AssertFailure(shortEncoding, DiameterCodecStatus.DestinationTooSmall, 4, 0, "destination-too-small");
            Assert.That(shortDestination, Is.All.EqualTo(170));
            AssertFailure(shortDecoding, DiameterCodecStatus.Incomplete, 4, 3, "incomplete-time");
            AssertFailure(longDecoding, DiameterCodecStatus.Invalid, 4, 4, "invalid-time-length");
            AssertFailure(earlyReference, DiameterCodecStatus.Invalid, 4, 0, "reference-before-ntp-epoch");
            AssertFailure(unfoldedBeforeEpoch, DiameterCodecStatus.Invalid, 4, 0, "time-before-ntp-epoch");
        });
    }

    private static void AssertSuccess(
        DiameterValueResult result,
        int consumedCount,
        int writtenCount)
    {
        Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Success));
        Assert.That(result.RequiredCount, Is.EqualTo(4));
        Assert.That(result.ConsumedCount, Is.EqualTo(consumedCount));
        Assert.That(result.WrittenCount, Is.EqualTo(writtenCount));
        Assert.That(result.Issues, Is.Empty);
    }

    private static void AssertFailure(
        DiameterValueResult result,
        DiameterCodecStatus status,
        int requiredCount,
        int offset,
        string code)
    {
        Assert.That(result.Status, Is.EqualTo(status));
        Assert.That(result.RequiredCount, Is.EqualTo(requiredCount));
        Assert.That(result.ConsumedCount, Is.Zero);
        Assert.That(result.WrittenCount, Is.Zero);
        Assert.That(result.Issues, Has.Count.EqualTo(1));
        Assert.That(result.Issues[0].Offset, Is.EqualTo(offset));
        Assert.That(result.Issues[0].Code, Is.EqualTo(code));
    }
}
