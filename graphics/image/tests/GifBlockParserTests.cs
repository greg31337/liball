using System.Collections.Generic;
using System.IO;
using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class GifBlockParserTests
{
    [Test]
    public void Parse_ReadsHeaderLogicalScreenAndGlobalColorTable()
    {
        List<byte> bytes = CreateHeader(320, 240, 241, 3, 49);
        AddBytes(bytes, new byte[]
        {
            0, 0, 0,
            255, 0, 0,
            0, 255, 0,
            0, 0, 255
        });
        bytes.Add(59);

        ImageMetadataDocument document = ParseGif(bytes.ToArray(), new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Directories, Has.Count.EqualTo(4));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Gif.Header.Signature").Value.Text, Is.EqualTo("GIF"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Gif.Header.Version").Value.Text, Is.EqualTo("89a"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Gif.LogicalScreen.Width").Value.IntegerValue, Is.EqualTo(320));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Gif.LogicalScreen.Height").Value.IntegerValue, Is.EqualTo(240));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Gif.LogicalScreen.ColorResolution").Value.IntegerValue, Is.EqualTo(8));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Gif.LogicalScreen.GlobalColorTableEntries").Value.IntegerValue, Is.EqualTo(4));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Gif.LogicalScreen.PixelAspectRatio").Value.DoubleValue, Is.EqualTo(1.0));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Gif.GlobalColorTable.EntryCount").Value.IntegerValue, Is.EqualTo(4));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Gif.GlobalColorTable.Entries").Value.Bytes, Is.EqualTo(new byte[]
        {
            0, 0, 0,
            255, 0, 0,
            0, 255, 0,
            0, 0, 255
        }));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Gif.Trailer.Present").Value.BooleanValue, Is.True);
    }

    [Test]
    public void Parse_ReadsImageDescriptorAndSubBlocksUsingGlobalPalette()
    {
        List<byte> bytes = CreateHeader(4, 3, 128, 0, 0);
        AddBytes(bytes, new byte[] { 0, 0, 0, 255, 255, 255 });
        AddImageDescriptor(bytes, 1, 1, 2, 2, 0);
        AddBytes(bytes, new byte[] { 2, 2, 68, 1, 1, 0, 0, 59 });

        ImageMetadataDocument document = ParseGif(bytes.ToArray(), new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(document.Frames[0].Width, Is.EqualTo(2));
        Assert.That(document.Frames[0].Height, Is.EqualTo(2));
        Assert.That(document.Frames[0].CanvasWidth, Is.EqualTo(4));
        Assert.That(document.Frames[0].CanvasHeight, Is.EqualTo(3));
        Assert.That(document.Frames[0].CanvasX, Is.EqualTo(1));
        Assert.That(document.Frames[0].CanvasY, Is.EqualTo(1));
        Assert.That(document.Frames[0].BitDepth, Is.EqualTo(1));
        Assert.That(document.Frames[0].ChannelCount, Is.EqualTo(1));
        Assert.That(document.Frames[0].Compression, Is.EqualTo(ImageCompressionKind.Lzw));
        Assert.That(document.Frames[0].ColorSpace, Is.EqualTo(ImageColorSpaceKind.Indexed));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Gif.ImageDescriptor.Left").Value.IntegerValue, Is.EqualTo(1));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Gif.ImageDescriptor.Top").Value.IntegerValue, Is.EqualTo(1));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Gif.ImageData.LzwMinimumCodeSize").Value.IntegerValue, Is.EqualTo(2));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Gif.ImageData.SubBlockCount").Value.IntegerValue, Is.EqualTo(2));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Gif.ImageData.CompressedByteLength").Value.IntegerValue, Is.EqualTo(3));
        Assert.That(document.Summary.Width, Is.EqualTo(2));
        Assert.That(document.Summary.Height, Is.EqualTo(2));
        Assert.That(document.Summary.FrameCount, Is.EqualTo(1));
    }

    [Test]
    public void Parse_ReadsLocalColorTableAndImageFlags()
    {
        List<byte> bytes = CreateHeader(8, 8, 0, 0, 0);
        AddImageDescriptor(bytes, 0, 0, 4, 4, 225);
        AddBytes(bytes, new byte[]
        {
            0, 0, 0,
            64, 64, 64,
            128, 128, 128,
            255, 255, 255,
            3, 0, 59
        });

        ImageMetadataDocument document = ParseGif(bytes.ToArray(), new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(document.Frames[0].BitDepth, Is.EqualTo(2));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Gif.ImageDescriptor.LocalColorTable").Value.BooleanValue, Is.True);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Gif.ImageDescriptor.Interlaced").Value.BooleanValue, Is.True);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Gif.ImageDescriptor.SortFlag").Value.BooleanValue, Is.True);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Gif.ImageDescriptor.LocalColorTableEntries").Value.IntegerValue, Is.EqualTo(4));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Gif.LocalColorTable.EntryCount").Value.IntegerValue, Is.EqualTo(4));
    }

    [Test]
    public void Parse_ReadsKnownAndUnknownExtensionStructures()
    {
        List<byte> bytes = CreateHeader(1, 1, 0, 0, 0);
        AddBytes(bytes, new byte[] { 33, 249, 4, 0, 0, 0, 0, 0 });
        AddBytes(bytes, new byte[] { 33, 254, 3, 97, 98, 99, 2, 100, 101, 0 });
        AddBytes(bytes, new byte[] { 33, 1, 12 });
        AddBytes(bytes, new byte[12]);
        AddBytes(bytes, new byte[] { 1, 120, 0 });
        AddBytes(bytes, new byte[] { 33, 255, 11 });
        AddBytes(bytes, Encoding.ASCII.GetBytes("NETSCAPE2.0"));
        AddBytes(bytes, new byte[] { 3, 1, 0, 0, 0 });
        AddBytes(bytes, new byte[] { 33, 240, 2, 1, 2, 0, 59 });

        ImageMetadataDocument document = ParseGif(bytes.ToArray(), new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        AssertExtension(document, "GraphicControlExtension", 249, 4, 0, 0, 0);
        AssertExtension(document, "CommentExtension", 254, 0, 2, 5, 0);
        AssertExtension(document, "PlainTextExtension", 1, 12, 1, 1, 0);
        AssertExtension(document, "ApplicationExtension", 255, 11, 1, 3, 0);
        AssertExtension(document, "Extension", 240, 0, 1, 2, 0);
    }

    [Test]
    public void Parse_AssignsZeroBasedIndexesToRepeatedExtensions()
    {
        List<byte> bytes = CreateHeader(1, 1, 0, 0, 0);
        AddBytes(bytes, new byte[] { 33, 254, 1, 97, 0, 33, 254, 1, 98, 0, 59 });

        ImageMetadataDocument document = ParseGif(bytes.ToArray(), new ImageMetadataReaderOptions());

        List<MetadataDirectory> directories = FindDirectories(document, "CommentExtension");
        Assert.That(directories, Has.Count.EqualTo(2));
        Assert.That(directories[0].Path, Is.EqualTo("GIF/Extension/CommentExtension/0"));
        Assert.That(directories[0].RepeatIndex, Is.EqualTo(0));
        Assert.That(directories[1].Path, Is.EqualTo("GIF/Extension/CommentExtension/1"));
        Assert.That(directories[1].RepeatIndex, Is.EqualTo(1));
    }

    [Test]
    public void Parse_ReportsTruncatedColorTableAndSubBlockPayloads()
    {
        List<byte> colorTable = CreateHeader(1, 1, 129, 0, 0);
        AddBytes(colorTable, new byte[] { 0, 0, 0 });

        ImageMetadataDocument truncatedTable = ParseGif(colorTable.ToArray(), new ImageMetadataReaderOptions());

        Assert.That(truncatedTable.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(truncatedTable.Diagnostics[0].Code, Is.EqualTo("gif.color-table.truncated"));
        Assert.That(truncatedTable.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.Truncated));

        List<byte> extension = CreateHeader(1, 1, 0, 0, 0);
        AddBytes(extension, new byte[] { 33, 254, 4, 1, 2 });

        ImageMetadataDocument truncatedSubBlock = ParseGif(extension.ToArray(), new ImageMetadataReaderOptions());

        Assert.That(truncatedSubBlock.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(truncatedSubBlock.Diagnostics[0].Code, Is.EqualTo("gif.sub-block.data.truncated"));
        Assert.That(truncatedSubBlock.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.Truncated));
    }

    [Test]
    public void Parse_ReportsInvalidDimensionsImageBoundsCodeSizeAndTrailingData()
    {
        List<byte> bytes = CreateHeader(2, 2, 0, 0, 0);
        AddImageDescriptor(bytes, 1, 1, 2, 0, 0);
        AddBytes(bytes, new byte[] { 1, 0, 59, 99 });

        ImageMetadataDocument document = ParseGif(bytes.ToArray(), new ImageMetadataReaderOptions());

        AssertDiagnostic(document, "gif.image.dimensions.invalid", MetadataSeverity.RecoverableError);
        AssertDiagnostic(document, "gif.image.bounds.invalid", MetadataSeverity.RecoverableError);
        AssertDiagnostic(document, "gif.image-data.code-size.invalid", MetadataSeverity.RecoverableError);
        AssertDiagnostic(document, "gif.trailing.data", MetadataSeverity.Warning);
    }

    [Test]
    public void Parse_EnforcesSubBlockByteLimit()
    {
        List<byte> bytes = CreateHeader(1, 1, 0, 0, 0);
        AddBytes(bytes, new byte[] { 33, 254, 3, 1, 2, 3, 0, 59 });
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxSegmentBytes = 2;

        ImageMetadataDocument document = ParseGif(bytes.ToArray(), options);

        AssertDiagnostic(document, "gif.sub-block.data.limit", MetadataSeverity.LimitExceeded);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Gif.Extension.DataByteLength").Value.IntegerValue, Is.EqualTo(3));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Gif.Trailer.Present"), Is.Not.Null);
    }

    public static void AssertExtension(ImageMetadataDocument document, string group, int label, int headerLength, int blockCount, int dataLength, int repeatIndex)
    {
        MetadataDirectory directory = FindDirectories(document, group)[repeatIndex];
        Assert.That(FindTag(directory, "Gif.Extension.Label").Value.IntegerValue, Is.EqualTo(label));
        Assert.That(FindTag(directory, "Gif.Extension.HeaderByteLength").Value.IntegerValue, Is.EqualTo(headerLength));
        Assert.That(FindTag(directory, "Gif.Extension.DataSubBlockCount").Value.IntegerValue, Is.EqualTo(blockCount));
        Assert.That(FindTag(directory, "Gif.Extension.DataByteLength").Value.IntegerValue, Is.EqualTo(dataLength));
    }

    public static void AssertDiagnostic(ImageMetadataDocument document, string code, MetadataSeverity severity)
    {
        Assert.That(document.Diagnostics, Has.Some.Matches<MetadataDiagnostic>(diagnostic => diagnostic.Code == code && diagnostic.Severity == severity));
    }

    public static List<MetadataDirectory> FindDirectories(ImageMetadataDocument document, string group)
    {
        List<MetadataDirectory> matches = new List<MetadataDirectory>();
        foreach (MetadataDirectory directory in document.Directories)
        {
            if (directory.Group == group)
            {
                matches.Add(directory);
            }
        }

        return matches;
    }

    public static MetadataTag FindTag(MetadataDirectory directory, string key)
    {
        foreach (MetadataTag tag in directory.Tags)
        {
            if (tag.Key == key)
            {
                return tag;
            }
        }

        throw new System.InvalidOperationException("Expected metadata tag was not found: " + key);
    }

    public static List<byte> CreateHeader(int width, int height, int packed, int background, int aspect)
    {
        List<byte> bytes = new List<byte>();
        AddBytes(bytes, Encoding.ASCII.GetBytes("GIF89a"));
        AddUnsigned16(bytes, width);
        AddUnsigned16(bytes, height);
        bytes.Add((byte)packed);
        bytes.Add((byte)background);
        bytes.Add((byte)aspect);
        return bytes;
    }

    public static void AddImageDescriptor(List<byte> bytes, int left, int top, int width, int height, int packed)
    {
        bytes.Add(44);
        AddUnsigned16(bytes, left);
        AddUnsigned16(bytes, top);
        AddUnsigned16(bytes, width);
        AddUnsigned16(bytes, height);
        bytes.Add((byte)packed);
    }

    public static void AddUnsigned16(List<byte> bytes, int value)
    {
        bytes.Add((byte)(value & 255));
        bytes.Add((byte)((value >> 8) & 255));
    }

    public static void AddBytes(List<byte> bytes, byte[] values)
    {
        foreach (byte value in values)
        {
            bytes.Add(value);
        }
    }

    public static ImageMetadataDocument ParseGif(byte[] bytes, ImageMetadataReaderOptions options)
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes);
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return GifMetadataParser.Parse(reader, ImageFormatMapping.CreateInfo(ImageFormatKind.Gif), options);
    }
}
