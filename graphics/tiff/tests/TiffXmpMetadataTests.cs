using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffXmpMetadataTests
{
    [Test]
    public void WriteClassicXmp_WritesAndReadsUtf8Packet()
    {
        TiffTagValue[] values = new TiffTagValue[] { TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 10) };
        string packetText = "<x:xmpmeta><rdf:Description dc:title=\"Cafe\" /></x:xmpmeta>";
        TiffTagValue packetValue = TiffXmpMetadataWriter.CreateValue(packetText);
        byte[] fileBytes = new byte[8 + TiffXmpMetadataWriter.CalculateClassicWriteByteCount(values, packetValue.RawBytes)];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);

        TiffXmpWriteResult writeResult = TiffCodec.WriteClassicXmp(fileBytes, values, packetValue.RawBytes, TiffByteOrder.LittleEndian, 8, 0);
        TiffHeaderReadResult header = TiffHeader.ReadClassic(fileBytes);
        TiffDirectory directory = TiffDirectoryReader.Read(header.Header, fileBytes).Directories[0];
        TiffXmpReadResult readResult = TiffCodec.ReadXmp(directory, fileBytes);

        Assert.That(writeResult.Success, Is.True);
        Assert.That(directory.FindEntry(TiffTagId.Xmp)!.DataType, Is.EqualTo(TiffDataType.Byte));
        Assert.That(readResult.Success, Is.True);
        Assert.That(readResult.Packet.HasXmp, Is.True);
        Assert.That(readResult.Packet.Text, Is.EqualTo(packetText));
        Assert.That(readResult.Packet.Bytes, Is.EqualTo(packetValue.RawBytes));
    }

    [Test]
    public void ReadXmp_ReturnsEmptyPacketWhenTagIsMissing()
    {
        byte[] fileBytes = new byte[64];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);
        WriteClassicDirectory(fileBytes, 8, TiffTagId.ImageWidth, 0);
        TiffHeaderReadResult header = TiffHeader.ReadClassic(fileBytes);
        TiffDirectory directory = TiffDirectoryReader.Read(header.Header, fileBytes).Directories[0];

        TiffXmpReadResult result = TiffCodec.ReadXmp(directory, fileBytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Packet.HasXmp, Is.False);
        Assert.That(result.Packet.ByteCount(), Is.EqualTo(0));
    }

    [Test]
    public void WriteClassicXmp_ReplacesExistingXmpValue()
    {
        TiffTagValue[] values = new TiffTagValue[]
        {
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 10),
            TiffXmpMetadataWriter.CreateValue("old")
        };
        byte[] newBytes = TiffXmpMetadataWriter.CreateValue("new").RawBytes;
        byte[] fileBytes = new byte[8 + TiffXmpMetadataWriter.CalculateClassicWriteByteCount(values, newBytes)];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);

        TiffXmpWriteResult writeResult = TiffCodec.WriteClassicXmp(fileBytes, values, newBytes, TiffByteOrder.LittleEndian, 8, 0);
        TiffDirectory directory = TiffDirectoryReader.ReadClassic(fileBytes, TiffByteOrder.LittleEndian, 8).Directories[0];
        TiffXmpReadResult readResult = TiffCodec.ReadXmp(directory, fileBytes);

        Assert.That(writeResult.Success, Is.True);
        Assert.That(directory.EntryCount(), Is.EqualTo(2));
        Assert.That(readResult.Packet.Text, Is.EqualTo("new"));
    }

    [Test]
    public void ReadXmp_ReportsInvalidXmpTagType()
    {
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { CreateEntry(TiffTagId.Xmp, TiffDataType.Ascii, 4, 0) });

        TiffXmpReadResult result = TiffCodec.ReadXmp(directory, new byte[16]);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagType), Is.True);
        Assert.That(result.Diagnostics.Get(0).TagId, Is.EqualTo(TiffTagId.Xmp));
    }

    [Test]
    public void WriteClassicXmp_ReportsShortDestination()
    {
        TiffTagValue[] values = new TiffTagValue[] { TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 10) };
        byte[] bytes = TiffXmpMetadataWriter.CreateValue("<x:xmpmeta />").RawBytes;

        TiffXmpWriteResult result = TiffCodec.WriteClassicXmp(new byte[16], values, bytes, TiffByteOrder.LittleEndian, 8, 0);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    static void WriteClassicDirectory(byte[] bytes, int offset, int tagId, uint nextOffset)
    {
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset, 2), 1, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset + 2, 2), (ushort)tagId, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset + 4, 2), (ushort)TiffDataType.Long, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(offset + 6, 4), 1, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(offset + 10, 4), 100, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(offset + 14, 4), nextOffset, TiffByteOrder.LittleEndian);
    }

    static TiffIfdEntry CreateEntry(int tagId, TiffDataType dataType, long count, ulong valueOffset)
    {
        TiffIfdEntry entry = new TiffIfdEntry();
        entry.TagId = tagId;
        entry.DataType = dataType;
        entry.Count = count;
        entry.RawCount = (ulong)count;
        entry.ValueOffset = valueOffset;
        entry.ValueOffsetBytes = new byte[4];
        entry.ValueByteCount = TiffDataTypeInfo.CalculateValueByteCount(dataType, count);
        entry.ValueSlotByteCount = 4;
        return entry;
    }
}
