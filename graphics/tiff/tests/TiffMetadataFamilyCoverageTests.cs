using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffMetadataFamilyCoverageTests
{
    const int DateTimeOriginal = 36867;
    const int GpsAltitudeRef = 5;

    [Test]
    public void MetadataReaders_ReadAllSupportedFamiliesFromOneDirectory()
    {
        byte[] fileBytes = new byte[1024];
        byte[] exifDate = TiffAscii.EncodeString("2026:08:06 12:34:56");
        byte[] xmpBytes = CreateAsciiBytes("<x:xmpmeta><rdf:Description /></x:xmpmeta>");
        byte[] iccBytes = new byte[] { 0, 0, 0, 12, 97, 99, 115, 112, 1, 2, 3, 4 };
        byte[] geoAsciiBytes = CreateAsciiBytes("WGS 84|");
        byte[] gdalMetadata = TiffAscii.EncodeString("<GDALMetadata><Item name=\"AREA_OR_POINT\">Area</Item></GDALMetadata>");
        byte[] gdalNoData = TiffAscii.EncodeString("-9999");
        WriteSingleEntryDirectory(fileBytes, 128, DateTimeOriginal, TiffDataType.Ascii, exifDate.Length, 544);
        WriteBytes(fileBytes, 544, exifDate);
        WriteSingleEntryDirectory(fileBytes, 192, GpsAltitudeRef, TiffDataType.Byte, 1, 0);
        WriteBytes(fileBytes, 256, xmpBytes);
        WriteBytes(fileBytes, 320, iccBytes);
        WriteDoubles(fileBytes, 384, new double[] { 30, 30, 0 });
        WriteDoubles(fileBytes, 416, new double[] { 0, 0, 0, 100, 200, 0 });
        WriteShorts(fileBytes, 480, new ushort[] { 1, 1, 1, 1, 1024, 0, 1, 1 });
        WriteDoubles(fileBytes, 512, new double[] { 298.257223563 });
        WriteBytes(fileBytes, 576, geoAsciiBytes);
        WriteBytes(fileBytes, 640, gdalMetadata);
        WriteBytes(fileBytes, 736, gdalNoData);
        TiffDirectory parent = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ExifIfd, TiffDataType.Long, 1, 128),
            CreateEntry(TiffTagId.GpsIfd, TiffDataType.Long, 1, 192),
            CreateEntry(TiffTagId.Xmp, TiffDataType.Byte, xmpBytes.Length, 256),
            CreateEntry(TiffTagId.IccProfile, TiffDataType.Undefined, iccBytes.Length, 320),
            CreateEntry(TiffTagId.ModelPixelScale, TiffDataType.Double, 3, 384),
            CreateEntry(TiffTagId.ModelTiepoint, TiffDataType.Double, 6, 416),
            CreateEntry(TiffTagId.GeoKeyDirectory, TiffDataType.Short, 8, 480),
            CreateEntry(TiffTagId.GeoDoubleParams, TiffDataType.Double, 1, 512),
            CreateEntry(TiffTagId.GeoAsciiParams, TiffDataType.Ascii, geoAsciiBytes.Length, 576),
            CreateEntry(TiffTagId.GdalMetadata, TiffDataType.Ascii, gdalMetadata.Length, 640),
            CreateEntry(TiffTagId.GdalNoData, TiffDataType.Ascii, gdalNoData.Length, 736)
        });
        TiffHeader header = TiffHeader.Create(TiffByteOrder.LittleEndian, TiffFileKind.Classic, 8);

        TiffExifReadResult exif = TiffCodec.ReadExif(parent, fileBytes, header);
        TiffGpsReadResult gps = TiffCodec.ReadGps(parent, fileBytes, header);
        TiffXmpReadResult xmp = TiffCodec.ReadXmp(parent, fileBytes);
        TiffIccProfileReadResult icc = TiffCodec.ReadIccProfile(parent, fileBytes);
        TiffGeoModelReadResult geoModel = TiffCodec.ReadGeoModel(parent, fileBytes, header);
        TiffGeoKeyDirectoryReadResult geoKeys = TiffCodec.ReadGeoKeyDirectory(parent, fileBytes, header);
        TiffGeoParameterReadResult geoParams = TiffCodec.ReadGeoParameters(parent, fileBytes, header);
        TiffGdalMetadataReadResult gdal = TiffCodec.ReadGdalMetadata(parent, fileBytes);

        Assert.That(exif.Success, Is.True);
        Assert.That(exif.Metadata.HasExif, Is.True);
        Assert.That(exif.Metadata.GetAscii(DateTimeOriginal), Is.EqualTo("2026:08:06 12:34:56"));
        Assert.That(gps.Success, Is.True);
        Assert.That(gps.Metadata.HasGps, Is.True);
        Assert.That(gps.Metadata.GetUnsignedScalar(GpsAltitudeRef), Is.EqualTo(0));
        Assert.That(xmp.Success, Is.True);
        Assert.That(xmp.Packet.Text, Is.EqualTo("<x:xmpmeta><rdf:Description /></x:xmpmeta>"));
        Assert.That(icc.Success, Is.True);
        Assert.That(icc.Profile.Bytes, Is.EqualTo(iccBytes));
        Assert.That(geoModel.Success, Is.True);
        Assert.That(geoModel.Metadata.PixelScale, Is.EqualTo(new double[] { 30, 30, 0 }));
        Assert.That(geoModel.Metadata.Tiepoints[0].ModelX, Is.EqualTo(100));
        Assert.That(geoKeys.Success, Is.True);
        Assert.That(geoKeys.Directory.KeyCount(), Is.EqualTo(1));
        Assert.That(geoKeys.Directory.Keys[0].KeyId, Is.EqualTo(1024));
        Assert.That(geoParams.Success, Is.True);
        Assert.That(geoParams.Metadata.DoubleParams[0], Is.EqualTo(298.257223563));
        Assert.That(geoParams.Metadata.AsciiValues[0], Is.EqualTo("WGS 84"));
        Assert.That(gdal.Success, Is.True);
        Assert.That(gdal.Metadata.MetadataXml, Does.Contain("AREA_OR_POINT"));
        Assert.That(gdal.Metadata.NoDataText, Is.EqualTo("-9999"));
    }

    [Test]
    public void MetadataReaders_ReturnEmptyWhenOptionalMetadataFamiliesAreMissing()
    {
        TiffDirectory parent = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 100) });
        byte[] fileBytes = new byte[128];
        TiffHeader header = TiffHeader.Create(TiffByteOrder.LittleEndian, TiffFileKind.Classic, 8);

        TiffExifReadResult exif = TiffCodec.ReadExif(parent, fileBytes, header);
        TiffGpsReadResult gps = TiffCodec.ReadGps(parent, fileBytes, header);
        TiffXmpReadResult xmp = TiffCodec.ReadXmp(parent, fileBytes);
        TiffIccProfileReadResult icc = TiffCodec.ReadIccProfile(parent, fileBytes);
        TiffGeoModelReadResult geoModel = TiffCodec.ReadGeoModel(parent, fileBytes, header);
        TiffGeoKeyDirectoryReadResult geoKeys = TiffCodec.ReadGeoKeyDirectory(parent, fileBytes, header);
        TiffGeoParameterReadResult geoParams = TiffCodec.ReadGeoParameters(parent, fileBytes, header);
        TiffGdalMetadataReadResult gdal = TiffCodec.ReadGdalMetadata(parent, fileBytes);

        Assert.That(exif.Success, Is.True);
        Assert.That(exif.Metadata.HasExif, Is.False);
        Assert.That(gps.Success, Is.True);
        Assert.That(gps.Metadata.HasGps, Is.False);
        Assert.That(xmp.Success, Is.True);
        Assert.That(xmp.Packet.HasXmp, Is.False);
        Assert.That(icc.Success, Is.True);
        Assert.That(icc.Profile.HasProfile, Is.False);
        Assert.That(geoModel.Success, Is.True);
        Assert.That(geoModel.Metadata.HasPixelScale, Is.False);
        Assert.That(geoKeys.Success, Is.True);
        Assert.That(geoKeys.Directory.HasDirectory, Is.False);
        Assert.That(geoParams.Success, Is.True);
        Assert.That(geoParams.Metadata.HasDoubleParams, Is.False);
        Assert.That(geoParams.Metadata.HasAsciiParams, Is.False);
        Assert.That(gdal.Success, Is.True);
        Assert.That(gdal.Metadata.HasMetadata, Is.False);
        Assert.That(gdal.Metadata.HasNoData, Is.False);
    }

    static void WriteSingleEntryDirectory(byte[] bytes, int offset, int tagId, TiffDataType dataType, int count, uint valueOffset)
    {
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset, 2), 1, TiffByteOrder.LittleEndian);
        WriteClassicEntry(bytes, offset + 2, tagId, dataType, (uint)count, valueOffset);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(offset + 14, 4), 0, TiffByteOrder.LittleEndian);
    }

    static void WriteClassicEntry(byte[] bytes, int offset, int tagId, TiffDataType dataType, uint count, uint valueOffset)
    {
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset, 2), (ushort)tagId, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset + 2, 2), (ushort)dataType, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(offset + 4, 4), count, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(offset + 8, 4), valueOffset, TiffByteOrder.LittleEndian);
    }

    static void WriteDoubles(byte[] bytes, int offset, double[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            TiffBinaryWriter.WriteDouble(bytes.AsSpan(offset + (index * 8), 8), values[index], TiffByteOrder.LittleEndian);
            index = index + 1;
        }
    }

    static void WriteShorts(byte[] bytes, int offset, ushort[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset + (index * 2), 2), values[index], TiffByteOrder.LittleEndian);
            index = index + 1;
        }
    }

    static void WriteBytes(byte[] destination, int offset, byte[] source)
    {
        int index = 0;
        while (index < source.Length)
        {
            destination[offset + index] = source[index];
            index = index + 1;
        }
    }

    static byte[] CreateAsciiBytes(string text)
    {
        byte[] bytes = new byte[text.Length];
        int index = 0;
        while (index < text.Length)
        {
            bytes[index] = (byte)text[index];
            index = index + 1;
        }

        return bytes;
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
        TiffBinaryWriter.WriteUInt32(entry.ValueOffsetBytes, (uint)valueOffset, TiffByteOrder.LittleEndian);
        entry.ValueByteCount = TiffDataTypeInfo.CalculateValueByteCount(dataType, count);
        entry.ValueSlotByteCount = 4;
        return entry;
    }
}
