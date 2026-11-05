using System.Collections.Generic;
using System.IO;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class TiffGeoKeyExtractorTests
{
    [Test]
    public void Parse_ExtractsGeoTiffKeysAndNamesGeoTiffTags()
    {
        ImageMetadataDocument document = ParseTiff(CreateGeoTiff());

        MetadataTag keyDirectory = RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.GeoKeyDirectoryTag");
        MetadataTag doubleParams = RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.GeoDoubleParamsTag");
        MetadataTag asciiParams = RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.GeoAsciiParamsTag");
        MetadataTag modelType = RequiredMetadataQuery.FindFirst(document, "Tiff.GeoTIFF.GTModelTypeGeoKey");
        MetadataTag citation = RequiredMetadataQuery.FindFirst(document, "Tiff.GeoTIFF.GTCitationGeoKey");
        MetadataTag semiMajor = RequiredMetadataQuery.FindFirst(document, "Tiff.GeoTIFF.GeogSemiMajorAxisGeoKey");

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(keyDirectory, Is.Not.Null);
        Assert.That(keyDirectory.Name, Is.EqualTo("GeoKeyDirectoryTag"));
        Assert.That(doubleParams.Name, Is.EqualTo("GeoDoubleParamsTag"));
        Assert.That(asciiParams.Name, Is.EqualTo("GeoAsciiParamsTag"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.GeoTIFF.KeyCount").Value.UnsignedIntegerValue, Is.EqualTo(3));
        Assert.That(modelType.Value.UnsignedIntegerValue, Is.EqualTo(2));
        Assert.That(citation.Value.Kind, Is.EqualTo(MetadataValueKind.String));
        Assert.That(citation.Value.Text, Is.EqualTo("WGS 84"));
        Assert.That(semiMajor.Value.Kind, Is.EqualTo(MetadataValueKind.Double));
        Assert.That(semiMajor.Value.DoubleValue, Is.EqualTo(1.25));
    }

    [Test]
    public void Parse_TruncatedGeoKeyDirectoryAddsDiagnostic()
    {
        byte[] bytes = new byte[]
        {
            73, 73, 42, 0, 8, 0, 0, 0,
            1, 0,
            175, 135, 3, 0, 4, 0, 0, 0, 26, 0, 0, 0,
            0, 0, 0, 0,
            1, 0, 1, 0, 0, 0, 1, 0
        };

        ImageMetadataDocument document = ParseTiff(bytes);

        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.GeoTIFF.KeyCount").Value.UnsignedIntegerValue, Is.EqualTo(0));
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("geotiff.key.directory.truncated"));
        Assert.That(document.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.Truncated));
    }

    public static byte[] CreateGeoTiff()
    {
        List<byte> bytes = new List<byte>();
        bytes.Add(73);
        bytes.Add(73);
        AddUnsigned16(bytes, 42);
        AddUnsigned32(bytes, 8);

        AddUnsigned16(bytes, 3);
        AddEntry(bytes, 34735, 3, 16, 50);
        AddEntry(bytes, 34736, 12, 1, 82);
        AddEntry(bytes, 34737, 2, 7, 90);
        AddUnsigned32(bytes, 0);

        AddUnsigned16(bytes, 1);
        AddUnsigned16(bytes, 1);
        AddUnsigned16(bytes, 0);
        AddUnsigned16(bytes, 3);
        AddGeoKey(bytes, 1024, 0, 1, 2);
        AddGeoKey(bytes, 1026, 34737, 7, 0);
        AddGeoKey(bytes, 2057, 34736, 1, 0);
        bytes.Add(0);
        bytes.Add(0);
        bytes.Add(0);
        bytes.Add(0);
        bytes.Add(0);
        bytes.Add(0);
        bytes.Add(244);
        bytes.Add(63);
        bytes.Add(87);
        bytes.Add(71);
        bytes.Add(83);
        bytes.Add(32);
        bytes.Add(56);
        bytes.Add(52);
        bytes.Add(124);
        return bytes.ToArray();
    }

    public static void AddGeoKey(List<byte> bytes, int keyId, int tagLocation, int count, int valueOffset)
    {
        AddUnsigned16(bytes, keyId);
        AddUnsigned16(bytes, tagLocation);
        AddUnsigned16(bytes, count);
        AddUnsigned16(bytes, valueOffset);
    }

    public static void AddEntry(List<byte> bytes, int tagId, int typeId, uint count, uint value)
    {
        AddUnsigned16(bytes, tagId);
        AddUnsigned16(bytes, typeId);
        AddUnsigned32(bytes, count);
        AddUnsigned32(bytes, value);
    }

    public static void AddUnsigned16(List<byte> bytes, int value)
    {
        bytes.Add((byte)(value & 255));
        bytes.Add((byte)((value >> 8) & 255));
    }

    public static void AddUnsigned32(List<byte> bytes, uint value)
    {
        bytes.Add((byte)(value & 255));
        bytes.Add((byte)((value >> 8) & 255));
        bytes.Add((byte)((value >> 16) & 255));
        bytes.Add((byte)((value >> 24) & 255));
    }

    public static ImageMetadataDocument ParseTiff(byte[] bytes)
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes);
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return TiffMetadataParser.Parse(reader, ImageFormatMapping.CreateInfo(ImageFormatKind.Tiff), new ImageMetadataReaderOptions());
    }
}
