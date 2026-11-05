using System.Collections.Generic;
using System.IO;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class ExifUserCommentTests
{
    [Test]
    public void Parse_DecodesAsciiUserComment()
    {
        ImageMetadataDocument document = ParseTiff(CreateUserCommentTiff(AsciiComment("hello")));

        MetadataTag tag = RequiredMetadataQuery.FindFirst(document, "Exif.ExifIFD.UserComment");

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(tag, Is.Not.Null);
        Assert.That(tag.Value.Kind, Is.EqualTo(MetadataValueKind.String));
        Assert.That(tag.Value.Text, Is.EqualTo("hello"));
        Assert.That(tag.DisplayValue, Is.EqualTo("hello"));
        Assert.That(tag.RawValue, Has.Length.EqualTo(14));
    }

    [Test]
    public void Parse_DecodesUnicodeUserCommentUsingTiffEndian()
    {
        ImageMetadataDocument document = ParseTiff(CreateUserCommentTiff(new byte[]
        {
            85, 78, 73, 67, 79, 68, 69, 0,
            72, 0, 105, 0, 0, 0
        }));

        MetadataTag tag = RequiredMetadataQuery.FindFirst(document, "Exif.ExifIFD.UserComment");

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(tag.Value.Kind, Is.EqualTo(MetadataValueKind.String));
        Assert.That(tag.Value.Text, Is.EqualTo("Hi"));
    }

    [Test]
    public void Parse_DecodesUndefinedUserCommentAsUtf8()
    {
        ImageMetadataDocument document = ParseTiff(CreateUserCommentTiff(new byte[]
        {
            0, 0, 0, 0, 0, 0, 0, 0,
            112, 108, 97, 105, 110, 0
        }));

        MetadataTag tag = RequiredMetadataQuery.FindFirst(document, "Exif.ExifIFD.UserComment");

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(tag.Value.Kind, Is.EqualTo(MetadataValueKind.String));
        Assert.That(tag.Value.Text, Is.EqualTo("plain"));
    }

    [Test]
    public void Parse_UnsupportedJisUserCommentKeepsRawBytesAndAddsDiagnostic()
    {
        byte[] payload = new byte[]
        {
            74, 73, 83, 0, 0, 0, 0, 0,
            65, 66, 0
        };

        ImageMetadataDocument document = ParseTiff(CreateUserCommentTiff(payload));

        MetadataTag tag = RequiredMetadataQuery.FindFirst(document, "Exif.ExifIFD.UserComment");

        Assert.That(tag.Value.Kind, Is.EqualTo(MetadataValueKind.ByteArray));
        Assert.That(tag.RawValue, Is.EqualTo(payload));
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("exif.usercomment.encoding.unsupported"));
        Assert.That(document.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.Unsupported));
    }

    [Test]
    public void Parse_TruncatedUserCommentEncodingAddsDiagnostic()
    {
        byte[] payload = new byte[] { 65, 83, 67, 73, 73 };

        ImageMetadataDocument document = ParseTiff(CreateUserCommentTiff(payload));

        MetadataTag tag = RequiredMetadataQuery.FindFirst(document, "Exif.ExifIFD.UserComment");

        Assert.That(tag.Value.Kind, Is.EqualTo(MetadataValueKind.ByteArray));
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("exif.usercomment.encoding.truncated"));
        Assert.That(document.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.Truncated));
    }

    public static byte[] CreateUserCommentTiff(byte[] payload)
    {
        List<byte> bytes = new List<byte>();
        bytes.Add(73);
        bytes.Add(73);
        AddUnsigned16(bytes, 42);
        AddUnsigned32(bytes, 8);

        AddUnsigned16(bytes, 1);
        AddEntry(bytes, 34665, 4, 1, 26);
        AddUnsigned32(bytes, 0);

        AddUnsigned16(bytes, 1);
        AddEntry(bytes, 37510, 7, (uint)payload.Length, 44);
        AddUnsigned32(bytes, 0);
        AddBytes(bytes, payload);
        return bytes.ToArray();
    }

    public static byte[] AsciiComment(string text)
    {
        List<byte> bytes = new List<byte>();
        bytes.Add(65);
        bytes.Add(83);
        bytes.Add(67);
        bytes.Add(73);
        bytes.Add(73);
        bytes.Add(0);
        bytes.Add(0);
        bytes.Add(0);
        int index = 0;
        while (index < text.Length)
        {
            bytes.Add((byte)text[index]);
            index = index + 1;
        }

        bytes.Add(0);
        return bytes.ToArray();
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

    public static void AddBytes(List<byte> bytes, byte[] payload)
    {
        int index = 0;
        while (index < payload.Length)
        {
            bytes.Add(payload[index]);
            index = index + 1;
        }
    }

    public static ImageMetadataDocument ParseTiff(byte[] bytes)
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes);
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return TiffMetadataParser.Parse(reader, ImageFormatMapping.CreateInfo(ImageFormatKind.Tiff), new ImageMetadataReaderOptions());
    }
}
