using System;
using System.IO;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffBinaryReaderTests
{
    [Test]
    public void SpanReader_ReadsOneByteValues()
    {
        byte[] bytes = new byte[] { 0xFE };

        Assert.That(TiffBinaryReader.ReadByte(bytes), Is.EqualTo(254));
        Assert.That(TiffBinaryReader.ReadSByte(bytes), Is.EqualTo(-2));
    }

    [Test]
    public void SpanReader_ReadsLittleEndianIntegerValues()
    {
        byte[] bytes = new byte[]
        {
            0x34, 0x12,
            0x78, 0x56, 0x34, 0x12,
            0xF0, 0xDE, 0xBC, 0x9A, 0x78, 0x56, 0x34, 0x12
        };

        Assert.That(TiffBinaryReader.ReadUInt16(bytes, TiffByteOrder.LittleEndian), Is.EqualTo(0x1234));
        Assert.That(TiffBinaryReader.ReadInt16(new byte[] { 0xFE, 0xFF }, TiffByteOrder.LittleEndian), Is.EqualTo(-2));
        Assert.That(TiffBinaryReader.ReadUInt32(bytes.AsSpan(2), TiffByteOrder.LittleEndian), Is.EqualTo(0x12345678));
        Assert.That(TiffBinaryReader.ReadInt32(new byte[] { 0xFE, 0xFF, 0xFF, 0xFF }, TiffByteOrder.LittleEndian), Is.EqualTo(-2));
        Assert.That(TiffBinaryReader.ReadUInt64(bytes.AsSpan(6), TiffByteOrder.LittleEndian), Is.EqualTo(0x123456789ABCDEF0UL));
        Assert.That(TiffBinaryReader.ReadInt64(new byte[] { 0xFE, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, TiffByteOrder.LittleEndian), Is.EqualTo(-2));
    }

    [Test]
    public void SpanReader_ReadsBigEndianIntegerValues()
    {
        byte[] bytes = new byte[]
        {
            0x12, 0x34,
            0x12, 0x34, 0x56, 0x78,
            0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0
        };

        Assert.That(TiffBinaryReader.ReadUInt16(bytes, TiffByteOrder.BigEndian), Is.EqualTo(0x1234));
        Assert.That(TiffBinaryReader.ReadInt16(new byte[] { 0xFF, 0xFE }, TiffByteOrder.BigEndian), Is.EqualTo(-2));
        Assert.That(TiffBinaryReader.ReadUInt32(bytes.AsSpan(2), TiffByteOrder.BigEndian), Is.EqualTo(0x12345678));
        Assert.That(TiffBinaryReader.ReadInt32(new byte[] { 0xFF, 0xFF, 0xFF, 0xFE }, TiffByteOrder.BigEndian), Is.EqualTo(-2));
        Assert.That(TiffBinaryReader.ReadUInt64(bytes.AsSpan(6), TiffByteOrder.BigEndian), Is.EqualTo(0x123456789ABCDEF0UL));
        Assert.That(TiffBinaryReader.ReadInt64(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFE }, TiffByteOrder.BigEndian), Is.EqualTo(-2));
    }

    [Test]
    public void SpanReader_ReadsFloatingPointValues()
    {
        byte[] littleSingle = BitConverter.GetBytes(1.5f);
        byte[] littleDouble = BitConverter.GetBytes(1.25d);
        byte[] bigSingle = new byte[] { littleSingle[3], littleSingle[2], littleSingle[1], littleSingle[0] };
        byte[] bigDouble = new byte[] { littleDouble[7], littleDouble[6], littleDouble[5], littleDouble[4], littleDouble[3], littleDouble[2], littleDouble[1], littleDouble[0] };

        Assert.That(TiffBinaryReader.ReadSingle(littleSingle, TiffByteOrder.LittleEndian), Is.EqualTo(1.5f));
        Assert.That(TiffBinaryReader.ReadSingle(bigSingle, TiffByteOrder.BigEndian), Is.EqualTo(1.5f));
        Assert.That(TiffBinaryReader.ReadDouble(littleDouble, TiffByteOrder.LittleEndian), Is.EqualTo(1.25d));
        Assert.That(TiffBinaryReader.ReadDouble(bigDouble, TiffByteOrder.BigEndian), Is.EqualTo(1.25d));
    }

    [Test]
    public void SpanReader_RejectsUnknownByteOrder()
    {
        byte[] bytes = new byte[] { 0x12, 0x34 };

        Assert.Throws<ArgumentException>(() => TiffBinaryReader.ReadUInt16(bytes, TiffByteOrder.Unknown));
    }

    [Test]
    public void SpanReader_RejectsShortBuffers()
    {
        byte[] bytes = new byte[] { 0x12 };

        Assert.Throws<ArgumentException>(() => TiffBinaryReader.ReadUInt16(bytes, TiffByteOrder.BigEndian));
        Assert.Throws<ArgumentException>(() => TiffBinaryReader.ReadUInt32(bytes, TiffByteOrder.BigEndian));
        Assert.Throws<ArgumentException>(() => TiffBinaryReader.ReadUInt64(bytes, TiffByteOrder.BigEndian));
    }

    [Test]
    public void StreamReader_ReadsPrimitiveValuesAndAdvancesPosition()
    {
        byte[] bytes = new byte[]
        {
            0x7F,
            0x12, 0x34,
            0x12, 0x34, 0x56, 0x78,
            0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0
        };
        MemoryStream stream = new MemoryStream(bytes);

        Assert.That(TiffBinaryReader.ReadByte(stream), Is.EqualTo(0x7F));
        Assert.That(TiffBinaryReader.ReadUInt16(stream, TiffByteOrder.BigEndian), Is.EqualTo(0x1234));
        Assert.That(TiffBinaryReader.ReadUInt32(stream, TiffByteOrder.BigEndian), Is.EqualTo(0x12345678));
        Assert.That(TiffBinaryReader.ReadUInt64(stream, TiffByteOrder.BigEndian), Is.EqualTo(0x123456789ABCDEF0UL));
        Assert.That(stream.Position, Is.EqualTo(bytes.Length));
    }

    [Test]
    public void StreamReader_ReadsFloatingPointValues()
    {
        byte[] singleBytes = BitConverter.GetBytes(2.5f);
        byte[] doubleBytes = BitConverter.GetBytes(3.5d);
        MemoryStream singleStream = new MemoryStream(singleBytes);
        MemoryStream doubleStream = new MemoryStream(doubleBytes);

        Assert.That(TiffBinaryReader.ReadSingle(singleStream, TiffByteOrder.LittleEndian), Is.EqualTo(2.5f));
        Assert.That(TiffBinaryReader.ReadDouble(doubleStream, TiffByteOrder.LittleEndian), Is.EqualTo(3.5d));
    }

    [Test]
    public void StreamReader_ThrowsWhenStreamEndsBeforePrimitive()
    {
        MemoryStream stream = new MemoryStream(new byte[] { 0x12 });

        Assert.Throws<EndOfStreamException>(() => TiffBinaryReader.ReadUInt16(stream, TiffByteOrder.BigEndian));
    }

    [Test]
    public void ReadBytes_ReturnsExactLength()
    {
        MemoryStream stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });

        byte[] bytes = TiffBinaryReader.ReadBytes(stream, 3);

        Assert.That(bytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(stream.Position, Is.EqualTo(3));
    }
}
