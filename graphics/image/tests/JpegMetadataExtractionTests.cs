using System.Collections.Generic;
using System.IO;
using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class JpegMetadataExtractionTests
{
    [Test]
    public void ReadStream_CompositeJpegCombinesMetadataFamiliesAndSummary()
    {
        byte[] bytes = BuildCompositeJpeg();

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Jpeg));
        Assert.That(document.Format.MimeType, Is.EqualTo("image/jpeg"));
        Assert.That(document.ByteLength, Is.EqualTo(bytes.Length));
        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(document.Frames[0].Width, Is.EqualTo(320));
        Assert.That(document.Frames[0].Height, Is.EqualTo(200));
        Assert.That(document.Summary.Width, Is.EqualTo(320));
        Assert.That(document.Summary.Height, Is.EqualTo(200));
        Assert.That(document.Summary.FrameCount, Is.EqualTo(1));
        Assert.That(document.Summary.HasColorProfile, Is.True);
        Assert.That(document.Profiles, Has.Count.EqualTo(6));
        Assert.That(document.Profiles, Has.Some.Matches<MetadataProfile>(profile => profile.Kind == MetadataProfileKind.Exif));
        Assert.That(document.Profiles, Has.Some.Matches<MetadataProfile>(profile => profile.Kind == MetadataProfileKind.Xmp));
        Assert.That(document.Profiles, Has.Some.Matches<MetadataProfile>(profile => profile.Kind == MetadataProfileKind.Icc));
        Assert.That(document.Profiles, Has.Some.Matches<MetadataProfile>(profile => profile.Kind == MetadataProfileKind.PhotoshopImageResource));
        Assert.That(document.Profiles, Has.Some.Matches<MetadataProfile>(profile => profile.Kind == MetadataProfileKind.Iptc));
        Assert.That(document.Profiles, Has.Some.Matches<MetadataProfile>(profile => profile.Key == "Jpeg.APP2.MPF"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jfif.APP0.Version").Value.Text, Is.EqualTo("1.02"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Exif.APP1.TiffByteOrder").Value.Text, Is.EqualTo("LittleEndian"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Xmp.APP1.Identifier").Value.Text, Is.EqualTo("XMP"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Icc.APP2.Identifier").Value.Text, Is.EqualTo("ICC_PROFILE"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Iptc.Application2.Byline").Value.Text, Is.EqualTo("Ada"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jpeg.APP14.ColorTransformName").Value.Text, Is.EqualTo("YCbCr"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jpeg.COM.Comment").Value.Text, Is.EqualTo("integration comment"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jpeg.APP2.MPF.DataByteLength").Value.IntegerValue, Is.EqualTo(3));
    }

    [Test]
    public void ReadStream_FromCurrentPositionUsesRelativeJpegOffsets()
    {
        byte[] jpeg = BuildSimpleJpeg();
        byte[] bytes = new byte[jpeg.Length + 5];
        int index = 0;
        while (index < jpeg.Length)
        {
            bytes[index + 5] = jpeg[index];
            index = index + 1;
        }

        MemoryStream stream = new MemoryStream(bytes);
        stream.Position = 5;

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(stream);

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Jpeg));
        Assert.That(document.ByteLength, Is.EqualTo(jpeg.Length));
        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Frames[0].Directories[0].Offset, Is.EqualTo(2));
        Assert.That(document.Frames[0].Width, Is.EqualTo(32));
        Assert.That(document.Frames[0].Height, Is.EqualTo(16));
    }

    [Test]
    public void ReadStream_NonSeekableJpegIsBufferedAndFullyExtracted()
    {
        byte[] bytes = BuildSimpleJpeg();
        NonSeekableReadStream stream = new NonSeekableReadStream(bytes);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(stream);

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Jpeg));
        Assert.That(document.ByteLength, Is.EqualTo(bytes.Length));
        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Summary.Width, Is.EqualTo(32));
        Assert.That(document.Summary.Height, Is.EqualTo(16));
    }

    [Test]
    public void Parse_NullReaderReturnsJpegDiagnosticDocument()
    {
        ImageMetadataDocument document = JpegMetadataParser.Parse(null, ImageFormatMapping.CreateInfo(ImageFormatKind.Jpeg), null);

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Jpeg));
        Assert.That(document.Frames, Is.Empty);
        Assert.That(document.Directories, Is.Empty);
        Assert.That(document.Profiles, Is.Empty);
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("jpeg.reader.null"));
        Assert.That(document.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.RecoverableError));
    }

    public static byte[] BuildCompositeJpeg()
    {
        List<byte> bytes = new List<byte>();
        bytes.Add(255);
        bytes.Add(216);

        AddSegment(bytes, 224, new byte[]
        {
            74, 70, 73, 70, 0,
            1, 2, 1,
            0, 72, 0, 72,
            0, 0
        });

        AddSegment(bytes, 225, new byte[]
        {
            69, 120, 105, 102, 0, 0,
            73, 73, 42, 0, 8, 0, 0, 0
        });

        byte[] xmpPacket = Encoding.UTF8.GetBytes("<x:xmpmeta>ok</x:xmpmeta>");
        AddSegment(bytes, 225, Combine(Encoding.ASCII.GetBytes(JpegXmpExtractor.StandardXmpIdentifier), xmpPacket));
        AddSegment(bytes, 226, JpegIccExtractorTests.CreateIccPayload(1, 1, new byte[] { 1, 2, 3, 4 }));

        byte[] iptc = new byte[] { 28, 2, 80, 0, 3, 65, 100, 97 };
        byte[] resource = JpegPhotoshopExtractorTests.BuildResource(1028, "", iptc);
        AddSegment(bytes, 237, JpegPhotoshopExtractorTests.BuildPhotoshopPayload(new byte[][] { resource }));

        AddSegment(bytes, 238, new byte[]
        {
            65, 100, 111, 98, 101,
            0, 100,
            0, 1,
            0, 2,
            1
        });

        AddSegment(bytes, 254, Encoding.UTF8.GetBytes("integration comment"));
        AddSegment(bytes, 226, Combine(Encoding.ASCII.GetBytes(JpegRawProfileExtractor.MpfIdentifier), new byte[] { 7, 8, 9 }));
        AddFrame(bytes, 192, 320, 200, 3);
        bytes.Add(255);
        bytes.Add(217);
        return bytes.ToArray();
    }

    public static byte[] BuildSimpleJpeg()
    {
        List<byte> bytes = new List<byte>();
        bytes.Add(255);
        bytes.Add(216);
        AddFrame(bytes, 192, 32, 16, 1);
        bytes.Add(255);
        bytes.Add(217);
        return bytes.ToArray();
    }

    public static void AddFrame(List<byte> bytes, int marker, int width, int height, int components)
    {
        List<byte> payload = new List<byte>();
        payload.Add(8);
        payload.Add((byte)(height / 256));
        payload.Add((byte)(height & 255));
        payload.Add((byte)(width / 256));
        payload.Add((byte)(width & 255));
        payload.Add((byte)components);

        int component = 1;
        while (component <= components)
        {
            payload.Add((byte)component);
            payload.Add(17);
            payload.Add(0);
            component = component + 1;
        }

        AddSegment(bytes, marker, payload.ToArray());
    }

    public static void AddSegment(List<byte> bytes, int marker, byte[] payload)
    {
        int declaredLength = payload.Length + 2;
        bytes.Add(255);
        bytes.Add((byte)marker);
        bytes.Add((byte)(declaredLength / 256));
        bytes.Add((byte)(declaredLength & 255));
        bytes.AddRange(payload);
    }

    public static byte[] Combine(byte[] first, byte[] second)
    {
        byte[] result = new byte[first.Length + second.Length];
        int index = 0;
        while (index < first.Length)
        {
            result[index] = first[index];
            index = index + 1;
        }

        int secondIndex = 0;
        while (secondIndex < second.Length)
        {
            result[index + secondIndex] = second[secondIndex];
            secondIndex = secondIndex + 1;
        }

        return result;
    }
}
