using System;
using System.IO;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffBinaryWriterTests
{
    [Test]
    public void SpanWriter_WritesOneByteValues()
    {
        byte[] bytes = new byte[2];

        TiffBinaryWriter.WriteByte(bytes.AsSpan(0, 1), 0xFE);
        TiffBinaryWriter.WriteSByte(bytes.AsSpan(1, 1), -2);

        Assert.That(bytes, Is.EqualTo(new byte[] { 0xFE, 0xFE }));
    }

    [Test]
    public void SpanWriter_WritesLittleEndianIntegerValues()
    {
        byte[] bytes = new byte[14];

        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(0), 0x1234, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(2), 0x12345678, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt64(bytes.AsSpan(6), 0x123456789ABCDEF0UL, TiffByteOrder.LittleEndian);

        Assert.That(bytes, Is.EqualTo(new byte[]
        {
            0x34, 0x12,
            0x78, 0x56, 0x34, 0x12,
            0xF0, 0xDE, 0xBC, 0x9A, 0x78, 0x56, 0x34, 0x12
        }));
    }

    [Test]
    public void SpanWriter_WritesBigEndianIntegerValues()
    {
        byte[] bytes = new byte[14];

        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(0), 0x1234, TiffByteOrder.BigEndian);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(2), 0x12345678, TiffByteOrder.BigEndian);
        TiffBinaryWriter.WriteUInt64(bytes.AsSpan(6), 0x123456789ABCDEF0UL, TiffByteOrder.BigEndian);

        Assert.That(bytes, Is.EqualTo(new byte[]
        {
            0x12, 0x34,
            0x12, 0x34, 0x56, 0x78,
            0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0
        }));
    }

    [Test]
    public void SpanWriter_WritesSignedIntegerValues()
    {
        byte[] bytes = new byte[14];

        TiffBinaryWriter.WriteInt16(bytes.AsSpan(0), -2, TiffByteOrder.BigEndian);
        TiffBinaryWriter.WriteInt32(bytes.AsSpan(2), -2, TiffByteOrder.BigEndian);
        TiffBinaryWriter.WriteInt64(bytes.AsSpan(6), -2, TiffByteOrder.BigEndian);

        Assert.That(bytes, Is.EqualTo(new byte[]
        {
            0xFF, 0xFE,
            0xFF, 0xFF, 0xFF, 0xFE,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFE
        }));
    }

    [Test]
    public void SpanWriter_WritesFloatingPointValues()
    {
        byte[] littleSingle = new byte[4];
        byte[] bigSingle = new byte[4];
        byte[] littleDouble = new byte[8];
        byte[] bigDouble = new byte[8];

        TiffBinaryWriter.WriteSingle(littleSingle, 1.5f, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteSingle(bigSingle, 1.5f, TiffByteOrder.BigEndian);
        TiffBinaryWriter.WriteDouble(littleDouble, 1.25d, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteDouble(bigDouble, 1.25d, TiffByteOrder.BigEndian);

        Assert.That(TiffBinaryReader.ReadSingle(littleSingle, TiffByteOrder.LittleEndian), Is.EqualTo(1.5f));
        Assert.That(TiffBinaryReader.ReadSingle(bigSingle, TiffByteOrder.BigEndian), Is.EqualTo(1.5f));
        Assert.That(TiffBinaryReader.ReadDouble(littleDouble, TiffByteOrder.LittleEndian), Is.EqualTo(1.25d));
        Assert.That(TiffBinaryReader.ReadDouble(bigDouble, TiffByteOrder.BigEndian), Is.EqualTo(1.25d));
    }

    [Test]
    public void SpanWriter_RejectsUnknownByteOrder()
    {
        byte[] bytes = new byte[2];

        Assert.Throws<ArgumentException>(() => TiffBinaryWriter.WriteUInt16(bytes, 0x1234, TiffByteOrder.Unknown));
    }

    [Test]
    public void SpanWriter_RejectsShortBuffers()
    {
        byte[] bytes = new byte[1];

        Assert.Throws<ArgumentException>(() => TiffBinaryWriter.WriteUInt16(bytes, 0x1234, TiffByteOrder.BigEndian));
        Assert.Throws<ArgumentException>(() => TiffBinaryWriter.WriteUInt32(bytes, 0x12345678, TiffByteOrder.BigEndian));
        Assert.Throws<ArgumentException>(() => TiffBinaryWriter.WriteUInt64(bytes, 0x123456789ABCDEF0UL, TiffByteOrder.BigEndian));
    }

    [Test]
    public void StreamWriter_WritesPrimitiveValuesAndAdvancesPosition()
    {
        MemoryStream stream = new MemoryStream();

        TiffBinaryWriter.WriteByte(stream, 0x7F);
        TiffBinaryWriter.WriteUInt16(stream, 0x1234, TiffByteOrder.BigEndian);
        TiffBinaryWriter.WriteUInt32(stream, 0x12345678, TiffByteOrder.BigEndian);
        TiffBinaryWriter.WriteUInt64(stream, 0x123456789ABCDEF0UL, TiffByteOrder.BigEndian);

        Assert.That(stream.Position, Is.EqualTo(15));
        Assert.That(stream.ToArray(), Is.EqualTo(new byte[]
        {
            0x7F,
            0x12, 0x34,
            0x12, 0x34, 0x56, 0x78,
            0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0
        }));
    }

    [Test]
    public void StreamWriter_WritesFloatingPointValues()
    {
        MemoryStream stream = new MemoryStream();

        TiffBinaryWriter.WriteSingle(stream, 2.5f, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteDouble(stream, 3.5d, TiffByteOrder.LittleEndian);

        stream.Position = 0;
        Assert.That(TiffBinaryReader.ReadSingle(stream, TiffByteOrder.LittleEndian), Is.EqualTo(2.5f));
        Assert.That(TiffBinaryReader.ReadDouble(stream, TiffByteOrder.LittleEndian), Is.EqualTo(3.5d));
    }

    [Test]
    public void WriteBytes_WritesExactSourceBytes()
    {
        MemoryStream stream = new MemoryStream();

        TiffBinaryWriter.WriteBytes(stream, new byte[] { 1, 2, 3 });

        Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3 }));
    }
}
