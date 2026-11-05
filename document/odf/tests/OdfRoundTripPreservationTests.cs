using System.IO.Compression;
using System.Text;

namespace odf.Tests;

public class OdfRoundTripPreservationTests
{
    [Test]
    public void FlatRoundTripPreservesFutureContentPrefixesLexicalValuesAndIds()
    {
        byte[] source = Encoding.UTF8.GetBytes(FutureFlatXml());
        IOdfDocumentReader reader = new OdfDocumentReader();
        IOdfDocumentWriter writer = new OdfDocumentWriter();
        OdfDocument document = reader.ReadDocumentBytes(
            source,
            "future.fodt",
            new OdfLoadOptions());

        byte[] written = writer.WriteDocumentBytes(
            document,
            new OdfSaveOptions());
        string xml = Encoding.UTF8.GetString(written);
        OdfDocument roundTrip = reader.ReadDocumentBytes(
            written,
            "future-roundtrip.fodt",
            new OdfLoadOptions());
        OdfNode paragraph = FindByXmlId(roundTrip, "paragraph-007");
        OdfNode futureSection = FindByXmlId(roundTrip, "future-section-009");

        Assert.Multiple(() =>
        {
            Assert.That(document.IsFutureVersion, Is.True);
            Assert.That(document.VersionInfo?.LexicalValue, Is.EqualTo("9.7"));
            Assert.That(xml, Does.Contain("<o:document"));
            Assert.That(xml, Does.Contain("<t:p"));
            Assert.That(xml, Does.Contain("<lo:vendor-token"));
            Assert.That(xml, Does.Contain("<v:future-section"));
            Assert.That(xml, Does.Contain("o:version=\"9.7\""));
            Assert.That(xml, Does.Contain("v:decimal=\"+001.2300\""));
            Assert.That(xml, Does.Contain("lo:number=\"0007\""));
            Assert.That(xml, Does.Contain("xml:id=\"paragraph-007\""));
            Assert.That(xml, Does.Contain("xml:id=\"future-section-009\""));
            Assert.That(roundTrip.IsFutureVersion, Is.True);
            Assert.That(roundTrip.VersionInfo?.LexicalValue, Is.EqualTo("9.7"));
            Assert.That(roundTrip.PassthroughNodes, Has.Length.EqualTo(1));
            Assert.That(paragraph.Name?.Prefix, Is.EqualTo("t"));
            Assert.That(ReadAttribute(paragraph, "urn:vendor:future", "decimal"),
                Is.EqualTo("+001.2300"));
            Assert.That(futureSection.Name?.Prefix, Is.EqualTo("v"));
            Assert.That(futureSection.Parent, Is.SameAs(roundTrip.Parts[0].Root));
        });
    }

    [Test]
    public void PackageRoundTripPreservesExtensionsAndArbitraryEntries()
    {
        byte[] binary = [0, 255, 1, 254, 2, 253, 3, 252];
        byte[] customXml = Encoding.UTF8.GetBytes(
            "<?keep exact?><vendor:payload xmlns:vendor='urn:vendor:opaque' "
                + "vendor:lexical='0000042'/>");
        byte[] source = CreatePackage(binary, customXml);
        IOdfDocumentReader documentReader = new OdfDocumentReader();
        IOdfDocumentWriter documentWriter = new OdfDocumentWriter();
        OdfDocument document = documentReader.ReadDocumentBytes(
            source,
            "preserve.odt",
            new OdfLoadOptions());

        byte[] written = documentWriter.WriteDocumentBytes(
            document,
            new OdfSaveOptions());
        OdfDocument roundTrip = documentReader.ReadDocumentBytes(
            written,
            "preserve-roundtrip.odt",
            new OdfLoadOptions());
        IOdfPackageReader packageReader = new OdfPackageReader();
        OdfPackage package = packageReader.ReadPackageBytes(
            written,
            new OdfLoadOptions());
        byte[] roundTripBinary = ReadEntry(
            packageReader,
            package,
            "Vendor/raw.bin");
        byte[] roundTripCustomXml = ReadEntry(
            packageReader,
            package,
            "META-INF/custom.xml");
        string contentXml = Encoding.UTF8.GetString(ReadEntry(
            packageReader,
            package,
            OdfPartNames.Content));
        string manifestXml = Encoding.UTF8.GetString(ReadEntry(
            packageReader,
            package,
            OdfPartNames.Manifest));
        OdfNode paragraph = FindByXmlId(roundTrip, "package-paragraph-003");

        Assert.Multiple(() =>
        {
            Assert.That(package.IsValid, Is.True);
            Assert.That(roundTrip.PackageKind, Is.EqualTo(OdfPackageKind.Package));
            Assert.That(roundTripBinary, Is.EqualTo(binary));
            Assert.That(roundTripCustomXml, Is.EqualTo(customXml));
            Assert.That(HasEntry(package, "Vendor/raw.bin"), Is.True);
            Assert.That(HasEntry(package, "META-INF/custom.xml"), Is.True);
            Assert.That(contentXml, Does.Contain("<o:document-content"));
            Assert.That(contentXml, Does.Contain("<lo:vendor-token"));
            Assert.That(contentXml, Does.Contain("<v:opaque"));
            Assert.That(contentXml, Does.Contain("v:code=\"000042.5000\""));
            Assert.That(contentXml,
                Does.Contain("xml:id=\"package-paragraph-003\""));
            Assert.That(manifestXml, Is.EqualTo(ManifestXml()));
            Assert.That(paragraph.Name?.Prefix, Is.EqualTo("t"));
            Assert.That(ReadAttribute(paragraph, "urn:vendor:package", "code"),
                Is.EqualTo("000042.5000"));
        });
    }

    [Test]
    public void PrefixPreservationCanBeDisabledWithoutChangingNamesOrIds()
    {
        IOdfDocumentReader reader = new OdfDocumentReader();
        IOdfDocumentWriter writer = new OdfDocumentWriter();
        OdfDocument document = reader.ReadDocumentBytes(
            Encoding.UTF8.GetBytes(FutureFlatXml()),
            "prefixes.fodt",
            new OdfLoadOptions());

        string preserved = Encoding.UTF8.GetString(writer.WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                PreserveLexicalValues = true
            }));
        byte[] canonicalBytes = writer.WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                PreserveLexicalValues = false
            });
        string canonical = Encoding.UTF8.GetString(canonicalBytes);
        OdfDocument roundTrip = reader.ReadDocumentBytes(
            canonicalBytes,
            "canonical.fodt",
            new OdfLoadOptions());
        OdfNode paragraph = FindByXmlId(roundTrip, "paragraph-007");
        OdfNode vendor = FindElement(
            paragraph,
            OdfNamespaces.LibreOffice,
            "vendor-token");

        Assert.Multiple(() =>
        {
            Assert.That(preserved, Does.Contain("<o:document"));
            Assert.That(preserved, Does.Contain("<t:p"));
            Assert.That(canonical, Does.Contain("<office:document"));
            Assert.That(canonical, Does.Contain("<text:p"));
            Assert.That(canonical, Does.Not.Contain("<o:document"));
            Assert.That(canonical, Does.Not.Contain("<t:p"));
            Assert.That(paragraph.Name?.NamespaceName,
                Is.EqualTo(OdfNamespaces.Text));
            Assert.That(paragraph.XmlId, Is.EqualTo("paragraph-007"));
            Assert.That(vendor.Name?.NamespaceName,
                Is.EqualTo(OdfNamespaces.LibreOffice));
            Assert.That(ReadAttribute(vendor, OdfNamespaces.LibreOffice, "number"),
                Is.EqualTo("0007"));
        });
    }

    static string FutureFlatXml()
    {
        return "<o:document xmlns:o='"
            + OdfNamespaces.Office
            + "' xmlns:t='"
            + OdfNamespaces.Text
            + "' xmlns:lo='"
            + OdfNamespaces.LibreOffice
            + "' xmlns:v='urn:vendor:future' o:version='9.7' o:mimetype='"
            + OdfMediaTypes.Text
            + "' v:root-token='00001'><o:body><o:text>"
            + "<t:p xml:id='paragraph-007' v:decimal='+001.2300'>"
            + "before<lo:vendor-token lo:number='0007'/>after</t:p>"
            + "</o:text></o:body><v:future-section "
            + "xml:id='future-section-009' v:date='02026-09-02'/>"
            + "</o:document>";
    }

    static byte[] CreatePackage(byte[] binary, byte[] customXml)
    {
        using MemoryStream output = new MemoryStream();
        using (ZipArchive archive = new ZipArchive(
            output,
            ZipArchiveMode.Create,
            true))
        {
            WriteEntry(
                archive,
                OdfPartNames.Mimetype,
                Encoding.UTF8.GetBytes(OdfMediaTypes.Text),
                CompressionLevel.NoCompression);
            WriteEntry(
                archive,
                OdfPartNames.Content,
                Encoding.UTF8.GetBytes(PackageContentXml()),
                CompressionLevel.SmallestSize);
            WriteEntry(
                archive,
                OdfPartNames.Manifest,
                Encoding.UTF8.GetBytes(ManifestXml()),
                CompressionLevel.SmallestSize);
            WriteEntry(
                archive,
                "Vendor/raw.bin",
                binary,
                CompressionLevel.NoCompression);
            WriteEntry(
                archive,
                "META-INF/custom.xml",
                customXml,
                CompressionLevel.SmallestSize);
        }
        return output.ToArray();
    }

    static string PackageContentXml()
    {
        return "<o:document-content xmlns:o='"
            + OdfNamespaces.Office
            + "' xmlns:t='"
            + OdfNamespaces.Text
            + "' xmlns:lo='"
            + OdfNamespaces.LibreOffice
            + "' xmlns:v='urn:vendor:package' o:version='1.4'>"
            + "<o:body><o:text><t:p xml:id='package-paragraph-003' "
            + "v:code='000042.5000'>before<lo:vendor-token "
            + "lo:number='0005'/><v:opaque v:value='+0006'/>after</t:p>"
            + "</o:text></o:body></o:document-content>";
    }

    static string ManifestXml()
    {
        return "<m:manifest xmlns:m='"
            + OdfNamespaces.Manifest
            + "' xmlns:v='urn:vendor:manifest' m:version='1.4' v:token='0009'>"
            + ManifestEntry(OdfPartNames.Root, OdfMediaTypes.Text)
            + ManifestEntry(OdfPartNames.Content, OdfMediaTypes.Xml)
            + ManifestEntry("Vendor/raw.bin", OdfMediaTypes.OctetStream)
            + ManifestEntry("META-INF/custom.xml", OdfMediaTypes.Xml)
            + "<v:manifest-extension v:value='+0010'/>"
            + "</m:manifest>";
    }

    static string ManifestEntry(string path, string mediaType)
    {
        return "<m:file-entry m:full-path='"
            + path
            + "' m:media-type='"
            + mediaType
            + "'/>";
    }

    static void WriteEntry(
        ZipArchive archive,
        string path,
        byte[] data,
        CompressionLevel compression)
    {
        ZipArchiveEntry entry = archive.CreateEntry(path, compression);
        using Stream output = entry.Open();
        output.Write(data, 0, data.Length);
    }

    static byte[] ReadEntry(
        IOdfPackageReader reader,
        OdfPackage package,
        string path)
    {
        using Stream input = reader.OpenPackageEntry(package, path);
        using MemoryStream output = new MemoryStream();
        input.CopyTo(output);
        return output.ToArray();
    }

    static bool HasEntry(OdfPackage package, string path)
    {
        int index = 0;
        while (index < package.Entries.Length)
        {
            if (package.Entries[index].Path == path)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    static OdfNode FindByXmlId(OdfDocument document, string xmlId)
    {
        int index = 0;
        while (index < document.Parts.Length)
        {
            OdfNode? result = FindOptionalByXmlId(
                document.Parts[index].Root,
                xmlId);
            if (result != null)
            {
                return result;
            }
            index++;
        }
        Assert.Fail($"XML ID '{xmlId}' was not found.");
        return new OdfNode();
    }

    static OdfNode? FindOptionalByXmlId(OdfNode? node, string xmlId)
    {
        if (node == null)
        {
            return null;
        }
        if (node.XmlId == xmlId)
        {
            return node;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode? result = FindOptionalByXmlId(node.Children[index], xmlId);
            if (result != null)
            {
                return result;
            }
            index++;
        }
        return null;
    }

    static OdfNode FindElement(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        if (node.Kind == OdfNodeKind.Element
            && node.Name?.NamespaceName == namespaceName
            && node.Name.LocalName == localName)
        {
            return node;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode? result = FindOptionalElement(
                node.Children[index],
                namespaceName,
                localName);
            if (result != null)
            {
                return result;
            }
            index++;
        }
        Assert.Fail($"Element '{{{namespaceName}}}{localName}' was not found.");
        return new OdfNode();
    }

    static OdfNode? FindOptionalElement(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        if (node.Kind == OdfNodeKind.Element
            && node.Name?.NamespaceName == namespaceName
            && node.Name.LocalName == localName)
        {
            return node;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode? result = FindOptionalElement(
                node.Children[index],
                namespaceName,
                localName);
            if (result != null)
            {
                return result;
            }
            index++;
        }
        return null;
    }

    static string ReadAttribute(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            if (attribute.Name.NamespaceName == namespaceName
                && attribute.Name.LocalName == localName)
            {
                return attribute.Value;
            }
            index++;
        }
        return "";
    }
}
