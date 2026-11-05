using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffGpsMetadataTests
{
    const int GpsLatitudeRef = 1;
    const int GpsLatitude = 2;
    const int GpsLongitudeRef = 3;
    const int GpsLongitude = 4;
    const int GpsAltitudeRef = 5;
    const int GpsAltitude = 6;

    [Test]
    public void WriteClassicGps_WritesParentPointerAndRoundTripsTypedValues()
    {
        TiffIfdEntry[] parentEntries = new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 100) };
        TiffTagValue[] gpsValues = CreateGpsValues();
        int byteCount = TiffGpsMetadataWriter.CalculateClassicWriteByteCount(parentEntries, gpsValues);
        byte[] fileBytes = new byte[8 + byteCount];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);

        TiffGpsWriteResult writeResult = TiffCodec.WriteClassicGps(fileBytes, parentEntries, gpsValues, TiffByteOrder.LittleEndian, 8, 0);
        TiffHeaderReadResult header = TiffHeader.ReadClassic(fileBytes);
        TiffDirectoryReadResult directories = TiffDirectoryReader.Read(header.Header, fileBytes);
        TiffGpsReadResult readResult = TiffCodec.ReadGps(directories.Directories[0], fileBytes, header.Header);

        Assert.That(writeResult.Success, Is.True);
        Assert.That(directories.Success, Is.True);
        Assert.That(directories.Directories[0].FindEntry(TiffTagId.GpsIfd)!.ValueOffset, Is.EqualTo(writeResult.GpsOffset));
        Assert.That(readResult.Success, Is.True);
        Assert.That(readResult.Metadata.HasGps, Is.True);
        Assert.That(readResult.Metadata.GetAscii(GpsLatitudeRef), Is.EqualTo("N"));
        Assert.That(readResult.Metadata.GetAscii(GpsLongitudeRef), Is.EqualTo("W"));
        Assert.That(readResult.Metadata.GetUnsignedScalar(GpsAltitudeRef), Is.EqualTo(0));
        Assert.That(readResult.Metadata.GetRationalScalar(GpsAltitude).Numerator, Is.EqualTo(1234));
        Assert.That(readResult.Metadata.GetRationalScalar(GpsAltitude).Denominator, Is.EqualTo(10));
        Assert.That(readResult.Metadata.FindValue(GpsLatitude)!.RationalValues.Length, Is.EqualTo(3));
        Assert.That(readResult.Metadata.FindValue(GpsLongitude)!.RationalValues[0].Numerator, Is.EqualTo(122));
    }

    [Test]
    public void ReadGps_ReturnsEmptyMetadataWhenNoGpsPointerExists()
    {
        byte[] bytes = new byte[64];
        TiffHeader.WriteClassic(bytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);
        WriteClassicDirectory(bytes, 8, TiffTagId.ImageWidth, 0);
        TiffHeaderReadResult header = TiffHeader.ReadClassic(bytes);
        TiffDirectory parent = TiffDirectoryReader.Read(header.Header, bytes).Directories[0];

        TiffGpsReadResult result = TiffCodec.ReadGps(parent, bytes, header.Header);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Metadata.HasGps, Is.False);
        Assert.That(result.Metadata.ValueCount(), Is.EqualTo(0));
    }

    [Test]
    public void ReadGps_ReportsInvalidGpsPointerTagType()
    {
        TiffDirectory parent = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { CreateEntry(TiffTagId.GpsIfd, TiffDataType.Short, 1, 32) });

        TiffGpsReadResult result = TiffCodec.ReadGps(parent, new byte[64], TiffByteOrder.LittleEndian, TiffFileKind.Classic);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Metadata.HasGps, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagType), Is.True);
        Assert.That(result.Diagnostics.Get(0).TagId, Is.EqualTo(TiffTagId.GpsIfd));
    }

    [Test]
    public void WriteClassicGps_ReportsShortDestination()
    {
        TiffIfdEntry[] parentEntries = new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 100) };
        TiffTagValue[] gpsValues = CreateGpsValues();
        byte[] fileBytes = new byte[16];

        TiffGpsWriteResult result = TiffCodec.WriteClassicGps(fileBytes, parentEntries, gpsValues, TiffByteOrder.LittleEndian, 8, 0);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    static TiffTagValue[] CreateGpsValues()
    {
        return new TiffTagValue[]
        {
            TiffTagValue.CreateAscii(GpsLatitudeRef, "N"),
            TiffTagValue.CreateRationalArray(GpsLatitude, TiffDataType.Rational, new TiffRational[] { TiffRational.Create(37, 1), TiffRational.Create(48, 1), TiffRational.Create(3000, 100) }),
            TiffTagValue.CreateAscii(GpsLongitudeRef, "W"),
            TiffTagValue.CreateRationalArray(GpsLongitude, TiffDataType.Rational, new TiffRational[] { TiffRational.Create(122, 1), TiffRational.Create(24, 1), TiffRational.Create(1500, 100) }),
            TiffTagValue.CreateUnsignedScalar(GpsAltitudeRef, TiffDataType.Byte, 0),
            TiffTagValue.CreateRationalScalar(GpsAltitude, TiffDataType.Rational, 1234, 10)
        };
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

    static void WriteClassicDirectory(byte[] bytes, int offset, int tagId, uint nextOffset)
    {
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset, 2), 1, TiffByteOrder.LittleEndian);
        WriteClassicEntry(bytes, offset + 2, tagId, TiffDataType.Long, 1, 100);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(offset + 14, 4), nextOffset, TiffByteOrder.LittleEndian);
    }

    static void WriteClassicEntry(byte[] bytes, int offset, int tagId, TiffDataType dataType, uint count, uint valueOffset)
    {
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset, 2), (ushort)tagId, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset + 2, 2), (ushort)dataType, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(offset + 4, 4), count, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(offset + 8, 4), valueOffset, TiffByteOrder.LittleEndian);
    }
}
