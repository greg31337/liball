namespace avif.Tests;

public class IntegerByteIoTests
{
    [Test]
    public void SignedAndUnsignedIntegersRoundTripAtBoundaries()
    {
        byte[] buffer = new byte[96];
        ByteWriter writer = new ByteWriter(buffer.Length);

        writer.WriteUInt8(buffer, 165);
        writer.WriteInt8(buffer, -2);
        writer.WriteUInt16BigEndian(buffer, 4660);
        writer.WriteUInt16LittleEndian(buffer, 22136);
        writer.WriteInt16BigEndian(buffer, -1234);
        writer.WriteInt16LittleEndian(buffer, -2345);
        writer.WriteUInt24BigEndian(buffer, 11259375);
        writer.WriteUInt24LittleEndian(buffer, 1193046);
        writer.WriteInt24BigEndian(buffer, -8388608);
        writer.WriteInt24LittleEndian(buffer, 8388607);
        writer.WriteUInt32BigEndian(buffer, 2309737967);
        writer.WriteUInt32LittleEndian(buffer, 19088743);
        writer.WriteInt32BigEndian(buffer, int.MinValue);
        writer.WriteInt32LittleEndian(buffer, -1);
        writer.WriteUInt64BigEndian(buffer, ulong.MaxValue);
        writer.WriteUInt64LittleEndian(buffer, 81985529216486895);
        writer.WriteInt64BigEndian(buffer, long.MinValue);
        writer.WriteInt64LittleEndian(buffer, -1);

        ByteReader reader = new ByteReader(writer.Position);
        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadUInt8(buffer), Is.EqualTo(165));
            Assert.That(reader.ReadInt8(buffer), Is.EqualTo(-2));
            Assert.That(reader.ReadUInt16BigEndian(buffer), Is.EqualTo(4660));
            Assert.That(reader.ReadUInt16LittleEndian(buffer), Is.EqualTo(22136));
            Assert.That(reader.ReadInt16BigEndian(buffer), Is.EqualTo(-1234));
            Assert.That(reader.ReadInt16LittleEndian(buffer), Is.EqualTo(-2345));
            Assert.That(reader.ReadUInt24BigEndian(buffer), Is.EqualTo(11259375));
            Assert.That(reader.ReadUInt24LittleEndian(buffer), Is.EqualTo(1193046));
            Assert.That(reader.ReadInt24BigEndian(buffer), Is.EqualTo(-8388608));
            Assert.That(reader.ReadInt24LittleEndian(buffer), Is.EqualTo(8388607));
            Assert.That(reader.ReadUInt32BigEndian(buffer), Is.EqualTo(2309737967));
            Assert.That(reader.ReadUInt32LittleEndian(buffer), Is.EqualTo(19088743));
            Assert.That(reader.ReadInt32BigEndian(buffer), Is.EqualTo(int.MinValue));
            Assert.That(reader.ReadInt32LittleEndian(buffer), Is.EqualTo(-1));
            Assert.That(reader.ReadUInt64BigEndian(buffer), Is.EqualTo(ulong.MaxValue));
            Assert.That(reader.ReadUInt64LittleEndian(buffer), Is.EqualTo(81985529216486895));
            Assert.That(reader.ReadInt64BigEndian(buffer), Is.EqualTo(long.MinValue));
            Assert.That(reader.ReadInt64LittleEndian(buffer), Is.EqualTo(-1));
            Assert.That(reader.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(reader.Remaining(), Is.Zero);
        });
    }

    [Test]
    public void IntegerWritersProduceExpectedEndianBytes()
    {
        byte[] buffer = new byte[17];
        ByteWriter writer = new ByteWriter(buffer.Length);

        writer.WriteUInt16BigEndian(buffer, 4660);
        writer.WriteUInt24LittleEndian(buffer, 10597059);
        writer.WriteUInt32BigEndian(buffer, 2309737967);
        writer.WriteUInt64LittleEndian(buffer, 72623859790382856);

        Assert.That(buffer, Is.EqualTo(new byte[]
        {
            18, 52,
            195, 178, 161,
            137, 171, 205, 239,
            8, 7, 6, 5, 4, 3, 2, 1
        }));
    }

    [Test]
    public void ReaderBoundsFailureIsAtomicAndSticky()
    {
        byte[] source = [1, 2, 3];
        ByteReader reader = new ByteReader(4);

        uint value = reader.ReadUInt32BigEndian(source);
        byte laterValue = reader.ReadUInt8(source);

        Assert.Multiple(() =>
        {
            Assert.That(value, Is.Zero);
            Assert.That(laterValue, Is.Zero);
            Assert.That(reader.Position, Is.Zero);
            Assert.That(reader.Code, Is.EqualTo(AvifResultCode.TruncatedData));
        });
    }

    [Test]
    public void WriterBoundsFailureIsAtomicAndSticky()
    {
        byte[] destination = [170, 170, 170];
        ByteWriter writer = new ByteWriter(4);

        AvifResultCode first = writer.WriteUInt32BigEndian(destination, 305419896);
        AvifResultCode second = writer.WriteUInt8(destination, 1);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(AvifResultCode.BufferTooSmall));
            Assert.That(second, Is.EqualTo(AvifResultCode.BufferTooSmall));
            Assert.That(writer.Position, Is.Zero);
            Assert.That(destination, Is.EqualTo(new byte[] { 170, 170, 170 }));
        });
    }

    [Test]
    public void LogicalLimitIsEnforcedEvenWhenSpanIsLarger()
    {
        byte[] source = [1, 2, 3, 4];
        byte[] destination = new byte[4];
        ByteReader reader = new ByteReader(3);
        ByteWriter writer = new ByteWriter(3);

        uint readValue = reader.ReadUInt32BigEndian(source);
        AvifResultCode writeCode = writer.WriteUInt32BigEndian(destination, 16909060);

        Assert.Multiple(() =>
        {
            Assert.That(readValue, Is.Zero);
            Assert.That(reader.Code, Is.EqualTo(AvifResultCode.TruncatedData));
            Assert.That(writeCode, Is.EqualTo(AvifResultCode.BufferTooSmall));
            Assert.That(destination, Is.EqualTo(new byte[4]));
        });
    }

    [Test]
    public void InvalidTwentyFourBitValueDoesNotWrite()
    {
        byte[] destination = [204, 204, 204];
        ByteWriter writer = new ByteWriter(destination.Length);

        AvifResultCode code = writer.WriteInt24BigEndian(destination, 8388608);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(writer.Position, Is.Zero);
            Assert.That(destination, Is.EqualTo(new byte[] { 204, 204, 204 }));
        });
    }
}
