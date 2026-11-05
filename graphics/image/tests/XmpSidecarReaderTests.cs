using System.IO;
using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class XmpSidecarReaderTests
{
    [Test]
    public void ReadStream_DetectsPreservesParsesAndNormalizesSidecar()
    {
        string xml = Packet("""
            <dc:title><rdf:Alt><rdf:li xml:lang="x-default">Sidecar title</rdf:li><rdf:li xml:lang="fr">Titre</rdf:li></rdf:Alt></dc:title>
            <dc:subject><rdf:Bag><rdf:li>ocean</rdf:li><rdf:li>sunset</rdf:li></rdf:Bag></dc:subject>
            """);
        byte[] bytes = Encoding.UTF8.GetBytes(xml);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Xmp));
        Assert.That(document.Format.Name, Is.EqualTo("XMP"));
        Assert.That(document.Format.MimeType, Is.EqualTo("application/rdf+xml"));
        Assert.That(document.Format.PreferredExtension, Is.EqualTo(".xmp"));
        Assert.That(document.Format.DetectedByMagic, Is.True);
        Assert.That(document.ByteLength, Is.EqualTo(bytes.Length));
        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Profiles, Has.Count.EqualTo(1));
        Assert.That(document.Profiles[0].Kind, Is.EqualTo(MetadataProfileKind.Xmp));
        Assert.That(document.Profiles[0].Key, Is.EqualTo("Xmp.Sidecar"));
        Assert.That(document.Profiles[0].Data, Is.EqualTo(bytes));
        Assert.That(document.Profiles[0].Text, Is.EqualTo(xml));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Xmp.dc.title").Value.Text, Is.EqualTo("Sidecar title"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Xmp.dc.title.fr").Value.Text, Is.EqualTo("Titre"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Xmp.dc.subject").Value.TextValues, Is.EqualTo(new[] { "ocean", "sunset" }));
        Assert.That(document.Summary.Title, Is.EqualTo("Sidecar title"));
        Assert.That(document.Summary.Keywords, Is.EqualTo(new[] { "ocean", "sunset" }));
        Assert.That(document.RawBlocks, Has.Count.EqualTo(1));
        Assert.That(document.RawBlocks[0].Data, Is.Empty);
    }

    [Test]
    public void ReadStream_DetectsAndDecodesUtf16Sidecar()
    {
        string xml = Packet("<xmp:Rating>5</xmp:Rating>");
        byte[] content = Encoding.Unicode.GetBytes(xml);
        byte[] bytes = new byte[content.Length + 2];
        bytes[0] = 255;
        bytes[1] = 254;
        System.Array.Copy(content, 0, bytes, 2, content.Length);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Xmp));
        Assert.That(document.Profiles[0].Text, Is.EqualTo(xml));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Xmp.xmp.Rating").Value.IntegerValue, Is.EqualTo(5));
        Assert.That(document.Summary.Rating, Is.EqualTo(5));
    }

    [Test]
    public void ReadFile_XmpExtensionProvidesFallbackForAlternateRdfPrefix()
    {
        string xml = new string(' ', ImageFormatDetectorCore.StreamProbeLength + 1)
            + "<r:RDF xmlns:r=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\"><r:Description dc:format=\"image/raw\" /></r:RDF>";
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "alternate-prefix.XMP");
        File.WriteAllText(path, xml, new UTF8Encoding(false));

        ImageMetadataDocument document = new ImageMetadataReader().ReadFile(path);

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Xmp));
        Assert.That(document.Format.DetectedByMagic, Is.False);
        Assert.That(document.Format.Confidence, Is.EqualTo(60));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Xmp.dc.format").Value.Text, Is.EqualTo("image/raw"));
        Assert.That(document.Diagnostics, Is.Empty);

        File.Delete(path);
    }

    [Test]
    public void ReadStream_ParseXmpFalseSkipsPacketExtraction()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(Packet("<xmp:CreatorTool>Tool</xmp:CreatorTool>"));
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.ParseXmp = false;

        ImageMetadataDocument document = new ImageMetadataReader().ReadStreamWithOptions(new MemoryStream(bytes), options);

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Xmp));
        Assert.That(document.Profiles, Is.Empty);
        Assert.That(document.Directories, Is.Empty);
        Assert.That(document.RawBlocks, Is.Empty);
        Assert.That(document.Diagnostics, Is.Empty);
    }

    [Test]
    public void ReadStream_XmlLimitPreservesProfileIdentityWithoutPayload()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(Packet("<xmp:CreatorTool>Tool</xmp:CreatorTool>"));
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxXmlBytes = 32;

        ImageMetadataDocument document = new ImageMetadataReader().ReadStreamWithOptions(new MemoryStream(bytes), options);

        Assert.That(document.Profiles, Has.Count.EqualTo(1));
        Assert.That(document.Profiles[0].Length, Is.EqualTo(bytes.Length));
        Assert.That(document.Profiles[0].Data, Is.Empty);
        Assert.That(document.Profiles[0].Text, Is.Empty);
        Assert.That(document.Directories, Is.Empty);
        Assert.That(document.Diagnostics, Has.Some.Matches<MetadataDiagnostic>(item => item.Code == "xmp.sidecar.limit" && item.Severity == MetadataSeverity.LimitExceeded));
    }

    [Test]
    public void ReadFile_InvalidXmpExtensionReportsSignatureDiagnostic()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "invalid-sidecar.xmp");
        File.WriteAllText(path, "<metadata>not xmp</metadata>", new UTF8Encoding(false));

        ImageMetadataDocument document = new ImageMetadataReader().ReadFile(path);

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Xmp));
        Assert.That(document.Profiles, Has.Count.EqualTo(1));
        Assert.That(document.Directories, Is.Empty);
        Assert.That(document.Diagnostics, Has.Some.Matches<MetadataDiagnostic>(item => item.Code == "xmp.sidecar.signature.invalid"));

        File.Delete(path);
    }

    [Test]
    public void ReadStream_CaptureRawBlockDataHonorsRawLimit()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(Packet("<xmp:Label>Selected</xmp:Label>"));
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.CaptureRawBlockData = true;

        ImageMetadataDocument document = new ImageMetadataReader().ReadStreamWithOptions(new MemoryStream(bytes), options);

        Assert.That(document.RawBlocks[0].Data, Is.EqualTo(bytes));

        options.MaxRawBlockBytes = 8;
        ImageMetadataDocument limited = new ImageMetadataReader().ReadStreamWithOptions(new MemoryStream(bytes), options);
        Assert.That(limited.RawBlocks[0].Data, Is.Empty);
        Assert.That(limited.Diagnostics, Has.Some.Matches<MetadataDiagnostic>(item => item.Code == "xmp.sidecar.raw.limit"));
    }

    public static string Packet(string properties)
    {
        return "<?xpacket begin=\"\uFEFF\" id=\"W5M0MpCehiHzreSzNTczkc9d\"?>"
            + "<x:xmpmeta xmlns:x=\"adobe:ns:meta/\"><rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:xmp=\"http://ns.adobe.com/xap/1.0/\"><rdf:Description>"
            + properties
            + "</rdf:Description></rdf:RDF></x:xmpmeta><?xpacket end=\"w\"?>";
    }
}
