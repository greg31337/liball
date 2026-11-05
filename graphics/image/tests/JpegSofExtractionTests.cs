using System.Collections.Generic;
using System.IO;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class JpegSofExtractionTests
{
    [Test]
    public void Parse_BaselineSofExtractsFrameDimensionsAndComponentCount()
    {
        byte[] bytes = new byte[]
        {
            255, 216,
            255, 192, 0, 17,
            8, 1, 224, 2, 128, 3,
            1, 17, 0,
            2, 17, 1,
            3, 17, 1,
            255, 217
        };
        ImageMetadataDocument document = ParseJpeg(bytes);

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Jpeg));
        Assert.That(document.ByteLength, Is.EqualTo(bytes.Length));
        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(document.Frames[0].Width, Is.EqualTo(640));
        Assert.That(document.Frames[0].Height, Is.EqualTo(480));
        Assert.That(document.Frames[0].CanvasWidth, Is.EqualTo(640));
        Assert.That(document.Frames[0].CanvasHeight, Is.EqualTo(480));
        Assert.That(document.Frames[0].BitDepth, Is.EqualTo(8));
        Assert.That(document.Frames[0].ChannelCount, Is.EqualTo(3));
        Assert.That(document.Frames[0].Compression, Is.EqualTo(ImageCompressionKind.Jpeg));
        Assert.That(document.Frames[0].ColorSpace, Is.EqualTo(ImageColorSpaceKind.YCbCr));
        Assert.That(document.Frames[0].SourceColorType, Is.EqualTo("YCbCr"));
        Assert.That(document.Frames[0].IsProgressive, Is.False);
        Assert.That(document.Summary.Width, Is.EqualTo(640));
        Assert.That(document.Summary.Height, Is.EqualTo(480));
        Assert.That(document.Summary.FrameCount, Is.EqualTo(1));
    }

    [Test]
    public void Parse_SofDirectoryRecordsBaselineProcessTags()
    {
        byte[] bytes = new byte[]
        {
            255, 216,
            255, 192, 0, 11,
            8, 0, 16, 0, 32, 1,
            1, 17, 0,
            255, 217
        };
        ImageMetadataDocument document = ParseJpeg(bytes);
        MetadataDirectory directory = document.Frames[0].Directories[0];

        Assert.That(directory.Family, Is.EqualTo(MetadataFamily.Jpeg));
        Assert.That(directory.Group, Is.EqualTo("SOF0"));
        Assert.That(directory.Offset, Is.EqualTo(2));
        Assert.That(directory.Length, Is.EqualTo(13));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jpeg.SOF0.Width").Value.IntegerValue, Is.EqualTo(32));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jpeg.SOF0.Height").Value.IntegerValue, Is.EqualTo(16));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jpeg.SOF0.Precision").Value.IntegerValue, Is.EqualTo(8));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jpeg.SOF0.ComponentCount").Value.IntegerValue, Is.EqualTo(1));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jpeg.SOF0.Process").Value.Text, Is.EqualTo("Baseline DCT"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jpeg.SOF0.Progressive").Value.BooleanValue, Is.False);
    }

    [Test]
    public void Parse_ProgressiveSofRecordsProgressiveStatus()
    {
        byte[] bytes = new byte[]
        {
            255, 216,
            255, 194, 0, 17,
            8, 0, 200, 1, 44, 3,
            1, 17, 0,
            2, 17, 1,
            3, 17, 1,
            255, 217
        };
        ImageMetadataDocument document = ParseJpeg(bytes);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(document.Frames[0].Width, Is.EqualTo(300));
        Assert.That(document.Frames[0].Height, Is.EqualTo(200));
        Assert.That(document.Frames[0].Directories[0].Group, Is.EqualTo("SOF2"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jpeg.SOF2.Process").Value.Text, Is.EqualTo("Progressive DCT"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jpeg.SOF2.Progressive").Value.BooleanValue, Is.True);
        Assert.That(document.Frames[0].IsProgressive, Is.True);
        Assert.That(document.Summary.IsProgressive, Is.True);
    }

    [Test]
    public void Parse_TruncatedSofPayloadAddsDiagnosticAndNoFrame()
    {
        byte[] bytes = new byte[]
        {
            255, 216,
            255, 192, 0, 7,
            8, 0, 16, 0, 32,
            255, 217
        };
        ImageMetadataDocument document = ParseJpeg(bytes);

        Assert.That(document.Frames, Is.Empty);
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("jpeg.sof.truncated"));
        Assert.That(document.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.Truncated));
        Assert.That(document.IsCorrupt, Is.True);
        Assert.That(document.IsIncomplete, Is.True);
    }

    [Test]
    public void Parse_RespectsConfiguredFrameLimit()
    {
        byte[] bytes = new byte[]
        {
            255, 216,
            255, 192, 0, 11,
            8, 0, 16, 0, 32, 1,
            1, 17, 0,
            255, 192, 0, 11,
            8, 0, 8, 0, 8, 1,
            1, 17, 0,
            255, 217
        };
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes);
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxFrameCount = 1;

        ImageMetadataDocument document = JpegMetadataParser.Parse(reader, ImageFormatMapping.CreateInfo(ImageFormatKind.Jpeg), options);

        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("jpeg.frame.limit"));
        Assert.That(document.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.LimitExceeded));
    }

    [TestCase(192, "SOF0", "Baseline DCT", false)]
    [TestCase(193, "SOF1", "Extended Sequential DCT", false)]
    [TestCase(194, "SOF2", "Progressive DCT", true)]
    [TestCase(195, "SOF3", "Lossless", false)]
    [TestCase(197, "SOF5", "Differential Sequential DCT", false)]
    [TestCase(198, "SOF6", "Differential Progressive DCT", true)]
    [TestCase(199, "SOF7", "Differential Lossless", false)]
    [TestCase(201, "SOF9", "Extended Sequential Arithmetic", false)]
    [TestCase(202, "SOF10", "Progressive Arithmetic", true)]
    [TestCase(203, "SOF11", "Lossless Arithmetic", false)]
    [TestCase(205, "SOF13", "Differential Sequential Arithmetic", false)]
    [TestCase(206, "SOF14", "Differential Progressive Arithmetic", true)]
    [TestCase(207, "SOF15", "Differential Lossless Arithmetic", false)]
    public void Parse_AllSupportedSofMarkersReportTheirProcess(int marker, string group, string process, bool progressive)
    {
        byte[] bytes = BuildSingleComponentJpeg(marker);

        ImageMetadataDocument document = ParseJpeg(bytes);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(document.Frames[0].Directories[0].Group, Is.EqualTo(group));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jpeg." + group + ".Process").Value.Text, Is.EqualTo(process));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jpeg." + group + ".Progressive").Value.BooleanValue, Is.EqualTo(progressive));
    }

    [TestCase(1, ImageColorSpaceKind.Gray)]
    [TestCase(2, ImageColorSpaceKind.Unknown)]
    [TestCase(3, ImageColorSpaceKind.YCbCr)]
    [TestCase(4, ImageColorSpaceKind.Cmyk)]
    public void Parse_MapsComponentCountToFrameColorSpace(int componentCount, ImageColorSpaceKind expected)
    {
        byte[] bytes = BuildJpegWithComponents(componentCount);

        ImageMetadataDocument document = ParseJpeg(bytes);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Frames[0].ChannelCount, Is.EqualTo(componentCount));
        Assert.That(document.Frames[0].ColorSpace, Is.EqualTo(expected));
    }

    public static ImageMetadataDocument ParseJpeg(byte[] bytes)
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes);
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return JpegMetadataParser.Parse(reader, ImageFormatMapping.CreateInfo(ImageFormatKind.Jpeg), new ImageMetadataReaderOptions());
    }

    public static byte[] BuildSingleComponentJpeg(int marker)
    {
        return new byte[]
        {
            255, 216,
            255, (byte)marker, 0, 11,
            8, 0, 16, 0, 32, 1,
            1, 17, 0,
            255, 217
        };
    }

    public static byte[] BuildJpegWithComponents(int componentCount)
    {
        List<byte> bytes = new List<byte>();
        bytes.Add(255);
        bytes.Add(216);
        bytes.Add(255);
        bytes.Add(192);
        int payloadLength = 6 + (componentCount * 3);
        int declaredLength = payloadLength + 2;
        bytes.Add((byte)(declaredLength / 256));
        bytes.Add((byte)(declaredLength & 255));
        bytes.Add(8);
        bytes.Add(0);
        bytes.Add(16);
        bytes.Add(0);
        bytes.Add(32);
        bytes.Add((byte)componentCount);

        int component = 1;
        while (component <= componentCount)
        {
            bytes.Add((byte)component);
            bytes.Add(17);
            bytes.Add(0);
            component = component + 1;
        }

        bytes.Add(255);
        bytes.Add(217);
        return bytes.ToArray();
    }
}
