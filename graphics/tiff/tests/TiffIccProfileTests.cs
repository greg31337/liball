using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffIccProfileTests
{
    [Test]
    public void WriteClassicIccProfile_WritesAndReadsProfileBytes()
    {
        TiffTagValue[] values = new TiffTagValue[] { TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 10) };
        byte[] profileBytes = CreateProfileBytes();
        byte[] fileBytes = new byte[8 + TiffIccProfileWriter.CalculateClassicWriteByteCount(values, profileBytes)];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);

        TiffIccProfileWriteResult writeResult = TiffCodec.WriteClassicIccProfile(fileBytes, values, profileBytes, TiffByteOrder.LittleEndian, 8, 0);
        TiffHeaderReadResult header = TiffHeader.ReadClassic(fileBytes);
        TiffDirectory directory = TiffDirectoryReader.Read(header.Header, fileBytes).Directories[0];
        TiffIccProfileReadResult readResult = TiffCodec.ReadIccProfile(directory, fileBytes);

        Assert.That(writeResult.Success, Is.True);
        Assert.That(directory.FindEntry(TiffTagId.IccProfile)!.DataType, Is.EqualTo(TiffDataType.Undefined));
        Assert.That(readResult.Success, Is.True);
        Assert.That(readResult.Profile.HasProfile, Is.True);
        Assert.That(readResult.Profile.Bytes, Is.EqualTo(profileBytes));
    }

    [Test]
    public void ReadIccProfile_ReturnsEmptyProfileWhenTagIsMissing()
    {
        byte[] fileBytes = new byte[64];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);
        WriteClassicDirectory(fileBytes, 8, TiffTagId.ImageWidth, 0);
        TiffHeaderReadResult header = TiffHeader.ReadClassic(fileBytes);
        TiffDirectory directory = TiffDirectoryReader.Read(header.Header, fileBytes).Directories[0];

        TiffIccProfileReadResult result = TiffCodec.ReadIccProfile(directory, fileBytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Profile.HasProfile, Is.False);
        Assert.That(result.Profile.ByteCount(), Is.EqualTo(0));
    }

    [Test]
    public void WriteClassicIccProfile_ReplacesExistingProfileValue()
    {
        TiffTagValue[] values = new TiffTagValue[]
        {
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 10),
            TiffIccProfileWriter.CreateValue(new byte[] { 1, 2, 3 })
        };
        byte[] newBytes = new byte[] { 4, 5, 6, 7 };
        byte[] fileBytes = new byte[8 + TiffIccProfileWriter.CalculateClassicWriteByteCount(values, newBytes)];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);

        TiffIccProfileWriteResult writeResult = TiffCodec.WriteClassicIccProfile(fileBytes, values, newBytes, TiffByteOrder.LittleEndian, 8, 0);
        TiffDirectory directory = TiffDirectoryReader.ReadClassic(fileBytes, TiffByteOrder.LittleEndian, 8).Directories[0];
        TiffIccProfileReadResult readResult = TiffCodec.ReadIccProfile(directory, fileBytes);

        Assert.That(writeResult.Success, Is.True);
        Assert.That(directory.EntryCount(), Is.EqualTo(2));
        Assert.That(readResult.Profile.Bytes, Is.EqualTo(newBytes));
    }

    [Test]
    public void ReadIccProfile_ReportsInvalidProfileTagType()
    {
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { CreateEntry(TiffTagId.IccProfile, TiffDataType.Byte, 4, 0) });

        TiffIccProfileReadResult result = TiffCodec.ReadIccProfile(directory, new byte[16]);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagType), Is.True);
        Assert.That(result.Diagnostics.Get(0).TagId, Is.EqualTo(TiffTagId.IccProfile));
    }

    [Test]
    public void WriteClassicIccProfile_ReportsShortDestination()
    {
        TiffTagValue[] values = new TiffTagValue[] { TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 10) };

        TiffIccProfileWriteResult result = TiffCodec.WriteClassicIccProfile(new byte[16], values, CreateProfileBytes(), TiffByteOrder.LittleEndian, 8, 0);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    static byte[] CreateProfileBytes()
    {
        return new byte[] { 0, 0, 0, 12, 97, 99, 115, 112, 0, 1, 2, 3 };
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
