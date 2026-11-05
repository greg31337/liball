using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffImageDescriptionMetadataTests
{
    [Test]
    public void WriteClassicImageDescription_UpdatesDescriptionAndPreservesOtherValues()
    {
        TiffTagValue[] values = new TiffTagValue[]
        {
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 10),
            TiffTagValue.CreateAscii(TiffTagId.Software, "writer"),
            TiffTagValue.CreateAscii(TiffTagId.ImageDescription, "old")
        };
        byte[] fileBytes = new byte[8 + TiffImageDescriptionWriter.CalculateClassicWriteByteCount(values, "new description")];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);

        TiffImageDescriptionWriteResult writeResult = TiffCodec.WriteClassicImageDescription(fileBytes, values, "new description", TiffByteOrder.LittleEndian, 8, 0);
        TiffHeaderReadResult header = TiffHeader.ReadClassic(fileBytes);
        TiffDirectory directory = TiffDirectoryReader.Read(header.Header, fileBytes).Directories[0];
        TiffImageDescriptionReadResult readResult = TiffCodec.ReadImageDescription(directory, fileBytes);
        TiffTagValueLoadResult softwareBytes = TiffTagValueLoader.LoadBytes(directory.FindEntry(TiffTagId.Software)!, fileBytes);
        TiffAsciiResult software = TiffAscii.Decode(softwareBytes.Bytes);

        Assert.That(writeResult.Success, Is.True);
        Assert.That(directory.EntryCount(), Is.EqualTo(3));
        Assert.That(readResult.Success, Is.True);
        Assert.That(readResult.Metadata.HasDescription, Is.True);
        Assert.That(readResult.Metadata.Text, Is.EqualTo("new description"));
        Assert.That(software.Text, Is.EqualTo("writer"));
    }

    [Test]
    public void ReadImageDescription_ReturnsEmptyWhenTagIsMissing()
    {
        byte[] fileBytes = new byte[64];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);
        WriteClassicDirectory(fileBytes, 8, TiffTagId.ImageWidth, 0);
        TiffHeaderReadResult header = TiffHeader.ReadClassic(fileBytes);
        TiffDirectory directory = TiffDirectoryReader.Read(header.Header, fileBytes).Directories[0];

        TiffImageDescriptionReadResult result = TiffCodec.ReadImageDescription(directory, fileBytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Metadata.HasDescription, Is.False);
        Assert.That(result.Metadata.Text, Is.EqualTo(""));
    }

    [Test]
    public void ReadImageDescription_ReportsInvalidTagType()
    {
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageDescription, TiffDataType.Long, 1, 100) });

        TiffImageDescriptionReadResult result = TiffCodec.ReadImageDescription(directory, new byte[16]);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagType), Is.True);
        Assert.That(result.Diagnostics.Get(0).TagId, Is.EqualTo(TiffTagId.ImageDescription));
    }

    [Test]
    public void WriteClassicImageDescription_ReportsShortDestination()
    {
        TiffTagValue[] values = new TiffTagValue[] { TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 10) };

        TiffImageDescriptionWriteResult result = TiffCodec.WriteClassicImageDescription(new byte[16], values, "description", TiffByteOrder.LittleEndian, 8, 0);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void ReadImageDescription_ReportsInvalidAscii()
    {
        TiffIfdEntry entry = CreateEntry(TiffTagId.ImageDescription, TiffDataType.Ascii, 3, 0);
        entry.ValueOffsetBytes = new byte[] { 65, 66, 67, 0 };
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { entry });

        TiffImageDescriptionReadResult result = TiffCodec.ReadImageDescription(directory, new byte[0]);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidAsciiValue), Is.True);
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
