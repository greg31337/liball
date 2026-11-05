using websocket;

namespace websocket.Tests;

public class DeflateBlockHeaderTests
{
    [Test]
    public void StoredHeaderAlignsAfterBlockBitsAndReadsIncrementally()
    {
        var bitReader = new DeflateBitReader();
        var blockReader = new DeflateBlockHeaderReader(bitReader);
        var storedReader = new DeflateStoredBlockHeaderReader(bitReader);

        var block = blockReader.ReadBlockHeader(new byte[] { 0x01 });
        var first = storedReader.ReadStoredBlockHeader(new byte[] { 0x03 });
        var second = storedReader.ReadStoredBlockHeader(new byte[] { 0x00 });
        var third = storedReader.ReadStoredBlockHeader(new byte[] { 0xfc });
        var fourth = storedReader.ReadStoredBlockHeader(new byte[] { 0xff });

        Assert.Multiple(() =>
        {
            Assert.That(block.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(block.FinalBlock, Is.True);
            Assert.That(block.BlockType, Is.Zero);
            Assert.That(first.Status, Is.EqualTo(WebSocketParseStatus.NeedInput));
            Assert.That(second.Status, Is.EqualTo(WebSocketParseStatus.NeedInput));
            Assert.That(third.Status, Is.EqualTo(WebSocketParseStatus.NeedInput));
            Assert.That(fourth.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(fourth.Length, Is.EqualTo(3));
        });
    }

    [Test]
    public void ReservedBlockTypeAndWrongStoredComplementAreRejected()
    {
        var reservedReader = new DeflateBitReader();
        var reserved = new DeflateBlockHeaderReader(reservedReader)
            .ReadBlockHeader(new byte[] { 0x07 });
        var storedBits = new DeflateBitReader();
        var storedHeader = new DeflateBlockHeaderReader(storedBits);
        var storedReader = new DeflateStoredBlockHeaderReader(storedBits);
        storedHeader.ReadBlockHeader(new byte[] { 0x00 });

        var invalid = storedReader.ReadStoredBlockHeader(
            new byte[] { 0x03, 0x00, 0x03, 0x00 });
        var repeated = storedReader.ReadStoredBlockHeader([]);

        Assert.Multiple(() =>
        {
            Assert.That(reserved.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(reserved.BlockType, Is.EqualTo(3));
            Assert.That(invalid.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(invalid.ConsumedCount, Is.EqualTo(4));
            Assert.That(repeated.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
        });
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(65535)]
    public void StoredHeaderWriterUsesLittleEndianOnesComplement(int length)
    {
        var destination = new byte[4];

        var write = DeflateStoredBlockHeaderWriter.WriteStoredBlockHeader(
            length,
            destination);

        int decodedLength = destination[0] | (destination[1] << 8);
        int decodedComplement = destination[2] | (destination[3] << 8);
        Assert.Multiple(() =>
        {
            Assert.That(write.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(write.ProducedCount, Is.EqualTo(4));
            Assert.That(decodedLength, Is.EqualTo(length));
            Assert.That((decodedLength ^ decodedComplement) & 65535,
                Is.EqualTo(65535));
        });
    }

    [Test]
    public void StoredHeaderWriterRequiresCompleteCapacityAndValidLength()
    {
        var shortDestination = new byte[] { 1, 2, 3 };

        var shortWrite = DeflateStoredBlockHeaderWriter.WriteStoredBlockHeader(
            1,
            shortDestination);
        var invalid = DeflateStoredBlockHeaderWriter.WriteStoredBlockHeader(
            65536,
            new byte[4]);

        Assert.Multiple(() =>
        {
            Assert.That(shortWrite.Status,
                Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            Assert.That(shortWrite.ProducedCount, Is.Zero);
            Assert.That(shortDestination, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(invalid.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
        });
    }
}
