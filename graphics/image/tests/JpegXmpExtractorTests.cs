using System.Collections.Generic;
using System.IO;
using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class JpegXmpExtractorTests
{
    [Test]
    public void Parse_StandardXmpApp1ExtractsProfileAndDirectory()
    {
        byte[] packet = Encoding.UTF8.GetBytes("<x:xmpmeta>ok</x:xmpmeta>");
        byte[] payload = Combine(Encoding.ASCII.GetBytes(JpegXmpExtractor.StandardXmpIdentifier), packet);
        ImageMetadataDocument document = ParseJpeg(new byte[][] { payload }, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Profiles, Has.Count.EqualTo(1));
        Assert.That(document.Profiles[0].Kind, Is.EqualTo(MetadataProfileKind.Xmp));
        Assert.That(document.Profiles[0].Family, Is.EqualTo(MetadataFamily.Xmp));
        Assert.That(document.Profiles[0].Key, Is.EqualTo("Xmp.APP1"));
        Assert.That(document.Profiles[0].Name, Is.EqualTo("XMP"));
        Assert.That(document.Profiles[0].MimeType, Is.EqualTo("application/rdf+xml"));
        Assert.That(document.Profiles[0].Offset, Is.EqualTo(6 + JpegXmpExtractor.StandardXmpIdentifier.Length));
        Assert.That(document.Profiles[0].Length, Is.EqualTo(packet.Length));
        Assert.That(document.Profiles[0].Text, Is.EqualTo("<x:xmpmeta>ok</x:xmpmeta>"));
        Assert.That(document.Profiles[0].Data, Is.EqualTo(packet));
        Assert.That(document.Directories, Has.Count.EqualTo(1));
        Assert.That(document.Directories[0].Family, Is.EqualTo(MetadataFamily.Xmp));
        Assert.That(document.Directories[0].Group, Is.EqualTo("APP1"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Xmp.APP1.Identifier").Value.Text, Is.EqualTo("XMP"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Xmp.APP1.PacketByteLength").Value.IntegerValue, Is.EqualTo(packet.Length));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Xmp.APP1.Extended").Value.BooleanValue, Is.False);
    }

    [Test]
    public void Parse_StandardXmpApp1ParsesRdfProperties()
    {
        string xml = "<rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\"><rdf:Description><dc:subject><rdf:Bag><rdf:li>portrait</rdf:li><rdf:li>studio</rdf:li></rdf:Bag></dc:subject></rdf:Description></rdf:RDF>";
        byte[] packet = Encoding.UTF8.GetBytes(xml);
        byte[] payload = Combine(Encoding.ASCII.GetBytes(JpegXmpExtractor.StandardXmpIdentifier), packet);

        ImageMetadataDocument document = ParseJpeg(new byte[][] { payload }, new ImageMetadataReaderOptions());

        MetadataTag subject = RequiredMetadataQuery.FindFirst(document, "Xmp.dc.subject");
        Assert.That(subject, Is.Not.Null);
        Assert.That(subject.Value.TextValues, Is.EqualTo(new[] { "portrait", "studio" }));
        Assert.That(document.Directories, Has.Some.Matches<MetadataDirectory>(directory => directory.Path == "JPEG/APP1/XMP/dc"));
    }

    [Test]
    public void Parse_ExtendedXmpChunksAreReassembledByGuid()
    {
        string guid = "1234567890abcdef1234567890abcdef";
        byte[] first = Encoding.UTF8.GetBytes("abcdef");
        byte[] second = Encoding.UTF8.GetBytes("ghijkl");
        byte[] secondPayload = CreateExtendedPayload(guid, 12, 6, second);
        byte[] firstPayload = CreateExtendedPayload(guid, 12, 0, first);

        ImageMetadataDocument document = ParseJpeg(new byte[][] { secondPayload, firstPayload }, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Directories, Has.Count.EqualTo(2));
        Assert.That(document.Directories[0].Name, Is.EqualTo("Extended XMP Chunk"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Xmp.APP1.Extended.Guid").Value.Text, Is.EqualTo(guid));
        Assert.That(document.Profiles, Has.Count.EqualTo(1));
        Assert.That(document.Profiles[0].Kind, Is.EqualTo(MetadataProfileKind.Xmp));
        Assert.That(document.Profiles[0].Key, Is.EqualTo("Xmp.APP1.Extended." + guid));
        Assert.That(document.Profiles[0].Name, Is.EqualTo("Extended XMP"));
        Assert.That(document.Profiles[0].Length, Is.EqualTo(12));
        Assert.That(document.Profiles[0].SequenceCount, Is.EqualTo(2));
        Assert.That(document.Profiles[0].Text, Is.EqualTo("abcdefghijkl"));
        Assert.That(document.Profiles[0].Data, Is.EqualTo(Encoding.UTF8.GetBytes("abcdefghijkl")));
    }

    [Test]
    public void Parse_IncompleteExtendedXmpAddsDiagnosticAndSkipsProfile()
    {
        string guid = "1234567890abcdef1234567890abcdef";
        byte[] chunk = Encoding.UTF8.GetBytes("abc");
        byte[] payload = CreateExtendedPayload(guid, 6, 0, chunk);

        ImageMetadataDocument document = ParseJpeg(new byte[][] { payload }, new ImageMetadataReaderOptions());

        Assert.That(document.Profiles, Is.Empty);
        Assert.That(document.Directories, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("jpeg.xmp.extended.incomplete"));
        Assert.That(document.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.Truncated));
    }

    [Test]
    public void Parse_ParseXmpFalseSkipsXmpExtraction()
    {
        byte[] packet = Encoding.UTF8.GetBytes("<x:xmpmeta>ok</x:xmpmeta>");
        byte[] payload = Combine(Encoding.ASCII.GetBytes(JpegXmpExtractor.StandardXmpIdentifier), packet);
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.ParseXmp = false;

        ImageMetadataDocument document = ParseJpeg(new byte[][] { payload }, options);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Profiles, Is.Empty);
        Assert.That(document.Directories, Is.Empty);
    }

    [Test]
    public void Parse_XmpXmlLimitSkipsBufferedProfileData()
    {
        byte[] packet = Encoding.UTF8.GetBytes("<x:xmpmeta>ok</x:xmpmeta>");
        byte[] payload = Combine(Encoding.ASCII.GetBytes(JpegXmpExtractor.StandardXmpIdentifier), packet);
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxXmlBytes = 4;

        ImageMetadataDocument document = ParseJpeg(new byte[][] { payload }, options);

        Assert.That(document.Profiles, Has.Count.EqualTo(1));
        Assert.That(document.Profiles[0].Data, Is.Empty);
        Assert.That(document.Profiles[0].Text, Is.EqualTo(""));
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("jpeg.xmp.profile.limit"));
        Assert.That(document.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.LimitExceeded));
    }

    public static ImageMetadataDocument ParseJpeg(byte[][] app1Payloads, ImageMetadataReaderOptions options)
    {
        List<byte> bytes = new List<byte>();
        bytes.Add(255);
        bytes.Add(216);

        int index = 0;
        while (index < app1Payloads.Length)
        {
            AddApp1(bytes, app1Payloads[index]);
            index = index + 1;
        }

        bytes.Add(255);
        bytes.Add(217);

        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes.ToArray());
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return JpegMetadataParser.Parse(reader, ImageFormatMapping.CreateInfo(ImageFormatKind.Jpeg), options);
    }

    public static void AddApp1(List<byte> bytes, byte[] payload)
    {
        int declaredLength = payload.Length + 2;
        bytes.Add(255);
        bytes.Add(225);
        bytes.Add((byte)(declaredLength / 256));
        bytes.Add((byte)(declaredLength & 255));
        bytes.AddRange(payload);
    }

    public static byte[] CreateExtendedPayload(string guid, long fullLength, long offset, byte[] chunk)
    {
        byte[] identifier = Encoding.ASCII.GetBytes(JpegXmpExtractor.ExtendedXmpIdentifier);
        byte[] guidBytes = Encoding.ASCII.GetBytes(guid);
        List<byte> payload = new List<byte>();
        payload.AddRange(identifier);
        payload.AddRange(guidBytes);
        AddUInt32(payload, fullLength);
        AddUInt32(payload, offset);
        payload.AddRange(chunk);
        return payload.ToArray();
    }

    public static void AddUInt32(List<byte> bytes, long value)
    {
        bytes.Add((byte)((value >> 24) & 255));
        bytes.Add((byte)((value >> 16) & 255));
        bytes.Add((byte)((value >> 8) & 255));
        bytes.Add((byte)(value & 255));
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
