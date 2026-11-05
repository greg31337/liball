using rtp;

namespace rtp.Tests.Buffering;

public class RtpRetransmissionAdapterTests
{
    [TestCase((ushort)0, (byte)0, (byte)0)]
    [TestCase((ushort)255, (byte)0, (byte)255)]
    [TestCase((ushort)256, (byte)1, (byte)0)]
    [TestCase((ushort)65535, (byte)255, (byte)255)]
    public void WrapsOriginalSequenceNumberInNetworkOrder(
        ushort originalSequenceNumber,
        byte expectedHighByte,
        byte expectedLowByte)
    {
        RtpPacket original = CreateOriginalPacket(originalSequenceNumber);
        IRtpRetransmissionAdapter adapter = new RtpRetransmissionAdapter();

        RtpPacket retransmission = adapter.WrapRtpRetransmission(
            original,
            200U,
            97,
            500);

        Assert.Multiple(() =>
        {
            Assert.That(retransmission.SynchronizationSource, Is.EqualTo(200U));
            Assert.That(retransmission.PayloadType, Is.EqualTo(97));
            Assert.That(retransmission.SequenceNumber, Is.EqualTo(500));
            Assert.That(retransmission.Timestamp, Is.EqualTo(original.Timestamp));
            Assert.That(retransmission.Marker, Is.EqualTo(original.Marker));
            Assert.That(retransmission.ContributingSources,
                Is.EqualTo(original.ContributingSources));
            Assert.That(retransmission.HeaderExtension?.Data,
                Is.EqualTo(original.HeaderExtension?.Data));
            Assert.That(retransmission.Payload,
                Is.EqualTo(new byte[]
                {
                    expectedHighByte,
                    expectedLowByte,
                    10,
                    20,
                    30
                }));
            Assert.That(retransmission.HasPadding, Is.False);
            Assert.That(retransmission.Padding, Is.Empty);
        });
    }

    [Test]
    public void WrapProducesAnOwnedPacketAndRemovesOriginalPadding()
    {
        RtpPacket original = CreateOriginalPacket(321);
        RtpRetransmissionAdapter adapter = new RtpRetransmissionAdapter();

        RtpPacket retransmission = adapter.WrapRtpRetransmission(
            original,
            200U,
            97,
            500);
        original.Payload[0] = 99;
        original.ContributingSources[0] = 999U;
        original.HeaderExtension!.Data[0] = 99;
        retransmission.Payload[2] = 88;

        Assert.Multiple(() =>
        {
            Assert.That(retransmission.Payload,
                Is.EqualTo(new byte[] { 1, 65, 88, 20, 30 }));
            Assert.That(retransmission.ContributingSources,
                Is.EqualTo(new uint[] { 77U }));
            Assert.That(retransmission.HeaderExtension?.Data,
                Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(original.Payload,
                Is.EqualTo(new byte[] { 99, 20, 30 }));
        });
    }

    [Test]
    public void UnwrapRestoresOriginalIdentifiersAndPayload()
    {
        RtpRetransmissionAdapter adapter = new RtpRetransmissionAdapter();
        RtpPacket retransmission = CreateOriginalPacket(900);
        retransmission.SynchronizationSource = 200U;
        retransmission.PayloadType = 97;
        retransmission.SequenceNumber = 500;
        retransmission.Payload = new byte[] { 255, 255, 10, 20, 30 };
        retransmission.Padding = new byte[] { 0, 2 };
        retransmission.HasPadding = true;

        RtpPacket restored = adapter.UnwrapRtpRetransmission(
            retransmission,
            100U,
            96);
        retransmission.Payload[2] = 99;
        retransmission.HeaderExtension!.Data[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(restored.SynchronizationSource, Is.EqualTo(100U));
            Assert.That(restored.PayloadType, Is.EqualTo(96));
            Assert.That(restored.SequenceNumber, Is.EqualTo(65535));
            Assert.That(restored.Timestamp, Is.EqualTo(retransmission.Timestamp));
            Assert.That(restored.Marker, Is.EqualTo(retransmission.Marker));
            Assert.That(restored.Payload, Is.EqualTo(new byte[] { 10, 20, 30 }));
            Assert.That(restored.HeaderExtension?.Data,
                Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(restored.HasPadding, Is.False);
            Assert.That(restored.Padding, Is.Empty);
        });
    }

    [Test]
    public void RtxSequenceWrapIsIndependentFromOriginalPacketIdentity()
    {
        RtpRetransmissionAdapter adapter = new RtpRetransmissionAdapter();
        RtpPacket firstOriginal = CreateOriginalPacket(40000);
        RtpPacket secondOriginal = CreateOriginalPacket(40001);
        secondOriginal.Timestamp = firstOriginal.Timestamp + 3000U;

        RtpPacket beforeRtxWrap = adapter.WrapRtpRetransmission(
            firstOriginal,
            200U,
            97,
            65535);
        RtpPacket afterRtxWrap = adapter.WrapRtpRetransmission(
            secondOriginal,
            200U,
            97,
            0);
        RtpPacket firstRestored = adapter.UnwrapRtpRetransmission(
            beforeRtxWrap,
            100U,
            96);
        RtpPacket secondRestored = adapter.UnwrapRtpRetransmission(
            afterRtxWrap,
            100U,
            96);

        Assert.Multiple(() =>
        {
            Assert.That(beforeRtxWrap.SequenceNumber, Is.EqualTo(65535));
            Assert.That(afterRtxWrap.SequenceNumber, Is.Zero);
            Assert.That(firstRestored.SequenceNumber, Is.EqualTo(40000));
            Assert.That(secondRestored.SequenceNumber, Is.EqualTo(40001));
            Assert.That(firstRestored.Timestamp,
                Is.EqualTo(firstOriginal.Timestamp));
            Assert.That(secondRestored.Timestamp,
                Is.EqualTo(secondOriginal.Timestamp));
            Assert.That(firstRestored.SynchronizationSource, Is.EqualTo(100U));
            Assert.That(secondRestored.PayloadType, Is.EqualTo(96));
        });
    }

    [Test]
    public void RepeatedRetransmissionKeepsOsnWhileRtxIdentityChanges()
    {
        RtpRetransmissionAdapter adapter = new RtpRetransmissionAdapter();
        RtpPacket original = CreateOriginalPacket(65535);

        RtpPacket first = adapter.WrapRtpRetransmission(
            original,
            200U,
            97,
            20);
        RtpPacket second = adapter.WrapRtpRetransmission(
            original,
            201U,
            98,
            21);

        Assert.Multiple(() =>
        {
            Assert.That(first.SequenceNumber, Is.EqualTo(20));
            Assert.That(second.SequenceNumber, Is.EqualTo(21));
            Assert.That(first.SynchronizationSource, Is.EqualTo(200U));
            Assert.That(second.SynchronizationSource, Is.EqualTo(201U));
            Assert.That(first.PayloadType, Is.EqualTo(97));
            Assert.That(second.PayloadType, Is.EqualTo(98));
            Assert.That(first.Payload[0], Is.EqualTo(255));
            Assert.That(first.Payload[1], Is.EqualTo(255));
            Assert.That(second.Payload[0], Is.EqualTo(255));
            Assert.That(second.Payload[1], Is.EqualTo(255));
        });
    }

    [Test]
    public void HistoryLifecycleAcrossWrapFeedsIndependentRtxSequenceSpace()
    {
        RtpSessionConfiguration configuration =
            new RtpSessionConfiguration();
        configuration.MaximumPacketHistoryCount = 2;
        configuration.MaximumPacketHistoryBytes = 1000;
        configuration.MaximumPacketHistoryAgeTicks = 5L;
        RtpPacketHistory history = new RtpPacketHistory(configuration);
        RtpPacket beforeMediaWrap = CreateOriginalPacket(65535);
        RtpPacket afterMediaWrap = CreateOriginalPacket(0);
        afterMediaWrap.Payload = new byte[] { 20 };
        history.StoreRtpHistoryPacket(
            beforeMediaWrap,
            CreateContext(1L));
        history.StoreRtpHistoryPacket(
            afterMediaWrap,
            CreateContext(2L));

        RtpRetransmissionAdapter adapter = new RtpRetransmissionAdapter();
        RtpPacket firstRetransmission = adapter.WrapRtpRetransmission(
            history.FindRtpHistoryPacket(100U, 65535)!,
            200U,
            97,
            65535);
        RtpPacket firstRestored = adapter.UnwrapRtpRetransmission(
            firstRetransmission,
            100U,
            96);

        RtpPacket replacement = CreateOriginalPacket(0);
        replacement.Payload = new byte[] { 40 };
        history.StoreRtpHistoryPacket(replacement, CreateContext(3L));
        RtpPacket? replacementLookup =
            history.FindRtpHistoryPacket(100U, 0);
        RtpPacket nextOriginal = CreateOriginalPacket(1);
        nextOriginal.Payload = new byte[] { 50 };
        history.StoreRtpHistoryPacket(nextOriginal, CreateContext(4L));
        RtpPacket? countEvicted =
            history.FindRtpHistoryPacket(100U, 65535);
        int expiredCount = history.RemoveExpiredRtpHistoryPackets(9L);
        RtpPacket? ageExpired = history.FindRtpHistoryPacket(100U, 0);
        RtpPacket? retainedAtBoundary =
            history.FindRtpHistoryPacket(100U, 1);
        RtpPacket? missingOriginal =
            history.FindRtpHistoryPacket(100U, 2);

        RtpPacket secondRetransmission = adapter.WrapRtpRetransmission(
            retainedAtBoundary!,
            200U,
            97,
            0);
        RtpPacket secondRestored = adapter.UnwrapRtpRetransmission(
            secondRetransmission,
            100U,
            96);

        Assert.Multiple(() =>
        {
            Assert.That(firstRetransmission.SynchronizationSource,
                Is.EqualTo(200U));
            Assert.That(firstRetransmission.PayloadType, Is.EqualTo(97));
            Assert.That(firstRetransmission.SequenceNumber,
                Is.EqualTo(65535));
            Assert.That(firstRestored.SynchronizationSource,
                Is.EqualTo(100U));
            Assert.That(firstRestored.PayloadType, Is.EqualTo(96));
            Assert.That(firstRestored.SequenceNumber, Is.EqualTo(65535));
            Assert.That(replacementLookup?.Payload,
                Is.EqualTo(new byte[] { 40 }));
            Assert.That(countEvicted, Is.Null);
            Assert.That(expiredCount, Is.EqualTo(1));
            Assert.That(ageExpired, Is.Null);
            Assert.That(retainedAtBoundary?.Payload,
                Is.EqualTo(new byte[] { 50 }));
            Assert.That(missingOriginal, Is.Null);
            Assert.That(secondRetransmission.SynchronizationSource,
                Is.EqualTo(200U));
            Assert.That(secondRetransmission.PayloadType, Is.EqualTo(97));
            Assert.That(secondRetransmission.SequenceNumber, Is.Zero);
            Assert.That(secondRestored.SynchronizationSource,
                Is.EqualTo(100U));
            Assert.That(secondRestored.PayloadType, Is.EqualTo(96));
            Assert.That(secondRestored.SequenceNumber, Is.EqualTo(1));
            Assert.That(secondRestored.Payload,
                Is.EqualTo(new byte[] { 50 }));
            Assert.That(history.entries, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void RejectsRetransmissionThatExceedsMaximumRtpPacketSize()
    {
        RtpRetransmissionAdapter adapter = new RtpRetransmissionAdapter();
        RtpPacket original = new RtpPacket();
        original.PayloadType = 96;
        original.SequenceNumber = 1;
        original.SynchronizationSource = 100U;
        original.Payload = new byte[65523];

        Assert.Throws<RtpPacketFormatException>(() =>
            adapter.WrapRtpRetransmission(original, 200U, 97, 1));
    }

    [Test]
    public void RejectsMalformedPacketsAndPayloadTypes()
    {
        RtpRetransmissionAdapter adapter = new RtpRetransmissionAdapter();
        RtpPacket valid = CreateOriginalPacket(1);
        RtpPacket shortRetransmission = CreateOriginalPacket(2);
        shortRetransmission.Payload = new byte[] { 0 };
        RtpPacket invalidVersion = CreateOriginalPacket(3);
        invalidVersion.Version = 1;

        Assert.Multiple(() =>
        {
            Assert.Throws<RtpPacketFormatException>(() =>
                adapter.WrapRtpRetransmission(null!, 1U, 96, 1));
            Assert.Throws<RtpPacketFormatException>(() =>
                adapter.WrapRtpRetransmission(invalidVersion, 1U, 96, 1));
            Assert.Throws<RtpPacketFormatException>(() =>
                adapter.WrapRtpRetransmission(valid, 1U, 128, 1));
            Assert.Throws<RtpPacketFormatException>(() =>
                adapter.UnwrapRtpRetransmission(
                    shortRetransmission,
                    1U,
                    96));
            Assert.Throws<RtpPacketFormatException>(() =>
                adapter.UnwrapRtpRetransmission(valid, 1U, 128));
        });
    }

    static RtpPacket CreateOriginalPacket(ushort sequenceNumber)
    {
        RtpPacket packet = new RtpPacket();
        packet.Marker = true;
        packet.PayloadType = 96;
        packet.SequenceNumber = sequenceNumber;
        packet.Timestamp = 123456U;
        packet.SynchronizationSource = 100U;
        packet.ContributingSources = new uint[] { 77U };
        packet.HasExtension = true;
        packet.HeaderExtension = new RtpHeaderExtension();
        packet.HeaderExtension.Profile = 4660;
        packet.HeaderExtension.Data = new byte[] { 1, 2, 3, 4 };
        packet.Payload = new byte[] { 10, 20, 30 };
        packet.HasPadding = true;
        packet.Padding = new byte[] { 0, 2 };
        return packet;
    }

    static RtpPacketContext CreateContext(long arrivalTimeTicks)
    {
        RtpPacketContext context = new RtpPacketContext();
        context.ArrivalTimeTicks = arrivalTimeTicks;
        return context;
    }
}
