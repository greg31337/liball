using System.Collections.Generic;
using System.IO;
using System.Linq;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class QoiMetadataParserTests
{
    [Test]
    public void Parse_ExtractsRgbHeaderFrameAndPayloadLayout()
    {
        byte[] bytes = CreateQoi(800, 600, 3, 0, new byte[] { 254, 10, 20, 30 });
        MemoryStream stream = new MemoryStream(bytes);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(stream);

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Qoi));
        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.ByteLength, Is.EqualTo(bytes.Length));
        Assert.That(Find(document, "Qoi.Header.Magic").Value.Text, Is.EqualTo("qoif"));
        Assert.That(Find(document, "Qoi.Header.Width").Value.UnsignedIntegerValue, Is.EqualTo(800));
        Assert.That(Find(document, "Qoi.Header.Height").Value.UnsignedIntegerValue, Is.EqualTo(600));
        Assert.That(Find(document, "Qoi.Header.Channels").Value.IntegerValue, Is.EqualTo(3));
        Assert.That(Find(document, "Qoi.Header.Channels").DisplayValue, Is.EqualTo("RGB"));
        Assert.That(Find(document, "Qoi.Header.ColorSpace").Value.IntegerValue, Is.EqualTo(0));
        Assert.That(Find(document, "Qoi.Header.ColorSpace").DisplayValue, Is.EqualTo("sRGB with linear alpha"));
        Assert.That(Find(document, "Qoi.Header.PixelCount").Value.UnsignedIntegerValue, Is.EqualTo(480000));
        Assert.That(Find(document, "Qoi.Header.DecodedByteLength").Value.UnsignedIntegerValue, Is.EqualTo(1440000));
        Assert.That(Find(document, "Qoi.Header.DataOffset").Value.IntegerValue, Is.EqualTo(14));
        Assert.That(Find(document, "Qoi.Header.EncodedDataLength").Value.IntegerValue, Is.EqualTo(4));
        Assert.That(Find(document, "Qoi.Header.EndMarkerOffset").Value.IntegerValue, Is.EqualTo(18));
        Assert.That(document.Directories.Single().Path, Is.EqualTo("QOI/Header"));
        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(document.Frames[0].Width, Is.EqualTo(800));
        Assert.That(document.Frames[0].Height, Is.EqualTo(600));
        Assert.That(document.Frames[0].BitDepth, Is.EqualTo(8));
        Assert.That(document.Frames[0].ChannelCount, Is.EqualTo(3));
        Assert.That(document.Frames[0].HasAlpha, Is.False);
        Assert.That(document.Frames[0].ColorSpace, Is.EqualTo(ImageColorSpaceKind.Rgb));
        Assert.That(document.Frames[0].Compression, Is.EqualTo(ImageCompressionKind.Unknown));
        Assert.That(document.Frames[0].Orientation, Is.EqualTo(ImageOrientation.Normal));
        Assert.That(document.Summary.Width, Is.EqualTo(800));
        Assert.That(document.Summary.Height, Is.EqualTo(600));
        Assert.That(document.RawBlocks.Select(item => item.BlockType), Is.EqualTo(new[] { "Header", "EndMarker" }));
        Assert.That(stream.Position, Is.EqualTo(0));
    }

    [Test]
    public void Parse_ExtractsRgbaLinearHeader()
    {
        byte[] bytes = CreateQoi(37, 19, 4, 1, new byte[] { 255, 1, 2, 3, 4 });

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(Find(document, "Qoi.Header.Channels").DisplayValue, Is.EqualTo("RGBA"));
        Assert.That(Find(document, "Qoi.Header.ColorSpace").DisplayValue, Is.EqualTo("Linear color and alpha"));
        Assert.That(Find(document, "Qoi.Header.PixelCount").Value.UnsignedIntegerValue, Is.EqualTo(703));
        Assert.That(Find(document, "Qoi.Header.DecodedByteLength").Value.UnsignedIntegerValue, Is.EqualTo(2812));
        Assert.That(document.Frames[0].ChannelCount, Is.EqualTo(4));
        Assert.That(document.Frames[0].HasAlpha, Is.True);
    }

    [Test]
    public void Parse_DecodesBigEndianDimensions()
    {
        byte[] bytes = CreateQoi(66051, 258, 3, 0, new byte[] { 192 });

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(Find(document, "Qoi.Header.Width").Value.UnsignedIntegerValue, Is.EqualTo(66051));
        Assert.That(Find(document, "Qoi.Header.Height").Value.UnsignedIntegerValue, Is.EqualTo(258));
        Assert.That(document.Frames[0].Width, Is.EqualTo(66051));
        Assert.That(document.Frames[0].Height, Is.EqualTo(258));
    }

    [Test]
    public void Parse_TruncatedHeaderReportsDiagnostic()
    {
        byte[] bytes = new byte[] { 113, 111, 105, 102, 0, 0, 0, 1 };

        ImageMetadataDocument document = ParseDirect(bytes, new ImageMetadataReaderOptions());

        Assert.That(HasDiagnostic(document, "qoi.header.truncated"), Is.True);
        Assert.That(document.Directories, Is.Empty);
        Assert.That(document.Frames, Is.Empty);
        Assert.That(document.RawBlocks, Is.Empty);
    }

    [Test]
    public void Parse_InvalidMagicReportsDiagnostic()
    {
        byte[] bytes = CreateQoi(1, 1, 3, 0, new byte[] { 192 });
        bytes[3] = (byte)'x';

        ImageMetadataDocument document = ParseDirect(bytes, new ImageMetadataReaderOptions());

        Assert.That(HasDiagnostic(document, "qoi.magic.invalid"), Is.True);
        Assert.That(document.Directories, Is.Empty);
        Assert.That(document.Frames, Is.Empty);
    }

    [TestCase(0, 10)]
    [TestCase(10, 0)]
    public void Parse_ZeroDimensionsAreRejected(int width, int height)
    {
        byte[] bytes = CreateQoi(width, height, 3, 0, new byte[] { 192 });

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(HasDiagnostic(document, "qoi.dimensions.zero"), Is.True);
        Assert.That(document.Directories, Has.Count.EqualTo(1));
        Assert.That(document.Frames, Is.Empty);
    }

    [TestCase(0)]
    [TestCase(2)]
    [TestCase(5)]
    public void Parse_InvalidChannelCountsAreRejected(int channels)
    {
        byte[] bytes = CreateQoi(10, 10, channels, 0, new byte[] { 192 });

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(HasDiagnostic(document, "qoi.channels.invalid"), Is.True);
        Assert.That(Find(document, "Qoi.Header.Channels").DisplayValue, Is.EqualTo("Unknown"));
        Assert.That(document.Frames, Is.Empty);
    }

    [Test]
    public void Parse_InvalidColorSpaceIsRejected()
    {
        byte[] bytes = CreateQoi(10, 10, 3, 2, new byte[] { 192 });

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(HasDiagnostic(document, "qoi.colorspace.invalid"), Is.True);
        Assert.That(Find(document, "Qoi.Header.ColorSpace").DisplayValue, Is.EqualTo("Unknown"));
        Assert.That(document.Frames, Is.Empty);
    }

    [Test]
    public void Parse_PixelCountOverCanonicalLimitIsRejected()
    {
        byte[] bytes = CreateQoi(20001, 20000, 3, 0, new byte[] { 192 });

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(HasDiagnostic(document, "qoi.pixel-count.limit"), Is.True);
        Assert.That(Find(document, "Qoi.Header.PixelCount").Value.UnsignedIntegerValue, Is.EqualTo(400020000));
        Assert.That(document.Frames, Is.Empty);
    }

    [Test]
    public void Parse_DimensionOutsideFrameRangeIsRejectedWithoutOverflow()
    {
        byte[] bytes = CreateQoiUnsigned(4294967295, 4294967295, 4, 0, new byte[] { 192 });

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(HasDiagnostic(document, "qoi.dimensions.range"), Is.True);
        Assert.That(HasDiagnostic(document, "qoi.pixel-count.limit"), Is.True);
        Assert.That(Find(document, "Qoi.Header.PixelCount").Value.UnsignedIntegerValue, Is.EqualTo(18446744065119617025UL));
        Assert.That(Find(document, "Qoi.Header.DecodedByteLength").Value.UnsignedIntegerValue, Is.EqualTo(ulong.MaxValue));
        Assert.That(document.Frames, Is.Empty);
    }

    [Test]
    public void Parse_MissingEndMarkerPreservesHeaderAndFrame()
    {
        byte[] bytes = CreateHeader(20, 10, 3, 0);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(HasDiagnostic(document, "qoi.end-marker.truncated"), Is.True);
        Assert.That(Find(document, "Qoi.Header.EndMarkerOffset").Value.IntegerValue, Is.EqualTo(-1));
        Assert.That(Find(document, "Qoi.Header.EncodedDataLength").Value.IntegerValue, Is.EqualTo(0));
        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(document.RawBlocks.Select(item => item.BlockType), Is.EqualTo(new[] { "Header" }));
    }

    [Test]
    public void Parse_InvalidEndMarkerPreservesHeaderAndFrame()
    {
        byte[] bytes = CreateQoi(20, 10, 3, 0, new byte[] { 192 });
        bytes[bytes.Length - 1] = 2;

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(HasDiagnostic(document, "qoi.end-marker.invalid"), Is.True);
        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(document.RawBlocks.Select(item => item.BlockType), Is.EqualTo(new[] { "Header" }));
    }

    [Test]
    public void Parse_EmptyEncodedPayloadIsDiagnosed()
    {
        byte[] bytes = CreateQoi(1, 1, 3, 0, new byte[0]);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(HasDiagnostic(document, "qoi.data.empty"), Is.True);
        Assert.That(Find(document, "Qoi.Header.EncodedDataLength").Value.IntegerValue, Is.EqualTo(0));
        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(document.RawBlocks.Select(item => item.BlockType), Is.EqualTo(new[] { "Header", "EndMarker" }));
    }

    [Test]
    public void Parse_FileLimitCanExposeHeaderWithoutEndMarker()
    {
        byte[] bytes = CreateQoi(9, 7, 4, 1, new byte[] { 255, 1, 2, 3, 4 });
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxFileBytes = 14;

        ImageMetadataDocument document = new ImageMetadataReader().ReadStreamWithOptions(new MemoryStream(bytes), options);

        Assert.That(HasDiagnostic(document, "reader.file.limit"), Is.True);
        Assert.That(HasDiagnostic(document, "qoi.end-marker.truncated"), Is.True);
        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(document.Summary.Width, Is.EqualTo(9));
        Assert.That(document.Summary.Height, Is.EqualTo(7));
    }

    [Test]
    public void Parse_RawBlockCaptureHonorsOptionsAndLimit()
    {
        byte[] bytes = CreateQoi(2, 2, 4, 0, new byte[] { 255, 1, 2, 3, 4 });
        ImageMetadataReaderOptions captured = new ImageMetadataReaderOptions();
        captured.CaptureRawBlockData = true;

        ImageMetadataDocument capturedDocument = ParseDirect(bytes, captured);

        Assert.That(capturedDocument.RawBlocks.Single(item => item.BlockType == "Header").Data, Is.EqualTo(bytes.Take(14).ToArray()));
        Assert.That(capturedDocument.RawBlocks.Single(item => item.BlockType == "EndMarker").Data, Is.EqualTo(new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 }));

        ImageMetadataReaderOptions limited = new ImageMetadataReaderOptions();
        limited.CaptureRawBlockData = true;
        limited.MaxRawBlockBytes = 10;
        ImageMetadataDocument limitedDocument = ParseDirect(bytes, limited);
        Assert.That(limitedDocument.RawBlocks.Single(item => item.BlockType == "Header").Data, Is.Empty);
        Assert.That(limitedDocument.RawBlocks.Single(item => item.BlockType == "EndMarker").Data.Length, Is.EqualTo(8));
        Assert.That(HasDiagnostic(limitedDocument, "qoi.raw.block.limit"), Is.True);

        ImageMetadataReaderOptions disabled = new ImageMetadataReaderOptions();
        disabled.CaptureRawBlocks = false;
        Assert.That(ParseDirect(bytes, disabled).RawBlocks, Is.Empty);
    }

    [Test]
    public void Parse_NullReaderReportsDiagnostic()
    {
        ImageMetadataDocument document = QoiMetadataParser.Parse(null, ImageFormatMapping.CreateInfo(ImageFormatKind.Qoi), new ImageMetadataReaderOptions());

        Assert.That(HasDiagnostic(document, "qoi.reader.null"), Is.True);
        Assert.That(document.Frames, Is.Empty);
    }

    public static byte[] CreateQoi(int width, int height, int channels, int colorSpace, byte[] encoded)
    {
        return CreateQoiUnsigned((uint)width, (uint)height, channels, colorSpace, encoded);
    }

    public static byte[] CreateQoiUnsigned(uint width, uint height, int channels, int colorSpace, byte[] encoded)
    {
        byte[] header = CreateHeaderUnsigned(width, height, channels, colorSpace);
        byte[] bytes = new byte[header.Length + encoded.Length + 8];
        for (int index = 0; index < header.Length; index++)
        {
            bytes[index] = header[index];
        }
        for (int index = 0; index < encoded.Length; index++)
        {
            bytes[header.Length + index] = encoded[index];
        }
        bytes[bytes.Length - 1] = 1;
        return bytes;
    }

    public static byte[] CreateHeader(int width, int height, int channels, int colorSpace)
    {
        return CreateHeaderUnsigned((uint)width, (uint)height, channels, colorSpace);
    }

    public static byte[] CreateHeaderUnsigned(uint width, uint height, int channels, int colorSpace)
    {
        byte[] bytes = new byte[14];
        bytes[0] = (byte)'q';
        bytes[1] = (byte)'o';
        bytes[2] = (byte)'i';
        bytes[3] = (byte)'f';
        WriteUnsigned32BigEndian(bytes, 4, width);
        WriteUnsigned32BigEndian(bytes, 8, height);
        bytes[12] = (byte)channels;
        bytes[13] = (byte)colorSpace;
        return bytes;
    }

    public static void WriteUnsigned32BigEndian(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }

    public static ImageMetadataDocument ParseDirect(byte[] bytes, ImageMetadataReaderOptions options)
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes);
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return QoiMetadataParser.Parse(reader, ImageFormatMapping.CreateInfo(ImageFormatKind.Qoi), options);
    }

    public static MetadataTag Find(ImageMetadataDocument document, string key)
    {
        return RequiredMetadataQuery.FindFirst(document, key);
    }

    public static bool HasDiagnostic(ImageMetadataDocument document, string code)
    {
        return document.Diagnostics.Any(item => item.Code == code);
    }
}
