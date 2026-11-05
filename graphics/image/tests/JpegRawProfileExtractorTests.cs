using System.Collections.Generic;
using System.IO;
using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class JpegRawProfileExtractorTests
{
    [Test]
    public void Parse_App2MpfAndFlashPixAreCapturedAsRawProfiles()
    {
        byte[] mpf = Combine(Encoding.ASCII.GetBytes(JpegRawProfileExtractor.MpfIdentifier), new byte[] { 1, 2 });
        byte[] flashPix = Combine(Encoding.ASCII.GetBytes(JpegRawProfileExtractor.FlashPixIdentifier), new byte[] { 3, 4, 5 });

        ImageMetadataDocument document = ParseJpeg(new int[] { 226, 226 }, new byte[][] { mpf, flashPix }, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Profiles, Has.Count.EqualTo(2));
        Assert.That(document.Profiles[0].Kind, Is.EqualTo(MetadataProfileKind.Unknown));
        Assert.That(document.Profiles[0].Family, Is.EqualTo(MetadataFamily.Jpeg));
        Assert.That(document.Profiles[0].Key, Is.EqualTo("Jpeg.APP2.MPF"));
        Assert.That(document.Profiles[0].Name, Is.EqualTo("MPF"));
        Assert.That(document.Profiles[0].Data, Is.EqualTo(new byte[] { 1, 2 }));
        Assert.That(document.Profiles[1].Key, Is.EqualTo("Jpeg.APP2.FlashPix"));
        Assert.That(document.Profiles[1].Name, Is.EqualTo("FlashPix"));
        Assert.That(document.Profiles[1].Data, Is.EqualTo(new byte[] { 3, 4, 5 }));
        Assert.That(document.Directories, Has.Count.EqualTo(2));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jpeg.APP2.MPF.Identifier").Value.Text, Is.EqualTo("MPF"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jpeg.APP2.MPF.DataByteLength").Value.IntegerValue, Is.EqualTo(2));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jpeg.APP2.FlashPix.Identifier").Value.Text, Is.EqualTo("FPXR"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jpeg.APP2.FlashPix.DataByteLength").Value.IntegerValue, Is.EqualTo(3));
    }

    [Test]
    public void Parse_App12DuckyIsCapturedAsRawProfile()
    {
        byte[] ducky = Combine(Encoding.ASCII.GetBytes(JpegRawProfileExtractor.DuckyIdentifier), new byte[] { 10, 11, 12 });

        ImageMetadataDocument document = ParseJpeg(new int[] { 236 }, new byte[][] { ducky }, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Profiles, Has.Count.EqualTo(1));
        Assert.That(document.Profiles[0].Key, Is.EqualTo("Jpeg.APP12.Ducky"));
        Assert.That(document.Profiles[0].Name, Is.EqualTo("Ducky"));
        Assert.That(document.Profiles[0].Data, Is.EqualTo(new byte[] { 10, 11, 12 }));
        Assert.That(document.Directories[0].Group, Is.EqualTo("APP12"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jpeg.APP12.Ducky.Identifier").Value.Text, Is.EqualTo("Ducky"));
    }

    [Test]
    public void Parse_App11JumbfAndC2paAreCapturedWithSpecificProfileKinds()
    {
        byte[] jumbf = Combine(Encoding.ASCII.GetBytes(JpegRawProfileExtractor.JumbfIdentifier), new byte[] { 1, 2, 3 });
        byte[] c2pa = Combine(Encoding.ASCII.GetBytes(JpegRawProfileExtractor.C2paIdentifier), new byte[] { 4, 5 });

        ImageMetadataDocument document = ParseJpeg(new int[] { 235, 235 }, new byte[][] { jumbf, c2pa }, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Profiles, Has.Count.EqualTo(2));
        Assert.That(document.Profiles[0].Kind, Is.EqualTo(MetadataProfileKind.Jumbf));
        Assert.That(document.Profiles[0].Family, Is.EqualTo(MetadataFamily.Container));
        Assert.That(document.Profiles[0].Key, Is.EqualTo("Jumbf.APP11"));
        Assert.That(document.Profiles[0].Data, Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(document.Profiles[1].Kind, Is.EqualTo(MetadataProfileKind.C2pa));
        Assert.That(document.Profiles[1].Family, Is.EqualTo(MetadataFamily.Container));
        Assert.That(document.Profiles[1].Key, Is.EqualTo("C2pa.APP11"));
        Assert.That(document.Profiles[1].Data, Is.EqualTo(new byte[] { 4, 5 }));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jumbf.APP11.Identifier").Value.Text, Is.EqualTo("JUMBF"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "C2pa.APP11.Identifier").Value.Text, Is.EqualTo("C2PA"));
    }

    [Test]
    public void Parse_RawProfileLimitKeepsMetadataAndSkipsBufferedData()
    {
        byte[] mpf = Combine(Encoding.ASCII.GetBytes(JpegRawProfileExtractor.MpfIdentifier), new byte[] { 1, 2, 3, 4 });
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxProfileBytes = 2;

        ImageMetadataDocument document = ParseJpeg(new int[] { 226 }, new byte[][] { mpf }, options);

        Assert.That(document.Profiles, Has.Count.EqualTo(1));
        Assert.That(document.Profiles[0].Length, Is.EqualTo(4));
        Assert.That(document.Profiles[0].Data, Is.Empty);
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("jpeg.raw.profile.limit"));
        Assert.That(document.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.LimitExceeded));
    }

    [Test]
    public void Parse_UnknownRawProfileSegmentIsIgnored()
    {
        byte[] unknown = Encoding.ASCII.GetBytes("UNKNOWN");

        ImageMetadataDocument document = ParseJpeg(new int[] { 226 }, new byte[][] { unknown }, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Profiles, Is.Empty);
        Assert.That(document.Directories, Is.Empty);
    }

    public static ImageMetadataDocument ParseJpeg(int[] markers, byte[][] payloads, ImageMetadataReaderOptions options)
    {
        List<byte> bytes = new List<byte>();
        bytes.Add(255);
        bytes.Add(216);

        int index = 0;
        while (index < markers.Length)
        {
            AddSegment(bytes, markers[index], payloads[index]);
            index = index + 1;
        }

        bytes.Add(255);
        bytes.Add(217);

        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes.ToArray());
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return JpegMetadataParser.Parse(reader, ImageFormatMapping.CreateInfo(ImageFormatKind.Jpeg), options);
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
