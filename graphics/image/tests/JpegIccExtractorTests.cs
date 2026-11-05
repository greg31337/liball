using System.Collections.Generic;
using System.IO;
using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class JpegIccExtractorTests
{
    [Test]
    public void Parse_SingleIccApp2ChunkExtractsProfileAndDirectory()
    {
        byte[] payload = CreateIccPayload(1, 1, new byte[] { 1, 2, 3, 4 });

        ImageMetadataDocument document = ParseJpeg(new byte[][] { payload }, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Profiles, Has.Count.EqualTo(1));
        Assert.That(document.Profiles[0].Kind, Is.EqualTo(MetadataProfileKind.Icc));
        Assert.That(document.Profiles[0].Family, Is.EqualTo(MetadataFamily.Icc));
        Assert.That(document.Profiles[0].Key, Is.EqualTo("Icc.APP2"));
        Assert.That(document.Profiles[0].Name, Is.EqualTo("ICC Profile"));
        Assert.That(document.Profiles[0].MimeType, Is.EqualTo("application/vnd.iccprofile"));
        Assert.That(document.Profiles[0].SequenceNumber, Is.EqualTo(1));
        Assert.That(document.Profiles[0].SequenceCount, Is.EqualTo(1));
        Assert.That(document.Profiles[0].Data, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
        Assert.That(document.Summary.HasColorProfile, Is.True);
        Assert.That(document.Summary.ColorProfileName, Is.EqualTo("ICC Profile"));
        Assert.That(document.Directories, Has.Count.EqualTo(1));
        Assert.That(document.Directories[0].Family, Is.EqualTo(MetadataFamily.Icc));
        Assert.That(document.Directories[0].Group, Is.EqualTo("APP2"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Icc.APP2.Identifier").Value.Text, Is.EqualTo("ICC_PROFILE"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Icc.APP2.SequenceNumber").Value.IntegerValue, Is.EqualTo(1));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Icc.APP2.SequenceCount").Value.IntegerValue, Is.EqualTo(1));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Icc.APP2.ChunkByteLength").Value.IntegerValue, Is.EqualTo(4));
    }

    [Test]
    public void Parse_MultipleIccChunksAreReassembledBySequenceNumber()
    {
        byte[] second = CreateIccPayload(2, 2, new byte[] { 3, 4 });
        byte[] first = CreateIccPayload(1, 2, new byte[] { 1, 2 });

        ImageMetadataDocument document = ParseJpeg(new byte[][] { second, first }, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Directories, Has.Count.EqualTo(2));
        Assert.That(document.Profiles, Has.Count.EqualTo(1));
        Assert.That(document.Profiles[0].SequenceCount, Is.EqualTo(2));
        Assert.That(document.Profiles[0].Length, Is.EqualTo(4));
        Assert.That(document.Profiles[0].Data, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
    }

    [Test]
    public void Parse_MissingIccSequenceAddsDiagnosticAndSkipsProfile()
    {
        byte[] onlySecond = CreateIccPayload(2, 2, new byte[] { 3, 4 });

        ImageMetadataDocument document = ParseJpeg(new byte[][] { onlySecond }, new ImageMetadataReaderOptions());

        Assert.That(document.Profiles, Is.Empty);
        Assert.That(document.Directories, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("jpeg.icc.sequence.incomplete"));
        Assert.That(document.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.Truncated));
    }

    [Test]
    public void Parse_ParseIccFalseSkipsIccExtraction()
    {
        byte[] payload = CreateIccPayload(1, 1, new byte[] { 1, 2, 3, 4 });
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.ParseIcc = false;

        ImageMetadataDocument document = ParseJpeg(new byte[][] { payload }, options);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Profiles, Is.Empty);
        Assert.That(document.Directories, Is.Empty);
        Assert.That(document.Summary.HasColorProfile, Is.False);
    }

    [Test]
    public void Parse_IccProfileLimitKeepsMetadataAndSkipsBufferedData()
    {
        byte[] payload = CreateIccPayload(1, 1, new byte[] { 1, 2, 3, 4 });
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxProfileBytes = 2;

        ImageMetadataDocument document = ParseJpeg(new byte[][] { payload }, options);

        Assert.That(document.Profiles, Has.Count.EqualTo(1));
        Assert.That(document.Profiles[0].Length, Is.EqualTo(4));
        Assert.That(document.Profiles[0].Data, Is.Empty);
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("jpeg.icc.profile.limit"));
        Assert.That(document.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.LimitExceeded));
    }

    public static ImageMetadataDocument ParseJpeg(byte[][] app2Payloads, ImageMetadataReaderOptions options)
    {
        List<byte> bytes = new List<byte>();
        bytes.Add(255);
        bytes.Add(216);

        int index = 0;
        while (index < app2Payloads.Length)
        {
            AddApp2(bytes, app2Payloads[index]);
            index = index + 1;
        }

        bytes.Add(255);
        bytes.Add(217);

        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes.ToArray());
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return JpegMetadataParser.Parse(reader, ImageFormatMapping.CreateInfo(ImageFormatKind.Jpeg), options);
    }

    public static void AddApp2(List<byte> bytes, byte[] payload)
    {
        int declaredLength = payload.Length + 2;
        bytes.Add(255);
        bytes.Add(226);
        bytes.Add((byte)(declaredLength / 256));
        bytes.Add((byte)(declaredLength & 255));
        bytes.AddRange(payload);
    }

    public static byte[] CreateIccPayload(int sequenceNumber, int sequenceCount, byte[] data)
    {
        List<byte> payload = new List<byte>();
        payload.AddRange(Encoding.ASCII.GetBytes(JpegIccExtractor.IccIdentifier));
        payload.Add((byte)sequenceNumber);
        payload.Add((byte)sequenceCount);
        payload.AddRange(data);
        return payload.ToArray();
    }
}
