using System.Collections.Generic;
using System.IO;
using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class JpegPhotoshopExtractorTests
{
    [Test]
    public void Parse_App13PhotoshopIrbExtractsProfilesDirectoriesAndIptcTags()
    {
        byte[] iptc = BuildIptcData();
        byte[] payload = BuildPhotoshopPayload(new byte[][] { BuildResource(1028, "", iptc) });

        ImageMetadataDocument document = ParseJpeg(payload, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Profiles, Has.Count.EqualTo(2));
        Assert.That(document.Profiles[0].Kind, Is.EqualTo(MetadataProfileKind.PhotoshopImageResource));
        Assert.That(document.Profiles[0].Family, Is.EqualTo(MetadataFamily.Photoshop));
        Assert.That(document.Profiles[0].Key, Is.EqualTo("Photoshop.APP13"));
        Assert.That(document.Profiles[0].Data, Is.EqualTo(payload));
        Assert.That(document.Profiles[1].Kind, Is.EqualTo(MetadataProfileKind.Iptc));
        Assert.That(document.Profiles[1].Family, Is.EqualTo(MetadataFamily.Iptc));
        Assert.That(document.Profiles[1].Key, Is.EqualTo("Iptc.APP13"));
        Assert.That(document.Profiles[1].Data, Is.EqualTo(iptc));
        Assert.That(document.Directories, Has.Count.EqualTo(3));
        Assert.That(document.Directories[0].Family, Is.EqualTo(MetadataFamily.Photoshop));
        Assert.That(document.Directories[0].Group, Is.EqualTo("APP13"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Photoshop.APP13.Identifier").Value.Text, Is.EqualTo("Photoshop 3.0"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Photoshop.APP13.ResourceCount").Value.IntegerValue, Is.EqualTo(1));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Photoshop.IRB.ResourceId").Value.IntegerValue, Is.EqualTo(1028));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Photoshop.IRB.DataByteLength").Value.IntegerValue, Is.EqualTo(iptc.Length));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Iptc.Application2.Byline").Value.Text, Is.EqualTo("Ada"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Iptc.Application2.Caption").Value.Text, Is.EqualTo("Caption text"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Iptc.Application2.CopyrightNotice").Value.Text, Is.EqualTo("Copyright 2026"));
        Assert.That(new ImageMetadataQuery().Find(document, "Iptc.Application2.Keywords"), Has.Count.EqualTo(2));
    }

    [Test]
    public void Parse_App13ResourceNameAndPaddingAreHandled()
    {
        byte[] resource = BuildResource(1005, "Alpha", new byte[] { 1, 2, 3 });
        byte[] payload = BuildPhotoshopPayload(new byte[][] { resource });

        ImageMetadataDocument document = ParseJpeg(payload, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Profiles, Has.Count.EqualTo(1));
        Assert.That(document.Directories, Has.Count.EqualTo(2));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Photoshop.IRB.ResourceId").Value.IntegerValue, Is.EqualTo(1005));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Photoshop.IRB.Name").Value.Text, Is.EqualTo("Alpha"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Photoshop.IRB.DataByteLength").Value.IntegerValue, Is.EqualTo(3));
    }

    [Test]
    public void Parse_PhotoshopProfileLimitKeepsIptcProfileAndAddsDiagnostic()
    {
        byte[] iptc = BuildIptcData();
        byte[] payload = BuildPhotoshopPayload(new byte[][] { BuildResource(1028, "", iptc) });
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxProfileBytes = 4;

        ImageMetadataDocument document = ParseJpeg(payload, options);

        Assert.That(document.Profiles, Has.Count.EqualTo(2));
        Assert.That(document.Profiles[0].Data, Is.Empty);
        Assert.That(document.Profiles[1].Data, Is.Empty);
        Assert.That(document.Diagnostics, Has.Count.EqualTo(2));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("jpeg.photoshop.profile.limit"));
        Assert.That(document.Diagnostics[1].Code, Is.EqualTo("jpeg.iptc.profile.limit"));
    }

    [Test]
    public void Parse_InvalidIrbSignatureAddsDiagnostic()
    {
        List<byte> payload = new List<byte>();
        payload.AddRange(Encoding.ASCII.GetBytes(JpegPhotoshopExtractor.PhotoshopIdentifier));
        payload.AddRange(Encoding.ASCII.GetBytes("BAD!"));
        payload.Add(4);
        payload.Add(4);
        payload.Add(0);
        payload.Add(0);
        payload.Add(0);
        payload.Add(0);
        payload.Add(0);
        payload.Add(0);

        ImageMetadataDocument document = ParseJpeg(payload.ToArray(), new ImageMetadataReaderOptions());

        Assert.That(document.Profiles, Has.Count.EqualTo(1));
        Assert.That(document.Directories, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("jpeg.photoshop.irb.signature.invalid"));
        Assert.That(document.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.RecoverableError));
    }

    [Test]
    public void Parse_TruncatedIptcDatasetAddsDiagnostic()
    {
        byte[] iptc = new byte[] { 28, 2, 80, 0, 5, 65, 100 };
        byte[] payload = BuildPhotoshopPayload(new byte[][] { BuildResource(1028, "", iptc) });

        ImageMetadataDocument document = ParseJpeg(payload, new ImageMetadataReaderOptions());

        Assert.That(document.Profiles, Has.Count.EqualTo(2));
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("jpeg.iptc.dataset.value.truncated"));
        Assert.That(document.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.Truncated));
    }

    public static ImageMetadataDocument ParseJpeg(byte[] app13Payload, ImageMetadataReaderOptions options)
    {
        List<byte> bytes = new List<byte>();
        bytes.Add(255);
        bytes.Add(216);
        AddApp13(bytes, app13Payload);
        bytes.Add(255);
        bytes.Add(217);

        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes.ToArray());
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return JpegMetadataParser.Parse(reader, ImageFormatMapping.CreateInfo(ImageFormatKind.Jpeg), options);
    }

    public static void AddApp13(List<byte> bytes, byte[] payload)
    {
        int declaredLength = payload.Length + 2;
        bytes.Add(255);
        bytes.Add(237);
        bytes.Add((byte)(declaredLength / 256));
        bytes.Add((byte)(declaredLength & 255));
        bytes.AddRange(payload);
    }

    public static byte[] BuildPhotoshopPayload(byte[][] resources)
    {
        List<byte> payload = new List<byte>();
        payload.AddRange(Encoding.ASCII.GetBytes(JpegPhotoshopExtractor.PhotoshopIdentifier));

        int index = 0;
        while (index < resources.Length)
        {
            payload.AddRange(resources[index]);
            index = index + 1;
        }

        return payload.ToArray();
    }

    public static byte[] BuildResource(int resourceId, string name, byte[] data)
    {
        List<byte> bytes = new List<byte>();
        bytes.AddRange(Encoding.ASCII.GetBytes("8BIM"));
        bytes.Add((byte)(resourceId / 256));
        bytes.Add((byte)(resourceId & 255));
        byte[] nameBytes = Encoding.ASCII.GetBytes(name);
        bytes.Add((byte)nameBytes.Length);
        bytes.AddRange(nameBytes);
        if (((1 + nameBytes.Length) & 1) != 0)
        {
            bytes.Add(0);
        }

        AddInt32(bytes, data.Length);
        bytes.AddRange(data);
        if ((data.Length & 1) != 0)
        {
            bytes.Add(0);
        }

        return bytes.ToArray();
    }

    public static byte[] BuildIptcData()
    {
        List<byte> bytes = new List<byte>();
        AddIptcDataset(bytes, 80, "Ada");
        AddIptcDataset(bytes, 25, "one");
        AddIptcDataset(bytes, 25, "two");
        AddIptcDataset(bytes, 120, "Caption text");
        AddIptcDataset(bytes, 116, "Copyright 2026");
        return bytes.ToArray();
    }

    public static void AddIptcDataset(List<byte> bytes, int dataset, string text)
    {
        byte[] value = Encoding.UTF8.GetBytes(text);
        bytes.Add(28);
        bytes.Add(2);
        bytes.Add((byte)dataset);
        bytes.Add((byte)(value.Length / 256));
        bytes.Add((byte)(value.Length & 255));
        bytes.AddRange(value);
    }

    public static void AddInt32(List<byte> bytes, int value)
    {
        bytes.Add((byte)((value >> 24) & 255));
        bytes.Add((byte)((value >> 16) & 255));
        bytes.Add((byte)((value >> 8) & 255));
        bytes.Add((byte)(value & 255));
    }
}
