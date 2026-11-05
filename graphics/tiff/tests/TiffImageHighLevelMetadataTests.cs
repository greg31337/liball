using System;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffImageHighLevelMetadataTests
{
    const int DateTimeOriginal = 36867;
    const int GpsAltitude = 6;

    [Test]
    public void ImageMetadataReaders_ReadTagFamiliesFromRandomAccessSource()
    {
        byte[] fileBytes = CreateMetadataFile();
        byte[] paddedBytes = new byte[fileBytes.Length + 1024 * 1024];
        fileBytes.CopyTo(paddedBytes, 0);
        TrackingSource source = new TrackingSource(paddedBytes);
        TiffReadOptions options = new TiffReadOptions();
        options.LeaveOpen = true;
        ITiffImage image = TiffApi.Factory.OpenMetadataSource(source, options).GetImage(0);

        TiffXmpReadResult xmp = image.ReadXmp();
        TiffIccProfileReadResult icc = image.ReadIccProfile();
        TiffPhotoshopReadResult photoshop = image.ReadPhotoshop();
        TiffIptcReadResult iptc = image.ReadIptc();
        TiffImageDescriptionReadResult description = image.ReadImageDescription();
        TiffImageJMetadataReadResult imageJ = image.ReadImageJMetadata();
        TiffGdalMetadataReadResult gdal = image.ReadGdalMetadata();
        TiffGeoKeyDirectoryReadResult keys = image.ReadGeoKeyDirectory();
        TiffGeoModelReadResult model = image.ReadGeoModel();
        TiffGeoParameterReadResult parameters = image.ReadGeoParameters();
        TiffVendorMetadataReadResult vendor = image.ReadVendorMetadata();

        Assert.That(xmp.Success, Is.True);
        Assert.That(xmp.Packet.Text, Is.EqualTo("<x:xmpmeta>Aperio</x:xmpmeta>"));
        Assert.That(icc.Success, Is.True);
        Assert.That(icc.Profile.Bytes, Is.EqualTo(new byte[] { 0, 0, 0, 4, 97, 99, 115, 112 }));
        Assert.That(photoshop.Success, Is.True);
        Assert.That(photoshop.Metadata.BlockCount(), Is.EqualTo(1));
        Assert.That(iptc.Success, Is.True);
        Assert.That(iptc.Metadata.Blocks[0].Data, Is.EqualTo(new byte[] { 116, 101, 115, 116 }));
        Assert.That(description.Success, Is.True);
        Assert.That(description.Metadata.Text, Does.Contain("ImageJ=1.54"));
        Assert.That(imageJ.Success, Is.True);
        Assert.That(imageJ.Metadata.HasImageJ, Is.True);
        Assert.That(imageJ.Metadata.Channels, Is.EqualTo(2));
        Assert.That(gdal.Success, Is.True);
        Assert.That(gdal.Metadata.MetadataXml, Does.Contain("AREA_OR_POINT"));
        Assert.That(gdal.Metadata.NoDataText, Is.EqualTo("-9999"));
        Assert.That(keys.Success, Is.True);
        Assert.That(keys.Directory.Keys[0].KeyId, Is.EqualTo(1024));
        Assert.That(model.Success, Is.True);
        Assert.That(model.Metadata.PixelScale, Is.EqualTo(new double[] { 30, 30, 0 }));
        Assert.That(parameters.Success, Is.True);
        Assert.That(parameters.Metadata.DoubleParams[0], Is.EqualTo(298.257223563));
        Assert.That(parameters.Metadata.AsciiValues[0], Is.EqualTo("WGS 84"));
        Assert.That(vendor.Success, Is.True);
        Assert.That(vendor.Metadata.PrimaryVendor, Is.EqualTo("Aperio"));
        Assert.That(source.LargestRequest, Is.LessThan(paddedBytes.Length));
        Assert.That(source.TotalRequested, Is.LessThan(paddedBytes.Length));
    }

    [Test]
    public void ReadOmeMetadata_UsesTheImageDescriptionFromTheOwnedSource()
    {
        string xml = "<OME><Image ID=\"Image:0\"><Pixels ID=\"Pixels:0\" SizeX=\"4\" SizeY=\"3\" /></Image></OME>";
        TiffWriter writer = new TiffFactory().CreateWriter(new TiffWriteOptions());
        writer.AddImage(TiffImageDefinition.Create(new TiffTagValue[] { TiffTagValue.CreateAscii(TiffTagId.ImageDescription, xml) }));
        writer.Flush();
        ITiffImage image = TiffApi.Factory.OpenMetadataBytes(writer.GetFileBytes(), new TiffReadOptions()).GetImage(0);

        TiffOmeMetadataReadResult result = image.ReadOmeMetadata();

        Assert.That(result.Success, Is.True);
        Assert.That(result.Metadata.HasOmeXml, Is.True);
        Assert.That(result.Metadata.Xml, Is.EqualTo(xml));
        Assert.That(result.Metadata.Images.Length, Is.EqualTo(1));
    }

    [Test]
    public void ReadExifAndGps_ResolveNestedIfdsFromTheOwnedSource()
    {
        ITiffImage exifImage = TiffApi.Factory.OpenMetadataBytes(CreateExifFile(), new TiffReadOptions()).GetImage(0);
        ITiffImage gpsImage = TiffApi.Factory.OpenMetadataBytes(CreateGpsFile(), new TiffReadOptions()).GetImage(0);

        TiffExifReadResult exif = exifImage.ReadExif();
        TiffGpsReadResult gps = gpsImage.ReadGps();

        Assert.That(exif.Success, Is.True);
        Assert.That(exif.Metadata.HasExif, Is.True);
        Assert.That(exif.Metadata.GetAscii(DateTimeOriginal), Is.EqualTo("2026:08:26 12:00:00"));
        Assert.That(gps.Success, Is.True);
        Assert.That(gps.Metadata.HasGps, Is.True);
        Assert.That(gps.Metadata.GetRationalScalar(GpsAltitude).Numerator, Is.EqualTo(1234));
        Assert.That(gps.Metadata.GetRationalScalar(GpsAltitude).Denominator, Is.EqualTo(10));
    }

    [Test]
    public void ImageMetadataReaders_ReportClosedSource()
    {
        TiffFile file = TiffApi.Factory.OpenMetadataBytes(CreateMetadataFile(), new TiffReadOptions());
        ITiffImage image = file.GetImage(0);
        file.Close();

        TiffXmpReadResult result = image.ReadXmp();

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.UnsupportedStream), Is.True);
    }

    static byte[] CreateMetadataFile()
    {
        byte[] xmp = CreateAsciiBytes("<x:xmpmeta>Aperio</x:xmpmeta>");
        byte[] icc = new byte[] { 0, 0, 0, 4, 97, 99, 115, 112 };
        byte[] photoshop = new byte[] { 56, 66, 73, 77, 4, 4, 0, 0, 0, 0, 0, 3, 1, 2, 3, 0 };
        byte[] iptc = new byte[] { 0x1c, 2, 5, 0, 4, 116, 101, 115, 116 };
        string description = "ImageJ=1.54\nimages=2\nchannels=2\nAperio";
        TiffTagValue[] tags = new TiffTagValue[]
        {
            TiffTagValue.CreateAscii(TiffTagId.ImageDescription, description),
            TiffTagValue.CreateRaw(TiffTagId.Xmp, TiffDataType.Byte, xmp.Length, xmp),
            TiffTagValue.CreateRaw(TiffTagId.IccProfile, TiffDataType.Undefined, icc.Length, icc),
            TiffTagValue.CreateRaw(TiffTagId.Photoshop, TiffDataType.Byte, photoshop.Length, photoshop),
            TiffTagValue.CreateRaw(TiffTagId.Iptc, TiffDataType.Undefined, iptc.Length, iptc),
            TiffTagValue.CreateAscii(TiffTagId.GdalMetadata, "<GDALMetadata><Item name=\"AREA_OR_POINT\">Area</Item></GDALMetadata>"),
            TiffTagValue.CreateAscii(TiffTagId.GdalNoData, "-9999"),
            TiffTagValue.CreateAscii(TiffTagId.Make, "Aperio Technologies"),
            TiffTagValue.CreateFloatingArray(TiffTagId.ModelPixelScale, TiffDataType.Double, new double[] { 30, 30, 0 }),
            TiffTagValue.CreateFloatingArray(TiffTagId.ModelTiepoint, TiffDataType.Double, new double[] { 0, 0, 0, 100, 200, 0 }),
            TiffTagValue.CreateUnsignedArray(TiffTagId.GeoKeyDirectory, TiffDataType.Short, new ulong[] { 1, 1, 1, 1, 1024, 0, 1, 1 }),
            TiffTagValue.CreateFloatingArray(TiffTagId.GeoDoubleParams, TiffDataType.Double, new double[] { 298.257223563 }),
            TiffTagValue.CreateAscii(TiffTagId.GeoAsciiParams, "WGS 84|")
        };
        TiffWriter writer = new TiffFactory().CreateWriter(new TiffWriteOptions());
        writer.AddImage(TiffImageDefinition.Create(tags));
        TiffDiagnostics flush = writer.Flush();
        Assert.That(flush.Count(), Is.EqualTo(0));
        return writer.GetFileBytes();
    }

    static byte[] CreateExifFile()
    {
        TiffIfdEntry[] parent = new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 10) };
        TiffTagValue[] values = new TiffTagValue[] { TiffTagValue.CreateAscii(DateTimeOriginal, "2026:08:26 12:00:00") };
        byte[] bytes = new byte[8 + TiffExifMetadataWriter.CalculateClassicWriteByteCount(parent, values)];
        TiffHeader.WriteClassic(bytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);
        TiffCodec.WriteClassicExif(bytes, parent, values, TiffByteOrder.LittleEndian, 8, 0);
        return bytes;
    }

    static byte[] CreateGpsFile()
    {
        TiffIfdEntry[] parent = new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 10) };
        TiffTagValue[] values = new TiffTagValue[] { TiffTagValue.CreateRationalScalar(GpsAltitude, TiffDataType.Rational, 1234, 10) };
        byte[] bytes = new byte[8 + TiffGpsMetadataWriter.CalculateClassicWriteByteCount(parent, values)];
        TiffHeader.WriteClassic(bytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);
        TiffCodec.WriteClassicGps(bytes, parent, values, TiffByteOrder.LittleEndian, 8, 0);
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
        entry.ValueByteCount = TiffDataTypeInfo.CalculateValueByteCount(dataType, count);
        entry.ValueSlotByteCount = 4;
        return entry;
    }

    static byte[] CreateAsciiBytes(string text)
    {
        byte[] bytes = new byte[text.Length];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = (byte)text[index];
            index = index + 1;
        }

        return bytes;
    }

    public class TrackingSource : ITiffRandomAccessSource
    {
        public byte[] Bytes;
        public bool IsOpen;
        public int LargestRequest;
        public long TotalRequested;

        public TrackingSource(byte[] bytes)
        {
            Bytes = bytes;
            IsOpen = true;
            LargestRequest = 0;
            TotalRequested = 0;
        }

        public bool GetIsOpen()
        {
            return IsOpen;
        }

        public long GetLength()
        {
            return Bytes.Length;
        }

        public int Read(long offset, Span<byte> destination)
        {
            if (destination.Length > LargestRequest)
            {
                LargestRequest = destination.Length;
            }

            TotalRequested = TotalRequested + destination.Length;
            int count = destination.Length;
            long remaining = Bytes.Length - offset;
            if (remaining < count)
            {
                count = (int)remaining;
            }

            Bytes.AsSpan((int)offset, count).CopyTo(destination);
            return count;
        }

        public void Close()
        {
            IsOpen = false;
        }
    }
}
