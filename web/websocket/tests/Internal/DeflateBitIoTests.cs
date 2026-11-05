using websocket;

namespace websocket.Tests;

public class DeflateBitIoTests
{
    [Test]
    public void ReaderPreservesLsbFirstBitsAcrossInputBoundaries()
    {
        var reader = new DeflateBitReader();

        var first = reader.ReadBits(new byte[] { 0xca }, 3);
        var incomplete = reader.ReadBits([], 7);
        var second = reader.ReadBits(new byte[] { 0x35 }, 7);
        var remaining = reader.ReadBits([], 6);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(first.ConsumedCount, Is.EqualTo(1));
            Assert.That(first.Value, Is.EqualTo(2));
            Assert.That(incomplete.Status, Is.EqualTo(WebSocketParseStatus.NeedInput));
            Assert.That(incomplete.ConsumedCount, Is.Zero);
            Assert.That(second.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(second.ConsumedCount, Is.EqualTo(1));
            Assert.That(second.Value, Is.EqualTo(57));
            Assert.That(remaining.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(remaining.Value, Is.EqualTo(13));
            Assert.That(reader.GetBufferedBitCount(), Is.Zero);
        });
    }

    [Test]
    public void WriterPacksFixedValuesLsbFirstAndHuffmanCodesMsbFirst()
    {
        var writer = new DeflateBitWriter();
        var destination = new byte[1];

        var fixedBits = writer.WriteBits(5, 3, []);
        var huffman = writer.WriteHuffmanCode(2, 2, []);
        var finished = writer.FinishWriterByte(destination);

        Assert.Multiple(() =>
        {
            Assert.That(fixedBits.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(fixedBits.BitsAccepted, Is.True);
            Assert.That(huffman.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(finished.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(finished.ProducedCount, Is.EqualTo(1));
            Assert.That(destination[0], Is.EqualTo(0x0d));
            Assert.That(writer.GetPendingBitCount(), Is.Zero);
        });
    }

    [Test]
    public void WriterRetainsWholeBytesUntilOutputCapacityReturns()
    {
        var writer = new DeflateBitWriter();
        var first = new byte[1];
        var second = new byte[1];
        var third = new byte[1];

        var write = writer.WriteBits(0xabcdef, 24, first);
        var drainSecond = writer.DrainWriterBytes(second);
        var drainThird = writer.DrainWriterBytes(third);

        Assert.Multiple(() =>
        {
            Assert.That(write.Status,
                Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            Assert.That(write.BitsAccepted, Is.True);
            Assert.That(first[0], Is.EqualTo(0xef));
            Assert.That(drainSecond.Status,
                Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            Assert.That(second[0], Is.EqualTo(0xcd));
            Assert.That(drainThird.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(third[0], Is.EqualTo(0xab));
            Assert.That(writer.GetPendingBitCount(), Is.Zero);
        });
    }

    [Test]
    public void FinishRetainsPartialByteAfterDrainingQueuedWholeBytes()
    {
        var writer = new DeflateBitWriter();
        var first = new byte[2];
        var last = new byte[1];
        writer.WriteBits(0x12345, 17, []);

        var finish = writer.FinishWriterByte(first);
        var drain = writer.FinishWriterByte(last);

        Assert.Multiple(() =>
        {
            Assert.That(finish.Status,
                Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            Assert.That(finish.ProducedCount, Is.EqualTo(2));
            Assert.That(first, Is.EqualTo(new byte[] { 0x45, 0x23 }));
            Assert.That(drain.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(drain.ProducedCount, Is.EqualTo(1));
            Assert.That(last[0], Is.EqualTo(0x01));
            Assert.That(writer.GetPendingBitCount(), Is.Zero);
        });
    }

    [Test]
    public void BitIoRejectsInvalidWidthsAndValuesWithoutMutation()
    {
        var reader = new DeflateBitReader();
        var writer = new DeflateBitWriter();

        var read = reader.ReadBits(new byte[] { 1 }, 25);
        var negative = writer.WriteBits(-1, 3, new byte[1]);
        var oversized = writer.WriteBits(8, 3, new byte[1]);
        var invalidHuffman = writer.WriteHuffmanCode(4, 2, new byte[1]);

        Assert.Multiple(() =>
        {
            Assert.That(read.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(read.ConsumedCount, Is.Zero);
            Assert.That(negative.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(negative.BitsAccepted, Is.False);
            Assert.That(oversized.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(invalidHuffman.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(writer.GetPendingBitCount(), Is.Zero);
        });
    }
}
