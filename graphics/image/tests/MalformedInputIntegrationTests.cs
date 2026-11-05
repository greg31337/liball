using System.Collections.Generic;
using System.IO;
using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class MalformedInputIntegrationTests
{
    [Test]
    public void ReadStream_TruncatedChunkReturnsPartialDocumentWithDiagnostic()
    {
        List<byte> bytes = new List<byte>();
        PngChunkTraverserTests.AddSignature(bytes);
        PngChunkTraverserTests.AddUnsigned32(bytes, 5);
        PngChunkTraverserTests.AddAscii(bytes, "tEXt");
        bytes.Add(1);
        bytes.Add(2);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes.ToArray()));

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Png));
        Assert.That(document.ByteLength, Is.EqualTo(bytes.Count));
        AssertDiagnostic(document, "png.chunk.truncated", MetadataSeverity.Truncated);
        Assert.That(document.IsCorrupt, Is.True);
        Assert.That(document.IsIncomplete, Is.True);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Png.Header.SignatureValid").Value.BooleanValue, Is.True);
        Assert.That(new ImageMetadataQuery().FindFirst(document, "Png.tEXt.Length"), Is.Null);
    }

    [Test]
    public void ReadStream_HugeDeclaredLengthIsRejectedWithoutAllocatingPayload()
    {
        List<byte> bytes = new List<byte>();
        PngChunkTraverserTests.AddSignature(bytes);
        PngChunkTraverserTests.AddUnsigned32(bytes, uint.MaxValue);
        PngChunkTraverserTests.AddAscii(bytes, "tEXt");

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes.ToArray()));

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Png));
        AssertDiagnostic(document, "png.chunk.length.limit", MetadataSeverity.LimitExceeded);
        AssertDiagnostic(document, "png.chunk.truncated", MetadataSeverity.Truncated);
        Assert.That(document.Directories, Has.Count.EqualTo(1));
        Assert.That(document.Profiles, Is.Empty);
    }

    [Test]
    public void ReadStream_CircularIfdStopsAtVisitedOffsetAndPreservesFirstDirectory()
    {
        byte[] bytes = new byte[]
        {
            73, 73, 42, 0, 8, 0, 0, 0,
            0, 0,
            8, 0, 0, 0
        };

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Tiff));
        AssertDiagnostic(document, "tiff.ifd.loop", MetadataSeverity.RecoverableError);
        Assert.That(document.IsCorrupt, Is.True);
        Assert.That(document.IsIncomplete, Is.False);
        Assert.That(new ImageMetadataQuery().Find(document, "Tiff.IFD0.EntryCount"), Has.Count.EqualTo(1));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.NextIfdOffset").Value.UnsignedIntegerValue, Is.EqualTo(8));
    }

    [Test]
    public void ReadStream_InvalidXmlReturnsDiagnosticsWithoutPartialXmlMetadata()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("<svg xmlns='http://www.w3.org/2000/svg'><g></svg>");

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Svg));
        AssertDiagnostic(document, "xml.malformed", MetadataSeverity.RecoverableError);
        AssertDiagnostic(document, "svg.root.missing", MetadataSeverity.RecoverableError);
        Assert.That(document.Directories, Is.Empty);
        Assert.That(document.Frames, Is.Empty);
    }

    [Test]
    public void ReadStream_InvalidCompressionIsPreservedAsUnknown()
    {
        byte[] bytes = PsdMetadataParserTests.CreateDocument(
            1,
            3,
            20,
            30,
            8,
            3,
            new byte[0],
            new byte[0],
            new byte[0],
            9,
            new byte[0]);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Psd));
        AssertDiagnostic(document, "psd.image-data.compression.unsupported", MetadataSeverity.Unsupported);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Psd.ImageData.Compression").Value.IntegerValue, Is.EqualTo(9));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Psd.ImageData.Compression").DisplayValue, Is.EqualTo("Unknown"));
        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(document.Frames[0].Compression, Is.EqualTo(ImageCompressionKind.Unknown));
    }

    [Test]
    public void ReadStream_DuplicateTiffTagsRemainQueryableInSourceOrder()
    {
        byte[] bytes = CreateTiffWithDuplicateWidths(640, 800);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        List<MetadataTag> widths = new ImageMetadataQuery().Find(document, "Tiff.IFD0.ImageWidth");
        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Tiff));
        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(widths, Has.Count.EqualTo(2));
        Assert.That(widths[0].Value.UnsignedIntegerValue, Is.EqualTo(640));
        Assert.That(widths[1].Value.UnsignedIntegerValue, Is.EqualTo(800));
        Assert.That(widths[0].Offset, Is.LessThan(widths[1].Offset));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.ImageWidth"), Is.SameAs(widths[0]));
    }

    public static byte[] CreateTiffWithDuplicateWidths(uint firstWidth, uint secondWidth)
    {
        List<byte> bytes = new List<byte>();
        bytes.Add(73);
        bytes.Add(73);
        AddUnsigned16(bytes, 42);
        AddUnsigned32(bytes, 8);
        AddUnsigned16(bytes, 2);
        AddTiffLongEntry(bytes, 256, firstWidth);
        AddTiffLongEntry(bytes, 256, secondWidth);
        AddUnsigned32(bytes, 0);
        return bytes.ToArray();
    }

    public static void AddTiffLongEntry(List<byte> bytes, ushort tagId, uint value)
    {
        AddUnsigned16(bytes, tagId);
        AddUnsigned16(bytes, 4);
        AddUnsigned32(bytes, 1);
        AddUnsigned32(bytes, value);
    }

    public static void AddUnsigned16(List<byte> bytes, ushort value)
    {
        bytes.Add((byte)value);
        bytes.Add((byte)(value >> 8));
    }

    public static void AddUnsigned32(List<byte> bytes, uint value)
    {
        bytes.Add((byte)value);
        bytes.Add((byte)(value >> 8));
        bytes.Add((byte)(value >> 16));
        bytes.Add((byte)(value >> 24));
    }

    public static void AssertDiagnostic(ImageMetadataDocument document, string code, MetadataSeverity severity)
    {
        Assert.That(document.Diagnostics, Has.Some.Matches<MetadataDiagnostic>(diagnostic => diagnostic.Code == code && diagnostic.Severity == severity), code);
    }
}
