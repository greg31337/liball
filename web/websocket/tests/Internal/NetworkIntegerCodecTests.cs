using websocket;

namespace websocket.Tests;

public class NetworkIntegerCodecTests
{
    [Test]
    public void Unsigned16ReadUsesNetworkOrderAndPreservesTrailingBytes()
    {
        var result = NetworkIntegerCodec.ReadUnsigned16(new byte[] { 0x12, 0x34, 0x99 });

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.ConsumedCount, Is.EqualTo(2));
            Assert.That(result.Value, Is.EqualTo(0x1234));
        });
    }

    [TestCase(0)]
    [TestCase(1)]
    public void Unsigned16ReadReportsIncompleteWithoutConsumption(int sourceLength)
    {
        var source = Enumerable.Repeat((byte)0xFF, sourceLength).ToArray();

        var result = NetworkIntegerCodec.ReadUnsigned16(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.NeedInput));
            Assert.That(result.ConsumedCount, Is.Zero);
            Assert.That(result.Value, Is.Zero);
        });
    }

    [Test]
    public void Unsigned63ReadSupportsMaximumValueAndPreservesTrailingBytes()
    {
        var source = new byte[]
        {
            0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x42
        };

        var result = NetworkIntegerCodec.ReadUnsigned63(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.ConsumedCount, Is.EqualTo(8));
            Assert.That(result.Value, Is.EqualTo(long.MaxValue));
        });
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    public void Unsigned63ReadReportsEveryPartialWidthWithoutConsumption(int sourceLength)
    {
        var source = Enumerable.Repeat((byte)0x7F, sourceLength).ToArray();

        var result = NetworkIntegerCodec.ReadUnsigned63(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.NeedInput));
            Assert.That(result.ConsumedCount, Is.Zero);
            Assert.That(result.Value, Is.Zero);
        });
    }

    [Test]
    public void Unsigned63ReadRejectsSetSignBitWithoutOverflow()
    {
        var source = new byte[]
        {
            0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x55
        };

        var result = NetworkIntegerCodec.ReadUnsigned63(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(result.ConsumedCount, Is.EqualTo(8));
            Assert.That(result.Value, Is.Zero);
        });
    }

    [Test]
    public void Canonical16ReadAcceptsBoundaryAndRejectsShortEncoding()
    {
        var nonCanonical = NetworkIntegerCodec.ReadCanonicalPayloadLength16(new byte[] { 0x00, 0x7D });
        var canonical = NetworkIntegerCodec.ReadCanonicalPayloadLength16(new byte[] { 0x00, 0x7E });

        Assert.Multiple(() =>
        {
            Assert.That(nonCanonical.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(nonCanonical.ConsumedCount, Is.EqualTo(2));
            Assert.That(nonCanonical.Value, Is.EqualTo(125));
            Assert.That(canonical.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(canonical.Value, Is.EqualTo(126));
        });
    }

    [Test]
    public void Canonical63ReadAcceptsBoundaryAndRejectsShortEncoding()
    {
        var nonCanonical = NetworkIntegerCodec.ReadCanonicalPayloadLength63(
            new byte[] { 0, 0, 0, 0, 0, 0, 0xFF, 0xFF });
        var canonical = NetworkIntegerCodec.ReadCanonicalPayloadLength63(
            new byte[] { 0, 0, 0, 0, 0, 1, 0, 0 });

        Assert.Multiple(() =>
        {
            Assert.That(nonCanonical.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(nonCanonical.ConsumedCount, Is.EqualTo(8));
            Assert.That(nonCanonical.Value, Is.EqualTo(65535));
            Assert.That(canonical.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(canonical.Value, Is.EqualTo(65536));
        });
    }

    [Test]
    public void Unsigned16WriteUsesNetworkOrderAndRejectsOutOfRangeValues()
    {
        var destination = new byte[] { 0xCC, 0xCC, 0xCC };
        var complete = NetworkIntegerCodec.WriteUnsigned16(0x1234, destination);
        var negative = NetworkIntegerCodec.WriteUnsigned16(-1, new byte[2]);
        var tooLarge = NetworkIntegerCodec.WriteUnsigned16(65536, new byte[2]);

        Assert.Multiple(() =>
        {
            Assert.That(complete.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(complete.ProducedCount, Is.EqualTo(2));
            Assert.That(destination, Is.EqualTo(new byte[] { 0x12, 0x34, 0xCC }));
            Assert.That(negative.Status, Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(negative.ProducedCount, Is.Zero);
            Assert.That(tooLarge.Status, Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(tooLarge.ProducedCount, Is.Zero);
        });
    }

    [TestCase(0)]
    [TestCase(1)]
    public void Unsigned16WriteLeavesPartialDestinationUntouched(int destinationLength)
    {
        var storage = new byte[] { 0xAA, 0xAA };

        var result = NetworkIntegerCodec.WriteUnsigned16(0x1234, storage.AsSpan(0, destinationLength));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            Assert.That(result.ProducedCount, Is.Zero);
            Assert.That(storage, Is.EqualTo(new byte[] { 0xAA, 0xAA }));
        });
    }

    [Test]
    public void Unsigned63WriteUsesNetworkOrderAndRejectsNegativeValues()
    {
        var destination = Enumerable.Repeat((byte)0xCC, 9).ToArray();
        var complete = NetworkIntegerCodec.WriteUnsigned63(0x0102030405060708L, destination);
        var rejected = NetworkIntegerCodec.WriteUnsigned63(-1, new byte[8]);

        Assert.Multiple(() =>
        {
            Assert.That(complete.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(complete.ProducedCount, Is.EqualTo(8));
            Assert.That(destination, Is.EqualTo(new byte[]
            {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0xCC
            }));
            Assert.That(rejected.Status, Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(rejected.ProducedCount, Is.Zero);
        });
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    public void Unsigned63WriteLeavesEveryPartialDestinationUntouched(int destinationLength)
    {
        var storage = Enumerable.Repeat((byte)0xAA, 8).ToArray();

        var result = NetworkIntegerCodec.WriteUnsigned63(65536, storage.AsSpan(0, destinationLength));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            Assert.That(result.ProducedCount, Is.Zero);
            Assert.That(storage, Is.EqualTo(Enumerable.Repeat((byte)0xAA, 8)));
        });
    }

    [Test]
    public void CanonicalWritesEnforceMinimalLengthBoundaries()
    {
        var short16 = NetworkIntegerCodec.WriteCanonicalPayloadLength16(125, new byte[2]);
        var valid16Destination = new byte[2];
        var valid16 = NetworkIntegerCodec.WriteCanonicalPayloadLength16(126, valid16Destination);
        var short63 = NetworkIntegerCodec.WriteCanonicalPayloadLength63(65535, new byte[8]);
        var valid63Destination = new byte[8];
        var valid63 = NetworkIntegerCodec.WriteCanonicalPayloadLength63(65536, valid63Destination);

        Assert.Multiple(() =>
        {
            Assert.That(short16.Status, Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(valid16.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(valid16Destination, Is.EqualTo(new byte[] { 0x00, 0x7E }));
            Assert.That(short63.Status, Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(valid63.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(valid63Destination, Is.EqualTo(new byte[] { 0, 0, 0, 0, 0, 1, 0, 0 }));
        });
    }

    [Test]
    public void GeneralUnsignedValuesRoundTripAtBoundaries()
    {
        var values16 = new[] { 0, 1, 125, 126, 65535 };
        foreach (var value in values16)
        {
            var destination = new byte[2];
            var write = NetworkIntegerCodec.WriteUnsigned16(value, destination);
            var read = NetworkIntegerCodec.ReadUnsigned16(destination);
            Assert.That(write.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(read.Value, Is.EqualTo(value));
        }

        var values63 = new[] { 0L, 1L, 65535L, 65536L, long.MaxValue };
        foreach (var value in values63)
        {
            var destination = new byte[8];
            var write = NetworkIntegerCodec.WriteUnsigned63(value, destination);
            var read = NetworkIntegerCodec.ReadUnsigned63(destination);
            Assert.That(write.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(read.Value, Is.EqualTo(value));
        }
    }
}
