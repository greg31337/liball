using System.Collections.Generic;
using System.IO;
using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class JpegCommentExtractorTests
{
    [Test]
    public void Parse_ComSegmentExtractsCommentDirectoryAndRawValue()
    {
        byte[] comment = Encoding.UTF8.GetBytes("hello");

        ImageMetadataDocument document = ParseJpeg(new byte[][] { comment });

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Directories, Has.Count.EqualTo(1));
        Assert.That(document.Directories[0].Family, Is.EqualTo(MetadataFamily.Jpeg));
        Assert.That(document.Directories[0].Group, Is.EqualTo("COM"));
        Assert.That(document.Directories[0].Name, Is.EqualTo("Comment"));
        Assert.That(document.Directories[0].Offset, Is.EqualTo(2));
        Assert.That(document.Directories[0].Length, Is.EqualTo(9));
        MetadataTag tag = RequiredMetadataQuery.FindFirst(document, "Jpeg.COM.Comment");
        Assert.That(tag.Value.Text, Is.EqualTo("hello"));
        Assert.That(tag.DisplayValue, Is.EqualTo("hello"));
        Assert.That(tag.RawValue, Is.EqualTo(comment));
        Assert.That(tag.Offset, Is.EqualTo(6));
        Assert.That(tag.Length, Is.EqualTo(5));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jpeg.COM.ByteLength").Value.IntegerValue, Is.EqualTo(5));
    }

    [Test]
    public void Parse_MultipleComSegmentsPreserveOrder()
    {
        byte[] first = Encoding.UTF8.GetBytes("first");
        byte[] second = Encoding.UTF8.GetBytes("second");

        ImageMetadataDocument document = ParseJpeg(new byte[][] { first, second });

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Directories, Has.Count.EqualTo(2));
        Assert.That(new ImageMetadataQuery().Find(document, "Jpeg.COM.Comment"), Has.Count.EqualTo(2));
        Assert.That(new ImageMetadataQuery().Find(document, "Jpeg.COM.Comment")[0].Value.Text, Is.EqualTo("first"));
        Assert.That(new ImageMetadataQuery().Find(document, "Jpeg.COM.Comment")[1].Value.Text, Is.EqualTo("second"));
    }

    [Test]
    public void Parse_EmptyComSegmentCreatesEmptyComment()
    {
        ImageMetadataDocument document = ParseJpeg(new byte[][] { new byte[0] });

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Directories, Has.Count.EqualTo(1));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jpeg.COM.Comment").Value.Text, Is.EqualTo(""));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jpeg.COM.ByteLength").Value.IntegerValue, Is.EqualTo(0));
    }

    [Test]
    public void Parse_InvalidUtf8ComUsesLatin1FallbackDiagnostic()
    {
        byte[] comment = new byte[] { 99, 97, 102, 233 };

        ImageMetadataDocument document = ParseJpeg(new byte[][] { comment });

        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jpeg.COM.Comment").Value.Text, Is.EqualTo("caf" + '\u00e9'));
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("string.utf8.invalid"));
        Assert.That(document.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.Warning));
        Assert.That(document.Diagnostics[0].BlockType, Is.EqualTo("COM"));
    }

    public static ImageMetadataDocument ParseJpeg(byte[][] comments)
    {
        List<byte> bytes = new List<byte>();
        bytes.Add(255);
        bytes.Add(216);

        int index = 0;
        while (index < comments.Length)
        {
            AddCom(bytes, comments[index]);
            index = index + 1;
        }

        bytes.Add(255);
        bytes.Add(217);

        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes.ToArray());
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return JpegMetadataParser.Parse(reader, ImageFormatMapping.CreateInfo(ImageFormatKind.Jpeg), new ImageMetadataReaderOptions());
    }

    public static void AddCom(List<byte> bytes, byte[] payload)
    {
        int declaredLength = payload.Length + 2;
        bytes.Add(255);
        bytes.Add(254);
        bytes.Add((byte)(declaredLength / 256));
        bytes.Add((byte)(declaredLength & 255));
        bytes.AddRange(payload);
    }
}
