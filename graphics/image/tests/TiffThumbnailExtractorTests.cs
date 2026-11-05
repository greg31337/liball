using System.Collections.Generic;
using System.IO;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class TiffThumbnailExtractorTests
{
    [Test]
    public void Parse_ExtractsJpegThumbnailFromIfd1()
    {
        byte[] bytes = new byte[]
        {
            73, 73, 42, 0, 8, 0, 0, 0,
            0, 0,
            14, 0, 0, 0,
            2, 0,
            1, 2, 4, 0, 1, 0, 0, 0, 44, 0, 0, 0,
            2, 2, 4, 0, 1, 0, 0, 0, 4, 0, 0, 0,
            0, 0, 0, 0,
            255, 216, 255, 217
        };

        ImageMetadataDocument document = ParseTiff(bytes, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Profiles, Has.Count.EqualTo(1));
        Assert.That(document.Profiles[0].Kind, Is.EqualTo(MetadataProfileKind.Thumbnail));
        Assert.That(document.Profiles[0].Family, Is.EqualTo(MetadataFamily.Thumbnail));
        Assert.That(document.Profiles[0].Key, Is.EqualTo("Thumbnail.IFD1.JPEG"));
        Assert.That(document.Profiles[0].MimeType, Is.EqualTo("image/jpeg"));
        Assert.That(document.Profiles[0].Offset, Is.EqualTo(44));
        Assert.That(document.Profiles[0].Length, Is.EqualTo(4));
        Assert.That(document.Profiles[0].Data, Is.EqualTo(new byte[] { 255, 216, 255, 217 }));
        Assert.That(document.RawBlocks, Has.Count.EqualTo(1));
        Assert.That(document.RawBlocks[0].Family, Is.EqualTo(MetadataFamily.Thumbnail));
        Assert.That(document.RawBlocks[0].BlockType, Is.EqualTo("JPEG"));
        Assert.That(document.RawBlocks[0].Data, Is.Empty);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Thumbnail.IFD1.Format").Value.Text, Is.EqualTo("JPEG"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Thumbnail.IFD1.ByteLength").Value.UnsignedIntegerValue, Is.EqualTo(4));
    }

    [Test]
    public void Parse_ExtractsStripThumbnailFromIfd1()
    {
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.CaptureRawBlockData = true;

        ImageMetadataDocument document = ParseTiff(CreateStripThumbnailTiff(), options);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Profiles, Is.Empty);
        Assert.That(document.RawBlocks, Has.Count.EqualTo(2));
        Assert.That(document.RawBlocks[0].BlockType, Is.EqualTo("TIFFStrip"));
        Assert.That(document.RawBlocks[0].Offset, Is.EqualTo(84));
        Assert.That(document.RawBlocks[0].Length, Is.EqualTo(3));
        Assert.That(document.RawBlocks[0].Data, Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(document.RawBlocks[1].Offset, Is.EqualTo(87));
        Assert.That(document.RawBlocks[1].Length, Is.EqualTo(2));
        Assert.That(document.RawBlocks[1].Data, Is.EqualTo(new byte[] { 4, 5 }));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Thumbnail.IFD1.Width").Value.UnsignedIntegerValue, Is.EqualTo(16));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Thumbnail.IFD1.Height").Value.UnsignedIntegerValue, Is.EqualTo(8));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Thumbnail.IFD1.StripCount").Value.IntegerValue, Is.EqualTo(2));
    }

    [Test]
    public void Parse_ThumbnailOutsideStreamAddsDiagnostic()
    {
        byte[] bytes = new byte[]
        {
            73, 73, 42, 0, 8, 0, 0, 0,
            0, 0,
            14, 0, 0, 0,
            2, 0,
            1, 2, 4, 0, 1, 0, 0, 0, 100, 0, 0, 0,
            2, 2, 4, 0, 1, 0, 0, 0, 4, 0, 0, 0,
            0, 0, 0, 0
        };

        ImageMetadataDocument document = ParseTiff(bytes, new ImageMetadataReaderOptions());

        Assert.That(document.Profiles, Is.Empty);
        Assert.That(document.RawBlocks, Is.Empty);
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("tiff.thumbnail.truncated"));
        Assert.That(document.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.Truncated));
    }

    [Test]
    public void Parse_ThumbnailOverRawBlockLimitAddsDiagnosticAndSkipsCapture()
    {
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxRawBlockBytes = 3;

        ImageMetadataDocument document = ParseTiff(CreateJpegThumbnailTiff(), options);

        Assert.That(document.Profiles, Is.Empty);
        Assert.That(document.RawBlocks, Is.Empty);
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("tiff.thumbnail.limit"));
        Assert.That(document.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.LimitExceeded));
        Assert.That(document.Diagnostics[0].Offset, Is.EqualTo(44));
        Assert.That(document.Diagnostics[0].Length, Is.EqualTo(4));
    }

    [Test]
    public void Parse_NegativeRawBlockLimitStopsSafely()
    {
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxRawBlockBytes = -1;

        ImageMetadataDocument document = ParseTiff(CreateJpegThumbnailTiff(), options);

        Assert.That(document.Profiles, Is.Empty);
        Assert.That(document.RawBlocks, Is.Empty);
        Assert.That(document.Diagnostics, Has.Some.Matches<MetadataDiagnostic>(item => item.Code == "tiff.thumbnail.limit"));
    }

    [Test]
    public void Parse_CaptureRawBlocksFalseStillPreservesThumbnailProfile()
    {
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.CaptureRawBlocks = false;
        options.CaptureRawBlockData = true;

        ImageMetadataDocument document = ParseTiff(CreateJpegThumbnailTiff(), options);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Profiles, Has.Count.EqualTo(1));
        Assert.That(document.Profiles[0].Data, Is.EqualTo(new byte[] { 255, 216, 255, 217 }));
        Assert.That(document.RawBlocks, Is.Empty);
    }

    public static byte[] CreateJpegThumbnailTiff()
    {
        return new byte[]
        {
            73, 73, 42, 0, 8, 0, 0, 0,
            0, 0,
            14, 0, 0, 0,
            2, 0,
            1, 2, 4, 0, 1, 0, 0, 0, 44, 0, 0, 0,
            2, 2, 4, 0, 1, 0, 0, 0, 4, 0, 0, 0,
            0, 0, 0, 0,
            255, 216, 255, 217
        };
    }

    public static byte[] CreateStripThumbnailTiff()
    {
        List<byte> bytes = new List<byte>();
        bytes.Add(73);
        bytes.Add(73);
        AddUnsigned16(bytes, 42);
        AddUnsigned32(bytes, 8);

        AddUnsigned16(bytes, 0);
        AddUnsigned32(bytes, 14);

        AddUnsigned16(bytes, 4);
        AddEntry(bytes, 256, 4, 1, 16);
        AddEntry(bytes, 257, 4, 1, 8);
        AddEntry(bytes, 273, 4, 2, 68);
        AddEntry(bytes, 279, 4, 2, 76);
        AddUnsigned32(bytes, 0);

        AddUnsigned32(bytes, 84);
        AddUnsigned32(bytes, 87);
        AddUnsigned32(bytes, 3);
        AddUnsigned32(bytes, 2);
        bytes.Add(1);
        bytes.Add(2);
        bytes.Add(3);
        bytes.Add(4);
        bytes.Add(5);
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

    public static ImageMetadataDocument ParseTiff(byte[] bytes, ImageMetadataReaderOptions options)
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes);
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return TiffMetadataParser.Parse(reader, ImageFormatMapping.CreateInfo(ImageFormatKind.Tiff), options);
    }
}
