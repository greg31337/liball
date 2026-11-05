using System.Collections.Generic;
using System.IO;
using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class TiffTagNameTableTests
{
    [Test]
    public void Parse_UsesCommonTiffTagNames()
    {
        byte[] bytes = new byte[]
        {
            73, 73, 42, 0, 8, 0, 0, 0,
            2, 0,
            15, 1, 2, 0, 6, 0, 0, 0, 38, 0, 0, 0,
            18, 1, 3, 0, 1, 0, 0, 0, 6, 0, 0, 0,
            0, 0, 0, 0,
            67, 97, 110, 111, 110, 0
        };

        ImageMetadataDocument document = ParseTiff(bytes);

        MetadataTag make = RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Make");
        MetadataTag orientation = RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Orientation");

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(make, Is.Not.Null);
        Assert.That(make.Name, Is.EqualTo("Make"));
        Assert.That(make.NumericId, Is.EqualTo(271));
        Assert.That(make.Value.Text, Is.EqualTo("Canon"));
        Assert.That(orientation, Is.Not.Null);
        Assert.That(orientation.Name, Is.EqualTo("Orientation"));
        Assert.That(orientation.Value.UnsignedIntegerValue, Is.EqualTo(6));
        Assert.That(new ImageMetadataQuery().FindFirst(document, "Tiff.IFD0.Tag274"), Is.Null);
    }

    [Test]
    public void Parse_UsesCommonExifAndGpsTagNames()
    {
        ImageMetadataDocument document = ParseTiff(CreateExifGpsTiff());

        MetadataTag dateTime = RequiredMetadataQuery.FindFirst(document, "Exif.ExifIFD.DateTimeOriginal");
        MetadataTag latitudeRef = RequiredMetadataQuery.FindFirst(document, "Gps.GPS.LatitudeRef");
        MetadataTag latitude = RequiredMetadataQuery.FindFirst(document, "Gps.GPS.Latitude");

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(dateTime, Is.Not.Null);
        Assert.That(dateTime.Name, Is.EqualTo("DateTimeOriginal"));
        Assert.That(dateTime.Value.Text, Is.EqualTo("2024:01:02 03:04:05"));
        Assert.That(latitudeRef, Is.Not.Null);
        Assert.That(latitudeRef.Name, Is.EqualTo("LatitudeRef"));
        Assert.That(latitudeRef.Value.Text, Is.EqualTo("N"));
        Assert.That(latitude, Is.Not.Null);
        Assert.That(latitude.Name, Is.EqualTo("Latitude"));
        Assert.That(latitude.Value.Kind, Is.EqualTo(MetadataValueKind.RationalArray));
        Assert.That(latitude.Value.Numerators, Is.EqualTo(new long[] { 45, 30, 0 }));
        Assert.That(latitude.Value.Denominators, Is.EqualTo(new long[] { 1, 1, 1 }));
    }

    [Test]
    public void Parse_DecodesWindowsTextAndRatingTagsIntoSummary()
    {
        List<byte> bytes = new List<byte>();
        bytes.AddRange(new byte[] { 73, 73, 42, 0, 8, 0, 0, 0 });
        AddUnsigned16(bytes, 5);
        AddEntry(bytes, 18246, 3, 1, 4);
        AddEntry(bytes, 18249, 3, 1, 88);
        AddEntry(bytes, 40091, 1, 12, 74);
        AddEntry(bytes, 40093, 1, 8, 86);
        AddEntry(bytes, 40094, 1, 18, 94);
        AddUnsigned32(bytes, 0);
        bytes.AddRange(Encoding.Unicode.GetBytes("Title\0"));
        bytes.AddRange(Encoding.Unicode.GetBytes("Ada\0"));
        bytes.AddRange(Encoding.Unicode.GetBytes("one; two\0"));

        ImageMetadataDocument document = ParseTiff(bytes.ToArray());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.XPTitle").Value.Text, Is.EqualTo("Title"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.XPAuthor").Value.Text, Is.EqualTo("Ada"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Rating").Value.UnsignedIntegerValue, Is.EqualTo(4));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.RatingPercent").Value.UnsignedIntegerValue, Is.EqualTo(88));
        Assert.That(document.Summary.Title, Is.EqualTo("Title"));
        Assert.That(document.Summary.Author, Is.EqualTo("Ada"));
        Assert.That(document.Summary.Keywords, Is.EqualTo(new[] { "one", "two" }));
        Assert.That(document.Summary.Rating, Is.EqualTo(4));
    }

    [Test]
    public void KeyNameFor_UnknownTagFallsBackToNumericTagName()
    {
        Assert.That(TiffTagNameTable.KeyNameFor(MetadataFamily.Tiff, "IFD0", 65000), Is.EqualTo("Tag65000"));
        Assert.That(TiffTagNameTable.DisplayNameFor(MetadataFamily.Tiff, "IFD0", 65000), Is.EqualTo("Tag 65000"));
    }

    public static byte[] CreateExifGpsTiff()
    {
        List<byte> bytes = new List<byte>();
        bytes.Add(73);
        bytes.Add(73);
        AddUnsigned16(bytes, 42);
        AddUnsigned32(bytes, 8);

        AddUnsigned16(bytes, 2);
        AddEntry(bytes, 34665, 4, 1, 38);
        AddEntry(bytes, 34853, 4, 1, 56);
        AddUnsigned32(bytes, 0);

        AddUnsigned16(bytes, 1);
        AddEntry(bytes, 36867, 2, 20, 86);
        AddUnsigned32(bytes, 0);

        AddUnsigned16(bytes, 2);
        AddEntry(bytes, 1, 2, 2, 78);
        AddEntry(bytes, 2, 5, 3, 106);
        AddUnsigned32(bytes, 0);

        AddAscii(bytes, "2024:01:02 03:04:05");
        AddUnsigned32(bytes, 45);
        AddUnsigned32(bytes, 1);
        AddUnsigned32(bytes, 30);
        AddUnsigned32(bytes, 1);
        AddUnsigned32(bytes, 0);
        AddUnsigned32(bytes, 1);
        return bytes.ToArray();
    }

    public static void AddEntry(List<byte> bytes, int tagId, int typeId, uint count, uint value)
    {
        AddUnsigned16(bytes, tagId);
        AddUnsigned16(bytes, typeId);
        AddUnsigned32(bytes, count);
        AddUnsigned32(bytes, value);
    }

    public static void AddAscii(List<byte> bytes, string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            bytes.Add((byte)text[index]);
            index = index + 1;
        }

        bytes.Add(0);
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
