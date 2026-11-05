using System.Collections.Generic;
using System.IO;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class IcoMetadataParserTests
{
    [Test]
    public void Parse_IconDirectoryExtractsEmbeddedPngAndDibItems()
    {
        byte[] png = CreatePng(32, 24, 8, 6);
        byte[] dib = CreateDib(16, 16, 32);
        byte[] bytes = CreateContainer(1, new byte[][] { png, dib }, new int[] { 32, 16 }, new int[] { 24, 16 }, new int[] { 32, 32 });

        ImageMetadataDocument document = Parse(bytes, ImageFormatKind.Ico, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Ico));
        Assert.That(document.ByteLength, Is.EqualTo(bytes.Length));
        Assert.That(document.Directories, Has.Count.EqualTo(1));
        Assert.That(document.Directories[0].Path, Is.EqualTo("ICO/Header"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Ico.Header.Type").DisplayValue, Is.EqualTo("Icon"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Ico.Header.Count").Value.IntegerValue, Is.EqualTo(2));
        Assert.That(document.Frames, Has.Count.EqualTo(2));

        Assert.That(document.Frames[0].Index, Is.EqualTo(0));
        Assert.That(document.Frames[0].Role, Is.EqualTo(ImageMetadataRole.Primary));
        Assert.That(document.Frames[0].Width, Is.EqualTo(32));
        Assert.That(document.Frames[0].Height, Is.EqualTo(24));
        Assert.That(document.Frames[0].BitDepth, Is.EqualTo(8));
        Assert.That(document.Frames[0].ChannelCount, Is.EqualTo(4));
        Assert.That(document.Frames[0].HasAlpha, Is.True);
        Assert.That(document.Frames[0].Compression, Is.EqualTo(ImageCompressionKind.Deflate));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Ico.Entry.Encoding").Value.Text, Is.EqualTo("PNG"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Png.IHDR.Width").Value.UnsignedIntegerValue, Is.EqualTo(32));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Png.IHDR.Height").Value.UnsignedIntegerValue, Is.EqualTo(24));

        Assert.That(document.Frames[1].Index, Is.EqualTo(1));
        Assert.That(document.Frames[1].Role, Is.EqualTo(ImageMetadataRole.Page));
        Assert.That(document.Frames[1].Width, Is.EqualTo(16));
        Assert.That(document.Frames[1].Height, Is.EqualTo(16));
        Assert.That(document.Frames[1].BitDepth, Is.EqualTo(32));
        Assert.That(document.Frames[1].ChannelCount, Is.EqualTo(4));
        Assert.That(document.Frames[1].HasAlpha, Is.True);
        Assert.That(document.Frames[1].Compression, Is.EqualTo(ImageCompressionKind.None));
        Assert.That(new ImageMetadataQuery().Find(document, "Ico.Entry.Encoding")[1].Value.Text, Is.EqualTo("DIB"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmp.DIBHeader.Width").Value.IntegerValue, Is.EqualTo(16));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmp.DIBHeader.Height").Value.IntegerValue, Is.EqualTo(32));
        Assert.That(document.Summary.Width, Is.EqualTo(32));
        Assert.That(document.Summary.Height, Is.EqualTo(24));
        Assert.That(document.Summary.FrameCount, Is.EqualTo(2));
    }

    [Test]
    public void Parse_CursorEntryExtractsHotspotAndUsesCurFormat()
    {
        byte[] png = CreatePng(24, 24, 8, 6);
        byte[] bytes = CreateContainer(2, new byte[][] { png }, new int[] { 24 }, new int[] { 24 }, new int[] { 0 });
        WriteUnsigned16(bytes, 10, 7);
        WriteUnsigned16(bytes, 12, 9);

        ImageMetadataDocument document = Parse(bytes, ImageFormatKind.Ico, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Cur));
        Assert.That(document.Format.Name, Is.EqualTo("CUR"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Ico.Header.Type").DisplayValue, Is.EqualTo("Cursor"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Cur.Entry.HotspotX").Value.IntegerValue, Is.EqualTo(7));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Cur.Entry.HotspotY").Value.IntegerValue, Is.EqualTo(9));
        Assert.That(new ImageMetadataQuery().FindFirst(document, "Ico.Entry.BitCount"), Is.Null);
    }

    [Test]
    public void Parse_ZeroDimensionBytesRepresent256Pixels()
    {
        byte[] png = CreatePng(256, 256, 8, 6);
        byte[] bytes = CreateContainer(1, new byte[][] { png }, new int[] { 256 }, new int[] { 256 }, new int[] { 32 });

        ImageMetadataDocument document = Parse(bytes, ImageFormatKind.Ico, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Frames[0].Width, Is.EqualTo(256));
        Assert.That(document.Frames[0].Height, Is.EqualTo(256));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Ico.Entry.Width").Value.IntegerValue, Is.EqualTo(256));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Ico.Entry.Height").Value.IntegerValue, Is.EqualTo(256));
    }

    [Test]
    public void Parse_EmbeddedMetadataOffsetsAreShiftedToContainerPositions()
    {
        byte[] png = CreatePng(8, 8, 8, 6);
        byte[] dib = CreateDib(4, 4, 24);
        byte[] bytes = CreateContainer(1, new byte[][] { png, dib }, new int[] { 8, 4 }, new int[] { 8, 4 }, new int[] { 32, 24 });
        long pngOffset = 38;
        long dibOffset = pngOffset + png.Length;

        ImageMetadataDocument document = Parse(bytes, ImageFormatKind.Ico, new ImageMetadataReaderOptions());

        MetadataTag pngWidth = RequiredMetadataQuery.FindFirst(document, "Png.IHDR.Width");
        MetadataTag dibWidth = RequiredMetadataQuery.FindFirst(document, "Bmp.DIBHeader.Width");
        Assert.That(pngWidth.Offset, Is.EqualTo(pngOffset + 16));
        Assert.That(dibWidth.Offset, Is.EqualTo(dibOffset + 4));
        Assert.That(FindDirectory(document.Frames[0], MetadataFamily.Png, "IHDR").Offset, Is.EqualTo(pngOffset + 8));
        Assert.That(FindDirectory(document.Frames[1], MetadataFamily.Bmp, "DIBHeader").Offset, Is.EqualTo(dibOffset));
    }

    [Test]
    public void Parse_DibDimensionMismatchWarnsButDirectoryEntryControlsFrame()
    {
        byte[] dib = CreateDib(20, 10, 24);
        byte[] bytes = CreateContainer(1, new byte[][] { dib }, new int[] { 16 }, new int[] { 16 }, new int[] { 24 });

        ImageMetadataDocument document = Parse(bytes, ImageFormatKind.Ico, new ImageMetadataReaderOptions());

        Assert.That(document.Frames[0].Width, Is.EqualTo(16));
        Assert.That(document.Frames[0].Height, Is.EqualTo(16));
        AssertDiagnostic(document, "ico.dib.dimensions.mismatch", MetadataSeverity.Warning);
    }

    [Test]
    public void Parse_EmbeddedDibRetainsExternalMasksAndPaletteMetadata()
    {
        byte[] maskedDib = new byte[52];
        WriteUnsigned32(maskedDib, 0, 40);
        WriteSigned32(maskedDib, 4, 16);
        WriteSigned32(maskedDib, 8, 32);
        WriteUnsigned16(maskedDib, 12, 1);
        WriteUnsigned16(maskedDib, 14, 16);
        WriteUnsigned32(maskedDib, 16, BmpMetadataExtractor.BiBitfields);
        WriteUnsigned32(maskedDib, 40, 0x00007c00);
        WriteUnsigned32(maskedDib, 44, 0x000003e0);
        WriteUnsigned32(maskedDib, 48, 0x0000001f);

        byte[] paletteDib = new byte[48];
        WriteUnsigned32(paletteDib, 0, 40);
        WriteSigned32(paletteDib, 4, 8);
        WriteSigned32(paletteDib, 8, 16);
        WriteUnsigned16(paletteDib, 12, 1);
        WriteUnsigned16(paletteDib, 14, 8);
        WriteUnsigned32(paletteDib, 32, 2);
        paletteDib[40] = 3;
        paletteDib[41] = 2;
        paletteDib[42] = 1;
        paletteDib[44] = 30;
        paletteDib[45] = 20;
        paletteDib[46] = 10;

        byte[] bytes = CreateContainer(1, new byte[][] { maskedDib, paletteDib }, new int[] { 16, 8 }, new int[] { 16, 8 }, new int[] { 16, 8 });
        ImageMetadataDocument document = Parse(bytes, ImageFormatKind.Ico, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmp.DIBHeader.RedMask").Value.UnsignedIntegerValue, Is.EqualTo(0x00007c00));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmp.DIBHeader.GreenMask").Value.UnsignedIntegerValue, Is.EqualTo(0x000003e0));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmp.DIBHeader.BlueMask").Value.UnsignedIntegerValue, Is.EqualTo(0x0000001f));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmp.Palette.EntryCount").Value.UnsignedIntegerValue, Is.EqualTo(2));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmp.Palette.Entries").Value.Bytes, Is.EqualTo(new byte[] { 3, 2, 1, 0, 30, 20, 10, 0 }));
    }

    [Test]
    public void Parse_EmbeddedDibRetainsV5IccProfile()
    {
        byte[] profile = BmpMetadataExtractorTests.CreateIccProfile(128);
        byte[] dib = new byte[124 + profile.Length];
        WriteUnsigned32(dib, 0, 124);
        WriteSigned32(dib, 4, 16);
        WriteSigned32(dib, 8, 32);
        WriteUnsigned16(dib, 12, 1);
        WriteUnsigned16(dib, 14, 32);
        WriteUnsigned32(dib, 56, BmpMetadataExtractor.ProfileEmbedded);
        WriteUnsigned32(dib, 108, 4);
        WriteUnsigned32(dib, 112, 124);
        WriteUnsigned32(dib, 116, (uint)profile.Length);
        System.Array.Copy(profile, 0, dib, 124, profile.Length);
        byte[] bytes = CreateContainer(1, new byte[][] { dib }, new int[] { 16 }, new int[] { 16 }, new int[] { 32 });

        ImageMetadataDocument document = Parse(bytes, ImageFormatKind.Ico, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Profiles, Has.Count.EqualTo(1));
        Assert.That(document.Profiles[0].Kind, Is.EqualTo(MetadataProfileKind.Icc));
        Assert.That(document.Profiles[0].Offset, Is.EqualTo(22 + 124));
        Assert.That(document.Profiles[0].Data, Is.EqualTo(profile));
        Assert.That(document.Summary.HasColorProfile, Is.True);
    }

    [Test]
    public void Parse_PngDimensionMismatchWarnsButDirectoryEntryControlsFrame()
    {
        byte[] png = CreatePng(20, 10, 8, 6);
        byte[] bytes = CreateContainer(1, new byte[][] { png }, new int[] { 16 }, new int[] { 16 }, new int[] { 32 });

        ImageMetadataDocument document = Parse(bytes, ImageFormatKind.Ico, new ImageMetadataReaderOptions());

        Assert.That(document.Frames[0].Width, Is.EqualTo(16));
        Assert.That(document.Frames[0].Height, Is.EqualTo(16));
        AssertDiagnostic(document, "ico.png.dimensions.mismatch", MetadataSeverity.Warning);
    }

    [Test]
    public void Parse_TruncatedDirectoryStopsBeforeCreatingFrames()
    {
        byte[] bytes = new byte[] { 0, 0, 1, 0, 2, 0, 16, 16, 0, 0 };

        ImageMetadataDocument document = Parse(bytes, ImageFormatKind.Ico, new ImageMetadataReaderOptions());

        Assert.That(document.Frames, Is.Empty);
        Assert.That(document.Directories, Has.Count.EqualTo(1));
        AssertDiagnostic(document, "ico.directory.truncated", MetadataSeverity.Truncated);
    }

    [Test]
    public void Parse_ItemOutsideStreamReportsRangeAndKeepsEntryFrame()
    {
        byte[] bytes = CreateContainer(1, new byte[][] { CreateDib(16, 16, 32) }, new int[] { 16 }, new int[] { 16 }, new int[] { 32 });
        WriteUnsigned32(bytes, 18, 1000);

        ImageMetadataDocument document = Parse(bytes, ImageFormatKind.Ico, new ImageMetadataReaderOptions());

        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(document.Frames[0].Directories, Has.Count.EqualTo(1));
        AssertDiagnostic(document, "ico.item.outside", MetadataSeverity.Truncated);
    }

    [Test]
    public void Parse_ItemInsideDirectoryIsRejected()
    {
        byte[] bytes = CreateContainer(1, new byte[][] { CreateDib(16, 16, 32) }, new int[] { 16 }, new int[] { 16 }, new int[] { 32 });
        WriteUnsigned32(bytes, 18, 6);

        ImageMetadataDocument document = Parse(bytes, ImageFormatKind.Ico, new ImageMetadataReaderOptions());

        AssertDiagnostic(document, "ico.item.inside-directory", MetadataSeverity.RecoverableError);
        Assert.That(new ImageMetadataQuery().FindFirst(document, "Ico.Entry.Encoding"), Is.Null);
    }

    [Test]
    public void Parse_FrameLimitProcessesOnlyConfiguredNumberOfItems()
    {
        byte[] first = CreateDib(16, 16, 32);
        byte[] second = CreateDib(32, 32, 32);
        byte[] bytes = CreateContainer(1, new byte[][] { first, second }, new int[] { 16, 32 }, new int[] { 16, 32 }, new int[] { 32, 32 });
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxFrameCount = 1;

        ImageMetadataDocument document = Parse(bytes, ImageFormatKind.Ico, options);

        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(document.Frames[0].Width, Is.EqualTo(16));
        AssertDiagnostic(document, "ico.frame.limit", MetadataSeverity.LimitExceeded);
    }

    [Test]
    public void Parse_ItemLimitStopsEmbeddedParsing()
    {
        byte[] dib = CreateDib(16, 16, 32);
        byte[] bytes = CreateContainer(1, new byte[][] { dib }, new int[] { 16 }, new int[] { 16 }, new int[] { 32 });
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxSegmentBytes = 20;

        ImageMetadataDocument document = Parse(bytes, ImageFormatKind.Ico, options);

        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(document.Frames[0].Directories, Has.Count.EqualTo(1));
        AssertDiagnostic(document, "ico.item.limit", MetadataSeverity.LimitExceeded);
    }

    [Test]
    public void Parse_RawCaptureIncludesHeaderItemAndEmbeddedDib()
    {
        byte[] dib = CreateDib(16, 16, 32);
        byte[] bytes = CreateContainer(1, new byte[][] { dib }, new int[] { 16 }, new int[] { 16 }, new int[] { 32 });
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.CaptureRawBlockData = true;

        ImageMetadataDocument document = Parse(bytes, ImageFormatKind.Ico, options);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.RawBlocks, Has.Count.EqualTo(3));
        Assert.That(document.RawBlocks[0].BlockType, Is.EqualTo("Header"));
        Assert.That(document.RawBlocks[0].Data, Is.EqualTo(new byte[] { 0, 0, 1, 0, 1, 0 }));
        Assert.That(document.RawBlocks[1].BlockType, Is.EqualTo("Item"));
        Assert.That(document.RawBlocks[1].Data, Is.EqualTo(dib));
        Assert.That(document.RawBlocks[2].BlockType, Is.EqualTo("DIBHeader"));
        Assert.That(document.RawBlocks[2].Offset, Is.EqualTo(22));
    }

    [Test]
    public void Parse_InvalidEmbeddedPngDiagnosticUsesContainerOffset()
    {
        byte[] png = CreatePng(16, 16, 8, 6);
        png[29] = (byte)(png[29] + 1);
        byte[] bytes = CreateContainer(1, new byte[][] { png }, new int[] { 16 }, new int[] { 16 }, new int[] { 32 });

        ImageMetadataDocument document = Parse(bytes, ImageFormatKind.Ico, new ImageMetadataReaderOptions());

        MetadataDiagnostic diagnostic = FindDiagnostic(document, "png.crc.mismatch");
        Assert.That(diagnostic, Is.Not.Null);
        Assert.That(diagnostic.Offset, Is.EqualTo(30));
    }

    [Test]
    public void Parse_InvalidTypeStopsWithDiagnostic()
    {
        byte[] bytes = new byte[] { 0, 0, 3, 0, 1, 0 };

        ImageMetadataDocument document = Parse(bytes, ImageFormatKind.Ico, new ImageMetadataReaderOptions());

        Assert.That(document.Frames, Is.Empty);
        Assert.That(document.Directories, Is.Empty);
        AssertDiagnostic(document, "ico.type.invalid", MetadataSeverity.RecoverableError);
    }

    [Test]
    public void CreateSyntheticBmp_ClampsOverflowingPixelOffset()
    {
        byte[] bytes = IcoMetadataParser.CreateSyntheticBmp(new byte[4], uint.MaxValue);

        Assert.That(BmpHeaderParser.ReadUnsigned32(bytes, 10), Is.EqualTo(bytes.Length));
    }

    public static ImageMetadataDocument Parse(byte[] bytes, ImageFormatKind kind, ImageMetadataReaderOptions options)
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes);
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return IcoMetadataParser.Parse(reader, ImageFormatMapping.CreateInfo(kind), options);
    }

    public static byte[] CreateContainer(int type, byte[][] payloads, int[] widths, int[] heights, int[] bitDepths)
    {
        int directoryLength = 6 + payloads.Length * 16;
        int totalLength = directoryLength;
        foreach (byte[] payload in payloads)
        {
            totalLength = totalLength + payload.Length;
        }

        byte[] bytes = new byte[totalLength];
        WriteUnsigned16(bytes, 2, (ushort)type);
        WriteUnsigned16(bytes, 4, (ushort)payloads.Length);
        int dataOffset = directoryLength;
        int index = 0;
        while (index < payloads.Length)
        {
            int entryOffset = 6 + index * 16;
            bytes[entryOffset] = widths[index] == 256 ? (byte)0 : (byte)widths[index];
            bytes[entryOffset + 1] = heights[index] == 256 ? (byte)0 : (byte)heights[index];
            bytes[entryOffset + 2] = 0;
            bytes[entryOffset + 3] = 0;
            if (type == 1)
            {
                WriteUnsigned16(bytes, entryOffset + 4, 1);
                WriteUnsigned16(bytes, entryOffset + 6, (ushort)bitDepths[index]);
            }

            WriteUnsigned32(bytes, entryOffset + 8, (uint)payloads[index].Length);
            WriteUnsigned32(bytes, entryOffset + 12, (uint)dataOffset);
            System.Array.Copy(payloads[index], 0, bytes, dataOffset, payloads[index].Length);
            dataOffset = dataOffset + payloads[index].Length;
            index = index + 1;
        }

        return bytes;
    }

    public static byte[] CreatePng(uint width, uint height, int bitDepth, int colorType)
    {
        return PngChunkTraverserTests.CreatePng(
            new string[] { "IHDR", "IEND" },
            new byte[][] { PngIhdrExtractorTests.CreateIhdr(width, height, bitDepth, colorType, 0, 0, 0), new byte[0] });
    }

    public static byte[] CreateDib(int width, int height, int bitDepth)
    {
        byte[] dib = new byte[40];
        WriteUnsigned32(dib, 0, 40);
        WriteSigned32(dib, 4, width);
        WriteSigned32(dib, 8, height * 2);
        WriteUnsigned16(dib, 12, 1);
        WriteUnsigned16(dib, 14, (ushort)bitDepth);
        WriteUnsigned32(dib, 16, 0);
        return dib;
    }

    public static MetadataDirectory FindDirectory(ImageMetadataFrame frame, MetadataFamily family, string group)
    {
        foreach (MetadataDirectory directory in frame.Directories)
        {
            if (directory.Family == family && directory.Group == group)
            {
                return directory;
            }
        }

        throw new System.InvalidOperationException("Expected metadata directory was not found: " + group);
    }

    public static MetadataDiagnostic FindDiagnostic(ImageMetadataDocument document, string code)
    {
        foreach (MetadataDiagnostic diagnostic in document.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return diagnostic;
            }
        }

        throw new System.InvalidOperationException("Expected diagnostic was not found: " + code);
    }

    public static void AssertDiagnostic(ImageMetadataDocument document, string code, MetadataSeverity severity)
    {
        MetadataDiagnostic diagnostic = FindDiagnostic(document, code);
        Assert.That(diagnostic, Is.Not.Null);
        Assert.That(diagnostic.Severity, Is.EqualTo(severity));
    }

    public static void WriteUnsigned16(byte[] data, int offset, ushort value)
    {
        data[offset] = (byte)value;
        data[offset + 1] = (byte)(value >> 8);
    }

    public static void WriteUnsigned32(byte[] data, int offset, uint value)
    {
        data[offset] = (byte)value;
        data[offset + 1] = (byte)(value >> 8);
        data[offset + 2] = (byte)(value >> 16);
        data[offset + 3] = (byte)(value >> 24);
    }

    public static void WriteSigned32(byte[] data, int offset, int value)
    {
        WriteUnsigned32(data, offset, (uint)value);
    }
}
