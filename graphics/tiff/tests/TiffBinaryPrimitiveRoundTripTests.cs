using System.IO;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffBinaryPrimitiveRoundTripTests
{
    [Test]
    public void SpanPrimitives_RoundTripLittleEndianValues()
    {
        byte[] bytes = new byte[42];
        int offset = 0;

        TiffBinaryWriter.WriteByte(bytes.AsSpan(offset, 1), 0x7F);
        offset = offset + 1;
        TiffBinaryWriter.WriteSByte(bytes.AsSpan(offset, 1), -7);
        offset = offset + 1;
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset, 2), 0x1234, TiffByteOrder.LittleEndian);
        offset = offset + 2;
        TiffBinaryWriter.WriteInt16(bytes.AsSpan(offset, 2), -1234, TiffByteOrder.LittleEndian);
        offset = offset + 2;
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(offset, 4), 0x89ABCDEFU, TiffByteOrder.LittleEndian);
        offset = offset + 4;
        TiffBinaryWriter.WriteInt32(bytes.AsSpan(offset, 4), -12345678, TiffByteOrder.LittleEndian);
        offset = offset + 4;
        TiffBinaryWriter.WriteUInt64(bytes.AsSpan(offset, 8), 0x0123456789ABCDEFUL, TiffByteOrder.LittleEndian);
        offset = offset + 8;
        TiffBinaryWriter.WriteInt64(bytes.AsSpan(offset, 8), -1234567890123456789L, TiffByteOrder.LittleEndian);
        offset = offset + 8;
        TiffBinaryWriter.WriteSingle(bytes.AsSpan(offset, 4), -12.5f, TiffByteOrder.LittleEndian);
        offset = offset + 4;
        TiffBinaryWriter.WriteDouble(bytes.AsSpan(offset, 8), 12345.25d, TiffByteOrder.LittleEndian);

        offset = 0;
        Assert.That(TiffBinaryReader.ReadByte(bytes.AsSpan(offset, 1)), Is.EqualTo(0x7F));
        offset = offset + 1;
        Assert.That(TiffBinaryReader.ReadSByte(bytes.AsSpan(offset, 1)), Is.EqualTo(-7));
        offset = offset + 1;
        Assert.That(TiffBinaryReader.ReadUInt16(bytes.AsSpan(offset, 2), TiffByteOrder.LittleEndian), Is.EqualTo(0x1234));
        offset = offset + 2;
        Assert.That(TiffBinaryReader.ReadInt16(bytes.AsSpan(offset, 2), TiffByteOrder.LittleEndian), Is.EqualTo(-1234));
        offset = offset + 2;
        Assert.That(TiffBinaryReader.ReadUInt32(bytes.AsSpan(offset, 4), TiffByteOrder.LittleEndian), Is.EqualTo(0x89ABCDEFU));
        offset = offset + 4;
        Assert.That(TiffBinaryReader.ReadInt32(bytes.AsSpan(offset, 4), TiffByteOrder.LittleEndian), Is.EqualTo(-12345678));
        offset = offset + 4;
        Assert.That(TiffBinaryReader.ReadUInt64(bytes.AsSpan(offset, 8), TiffByteOrder.LittleEndian), Is.EqualTo(0x0123456789ABCDEFUL));
        offset = offset + 8;
        Assert.That(TiffBinaryReader.ReadInt64(bytes.AsSpan(offset, 8), TiffByteOrder.LittleEndian), Is.EqualTo(-1234567890123456789L));
        offset = offset + 8;
        Assert.That(TiffBinaryReader.ReadSingle(bytes.AsSpan(offset, 4), TiffByteOrder.LittleEndian), Is.EqualTo(-12.5f));
        offset = offset + 4;
        Assert.That(TiffBinaryReader.ReadDouble(bytes.AsSpan(offset, 8), TiffByteOrder.LittleEndian), Is.EqualTo(12345.25d));
    }

    [Test]
    public void SpanPrimitives_RoundTripBigEndianValues()
    {
        byte[] bytes = new byte[42];
        int offset = 0;

        TiffBinaryWriter.WriteByte(bytes.AsSpan(offset, 1), 0x7F);
        offset = offset + 1;
        TiffBinaryWriter.WriteSByte(bytes.AsSpan(offset, 1), -7);
        offset = offset + 1;
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset, 2), 0x1234, TiffByteOrder.BigEndian);
        offset = offset + 2;
        TiffBinaryWriter.WriteInt16(bytes.AsSpan(offset, 2), -1234, TiffByteOrder.BigEndian);
        offset = offset + 2;
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(offset, 4), 0x89ABCDEFU, TiffByteOrder.BigEndian);
        offset = offset + 4;
        TiffBinaryWriter.WriteInt32(bytes.AsSpan(offset, 4), -12345678, TiffByteOrder.BigEndian);
        offset = offset + 4;
        TiffBinaryWriter.WriteUInt64(bytes.AsSpan(offset, 8), 0x0123456789ABCDEFUL, TiffByteOrder.BigEndian);
        offset = offset + 8;
        TiffBinaryWriter.WriteInt64(bytes.AsSpan(offset, 8), -1234567890123456789L, TiffByteOrder.BigEndian);
        offset = offset + 8;
        TiffBinaryWriter.WriteSingle(bytes.AsSpan(offset, 4), -12.5f, TiffByteOrder.BigEndian);
        offset = offset + 4;
        TiffBinaryWriter.WriteDouble(bytes.AsSpan(offset, 8), 12345.25d, TiffByteOrder.BigEndian);

        offset = 0;
        Assert.That(TiffBinaryReader.ReadByte(bytes.AsSpan(offset, 1)), Is.EqualTo(0x7F));
        offset = offset + 1;
        Assert.That(TiffBinaryReader.ReadSByte(bytes.AsSpan(offset, 1)), Is.EqualTo(-7));
        offset = offset + 1;
        Assert.That(TiffBinaryReader.ReadUInt16(bytes.AsSpan(offset, 2), TiffByteOrder.BigEndian), Is.EqualTo(0x1234));
        offset = offset + 2;
        Assert.That(TiffBinaryReader.ReadInt16(bytes.AsSpan(offset, 2), TiffByteOrder.BigEndian), Is.EqualTo(-1234));
        offset = offset + 2;
        Assert.That(TiffBinaryReader.ReadUInt32(bytes.AsSpan(offset, 4), TiffByteOrder.BigEndian), Is.EqualTo(0x89ABCDEFU));
        offset = offset + 4;
        Assert.That(TiffBinaryReader.ReadInt32(bytes.AsSpan(offset, 4), TiffByteOrder.BigEndian), Is.EqualTo(-12345678));
        offset = offset + 4;
        Assert.That(TiffBinaryReader.ReadUInt64(bytes.AsSpan(offset, 8), TiffByteOrder.BigEndian), Is.EqualTo(0x0123456789ABCDEFUL));
        offset = offset + 8;
        Assert.That(TiffBinaryReader.ReadInt64(bytes.AsSpan(offset, 8), TiffByteOrder.BigEndian), Is.EqualTo(-1234567890123456789L));
        offset = offset + 8;
        Assert.That(TiffBinaryReader.ReadSingle(bytes.AsSpan(offset, 4), TiffByteOrder.BigEndian), Is.EqualTo(-12.5f));
        offset = offset + 4;
        Assert.That(TiffBinaryReader.ReadDouble(bytes.AsSpan(offset, 8), TiffByteOrder.BigEndian), Is.EqualTo(12345.25d));
    }

    [Test]
    public void StreamPrimitives_RoundTripSequentialLittleEndianValues()
    {
        MemoryStream stream = new MemoryStream();

        TiffBinaryWriter.WriteByte(stream, 0x7F);
        TiffBinaryWriter.WriteSByte(stream, -7);
        TiffBinaryWriter.WriteUInt16(stream, 0x1234, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteInt16(stream, -1234, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt32(stream, 0x89ABCDEFU, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteInt32(stream, -12345678, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt64(stream, 0x0123456789ABCDEFUL, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteInt64(stream, -1234567890123456789L, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteSingle(stream, -12.5f, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteDouble(stream, 12345.25d, TiffByteOrder.LittleEndian);

        Assert.That(stream.Position, Is.EqualTo(42));
        stream.Position = 0;
        Assert.That(TiffBinaryReader.ReadByte(stream), Is.EqualTo(0x7F));
        Assert.That(TiffBinaryReader.ReadSByte(stream), Is.EqualTo(-7));
        Assert.That(TiffBinaryReader.ReadUInt16(stream, TiffByteOrder.LittleEndian), Is.EqualTo(0x1234));
        Assert.That(TiffBinaryReader.ReadInt16(stream, TiffByteOrder.LittleEndian), Is.EqualTo(-1234));
        Assert.That(TiffBinaryReader.ReadUInt32(stream, TiffByteOrder.LittleEndian), Is.EqualTo(0x89ABCDEFU));
        Assert.That(TiffBinaryReader.ReadInt32(stream, TiffByteOrder.LittleEndian), Is.EqualTo(-12345678));
        Assert.That(TiffBinaryReader.ReadUInt64(stream, TiffByteOrder.LittleEndian), Is.EqualTo(0x0123456789ABCDEFUL));
        Assert.That(TiffBinaryReader.ReadInt64(stream, TiffByteOrder.LittleEndian), Is.EqualTo(-1234567890123456789L));
        Assert.That(TiffBinaryReader.ReadSingle(stream, TiffByteOrder.LittleEndian), Is.EqualTo(-12.5f));
        Assert.That(TiffBinaryReader.ReadDouble(stream, TiffByteOrder.LittleEndian), Is.EqualTo(12345.25d));
        Assert.That(stream.Position, Is.EqualTo(42));
    }

    [Test]
    public void StreamPrimitives_RoundTripSequentialBigEndianValues()
    {
        MemoryStream stream = new MemoryStream();

        TiffBinaryWriter.WriteByte(stream, 0x7F);
        TiffBinaryWriter.WriteSByte(stream, -7);
        TiffBinaryWriter.WriteUInt16(stream, 0x1234, TiffByteOrder.BigEndian);
        TiffBinaryWriter.WriteInt16(stream, -1234, TiffByteOrder.BigEndian);
        TiffBinaryWriter.WriteUInt32(stream, 0x89ABCDEFU, TiffByteOrder.BigEndian);
        TiffBinaryWriter.WriteInt32(stream, -12345678, TiffByteOrder.BigEndian);
        TiffBinaryWriter.WriteUInt64(stream, 0x0123456789ABCDEFUL, TiffByteOrder.BigEndian);
        TiffBinaryWriter.WriteInt64(stream, -1234567890123456789L, TiffByteOrder.BigEndian);
        TiffBinaryWriter.WriteSingle(stream, -12.5f, TiffByteOrder.BigEndian);
        TiffBinaryWriter.WriteDouble(stream, 12345.25d, TiffByteOrder.BigEndian);

        Assert.That(stream.Position, Is.EqualTo(42));
        stream.Position = 0;
        Assert.That(TiffBinaryReader.ReadByte(stream), Is.EqualTo(0x7F));
        Assert.That(TiffBinaryReader.ReadSByte(stream), Is.EqualTo(-7));
        Assert.That(TiffBinaryReader.ReadUInt16(stream, TiffByteOrder.BigEndian), Is.EqualTo(0x1234));
        Assert.That(TiffBinaryReader.ReadInt16(stream, TiffByteOrder.BigEndian), Is.EqualTo(-1234));
        Assert.That(TiffBinaryReader.ReadUInt32(stream, TiffByteOrder.BigEndian), Is.EqualTo(0x89ABCDEFU));
        Assert.That(TiffBinaryReader.ReadInt32(stream, TiffByteOrder.BigEndian), Is.EqualTo(-12345678));
        Assert.That(TiffBinaryReader.ReadUInt64(stream, TiffByteOrder.BigEndian), Is.EqualTo(0x0123456789ABCDEFUL));
        Assert.That(TiffBinaryReader.ReadInt64(stream, TiffByteOrder.BigEndian), Is.EqualTo(-1234567890123456789L));
        Assert.That(TiffBinaryReader.ReadSingle(stream, TiffByteOrder.BigEndian), Is.EqualTo(-12.5f));
        Assert.That(TiffBinaryReader.ReadDouble(stream, TiffByteOrder.BigEndian), Is.EqualTo(12345.25d));
        Assert.That(stream.Position, Is.EqualTo(42));
    }

    [Test]
    public void EndianPrimitiveWrites_ProduceDifferentMultiByteLayouts()
    {
        byte[] little = new byte[8];
        byte[] big = new byte[8];

        TiffBinaryWriter.WriteUInt64(little, 0x0123456789ABCDEFUL, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt64(big, 0x0123456789ABCDEFUL, TiffByteOrder.BigEndian);

        Assert.That(little, Is.EqualTo(new byte[] { 0xEF, 0xCD, 0xAB, 0x89, 0x67, 0x45, 0x23, 0x01 }));
        Assert.That(big, Is.EqualTo(new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF }));
    }
}
