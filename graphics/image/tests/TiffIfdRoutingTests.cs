using System.Collections.Generic;
using System.IO;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class TiffIfdRoutingTests
{
    [Test]
    public void Parse_RoutesReferencedIfdsAndThumbnailIfd()
    {
        ImageMetadataDocument document = ParseTiff(CreateRoutedTiff(), new ImageMetadataReaderOptions());

        MetadataDirectory ifd0 = FindDirectory(document, "IFD0") ?? throw new System.InvalidOperationException("Expected IFD0 was not found.");
        MetadataDirectory exif = FindDirectory(document, "ExifIFD") ?? throw new System.InvalidOperationException("Expected EXIF IFD was not found.");
        MetadataDirectory gps = FindDirectory(document, "GPS") ?? throw new System.InvalidOperationException("Expected GPS IFD was not found.");
        MetadataDirectory interop = FindDirectory(document, "Interoperability") ?? throw new System.InvalidOperationException("Expected interoperability IFD was not found.");
        MetadataDirectory sub0 = FindDirectory(document, "SubIFD0") ?? throw new System.InvalidOperationException("Expected first sub-IFD was not found.");
        MetadataDirectory sub1 = FindDirectory(document, "SubIFD1") ?? throw new System.InvalidOperationException("Expected second sub-IFD was not found.");
        MetadataDirectory thumbnail = FindDirectory(document, "IFD1") ?? throw new System.InvalidOperationException("Expected thumbnail IFD was not found.");

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Directories, Has.Count.EqualTo(8));
        Assert.That(ifd0, Is.Not.Null);
        Assert.That(exif.Family, Is.EqualTo(MetadataFamily.Exif));
        Assert.That(gps.Family, Is.EqualTo(MetadataFamily.Gps));
        Assert.That(interop.Family, Is.EqualTo(MetadataFamily.Exif));
        Assert.That(sub0.Family, Is.EqualTo(MetadataFamily.Tiff));
        Assert.That(sub1.Family, Is.EqualTo(MetadataFamily.Tiff));
        Assert.That(thumbnail.Name, Is.EqualTo("Thumbnail Image File Directory"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.ExifIFDPointer").Value.UnsignedIntegerValue, Is.EqualTo(56));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Exif.ExifIFD.InteroperabilityIFDPointer").Value.UnsignedIntegerValue, Is.EqualTo(92));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Gps.GPS.EntryCount").Value.UnsignedIntegerValue, Is.EqualTo(0));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.SubIFDs").Value.UnsignedIntegerValues, Is.EqualTo(new ulong[] { 80, 86 }));
    }

    [Test]
    public void Parse_RoutedIfdLoopAddsDiagnostic()
    {
        byte[] bytes = new byte[]
        {
            73, 73, 42, 0, 8, 0, 0, 0,
            1, 0,
            105, 135, 4, 0, 1, 0, 0, 0, 8, 0, 0, 0,
            0, 0, 0, 0
        };

        ImageMetadataDocument document = ParseTiff(bytes, new ImageMetadataReaderOptions());

        Assert.That(document.Directories, Has.Count.EqualTo(2));
        Assert.That(FindDirectory(document, "ExifIFD"), Is.Null);
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("tiff.ifd.loop"));
        Assert.That(document.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.RecoverableError));
    }

    [Test]
    public void Parse_RoutedIfdRespectsDepthLimit()
    {
        byte[] bytes = new byte[]
        {
            73, 73, 42, 0, 8, 0, 0, 0,
            1, 0,
            105, 135, 4, 0, 1, 0, 0, 0, 26, 0, 0, 0,
            0, 0, 0, 0,
            0, 0,
            0, 0, 0, 0
        };
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxNestingDepth = 1;

        ImageMetadataDocument document = ParseTiff(bytes, options);

        Assert.That(document.Directories, Has.Count.EqualTo(2));
        Assert.That(FindDirectory(document, "ExifIFD"), Is.Null);
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("tiff.ifd.depth.limit"));
        Assert.That(document.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.LimitExceeded));
    }

    public static byte[] CreateRoutedTiff()
    {
        List<byte> bytes = new List<byte>();
        bytes.Add(73);
        bytes.Add(73);
        AddUnsigned16(bytes, 42);
        AddUnsigned32(bytes, 8);

        AddUnsigned16(bytes, 3);
        AddEntry(bytes, 330, 4, 2, 98);
        AddEntry(bytes, 34665, 4, 1, 56);
        AddEntry(bytes, 34853, 4, 1, 74);
        AddUnsigned32(bytes, 50);

        AddEmptyIfd(bytes);
        AddUnsigned16(bytes, 1);
        AddEntry(bytes, 40965, 4, 1, 92);
        AddUnsigned32(bytes, 0);
        AddEmptyIfd(bytes);
        AddEmptyIfd(bytes);
        AddEmptyIfd(bytes);
        AddEmptyIfd(bytes);
        AddUnsigned32(bytes, 80);
        AddUnsigned32(bytes, 86);
        return bytes.ToArray();
    }

    public static void AddEmptyIfd(List<byte> bytes)
    {
        AddUnsigned16(bytes, 0);
        AddUnsigned32(bytes, 0);
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

    public static MetadataDirectory? FindDirectory(ImageMetadataDocument document, string group)
    {
        foreach (MetadataDirectory directory in document.Directories)
        {
            if (directory.Group == group)
            {
                return directory;
            }
        }

        return null;
    }

    public static ImageMetadataDocument ParseTiff(byte[] bytes, ImageMetadataReaderOptions options)
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes);
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return TiffMetadataParser.Parse(reader, ImageFormatMapping.CreateInfo(ImageFormatKind.Tiff), options);
    }
}
