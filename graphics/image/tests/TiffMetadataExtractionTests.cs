using System.Collections.Generic;
using System.IO;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class TiffMetadataExtractionTests
{
    [Test]
    public void ReadStream_ClassicTiffCombinesExifGpsAndThumbnailMetadata()
    {
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.CaptureRawBlockData = true;

        ImageMetadataDocument document = new ImageMetadataReader().ReadStreamWithOptions(new MemoryStream(BuildCompositeClassicTiff()), options);

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Tiff));
        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(FindDirectory(document, "IFD0").Family, Is.EqualTo(MetadataFamily.Tiff));
        Assert.That(FindDirectory(document, "ExifIFD").Family, Is.EqualTo(MetadataFamily.Exif));
        Assert.That(FindDirectory(document, "GPS").Family, Is.EqualTo(MetadataFamily.Gps));
        Assert.That(FindDirectory(document, "IFD1").Name, Is.EqualTo("Thumbnail Image File Directory"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Make").Value.Text, Is.EqualTo("Camera Co"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Exif.ExifIFD.DateTimeOriginal").Value.Text, Is.EqualTo("2026:08:12 10:11:12"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Gps.GPS.Latitude").Value.Numerators, Is.EqualTo(new long[] { 37, 48, 30 }));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Gps.GPS.Latitude").Value.Denominators, Is.EqualTo(new long[] { 1, 1, 1 }));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Gps.GPS.Longitude").Value.Numerators, Is.EqualTo(new long[] { 122, 24, 15 }));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Gps.GPS.Longitude").Value.Denominators, Is.EqualTo(new long[] { 1, 1, 1 }));
        Assert.That(document.Summary.CameraMake, Is.EqualTo("Camera Co"));
        Assert.That(document.Summary.CaptureTimeText, Is.EqualTo("2026:08:12 10:11:12"));
        Assert.That(document.Summary.Gps.IsValid, Is.True);
        Assert.That(document.Summary.Gps.Latitude, Is.EqualTo(-37.8083333333).Within(0.0000001));
        Assert.That(document.Summary.Gps.Longitude, Is.EqualTo(-122.4041666667).Within(0.0000001));
        Assert.That(document.Summary.Gps.Altitude, Is.EqualTo(-7.5));
        Assert.That(document.Summary.Gps.LatitudeReference, Is.EqualTo("S"));
        Assert.That(document.Summary.Gps.LongitudeReference, Is.EqualTo("W"));
        Assert.That(document.Summary.Gps.AltitudeReference, Is.EqualTo("1"));
        Assert.That(document.Profiles, Has.Count.EqualTo(1));
        Assert.That(document.Profiles[0].Kind, Is.EqualTo(MetadataProfileKind.Thumbnail));
        Assert.That(document.Profiles[0].Data, Is.EqualTo(new byte[] { 255, 216, 255, 217 }));
        Assert.That(document.RawBlocks, Has.Count.EqualTo(1));
        Assert.That(document.RawBlocks[0].Data, Is.EqualTo(new byte[] { 255, 216, 255, 217 }));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Thumbnail.IFD1.Width").Value.UnsignedIntegerValue, Is.EqualTo(16));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Thumbnail.IFD1.Height").Value.UnsignedIntegerValue, Is.EqualTo(8));
    }

    [TestCase(true, "LittleEndian")]
    [TestCase(false, "BigEndian")]
    public void ReadStream_BigTiffDecodesLong8SignedLong8Ifd8AndExternalArrays(bool littleEndian, string byteOrder)
    {
        byte[] bytes = BuildBigTiffWith64BitTypes(littleEndian);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.BigTiff));
        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.Header.ByteOrder").Value.Text, Is.EqualTo(byteOrder));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.ImageWidth").Value.UnsignedIntegerValue, Is.EqualTo(5000000000));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.ImageLength").Value.UnsignedIntegerValue, Is.EqualTo(4000));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.SubIFDs").Value.UnsignedIntegerValue, Is.EqualTo(132));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Tag65000").Value.IntegerValue, Is.EqualTo(-2));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Tag65001").Value.UnsignedIntegerValues, Is.EqualTo(new ulong[] { 11, 22 }));
        Assert.That(FindDirectory(document, "SubIFD0"), Is.Not.Null);
    }

    [Test]
    public void BuildSummary_ConvertsGpsRationalListsAndAppliesReferences()
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        MetadataDirectory gps = document.AddDirectoryWithName(MetadataFamily.Gps, "GPS", "GPS IFD");
        gps.AddTagWithValue("Gps.GPS.Latitude", "Latitude", StringList("10/1", "30/1", "0/1"));
        gps.AddTagWithValue("Gps.GPS.Longitude", "Longitude", StringList("20/1", "15/1", "30/2"));
        gps.AddTagWithValue("Gps.GPS.LatitudeRef", "Latitude Ref", MetadataValue.FromString("N"));
        gps.AddTagWithValue("Gps.GPS.LongitudeRef", "Longitude Ref", MetadataValue.FromString("W"));
        gps.AddTagWithValue("Gps.GPS.Altitude", "Altitude", MetadataValue.FromRational(25, 2));
        gps.AddTagWithValue("Gps.GPS.AltitudeRef", "Altitude Ref", MetadataValue.FromString("1"));

        ImageMetadataSummary summary = new ImageMetadataNormalizer().BuildSummary(document);

        Assert.That(summary.Gps.IsValid, Is.True);
        Assert.That(summary.Gps.Latitude, Is.EqualTo(10.5));
        Assert.That(summary.Gps.Longitude, Is.EqualTo(-20.2541666667).Within(0.0000001));
        Assert.That(summary.Gps.Altitude, Is.EqualTo(-12.5));
    }

    [Test]
    public void BuildSummary_AppliesCardinalReferenceToPositiveDecimalCoordinate()
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        MetadataDirectory gps = document.AddDirectoryWithName(MetadataFamily.Gps, "GPS", "GPS IFD");
        gps.AddTagWithValue("Gps.GPS.Latitude", "Latitude", MetadataValue.FromDouble(12.5));
        gps.AddTagWithValue("Gps.GPS.Longitude", "Longitude", MetadataValue.FromDouble(45.25));
        gps.AddTagWithValue("Gps.GPS.LatitudeRef", "Latitude Ref", MetadataValue.FromString("South"));
        gps.AddTagWithValue("Gps.GPS.LongitudeRef", "Longitude Ref", MetadataValue.FromString("West"));

        ImageMetadataSummary summary = new ImageMetadataNormalizer().BuildSummary(document);

        Assert.That(summary.Gps.Latitude, Is.EqualTo(-12.5));
        Assert.That(summary.Gps.Longitude, Is.EqualTo(-45.25));
    }

    [Test]
    public void ReadStream_ClassicIfdFieldTypeRoutesSubIfd()
    {
        byte[] bytes = new byte[]
        {
            73, 73, 42, 0, 8, 0, 0, 0,
            1, 0,
            74, 1, 13, 0, 1, 0, 0, 0, 26, 0, 0, 0,
            0, 0, 0, 0,
            0, 0,
            0, 0, 0, 0
        };

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.SubIFDs").Value.UnsignedIntegerValue, Is.EqualTo(26));
        Assert.That(FindDirectory(document, "SubIFD0"), Is.Not.Null);
    }

    public static byte[] BuildCompositeClassicTiff()
    {
        const int ifd0Offset = 8;
        const int ifd1Offset = 74;
        const int exifOffset = 128;
        const int gpsOffset = 146;
        const int makeOffset = 224;
        const int dateOffset = 234;
        const int latitudeOffset = 254;
        const int longitudeOffset = 278;
        const int altitudeOffset = 302;
        const int thumbnailOffset = 310;

        List<byte> bytes = new List<byte>();
        bytes.Add(73);
        bytes.Add(73);
        AddUnsigned16(bytes, 42, true);
        AddUnsigned32(bytes, ifd0Offset, true);

        AddUnsigned16(bytes, 5, true);
        AddClassicEntry(bytes, 256, TiffValueDecoder.TypeShort, 1, 640, true);
        AddClassicEntry(bytes, 257, TiffValueDecoder.TypeShort, 1, 480, true);
        AddClassicEntry(bytes, 271, TiffValueDecoder.TypeAscii, 10, makeOffset, true);
        AddClassicEntry(bytes, TiffIfdTraverser.TagExifIfdPointer, TiffValueDecoder.TypeLong, 1, exifOffset, true);
        AddClassicEntry(bytes, TiffIfdTraverser.TagGpsIfdPointer, TiffValueDecoder.TypeLong, 1, gpsOffset, true);
        AddUnsigned32(bytes, ifd1Offset, true);

        AddUnsigned16(bytes, 4, true);
        AddClassicEntry(bytes, 256, TiffValueDecoder.TypeLong, 1, 16, true);
        AddClassicEntry(bytes, 257, TiffValueDecoder.TypeLong, 1, 8, true);
        AddClassicEntry(bytes, TiffThumbnailExtractor.TagJpegInterchangeFormat, TiffValueDecoder.TypeLong, 1, thumbnailOffset, true);
        AddClassicEntry(bytes, TiffThumbnailExtractor.TagJpegInterchangeFormatLength, TiffValueDecoder.TypeLong, 1, 4, true);
        AddUnsigned32(bytes, 0, true);

        AddUnsigned16(bytes, 1, true);
        AddClassicEntry(bytes, 36867, TiffValueDecoder.TypeAscii, 20, dateOffset, true);
        AddUnsigned32(bytes, 0, true);

        AddUnsigned16(bytes, 6, true);
        AddClassicEntry(bytes, 1, TiffValueDecoder.TypeAscii, 2, 83, true);
        AddClassicEntry(bytes, 2, TiffValueDecoder.TypeRational, 3, latitudeOffset, true);
        AddClassicEntry(bytes, 3, TiffValueDecoder.TypeAscii, 2, 87, true);
        AddClassicEntry(bytes, 4, TiffValueDecoder.TypeRational, 3, longitudeOffset, true);
        AddClassicEntry(bytes, 5, TiffValueDecoder.TypeByte, 1, 1, true);
        AddClassicEntry(bytes, 6, TiffValueDecoder.TypeRational, 1, altitudeOffset, true);
        AddUnsigned32(bytes, 0, true);

        AddAscii(bytes, "Camera Co", true);
        AddAscii(bytes, "2026:08:12 10:11:12", true);
        AddRational(bytes, 37, 1, true);
        AddRational(bytes, 48, 1, true);
        AddRational(bytes, 30, 1, true);
        AddRational(bytes, 122, 1, true);
        AddRational(bytes, 24, 1, true);
        AddRational(bytes, 15, 1, true);
        AddRational(bytes, 15, 2, true);
        bytes.Add(255);
        bytes.Add(216);
        bytes.Add(255);
        bytes.Add(217);
        return bytes.ToArray();
    }

    public static byte[] BuildBigTiffWith64BitTypes(bool littleEndian)
    {
        List<byte> bytes = new List<byte>();
        bytes.Add(littleEndian ? (byte)73 : (byte)77);
        bytes.Add(littleEndian ? (byte)73 : (byte)77);
        AddUnsigned16(bytes, 43, littleEndian);
        AddUnsigned16(bytes, 8, littleEndian);
        AddUnsigned16(bytes, 0, littleEndian);
        AddUnsigned64(bytes, 16, littleEndian);
        AddUnsigned64(bytes, 5, littleEndian);
        AddBigTiffEntry(bytes, 256, TiffValueDecoder.TypeLong8, 1, 5000000000, littleEndian);
        AddBigTiffEntry(bytes, 257, TiffValueDecoder.TypeLong8, 1, 4000, littleEndian);
        AddBigTiffEntry(bytes, TiffIfdTraverser.TagSubIfds, TiffValueDecoder.TypeIfd8, 1, 132, littleEndian);
        AddBigTiffEntry(bytes, 65000, TiffValueDecoder.TypeSignedLong8, 1, IntegerBitPattern.ToUnsigned64(-2), littleEndian);
        AddBigTiffEntry(bytes, 65001, TiffValueDecoder.TypeLong8, 2, 148, littleEndian);
        AddUnsigned64(bytes, 0, littleEndian);
        AddUnsigned64(bytes, 0, littleEndian);
        AddUnsigned64(bytes, 0, littleEndian);
        AddUnsigned64(bytes, 11, littleEndian);
        AddUnsigned64(bytes, 22, littleEndian);
        return bytes.ToArray();
    }

    public static MetadataValue StringList(string first, string second, string third)
    {
        MetadataValue value = new MetadataValue();
        value.Kind = MetadataValueKind.StringList;
        value.TextValues = new string[] { first, second, third };
        return value;
    }

    public static MetadataDirectory FindDirectory(ImageMetadataDocument document, string group)
    {
        foreach (MetadataDirectory directory in document.Directories)
        {
            if (directory.Group == group)
            {
                return directory;
            }
        }

        throw new System.InvalidOperationException("Expected metadata directory was not found: " + group);
    }

    public static void AddClassicEntry(List<byte> bytes, int tag, int type, uint count, uint value, bool littleEndian)
    {
        AddUnsigned16(bytes, tag, littleEndian);
        AddUnsigned16(bytes, type, littleEndian);
        AddUnsigned32(bytes, count, littleEndian);
        AddUnsigned32(bytes, value, littleEndian);
    }

    public static void AddBigTiffEntry(List<byte> bytes, int tag, int type, ulong count, ulong value, bool littleEndian)
    {
        AddUnsigned16(bytes, tag, littleEndian);
        AddUnsigned16(bytes, type, littleEndian);
        AddUnsigned64(bytes, count, littleEndian);
        AddUnsigned64(bytes, value, littleEndian);
    }

    public static void AddRational(List<byte> bytes, uint numerator, uint denominator, bool littleEndian)
    {
        AddUnsigned32(bytes, numerator, littleEndian);
        AddUnsigned32(bytes, denominator, littleEndian);
    }

    public static void AddAscii(List<byte> bytes, string text, bool terminate)
    {
        int index = 0;
        while (index < text.Length)
        {
            bytes.Add((byte)text[index]);
            index = index + 1;
        }

        if (terminate)
        {
            bytes.Add(0);
        }
    }

    public static void AddUnsigned16(List<byte> bytes, int value, bool littleEndian)
    {
        if (littleEndian)
        {
            bytes.Add((byte)(value & 255));
            bytes.Add((byte)((value >> 8) & 255));
        }
        else
        {
            bytes.Add((byte)((value >> 8) & 255));
            bytes.Add((byte)(value & 255));
        }
    }

    public static void AddUnsigned32(List<byte> bytes, uint value, bool littleEndian)
    {
        if (littleEndian)
        {
            bytes.Add((byte)(value & 255));
            bytes.Add((byte)((value >> 8) & 255));
            bytes.Add((byte)((value >> 16) & 255));
            bytes.Add((byte)((value >> 24) & 255));
        }
        else
        {
            bytes.Add((byte)((value >> 24) & 255));
            bytes.Add((byte)((value >> 16) & 255));
            bytes.Add((byte)((value >> 8) & 255));
            bytes.Add((byte)(value & 255));
        }
    }

    public static void AddUnsigned64(List<byte> bytes, ulong value, bool littleEndian)
    {
        int index = littleEndian ? 0 : 7;
        int change = littleEndian ? 1 : -1;
        int written = 0;
        while (written < 8)
        {
            bytes.Add((byte)((value >> (index * 8)) & 255));
            index = index + change;
            written = written + 1;
        }
    }
}
