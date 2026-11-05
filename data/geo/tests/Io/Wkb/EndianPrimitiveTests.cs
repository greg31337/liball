using geo;
using System.IO;

namespace geo.Tests;

public class EndianPrimitiveTests
{
    [Test]
    public void UInt32SpanPrimitivesHonorBothByteOrdersAndOffsets()
    {
        byte[] big = Enumerable.Repeat((byte)0xcc, 8).ToArray();
        byte[] little = Enumerable.Repeat((byte)0xcc, 8).ToArray();

        EndianWriter.WriteUInt32ToSpan(big.AsSpan(), 2, 0x12345678u, ByteOrder.BigEndian);
        EndianWriter.WriteUInt32ToSpan(little.AsSpan(), 2, 0x12345678u, ByteOrder.LittleEndian);
        uint bigValue = EndianReader.ReadUInt32FromSpan(big.AsSpan(), 2, ByteOrder.BigEndian);
        uint littleValue = EndianReader.ReadUInt32FromSpan(little.AsSpan(), 2, ByteOrder.LittleEndian);

        Assert.Multiple(() =>
        {
            Assert.That(big, Is.EqualTo(new byte[] { 0xcc, 0xcc, 0x12, 0x34, 0x56, 0x78, 0xcc, 0xcc }));
            Assert.That(little, Is.EqualTo(new byte[] { 0xcc, 0xcc, 0x78, 0x56, 0x34, 0x12, 0xcc, 0xcc }));
            Assert.That(bigValue, Is.EqualTo(0x12345678u));
            Assert.That(littleValue, Is.EqualTo(0x12345678u));
        });
    }

    [Test]
    public void DoubleSpanPrimitivesPreserveAllIeeeBits()
    {
        long[] bitPatterns =
        [
            BitConverter.DoubleToInt64Bits(-123.25),
            BitConverter.DoubleToInt64Bits(-0.0),
            0x7ff8000000001234
        ];

        foreach (long expectedBits in bitPatterns)
        {
            double value = BitConverter.Int64BitsToDouble(expectedBits);
            byte[] big = new byte[8];
            byte[] little = new byte[8];
            EndianWriter.WriteDoubleToSpan(big.AsSpan(), 0, value, ByteOrder.BigEndian);
            EndianWriter.WriteDoubleToSpan(little.AsSpan(), 0, value, ByteOrder.LittleEndian);

            double bigValue = EndianReader.ReadDoubleFromSpan(big.AsSpan(), 0, ByteOrder.BigEndian);
            double littleValue = EndianReader.ReadDoubleFromSpan(little.AsSpan(), 0, ByteOrder.LittleEndian);

            Assert.Multiple(() =>
            {
                Assert.That(BitConverter.DoubleToInt64Bits(bigValue), Is.EqualTo(expectedBits));
                Assert.That(BitConverter.DoubleToInt64Bits(littleValue), Is.EqualTo(expectedBits));
                Assert.That(little, Is.EqualTo(big.Reverse().ToArray()));
            });
        }
    }

    [Test]
    public void ArrayPrimitivesReadAndWriteBytesIntegersAndDoubles()
    {
        byte[] buffer = Enumerable.Repeat((byte)0xee, 16).ToArray();

        EndianWriter.WriteByteToArray(buffer, 1, 0x01);
        EndianWriter.WriteUInt32ToArray(buffer, 2, 0x89abcdefu, ByteOrder.BigEndian);
        EndianWriter.WriteDoubleToArray(buffer, 6, 1.0, ByteOrder.BigEndian);

        Assert.Multiple(() =>
        {
            Assert.That(EndianReader.ReadByteFromArray(buffer, 1), Is.EqualTo(0x01));
            Assert.That(EndianReader.ReadUInt32FromArray(buffer, 2, ByteOrder.BigEndian), Is.EqualTo(0x89abcdefu));
            Assert.That(EndianReader.ReadDoubleFromArray(buffer, 6, ByteOrder.BigEndian), Is.EqualTo(1.0));
            Assert.That(buffer.Skip(6).Take(8), Is.EqualTo(new byte[] { 0x3f, 0xf0, 0, 0, 0, 0, 0, 0 }));
            Assert.That(buffer[0], Is.EqualTo(0xee));
            Assert.That(buffer[14], Is.EqualTo(0xee));
        });
    }

    [Test]
    public void StreamPrimitivesAreSequentialAndLeaveTheStreamOpen()
    {
        MemoryStream stream = new MemoryStream();

        EndianWriter.WriteByteToStream(stream, 0x01);
        EndianWriter.WriteUInt32ToStream(stream, 0x01020304u, ByteOrder.LittleEndian);
        EndianWriter.WriteDoubleToStream(stream, -2.5, ByteOrder.BigEndian);
        stream.Position = 0;

        byte marker = EndianReader.ReadByteFromStream(stream);
        uint integer = EndianReader.ReadUInt32FromStream(stream, ByteOrder.LittleEndian);
        double number = EndianReader.ReadDoubleFromStream(stream, ByteOrder.BigEndian);
        stream.WriteByte(0xff);

        Assert.Multiple(() =>
        {
            Assert.That(marker, Is.EqualTo(0x01));
            Assert.That(integer, Is.EqualTo(0x01020304u));
            Assert.That(number, Is.EqualTo(-2.5));
            Assert.That(stream.Length, Is.EqualTo(14));
        });
    }

    [Test]
    public void StreamReaderCompletesShortSuccessfulReads()
    {
        ChunkedReadStream stream = new ChunkedReadStream(
            new byte[] { 0x78, 0x56, 0x34, 0x12, 0, 0, 0, 0, 0, 0, 0xf0, 0x3f });

        uint integer = EndianReader.ReadUInt32FromStream(stream, ByteOrder.LittleEndian);
        double number = EndianReader.ReadDoubleFromStream(stream, ByteOrder.LittleEndian);

        Assert.Multiple(() =>
        {
            Assert.That(integer, Is.EqualTo(0x12345678u));
            Assert.That(number, Is.EqualTo(1.0));
            Assert.That(stream.Position, Is.EqualTo(stream.Length));
        });
    }

    [Test]
    public void SpanBoundsAreCheckedBeforeReadingOrWriting()
    {
        byte[] source = { 1, 2, 3 };
        byte[] destination = Enumerable.Repeat((byte)0xaa, 7).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(EndianReader.HasBytes(source.AsSpan(), 0, 3), Is.True);
            Assert.That(EndianReader.HasBytes(source.AsSpan(), 3, 0), Is.True);
            Assert.That(EndianReader.HasBytes(source.AsSpan(), -1, 1), Is.False);
            Assert.That(EndianReader.HasBytes(source.AsSpan(), 0, 4), Is.False);
            Assert.That(EndianWriter.HasBytes(destination.AsSpan(), 0, 8), Is.False);
            Assert.Throws<GeometryParseException>(() =>
                EndianReader.ReadUInt32FromSpan(source.AsSpan(), 0, ByteOrder.BigEndian));
            Assert.Throws<GeometryParseException>(() =>
                EndianReader.ReadByteFromSpan(source.AsSpan(), -1));
            Assert.Throws<InvalidGeometryException>(() =>
                EndianWriter.WriteDoubleToSpan(destination.AsSpan(), 0, 1.0, ByteOrder.BigEndian));
            Assert.That(destination, Is.All.EqualTo(0xaa));
        });
    }

    [Test]
    public void InvalidOrdersNullArraysAndTruncatedStreamsFailDeterministically()
    {
        ByteOrder invalid = (ByteOrder)99;
        byte[] source = new byte[8];
        byte[] destination = new byte[8];
        MemoryStream truncated = new MemoryStream(new byte[] { 1, 2, 3 });
        MemoryStream readOnly = new MemoryStream(new byte[8], false);

        Assert.Multiple(() =>
        {
            Assert.Throws<GeometryParseException>(() =>
                EndianReader.ReadUInt32FromSpan(source.AsSpan(), 0, invalid));
            Assert.Throws<InvalidGeometryException>(() =>
                EndianWriter.WriteUInt32ToSpan(destination.AsSpan(), 0, 1u, invalid));
            Assert.Throws<GeometryParseException>(() => EndianReader.ReadByteFromArray(null!, 0));
            Assert.Throws<InvalidGeometryException>(() => EndianWriter.WriteByteToArray(null!, 0, 1));
            Assert.Throws<GeometryParseException>(() =>
                EndianReader.ReadUInt32FromStream(truncated, ByteOrder.BigEndian));
            Assert.Throws<GeometryParseException>(() => EndianReader.ReadByteFromStream(null!));
            Assert.Throws<InvalidGeometryException>(() => EndianWriter.WriteByteToStream(null!, 1));
            Assert.Throws<InvalidGeometryException>(() => EndianWriter.WriteByteToStream(readOnly, 1));
        });
    }

    private sealed class ChunkedReadStream : MemoryStream
    {
        public ChunkedReadStream(byte[] source) : base(source)
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return base.Read(buffer, offset, Math.Min(count, 1));
        }
    }
}
