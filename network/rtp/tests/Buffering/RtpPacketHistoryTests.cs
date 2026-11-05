using rtp;

namespace rtp.Tests.Buffering;

public class RtpPacketHistoryTests
{
    [Test]
    public void FindsOwnedPacketsBySourceAndSequenceAcrossWrap()
    {
        RtpSessionConfiguration configuration =
            CreateConfiguration(5, 1000, 0L);
        RtpPacketHistory history = new RtpPacketHistory(configuration);
        configuration.MaximumPacketHistoryCount = 1;
        configuration.MaximumPacketHistoryBytes = 1;
        RtpPacket beforeWrap = CreatePacket(10U, 65535, 1);
        RtpPacketContext beforeWrapContext = CreateContext(1L);
        history.StoreRtpHistoryPacket(beforeWrap, beforeWrapContext);
        history.StoreRtpHistoryPacket(
            CreatePacket(10U, 0, 2),
            CreateContext(2L));
        history.StoreRtpHistoryPacket(
            CreatePacket(20U, 0, 3),
            CreateContext(3L));
        beforeWrap.Payload[0] = 99;
        beforeWrapContext.ArrivalTimeTicks = 99L;

        RtpPacket? foundBeforeWrap =
            history.FindRtpHistoryPacket(10U, 65535);
        RtpPacket? foundAfterWrap = history.FindRtpHistoryPacket(10U, 0);
        RtpPacket? foundOtherSource = history.FindRtpHistoryPacket(20U, 0);
        RtpPacket? missing = history.FindRtpHistoryPacket(30U, 0);
        foundAfterWrap!.Payload[0] = 88;
        RtpPacket? foundAgain = history.FindRtpHistoryPacket(10U, 0);
        history.StoreRtpHistoryPacket(
            CreatePacket(10U, 0, 4),
            CreateContext(4L));
        RtpPacket? replaced = history.FindRtpHistoryPacket(10U, 0);

        Assert.Multiple(() =>
        {
            Assert.That(foundBeforeWrap?.Payload,
                Is.EqualTo(new byte[] { 1 }));
            Assert.That(foundAfterWrap.SynchronizationSource,
                Is.EqualTo(10U));
            Assert.That(foundOtherSource?.Payload,
                Is.EqualTo(new byte[] { 3, 3, 3 }));
            Assert.That(missing, Is.Null);
            Assert.That(foundAgain?.Payload,
                Is.EqualTo(new byte[] { 2, 2 }));
            Assert.That(replaced?.Payload,
                Is.EqualTo(new byte[] { 4, 4, 4, 4 }));
            Assert.That(history.entries, Has.Count.EqualTo(3));
            Assert.That(history.entries[0].Context.ArrivalTimeTicks,
                Is.EqualTo(1L));
        });
    }

    [Test]
    public void NewestPacketsEvictOldestForCountAndEncodedByteBounds()
    {
        RtpPacketHistory countBounded = new RtpPacketHistory(
            CreateConfiguration(2, 1000, 0L));
        countBounded.StoreRtpHistoryPacket(
            CreatePacket(10U, 1, 1),
            CreateContext(1L));
        countBounded.StoreRtpHistoryPacket(
            CreatePacket(10U, 2, 1),
            CreateContext(2L));
        countBounded.StoreRtpHistoryPacket(
            CreatePacket(10U, 3, 1),
            CreateContext(3L));

        RtpPacketHistory byteBounded = new RtpPacketHistory(
            CreateConfiguration(5, 29, 0L));
        byteBounded.StoreRtpHistoryPacket(
            CreatePacket(20U, 1, 1),
            CreateContext(1L));
        byteBounded.StoreRtpHistoryPacket(
            CreatePacket(20U, 2, 2),
            CreateContext(2L));
        byteBounded.StoreRtpHistoryPacket(
            CreatePacket(20U, 3, 3),
            CreateContext(3L));

        Assert.Multiple(() =>
        {
            Assert.That(countBounded.FindRtpHistoryPacket(10U, 1), Is.Null);
            Assert.That(countBounded.FindRtpHistoryPacket(10U, 2),
                Is.Not.Null);
            Assert.That(countBounded.FindRtpHistoryPacket(10U, 3),
                Is.Not.Null);
            Assert.That(countBounded.entries, Has.Count.EqualTo(2));
            Assert.That(countBounded.retainedPacketBytes, Is.EqualTo(26));
            Assert.That(byteBounded.FindRtpHistoryPacket(20U, 1), Is.Null);
            Assert.That(byteBounded.FindRtpHistoryPacket(20U, 2),
                Is.Not.Null);
            Assert.That(byteBounded.FindRtpHistoryPacket(20U, 3),
                Is.Not.Null);
            Assert.That(byteBounded.entries, Has.Count.EqualTo(2));
            Assert.That(byteBounded.retainedPacketBytes, Is.EqualTo(29));
        });
    }

    [Test]
    public void AgeExpiryUsesStrictBoundaryAndStoreTimePruning()
    {
        RtpPacketHistory history = new RtpPacketHistory(
            CreateConfiguration(5, 1000, 5L));
        history.StoreRtpHistoryPacket(
            CreatePacket(10U, 1, 1),
            CreateContext(10L));
        history.StoreRtpHistoryPacket(
            CreatePacket(10U, 2, 1),
            CreateContext(12L));

        int atBoundary = history.RemoveExpiredRtpHistoryPackets(15L);
        int afterBoundary = history.RemoveExpiredRtpHistoryPackets(16L);
        history.StoreRtpHistoryPacket(
            CreatePacket(10U, 3, 1),
            CreateContext(20L));
        int newestAtBoundary =
            history.RemoveExpiredRtpHistoryPackets(25L);
        int newestAfterBoundary =
            history.RemoveExpiredRtpHistoryPackets(26L);

        Assert.Multiple(() =>
        {
            Assert.That(atBoundary, Is.Zero);
            Assert.That(afterBoundary, Is.EqualTo(1));
            Assert.That(history.FindRtpHistoryPacket(10U, 1), Is.Null);
            Assert.That(history.FindRtpHistoryPacket(10U, 2), Is.Null);
            Assert.That(newestAtBoundary, Is.Zero);
            Assert.That(newestAfterBoundary, Is.EqualTo(1));
            Assert.That(history.entries, Is.Empty);
            Assert.That(history.retainedPacketBytes, Is.Zero);
        });
    }

    [Test]
    public void CapacityFailureAndBackwardTimeDoNotMutateHistory()
    {
        RtpPacketHistory history = new RtpPacketHistory(
            CreateConfiguration(2, 13, 0L));
        history.StoreRtpHistoryPacket(
            CreatePacket(10U, 1, 1),
            CreateContext(1L));

        bool retained = history.TryStoreRtpHistoryPacket(
            CreatePacket(10U, 2, 2),
            CreateContext(2L));
        Assert.Throws<RtpBufferCapacityException>(() =>
            history.StoreRtpHistoryPacket(
                CreatePacket(10U, 2, 2),
                CreateContext(2L)));
        int unchangedTime = history.RemoveExpiredRtpHistoryPackets(1L);
        Assert.Throws<RtpSessionException>(() =>
            history.StoreRtpHistoryPacket(
                CreatePacket(10U, 3, 1),
                CreateContext(0L)));
        Assert.Throws<RtpSessionException>(() =>
            history.RemoveExpiredRtpHistoryPackets(0L));

        RtpPacketHistory disabled = new RtpPacketHistory(
            CreateConfiguration(0, 0, 0L));
        bool disabledTry = disabled.TryStoreRtpHistoryPacket(
            CreatePacket(20U, 1, 1),
            CreateContext(1L));
        Assert.Throws<RtpBufferCapacityException>(() =>
            disabled.StoreRtpHistoryPacket(
                CreatePacket(20U, 1, 1),
                CreateContext(1L)));

        history.ClearRtpPacketHistory();
        history.StoreRtpHistoryPacket(
            CreatePacket(10U, 4, 1),
            CreateContext(0L));

        Assert.Multiple(() =>
        {
            Assert.That(retained, Is.False);
            Assert.That(unchangedTime, Is.Zero);
            Assert.That(disabledTry, Is.False);
            Assert.That(disabled.entries, Is.Empty);
            Assert.That(history.entries, Has.Count.EqualTo(1));
            Assert.That(history.FindRtpHistoryPacket(10U, 4), Is.Not.Null);
            Assert.That(history.retainedPacketBytes, Is.EqualTo(13));
        });
    }

    [Test]
    public void InvalidConfigurationAndPacketInputsAreRejected()
    {
        RtpSessionConfiguration negativeCount =
            CreateConfiguration(-1, 100, 0L);
        RtpSessionConfiguration missingBytes =
            CreateConfiguration(1, 0, 0L);
        RtpSessionConfiguration negativeAge =
            CreateConfiguration(1, 100, -1L);
        RtpPacketHistory history = new RtpPacketHistory(
            CreateConfiguration(1, 100, 0L));
        RtpPacket invalidPacket = CreatePacket(10U, 1, 1);
        invalidPacket.Version = 1;
        RtpPacketContext negativeTime = CreateContext(-1L);

        Assert.Multiple(() =>
        {
            Assert.Throws<RtpConfigurationException>(() =>
                new RtpPacketHistory(negativeCount));
            Assert.Throws<RtpConfigurationException>(() =>
                new RtpPacketHistory(missingBytes));
            Assert.Throws<RtpConfigurationException>(() =>
                new RtpPacketHistory(negativeAge));
            Assert.Throws<RtpPacketFormatException>(() =>
                history.StoreRtpHistoryPacket(
                    invalidPacket,
                    CreateContext(0L)));
            Assert.Throws<RtpConfigurationException>(() =>
                history.TryStoreRtpHistoryPacket(
                    CreatePacket(10U, 1, 1),
                    negativeTime));
            Assert.Throws<RtpConfigurationException>(() =>
                history.TryStoreRtpHistoryPacket(
                    CreatePacket(10U, 1, 1),
                    null!));
            Assert.That(history.entries, Is.Empty);
        });
    }

    private static RtpSessionConfiguration CreateConfiguration(
        int maximumCount,
        int maximumBytes,
        long maximumAgeTicks)
    {
        RtpSessionConfiguration configuration =
            new RtpSessionConfiguration();
        configuration.MaximumPacketHistoryCount = maximumCount;
        configuration.MaximumPacketHistoryBytes = maximumBytes;
        configuration.MaximumPacketHistoryAgeTicks = maximumAgeTicks;
        return configuration;
    }

    private static RtpPacket CreatePacket(
        uint sourceIdentifier,
        ushort sequenceNumber,
        int payloadSize)
    {
        RtpPacket packet = new RtpPacket();
        packet.SynchronizationSource = sourceIdentifier;
        packet.SequenceNumber = sequenceNumber;
        packet.PayloadType = 96;
        packet.Payload = new byte[payloadSize];
        int payloadIndex = 0;
        while (payloadIndex < packet.Payload.Length)
        {
            packet.Payload[payloadIndex] = (byte)payloadSize;
            payloadIndex = payloadIndex + 1;
        }

        return packet;
    }

    private static RtpPacketContext CreateContext(long arrivalTimeTicks)
    {
        RtpPacketContext context = new RtpPacketContext();
        context.ArrivalTimeTicks = arrivalTimeTicks;
        return context;
    }
}
